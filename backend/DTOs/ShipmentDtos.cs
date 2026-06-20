using System.ComponentModel.DataAnnotations;
using Logistics.Api.Models;

namespace Logistics.Api.DTOs;

public record CreateShipmentRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string PickupAddress { get; init; } = string.Empty;

    [Required]
    [Range(-90.0, 90.0)]
    public decimal PickupLat { get; init; }

    [Required]
    [Range(-180.0, 180.0)]
    public decimal PickupLng { get; init; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string DropAddress { get; init; } = string.Empty;

    [Required]
    [Range(-90.0, 90.0)]
    public decimal DropLat { get; init; }

    [Required]
    [Range(-180.0, 180.0)]
    public decimal DropLng { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string ReceiverName { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
    public string ReceiverPhone { get; init; } = string.Empty;

    [Required]
    public PackageType PackageType { get; init; }

    [Required]
    [Range(0.01, 10000.0)]
    public decimal WeightKg { get; init; }

    public PreferredWindow? PreferredWindow { get; init; }

    [StringLength(500)]
    public string? SpecialNotes { get; init; }

    [Required]
    public PaymentMethod PaymentMethod { get; init; }
}

public record CreateShipmentResponse
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public ShipmentStatus Status { get; init; }
    public string? PaymentUrl { get; init; }
    public string? SenderOtp { get; init; }
}

public record ShipmentDriverDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public VehicleType? VehicleType { get; init; }
    public string VehicleNumber { get; init; } = string.Empty;
}

public record ShipmentResponse
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public ShipmentStatus Status { get; init; }
    public string PickupAddress { get; init; } = string.Empty;
    public decimal PickupLat { get; init; }
    public decimal PickupLng { get; init; }
    public string DropAddress { get; init; } = string.Empty;
    public decimal DropLat { get; init; }
    public decimal DropLng { get; init; }
    public string ReceiverName { get; init; } = string.Empty;
    public string ReceiverPhone { get; init; } = string.Empty;
    public PackageType PackageType { get; init; }
    public decimal WeightKg { get; init; }
    public PreferredWindow? PreferredWindow { get; init; }
    public string? SpecialNotes { get; init; }

    // LLM fields
    public string? DriverInstruction { get; init; }
    public bool RiskFlag { get; init; }
    public RiskSeverity RiskSeverity { get; init; }
    public string? RiskReason { get; init; }
    public TimeOnly? PreferredDeliveryAfter { get; init; }

    public ShipmentDriverDto? Driver { get; init; }
    
    public bool CashCollected { get; init; }
    public Guid? StatusChangedBy { get; init; }
    public DateTime? StatusUpdatedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record UpdateShipmentRequest
{
    public PreferredWindow? PreferredWindow { get; init; }
    [StringLength(500)]
    public string? SpecialNotes { get; init; }
}

public record CancelShipmentResponse
{
    public Guid Id { get; init; }
    public ShipmentStatus Status { get; init; }
    public bool RefundInitiated { get; init; }
}

public record AvailableShipmentDto
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public string PickupAddress { get; init; } = string.Empty;
    public string DropAddress { get; init; } = string.Empty;
    public PackageType PackageType { get; init; }
    public decimal WeightKg { get; init; }
    public PreferredWindow? PreferredWindow { get; init; }
    public string SenderPhone { get; init; } = string.Empty;
    public string ReceiverPhone { get; init; } = string.Empty;
    public double DistanceToPickupKm { get; init; }
    public string? DriverInstruction { get; init; }
}

public record ClaimShipmentResponse
{
    public Guid Id { get; init; }
    public ShipmentStatus Status { get; init; }
    public string OrderId { get; init; } = string.Empty;
}

public record CancelClaimRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; init; } = string.Empty;
}

public record CancelClaimResponse
{
    public Guid Id { get; init; }
    public ShipmentStatus Status { get; init; }
    public int DriverCancelCount { get; init; }
}

public record ConfirmPickupRequest
{
    [Required]
    public string Otp { get; init; } = string.Empty;
}

public record ConfirmPickupResponse
{
    public Guid Id { get; init; }
    public ShipmentStatus Status { get; init; }
}

public record ConfirmDeliveryRequest
{
    [Required]
    public string Otp { get; init; } = string.Empty;

    [Required]
    [Range(-90.0, 90.0)]
    public decimal DriverLat { get; init; }

    [Required]
    [Range(-180.0, 180.0)]
    public decimal DriverLng { get; init; }
}

public record ConfirmDeliveryResponse
{
    public Guid Id { get; init; }
    public ShipmentStatus Status { get; init; }
    public DateTime DeliveredAt { get; init; }
}

public record CashCollectedResponse
{
    public Guid Id { get; init; }
    public bool CashCollected { get; init; }
}

public record PickupFailedRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; init; } = string.Empty;
}

public record PickupFailedResponse
{
    public Guid Id { get; init; }
    public ShipmentStatus Status { get; init; }
}

public record PublicTrackingResponse
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public ShipmentStatus Status { get; init; }
    public string PickupAddress { get; init; } = string.Empty;
    public string DropAddress { get; init; } = string.Empty;
    public PackageType PackageType { get; init; }
    public decimal WeightKg { get; init; }
    public PreferredWindow? PreferredWindow { get; init; }
    public string? SpecialNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public PublicTrackingDriverDto? Driver { get; init; }
    public DriverLocationDto? DriverLocation { get; init; }
    public List<TimelineEntryDto> Timeline { get; init; } = new();
}

public record PublicTrackingDriverDto
{
    public string FullName { get; init; } = string.Empty;
    public VehicleType? VehicleType { get; init; }
    public string VehicleNumber { get; init; } = string.Empty;
}

public record DriverLocationDto
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public DateTime RecordedAt { get; init; }
}

public record TimelineEntryDto
{
    public string Status { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime? Timestamp { get; init; }
    public bool IsCompleted { get; init; }
}
