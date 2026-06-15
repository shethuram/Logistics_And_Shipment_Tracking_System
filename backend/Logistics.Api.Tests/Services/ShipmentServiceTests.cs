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

public class ShipmentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly Mock<IShipmentRepository> _shipmentRepoMock;
    private readonly Mock<IDriverRepository> _driverRepoMock;
    private readonly Mock<IVehicleRepository> _vehicleRepoMock;
    private readonly Mock<IGeoService> _geoServiceMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILlmService> _llmServiceMock;
    private readonly ShipmentService _service;

    public ShipmentServiceTests()
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
        _driverRepoMock = new Mock<IDriverRepository>();
        _vehicleRepoMock = new Mock<IVehicleRepository>();
        _geoServiceMock = new Mock<IGeoService>();
        _otpServiceMock = new Mock<IOtpService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _llmServiceMock = new Mock<ILlmService>();

        _service = new ShipmentService(
            _shipmentRepoMock.Object,
            _driverRepoMock.Object,
            _vehicleRepoMock.Object,
            _geoServiceMock.Object,
            _otpServiceMock.Object,
            _notificationServiceMock.Object,
            _db,
            _llmServiceMock.Object
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateAsync_InvalidPackageType_ThrowsValidationException()
    {
        var request = new CreateShipmentRequest { PackageType = "INVALID", PaymentMethod = "COD" };
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_InvalidPaymentMethod_ThrowsValidationException()
    {
        var request = new CreateShipmentRequest { PackageType = "SMALL_PARCEL", PaymentMethod = "INVALID" };
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_CodPayment_CreatesOpenShipmentAndBroadcastsJob()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateShipmentRequest 
        { 
            PackageType = "SMALL_PARCEL", 
            PaymentMethod = "COD",
            PickupAddress = "Salem",
            DropAddress = "Erode",
            WeightKg = 2.5m,
            SpecialNotes = "Careful"
        };

        _llmServiceMock.Setup(l => l.ParseDeliveryNoteAsync("Careful"))
            .ReturnsAsync((false, RiskSeverity.NONE, null, null, "Careful Instruction"));

        _otpServiceMock.Setup(o => o.GenerateOtp()).Returns("1234");
        _otpServiceMock.Setup(o => o.GenerateDeterministicOtp(It.IsAny<Guid>(), "receiver")).Returns("5678");

        // Act
        var result = await _service.CreateAsync(request, customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OPEN", result.Status);
        Assert.Null(result.PaymentUrl);
        Assert.Equal("1234", result.SenderOtp);

        _shipmentRepoMock.Verify(r => r.AddAsync(It.Is<Shipment>(s => 
            s.Status == ShipmentStatus.OPEN && 
            s.CustomerId == customerId && 
            s.WeightKg == 2.5m &&
            s.DriverInstruction == "Careful Instruction"
        )), Times.Once);

        // Notify customer + broadcast to eligible drivers (TWO_WHEELER, THREE_WHEELER)
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(customerId, It.IsAny<Guid>(), "Shipment Created", It.IsAny<string>()), Times.Once);
        _notificationServiceMock.Verify(n => n.BroadcastNewJobAlertAsync("TWO_WHEELER", It.IsAny<object>()), Times.Once);
        _notificationServiceMock.Verify(n => n.BroadcastNewJobAlertAsync("THREE_WHEELER", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OnlinePayment_CreatesPendingPaymentShipment()
    {
        // Arrange
        var request = new CreateShipmentRequest { PackageType = "SMALL_PARCEL", PaymentMethod = "ONLINE" };
        _otpServiceMock.Setup(o => o.GenerateOtp()).Returns("1234");

        // Act
        var result = await _service.CreateAsync(request, Guid.NewGuid());

        // Assert
        Assert.Equal("PENDING_PAYMENT", result.Status);
        Assert.NotNull(result.PaymentUrl);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Shipment)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id, Guid.NewGuid(), "CUSTOMER"));
    }

    [Fact]
    public async Task GetByIdAsync_NotOwnerCustomer_ThrowsForbiddenException()
    {
        var id = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = Guid.NewGuid() };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.GetByIdAsync(id, Guid.NewGuid(), "CUSTOMER"));
    }

    [Fact]
    public async Task GetByIdAsync_AuthorizedOwnerCustomer_ReturnsShipment()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, OrderId = "TRK-01" };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var result = await _service.GetByIdAsync(id, customerId, "CUSTOMER");

        Assert.NotNull(result);
        Assert.Equal("TRK-01", result.OrderId);
    }

    [Fact]
    public async Task UpdateAsync_InvalidStatus_ThrowsBusinessRuleException()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.ASSIGNED }; // Only PENDING_PAYMENT or OPEN allowed
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var request = new UpdateShipmentRequest { SpecialNotes = "New Note" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(id, request, customerId));
    }

    [Fact]
    public async Task UpdateAsync_ValidNotes_UpdatesAndCallsLlm()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        _llmServiceMock.Setup(l => l.ParseDeliveryNoteAsync("New Notes"))
            .ReturnsAsync((true, RiskSeverity.HIGH, "unattended", null, "New Instr"));

        var request = new UpdateShipmentRequest { SpecialNotes = "New Notes" };

        await _service.UpdateAsync(id, request, customerId);

        Assert.Equal("New Notes", shipment.SpecialNotes);
        Assert.True(shipment.RiskFlag);
        Assert.Equal("New Instr", shipment.DriverInstruction);
        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_AssignedShipment_ThrowsBusinessRuleException()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.ASSIGNED };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CancelAsync(id, customerId));
    }

    [Fact]
    public async Task CancelAsync_OpenShipment_CancelsAndTriggersRefund()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var result = await _service.CancelAsync(id, customerId);

        Assert.Equal("CANCELLED", result.Status);
        Assert.True(result.RefundInitiated);
        Assert.Equal(ShipmentStatus.CANCELLED, shipment.Status);
    }

    [Fact]
    public async Task ClaimAsync_DriverAlreadyHasActiveJob_ThrowsValidationException()
    {
        var driverUserId = Guid.NewGuid();
        var driver = new Driver { Id = Guid.NewGuid(), UserId = driverUserId, ApprovalStatus = ApprovalStatus.APPROVED, ActiveVehicleId = Guid.NewGuid() };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(driver.ActiveVehicleId.Value)).ReturnsAsync(new Vehicle());

        // Driver already has active job
        _shipmentRepoMock.Setup(r => r.GetActiveShipmentForDriverAsync(driver.Id)).ReturnsAsync(new Shipment());

        await Assert.ThrowsAsync<ValidationException>(() => _service.ClaimAsync(Guid.NewGuid(), driverUserId));
    }

    [Fact]
    public async Task ClaimAsync_ValidClaim_AssignsDriverAndVehicle()
    {
        // Arrange
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId, ApprovalStatus = ApprovalStatus.APPROVED, ActiveVehicleId = vehicleId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);
        
        var vehicle = new Vehicle { Id = vehicleId, VehicleType = VehicleType.TWO_WHEELER };
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        _shipmentRepoMock.Setup(r => r.GetActiveShipmentForDriverAsync(driverId)).ReturnsAsync((Shipment)null!);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, OrderId = "TRK-01", Status = ShipmentStatus.OPEN, PackageType = PackageType.DOCUMENT, WeightKg = 0.5m };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        _shipmentRepoMock.Setup(r => r.GetByIdForUpdateAsync(shipmentId)).ReturnsAsync(shipment);

        // Act
        var result = await _service.ClaimAsync(shipmentId, driverUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ASSIGNED", result.Status);
        Assert.Equal(driverId, shipment.DriverId);
        Assert.Equal(vehicleId, shipment.VehicleId);
        Assert.Equal(ShipmentStatus.ASSIGNED, shipment.Status);

        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(shipment.CustomerId, shipmentId, "Driver Assigned", It.IsAny<string>()), Times.Once);
        _notificationServiceMock.Verify(n => n.BroadcastShipmentUpdateAsync(shipmentId, "ASSIGNED", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CancelClaimAsync_ValidCancel_ResetsToOpenAndIncrementsCancelCount()
    {
        // Arrange
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId, CancelCount = 1 };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, OrderId = "TRK-01", DriverId = driverId, Status = ShipmentStatus.ASSIGNED };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var request = new CancelClaimRequest { Reason = "Breakdown" };

        // Act
        var result = await _service.CancelClaimAsync(shipmentId, driverUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OPEN", result.Status);
        Assert.Equal(2, result.DriverCancelCount);

        Assert.Null(shipment.DriverId);
        Assert.Equal(ShipmentStatus.OPEN, shipment.Status);
        Assert.Equal(2, driver.CancelCount);

        _driverRepoMock.Verify(r => r.UpdateAsync(driver), Times.Once);
        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(shipment.CustomerId, shipmentId, "Driver Cancelled Claim", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmPickupAsync_WrongOtp_IncrementsAttemptsAndThrowsValidationException()
    {
        // Arrange
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.ASSIGNED,
            SenderOtpHash = "hash",
            SenderOtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
            SenderOtpAttempts = 0
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        _otpServiceMock.Setup(o => o.VerifyOtp("1111", "hash")).Returns(false);

        var request = new ConfirmPickupRequest { Otp = "1111" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmPickupAsync(shipmentId, driverUserId, request));
        Assert.Equal(1, shipment.SenderOtpAttempts);
        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
    }

    [Fact]
    public async Task ConfirmPickupAsync_CorrectOtp_TransitionsToInTransit()
    {
        // Arrange
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.ASSIGNED,
            SenderOtpHash = "hash",
            SenderOtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
            SenderOtpAttempts = 0
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        _otpServiceMock.Setup(o => o.VerifyOtp("1234", "hash")).Returns(true);

        var request = new ConfirmPickupRequest { Otp = "1234" };

        // Act
        var result = await _service.ConfirmPickupAsync(shipmentId, driverUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IN_TRANSIT", result.Status);
        Assert.Equal(ShipmentStatus.IN_TRANSIT, shipment.Status);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(shipment.CustomerId, shipmentId, "Package Picked Up", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_GeofenceBreach_ThrowsValidationException()
    {
        // Arrange
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.IN_TRANSIT,
            DropLat = 11.0m,
            DropLng = 78.0m
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        // Driver coordinates too far (distance = 0.5km > 0.2km)
        _geoServiceMock.Setup(g => g.CalculateDistance(11.005m, 78.0m, 11.0m, 78.0m)).Returns(0.5);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.005m, DriverLng = 78.0m };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmDeliveryAsync(shipmentId, driverUserId, request));
        Assert.Equal("Driver is not within 200m of the delivery location.", ex.Message);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_ValidOtpAndGeofence_TransitionsToDelivered()
    {
        // Arrange
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.IN_TRANSIT,
            DropLat = 11.0m,
            DropLng = 78.0m,
            ReceiverOtpHash = "hash",
            ReceiverOtpExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        _geoServiceMock.Setup(g => g.CalculateDistance(11.0001m, 78.0m, 11.0m, 78.0m)).Returns(0.01);
        _otpServiceMock.Setup(o => o.VerifyOtp("5678", "hash")).Returns(true);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.0001m, DriverLng = 78.0m };

        // Act
        var result = await _service.ConfirmDeliveryAsync(shipmentId, driverUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DELIVERED", result.Status);
        Assert.Equal(ShipmentStatus.DELIVERED, shipment.Status);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(shipment.CustomerId, shipmentId, "Package Delivered", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicTrackingAsync_InvalidParams_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetPublicTrackingAsync("", "", ""));
    }

    [Fact]
    public async Task GetPublicTrackingAsync_ValidParams_ReturnsTimeline()
    {
        // Arrange
        var shipmentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customer = new User { Id = customerId, Phone = "9876543210" };
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            OrderId = "TRK-01", 
            CustomerId = customerId, 
            Customer = customer,
            CreatedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            Status = ShipmentStatus.OPEN,
            PickupAddress = "Salem",
            DropAddress = "Erode"
        };

        _shipmentRepoMock.Setup(r => r.GetByPublicTrackParamsAsync("TRK-01", "9876543210")).ReturnsAsync(shipment);
        _otpServiceMock.Setup(o => o.GenerateDeterministicOtp(shipmentId, "receiver")).Returns("5678");

        // Act
        var result = await _service.GetPublicTrackingAsync("TRK-01", "9876543210", "2026-06-15");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TRK-01", result.OrderId);
        Assert.Equal("5678", result.ReceiverOtp);
        Assert.Equal("OPEN", result.Status);
        Assert.True(result.Timeline.Count > 0);
    }

    [Fact]
    public async Task GetByIdAsync_AdminRole_ReturnsShipment()
    {
        var id = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = Guid.NewGuid(), OrderId = "TRK-ADMIN" };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var result = await _service.GetByIdAsync(id, Guid.NewGuid(), "ADMIN");

        Assert.NotNull(result);
        Assert.Equal("TRK-ADMIN", result.OrderId);
    }

    [Fact]
    public async Task GetByIdAsync_DriverAssigned_ReturnsShipment()
    {
        var id = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driverUserId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = Guid.NewGuid(), DriverId = driverId, OrderId = "TRK-DRIVER" };
        
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var result = await _service.GetByIdAsync(id, driverUserId, "DRIVER");

        Assert.NotNull(result);
        Assert.Equal("TRK-DRIVER", result.OrderId);
    }

    [Fact]
    public async Task UpdateAsync_InvalidPreferredWindow_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var request = new UpdateShipmentRequest { PreferredWindow = "INVALID" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(id, request, customerId));
    }

    [Fact]
    public async Task UpdateAsync_NullSpecialNotes_ResetsLlmInstruction()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN, SpecialNotes = "Note", DriverInstruction = "Instr" };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var request = new UpdateShipmentRequest { SpecialNotes = "" };

        await _service.UpdateAsync(id, request, customerId);

        Assert.Empty(shipment.SpecialNotes);
        Assert.False(shipment.RiskFlag);
        Assert.Null(shipment.DriverInstruction);
    }

    [Fact]
    public async Task CancelAsync_CodOpen_ReturnsCancelResponseWithNoRefund()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN };
        
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(shipment);

        var result = await _service.CancelAsync(id, customerId);

        Assert.Equal("CANCELLED", result.Status);
        Assert.True(result.RefundInitiated);
    }

    [Fact]
    public async Task GetShipmentsAsync_NotAuthorized_ThrowsForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => 
            _service.GetShipmentsAsync(Guid.NewGuid(), "DRIVER", null, null, null, null, 1, 20));
    }

    [Fact]
    public async Task GetShipmentsAsync_InvalidStatusFilter_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => 
            _service.GetShipmentsAsync(Guid.NewGuid(), "CUSTOMER", null, "INVALID", null, null, 1, 20));
    }

    [Fact]
    public async Task GetShipmentsAsync_ValidCustomer_ReturnsCustomerShipments()
    {
        var customerId = Guid.NewGuid();
        var shipments = new List<Shipment> { new Shipment { Id = Guid.NewGuid(), CustomerId = customerId, OrderId = "TRK-CUST" } };
        _shipmentRepoMock.Setup(r => r.GetShipmentsAsync(customerId, null, null, null, null, 1, 10))
            .ReturnsAsync((shipments, 1));

        var result = await _service.GetShipmentsAsync(customerId, "CUSTOMER", null, null, null, null, 1, 10);

        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetAvailableShipmentsAsync_ValidDriver_ReturnsEligibleAvailableShipments()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        
        var driver = new Driver { Id = driverId, UserId = driverUserId, ApprovalStatus = ApprovalStatus.APPROVED, ActiveVehicleId = vehicleId, CurrentLat = 11.0m, CurrentLng = 78.0m };
        var vehicle = new Vehicle { Id = vehicleId, VehicleType = VehicleType.TWO_WHEELER };
        
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        var openShipments = new List<Shipment>
        {
            new Shipment { Id = Guid.NewGuid(), Status = ShipmentStatus.OPEN, PackageType = PackageType.DOCUMENT, WeightKg = 0.2m, PickupLat = 11.01m, PickupLng = 78.0m, Customer = new User() }
        };
        _shipmentRepoMock.Setup(r => r.GetOpenShipmentsAsync()).ReturnsAsync(openShipments);
        _geoServiceMock.Setup(g => g.CalculateDistance(11.0m, 78.0m, 11.01m, 78.0m)).Returns(1.1);

        var result = await _service.GetAvailableShipmentsAsync(driverUserId);

        Assert.Single(result);
        Assert.Equal(1.1, result[0].DistanceToPickupKm);
    }

    [Fact]
    public async Task CancelClaimAsync_HighCancelCount_AlertsAdmin()
    {
        // Arrange
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId, CancelCount = 2, User = new User { FullName = "Driver" } };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, DriverId = driverId, Status = ShipmentStatus.ASSIGNED, Customer = new User() };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var request = new CancelClaimRequest { Reason = "Reason" };

        // Act
        var result = await _service.CancelClaimAsync(shipmentId, driverUserId, request);

        // Assert
        Assert.Equal(3, result.DriverCancelCount);
        _notificationServiceMock.Verify(n => n.BroadcastAdminAlertAsync("HIGH_CANCEL_COUNT", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmPickupAsync_OtpExpired_ThrowsValidationException()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.ASSIGNED,
            SenderOtpExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var request = new ConfirmPickupRequest { Otp = "1234" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmPickupAsync(shipmentId, driverUserId, request));
    }

    [Fact]
    public async Task ConfirmPickupAsync_TooManyAttempts_ThrowsTooManyRequestsException()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.ASSIGNED,
            SenderOtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
            SenderOtpAttempts = 3
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var request = new ConfirmPickupRequest { Otp = "1234" };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmPickupAsync(shipmentId, driverUserId, request));
    }

    [Fact]
    public async Task ConfirmPickupAsync_ThirdFailedAttempt_AlertsAdmin()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.ASSIGNED,
            SenderOtpHash = "hash",
            SenderOtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
            SenderOtpAttempts = 2
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        _otpServiceMock.Setup(o => o.VerifyOtp("1111", "hash")).Returns(false);

        var request = new ConfirmPickupRequest { Otp = "1111" };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmPickupAsync(shipmentId, driverUserId, request));
        Assert.Equal(3, shipment.SenderOtpAttempts);
        _notificationServiceMock.Verify(n => n.BroadcastAdminAlertAsync("OTP_BLOCKED", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_OtpExpired_ThrowsValidationException()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.IN_TRANSIT,
            DropLat = 11.0m,
            DropLng = 78.0m,
            ReceiverOtpExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        _geoServiceMock.Setup(g => g.CalculateDistance(11.0m, 78.0m, 11.0m, 78.0m)).Returns(0.0);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.0m, DriverLng = 78.0m };

        await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmDeliveryAsync(shipmentId, driverUserId, request));
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_TooManyAttempts_ThrowsTooManyRequestsException()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.IN_TRANSIT,
            DropLat = 11.0m,
            DropLng = 78.0m,
            ReceiverOtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
            ReceiverOtpAttempts = 3
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        _geoServiceMock.Setup(g => g.CalculateDistance(11.0m, 78.0m, 11.0m, 78.0m)).Returns(0.0);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.0m, DriverLng = 78.0m };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmDeliveryAsync(shipmentId, driverUserId, request));
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_ThirdFailedAttempt_AlertsAdmin()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment 
        { 
            Id = shipmentId, 
            DriverId = driverId, 
            Status = ShipmentStatus.IN_TRANSIT,
            DropLat = 11.0m,
            DropLng = 78.0m,
            ReceiverOtpHash = "hash",
            ReceiverOtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
            ReceiverOtpAttempts = 2
        };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);
        _geoServiceMock.Setup(g => g.CalculateDistance(11.0m, 78.0m, 11.0m, 78.0m)).Returns(0.0);
        _otpServiceMock.Setup(o => o.VerifyOtp("1111", "hash")).Returns(false);

        var request = new ConfirmDeliveryRequest { Otp = "1111", DriverLat = 11.0m, DriverLng = 78.0m };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmDeliveryAsync(shipmentId, driverUserId, request));
        Assert.Equal(3, shipment.ReceiverOtpAttempts);
        _notificationServiceMock.Verify(n => n.BroadcastAdminAlertAsync("OTP_BLOCKED", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmCashCollectedAsync_ValidMark_SetsCashCollected()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, DriverId = driverId, Status = ShipmentStatus.DELIVERED, CashCollected = false };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var result = await _service.ConfirmCashCollectedAsync(shipmentId, driverUserId);

        Assert.NotNull(result);
        Assert.True(result.CashCollected);
        Assert.True(shipment.CashCollected);
        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
    }

    [Fact]
    public async Task MarkPickupFailedAsync_ValidMark_TransitionsToPickupFailed()
    {
        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, DriverId = driverId, Status = ShipmentStatus.ASSIGNED, CustomerId = Guid.NewGuid(), OrderId = "TRK-01" };
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(shipmentId)).ReturnsAsync(shipment);

        var request = new PickupFailedRequest { Reason = "No show" };

        var result = await _service.MarkPickupFailedAsync(shipmentId, driverUserId, request);

        Assert.NotNull(result);
        Assert.Equal("PICKUP_FAILED", result.Status);
        Assert.Equal(ShipmentStatus.PICKUP_FAILED, shipment.Status);

        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(shipment.CustomerId, shipmentId, "Pickup Failed", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicTrackingAsync_InvalidDate_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.GetPublicTrackingAsync("TRK-01", "9876543210", "INVALID-DATE"));
    }

    [Fact]
    public async Task GetPublicTrackingAsync_WrongDateMatch_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        var customer = new User { Id = customerId, Phone = "9876543210" };
        var shipment = new Shipment 
        { 
            Id = Guid.NewGuid(), 
            OrderId = "TRK-01", 
            CustomerId = customerId, 
            Customer = customer,
            CreatedAt = new DateTime(2026, 6, 15)
        };

        _shipmentRepoMock.Setup(r => r.GetByPublicTrackParamsAsync("TRK-01", "9876543210")).ReturnsAsync(shipment);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.GetPublicTrackingAsync("TRK-01", "9876543210", "2026-06-16"));
    }
}
