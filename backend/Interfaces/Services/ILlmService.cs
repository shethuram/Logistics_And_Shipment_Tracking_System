using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface ILlmService
{
    Task<(string Summary, DisputeLlmType Type, string SuggestedResolution)> AnalyzeDisputeAsync(string complaintText);
    Task<(bool RiskFlag, RiskSeverity RiskSeverity, string? RiskReason, TimeOnly? PreferredDeliveryAfter, string? DriverInstruction)> ParseDeliveryNoteAsync(string notes);
}
