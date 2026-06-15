using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly AppDbContext _db;

    public DriverRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Driver> AddAsync(Driver driver)
    {
        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync();
        return driver;
    }

    public async Task<(IReadOnlyList<Driver> Items, int Total)> GetByApprovalStatusAsync(ApprovalStatus status, int page, int pageSize)
    {
        var query = _db.Drivers
            .AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Vehicles)
            .Where(d => d.ApprovalStatus == status)
            .OrderBy(d => d.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<Driver?> GetByIdAsync(Guid id) =>
        _db.Drivers.FirstOrDefaultAsync(d => d.Id == id);

    public Task<Driver?> GetByUserIdAsync(Guid userId) =>
        _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

    public Task UpdateAsync(Driver driver)
    {
        _db.Drivers.Update(driver);
        return _db.SaveChangesAsync();
    }
}
