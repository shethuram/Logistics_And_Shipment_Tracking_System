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

public record UpdateDriverProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^[A-Z]{2}[0-9]{2}[ ][0-9]{11}$", ErrorMessage = "Invalid License Number format. Must match SSRR YYYYNNNNNNN format (e.g. KA03 20150012345).")]
    public string LicenseNumber { get; init; } = string.Empty;
}
