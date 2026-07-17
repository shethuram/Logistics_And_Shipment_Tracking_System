using System;
using System.Collections.Generic;
using System.Text;
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

public class AdminServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly Mock<IShipmentRepository> _shipmentRepoMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _shipmentRepoMock = new Mock<IShipmentRepository>();
        _notificationServiceMock = new Mock<INotificationService>();

        _service = new AdminService(
            _db,
            _shipmentRepoMock.Object,
            _notificationServiceMock.Object,
            new Mock<Microsoft.Extensions.Logging.ILogger<AdminService>>().Object
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task ReassignShipmentAsync_ShipmentNotFound_ThrowsNotFoundException()
    {
        var shipmentId = Guid.NewGuid();
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync((Shipment)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ReassignShipmentAsync(shipmentId));
    }

    [Fact]
    public async Task ReassignShipmentAsync_ShipmentExists_ResetsStatusAndNotifies()
    {

        var shipmentId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driverUserId = Guid.NewGuid();

        var driver = new Driver { Id = driverId, UserId = driverUserId, LicenseNumber = "L1" };
        var user = new User { Id = driverUserId, Auth0Id = "auth0|reassign_driver", FullName = "Driver User", Email = "d@example.com", Phone = "123", Role = UserRole.DRIVER };
        _db.Users.Add(user);
        _db.Drivers.Add(driver);
        _db.SaveChanges();

        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            OrderId = "TRK-001",
            DriverId = driverId, 
            VehicleId = Guid.NewGuid(),
            Status = ShipmentStatus.IN_TRANSIT,
            PackageType = PackageType.SMALL_PARCEL,
            WeightKg = 2.0m
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var result = await _service.ReassignShipmentAsync(shipmentId);

        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.OPEN, result.Status);
        Assert.Null(result.DriverId);

        Assert.Null(shipment.DriverId);
        Assert.Null(shipment.VehicleId);
        Assert.Equal(ShipmentStatus.OPEN, shipment.Status);

        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(
            driverUserId, shipmentId, "Shipment Unassigned", It.IsAny<string>()), Times.Once);
        _notificationServiceMock.Verify(n => n.BroadcastShipmentUpdateAsync(shipmentId, "OPEN", It.IsAny<object>()), Times.Once);

        _notificationServiceMock.Verify(n => n.BroadcastNewJobAlertAsync("TWO_WHEELER", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsCorrectMetrics()
    {

        var customerId = Guid.NewGuid();
        var driver1UserId = Guid.NewGuid();
        var driver2UserId = Guid.NewGuid();

        var user = new User { Id = customerId, Auth0Id = "auth0|metric_cust", FullName = "Customer", Email = "c@example.com", Phone = "987", Role = UserRole.CUSTOMER };
        var driverUser1 = new User { Id = driver1UserId, Auth0Id = "auth0|metric_driver1", FullName = "Driver 1", Email = "d1@example.com", Phone = "111", Role = UserRole.DRIVER };
        var driverUser2 = new User { Id = driver2UserId, Auth0Id = "auth0|metric_driver2", FullName = "Driver 2", Email = "d2@example.com", Phone = "222", Role = UserRole.DRIVER };

        _db.Users.AddRange(user, driverUser1, driverUser2);

        _db.Shipments.AddRange(
            new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, OrderId = "TRK-1", Status = ShipmentStatus.DELIVERED, CreatedAt = DateTime.UtcNow.AddMinutes(-60), StatusUpdatedAt = DateTime.UtcNow, StatusChangedBy = customerId },
            new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, OrderId = "TRK-2", Status = ShipmentStatus.DELIVERED, CreatedAt = DateTime.UtcNow.AddMinutes(-30), StatusUpdatedAt = DateTime.UtcNow, StatusChangedBy = customerId, CashCollected = true },
            new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, OrderId = "TRK-3", Status = ShipmentStatus.CANCELLED, StatusChangedBy = customerId },
            new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, OrderId = "TRK-4", Status = ShipmentStatus.PICKUP_FAILED, StatusChangedBy = customerId },
            new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, OrderId = "TRK-5", Status = ShipmentStatus.STALE, StatusChangedBy = customerId }
        );

        _db.Drivers.AddRange(
            new Driver { Id = Guid.NewGuid(), UserId = driver1UserId, LicenseNumber = "D1", OperationalStatus = OperationalStatus.ONLINE, CancelCount = 4 },
            new Driver { Id = Guid.NewGuid(), UserId = driver2UserId, LicenseNumber = "D2", OperationalStatus = OperationalStatus.OFFLINE }
        );

        _db.SaveChanges();

        var result = await _service.GetMetricsAsync();

        Assert.Equal(5, result.TotalShipments);
        Assert.Equal(2, result.Delivered);
        Assert.Equal(1, result.Pending);
        Assert.Equal(1, result.Cancelled);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.StaleShipments);
        Assert.Equal(1, result.CodPending);
        Assert.Equal(1, result.DriversOnline);
        Assert.Equal(1, result.DriversWithHighCancelCount);
        Assert.Equal(45, result.AvgDeliveryTimeMinutes);
    }

    [Fact]
    public async Task ExportShipmentsCsvAsync_ReturnsValidCsvFile()
    {

        var customerId = Guid.NewGuid();
        var user = new User { Id = customerId, FullName = "Arjun Kumar", Email = "arjun@example.com", Phone = "9876543210" };
        _db.Users.Add(user);

        var shipment = new Shipment 
        { 
            Id = Guid.NewGuid(), 
            CustomerId = customerId, 
            OrderId = "TRK-00001", 
            Status = ShipmentStatus.DELIVERED,
            PickupAddress = "Salem Junction, Salem",
            DropAddress = "Big Bazaar, Salem",
            ReceiverName = "Priya",
            ReceiverPhone = "998877",
            PackageType = PackageType.SMALL_PARCEL,
            WeightKg = 3.5m,
            CreatedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc)
        };
        _db.Shipments.Add(shipment);
        _db.SaveChanges();

        var csvBytes = await _service.ExportShipmentsCsvAsync(ShipmentStatus.DELIVERED, null, null);
        var csvString = Encoding.UTF8.GetString(csvBytes);

        Assert.Contains("OrderId,CustomerName,CustomerEmail", csvString);
        Assert.Contains("TRK-00001", csvString);
        Assert.Contains("Arjun Kumar", csvString);
        Assert.Contains("Salem Junction", csvString);
    }
}
