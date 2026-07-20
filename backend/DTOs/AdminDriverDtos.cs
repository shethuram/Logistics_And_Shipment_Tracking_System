using System.ComponentModel.DataAnnotations;
using Logistics.Api.Models;

namespace Logistics.Api.DTOs;

public record PagedResult<T>
{
    public IReadOnlyList<T> Data { get; init; } = Array.Empty<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record PendingDriverVehicleDto
{
    public VehicleType VehicleType { get; init; }
    public string VehicleNumber { get; init; } = string.Empty;
}

public record PendingDriverDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
    public IReadOnlyList<PendingDriverVehicleDto> Vehicles { get; init; } = Array.Empty<PendingDriverVehicleDto>();
    public DateTime CreatedAt { get; init; }
}

public record ApproveDriverResponse
{
    public Guid Id { get; init; }
    public ApprovalStatus ApprovalStatus { get; init; }
    public DateTime? ApprovedAt { get; init; }
}

public record RejectDriverRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; init; } = string.Empty;
}

public record SuspendDriverRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; init; } = string.Empty;
}

public record DriverApprovalResponse
{
    public Guid Id { get; init; }
    public ApprovalStatus ApprovalStatus { get; init; }
    public string? ApprovalReason { get; init; }
}

public record AdminDriverDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; init; }
    public OperationalStatus OperationalStatus { get; init; }
    public IReadOnlyList<PendingDriverVehicleDto> Vehicles { get; init; } = Array.Empty<PendingDriverVehicleDto>();
    public int CancelCount { get; init; }
    public DateTime CreatedAt { get; init; }

    public string? LicenseFileUrl { get; init; }
    public VerificationStatus VerificationStatus { get; init; }
    public string? VerificationReport { get; init; }
    public string[]? LicenseClasses { get; init; }
    public string[]? AllowedVehicleTypes { get; init; }
}

public record UpdateDriverVerificationRequest
{
    public VerificationStatus VerificationStatus { get; init; }
    public string? VerificationReport { get; init; }
    public string[]? LicenseClasses { get; init; }
    public string[]? AllowedVehicleTypes { get; init; }
}
