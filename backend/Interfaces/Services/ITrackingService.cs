using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface ITrackingService
{
    Task RecordLocationAsync(TrackingLocationRequest request, Shipment shipment, Guid driverUserId);
    Task<LiveTrackingResponse> GetLiveLocationAsync(Shipment shipment);
    Task<IReadOnlyList<TrackingHistoryResponse>> GetHistoryAsync(Guid shipmentId);
}
