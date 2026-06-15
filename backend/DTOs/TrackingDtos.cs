using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.DTOs;

public record TrackingLocationRequest
{
    [Required]
    public Guid ShipmentId { get; init; }

    [Required]
    [Range(-90.0, 90.0)]
    public decimal Latitude { get; init; }

    [Required]
    [Range(-180.0, 180.0)]
    public decimal Longitude { get; init; }
}

public record LiveTrackingResponse
{
    public Guid ShipmentId { get; init; }
    public DriverLocationDto? DriverLocation { get; init; }
}

public record TrackingHistoryResponse
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public DateTime RecordedAt { get; init; }
}
