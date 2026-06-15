using Microsoft.AspNetCore.SignalR;

namespace Logistics.Api.Hubs;

public class TrackingHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    public async Task JoinShipmentGroup(string shipmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"shipment-{shipmentId}");
    }

    public async Task LeaveShipmentGroup(string shipmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"shipment-{shipmentId}");
    }

    public async Task JoinVehicleGroup(string vehicleType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"vehicle-{vehicleType}");
    }

    public async Task LeaveVehicleGroup(string vehicleType)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"vehicle-{vehicleType}");
    }

    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
    }

    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin");
    }
}
