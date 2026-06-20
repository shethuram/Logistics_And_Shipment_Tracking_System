using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;

namespace Logistics.Api.Services;

public class DriverVehicleService : IDriverVehicleService
{
    private readonly IDriverRepository _driverRepo;
    private readonly IVehicleRepository _vehicleRepo;

    public DriverVehicleService(IDriverRepository driverRepo, IVehicleRepository vehicleRepo)
    {
        _driverRepo = driverRepo;
        _vehicleRepo = vehicleRepo;
    }

    public async Task<IReadOnlyList<VehicleDto>> GetVehiclesAsync(Guid driverId)
    {
        await EnsureDriverExistsAsync(driverId);

        var vehicles = await _vehicleRepo.GetByDriverIdAsync(driverId);
        return vehicles.Select(v => v.ToVehicleDto()).ToList();
    }

    public async Task<AddVehicleResponse> AddVehicleAsync(Guid driverId, AddVehicleRequest request)
    {
        await EnsureDriverExistsAsync(driverId);

        if (await _vehicleRepo.ExistsByNumberForDriverAsync(driverId, request.VehicleNumber))
            throw new ConflictException("A vehicle with this number already exists for this driver.");

        var vehicle = new Vehicle
        {
            DriverId = driverId,
            VehicleType = request.VehicleType,
            VehicleNumber = request.VehicleNumber
        };

        await _vehicleRepo.AddAsync(vehicle);

        return vehicle.ToAddVehicleResponse();
    }

    public async Task<VehicleDto> UpdateVehicleAsync(Guid driverId, Guid vehicleId, UpdateVehicleRequest request)
    {
        var vehicle = await GetOwnedVehicleAsync(driverId, vehicleId);

        if (request.VehicleType is not null)
            vehicle.VehicleType = request.VehicleType.Value;

        if (request.VehicleNumber is not null)
        {
            if (!string.Equals(vehicle.VehicleNumber, request.VehicleNumber, StringComparison.Ordinal)
                && await _vehicleRepo.ExistsByNumberForDriverAsync(driverId, request.VehicleNumber))
                throw new ConflictException("A vehicle with this number already exists for this driver.");

            vehicle.VehicleNumber = request.VehicleNumber;
        }

        await _vehicleRepo.UpdateAsync(vehicle);

        return vehicle.ToVehicleDto();
    }

    public async Task<SetActiveVehicleResponse> SetActiveVehicleAsync(Guid driverId, Guid vehicleId)
    {
        var vehicle = await GetOwnedVehicleAsync(driverId, vehicleId);

        await _vehicleRepo.SetActiveAsync(driverId, vehicleId);

        return vehicle.ToSetActiveVehicleResponse();
    }

    private async Task EnsureDriverExistsAsync(Guid driverId)
    {
        var driver = await _driverRepo.GetByIdAsync(driverId);
        if (driver is null)
            throw new NotFoundException("Driver not found.");
    }

    private async Task<Vehicle> GetOwnedVehicleAsync(Guid driverId, Guid vehicleId)
    {
        await EnsureDriverExistsAsync(driverId);

        var vehicle = await _vehicleRepo.GetByIdAsync(vehicleId);
        if (vehicle is null || vehicle.DriverId != driverId)
            throw new NotFoundException("Vehicle not found for this driver.");

        return vehicle;
    }

}
