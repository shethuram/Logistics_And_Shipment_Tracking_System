using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class DisputeMappings
{
    public static RaiseDisputeResponse ToRaiseDisputeResponse(this Dispute d) => new()
    {
        Id = d.Id,
        Status = d.Status,
        CreatedAt = d.CreatedAt
    };

    public static DisputeAdminDto ToDisputeAdminDto(this Dispute d) => new()
    {
        Id = d.Id,
        ShipmentId = d.ShipmentId,
        OrderId = d.Shipment?.OrderId ?? string.Empty,
        RaisedBy = d.RaisedByUser?.FullName ?? string.Empty,
        ComplaintText = d.ComplaintText,
        LlmSummary = d.LlmSummary,
        LlmType = d.LlmType,
        LlmSuggestedResolution = d.LlmSuggestedResolution,
        Status = d.Status,
        CreatedAt = d.CreatedAt
    };

    public static ResolveDisputeResponse ToResolveDisputeResponse(this Dispute d) => new()
    {
        Id = d.Id,
        Status = d.Status,
        ResolvedAt = d.ResolvedAt ?? DateTime.UtcNow
    };
}
