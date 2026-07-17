using Microsoft.Extensions.Logging;
using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

public class DisputeService : IDisputeService
{
    private readonly IDisputeRepository _disputeRepo;
    private readonly IShipmentRepository _shipmentRepo;
    private readonly INotificationService _notificationService;
    private readonly ILlmService _llmService;
    private readonly AppDbContext _db;
    private readonly ILogger<DisputeService> _logger;

    public DisputeService(
        IDisputeRepository disputeRepo,
        IShipmentRepository shipmentRepo,
        INotificationService notificationService,
        ILlmService llmService,
        AppDbContext db,
        ILogger<DisputeService> logger)
    {
        _disputeRepo = disputeRepo;
        _shipmentRepo = shipmentRepo;
        _notificationService = notificationService;
        _llmService = llmService;
        _db = db;
        _logger = logger;
    }

    public async Task<RaiseDisputeResponse> RaiseDisputeAsync(Shipment shipment, string complaintText, Guid customerId)
    {
        var exists = await _db.Disputes.AnyAsync(d => d.ShipmentId == shipment.Id);
        if (exists)
        {
            throw new ConflictException("A dispute has already been raised for this shipment.");
        }

        var (summary, type, suggestedResolution) = await _llmService.AnalyzeDisputeAsync(complaintText);

        var dispute = new Dispute
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            RaisedBy = customerId,
            ComplaintText = complaintText,
            LlmSummary = summary,
            LlmType = type,
            LlmSuggestedResolution = suggestedResolution,
            Status = DisputeStatus.OPEN,
            CreatedAt = DateTime.UtcNow
        };

        await _disputeRepo.AddAsync(dispute);
        _logger.LogInformation("Dispute {DisputeId} raised for Shipment {ShipmentId} ({OrderId}) by Customer {CustomerId}.", dispute.Id, shipment.Id, shipment.OrderId, customerId);

        await _notificationService.BroadcastAdminAlertAsync("NEW_DISPUTE", new { DisputeId = dispute.Id, OrderId = shipment.OrderId });

        return dispute.ToRaiseDisputeResponse();
    }

    public async Task<PagedResult<DisputeAdminDto>> GetDisputesAsync(DisputeStatus? status, int page, int pageSize)
    {
        var ds = status;

        var (items, total) = await _disputeRepo.GetDisputesAsync(ds, page, pageSize);

        return new PagedResult<DisputeAdminDto>
        {
            Data = items.Select(d => d.ToDisputeAdminDto()).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ResolveDisputeResponse> ResolveDisputeAsync(Guid id, ResolveDisputeRequest request, Guid adminUserId)
    {
        var dispute = await _disputeRepo.GetByIdAsync(id);
        if (dispute == null)
        {
            throw new NotFoundException("Dispute not found.");
        }

        if (dispute.Status != DisputeStatus.OPEN)
        {
            throw new BusinessRuleException("Dispute is already resolved.");
        }

        if (request.Status != DisputeStatus.RESOLVED && request.Status != DisputeStatus.ESCALATED)
        {
            throw new ValidationException($"Invalid resolution status: {request.Status}. Status must be RESOLVED or ESCALATED.");
        }

        dispute.Status = request.Status;
        dispute.ResolvedBy = adminUserId;
        dispute.ResolutionNotes = request.ResolutionNotes;
        dispute.ResolvedAt = DateTime.UtcNow;

        await _disputeRepo.UpdateAsync(dispute);
        _logger.LogInformation("Dispute {DisputeId} resolved as {Status} by Admin {AdminId}.", dispute.Id, request.Status, adminUserId);

        await _notificationService.CreateNotificationAsync(dispute.Shipment.CustomerId, dispute.ShipmentId, "Dispute Resolved", $"Your dispute for shipment {dispute.Shipment.OrderId} has been resolved.");

        return dispute.ToResolveDisputeResponse();
    }

    public async Task<IEnumerable<DisputeResponse>> GetMyDisputesAsync(Guid customerId)
    {
        var disputes = await _db.Disputes
            .Include(d => d.Shipment)
            .Where(d => d.RaisedBy == customerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return disputes.Select(d => new DisputeResponse
        {
            Id = d.Id,
            ShipmentId = d.ShipmentId,
            OrderId = d.Shipment.OrderId,
            ComplaintText = d.ComplaintText,
            Status = d.Status,
            ResolutionNotes = d.ResolutionNotes,
            CreatedAt = d.CreatedAt,
            ResolvedAt = d.ResolvedAt
        });
    }
}
