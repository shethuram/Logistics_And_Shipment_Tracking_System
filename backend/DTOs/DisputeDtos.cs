using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.DTOs;

public record RaiseDisputeRequest
{
    [Required]
    public Guid ShipmentId { get; init; }

    [Required]
    public string ComplaintText { get; init; } = string.Empty;
}

public record RaiseDisputeResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
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
    public string? LlmType { get; init; }
    public string? LlmSuggestedResolution { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record ResolveDisputeRequest
{
    [Required]
    public string ResolutionNotes { get; init; } = string.Empty;

    [Required]
    public string Status { get; init; } = string.Empty;
}

public record ResolveDisputeResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ResolvedAt { get; init; }
}
