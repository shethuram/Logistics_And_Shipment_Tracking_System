using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class VehicleMappings
{
    public static VehicleDto ToVehicleDto(this Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        VehicleType = vehicle.VehicleType.ToString(),
        VehicleNumber = vehicle.VehicleNumber,
        IsActive = vehicle.IsActive,
        CreatedAt = vehicle.CreatedAt
    };

    public static AddVehicleResponse ToAddVehicleResponse(this Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        VehicleType = vehicle.VehicleType.ToString(),
        VehicleNumber = vehicle.VehicleNumber,
        IsActive = vehicle.IsActive
    };

    public static SetActiveVehicleResponse ToSetActiveVehicleResponse(this Vehicle vehicle) => new()
    {
        ActiveVehicleId = vehicle.Id,
        VehicleType = vehicle.VehicleType.ToString()
    };

    public static PendingDriverVehicleDto ToPendingDriverVehicleDto(this Vehicle vehicle) => new()
    {
        VehicleType = vehicle.VehicleType.ToString(),
        VehicleNumber = vehicle.VehicleNumber
    };
}
