using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.DTOs;

public record VehicleDto
{
    public Guid Id { get; init; }
    public string VehicleType { get; init; } = string.Empty;
    public string VehicleNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AddVehicleRequest
{
    [Required]
    public string VehicleType { get; init; } = string.Empty;

    [Required]
    public string VehicleNumber { get; init; } = string.Empty;
}

public record AddVehicleResponse
{
    public Guid Id { get; init; }
    public string VehicleType { get; init; } = string.Empty;
    public string VehicleNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public record UpdateVehicleRequest
{
    public string? VehicleType { get; init; }
    public string? VehicleNumber { get; init; }
}

public record SetActiveVehicleResponse
{
    public Guid ActiveVehicleId { get; init; }
    public string VehicleType { get; init; } = string.Empty;
}
