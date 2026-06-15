using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class TrackingServiceTests
{
    private readonly Mock<ITrackingRepository> _trackingRepoMock;
    private readonly Mock<IShipmentRepository> _shipmentRepoMock;
    private readonly Mock<IDriverRepository> _driverRepoMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly TrackingService _service;

    public TrackingServiceTests()
    {
        _trackingRepoMock = new Mock<ITrackingRepository>();
        _shipmentRepoMock = new Mock<IShipmentRepository>();
        _driverRepoMock = new Mock<IDriverRepository>();
        _notificationServiceMock = new Mock<INotificationService>();

        _service = new TrackingService(
            _trackingRepoMock.Object,
            _shipmentRepoMock.Object,
            _driverRepoMock.Object,
            _notificationServiceMock.Object
        );
    }

    [Fact]
    public async Task RecordLocationAsync_DriverNotFound_ThrowsNotFoundException()
    {
        var driverUserId = Guid.NewGuid();
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync((Driver)null!);
        var request = new TrackingLocationRequest { ShipmentId = Guid.NewGuid(), Latitude = 12.34m, Longitude = 56.78m };

        await Assert.ThrowsAsync<NotFoundException>(() => _service.RecordLocationAsync(request, driverUserId));
    }

    [Fact]
    public async Task RecordLocationAsync_ShipmentNotFound_ThrowsNotFoundException()
    {
        var driverUserId = Guid.NewGuid();
        var driver = new Driver { Id = Guid.NewGuid(), UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);
        
        var shipmentId = Guid.NewGuid();
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync((Shipment)null!);
        var request = new TrackingLocationRequest { ShipmentId = shipmentId, Latitude = 12.34m, Longitude = 56.78m };

        await Assert.ThrowsAsync<NotFoundException>(() => _service.RecordLocationAsync(request, driverUserId));
    }

    [Fact]
    public async Task RecordLocationAsync_NotAssignedDriver_ThrowsForbiddenException()
    {
        var driverUserId = Guid.NewGuid();
        var driver = new Driver { Id = Guid.NewGuid(), UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, DriverId = Guid.NewGuid() }; // Assigned to other driver
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        var request = new TrackingLocationRequest { ShipmentId = shipmentId, Latitude = 12.34m, Longitude = 56.78m };

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.RecordLocationAsync(request, driverUserId));
    }

    [Fact]
    public async Task RecordLocationAsync_ShipmentNotInTransit_ThrowsBusinessRuleException()
    {
        var driverUserId = Guid.NewGuid();
        var driver = new Driver { Id = Guid.NewGuid(), UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, DriverId = driver.Id, Status = ShipmentStatus.ASSIGNED }; // Not IN_TRANSIT
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        var request = new TrackingLocationRequest { ShipmentId = shipmentId, Latitude = 12.34m, Longitude = 56.78m };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RecordLocationAsync(request, driverUserId));
    }

    [Fact]
    public async Task RecordLocationAsync_ValidInputs_SavesPingUpdatesDriverAndBroadcasts()
    {
        var driverUserId = Guid.NewGuid();
        var driver = new Driver { Id = Guid.NewGuid(), UserId = driverUserId, CurrentLat = 0, CurrentLng = 0 };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, DriverId = driver.Id, Status = ShipmentStatus.IN_TRANSIT };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        
        var request = new TrackingLocationRequest { ShipmentId = shipmentId, Latitude = 12.3456m, Longitude = 56.7890m };

        await _service.RecordLocationAsync(request, driverUserId);

        Assert.Equal(12.3456m, driver.CurrentLat);
        Assert.Equal(56.7890m, driver.CurrentLng);
        Assert.True(driver.LastPingAt > DateTime.UtcNow.AddSeconds(-5));

        _trackingRepoMock.Verify(r => r.AddAsync(It.Is<Tracking>(t => 
            t.ShipmentId == shipmentId && 
            t.DriverId == driver.Id && 
            t.Latitude == 12.3456m && 
            t.Longitude == 56.7890m
        )), Times.Once);

        _driverRepoMock.Verify(r => r.UpdateAsync(driver), Times.Once);
        _notificationServiceMock.Verify(n => n.BroadcastDriverLocationAsync(shipmentId, 12.3456m, 56.7890m), Times.Once);
    }

    [Fact]
    public async Task GetLiveLocationAsync_ShipmentNotFound_ThrowsNotFoundException()
    {
        var shipmentId = Guid.NewGuid();
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync((Shipment)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.GetLiveLocationAsync(shipmentId, Guid.NewGuid(), "CUSTOMER"));
    }

    [Fact]
    public async Task GetLiveLocationAsync_NotAuthorizedCustomer_ThrowsForbiddenException()
    {
        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, CustomerId = Guid.NewGuid() };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        // Try to access with different customer ID
        await Assert.ThrowsAsync<ForbiddenException>(() => 
            _service.GetLiveLocationAsync(shipmentId, Guid.NewGuid(), "CUSTOMER"));
    }

    [Fact]
    public async Task GetLiveLocationAsync_AuthorizedCustomer_ReturnsLatestLocation()
    {
        var shipmentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, CustomerId = customerId };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var tracking = new Tracking { ShipmentId = shipmentId, Latitude = 1.0m, Longitude = 2.0m, RecordedAt = DateTime.UtcNow };
        _trackingRepoMock.Setup(r => r.GetLatestPingAsync(shipmentId)).ReturnsAsync(tracking);

        var result = await _service.GetLiveLocationAsync(shipmentId, customerId, "CUSTOMER");

        Assert.NotNull(result);
        Assert.Equal(shipmentId, result.ShipmentId);
        Assert.NotNull(result.DriverLocation);
        Assert.Equal(1.0m, result.DriverLocation.Latitude);
        Assert.Equal(2.0m, result.DriverLocation.Longitude);
    }

    [Fact]
    public async Task GetLiveLocationAsync_Admin_ReturnsLatestLocation()
    {
        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, CustomerId = Guid.NewGuid() };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var tracking = new Tracking { ShipmentId = shipmentId, Latitude = 1.0m, Longitude = 2.0m, RecordedAt = DateTime.UtcNow };
        _trackingRepoMock.Setup(r => r.GetLatestPingAsync(shipmentId)).ReturnsAsync(tracking);

        var result = await _service.GetLiveLocationAsync(shipmentId, Guid.NewGuid(), "ADMIN");

        Assert.NotNull(result);
        Assert.NotNull(result.DriverLocation);
    }

    [Fact]
    public async Task GetHistoryAsync_NotAdmin_ThrowsForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => 
            _service.GetHistoryAsync(Guid.NewGuid(), "CUSTOMER"));
    }

    [Fact]
    public async Task GetHistoryAsync_ShipmentNotFound_ThrowsNotFoundException()
    {
        var shipmentId = Guid.NewGuid();
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync((Shipment)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.GetHistoryAsync(shipmentId, "ADMIN"));
    }

    [Fact]
    public async Task GetHistoryAsync_ValidAdmin_ReturnsHistory()
    {
        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var history = new List<Tracking>
        {
            new Tracking { Latitude = 1.0m, Longitude = 2.0m, RecordedAt = DateTime.UtcNow }
        };
        _trackingRepoMock.Setup(r => r.GetHistoryAsync(shipmentId)).ReturnsAsync(history);

        var result = await _service.GetHistoryAsync(shipmentId, "ADMIN");

        Assert.Single(result);
        Assert.Equal(1.0m, result[0].Latitude);
        Assert.Equal(2.0m, result[0].Longitude);
    }
}
