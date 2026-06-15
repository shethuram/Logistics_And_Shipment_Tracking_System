using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IDriverVehicleService
{
    Task<IReadOnlyList<VehicleDto>> GetVehiclesAsync(Guid driverId);
    Task<AddVehicleResponse> AddVehicleAsync(Guid driverId, AddVehicleRequest request);
    Task<VehicleDto> UpdateVehicleAsync(Guid driverId, Guid vehicleId, UpdateVehicleRequest request);
    Task<SetActiveVehicleResponse> SetActiveVehicleAsync(Guid driverId, Guid vehicleId);
}
