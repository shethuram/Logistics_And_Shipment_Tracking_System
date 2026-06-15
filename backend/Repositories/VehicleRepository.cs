using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _db;

    public VehicleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Vehicle>> GetByDriverIdAsync(Guid driverId)
    {
        return await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.DriverId == driverId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();
    }

    public Task<Vehicle?> GetByIdAsync(Guid id) =>
        _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id);

    public Task<bool> ExistsByNumberForDriverAsync(Guid driverId, string vehicleNumber) =>
        _db.Vehicles.AnyAsync(v => v.DriverId == driverId && v.VehicleNumber == vehicleNumber);

    public async Task<Vehicle> AddAsync(Vehicle vehicle)
    {
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return vehicle;
    }

    public Task UpdateAsync(Vehicle vehicle)
    {
        _db.Vehicles.Update(vehicle);
        return _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(Guid driverId, Guid vehicleId)
    {
        var vehicles = await _db.Vehicles
            .Where(v => v.DriverId == driverId)
            .ToListAsync();

        foreach (var v in vehicles)
            v.IsActive = v.Id == vehicleId;

        var driver = await _db.Drivers.FirstAsync(d => d.Id == driverId);
        driver.ActiveVehicleId = vehicleId;

        await _db.SaveChangesAsync();
    }
}
