using System;

namespace Logistics.Api.DTOs;

public record ParseDeliveryNoteRequest
{
    public Guid? ShipmentId { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public record ParseDeliveryNoteResponse
{
    public bool RiskFlag { get; init; }
    public string RiskSeverity { get; init; } = string.Empty;
    public string? RiskReason { get; init; }
    public string? PreferredDeliveryAfter { get; init; }
    public string? DriverInstruction { get; init; }
}

public record SummariseDisputeRequest
{
    public Guid? DisputeId { get; init; }
    public string ComplaintText { get; init; } = string.Empty;
}

public record SummariseDisputeResponse
{
    public string Summary { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string SuggestedResolution { get; init; } = string.Empty;
}
