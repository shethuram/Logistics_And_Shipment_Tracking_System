using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class TrackingMappings
{
    public static TrackingHistoryResponse ToTrackingHistoryResponse(this Tracking t) => new()
    {
        Latitude = t.Latitude,
        Longitude = t.Longitude,
        RecordedAt = t.RecordedAt
    };

    public static DriverLocationDto ToDriverLocationDto(this Tracking t) => new()
    {
        Latitude = t.Latitude,
        Longitude = t.Longitude,
        RecordedAt = t.RecordedAt
    };
}
