using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;

namespace AISEP.Infrastructure.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiService(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateContentAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            var isVertexAi = !string.IsNullOrEmpty(_options.ProjectId);
            var url = ConstructUrl(isVertexAi, false);
            
            _logger.LogInformation("Calling Gemini API ({Type}): {Url}", isVertexAi ? "Vertex AI" : "Google AI", url);

            var request = CreateGeminiRequest(prompt);
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            await ApplyAuthenticationAsync(httpRequest, isVertexAi, ct);
            httpRequest.Content = JsonContent.Create(request, options: JsonOpts);

            var response = await _httpClient.SendAsync(httpRequest, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return $"Error: Gemini API returned {response.StatusCode}. {errorBody}";
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, ct);
            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Gemini API");
            return "Error: Internal server error while communicating with AI.";
        }
    }

    public async Task StreamGenerateContentAsync(string prompt, HttpResponse httpResponse, CancellationToken ct = default)
    {
        var isVertexAi = !string.IsNullOrEmpty(_options.ProjectId);
        var url = ConstructUrl(isVertexAi, true);
        
        _logger.LogInformation("Calling Gemini Stream API ({Type}): {Url}", isVertexAi ? "Vertex AI" : "Google AI", url);

        var request = CreateGeminiRequest(prompt);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            await ApplyAuthenticationAsync(httpRequest, isVertexAi, ct);
            httpRequest.Content = JsonContent.Create(request, options: JsonOpts);

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini Stream API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                await WriteSseErrorAsync(httpResponse, $"Gemini API error: {response.StatusCode}", ct);
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cleanedLine = line.Trim();
                
                // Handle the SSE 'data: ' prefix
                if (cleanedLine.StartsWith("data: "))
                {
                    cleanedLine = cleanedLine.Substring(6).Trim();
                }

                if (cleanedLine == "[DONE]" || string.IsNullOrEmpty(cleanedLine)) continue;

                // Handle the legacy array format
                if (cleanedLine == "[" || cleanedLine == "]") continue;
                if (cleanedLine.StartsWith(",")) cleanedLine = cleanedLine.Substring(1).Trim();
                if (cleanedLine.EndsWith(",")) cleanedLine = cleanedLine.Substring(0, cleanedLine.Length - 1).Trim();

                if (string.IsNullOrEmpty(cleanedLine)) continue;

                try
                {
                    var chunk = JsonSerializer.Deserialize<GeminiResponse>(cleanedLine, JsonOpts);
                    var text = chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                    if (!string.IsNullOrEmpty(text))
                    {
                        var sseData = $"data: {{\"type\":\"answer_chunk\",\"content\":\"{EscapeJsonString(text)}\"}}\n\n";
                        await httpResponse.WriteAsync(sseData, ct);
                        await httpResponse.Body.FlushAsync(ct);
                    }
                }
                catch (JsonException)
                {
                    _logger.LogTrace("Failed to parse Gemini chunk: {Line}", cleanedLine);
                }
            }

            await httpResponse.WriteAsync("data: [DONE]\n\n", ct);
            await httpResponse.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Gemini streaming");
            await WriteSseErrorAsync(httpResponse, "Internal error during streaming.", ct);
        }
    }

    private string ConstructUrl(bool isVertexAi, bool isStream)
    {
        var method = isStream ? "streamGenerateContent" : "generateContent";
        
        if (isVertexAi)
        {
            var location = _options.Location;
            var projectId = _options.ProjectId;
            var model = _options.Model;
            // Vertex AI URL format
            return $"https://{location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{location}/publishers/google/models/{model}:{method}{(isStream ? "?alt=sse" : "")}";
        }
        else
        {
            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var model = _options.Model.Trim();
            return $"{baseUrl}/models/{model}:{method}{(isStream ? "?alt=sse" : "")}";
        }
    }

    private async Task ApplyAuthenticationAsync(HttpRequestMessage request, bool isVertexAi, CancellationToken ct)
    {
        if (isVertexAi)
        {
            var token = await GetAccessTokenAsync(ct);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var credential = await GoogleCredential.GetApplicationDefaultAsync();
        if (credential.IsCreateScopedRequired)
        {
            credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        }
        
        var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: ct);
        return token;
    }

    private GeminiRequest CreateGeminiRequest(string prompt)
    {
        // For v1 compatibility, we prepend the system instruction to the prompt
        var fullPrompt = "HƯỚNG DẪN HỆ THỐNG: Bạn là AISEP Gemini Assistant, một trợ lý trí tuệ nhân tạo chuyên sâu về đầu tư và startup của nền tảng AISEP. Hãy trả lời chuyên nghiệp, súc tích và hữu ích bằng tiếng Việt.\n\nCÂU HỎI NGƯỜI DÙNG: " + prompt;
        
        return new GeminiRequest
        {
            Contents = new List<GeminiContent>
            {
                new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = fullPrompt } }
                }
            }
        };
    }

    private static async Task WriteSseErrorAsync(HttpResponse response, string message, CancellationToken ct)
    {
        try
        {
            var errorEvent = $"data: {{\"type\":\"error\",\"content\":\"{EscapeJsonString(message)}\"}}\n\n";
            await response.WriteAsync(errorEvent, ct);
            await response.WriteAsync("data: [DONE]\n\n", ct);
            await response.Body.FlushAsync(ct);
        }
        catch { /* ignore */ }
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    // --- Gemini DTOs ---
    private class GeminiRequest
    {
        [JsonPropertyName("system_instruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
