using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.DTOs;

public record GoOnlineRequest
{
    [Required]
    [Range(-90, 90)]
    public decimal Latitude { get; init; }

    [Required]
    [Range(-180, 180)]
    public decimal Longitude { get; init; }
}

public record GoOnlineResponse
{
    public string OperationalStatus { get; init; } = string.Empty;
    public string ActiveVehicleType { get; init; } = string.Empty;
}

public record GoOfflineResponse
{
    public string OperationalStatus { get; init; } = string.Empty;
}
