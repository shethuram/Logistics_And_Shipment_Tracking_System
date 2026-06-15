using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, Guid? shipmentId, string title, string message);
    Task BroadcastShipmentUpdateAsync(Guid shipmentId, string status, object data);
    Task BroadcastNewJobAlertAsync(string vehicleType, object data);
    Task BroadcastAdminAlertAsync(string alertType, object data);
    Task BroadcastDriverLocationAsync(Guid shipmentId, decimal latitude, decimal longitude);
    Task<MyNotificationsResponse> GetMyNotificationsAsync(Guid userId, int page, int pageSize);
    Task<MarkReadResponse> MarkAsReadAsync(Guid id, Guid userId);
}
