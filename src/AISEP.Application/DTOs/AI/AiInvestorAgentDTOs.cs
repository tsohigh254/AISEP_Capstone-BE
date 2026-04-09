using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISEP.Application.DTOs.AI;

// ═══════════════════════════════════════════════════════════════
//  FE → .NET  request DTOs  (camelCase by default)
// ═══════════════════════════════════════════════════════════════

/// <summary>Request body for investor-agent chat (stream and non-stream).</summary>
public class InvestorAgentChatRequest
{
    /// <summary>The investor's natural-language query.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Optional conversation thread ID.  Pass the same value to continue a thread;
    /// omit or send null to start a new conversation.
    /// </summary>
    public string? ThreadId { get; set; }
}

/// <summary>Request body for one-shot research (no thread memory).</summary>
public class InvestorAgentResearchRequest
{
    /// <summary>The research query.</summary>
    public string Query { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
//  .NET → Python  wire DTOs  (snake_case)
// ═══════════════════════════════════════════════════════════════

/// <summary>Payload sent to Python /api/v1/investor-agent/chat or /chat/stream.</summary>
public class PythonAgentChatRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }
}

/// <summary>Payload sent to Python /api/v1/investor-agent/research.</summary>
public class PythonAgentResearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
//  Python → .NET  response DTOs  (snake_case)
// ═══════════════════════════════════════════════════════════════

/// <summary>Reference item returned by the agent.</summary>
public class PythonAgentReference
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("source_domain")]
    public string? SourceDomain { get; set; }
}

/// <summary>Grounding quality summary from the agent.</summary>
public class PythonAgentGroundingSummary
{
    [JsonPropertyName("verified_claim_count")]
    public int VerifiedClaimCount { get; set; }

    [JsonPropertyName("weakly_supported_claim_count")]
    public int WeaklySupportedClaimCount { get; set; }

    [JsonPropertyName("conflicting_claim_count")]
    public int ConflictingClaimCount { get; set; }

    [JsonPropertyName("unsupported_claim_count")]
    public int UnsupportedClaimCount { get; set; }

    [JsonPropertyName("reference_count")]
    public int ReferenceCount { get; set; }

    [JsonPropertyName("coverage_status")]
    public string? CoverageStatus { get; set; }
}

/// <summary>
/// Full response from Python /api/v1/investor-agent/research.
/// The /chat response adds resolved_query and fallback_triggered.
/// </summary>
public class PythonAgentResearchResponse
{
    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("final_answer")]
    public string? FinalAnswer { get; set; }

    [JsonPropertyName("references")]
    public List<PythonAgentReference>? References { get; set; }

    [JsonPropertyName("caveats")]
    public List<string>? Caveats { get; set; }

    [JsonPropertyName("writer_notes")]
    public List<string>? WriterNotes { get; set; }

    [JsonPropertyName("processing_warnings")]
    public List<string>? ProcessingWarnings { get; set; }

    [JsonPropertyName("grounding_summary")]
    public PythonAgentGroundingSummary? GroundingSummary { get; set; }
}

/// <summary>
/// Full response from Python /api/v1/investor-agent/chat (non-stream).
/// Extends research response with chat-specific fields.
/// </summary>
public class PythonAgentChatResponse : PythonAgentResearchResponse
{
    [JsonPropertyName("resolved_query")]
    public string? ResolvedQuery { get; set; }

    [JsonPropertyName("fallback_triggered")]
    public bool FallbackTriggered { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  SSE event DTOs  (deserialized on-the-fly during streaming)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Generic SSE event envelope. The "type" field determines which
/// properties are populated.  Used only internally by the streaming proxy.
/// </summary>
public class SseEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Used by answer_chunk, final_answer, error events.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Used by progress events — the current graph node name.</summary>
    [JsonPropertyName("node")]
    public string? Node { get; set; }

    /// <summary>Used by final_metadata event.</summary>
    [JsonPropertyName("references")]
    public List<PythonAgentReference>? References { get; set; }

    /// <summary>Used by final_metadata event.</summary>
    [JsonPropertyName("caveats")]
    public List<string>? Caveats { get; set; }

    /// <summary>Used by final_metadata event.</summary>
    [JsonPropertyName("writer_notes")]
    public List<string>? WriterNotes { get; set; }

    /// <summary>Used by final_metadata event.</summary>
    [JsonPropertyName("processing_warnings")]
    public List<string>? ProcessingWarnings { get; set; }

    /// <summary>Used by final_metadata event.</summary>
    [JsonPropertyName("grounding_summary")]
    public PythonAgentGroundingSummary? GroundingSummary { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  FE-facing response DTOs  (camelCase — default ASP.NET serializer)
// ═══════════════════════════════════════════════════════════════

/// <summary>Reference item exposed to FE.</summary>
public class AgentReferenceResult
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? SourceDomain { get; set; }
}

/// <summary>Grounding summary exposed to FE.</summary>
public class AgentGroundingSummaryResult
{
    public int VerifiedClaimCount { get; set; }
    public int WeaklySupportedClaimCount { get; set; }
    public int ConflictingClaimCount { get; set; }
    public int UnsupportedClaimCount { get; set; }
    public int ReferenceCount { get; set; }
    public string? CoverageStatus { get; set; }
}

/// <summary>
/// FE-facing response for non-stream chat and research endpoints.
/// </summary>
public class InvestorAgentChatResult
{
    public string? Intent { get; set; }
    public string? FinalAnswer { get; set; }
    public List<AgentReferenceResult>? References { get; set; }
    public List<string>? Caveats { get; set; }
    public List<string>? WriterNotes { get; set; }
    public List<string>? ProcessingWarnings { get; set; }
    public AgentGroundingSummaryResult? GroundingSummary { get; set; }

    /// <summary>Only present on chat responses, not research.</summary>
    public string? ResolvedQuery { get; set; }

    /// <summary>Only present on chat responses, not research.</summary>
    public bool? FallbackTriggered { get; set; }
}
