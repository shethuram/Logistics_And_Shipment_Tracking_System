using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<(IReadOnlyList<Notification> Items, int Total, int UnreadCount)> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task UpdateAsync(Notification notification);
}
