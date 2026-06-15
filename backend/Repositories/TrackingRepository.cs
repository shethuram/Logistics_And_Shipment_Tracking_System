using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class TrackingRepository : ITrackingRepository
{
    private readonly AppDbContext _db;

    public TrackingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Tracking tracking)
    {
        _db.Tracking.Add(tracking);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Tracking>> GetHistoryAsync(Guid shipmentId)
    {
        return await _db.Tracking
            .Where(t => t.ShipmentId == shipmentId)
            .OrderBy(t => t.RecordedAt)
            .ToListAsync();
    }

    public async Task<Tracking?> GetLatestPingAsync(Guid shipmentId)
    {
        return await _db.Tracking
            .Where(t => t.ShipmentId == shipmentId)
            .OrderByDescending(t => t.RecordedAt)
            .FirstOrDefaultAsync();
    }
}
