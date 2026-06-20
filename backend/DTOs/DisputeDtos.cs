using System.ComponentModel.DataAnnotations;
using Logistics.Api.Models;

namespace Logistics.Api.DTOs;

public record RaiseDisputeRequest
{
    [Required]
    public Guid ShipmentId { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string ComplaintText { get; init; } = string.Empty;
}

public record RaiseDisputeResponse
{
    public Guid Id { get; init; }
    public DisputeStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record DisputeAdminDto
{
    public Guid Id { get; init; }
    public Guid ShipmentId { get; init; }
    public string OrderId { get; init; } = string.Empty;
    public string RaisedBy { get; init; } = string.Empty;
    public string ComplaintText { get; init; } = string.Empty;
    public string? LlmSummary { get; init; }
    public DisputeLlmType? LlmType { get; init; }
    public string? LlmSuggestedResolution { get; init; }
    public DisputeStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ResolveDisputeRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string ResolutionNotes { get; init; } = string.Empty;

    [Required]
    public DisputeStatus Status { get; init; }
}

public record ResolveDisputeResponse
{
    public Guid Id { get; init; }
    public DisputeStatus Status { get; init; }
    public DateTime ResolvedAt { get; init; }
}
