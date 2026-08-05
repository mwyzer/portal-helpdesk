using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Application.Options;
using Microsoft.Extensions.Options;

namespace AIHelpdesk.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly HttpClient _http;
    private readonly AIOptions _options;

    public AIService(HttpClient http, IOptions<AIOptions> options)
    {
        _http = http;
        _options = options.Value;
        // Deliberately NOT validated here: this constructor runs whenever ASP.NET Core builds
        // a controller that merely depends on IAIService/IRecruitmentAIService/IChatService --
        // e.g. CandidatesController needs IRecruitmentAIService for its ai-summarize action,
        // but every other action on that controller (plain candidate CRUD) has nothing to do
        // with AI. Throwing eagerly here previously meant the ENTIRE controller failed to
        // construct -- not just the AI endpoints -- whenever AI:ApiKey wasn't configured
        // (e.g. this local dev environment). Every caller already wraps _ai calls in a
        // try/catch with a graceful fallback (ChatService, TicketService, RecruitmentAIService),
        // so the check is deferred to actual use, where it's caught exactly like any other
        // AI-unavailable failure instead of crashing unrelated, non-AI functionality.
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _http.BaseAddress = new Uri(_options.Endpoint);
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("AI:ApiKey not configured");
    }

    public async Task<IReadOnlyList<double>> GenerateEmbeddingAsync(string text)
    {
        EnsureConfigured();
        var body = new { input = text, model = _options.EmbeddingModel };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("embeddings", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var embedding = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
        var result = new List<double>();
        foreach (var val in embedding.EnumerateArray())
            result.Add(val.GetDouble());
        return result;
    }

    public async Task<string> GenerateChatResponseAsync(string systemPrompt, string userMessage, List<(string Role, string Content)> history, Action<string>? onToken = null)
    {
        EnsureConfigured();
        var messages = new List<object>();
        messages.Add(new { role = "system", content = systemPrompt });

        foreach (var (role, content) in history)
            messages.Add(new { role, content });

        messages.Add(new { role = "user", content = userMessage });

        var body = new
        {
            model = _options.ChatModel,
            messages,
            stream = onToken != null,
            temperature = 0.3
        };

        var bodyJson = JsonSerializer.Serialize(body);
        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        if (onToken != null)
        {
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var fullResponse = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;
                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                try
                {
                    using var chunkDoc = JsonDocument.Parse(data);
                    var choices = chunkDoc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentEl))
                        {
                            var token = contentEl.GetString();
                            if (!string.IsNullOrEmpty(token))
                            {
                                fullResponse.Append(token);
                                onToken(token);
                            }
                        }
                    }
                }
                catch { /* skip malformed chunks */ }
            }
            return fullResponse.ToString();
        }
        else
        {
            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }
    }

    public int EstimateTokenCount(string text)
    {
        // Rough estimate: ~4 chars per token for English text
        return text.Length / 4;
    }
}
