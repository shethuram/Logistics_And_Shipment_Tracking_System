using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly AppDbContext _db;

    public ShipmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Shipment> AddAsync(Shipment shipment)
    {
        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();
        return shipment;
    }

    public Task<Shipment?> GetByIdAsync(Guid id)
    {
        return _db.Shipments
            .Include(s => s.Customer)
            .Include(s => s.Driver)
                .ThenInclude(d => d!.User)
            .Include(s => s.Driver)
                .ThenInclude(d => d!.ActiveVehicle)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Shipment?> GetByIdForUpdateAsync(Guid id)
    {
        return await _db.Shipments
            .FromSqlRaw("SELECT * FROM shipments WHERE id = {0} AND status = 'OPEN' FOR UPDATE SKIP LOCKED", id)
            .SingleOrDefaultAsync();
    }

    public async Task UpdateAsync(Shipment shipment)
    {
        _db.Shipments.Update(shipment);
        await _db.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<Shipment> Items, int Total)> GetShipmentsAsync(
        Guid? customerId,
        string? search,
        ShipmentStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize)
    {
        var query = _db.Shipments
            .Include(s => s.Customer)
            .Include(s => s.Driver)
                .ThenInclude(d => d!.User)
            .Include(s => s.Driver)
                .ThenInclude(d => d!.ActiveVehicle)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(s => s.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.OrderId.Contains(search) || s.ReceiverPhone.Contains(search));
        }

        query = query.OrderByDescending(s => s.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IReadOnlyList<Shipment>> GetOpenShipmentsAsync()
    {
        return await _db.Shipments
            .Where(s => s.Status == ShipmentStatus.OPEN)
            .ToListAsync();
    }

    public async Task<int> GetTodayCountAsync(string datePrefix)
    {
        return await _db.Shipments
            .CountAsync(s => s.OrderId.StartsWith($"TRK-{datePrefix}-"));
    }

    public Task<Shipment?> GetActiveShipmentForDriverAsync(Guid driverId)
    {
        return _db.Shipments
            .FirstOrDefaultAsync(s => s.DriverId == driverId &&
                (s.Status == ShipmentStatus.ASSIGNED || s.Status == ShipmentStatus.IN_TRANSIT));
    }

    public Task<Shipment?> GetByPublicTrackParamsAsync(string orderId, string phone)
    {
        return _db.Shipments
            .Include(s => s.Customer)
            .Include(s => s.Driver)
                .ThenInclude(d => d!.User)
            .Include(s => s.Driver)
                .ThenInclude(d => d!.ActiveVehicle)
            .Include(s => s.TrackingPings)
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.OrderId == orderId && s.Customer.Phone == phone);
    }
}
