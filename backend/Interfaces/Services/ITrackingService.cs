using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface ITrackingService
{
    Task RecordLocationAsync(TrackingLocationRequest request, Guid driverUserId);
    Task<LiveTrackingResponse> GetLiveLocationAsync(Guid shipmentId, Guid userId, string role);
    Task<IReadOnlyList<TrackingHistoryResponse>> GetHistoryAsync(Guid shipmentId, string role);
}
