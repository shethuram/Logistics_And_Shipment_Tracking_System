using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Notification notification)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<(IReadOnlyList<Notification> Items, int Total, int UnreadCount)> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var unreadCount = await query.CountAsync(n => !n.IsRead);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total, unreadCount);
    }

    public async Task UpdateAsync(Notification notification)
    {
        _db.Notifications.Update(notification);
        await _db.SaveChangesAsync();
    }
}
