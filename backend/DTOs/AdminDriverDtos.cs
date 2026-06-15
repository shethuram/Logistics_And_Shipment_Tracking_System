using System.ComponentModel.DataAnnotations;

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
    public string VehicleType { get; init; } = string.Empty;
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
    public string ApprovalStatus { get; init; } = string.Empty;
    public DateTime? ApprovedAt { get; init; }
}

public record RejectDriverRequest
{
    [Required]
    public string Reason { get; init; } = string.Empty;
}

public record SuspendDriverRequest
{
    [Required]
    public string Reason { get; init; } = string.Empty;
}

public record DriverApprovalResponse
{
    public Guid Id { get; init; }
    public string ApprovalStatus { get; init; } = string.Empty;
    public string? ApprovalReason { get; init; }
}
