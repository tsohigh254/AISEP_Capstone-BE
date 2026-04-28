using System.Text.Json;
using AISEP.Application.DTOs.Admin;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class AiLogsService : IAiLogsService
{
    private const int MaxTail = 1000;
    private const int DefaultTail = 200;

    private readonly string _logDirectory;
    private readonly ILogger<AiLogsService> _logger;

    private static readonly (string FileName, string Source)[] LogSources = new[]
    {
        ("aisep.log", "api"),
        ("aisep-worker.log", "worker"),
    };

    public AiLogsService(IConfiguration config, ILogger<AiLogsService> logger)
    {
        _logger = logger;
        _logDirectory = config["AiLogs:Directory"] ?? "/ai-logs";
    }

    public async Task<ApiResponse<AiLogsResponseDto>> GetLogsAsync(
        int tail,
        string? level,
        string? search,
        string? correlationId,
        CancellationToken ct)
    {
        var take = Math.Clamp(tail <= 0 ? DefaultTail : tail, 1, MaxTail);
        var levelFilter = string.IsNullOrWhiteSpace(level) ? null : level.Trim().ToUpperInvariant();
        var searchFilter = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var corrFilter = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();

        var sources = new List<AiLogFileInfoDto>();
        var collected = new List<AiLogEntry>();

        foreach (var (fileName, source) in LogSources)
        {
            ct.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(_logDirectory, fileName);
            var info = new AiLogFileInfoDto { FileName = fileName };

            if (!File.Exists(fullPath))
            {
                sources.Add(info);
                continue;
            }

            var fi = new FileInfo(fullPath);
            info.Exists = true;
            info.SizeBytes = fi.Length;
            info.LastModifiedUtc = fi.LastWriteTimeUtc;
            sources.Add(info);

            // Sliding window: keep only the last `take` matching entries per file.
            var window = new LinkedList<AiLogEntry>();
            try
            {
                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = await reader.ReadLineAsync(ct)) != null)
                {
                    var entry = ParseLine(line, source);
                    if (!Matches(entry, levelFilter, searchFilter, corrFilter)) continue;
                    window.AddLast(entry);
                    if (window.Count > take) window.RemoveFirst();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed reading AI log file {File}", fullPath);
                continue;
            }

            collected.AddRange(window);
        }

        // Merge sources, sort newest first, cap to `take`.
        var entries = collected
            .OrderByDescending(e => e.Timestamp)
            .Take(take)
            .ToList();

        return ApiResponse<AiLogsResponseDto>.SuccessResponse(new AiLogsResponseDto
        {
            Entries = entries,
            TotalReturned = entries.Count,
            Sources = sources,
        });
    }

    private static AiLogEntry ParseLine(string line, string source)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new AiLogEntry { Source = source, Raw = line, Message = string.Empty };
        }

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var entry = new AiLogEntry { Source = source };

            if (root.TryGetProperty("ts", out var ts) && ts.ValueKind == JsonValueKind.String
                && DateTime.TryParse(ts.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var t))
            {
                entry.Timestamp = t.ToUniversalTime();
            }
            else
            {
                entry.Timestamp = DateTime.UtcNow;
            }

            if (root.TryGetProperty("level", out var lv)) entry.Level = lv.GetString() ?? "INFO";
            if (root.TryGetProperty("logger", out var lg)) entry.Logger = lg.GetString() ?? string.Empty;
            if (root.TryGetProperty("message", out var ms)) entry.Message = ms.GetString() ?? string.Empty;
            if (root.TryGetProperty("correlation_id", out var cid)) entry.CorrelationId = cid.GetString();

            return entry;
        }
        catch (JsonException)
        {
            // Non-JSON line (e.g. uvicorn raw stdout that slipped past root logger).
            return new AiLogEntry
            {
                Source = source,
                Timestamp = DateTime.UtcNow,
                Level = "INFO",
                Message = line,
                Raw = line,
            };
        }
    }

    private static bool Matches(AiLogEntry e, string? level, string? search, string? correlationId)
    {
        if (level is not null && !string.Equals(e.Level, level, StringComparison.OrdinalIgnoreCase))
            return false;
        if (correlationId is not null && !string.Equals(e.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase))
            return false;
        if (search is not null
            && (e.Message?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) < 0
            && (e.Logger?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
            return false;
        return true;
    }
}
