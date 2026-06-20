using System.ComponentModel.DataAnnotations;
using Logistics.Api.Models;

namespace Logistics.Api.DTOs;

public record VehicleDto
{
    public Guid Id { get; init; }
    public VehicleType VehicleType { get; init; }
    public string VehicleNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AddVehicleRequest
{
    [Required]
    public VehicleType VehicleType { get; init; }

    [Required]
    [RegularExpression(@"^[A-Z]{2}[ -]?[0-9]{1,2}[ -]?[A-Z]{1,2}[ -]?[0-9]{4}$", ErrorMessage = "Invalid Vehicle Number format.")]
    public string VehicleNumber { get; init; } = string.Empty;
}

public record AddVehicleResponse
{
    public Guid Id { get; init; }
    public VehicleType VehicleType { get; init; }
    public string VehicleNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public record UpdateVehicleRequest
{
    public VehicleType? VehicleType { get; init; }
    
    [RegularExpression(@"^[A-Z]{2}[ -]?[0-9]{1,2}[ -]?[A-Z]{1,2}[ -]?[0-9]{4}$", ErrorMessage = "Invalid Vehicle Number format.")]
    public string? VehicleNumber { get; init; }
}

public record SetActiveVehicleResponse
{
    public Guid ActiveVehicleId { get; init; }
    public VehicleType VehicleType { get; init; }
}
