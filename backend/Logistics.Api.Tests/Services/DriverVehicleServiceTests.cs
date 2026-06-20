using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class DriverVehicleServiceTests
{
    private readonly Mock<IDriverRepository> _driverRepoMock;
    private readonly Mock<IVehicleRepository> _vehicleRepoMock;
    private readonly DriverVehicleService _service;

    public DriverVehicleServiceTests()
    {
        _driverRepoMock = new Mock<IDriverRepository>();
        _vehicleRepoMock = new Mock<IVehicleRepository>();
        _service = new DriverVehicleService(_driverRepoMock.Object, _vehicleRepoMock.Object);
    }

    private void SetupDriverExists(Guid driverId)
    {
        var driver = new Driver { Id = driverId };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
    }

    [Fact]
    public async Task GetVehiclesAsync_DriverNotFound_ThrowsNotFoundException()
    {
        var driverId = Guid.NewGuid();
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync((Driver)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetVehiclesAsync(driverId));
    }

    [Fact]
    public async Task GetVehiclesAsync_DriverExists_ReturnsVehicles()
    {
        var driverId = Guid.NewGuid();
        SetupDriverExists(driverId);

        var vehicles = new List<Vehicle>
        {
            new Vehicle { Id = Guid.NewGuid(), DriverId = driverId, VehicleType = VehicleType.TWO_WHEELER, VehicleNumber = "V1" }
        };
        _vehicleRepoMock.Setup(r => r.GetByDriverIdAsync(driverId)).ReturnsAsync(vehicles);

        var result = await _service.GetVehiclesAsync(driverId);

        Assert.Single(result);
        Assert.Equal(VehicleType.TWO_WHEELER, result[0].VehicleType);
        Assert.Equal("V1", result[0].VehicleNumber);
    }

    [Fact]
    public async Task AddVehicleAsync_DuplicateVehicleNumber_ThrowsConflictException()
    {
        var driverId = Guid.NewGuid();
        SetupDriverExists(driverId);
        _vehicleRepoMock.Setup(r => r.ExistsByNumberForDriverAsync(driverId, "V1")).ReturnsAsync(true);
        var request = new AddVehicleRequest { VehicleType = VehicleType.TWO_WHEELER, VehicleNumber = "V1" };

        await Assert.ThrowsAsync<ConflictException>(() => _service.AddVehicleAsync(driverId, request));
    }

    [Fact]
    public async Task AddVehicleAsync_ValidInputs_AddsVehicle()
    {
        var driverId = Guid.NewGuid();
        SetupDriverExists(driverId);
        _vehicleRepoMock.Setup(r => r.ExistsByNumberForDriverAsync(driverId, "V1")).ReturnsAsync(false);
        var request = new AddVehicleRequest { VehicleType = VehicleType.TWO_WHEELER, VehicleNumber = "V1" };

        var result = await _service.AddVehicleAsync(driverId, request);

        Assert.NotNull(result);
        Assert.Equal(VehicleType.TWO_WHEELER, result.VehicleType);
        Assert.Equal("V1", result.VehicleNumber);
        _vehicleRepoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Once);
    }

    [Fact]
    public async Task UpdateVehicleAsync_VehicleNotFoundOrNotOwned_ThrowsNotFoundException()
    {
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        SetupDriverExists(driverId);
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync((Vehicle)null!);
        var request = new UpdateVehicleRequest { VehicleNumber = "V2" };

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateVehicleAsync(driverId, vehicleId, request));
    }

    [Fact]
    public async Task UpdateVehicleAsync_DuplicateNumberOnUpdate_ThrowsConflictException()
    {
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        SetupDriverExists(driverId);
        var vehicle = new Vehicle { Id = vehicleId, DriverId = driverId, VehicleNumber = "V1", VehicleType = VehicleType.TWO_WHEELER };
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);
        _vehicleRepoMock.Setup(r => r.ExistsByNumberForDriverAsync(driverId, "V2")).ReturnsAsync(true);
        var request = new UpdateVehicleRequest { VehicleNumber = "V2" };

        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateVehicleAsync(driverId, vehicleId, request));
    }

    [Fact]
    public async Task UpdateVehicleAsync_ValidUpdate_UpdatesVehicle()
    {
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        SetupDriverExists(driverId);
        var vehicle = new Vehicle { Id = vehicleId, DriverId = driverId, VehicleNumber = "V1", VehicleType = VehicleType.TWO_WHEELER };
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);
        var request = new UpdateVehicleRequest { VehicleNumber = "V2", VehicleType = VehicleType.THREE_WHEELER };

        var result = await _service.UpdateVehicleAsync(driverId, vehicleId, request);

        Assert.Equal("V2", result.VehicleNumber);
        Assert.Equal(VehicleType.THREE_WHEELER, result.VehicleType);
        _vehicleRepoMock.Verify(r => r.UpdateAsync(vehicle), Times.Once);
    }

    [Fact]
    public async Task SetActiveVehicleAsync_ValidVehicle_SetsActive()
    {
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        SetupDriverExists(driverId);
        var vehicle = new Vehicle { Id = vehicleId, DriverId = driverId, VehicleNumber = "V1", VehicleType = VehicleType.TWO_WHEELER };
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicleId)).ReturnsAsync(vehicle);

        var result = await _service.SetActiveVehicleAsync(driverId, vehicleId);

        Assert.Equal(vehicleId, result.ActiveVehicleId);
        _vehicleRepoMock.Verify(r => r.SetActiveAsync(driverId, vehicleId), Times.Once);
    }
}
