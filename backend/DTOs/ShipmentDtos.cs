using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.DTOs;

public record CreateShipmentRequest
{
    [Required]
    public string PickupAddress { get; init; } = string.Empty;

    [Required]
    public decimal PickupLat { get; init; }

    [Required]
    public decimal PickupLng { get; init; }

    [Required]
    public string DropAddress { get; init; } = string.Empty;

    [Required]
    public decimal DropLat { get; init; }

    [Required]
    public decimal DropLng { get; init; }

    [Required]
    public string ReceiverName { get; init; } = string.Empty;

    [Required]
    [Phone]
    public string ReceiverPhone { get; init; } = string.Empty;

    [Required]
    public string PackageType { get; init; } = string.Empty; // DOCUMENT, SMALL_PARCEL, etc.

    [Required]
    [Range(0.01, 10000.0)]
    public decimal WeightKg { get; init; }

    public string? PreferredWindow { get; init; } // MORNING, AFTERNOON, EVENING

    public string? SpecialNotes { get; init; }

    [Required]
    public string PaymentMethod { get; init; } = string.Empty; // COD, ONLINE
}

public record CreateShipmentResponse
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? PaymentUrl { get; init; }
    public string? SenderOtp { get; init; }
}

public record ShipmentDriverDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string VehicleType { get; init; } = string.Empty;
    public string VehicleNumber { get; init; } = string.Empty;
}

public record ShipmentResponse
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PickupAddress { get; init; } = string.Empty;
    public decimal PickupLat { get; init; }
    public decimal PickupLng { get; init; }
    public string DropAddress { get; init; } = string.Empty;
    public decimal DropLat { get; init; }
    public decimal DropLng { get; init; }
    public string ReceiverName { get; init; } = string.Empty;
    public string ReceiverPhone { get; init; } = string.Empty;
    public string PackageType { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public string? PreferredWindow { get; init; }
    public string? SpecialNotes { get; init; }

    // LLM fields
    public string? DriverInstruction { get; init; }
    public bool RiskFlag { get; init; }
    public string RiskSeverity { get; init; } = string.Empty;
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
    public string? PreferredWindow { get; init; }
    public string? SpecialNotes { get; init; }
}

public record CancelShipmentResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool RefundInitiated { get; init; }
}

public record AvailableShipmentDto
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public string PickupAddress { get; init; } = string.Empty;
    public string DropAddress { get; init; } = string.Empty;
    public string PackageType { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public string? PreferredWindow { get; init; }
    public string SenderPhone { get; init; } = string.Empty;
    public string ReceiverPhone { get; init; } = string.Empty;
    public double DistanceToPickupKm { get; init; }
    public string? DriverInstruction { get; init; }
}

public record ClaimShipmentResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
}

public record CancelClaimRequest
{
    [Required]
    public string Reason { get; init; } = string.Empty;
}

public record CancelClaimResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
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
    public string Status { get; init; } = string.Empty;
}

public record ConfirmDeliveryRequest
{
    [Required]
    public string Otp { get; init; } = string.Empty;

    [Required]
    public decimal DriverLat { get; init; }

    [Required]
    public decimal DriverLng { get; init; }
}

public record ConfirmDeliveryResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
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
    public string Reason { get; init; } = string.Empty;
}

public record PickupFailedResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record PublicTrackingResponse
{
    public Guid Id { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PickupAddress { get; init; } = string.Empty;
    public string DropAddress { get; init; } = string.Empty;
    public string PackageType { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public string? PreferredWindow { get; init; }
    public string? SpecialNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public string ReceiverOtp { get; init; } = string.Empty;
    public PublicTrackingDriverDto? Driver { get; init; }
    public DriverLocationDto? DriverLocation { get; init; }
    public List<TimelineEntryDto> Timeline { get; init; } = new();
}

public record PublicTrackingDriverDto
{
    public string FullName { get; init; } = string.Empty;
    public string VehicleType { get; init; } = string.Empty;
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
