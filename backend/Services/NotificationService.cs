using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Hubs;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace Logistics.Api.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly AppDbContext _db;
    private readonly IHubContext<TrackingHub> _hubContext;

    public NotificationService(
        INotificationRepository notificationRepo,
        AppDbContext db,
        IHubContext<TrackingHub> hubContext)
    {
        _notificationRepo = notificationRepo;
        _db = db;
        _hubContext = hubContext;
    }

    public async Task CreateNotificationAsync(Guid userId, Guid? shipmentId, string title, string message)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShipmentId = shipmentId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepo.AddAsync(notification);

        await _hubContext.Clients.Group($"user-{userId}").SendAsync("notificationReceived", new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt,
            notification.ShipmentId
        });
    }

    public async Task BroadcastShipmentUpdateAsync(Guid shipmentId, string status, object data)
    {
        await _hubContext.Clients.Group($"shipment-{shipmentId}").SendAsync("shipmentUpdated", new
        {
            ShipmentId = shipmentId,
            Status = status,
            Timestamp = DateTime.UtcNow,
            Data = data
        });
    }

    public async Task BroadcastNewJobAlertAsync(string vehicleType, object data)
    {
        await _hubContext.Clients.Group($"vehicle-{vehicleType}").SendAsync("newJobAlert", data);
    }

    public async Task BroadcastAdminAlertAsync(string alertType, object data)
    {
        await _hubContext.Clients.Group("admin").SendAsync("adminAlert", new
        {
            Type = alertType,
            Timestamp = DateTime.UtcNow,
            Data = data
        });
    }

    public async Task BroadcastDriverLocationAsync(Guid shipmentId, decimal latitude, decimal longitude)
    {
        await _hubContext.Clients.Group($"shipment-{shipmentId}").SendAsync("locationUpdated", new
        {
            ShipmentId = shipmentId,
            Latitude = latitude,
            Longitude = longitude,
            RecordedAt = DateTime.UtcNow
        });
    }

    public async Task<MyNotificationsResponse> GetMyNotificationsAsync(Guid userId, int page, int pageSize)
    {
        var (items, total, unreadCount) = await _notificationRepo.GetByUserIdAsync(userId, page, pageSize);

        return new MyNotificationsResponse
        {
            Data = items.Select(n => n.ToNotificationDto()).ToList(),
            UnreadCount = unreadCount,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MarkReadResponse> MarkAsReadAsync(Guid id, Guid userId)
    {
        var notification = await _notificationRepo.GetByIdAsync(id);
        if (notification == null)
        {
            throw new NotFoundException("Notification not found.");
        }

        if (notification.UserId != userId)
        {
            throw new ForbiddenException("You are not authorized to mark this notification as read.");
        }

        notification.IsRead = true;
        await _notificationRepo.UpdateAsync(notification);

        return new MarkReadResponse
        {
            Id = notification.Id,
            IsRead = notification.IsRead
        };
    }
}
