using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logistics.Api.Services;

public class LlmService : ILlmService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LlmService> _logger;

    public LlmService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<LlmService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<(string Summary, DisputeLlmType Type, string SuggestedResolution)> AnalyzeDisputeAsync(string complaintText)
    {
        var systemPrompt = await ReadPromptFileAsync("DisputePrompt.txt");
        var responseText = await CallGroqAsync(systemPrompt, complaintText);

        using var parsedDoc = JsonDocument.Parse(responseText);
        var root = parsedDoc.RootElement;

        var summary = root.GetProperty("summary").GetString() ?? string.Empty;
        var typeStr = root.GetProperty("type").GetString() ?? string.Empty;
        var suggestedResolution = root.GetProperty("suggested_resolution").GetString() ?? string.Empty;

        if (!Enum.TryParse<DisputeLlmType>(typeStr, true, out var parsedType))
        {
            throw new JsonException($"Invalid dispute type returned by LLM: '{typeStr}'. Expected one of: WRONG_ADDRESS, LATE_DELIVERY, DAMAGED_PACKAGE, DRIVER_BEHAVIOUR.");
        }

        return (summary, parsedType, suggestedResolution);
    }

    public async Task<(bool RiskFlag, RiskSeverity RiskSeverity, string? RiskReason, TimeOnly? PreferredDeliveryAfter, string? DriverInstruction)> ParseDeliveryNoteAsync(string notes)
    {
        var systemPrompt = await ReadPromptFileAsync("DeliveryNotePrompt.txt");
        var responseText = await CallGroqAsync(systemPrompt, notes);

        using var parsedDoc = JsonDocument.Parse(responseText);
        var root = parsedDoc.RootElement;

        var risk = root.GetProperty("risk").GetBoolean();
        var severityStr = root.GetProperty("severity").GetString() ?? "NONE";
        var reason = root.TryGetProperty("reason", out var reasonProp) && reasonProp.ValueKind != JsonValueKind.Null 
            ? reasonProp.GetString() 
            : null;
        var prefAfterStr = root.TryGetProperty("preferred_delivery_after", out var prefProp) && prefProp.ValueKind != JsonValueKind.Null 
            ? prefProp.GetString() 
            : null;
        var driverInstruction = root.TryGetProperty("driver_instruction", out var instProp) && instProp.ValueKind != JsonValueKind.Null 
            ? instProp.GetString() 
            : null;

        if (!Enum.TryParse<RiskSeverity>(severityStr, true, out var severity))
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

    private async Task<string> CallGroqAsync(string systemPrompt, string userContent)
    {
        var url = _config["LlmSettings:Url"]
            ?? throw new InvalidOperationException("LlmSettings:Url is not configured.");
        var model = _config["LlmSettings:Model"]
            ?? throw new InvalidOperationException("LlmSettings:Model is not configured.");
        var apiKey = GetApiKey();

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
        client.Timeout = TimeSpan.FromSeconds(10);

        var requestPayload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            },
            response_format = new { type = "json_object" },
            temperature = 0.1
        };

        var response = await client.PostAsJsonAsync(url, requestPayload);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadFromJsonAsync<JsonObject>();
        var responseText = responseJson?["choices"]?[0]?["message"]?["content"]?.ToString();

        if (string.IsNullOrEmpty(responseText))
            throw new InvalidOperationException("Groq API returned empty response.");

        _logger.LogInformation("RAW LLM RESPONSE: {ResponseText}", responseText);

        return responseText;
    }

    private string GetApiKey()
    {
        var apiKey = _config["LlmSettings:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "LlmSettings:ApiKey is not configured. Add it to appsettings.Development.json.");
        return apiKey.Trim();
    }

    private static async Task<string> ReadPromptFileAsync(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", fileName);
        if (File.Exists(path))
        {
            return await File.ReadAllTextAsync(path);
        }

        path = Path.Combine(Directory.GetCurrentDirectory(), "Prompts", fileName);
        if (File.Exists(path))
        {
            return await File.ReadAllTextAsync(path);
        }

        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            path = Path.Combine(dir, "Prompts", fileName);
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
            
            path = Path.Combine(dir, "backend", "Prompts", fileName);
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }

            var parent = Directory.GetParent(dir);
            if (parent == null || parent.FullName == dir) break;
            dir = parent.FullName;
        }

        throw new FileNotFoundException($"Prompt file '{fileName}' could not be located.");
    }
}
