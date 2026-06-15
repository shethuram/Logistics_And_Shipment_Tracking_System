using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class DriverServiceTests
{
    private readonly Mock<IDriverRepository> _driverRepoMock;
    private readonly Mock<IVehicleRepository> _vehicleRepoMock;
    private readonly DriverService _driverService;

    public DriverServiceTests()
    {
        _driverRepoMock = new Mock<IDriverRepository>();
        _vehicleRepoMock = new Mock<IVehicleRepository>();
        _driverService = new DriverService(_driverRepoMock.Object, _vehicleRepoMock.Object);
    }

    [Fact]
    public async Task GoOnlineAsync_DriverNotFound_ThrowsNotFoundException()
    {

        var driverId = Guid.NewGuid();
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync((Driver)null!);

        var request = new GoOnlineRequest { Latitude = 12.34m, Longitude = 56.78m };

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _driverService.GoOnlineAsync(driverId, request));
    }

    [Fact]
    public async Task GoOnlineAsync_DriverNotApproved_ThrowsBusinessRuleException()
    {

        var driverId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId,
            ApprovalStatus = ApprovalStatus.PENDING
        };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var request = new GoOnlineRequest { Latitude = 12.34m, Longitude = 56.78m };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _driverService.GoOnlineAsync(driverId, request));
        Assert.Equal("Driver must be approved before going online.", ex.Message);
    }

    [Fact]
    public async Task GoOnlineAsync_NoActiveVehicleId_ThrowsValidationException()
    {

        var driverId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId,
            ApprovalStatus = ApprovalStatus.APPROVED,
            ActiveVehicleId = null
        };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var request = new GoOnlineRequest { Latitude = 12.34m, Longitude = 56.78m };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _driverService.GoOnlineAsync(driverId, request));
        Assert.Equal("Driver has no active vehicle set.", ex.Message);
    }

    [Fact]
    public async Task GoOnlineAsync_ActiveVehicleNotFound_ThrowsValidationException()
    {

        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId,
            ApprovalStatus = ApprovalStatus.APPROVED,
            ActiveVehicleId = vehicleId
        };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync((Vehicle)null!);

        var request = new GoOnlineRequest { Latitude = 12.34m, Longitude = 56.78m };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _driverService.GoOnlineAsync(driverId, request));
        Assert.Equal("Driver has no active vehicle set.", ex.Message);
    }

    [Fact]
    public async Task GoOnlineAsync_ValidApprovedDriverAndVehicle_UpdatesStatusOnline()
    {

        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId,
            ApprovalStatus = ApprovalStatus.APPROVED,
            ActiveVehicleId = vehicleId,
            OperationalStatus = OperationalStatus.OFFLINE
        };
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            VehicleType = VehicleType.FOUR_WHEELER,
            VehicleNumber = "TN-37-AB-1234"
        };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        var request = new GoOnlineRequest { Latitude = 12.3456m, Longitude = 78.9012m };

        var result = await _driverService.GoOnlineAsync(driverId, request);

        Assert.NotNull(result);
        Assert.Equal("ONLINE", result.OperationalStatus);
        Assert.Equal("FOUR_WHEELER", result.ActiveVehicleType);

        Assert.Equal(OperationalStatus.ONLINE, driver.OperationalStatus);
        Assert.Equal(12.3456m, driver.CurrentLat);
        Assert.Equal(78.9012m, driver.CurrentLng);
        Assert.True(driver.LastPingAt > DateTime.UtcNow.AddSeconds(-5));

        _driverRepoMock.Verify(r => r.UpdateAsync(driver), Times.Once);
    }

    [Fact]
    public async Task GoOfflineAsync_DriverNotFound_ThrowsNotFoundException()
    {

        var driverId = Guid.NewGuid();
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync((Driver)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _driverService.GoOfflineAsync(driverId));
    }

    [Fact]
    public async Task GoOfflineAsync_ValidDriver_UpdatesStatusOffline()
    {

        var driverId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId,
            OperationalStatus = OperationalStatus.ONLINE
        };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var result = await _driverService.GoOfflineAsync(driverId);

        Assert.NotNull(result);
        Assert.Equal("OFFLINE", result.OperationalStatus);
        Assert.Equal(OperationalStatus.OFFLINE, driver.OperationalStatus);

        _driverRepoMock.Verify(r => r.UpdateAsync(driver), Times.Once);
    }
}
