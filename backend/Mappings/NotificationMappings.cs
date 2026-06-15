using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class NotificationMappings
{
    public static NotificationDto ToNotificationDto(this Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
