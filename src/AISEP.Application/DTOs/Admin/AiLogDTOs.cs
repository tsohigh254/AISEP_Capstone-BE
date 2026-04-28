namespace AISEP.Application.DTOs.Admin;

public class AiLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INFO";
    public string Logger { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string Source { get; set; } = "api"; // "api" or "worker"
    public string? Raw { get; set; }            // populated only when JSON parse fails
}

public class AiLogFileInfoDto
{
    public string FileName { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public long SizeBytes { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
}

public class AiLogsResponseDto
{
    public List<AiLogEntry> Entries { get; set; } = new();
    public int TotalReturned { get; set; }
    public List<AiLogFileInfoDto> Sources { get; set; } = new();
}
