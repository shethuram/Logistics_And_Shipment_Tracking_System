using System.ComponentModel.DataAnnotations;
using Logistics.Api.Models;

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
    public OperationalStatus OperationalStatus { get; init; }
    public VehicleType ActiveVehicleType { get; init; }
}

public record GoOfflineResponse
{
    public OperationalStatus OperationalStatus { get; init; }
}
