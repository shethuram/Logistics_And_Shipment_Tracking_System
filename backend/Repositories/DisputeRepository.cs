using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly AppDbContext _db;

    public DisputeRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Dispute dispute)
    {
        _db.Disputes.Add(dispute);
        await _db.SaveChangesAsync();
    }

    public async Task<Dispute?> GetByIdAsync(Guid id)
    {
        return await _db.Disputes
            .Include(d => d.Shipment)
            .Include(d => d.RaisedByUser)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<(IReadOnlyList<Dispute> Items, int Total)> GetDisputesAsync(DisputeStatus? status, int page, int pageSize)
    {
        var query = _db.Disputes
            .Include(d => d.Shipment)
            .Include(d => d.RaisedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        query = query.OrderByDescending(d => d.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task UpdateAsync(Dispute dispute)
    {
        _db.Disputes.Update(dispute);
        await _db.SaveChangesAsync();
    }
}
