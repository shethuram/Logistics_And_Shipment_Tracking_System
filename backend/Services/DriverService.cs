using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;

namespace Logistics.Api.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _driverRepo;
    private readonly IVehicleRepository _vehicleRepo;

    public DriverService(IDriverRepository driverRepo, IVehicleRepository vehicleRepo)
    {
        _driverRepo = driverRepo;
        _vehicleRepo = vehicleRepo;
    }

    public async Task<GoOnlineResponse> GoOnlineAsync(Guid driverId, GoOnlineRequest request)
    {
        var driver = await GetOrThrowAsync(driverId);

        if (driver.ApprovalStatus != ApprovalStatus.APPROVED)
            throw new BusinessRuleException("Driver must be approved before going online.");

        if (driver.ActiveVehicleId is null)
            throw new ValidationException("Driver has no active vehicle set.");

        var activeVehicle = await _vehicleRepo.GetByIdAsync(driver.ActiveVehicleId.Value);
        if (activeVehicle is null)
            throw new ValidationException("Driver has no active vehicle set.");

        driver.OperationalStatus = OperationalStatus.ONLINE;
        driver.CurrentLat = request.Latitude;
        driver.CurrentLng = request.Longitude;
        driver.LastPingAt = DateTime.UtcNow;

        await _driverRepo.UpdateAsync(driver);

        return driver.ToGoOnlineResponse(activeVehicle);
    }

    public async Task<GoOfflineResponse> GoOfflineAsync(Guid driverId)
    {
        var driver = await GetOrThrowAsync(driverId);

        driver.OperationalStatus = OperationalStatus.OFFLINE;

        await _driverRepo.UpdateAsync(driver);

        return driver.ToGoOfflineResponse();
    }

    private async Task<Driver> GetOrThrowAsync(Guid id)
    {
        var driver = await _driverRepo.GetByIdAsync(id);
        if (driver is null)
            throw new NotFoundException("Driver not found.");
        return driver;
    }
}
