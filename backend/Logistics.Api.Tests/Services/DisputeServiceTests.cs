using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class DisputeServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly Mock<IDisputeRepository> _disputeRepoMock;
    private readonly Mock<IShipmentRepository> _shipmentRepoMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILlmService> _llmServiceMock;
    private readonly DisputeService _service;

    public DisputeServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _disputeRepoMock = new Mock<IDisputeRepository>();
        _shipmentRepoMock = new Mock<IShipmentRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
        _llmServiceMock = new Mock<ILlmService>();

        _service = new DisputeService(
            _disputeRepoMock.Object,
            _shipmentRepoMock.Object,
            _notificationServiceMock.Object,
            _llmServiceMock.Object,
            _db,
            new Mock<Microsoft.Extensions.Logging.ILogger<DisputeService>>().Object
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task RaiseDisputeAsync_DuplicateDispute_ThrowsConflictException()
    {
        var customerId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        
        var user = new User { Id = customerId, FullName = "Customer", Email = "c@example.com", Phone = "98765", Role = UserRole.CUSTOMER };
        _db.Users.Add(user);
        
        var shipment = new Shipment { Id = shipmentId, CustomerId = customerId, OrderId = "TRK-001" };
        _db.Shipments.Add(shipment);
        _db.SaveChanges();

        var existingDispute = new Dispute { Id = Guid.NewGuid(), ShipmentId = shipmentId, RaisedBy = customerId, ComplaintText = "First text" };
        _db.Disputes.Add(existingDispute);
        _db.SaveChanges();

        await Assert.ThrowsAsync<ConflictException>(() => _service.RaiseDisputeAsync(shipment, "Second text", customerId));
    }

    [Fact]
    public async Task RaiseDisputeAsync_ValidInputs_CreatesDisputeAndCallsLlm()
    {
        var customerId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, CustomerId = customerId, OrderId = "TRK-001" };

        _llmServiceMock.Setup(l => l.AnalyzeDisputeAsync("Complaint"))
            .ReturnsAsync(("Summary", DisputeLlmType.WRONG_ADDRESS, "Resolution"));

        var result = await _service.RaiseDisputeAsync(shipment, "Complaint", customerId);

        Assert.NotNull(result);
        Assert.Equal(DisputeStatus.OPEN, result.Status);
        
        _disputeRepoMock.Verify(r => r.AddAsync(It.Is<Dispute>(d => 
            d.ShipmentId == shipmentId && 
            d.LlmSummary == "Summary" && 
            d.LlmType == DisputeLlmType.WRONG_ADDRESS && 
            d.LlmSuggestedResolution == "Resolution"
        )), Times.Once);

        _notificationServiceMock.Verify(n => n.BroadcastAdminAlertAsync("NEW_DISPUTE", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetDisputesAsync_ValidInputs_ReturnsPagedResult()
    {
        var disputes = new List<Dispute>
        {
            new Dispute 
            { 
                Id = Guid.NewGuid(), 
                Status = DisputeStatus.OPEN, 
                ComplaintText = "Complaint",
                RaisedByUser = new User { FullName = "Customer Name" },
                Shipment = new Shipment { OrderId = "TRK-001" }
            }
        };
        _disputeRepoMock.Setup(r => r.GetDisputesAsync(DisputeStatus.OPEN, 1, 20))
            .ReturnsAsync((disputes, 1));

        var result = await _service.GetDisputesAsync(DisputeStatus.OPEN, 1, 20);

        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Single(result.Data);
        Assert.Equal("Complaint", result.Data[0].ComplaintText);
    }

    [Fact]
    public async Task ResolveDisputeAsync_DisputeNotFound_ThrowsNotFoundException()
    {
        var disputeId = Guid.NewGuid();
        _disputeRepoMock.Setup(r => r.GetByIdAsync(disputeId)).ReturnsAsync((Dispute)null!);
        var request = new ResolveDisputeRequest { Status = DisputeStatus.RESOLVED, ResolutionNotes = "Notes" };

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.ResolveDisputeAsync(disputeId, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task ResolveDisputeAsync_DisputeAlreadyResolved_ThrowsBusinessRuleException()
    {
        var disputeId = Guid.NewGuid();
        var dispute = new Dispute { Id = disputeId, Status = DisputeStatus.RESOLVED };
        _disputeRepoMock.Setup(r => r.GetByIdAsync(disputeId)).ReturnsAsync(dispute);
        var request = new ResolveDisputeRequest { Status = DisputeStatus.RESOLVED, ResolutionNotes = "Notes" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _service.ResolveDisputeAsync(disputeId, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task ResolveDisputeAsync_InvalidStatusRequest_ThrowsValidationException()
    {
        var disputeId = Guid.NewGuid();
        var dispute = new Dispute { Id = disputeId, Status = DisputeStatus.OPEN };
        _disputeRepoMock.Setup(r => r.GetByIdAsync(disputeId)).ReturnsAsync(dispute);
        var request = new ResolveDisputeRequest { Status = DisputeStatus.OPEN, ResolutionNotes = "Notes" };

        await Assert.ThrowsAsync<ValidationException>(() => 
            _service.ResolveDisputeAsync(disputeId, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task ResolveDisputeAsync_ValidResolution_UpdatesStatusAndNotifies()
    {
        var disputeId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var dispute = new Dispute 
        { 
            Id = disputeId, 
            Status = DisputeStatus.OPEN,
            ShipmentId = Guid.NewGuid(),
            Shipment = new Shipment { CustomerId = customerId, OrderId = "TRK-001" }
        };
        _disputeRepoMock.Setup(r => r.GetByIdAsync(disputeId)).ReturnsAsync(dispute);
        var adminUserId = Guid.NewGuid();
        var request = new ResolveDisputeRequest { Status = DisputeStatus.RESOLVED, ResolutionNotes = "Refund given" };

        var result = await _service.ResolveDisputeAsync(disputeId, request, adminUserId);

        Assert.NotNull(result);
        Assert.Equal(DisputeStatus.RESOLVED, result.Status);
        Assert.Equal(DisputeStatus.RESOLVED, dispute.Status);
        Assert.Equal(adminUserId, dispute.ResolvedBy);
        Assert.Equal("Refund given", dispute.ResolutionNotes);
        Assert.True(dispute.ResolvedAt > DateTime.UtcNow.AddSeconds(-5));

        _disputeRepoMock.Verify(r => r.UpdateAsync(dispute), Times.Once);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(customerId, dispute.ShipmentId, "Dispute Resolved", It.IsAny<string>()), Times.Once);
    }
}
