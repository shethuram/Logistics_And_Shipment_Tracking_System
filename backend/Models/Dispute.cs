namespace Logistics.Api.Models;

public class Dispute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public Guid RaisedBy { get; set; }

    public string ComplaintText { get; set; } = string.Empty;

    public string? LlmSummary { get; set; }
    public DisputeLlmType? LlmType { get; set; }
    public string? LlmSuggestedResolution { get; set; }

    public DisputeStatus Status { get; set; } = DisputeStatus.OPEN;

    public Guid? ResolvedBy { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public Shipment Shipment { get; set; } = null!;
    public User RaisedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
}
