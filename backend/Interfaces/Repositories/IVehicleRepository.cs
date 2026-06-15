using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface IVehicleRepository
{
    Task<IReadOnlyList<Vehicle>> GetByDriverIdAsync(Guid driverId);
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task<bool> ExistsByNumberForDriverAsync(Guid driverId, string vehicleNumber);
    Task<Vehicle> AddAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
    Task SetActiveAsync(Guid driverId, Guid vehicleId);
}
