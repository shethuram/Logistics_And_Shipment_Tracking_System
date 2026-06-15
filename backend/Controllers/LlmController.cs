using System;
using System.Threading.Tasks;
using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/llm")]
public class LlmController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly AppDbContext _db;

    public LlmController(ILlmService llmService, AppDbContext db)
    {
        _llmService = llmService;
        _db = db;
    }

    [HttpPost("parse-delivery-note")]
    public async Task<IActionResult> ParseDeliveryNote(ParseDeliveryNoteRequest request)
    {
        var parsed = await _llmService.ParseDeliveryNoteAsync(request.Notes);

        if (request.ShipmentId.HasValue)
        {
            var shipment = await _db.Shipments.FindAsync(request.ShipmentId.Value);
            if (shipment != null)
            {
                shipment.RiskFlag = parsed.RiskFlag;
                shipment.RiskSeverity = parsed.RiskSeverity;
                shipment.RiskReason = parsed.RiskReason;
                shipment.PreferredDeliveryAfter = parsed.PreferredDeliveryAfter;
                shipment.DriverInstruction = parsed.DriverInstruction;
                shipment.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }
        }

        var response = new ParseDeliveryNoteResponse
        {
            RiskFlag = parsed.RiskFlag,
            RiskSeverity = parsed.RiskSeverity.ToString(),
            RiskReason = parsed.RiskReason,
            PreferredDeliveryAfter = parsed.PreferredDeliveryAfter?.ToString("HH:mm"),
            DriverInstruction = parsed.DriverInstruction
        };

        return Ok(response);
    }

    [HttpPost("summarise-dispute")]
    public async Task<IActionResult> SummariseDispute(SummariseDisputeRequest request)
    {
        var parsed = await _llmService.AnalyzeDisputeAsync(request.ComplaintText);

        if (request.DisputeId.HasValue)
        {
            var dispute = await _db.Disputes.FindAsync(request.DisputeId.Value);
            if (dispute != null)
            {
                dispute.LlmSummary = parsed.Summary;
                dispute.LlmType = parsed.Type;
                dispute.LlmSuggestedResolution = parsed.SuggestedResolution;

                await _db.SaveChangesAsync();
            }
        }

        var response = new SummariseDisputeResponse
        {
            Summary = parsed.Summary,
            Type = parsed.Type.ToString(),
            SuggestedResolution = parsed.SuggestedResolution
        };

        return Ok(response);
    }
}
