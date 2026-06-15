using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logistics.Api.Services;

public class LlmService : ILlmService
{
    private readonly IConfiguration _config;
    private readonly ILogger<LlmService> _logger;

    public LlmService(IConfiguration config, ILogger<LlmService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<(string Summary, DisputeLlmType Type, string SuggestedResolution)> AnalyzeDisputeAsync(string complaintText)
    {
        var ollamaUrl = _config["LlmSettings:OllamaUrl"];
        var model = _config["LlmSettings:Model"];

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var systemPrompt = "You are a dispute analysis AI. Analyze the customer complaint and output a JSON object containing:\n" +
                               "1. \"summary\": A brief 1-sentence summary.\n" +
                               "2. \"type\": Must be exactly one of: \"WRONG_ADDRESS\", \"LATE_DELIVERY\", \"DAMAGED_PACKAGE\", \"DRIVER_BEHAVIOUR\".\n" +
                               "3. \"suggested_resolution\": A brief suggested resolution step.\n" +
                               "Return ONLY the raw JSON.";

            var requestPayload = new
            {
                model = model,
                prompt = $"{systemPrompt}\n\nComplaint: \"{complaintText}\"",
                stream = false,
                format = "json"
            };

            var response = await client.PostAsJsonAsync($"{ollamaUrl}/api/generate", requestPayload);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadFromJsonAsync<JsonObject>();
                var responseText = responseJson?["response"]?.ToString();

                if (!string.IsNullOrEmpty(responseText))
                {
                    using var parsedDoc = JsonDocument.Parse(responseText);
                    var root = parsedDoc.RootElement;

                    var summary = root.GetProperty("summary").GetString() ?? string.Empty;
                    var typeStr = root.GetProperty("type").GetString() ?? string.Empty;
                    var suggestedResolution = root.GetProperty("suggested_resolution").GetString() ?? string.Empty;

                    if (!Enum.TryParse<DisputeLlmType>(typeStr, out var parsedType))
                    {
                        parsedType = DetermineFallbackType(complaintText);
                    }

                    if (!string.IsNullOrEmpty(summary) && !string.IsNullOrEmpty(suggestedResolution))
                    {
                        return (summary, parsedType, suggestedResolution);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama analysis failed. Falling back to rule-based parser.");
        }

        return PerformRuleBasedFallback(complaintText);
    }

    private static (string Summary, DisputeLlmType Type, string SuggestedResolution) PerformRuleBasedFallback(string complaintText)
    {
        var normalized = complaintText.ToLowerInvariant();
        var type = DetermineFallbackType(complaintText);
        string summary;
        string suggestedResolution;

        switch (type)
        {
            case DisputeLlmType.WRONG_ADDRESS:
                summary = "Customer claims shipment was marked as delivered but not received.";
                suggestedResolution = "Verify driver GPS ping history coordinates at the time of delivery confirmation.";
                break;
            case DisputeLlmType.LATE_DELIVERY:
                summary = "Customer complains about late delivery of the package.";
                suggestedResolution = "Analyze status change timestamps against preferred delivery window.";
                break;
            case DisputeLlmType.DAMAGED_PACKAGE:
                summary = "Customer reports the package was delivered in a damaged condition.";
                suggestedResolution = "Request photo proof of damage and contact the assigned driver for explanation.";
                break;
            case DisputeLlmType.DRIVER_BEHAVIOUR:
            default:
                if (normalized.Contains("driver") || normalized.Contains("rude") || normalized.Contains("behavior") || normalized.Contains("abuse"))
                {
                    summary = "Customer reports unprofessional or rude driver behavior.";
                    suggestedResolution = "Review driver performance log and contact driver for disciplinary feedback.";
                }
                else
                {
                    summary = "Customer has raised a general service dispute.";
                    suggestedResolution = "Contact customer and driver to gather additional details.";
                }
                break;
        }

        return (summary, type, suggestedResolution);
    }

    private static DisputeLlmType DetermineFallbackType(string complaintText)
    {
        var normalized = complaintText.ToLowerInvariant();
        if (normalized.Contains("delivered") || normalized.Contains("not received") || normalized.Contains("didn't receive") || normalized.Contains("missing"))
        {
            return DisputeLlmType.WRONG_ADDRESS;
        }
        if (normalized.Contains("late") || normalized.Contains("delay") || normalized.Contains("delayed") || normalized.Contains("time"))
        {
            return DisputeLlmType.LATE_DELIVERY;
        }
        if (normalized.Contains("damage") || normalized.Contains("broken") || normalized.Contains("torn") || normalized.Contains("wet"))
        {
            return DisputeLlmType.DAMAGED_PACKAGE;
        }
        return DisputeLlmType.DRIVER_BEHAVIOUR;
    }

    public async Task<(bool RiskFlag, RiskSeverity RiskSeverity, string? RiskReason, TimeOnly? PreferredDeliveryAfter, string? DriverInstruction)> ParseDeliveryNoteAsync(string notes)
    {
        var ollamaUrl = _config["LlmSettings:OllamaUrl"];
        var model = _config["LlmSettings:Model"];

        if (!string.IsNullOrEmpty(ollamaUrl) && !string.IsNullOrEmpty(model))
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var systemPrompt = "You are a delivery note parsing AI. Analyze the customer's delivery notes and output a JSON object containing:\n" +
                                   "1. \"risk\": boolean (true if the customer requests unattended delivery or leaving package in an insecure location like front door, mailbox, porch, yard, or with security without confirmation).\n" +
                                   "2. \"severity\": string (Must be exactly one of: \"HIGH\", \"LOW\", \"NONE\").\n" +
                                   "3. \"reason\": string (e.g. \"unattended_delivery\", or null/empty if no risk).\n" +
                                   "4. \"preferred_delivery_after\": string (extract any preferred delivery start time in \"HH:mm\" format, otherwise null/empty).\n" +
                                   "5. \"driver_instruction\": string (a clear, brief, direct instruction for the driver based on notes).\n" +
                                   "Return ONLY the raw JSON.";

                var requestPayload = new
                {
                    model = model,
                    prompt = $"{systemPrompt}\n\nNotes: \"{notes}\"",
                    stream = false,
                    format = "json"
                };

                var response = await client.PostAsJsonAsync($"{ollamaUrl}/api/generate", requestPayload);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadFromJsonAsync<JsonObject>();
                    var responseText = responseJson?["response"]?.ToString();

                    if (!string.IsNullOrEmpty(responseText))
                    {
                        using var parsedDoc = JsonDocument.Parse(responseText);
                        var root = parsedDoc.RootElement;

                        var risk = root.GetProperty("risk").GetBoolean();
                        var severityStr = root.GetProperty("severity").GetString() ?? "NONE";
                        var reason = root.GetProperty("reason").GetString();
                        var prefAfterStr = root.GetProperty("preferred_delivery_after").GetString();
                        var driverInstruction = root.GetProperty("driver_instruction").GetString();

                        if (!Enum.TryParse<RiskSeverity>(severityStr, out var severity))
                        {
                            severity = risk ? RiskSeverity.HIGH : RiskSeverity.NONE;
                        }

                        TimeOnly? prefTime = null;
                        if (!string.IsNullOrEmpty(prefAfterStr) && TimeOnly.TryParse(prefAfterStr, out var parsedTime))
                        {
                            prefTime = parsedTime;
                        }

                        return (risk, severity, reason, prefTime, driverInstruction);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ollama delivery note parsing failed. Falling back to rule-based parser.");
            }
        }

        return PerformDeliveryNoteFallback(notes);
    }

    private static (bool RiskFlag, RiskSeverity RiskSeverity, string? RiskReason, TimeOnly? PreferredDeliveryAfter, string? DriverInstruction) PerformDeliveryNoteFallback(string notes)
    {
        var normalized = notes.ToLowerInvariant();
        bool risk = false;
        var severity = RiskSeverity.NONE;
        string? reason = null;
        TimeOnly? preferredTime = null;
        string driverInstruction = notes;

        if (normalized.Contains("leave at door") || normalized.Contains("leave with security") || 
            normalized.Contains("leave outside") || normalized.Contains("not home") || 
            normalized.Contains("unattended") || normalized.Contains("front porch") || 
            normalized.Contains("porch") || normalized.Contains("mailbox") || 
            normalized.Contains("gate") || normalized.Contains("backyard") || 
            normalized.Contains("leave it with") || normalized.Contains("if not home"))
        {
            risk = true;
            severity = RiskSeverity.HIGH;
            reason = "unattended_delivery";
            driverInstruction = $"Customer requested unattended delivery or security handoff. Verify safety/rules: {notes}";
        }

        if (normalized.Contains("after 5pm") || normalized.Contains("after 5 pm") || 
            normalized.Contains("after 17") || normalized.Contains("after 5:00") || 
            normalized.Contains("after 5"))
        {
            preferredTime = new TimeOnly(17, 0);
        }
        else if (normalized.Contains("after 6pm") || normalized.Contains("after 6 pm") || 
                 normalized.Contains("after 18") || normalized.Contains("after 6:00") || 
                 normalized.Contains("after 6"))
        {
            preferredTime = new TimeOnly(18, 0);
        }
        else if (normalized.Contains("after 7pm") || normalized.Contains("after 7 pm") || 
                 normalized.Contains("after 19") || normalized.Contains("after 7:00") || 
                 normalized.Contains("after 7"))
        {
            preferredTime = new TimeOnly(19, 0);
        }

        return (risk, severity, reason, preferredTime, driverInstruction);
    }
}
