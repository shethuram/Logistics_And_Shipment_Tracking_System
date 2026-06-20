using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;

namespace Logistics.Api.Services;

public class TrackingService : ITrackingService
{
    private readonly ITrackingRepository _trackingRepo;
    private readonly IShipmentRepository _shipmentRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly INotificationService _notificationService;

    public TrackingService(
        ITrackingRepository trackingRepo,
        IShipmentRepository shipmentRepo,
        IDriverRepository driverRepo,
        INotificationService notificationService)
    {
        _trackingRepo = trackingRepo;
        _shipmentRepo = shipmentRepo;
        _driverRepo = driverRepo;
        _notificationService = notificationService;
    }

    public async Task RecordLocationAsync(TrackingLocationRequest request, Shipment shipment, Guid driverUserId)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
        {
            throw new NotFoundException("Driver profile not found.");
        }

        if (shipment.Status != ShipmentStatus.IN_TRANSIT)
        {
            throw new BusinessRuleException("Tracking is only allowed for shipments that are currently in transit.");
        }

        var tracking = new Tracking
        {
            Id = Guid.NewGuid(),
            ShipmentId = request.ShipmentId,
            DriverId = driver.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RecordedAt = DateTime.UtcNow
        };

        await _trackingRepo.AddAsync(tracking);

        driver.CurrentLat = request.Latitude;
        driver.CurrentLng = request.Longitude;
        driver.LastPingAt = DateTime.UtcNow;

        await _driverRepo.UpdateAsync(driver);

        await _notificationService.BroadcastDriverLocationAsync(request.ShipmentId, request.Latitude, request.Longitude);
    }

    public async Task<LiveTrackingResponse> GetLiveLocationAsync(Shipment shipment)
    {
        var latestPing = await _trackingRepo.GetLatestPingAsync(shipment.Id);

        return new LiveTrackingResponse
        {
            ShipmentId = shipment.Id,
            DriverLocation = latestPing?.ToDriverLocationDto()
        };
    }

    public async Task<IReadOnlyList<TrackingHistoryResponse>> GetHistoryAsync(Guid shipmentId)
    {
        var shipment = await _shipmentRepo.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            throw new NotFoundException("Shipment not found.");
        }

        var history = await _trackingRepo.GetHistoryAsync(shipmentId);

        return history.Select(t => t.ToTrackingHistoryResponse()).ToList();
    }
}
