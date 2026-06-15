using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class LlmServiceTests
{
    private readonly LlmService _service;

    public LlmServiceTests()
    {
        // Setup configuration with an invalid Ollama URL so it triggers fallbacks
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "LlmSettings:OllamaUrl", "http://localhost:9999" }, // Invalid port
            { "LlmSettings:Model", "qwen2.5" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<LlmService>>();

        _service = new LlmService(config, loggerMock.Object);
    }

    [Theory]
    [InlineData("leave at door please", true, RiskSeverity.HIGH, "unattended_delivery")]
    [InlineData("leave with security if not home", true, RiskSeverity.HIGH, "unattended_delivery")]
    [InlineData("leave outside on front porch", true, RiskSeverity.HIGH, "unattended_delivery")]
    [InlineData("please drop at mailbox", true, RiskSeverity.HIGH, "unattended_delivery")]
    [InlineData("deliver carefully, thanks", false, RiskSeverity.NONE, null)]
    public async Task ParseDeliveryNoteAsync_DetectsCorrectRisks(
        string notes, bool expectedRisk, RiskSeverity expectedSeverity, string? expectedReason)
    {
        // Act
        var result = await _service.ParseDeliveryNoteAsync(notes);

        // Assert
        Assert.Equal(expectedRisk, result.RiskFlag);
        Assert.Equal(expectedSeverity, result.RiskSeverity);
        Assert.Equal(expectedReason, result.RiskReason);
    }

    [Theory]
    [InlineData("deliver after 5pm", 17, 0)]
    [InlineData("please deliver after 6 pm", 18, 0)]
    [InlineData("after 7:00 pm", 19, 0)]
    [InlineData("morning delivery please", null, null)]
    public async Task ParseDeliveryNoteAsync_DetectsCorrectPreferredTime(
        string notes, int? expectedHour, int? expectedMinute)
    {
        // Act
        var result = await _service.ParseDeliveryNoteAsync(notes);

        // Assert
        if (expectedHour.HasValue)
        {
            Assert.NotNull(result.PreferredDeliveryAfter);
            Assert.Equal(expectedHour.Value, result.PreferredDeliveryAfter.Value.Hour);
            Assert.Equal(expectedMinute!.Value, result.PreferredDeliveryAfter.Value.Minute);
        }
        else
        {
            Assert.Null(result.PreferredDeliveryAfter);
        }
    }

    [Theory]
    [InlineData("I never received the package that was marked delivered", DisputeLlmType.WRONG_ADDRESS)]
    [InlineData("The package was missing", DisputeLlmType.WRONG_ADDRESS)]
    [InlineData("The delivery was delayed by three hours", DisputeLlmType.LATE_DELIVERY)]
    [InlineData("My package was late", DisputeLlmType.LATE_DELIVERY)]
    [InlineData("The item was broken and damaged", DisputeLlmType.DAMAGED_PACKAGE)]
    [InlineData("The box was wet and torn", DisputeLlmType.DAMAGED_PACKAGE)]
    [InlineData("The driver was rude and unprofessional", DisputeLlmType.DRIVER_BEHAVIOUR)]
    [InlineData("unprofessional behavior from the driver", DisputeLlmType.DRIVER_BEHAVIOUR)]
    [InlineData("General issue with app booking", DisputeLlmType.DRIVER_BEHAVIOUR)]
    public async Task AnalyzeDisputeAsync_CategorizesCorrectDisputeType(string complaint, DisputeLlmType expectedType)
    {
        // Act
        var result = await _service.AnalyzeDisputeAsync(complaint);

        // Assert
        Assert.Equal(expectedType, result.Type);
        Assert.NotNull(result.Summary);
        Assert.NotNull(result.SuggestedResolution);
    }
}
