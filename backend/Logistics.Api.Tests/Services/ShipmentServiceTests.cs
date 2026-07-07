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
using Microsoft.Extensions.Logging;

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
    private readonly Mock<ILogger<ShipmentService>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
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
        _loggerMock = new Mock<ILogger<ShipmentService>>();
        _emailServiceMock = new Mock<IEmailService>();
        _emailTemplateServiceMock = new Mock<IEmailTemplateService>();

        _emailTemplateServiceMock
            .Setup(t => t.GenerateDriverAssignedNotification(
                It.IsAny<Shipment>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("Subject", "Body"));

        _service = new ShipmentService(
            _shipmentRepoMock.Object,
            _driverRepoMock.Object,
            _vehicleRepoMock.Object,
            _geoServiceMock.Object,
            _otpServiceMock.Object,
            _notificationServiceMock.Object,
            _db,
            _llmServiceMock.Object,
            _loggerMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateAsync_CodPayment_CreatesOpenShipmentAndBroadcastsJob()
    {

        var customerId = Guid.NewGuid();
        var request = new CreateShipmentRequest 
        { 
            PackageType = PackageType.SMALL_PARCEL, 
            PaymentMethod = PaymentMethod.COD,
            PickupAddress = "Salem",
            DropAddress = "Erode",
            WeightKg = 2.5m,
            SpecialNotes = "Careful"
        };

        _llmServiceMock.Setup(l => l.ParseDeliveryNoteAsync("Careful"))
            .ReturnsAsync((false, RiskSeverity.NONE, null, null, "Careful Instruction"));

        var result = await _service.CreateAsync(request, customerId);

        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.OPEN, result.Status);
        Assert.Null(result.PaymentUrl);

        _shipmentRepoMock.Verify(r => r.AddAsync(It.Is<Shipment>(s => 
            s.Status == ShipmentStatus.OPEN && 
            s.CustomerId == customerId && 
            s.WeightKg == 2.5m &&
            s.DriverInstruction == "Careful Instruction"
        )), Times.Once);

        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(customerId, It.IsAny<Guid>(), "Shipment Created", It.IsAny<string>()), Times.Once);
        _notificationServiceMock.Verify(n => n.BroadcastNewJobAlertAsync("TWO_WHEELER", It.IsAny<object>()), Times.Once);
        _notificationServiceMock.Verify(n => n.BroadcastNewJobAlertAsync("THREE_WHEELER", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OnlinePayment_CreatesPendingPaymentShipment()
    {

        var request = new CreateShipmentRequest { PackageType = PackageType.SMALL_PARCEL, PaymentMethod = PaymentMethod.ONLINE };

        var result = await _service.CreateAsync(request, Guid.NewGuid());

        Assert.Equal(ShipmentStatus.PENDING_PAYMENT, result.Status);
        Assert.NotNull(result.PaymentUrl);
    }

    [Fact]
    public async Task GetRawByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _shipmentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Shipment)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetRawByIdAsync(id));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsShipment()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, OrderId = "TRK-01" };

        var result = await _service.GetByIdAsync(shipment);

        Assert.NotNull(result);
        Assert.Equal("TRK-01", result.OrderId);
    }

    [Fact]
    public async Task UpdateAsync_InvalidStatus_ThrowsBusinessRuleException()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.ASSIGNED };

        var request = new UpdateShipmentRequest { SpecialNotes = "New Note" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(shipment, request));
    }

    [Fact]
    public async Task UpdateAsync_ValidNotes_UpdatesAndCallsLlm()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN };

        _llmServiceMock.Setup(l => l.ParseDeliveryNoteAsync("New Notes"))
            .ReturnsAsync((true, RiskSeverity.HIGH, "unattended", null, "New Instr"));

        var request = new UpdateShipmentRequest { SpecialNotes = "New Notes" };

        await _service.UpdateAsync(shipment, request);

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

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CancelAsync(shipment, customerId));
    }

    [Fact]
    public async Task CancelAsync_OpenShipment_CancelsAndTriggersRefund()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN };

        var result = await _service.CancelAsync(shipment, customerId);

        Assert.Equal(ShipmentStatus.CANCELLED, result.Status);
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

        _shipmentRepoMock.Setup(r => r.GetActiveShipmentForDriverAsync(driver.Id)).ReturnsAsync(new Shipment());

        await Assert.ThrowsAsync<ValidationException>(() => _service.ClaimAsync(Guid.NewGuid(), driverUserId));
    }

    [Fact]
    public async Task ClaimAsync_ValidClaim_AssignsDriverAndVehicle()
    {

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

        var result = await _service.ClaimAsync(shipmentId, driverUserId);

        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.ASSIGNED, result.Status);
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

        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId, CancelCount = 1 };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, OrderId = "TRK-01", DriverId = driverId, Status = ShipmentStatus.ASSIGNED };

        var request = new CancelClaimRequest { Reason = "Breakdown" };

        var result = await _service.CancelClaimAsync(shipment, driverUserId, request);

        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.OPEN, result.Status);
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

        _otpServiceMock.Setup(o => o.VerifyOtp("1111", "hash")).Returns(false);

        var request = new ConfirmPickupRequest { Otp = "1111" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmPickupAsync(shipment, driverUserId, request));
        Assert.Equal(1, shipment.SenderOtpAttempts);
        _shipmentRepoMock.Verify(r => r.UpdateAsync(shipment), Times.Once);
    }

    [Fact]
    public async Task ConfirmPickupAsync_CorrectOtp_TransitionsToInTransit()
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
            SenderOtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
            SenderOtpAttempts = 0
        };

        _otpServiceMock.Setup(o => o.VerifyOtp("1234", "hash")).Returns(true);

        var request = new ConfirmPickupRequest { Otp = "1234" };

        var result = await _service.ConfirmPickupAsync(shipment, driverUserId, request);

        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.IN_TRANSIT, result.Status);
        Assert.Equal(ShipmentStatus.IN_TRANSIT, shipment.Status);
        _notificationServiceMock.Verify(n => n.CreateNotificationAsync(shipment.CustomerId, shipmentId, "Package Picked Up", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_GeofenceBreach_ThrowsValidationException()
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
            DropLng = 78.0m
        };

        _geoServiceMock.Setup(g => g.CalculateDistance(11.005m, 78.0m, 11.0m, 78.0m)).Returns(0.5);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.005m, DriverLng = 78.0m };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmDeliveryAsync(shipment, driverUserId, request));
        Assert.Equal("Driver is not within 200m of the delivery location.", ex.Message);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_ValidOtpAndGeofence_TransitionsToDelivered()
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
            ReceiverOtpExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _geoServiceMock.Setup(g => g.CalculateDistance(11.0001m, 78.0m, 11.0m, 78.0m)).Returns(0.01);
        _otpServiceMock.Setup(o => o.VerifyOtp("5678", "hash")).Returns(true);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.0001m, DriverLng = 78.0m };

        var result = await _service.ConfirmDeliveryAsync(shipment, driverUserId, request);

        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.DELIVERED, result.Status);
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

        var result = await _service.GetPublicTrackingAsync("TRK-01", "9876543210", "2026-06-15");

        Assert.NotNull(result);
        Assert.Equal("TRK-01", result.OrderId);
        Assert.Equal(ShipmentStatus.OPEN, result.Status);
        Assert.True(result.Timeline.Count > 0);
    }

    [Fact]
    public async Task UpdateAsync_NullSpecialNotes_ResetsLlmInstruction()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipment = new Shipment { Id = id, CustomerId = customerId, Status = ShipmentStatus.OPEN, SpecialNotes = "Note", DriverInstruction = "Instr" };

        var request = new UpdateShipmentRequest { SpecialNotes = "" };

        await _service.UpdateAsync(shipment, request);

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

        var result = await _service.CancelAsync(shipment, customerId);

        Assert.Equal(ShipmentStatus.CANCELLED, result.Status);
        Assert.True(result.RefundInitiated);
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

        var driverUserId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, UserId = driverUserId, CancelCount = 2, User = new User { FullName = "Driver" } };
        _driverRepoMock.Setup(r => r.GetByUserIdAsync(driverUserId)).ReturnsAsync(driver);

        var shipmentId = Guid.NewGuid();
        var shipment = new Shipment { Id = shipmentId, DriverId = driverId, Status = ShipmentStatus.ASSIGNED, Customer = new User() };

        var request = new CancelClaimRequest { Reason = "Reason" };

        var result = await _service.CancelClaimAsync(shipment, driverUserId, request);

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

        var request = new ConfirmPickupRequest { Otp = "1234" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmPickupAsync(shipment, driverUserId, request));
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

        var request = new ConfirmPickupRequest { Otp = "1234" };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmPickupAsync(shipment, driverUserId, request));
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
        _otpServiceMock.Setup(o => o.VerifyOtp("1111", "hash")).Returns(false);

        var request = new ConfirmPickupRequest { Otp = "1111" };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmPickupAsync(shipment, driverUserId, request));
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
        _geoServiceMock.Setup(g => g.CalculateDistance(11.0m, 78.0m, 11.0m, 78.0m)).Returns(0.0);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.0m, DriverLng = 78.0m };

        await Assert.ThrowsAsync<ValidationException>(() => _service.ConfirmDeliveryAsync(shipment, driverUserId, request));
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
        _geoServiceMock.Setup(g => g.CalculateDistance(11.0m, 78.0m, 11.0m, 78.0m)).Returns(0.0);

        var request = new ConfirmDeliveryRequest { Otp = "5678", DriverLat = 11.0m, DriverLng = 78.0m };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmDeliveryAsync(shipment, driverUserId, request));
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
        _geoServiceMock.Setup(g => g.CalculateDistance(11.0m, 78.0m, 11.0m, 78.0m)).Returns(0.0);
        _otpServiceMock.Setup(o => o.VerifyOtp("1111", "hash")).Returns(false);

        var request = new ConfirmDeliveryRequest { Otp = "1111", DriverLat = 11.0m, DriverLng = 78.0m };

        await Assert.ThrowsAsync<TooManyRequestsException>(() => _service.ConfirmDeliveryAsync(shipment, driverUserId, request));
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

        var result = await _service.ConfirmCashCollectedAsync(shipment, driverUserId);

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

        var request = new PickupFailedRequest { Reason = "No show" };

        var result = await _service.MarkPickupFailedAsync(shipment, driverUserId, request);

        Assert.NotNull(result);
        Assert.Equal(ShipmentStatus.PICKUP_FAILED, result.Status);
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
