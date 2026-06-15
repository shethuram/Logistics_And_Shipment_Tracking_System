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

    public DisputeService(
        IDisputeRepository disputeRepo,
        IShipmentRepository shipmentRepo,
        INotificationService notificationService,
        ILlmService llmService,
        AppDbContext db)
    {
        _disputeRepo = disputeRepo;
        _shipmentRepo = shipmentRepo;
        _notificationService = notificationService;
        _llmService = llmService;
        _db = db;
    }

    public async Task<RaiseDisputeResponse> RaiseDisputeAsync(RaiseDisputeRequest request, Guid customerId)
    {
        var shipment = await _shipmentRepo.GetByIdAsync(request.ShipmentId);
        if (shipment == null)
        {
            throw new NotFoundException("Shipment not found.");
        }

        if (shipment.CustomerId != customerId)
        {
            throw new ForbiddenException("You are not authorized to raise a dispute for this shipment.");
        }

        var exists = await _db.Disputes.AnyAsync(d => d.ShipmentId == request.ShipmentId);
        if (exists)
        {
            throw new ConflictException("A dispute has already been raised for this shipment.");
        }

        var (summary, type, suggestedResolution) = await _llmService.AnalyzeDisputeAsync(request.ComplaintText);

        var dispute = new Dispute
        {
            Id = Guid.NewGuid(),
            ShipmentId = request.ShipmentId,
            RaisedBy = customerId,
            ComplaintText = request.ComplaintText,
            LlmSummary = summary,
            LlmType = type,
            LlmSuggestedResolution = suggestedResolution,
            Status = DisputeStatus.OPEN,
            CreatedAt = DateTime.UtcNow
        };

        await _disputeRepo.AddAsync(dispute);

        await _notificationService.BroadcastAdminAlertAsync("NEW_DISPUTE", new { DisputeId = dispute.Id, OrderId = shipment.OrderId });

        return dispute.ToRaiseDisputeResponse();
    }

    public async Task<PagedResult<DisputeAdminDto>> GetDisputesAsync(string? status, int page, int pageSize, string role)
    {
        if (role != "ADMIN")
        {
            throw new ForbiddenException("Only admins can view disputes.");
        }

        DisputeStatus? ds = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<DisputeStatus>(status, out var parsedStatus))
            {
                ds = parsedStatus;
            }
            else
            {
                throw new ValidationException($"Invalid dispute status: {status}");
            }
        }

        var (items, total) = await _disputeRepo.GetDisputesAsync(ds, page, pageSize);

        return new PagedResult<DisputeAdminDto>
        {
            Data = items.Select(d => d.ToDisputeAdminDto()).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ResolveDisputeResponse> ResolveDisputeAsync(Guid id, ResolveDisputeRequest request, Guid adminUserId, string role)
    {
        if (role != "ADMIN")
        {
            throw new ForbiddenException("Only admins can resolve disputes.");
        }

        var dispute = await _disputeRepo.GetByIdAsync(id);
        if (dispute == null)
        {
            throw new NotFoundException("Dispute not found.");
        }

        if (dispute.Status != DisputeStatus.OPEN)
        {
            throw new BusinessRuleException("Dispute is already resolved.");
        }

        if (!Enum.TryParse<DisputeStatus>(request.Status, out var targetStatus) || 
            (targetStatus != DisputeStatus.RESOLVED && targetStatus != DisputeStatus.ESCALATED))
        {
            throw new ValidationException($"Invalid resolution status: {request.Status}. Status must be RESOLVED or ESCALATED.");
        }

        dispute.Status = targetStatus;
        dispute.ResolvedBy = adminUserId;
        dispute.ResolutionNotes = request.ResolutionNotes;
        dispute.ResolvedAt = DateTime.UtcNow;

        await _disputeRepo.UpdateAsync(dispute);

        await _notificationService.CreateNotificationAsync(dispute.Shipment.CustomerId, dispute.ShipmentId, "Dispute Resolved", $"Your dispute for shipment {dispute.Shipment.OrderId} has been resolved.");

        return dispute.ToResolveDisputeResponse();
    }
}
