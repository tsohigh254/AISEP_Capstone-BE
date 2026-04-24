using Microsoft.AspNetCore.Http;

namespace AISEP.Application.Interfaces;

public interface IGeminiService
{
    /// <summary>
    /// Generates a non-streaming response from Gemini.
    /// </summary>
    Task<string> GenerateContentAsync(string prompt, CancellationToken ct = default);

    /// <summary>
    /// Generates a streaming response from Gemini and writes it to the HTTP response
    /// in the format expected by the frontend.
    /// </summary>
    Task StreamGenerateContentAsync(string prompt, HttpResponse httpResponse, CancellationToken ct = default);
}
