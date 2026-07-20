using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class LlmServiceTests
{
    private readonly LlmService _service;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public LlmServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "LlmSettings:Url", "https://proxy.llm-gateway.ready.presidio.com/v1/messages" },
            { "LlmSettings:Model", "claude-sonnet-4-6" },
            { "LlmSettings:ApiKey", "mock_anthropic_api_key_123" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<LlmService>>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                var contentStr = request.Content!.ReadAsStringAsync(token).Result;
                using var jsonDoc = JsonDocument.Parse(contentStr);
                var root = jsonDoc.RootElement;
                
                var messages = root.GetProperty("messages");
                var userContent = "";
                foreach (var msg in messages.EnumerateArray())
                {
                    if (msg.GetProperty("role").GetString() == "user")
                    {
                        userContent = msg.GetProperty("content").GetString() ?? "";
                        break;
                    }
                }
                
                string innerContentJson;
                
                if (contentStr.Contains("delivery note parsing AI") || contentStr.Contains("risk"))
                {
                    var normalized = userContent.ToLowerInvariant();
                    bool risk = false;
                    var severity = "NONE";
                    string? reason = null;
                    string? preferredTime = null;
                    string driverInstruction = userContent;

                    if (normalized.Contains("leave at door") || normalized.Contains("leave with security") || 
                        normalized.Contains("leave outside") || normalized.Contains("not home") || 
                        normalized.Contains("unattended") || normalized.Contains("front porch") || 
                        normalized.Contains("porch") || normalized.Contains("mailbox") || 
                        normalized.Contains("gate") || normalized.Contains("backyard") || 
                        normalized.Contains("leave it with") || normalized.Contains("if not home"))
                    {
                        risk = true;
                        severity = "HIGH";
                        reason = "unattended_delivery";
                    }

                    if (normalized.Contains("after 5pm") || normalized.Contains("after 5 pm") || 
                        normalized.Contains("after 17") || normalized.Contains("after 5:00") || 
                        normalized.Contains("after 5"))
                    {
                        preferredTime = "17:00";
                    }
                    else if (normalized.Contains("after 6pm") || normalized.Contains("after 6 pm") || 
                             normalized.Contains("after 18") || normalized.Contains("after 6:00") || 
                             normalized.Contains("after 6"))
                    {
                        preferredTime = "18:00";
                    }
                    else if (normalized.Contains("after 7pm") || normalized.Contains("after 7 pm") || 
                             normalized.Contains("after 19") || normalized.Contains("after 7:00") || 
                             normalized.Contains("after 7"))
                    {
                        preferredTime = "19:00";
                    }

                    innerContentJson = JsonSerializer.Serialize(new
                    {
                        risk = risk,
                        severity = severity,
                        reason = reason,
                        preferred_delivery_after = preferredTime,
                        driver_instruction = driverInstruction
                    });
                }
                else
                {
                    var normalized = userContent.ToLowerInvariant();
                    string typeStr = "DRIVER_BEHAVIOUR";
                    if (normalized.Contains("delivered") || normalized.Contains("not received") || normalized.Contains("didn't receive") || normalized.Contains("missing"))
                    {
                        typeStr = "WRONG_ADDRESS";
                    }
                    else if (normalized.Contains("late") || normalized.Contains("delay") || normalized.Contains("delayed") || normalized.Contains("time"))
                    {
                        typeStr = "LATE_DELIVERY";
                    }
                    else if (normalized.Contains("damage") || normalized.Contains("broken") || normalized.Contains("torn") || normalized.Contains("wet"))
                    {
                        typeStr = "DAMAGED_PACKAGE";
                    }

                    innerContentJson = JsonSerializer.Serialize(new
                    {
                        summary = "Mock summary of complaint",
                        type = typeStr,
                        suggested_resolution = "Mock resolution suggestion"
                    });
                }
                
                var outerResponse = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = innerContentJson
                        }
                    }
                };
                
                var responseMsg = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(outerResponse)
                };
                return responseMsg;
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _service = new LlmService(httpClientFactoryMock.Object, config, loggerMock.Object);
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
        var result = await _service.ParseDeliveryNoteAsync(notes);

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
        var result = await _service.ParseDeliveryNoteAsync(notes);

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
        var result = await _service.AnalyzeDisputeAsync(complaint);

        Assert.Equal(expectedType, result.Type);
        Assert.NotNull(result.Summary);
        Assert.NotNull(result.SuggestedResolution);
    }
}
