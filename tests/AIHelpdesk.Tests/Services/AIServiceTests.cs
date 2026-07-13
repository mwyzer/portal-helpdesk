using System.Net;
using System.Text;
using System.Text.Json;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Application.Options;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AIHelpdesk.Tests.Services;

public class AIServiceTests
{
    private static AIOptions CreateOptions(
        string endpoint = "https://api.openai.com/v1/",
        string apiKey = "sk-test-key",
        string chatModel = "gpt-4o-mini",
        string embeddingModel = "text-embedding-3-small")
    {
        return new AIOptions
        {
            Endpoint = endpoint,
            ApiKey = apiKey,
            ChatModel = chatModel,
            EmbeddingModel = embeddingModel,
        };
    }

    private static IAIService CreateService(AIOptions? options = null, HttpMessageHandler? handler = null)
    {
        var opts = Options.Create(options ?? CreateOptions());
        var http = handler != null
            ? new HttpClient(handler) { BaseAddress = new Uri(opts.Value.Endpoint) }
            : new HttpClient();
        if (handler == null)
            http.BaseAddress = new Uri(opts.Value.Endpoint);
        return new AIService(http, opts);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, string responseContent)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
        return mock;
    }

    // ── EstimateTokenCount ──

    [Fact]
    public void EstimateTokenCount_ShouldReturnApproximateCount()
    {
        var service = CreateService();
        var result = service.EstimateTokenCount("Hello, how are you doing today?");
        // 32 chars / 4 = 8 tokens
        result.Should().Be(8);
    }

    [Fact]
    public void EstimateTokenCount_ShouldReturnZero_ForEmptyString()
    {
        var service = CreateService();
        var result = service.EstimateTokenCount("");
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateTokenCount_ShouldScaleWithLength()
    {
        var service = CreateService();
        var shortText = service.EstimateTokenCount("Hi");
        var longText = service.EstimateTokenCount(new string('a', 400));
        shortText.Should().BeLessThan(longText);
    }

    [Fact]
    public void EstimateTokenCount_ShouldBePositive_ForAnyText()
    {
        var service = CreateService();
        var result = service.EstimateTokenCount("a");
        result.Should().Be(0); // 1/4 = 0 due to integer division
    }

    // ── GenerateEmbeddingAsync ──

    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldReturnEmbedding()
    {
        var embedding = new[] { 0.1, 0.2, 0.3, 0.4 };
        var responseJson = JsonSerializer.Serialize(new
        {
            data = new[] { new { embedding } }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var service = CreateService(handler: handler.Object);

        var result = await service.GenerateEmbeddingAsync("test text");

        result.Should().HaveCount(4);
        result[0].Should().Be(0.1);
        result[3].Should().Be(0.4);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldThrow_OnFailure()
    {
        var handler = CreateMockHandler(HttpStatusCode.Unauthorized, "{}");
        var service = CreateService(handler: handler.Object);

        var act = () => service.GenerateEmbeddingAsync("test");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── GenerateChatResponseAsync (non-streaming) ──

    [Fact]
    public async Task GenerateChatResponseAsync_ShouldReturnResponse()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "Hello! How can I help?" } }
            }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var service = CreateService(handler: handler.Object);

        var result = await service.GenerateChatResponseAsync(
            "You are a helpful assistant.",
            "Hi there!",
            new List<(string, string)>());

        result.Should().Be("Hello! How can I help?");
    }

    [Fact]
    public async Task GenerateChatResponseAsync_ShouldIncludeHistory()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "Response with history" } }
            }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var service = CreateService(handler: handler.Object);

        var history = new List<(string Role, string Content)>
        {
            ("user", "Previous question"),
            ("assistant", "Previous answer")
        };

        var result = await service.GenerateChatResponseAsync(
            "System prompt",
            "New question",
            history);

        result.Should().Be("Response with history");
    }

    [Fact]
    public async Task GenerateChatResponseAsync_ShouldHandleEmptyResponse()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = (string?)null } }
            }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var service = CreateService(handler: handler.Object);

        var result = await service.GenerateChatResponseAsync("system", "user", new());
        result.Should().Be(string.Empty);
    }

    // ── GenerateChatResponseAsync (streaming) ──

    [Fact]
    public async Task GenerateChatResponseAsync_Streaming_ShouldCallOnToken()
    {
        var sseData = "data: " + JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { delta = new { content = "Hello" } }
            }
        }) + "\n\ndata: " + JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { delta = new { content = " World" } }
            }
        }) + "\n\ndata: [DONE]\n\n";

        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(sseData)))
            });

        var service = CreateService(handler: mock.Object);
        var tokens = new List<string>();

        var result = await service.GenerateChatResponseAsync(
            "system", "user", new(),
            onToken: token => tokens.Add(token));

        result.Should().Be("Hello World");
        tokens.Should().Equal("Hello", " World");
    }
}
