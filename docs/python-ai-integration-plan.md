# Python AI Service — .NET Integration Plan

> **Status**: Planning only — no code implemented yet  
> **Author**: Senior .NET Backend Engineer + Solution Architect  
> **Source of truth**:
>
> - Repo .NET: `c:\Users\LENOVO\Desktop\AISEP_Capstone-BE`
> - Python handoff: `docs/integration_handoff/`  
>   **Date created**: 2026-04-07

---

## Table of Contents

1. [Codebase Summary (.NET side)](#1-codebase-summary-net-side)
2. [Python Handoff Findings](#2-python-handoff-findings)
3. [Recommended Integration Architecture](#3-recommended-integration-architecture)
4. [Feature-by-Feature Mapping](#4-feature-by-feature-mapping)
5. [API / DTO Mapping](#5-api--dto-mapping)
6. [Config / Security Plan](#6-config--security-plan)
7. [Files to Change](#7-files-to-change)
8. [Code Snippets (Reference Implementation)](#8-code-snippets-reference-implementation)
9. [Risks / Gaps](#9-risks--gaps)
10. [Next Steps](#10-next-steps)

---

## 1. Codebase Summary (.NET side)

### Architecture

Clean-ish layered architecture with 4 projects:

| Project                | Role                                                 |
| ---------------------- | ---------------------------------------------------- |
| `AISEP.Domain`         | Entities, Enums, Domain interfaces                   |
| `AISEP.Application`    | Interfaces (IXxxService), DTOs, Extensions           |
| `AISEP.Infrastructure` | EF Core context, concrete service implementations    |
| `AISEP.WebAPI`         | Controllers, Hubs (SignalR), Middlewares, Program.cs |

### Controllers (existing)

`StartupsController`, `InvestorsController`, `DocumentsController`, `AuthController`, `AdminController`, `AdvisorsController`, `BlockchainController`, `ConnectionsController`, `ConversationsController`, `MessagesController`, `MentorshipsController`, `ModerationController`, `NotificationsController`, `PaymentController`, `PermissionsController`, `PortfolioController`, `RegistrationController`, `RolesController`, `UsersController`

**No AI/evaluation/recommendation/chatbot controller exists yet.**

### Application Services (existing)

All registered as `AddScoped<IXxx, XxxService>()` in `Program.cs`.  
`StartupService`, `InvestorService`, `DocumentService`, `AuthService`, `AdvisorService`, `AuditService`, `EmailService`, `MentorshipService`, `ConnectionsService`, `ChatService`, `NotificationService`, `ModerationService`, `RegistrationApprovalService`, `BlockchainProofService`, `AdminService`, `CloudinaryService`, `PaymentService`

**No PythonAiClient or typed HttpClient exists yet.**

### HttpClient pattern already used

`builder.Services.AddHttpClient<IEmailService, EmailService>()` — typed `HttpClient` injection is already an established pattern in this codebase. Reuse this pattern for Python AI.

### DTOs / Entities relevant to AI integration

| .NET entity/DTO               | Relevant fields                                                                                                                                                     | Maps to Python                              |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| `Startup` entity              | `CompanyName`, `Stage`, `IndustryID`, `Location`, `MarketScope`, `ProductStatus`, `ProblemStatement`, `SolutionSummary`, `IsVisible`, `ProfileStatus`, `StartupTag` | `ReindexStartupRequest` fields              |
| `StartupMeDto` / `StartupDto` | All above + `UpdatedAt`                                                                                                                                             | Source data for reindex                     |
| `Document` entity             | `DocumentID`, `FileURL`, `DocumentType`, `AnalysisStatus`, `IsAnalyzed`                                                                                             | `DocumentInputSchema` for evaluation submit |
| `Investor` entity             | `InvestorID`, `InvestmentThesis`, `Location`, `Country`, `ProfileStatus`, `InvestorTag`                                                                             | `ReindexInvestorRequest` base fields        |
| `InvestorPreferences`         | `PreferredStages`, `PreferredIndustries`, `PreferredGeographies`, `MinPotentialScore`, `MinInvestmentSize`, `MaxInvestmentSize`                                     | Critical reindex preference fields          |

### Background jobs / hosted services

None currently. No `IHostedService`, no `BackgroundService`, no Hangfire, no MassTransit.  
**Polling for AI evaluation must be client-driven (frontend polling) or implemented as a new .NET `BackgroundService`.**

### Config pattern

- `.env` file loaded via `DotNetEnv` → merges into `builder.Configuration`
- `builder.Configuration.AddEnvironmentVariables()` — double-underscore for nesting
- Pattern: `builder.Services.Configure<TOptions>(builder.Configuration.GetSection("SectionName"))` + inject via `IOptions<TOptions>`
- Currently configured: `Jwt`, `Email`, `CloudinaryOptions`, `PaymentOptions`, `Blockchain`, `Cors`

### Existing `appsettings.json` — no `PythonAi` section exists yet

---

## 2. Python Handoff Findings

### Service overview

| Module                   | Mode                | Maturity                                                                         |
| ------------------------ | ------------------- | -------------------------------------------------------------------------------- |
| Evaluation               | async-submit + poll | Partially ready — async orchestration works, some LLM/schema failure paths exist |
| Recommendation           | sync on-demand      | Partially ready — works but file-backed JSON storage (not enterprise-grade)      |
| Investor Agent / Chatbot | sync + SSE stream   | Experimental — in-process memory, no auth, upstream Tavily/Gemini dependency     |

### Python endpoints (confirmed from code, not assumed)

**Evaluation** (prefix `/api/v1/evaluations`):

- `GET /api/v1/evaluations/history?startup_id={id}` — list history for a startup
- `POST /api/v1/evaluations/` — async submit; returns `{ evaluation_run_id, status, message }`
- `GET /api/v1/evaluations/{id}` — poll status + document summary
- `GET /api/v1/evaluations/{id}/report` — canonical report (202 if not ready, 409 if failed, 500 if parse error)

**Recommendation** (no global prefix; absolute paths in router):

- `POST /internal/recommendations/reindex/startup/{startup_id}` — upsert startup doc, `X-Internal-Token` auth
- `POST /internal/recommendations/reindex/investor/{investor_id}` — upsert investor doc, `X-Internal-Token` auth
- `GET /api/v1/recommendations/startups?investor_id={id}&top_n={1..10}` — ranked list for investor
- `GET /api/v1/recommendations/startups/{startup_id}/explanation?investor_id={id}` — match explanation

**Investor Agent** (prefix `/api/v1/investor-agent`):

- `POST /api/v1/investor-agent/research` — one-shot research, sync, long-running
- `POST /api/v1/investor-agent/chat` — multi-turn non-stream chat with `thread_id`
- `POST /api/v1/investor-agent/chat/stream` — multi-turn SSE stream with `thread_id`

**Health**: `GET /health` → `{"status": "ok"}`

### Key Python schema facts

- **Evaluation submit**: `startup_id: str`, `documents: [{document_id, document_type, file_url_or_path}]`
- **Evaluation report**: `CanonicalEvaluationResult` with `criteria_results`, `overall_result`, `narrative`, `processing_warnings`
- **Recommendation result**: `RecommendationMatchResult` with `final_match_score`, `match_band` (LOW/MEDIUM/HIGH/VERY_HIGH), `match_reasons`, `positive_reasons`, `caution_reasons`
- **Chat request**: `{ query: str, thread_id: str }`
- **SSE event types**: `progress`, `answer_chunk`, `final_answer`, `final_metadata`, `error`, `[DONE]`
- **Python `startup_id`**: passed as `str` — map from .NET `int StartupID` using `.ToString()`
- **Python `document_type`**: `"pitch_deck"` or `"business_plan"` — map from .NET `DocumentType` enum

### Async infrastructure required on Python side

- Celery + Redis must be running for evaluation to work
- `.NET` is a **consumer only** — does not manage Celery workers
- Status source of truth: Python DB `EvaluationRun.status`, not Celery result backend

### Auth situation (from handoff)

- Recommendation internal reindex: `X-Internal-Token` header against `AISEP_INTERNAL_TOKEN` env var
- Evaluation + public recommendation + investor agent: **no auth observed in current Python code**
- Recommended: run Python AI service on internal network; .NET acts as auth proxy to frontend

---

## 3. Recommended Integration Architecture

### Answer to the 8 mandatory questions

**Q1. How should .NET call Python service via HTTP?**  
Use typed `HttpClient` registered in DI (`AddHttpClient<IPythonAiClient, PythonAiHttpClient>()`). Configure `BaseAddress` from `appsettings.json` `PythonAi:BaseUrl`. Add default timeout (30 s for sync calls, 120 s for streaming). For SSE streaming, use `HttpCompletionOption.ResponseHeadersRead` + `StreamReader`.

**Q2. How should AI Evaluation submit + poll work?**

- .NET `POST /api/ai/evaluation/submit` → calls Python `POST /api/v1/evaluations/` → receives `evaluation_run_id` → persists record in .NET DB (`AiEvaluationRun` table: `evaluation_run_id`, `startup_id`, `submitted_at`, `status`, `failure_reason`)
- Frontend polls `.NET GET /api/ai/evaluation/{runId}/status` → .NET fetches from Python `GET /api/v1/evaluations/{id}` on-demand and updates local status
- Frontend fetches `.NET GET /api/ai/evaluation/{runId}/report` → .NET fetches from Python `GET /api/v1/evaluations/{id}/report` when status = `completed`
- No .NET background job needed for MVP — polling is frontend-driven through .NET proxy

**Q3. Should Recommendation be on-demand or cached?**  
**On-demand (fetch-through)**: .NET calls Python recommendation endpoints on each request. The Python service already computes on-demand. No cache needed at .NET layer for MVP. If latency is a concern in production, add `IMemoryCache` in `.NET` with a 60-second TTL per `investor_id`. Reindex must be triggered from .NET on every profile or AI evaluation update.

**Q4. How should Investor chatbot stream be proxied to frontend?**  
.NET `POST /api/ai/investor-agent/chat/stream` → reads `HttpResponseMessage` as stream → proxies SSE bytes to frontend via `Response.Body` with `Content-Type: text/event-stream`. Use `HttpCompletionOption.ResponseHeadersRead` on the outbound call and stream chunks as they arrive. This is a transparent proxy — .NET does not parse SSE content, just forwards it.

**Q5. Thread/memory contract — what does .NET pass?**

- `thread_id: string` — must be stable per frontend conversation session
- Recommended format: `"investor-{investorId}-{conversationUuid}"` — .NET generates on session start, frontend stores it
- Passed in every chat/stream request body
- NOT stored in .NET DB for MVP (in-process memory on Python side only)

**Q6. Cloudinary/signed URL for AI Evaluation — how?**  
Documents are already uploaded to Cloudinary by `DocumentService` and `FileURL` is stored in the `.NET` `Document` entity. When submitting evaluation:

- .NET reads `Document.FileURL` (already a public Cloudinary URL)
- Passes it as `file_url_or_path` to Python
- Python downloads the file from Cloudinary URL directly
- No signed URL generation needed for this flow — the existing public URL is sufficient
- **If the startup has private/raw Cloudinary files**, generate a signed URL via `CloudinaryService` (already has `SignedUrlExpirationMinutes` config) and pass that instead

**Q7. Minimum internal auth between .NET and Python?**

- Recommendation reindex endpoints: include `X-Internal-Token` header (from config `PythonAi:InternalToken`)
- Evaluation + public recommendation + chatbot: no auth required currently on Python side
- Minimum production hardening: place Python service on VPC-internal URL not accessible from public internet; .NET is the only caller
- Optional (post-MVP): add a static `X-Internal-Token` or API key to all Python calls

**Q8. Gaps to fix in .NET before integration is possible?**

1. Create `AiEvaluationRun` entity + EF Core migration (to persist evaluation job records)
2. Create `PythonAiOptions` settings class + populate `appsettings.json`/`.env`
3. Register typed `HttpClient` for Python AI service in `Program.cs`
4. Create `IPythonAiClient` interface + `PythonAiHttpClient` implementation
5. Create 3 new Application service interfaces: `IAiEvaluationService`, `IAiRecommendationService`, `IAiInvestorAgentService`
6. Create 3 new Infrastructure service implementations
7. Create new controllers: `AiEvaluationController`, `AiRecommendationController`, `AiInvestorAgentController`
8. Add reindex trigger hooks in `StartupService.UpdateStartupAsync()` and `InvestorService.UpdateProfileAsync()` / `UpdatePreferencesAsync()`

### Architecture diagram (text)

```
Frontend (React)
    │
    │ HTTP (JWT Bearer)
    ▼
.NET WebAPI (AISEP.WebAPI)
    ├── AiEvaluationController       ──────────────────────────────────────────┐
    ├── AiRecommendationController   ──────────────────────────────────────────┤
    ├── AiInvestorAgentController    ──── SSE proxy ──────────────────────────┤
    │       │                                                                   │
    │       ▼ (DI)                                                             │
    │   IAiEvaluationService (Infrastructure)                                  │
    │   IAiRecommendationService (Infrastructure)                              │
    │   IAiInvestorAgentService (Infrastructure)                               │
    │       │                                                                   │
    │       ▼ (DI)                                                             │
    │   IPythonAiClient (typed HttpClient)                                    │
    │       │                                                                   │
    │       │ HTTP (X-Internal-Token for reindex)                             │
    │       ▼                                                                   │
    │   Python FastAPI Service                                                  │
    │       ├── /api/v1/evaluations/*   (Celery async)                       │
    │       ├── /internal/recommendations/reindex/*   (sync)                 │
    │       ├── /api/v1/recommendations/*   (sync)                           │
    │       └── /api/v1/investor-agent/*   (sync + SSE)                      │
    │                                                                           │
    ▼ (DB)                                                                     │
PostgreSQL (AISEP)                                                             │
    └── AiEvaluationRuns table (new)                          SSE proxied ◄──┘
```

---

## 4. Feature-by-Feature Mapping

### 4.1 AI Evaluation

**Flow overview**: Submit → async processing on Python (Celery) → poll status → get report

**Frontend calls .NET**:

- `POST /api/ai/evaluation/submit` — startup submits documents for evaluation
- `GET /api/ai/evaluation/{runId}/status` — poll run status
- `GET /api/ai/evaluation/{runId}/report` — get final canonical report
- `GET /api/ai/evaluation/history?startupId={id}` — list evaluation history for a startup

**Who is authorized**:

- Submit/status/report: `StartupOnly` policy (own startup's documents)
- History: `StartupOnly` (own startup) OR `InvestorOnly` (read-only history of a startup being researched)

**.NET service layer** (`IAiEvaluationService`):

1. `SubmitEvaluationAsync(int userId, SubmitAiEvaluationRequest)` → calls Python + persists `AiEvaluationRun`
2. `GetEvaluationStatusAsync(int runId, int userId)` → calls Python `GET /api/v1/evaluations/{pythonRunId}` → updates local status
3. `GetEvaluationReportAsync(int runId, int userId)` → calls Python `GET /api/v1/evaluations/{pythonRunId}/report`
4. `GetEvaluationHistoryAsync(string startupId)` → calls Python `GET /api/v1/evaluations/history?startup_id={id}`

**Python endpoints called**:

1. `POST /api/v1/evaluations/` with `SubmitEvaluationRequest`
2. `GET /api/v1/evaluations/{pythonRunId}`
3. `GET /api/v1/evaluations/{pythonRunId}/report`
4. `GET /api/v1/evaluations/history?startup_id={id}`

**Status lifecycle in .NET DB** (new `AiEvaluationRun` table):

```
AiEvaluationRun {
    Id (int, PK)
    StartupId (int, FK → Startups.StartupID)
    PythonEvaluationRunId (int)           ← from Python response
    SubmittedAt (DateTime)
    Status (string)                        ← "queued" | "processing" | "completed" | "failed" | "retry"
    FailureReason (string?)
    OverallScore (decimal?)               ← populated when completed
    UpdatedAt (DateTime?)
}
```

**Document type mapping** (critical):

| .NET `DocumentType` enum | Python `document_type` string |
| ------------------------ | ----------------------------- |
| `Pitch_Deck` (0)         | `"pitch_deck"`                |
| `Bussiness_Plan` (1)     | `"business_plan"`             |

**Startup ID mapping**:

- .NET: `int StartupID` (e.g., `42`)
- Python: `string startup_id` (e.g., `"42"`)
- Conversion: `.ToString()` when building request, `int.Parse()` when reading response

**Document file URL**: use `Document.FileURL` from .NET DB as `file_url_or_path` (already a Cloudinary URL).

**Re-evaluation behavior**: Python automatically marks prior active runs as `failed` when a new one is submitted. .NET should reflect this by updating status of previously tracked runs on next poll or after submit.

---

### 4.2 AI Recommendation

**Flow overview**: Profile updated → trigger reindex → investor queries recommendations on-demand

**Frontend calls .NET**:

- `GET /api/ai/recommendations/startups?topN={n}` — investor gets ranked startup list (investor authenticated)
- `GET /api/ai/recommendations/startups/{startupId}/explanation` — investor gets match explanation

**No .NET endpoint needed** for reindex — it's triggered internally from `StartupService` and `InvestorService`.

**Trigger points in existing services** (call Python reindex internally):

1. `StartupService.UpdateStartupAsync()` → after DB save → fire-and-forget call to `/internal/recommendations/reindex/startup/{startupId}`
2. `StartupService.CreateStartupAsync()` → after DB save → same reindex call
3. `InvestorService.UpdateProfileAsync()` → after DB save → reindex investor
4. `InvestorService.UpdatePreferencesAsync()` → after DB save → reindex investor (preferences are critical for filtering)
5. After AI evaluation completes (when .NET detects `status=completed` on poll) → reindex startup with updated AI fields

**.NET service layer** (`IAiRecommendationService`):

1. `GetStartupRecommendationsAsync(int investorId, int topN)` → calls Python `GET /api/v1/recommendations/startups?investor_id={id}&top_n={n}`
2. `GetMatchExplanationAsync(int investorId, int startupId)` → calls Python `GET /api/v1/recommendations/startups/{startupId}/explanation?investor_id={investorId}`
3. `ReindexStartupAsync(Startup startup, AiEvaluationSummary? aiData)` → calls Python `POST /internal/recommendations/reindex/startup/{id}` (internal, not exposed to FE)
4. `ReindexInvestorAsync(Investor investor, InvestorPreferences? prefs)` → calls Python `POST /internal/recommendations/reindex/investor/{id}` (internal, not exposed to FE)

**Field mapping for Startup reindex** (`ReindexStartupRequest`):

| Python field                      | .NET source                                                           |
| --------------------------------- | --------------------------------------------------------------------- |
| `startup_id`                      | `startup.StartupID.ToString()`                                        |
| `profile_version`                 | `startup.UpdatedAt?.ToString("o") ?? startup.CreatedAt.ToString("o")` |
| `source_updated_at`               | `startup.UpdatedAt ?? startup.CreatedAt` (ISO 8601)                   |
| `startup_name`                    | `startup.CompanyName`                                                 |
| `tagline`                         | `startup.OneLiner`                                                    |
| `stage`                           | `startup.Stage?.ToString()` (e.g., `"Seed"`)                          |
| `primary_industry`                | `startup.Industry?.IndustryName` (need navigation property load)      |
| `location`                        | `startup.Location`                                                    |
| `market_scope`                    | `startup.MarketScope`                                                 |
| `product_status`                  | `startup.ProductStatus`                                               |
| `is_profile_visible_to_investors` | `startup.IsVisible`                                                   |
| `verification_label`              | Map from `StartupTag` enum → Python string                            |
| `account_active`                  | `startup.ProfileStatus == ProfileStatus.Approved`                     |
| `ai_evaluation_status`            | From `AiEvaluationRun.Status` if exists                               |
| `ai_overall_score`                | From `AiEvaluationRun.OverallScore` if completed                      |
| `ai_summary`                      | From canonical report (optional, if cached)                           |
| `ai_strength_tags`                | From canonical report (optional)                                      |
| `ai_weakness_tags`                | From canonical report (optional)                                      |

**Field mapping for Investor reindex** (`ReindexInvestorRequest`):

| Python field                | .NET source                                                             |
| --------------------------- | ----------------------------------------------------------------------- |
| `investor_id`               | `investor.InvestorID.ToString()`                                        |
| `profile_version`           | `investor.UpdatedAt?.ToString("o") ?? investor.CreatedAt.ToString("o")` |
| `source_updated_at`         | `investor.UpdatedAt ?? investor.CreatedAt`                              |
| `full_name`                 | `investor.FullName`                                                     |
| `firm_name`                 | `investor.FirmName`                                                     |
| `investment_thesis`         | `investor.InvestmentThesis`                                             |
| `location`                  | `investor.Location`                                                     |
| `preferred_stages`          | `prefs.PreferredStages` (JSON deserialize from DB)                      |
| `preferred_industries`      | `prefs.PreferredIndustries` (JSON deserialize from DB)                  |
| `preferred_geographies`     | `prefs.PreferredGeographies`                                            |
| `min_ticket_size`           | `prefs.MinInvestmentSize`                                               |
| `max_ticket_size`           | `prefs.MaxInvestmentSize`                                               |
| `require_verified_startups` | infer from `MinPotentialScore` or default `false`                       |
| `require_visible_profiles`  | `true` (always require visible)                                         |
| `account_active`            | `investor.ProfileStatus == ProfileStatus.Approved`                      |
| `verification_label`        | Map from `InvestorTag` enum                                             |

**Cache strategy**: none for MVP. If recommendation latency > 2 s becomes a UX issue, add `IMemoryCache` per investor (TTL 60 s). Invalidate on reindex.

---

### 4.3 Investor Agent / Chatbot

**Flow overview**: Investor sends message → .NET validates JWT → calls Python chat/stream → proxies response to frontend

**Frontend calls .NET**:

- `POST /api/ai/investor-agent/chat` — non-streaming chat
- `POST /api/ai/investor-agent/chat/stream` — SSE streaming chat

**Who is authorized**: `InvestorOnly` policy

**.NET service layer** (`IAiInvestorAgentService`):

1. `ChatAsync(InvestorAgentChatRequest)` → calls Python `POST /api/v1/investor-agent/chat`
2. `ChatStreamAsync(InvestorAgentChatRequest, HttpResponse)` → calls Python `POST /api/v1/investor-agent/chat/stream` → pipes SSE bytes to `HttpResponse`

**thread_id contract**:

- Frontend must generate a stable UUID per conversation session and pass it in every request
- .NET accepts `thread_id` in request body and forwards it to Python unchanged
- .NET does NOT generate or store `thread_id`
- Recommended format: `"investor-{investorId}-{uuid}"` (enforced by frontend, documented in API)

**Non-stream response** — `.NET` response DTO mirrors Python dict:

```json
{
  "intent": "...",
  "finalAnswer": "...",
  "references": [...],
  "caveats": [...],
  "writerNotes": [...],
  "processingWarnings": [...],
  "groundingSummary": {...},
  "resolvedQuery": "...",
  "fallbackTriggered": false
}
```

**Streaming proxy approach**:

1. Controller sets `Response.ContentType = "text/event-stream"`
2. Controller sets `Response.Headers["Cache-Control"] = "no-cache"`
3. Calls `IAiInvestorAgentService.ChatStreamAsync(request, Response)`
4. Service opens `HttpRequestMessage` to Python with `HttpCompletionOption.ResponseHeadersRead`
5. Service reads `response.Content.ReadAsStreamAsync()` and copies to `Response.Body`
6. No SSE parsing in .NET — transparent byte-level proxy
7. Frontend receives identical SSE event stream as Python would emit

**Out-of-scope handling**: Python sends standardized refusal in `final_answer` when `intent == out_of_scope`. .NET does not need to filter; just proxy.

**Research endpoint** (`/api/v1/investor-agent/research`): one-shot, sync, can be very slow (10-30 s). Set timeout = 120 s. May consider not exposing this endpoint initially (experimental).

---

## 5. API / DTO Mapping

### 5.1 Evaluation DTOs

#### .NET public request DTO (FE → .NET)

```
SubmitAiEvaluationRequest {
    List<DocumentEvaluationItem> Documents
}
DocumentEvaluationItem {
    int DocumentId
    // document_type and file_url resolved server-side from DocumentId
}
```

#### .NET internal DTO (.NET → Python)

```
PySubmitEvaluationRequest {
    string StartupId
    List<PyDocumentInput> Documents
}
PyDocumentInput {
    string DocumentId
    string DocumentType       // "pitch_deck" | "business_plan"
    string FileUrlOrPath
}
```

#### Python response → .NET public DTO

```
AiEvaluationSubmitResultDto {
    int RunId                 // local .NET AiEvaluationRun.Id
    int PythonRunId           // Python evaluation_run_id
    string Status             // "queued"
    string Message
}
```

#### Poll status response

```
AiEvaluationStatusDto {
    int RunId
    string StartupId
    string Status             // queued | processing | completed | failed | retry
    DateTime SubmittedAt
    string? FailureReason
    decimal? OverallScore
    decimal? OverallConfidence
    List<AiDocumentStatusItem> Documents
}
AiDocumentStatusItem {
    string DocumentId
    string DocumentType
    string Status
    string ExtractionStatus
    string? Summary
}
```

#### Report response (forward Python canonical result)

```
AiEvaluationReportDto {
    string StartupId
    string Status
    AiClassificationResult Classification
    Dictionary<string, decimal> EffectiveWeights
    List<AiCriterionResult> CriteriaResults
    AiOverallResult OverallResult
    AiNarrative Narrative
    List<string> ProcessingWarnings
}
AiCriterionResult {
    string CriterionName
    decimal Score
    string Confidence
    string? Rationale
    List<string> SupportingEvidence
    List<string> Gaps
}
AiOverallResult {
    decimal Score
    string Band           // LOW | MEDIUM | HIGH | VERY_HIGH
    string Confidence
    string Summary
}
AiNarrative {
    string ExecutiveSummary
    string Strengths
    string Weaknesses
    string RecommendedNextSteps
}
AiClassificationResult {
    string PrimaryType
    string? SecondaryType
    string Confidence
    string Rationale
}
```

### 5.2 Recommendation DTOs

#### .NET public response DTO (FE ← .NET)

```
AiRecommendationListDto {
    string InvestorId
    List<AiRecommendationMatchDto> Items
    List<string> Warnings
    DateTime GeneratedAt
}
AiRecommendationMatchDto {
    string StartupId
    string StartupName
    decimal FinalMatchScore
    decimal StructuredScore
    decimal SemanticScore
    string MatchBand          // LOW | MEDIUM | HIGH | VERY_HIGH
    string FitSummaryLabel
    List<string> MatchReasons
    List<string> PositiveReasons
    List<string> CautionReasons
    List<string> WarningFlags
}
```

#### Explanation DTO

```
AiMatchExplanationDto {
    string InvestorId
    string StartupId
    AiRecommendationMatchDto Result
    DateTime GeneratedAt
}
```

### 5.3 Investor Agent DTOs

#### .NET public request DTO

```
InvestorAgentChatRequest {
    string Query
    string ThreadId    // required from frontend; format: "investor-{id}-{uuid}"
}
```

#### .NET public response DTO (non-stream)

```
InvestorAgentChatResponseDto {
    string Intent
    string FinalAnswer
    List<AiReferenceItem> References
    List<string> Caveats
    List<string> WriterNotes
    List<string> ProcessingWarnings
    AiGroundingSummary GroundingSummary
    string? ResolvedQuery
    bool FallbackTriggered
}
AiReferenceItem {
    string Title
    string Url
    string SourceDomain
}
AiGroundingSummary {
    int VerifiedClaimCount
    int WeaklySupportedClaimCount
    int ConflictingClaimCount
    int UnsupportedClaimCount
    int ReferenceCount
    string CoverageStatus    // sufficient | insufficient | conflicting
}
```

#### Stream: no .NET DTO — raw SSE bytes proxied to frontend

### 5.4 Endpoint routing summary

| FE endpoint (.NET)                                         | HTTP Method | Auth             | Maps to Python                                                            |
| ---------------------------------------------------------- | ----------- | ---------------- | ------------------------------------------------------------------------- |
| `/api/ai/evaluation/submit`                                | POST        | StartupOnly      | `POST /api/v1/evaluations/`                                               |
| `/api/ai/evaluation/{runId}/status`                        | GET         | StartupOnly      | `GET /api/v1/evaluations/{pythonRunId}`                                   |
| `/api/ai/evaluation/{runId}/report`                        | GET         | StartupOnly      | `GET /api/v1/evaluations/{pythonRunId}/report`                            |
| `/api/ai/evaluation/history`                               | GET         | StartupOnly      | `GET /api/v1/evaluations/history?startup_id={id}`                         |
| `/api/ai/recommendations/startups`                         | GET         | InvestorOnly     | `GET /api/v1/recommendations/startups?investor_id={id}&top_n={n}`         |
| `/api/ai/recommendations/startups/{startupId}/explanation` | GET         | InvestorOnly     | `GET /api/v1/recommendations/startups/{sid}/explanation?investor_id={id}` |
| `/api/ai/investor-agent/chat`                              | POST        | InvestorOnly     | `POST /api/v1/investor-agent/chat`                                        |
| `/api/ai/investor-agent/chat/stream`                       | POST        | InvestorOnly     | `POST /api/v1/investor-agent/chat/stream`                                 |
| _(internal, no FE endpoint)_                               | POST        | server-to-server | `POST /internal/recommendations/reindex/startup/{id}`                     |
| _(internal, no FE endpoint)_                               | POST        | server-to-server | `POST /internal/recommendations/reindex/investor/{id}`                    |

---

## 6. Config / Security Plan

### New `appsettings.json` section to add

```json
"PythonAi": {
  "BaseUrl": "http://localhost:8000",
  "InternalToken": "",
  "TimeoutSeconds": 30,
  "StreamTimeoutSeconds": 120,
  "RetryCount": 2,
  "RetryDelayMs": 500,
  "HealthCheckPath": "/health"
}
```

### New `.env` entries

```
PythonAi__BaseUrl=http://localhost:8000
PythonAi__InternalToken=your-shared-secret-here
```

### `PythonAiOptions` class (new)

```csharp
// src/AISEP.Application/Configuration/PythonAiOptions.cs
public class PythonAiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string? InternalToken { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int StreamTimeoutSeconds { get; set; } = 120;
    public int RetryCount { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 500;
    public string HealthCheckPath { get; set; } = "/health";
}
```

### DI registration in `Program.cs` (new additions)

```csharp
// Options binding
builder.Services.Configure<PythonAiOptions>(builder.Configuration.GetSection("PythonAi"));

// Typed HttpClient for Python AI
builder.Services.AddHttpClient<IPythonAiClient, PythonAiHttpClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<PythonAiOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
});

// AI application services
builder.Services.AddScoped<IAiEvaluationService, AiEvaluationService>();
builder.Services.AddScoped<IAiRecommendationService, AiRecommendationService>();
builder.Services.AddScoped<IAiInvestorAgentService, AiInvestorAgentService>();
```

### Security

| Concern                       | Plan                                                                                                                      |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| Python service auth from .NET | `X-Internal-Token` header from `PythonAiOptions.InternalToken` on all reindex calls                                       |
| Evaluation/chatbot endpoints  | No auth on Python side currently; deploy Python on internal VPC/network                                                   |
| Frontend → .NET               | Existing JWT Bearer remains; .NET is the public auth boundary                                                             |
| Signed documents              | Use public Cloudinary URLs (already in `Document.FileURL`); for private files generate signed URL via `CloudinaryService` |
| SSE stream                    | JWT validated at .NET before opening upstream SSE connection; frontend can't bypass                                       |
| Retry / circuit breaker       | Add `Polly` retry policy (2 retries, 500 ms delay) on Python HTTP client for transient failures (5xx); no retry on 4xx    |
| Streaming timeout             | Separate `HttpClient` instance for streaming with 120 s timeout                                                           |

### Error handling from Python

| Python response                        | .NET action                                                                                       |
| -------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `202` on report endpoint               | Return `ApiResponse.Fail("EVALUATION_NOT_READY", "Report not ready, please retry")` with 202      |
| `409` on report (failed run)           | Return `ApiResponse.Fail("EVALUATION_FAILED", reason)` with 400                                   |
| `500` from Python                      | Log error, return `ApiResponse.Fail("AI_SERVICE_ERROR", "AI service error")` with 502             |
| `404` from Python                      | Return `ApiResponse.Fail("NOT_FOUND", "...")` with 404                                            |
| `401` from Python (bad internal token) | Log as critical config error, return 500 to FE                                                    |
| Python service unreachable             | Catch `HttpRequestException`, return `ApiResponse.Fail("AI_SERVICE_UNAVAILABLE", "...")` with 503 |
| Mixed `detail` shape on error          | Use `JsonElement` to tolerate both `string` and `object` detail types                             |

---

## 7. Files to Change

### Legend: `CREATE` | `UPDATE` | `NO CHANGE`

### AISEP.Application project

| File                                     | Change     | What                                                          |
| ---------------------------------------- | ---------- | ------------------------------------------------------------- |
| `Configuration/PythonAiOptions.cs`       | **CREATE** | Options class for Python AI config                            |
| `Interfaces/IAiEvaluationService.cs`     | **CREATE** | Interface for AI evaluation business logic                    |
| `Interfaces/IAiRecommendationService.cs` | **CREATE** | Interface for AI recommendation business logic                |
| `Interfaces/IAiInvestorAgentService.cs`  | **CREATE** | Interface for AI investor agent business logic                |
| `Interfaces/IPythonAiClient.cs`          | **CREATE** | Interface for raw Python HTTP client                          |
| `DTOs/Ai/AiEvaluationDTOs.cs`            | **CREATE** | All evaluation request/response DTOs                          |
| `DTOs/Ai/AiRecommendationDTOs.cs`        | **CREATE** | All recommendation request/response DTOs                      |
| `DTOs/Ai/AiInvestorAgentDTOs.cs`         | **CREATE** | Chat request/response DTOs                                    |
| `Interfaces/IStartupService.cs`          | **UPDATE** | Add reindex-awareness (optional, or handle in Infrastructure) |

### AISEP.Domain project

| File                          | Change     | What                                                                                      |
| ----------------------------- | ---------- | ----------------------------------------------------------------------------------------- |
| `Entities/AiEvaluationRun.cs` | **CREATE** | New entity to persist evaluation job records                                              |
| `Enums/Enums.cs`              | **UPDATE** | Add `AiEvaluationRunStatus` enum (`Queued`, `Processing`, `Completed`, `Failed`, `Retry`) |

### AISEP.Infrastructure project

| File                                  | Change     | What                                                                                                 |
| ------------------------------------- | ---------- | ---------------------------------------------------------------------------------------------------- |
| `Services/PythonAiHttpClient.cs`      | **CREATE** | Typed `HttpClient` implementation of `IPythonAiClient`                                               |
| `Services/AiEvaluationService.cs`     | **CREATE** | Orchestrates evaluation submit/poll/report; persists `AiEvaluationRun`                               |
| `Services/AiRecommendationService.cs` | **CREATE** | Calls Python recommendation endpoints; triggers reindex                                              |
| `Services/AiInvestorAgentService.cs`  | **CREATE** | Chat + SSE stream proxy logic                                                                        |
| `Services/StartupService.cs`          | **UPDATE** | After profile update/create → fire-and-forget `IAiRecommendationService.ReindexStartupAsync()`       |
| `Services/InvestorService.cs`         | **UPDATE** | After profile/preferences update → fire-and-forget `IAiRecommendationService.ReindexInvestorAsync()` |
| `Data/ApplicationDbContext.cs`        | **UPDATE** | Add `DbSet<AiEvaluationRun> AiEvaluationRuns`                                                        |
| `Migrations/`                         | **CREATE** | New migration for `AiEvaluationRuns` table                                                           |

### AISEP.WebAPI project

| File                                        | Change     | What                                                         |
| ------------------------------------------- | ---------- | ------------------------------------------------------------ |
| `Controllers/AiEvaluationController.cs`     | **CREATE** | Expose evaluation submit/poll/report/history to FE           |
| `Controllers/AiRecommendationController.cs` | **CREATE** | Expose recommendation GET endpoints to FE                    |
| `Controllers/AiInvestorAgentController.cs`  | **CREATE** | Expose chat + stream endpoints to FE                         |
| `Program.cs`                                | **UPDATE** | Register `PythonAiOptions`, `IPythonAiClient`, 3 AI services |
| `appsettings.json`                          | **UPDATE** | Add `PythonAi` config section                                |
| `appsettings.Development.json`              | **UPDATE** | Set `PythonAi:BaseUrl` for local dev                         |

### Tests project

| File                            | Change                | What                                        |
| ------------------------------- | --------------------- | ------------------------------------------- |
| `tests/AISEP.Domain.UnitTests/` | **UPDATE** (optional) | Add unit tests for new DTOs / mapping logic |

### Files with NO CHANGE

All existing controllers, services, DTOs not listed above remain unchanged. Business logic of existing features is not modified.

---

## 8. Code Snippets (Reference Implementation)

> These are reference implementations for the developer to follow.  
> Not yet created in the repo — use these as the exact template.

### 8.1 `PythonAiOptions.cs`

```csharp
// src/AISEP.Application/Configuration/PythonAiOptions.cs
namespace AISEP.Application.Configuration;

public class PythonAiOptions
{
    public const string SectionName = "PythonAi";

    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string? InternalToken { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int StreamTimeoutSeconds { get; set; } = 120;
    public int RetryCount { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 500;
    public string HealthCheckPath { get; set; } = "/health";
}
```

### 8.2 `AiEvaluationRun.cs` entity

```csharp
// src/AISEP.Domain/Entities/AiEvaluationRun.cs
namespace AISEP.Domain.Entities;

public class AiEvaluationRun
{
    public int Id { get; set; }
    public int StartupId { get; set; }
    public int PythonEvaluationRunId { get; set; }     // Python's evaluation_run_id
    public string Status { get; set; } = "queued";     // queued|processing|completed|failed|retry
    public string? FailureReason { get; set; }
    public decimal? OverallScore { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Startup Startup { get; set; } = null!;
}
```

### 8.3 `IPythonAiClient.cs` interface

```csharp
// src/AISEP.Application/Interfaces/IPythonAiClient.cs
namespace AISEP.Application.Interfaces;

public interface IPythonAiClient
{
    // Evaluation
    Task<PySubmitEvaluationResponse> SubmitEvaluationAsync(PySubmitEvaluationRequest request, CancellationToken ct = default);
    Task<JsonElement> GetEvaluationStatusAsync(int pythonRunId, CancellationToken ct = default);
    Task<JsonElement> GetEvaluationReportAsync(int pythonRunId, CancellationToken ct = default);
    Task<JsonElement> GetEvaluationHistoryAsync(string startupId, CancellationToken ct = default);

    // Recommendation
    Task ReindexStartupAsync(JsonElement payload, string startupId, CancellationToken ct = default);
    Task ReindexInvestorAsync(JsonElement payload, string investorId, CancellationToken ct = default);
    Task<JsonElement> GetStartupRecommendationsAsync(string investorId, int topN, CancellationToken ct = default);
    Task<JsonElement> GetMatchExplanationAsync(string startupId, string investorId, CancellationToken ct = default);

    // Investor Agent
    Task<JsonElement> ChatAsync(PyChatRequest request, CancellationToken ct = default);
    Task StreamChatAsync(PyChatRequest request, Stream outputStream, CancellationToken ct = default);
}
```

### 8.4 `PythonAiHttpClient.cs` skeleton (key methods)

```csharp
// src/AISEP.Infrastructure/Services/PythonAiHttpClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using AISEP.Application.Interfaces;
using AISEP.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISEP.Infrastructure.Services;

public class PythonAiHttpClient : IPythonAiClient
{
    private readonly HttpClient _http;
    private readonly PythonAiOptions _opts;
    private readonly ILogger<PythonAiHttpClient> _logger;

    // Separate HttpClient instance for streaming (longer timeout)
    private readonly HttpClient _streamHttp;

    public PythonAiHttpClient(HttpClient http, IOptions<PythonAiOptions> opts, ILogger<PythonAiHttpClient> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;

        // Build a separate streaming client
        _streamHttp = new HttpClient
        {
            BaseAddress = new Uri(_opts.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_opts.StreamTimeoutSeconds)
        };
    }

    public async Task<PySubmitEvaluationResponse> SubmitEvaluationAsync(PySubmitEvaluationRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/evaluations/", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PySubmitEvaluationResponse>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Empty response from Python evaluation submit");
    }

    public async Task<JsonElement> GetEvaluationStatusAsync(int pythonRunId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/v1/evaluations/{pythonRunId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    public async Task<JsonElement> GetEvaluationReportAsync(int pythonRunId, CancellationToken ct = default)
    {
        // Note: 202 = not ready, 409 = failed — DO NOT call EnsureSuccessStatusCode here
        var response = await _http.GetAsync($"/api/v1/evaluations/{pythonRunId}/report", ct);
        return new JsonElement(); // caller inspects status code
        // IMPORTANT: caller must check response.StatusCode before reading body
        // Return HttpResponseMessage directly, or return (StatusCode, JsonElement) tuple
    }

    public async Task ReindexStartupAsync(JsonElement payload, string startupId, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/recommendations/reindex/startup/{startupId}");
        if (!string.IsNullOrEmpty(_opts.InternalToken))
            req.Headers.Add("X-Internal-Token", _opts.InternalToken);
        req.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Reindex startup {StartupId} failed: {Status}", startupId, response.StatusCode);
    }

    public async Task StreamChatAsync(PyChatRequest request, Stream outputStream, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/investor-agent/chat/stream");
        req.Content = JsonContent.Create(request);

        var response = await _streamHttp.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await stream.CopyToAsync(outputStream, ct);
    }
}
```

### 8.5 `AiEvaluationController.cs` skeleton

```csharp
// src/AISEP.WebAPI/Controllers/AiEvaluationController.cs
[ApiController]
[Route("api/ai/evaluation")]
[Tags("AI Evaluation")]
[Authorize(Policy = "StartupOnly")]
public class AiEvaluationController : ControllerBase
{
    private readonly IAiEvaluationService _evalService;

    public AiEvaluationController(IAiEvaluationService evalService) => _evalService = evalService;

    private int GetUserId() => int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : 0;

    /// <summary>Submit startup documents for AI evaluation (async).</summary>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(ApiResponse<AiEvaluationSubmitResultDto>), 200)]
    public async Task<IActionResult> Submit([FromBody] SubmitAiEvaluationRequest request, CancellationToken ct)
    {
        var result = await _evalService.SubmitEvaluationAsync(GetUserId(), request, ct);
        return result.ToActionResult();
    }

    /// <summary>Poll evaluation run status.</summary>
    [HttpGet("{runId:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<AiEvaluationStatusDto>), 200)]
    public async Task<IActionResult> GetStatus(int runId, CancellationToken ct)
    {
        var result = await _evalService.GetEvaluationStatusAsync(runId, GetUserId(), ct);
        return result.ToActionResult();
    }

    /// <summary>Get final evaluation report. Returns 202 if not ready yet.</summary>
    [HttpGet("{runId:int}/report")]
    [ProducesResponseType(typeof(ApiResponse<AiEvaluationReportDto>), 200)]
    [ProducesResponseType(202)]
    public async Task<IActionResult> GetReport(int runId, CancellationToken ct)
    {
        var result = await _evalService.GetEvaluationReportAsync(runId, GetUserId(), ct);
        if (!result.Success && result.Error?.Code == "EVALUATION_NOT_READY")
            return StatusCode(202, result);
        return result.ToActionResult();
    }

    /// <summary>List evaluation history for the current startup.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await _evalService.GetEvaluationHistoryAsync(GetUserId(), ct);
        return result.ToActionResult();
    }
}
```

### 8.6 `AiInvestorAgentController.cs` SSE proxy pattern

```csharp
// src/AISEP.WebAPI/Controllers/AiInvestorAgentController.cs
[ApiController]
[Route("api/ai/investor-agent")]
[Tags("AI Investor Agent")]
[Authorize(Policy = "InvestorOnly")]
public class AiInvestorAgentController : ControllerBase
{
    private readonly IAiInvestorAgentService _agentService;

    public AiInvestorAgentController(IAiInvestorAgentService agentService) => _agentService = agentService;

    private int GetUserId() => int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : 0;

    /// <summary>Non-streaming chat with AI investor agent.</summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiResponse<InvestorAgentChatResponseDto>), 200)]
    public async Task<IActionResult> Chat([FromBody] InvestorAgentChatRequest request, CancellationToken ct)
    {
        var result = await _agentService.ChatAsync(request, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Streaming chat — proxies SSE events from Python AI directly to client.
    /// Client receives text/event-stream with types: progress, answer_chunk, final_answer, final_metadata, error, [DONE].
    /// </summary>
    [HttpPost("chat/stream")]
    public async Task ChatStream([FromBody] InvestorAgentChatRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // disable Nginx buffering if behind proxy

        await _agentService.ChatStreamAsync(request, Response.Body, ct);
    }
}
```

### 8.7 Reindex hook in `StartupService.UpdateStartupAsync()` (update pattern)

```csharp
// In StartupService.UpdateStartupAsync() — after _context.SaveChangesAsync()
// Fire-and-forget: don't await, don't throw on failure
_ = Task.Run(async () =>
{
    try
    {
        await _aiRecommendationService.ReindexStartupAsync(startup, aiData: null);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Recommendation reindex failed for startup {StartupId} — will retry on next profile update", startup.StartupID);
    }
}, CancellationToken.None);
```

### 8.8 `Program.cs` additions (placement after existing `AddScoped` block)

```csharp
// Python AI Integration
builder.Services.Configure<PythonAiOptions>(builder.Configuration.GetSection(PythonAiOptions.SectionName));
builder.Services.AddHttpClient<IPythonAiClient, PythonAiHttpClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<PythonAiOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
});
builder.Services.AddScoped<IAiEvaluationService, AiEvaluationService>();
builder.Services.AddScoped<IAiRecommendationService, AiRecommendationService>();
builder.Services.AddScoped<IAiInvestorAgentService, AiInvestorAgentService>();
```

---

## 9. Risks / Gaps

### High Priority (must resolve before integrating)

| #   | Risk                                                                                               | Impact                                                                  | Mitigation                                                                              |
| --- | -------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| R1  | Python AI service has **no auth** on evaluation, recommendation read, and investor-agent endpoints | Direct exposure if service is accidentally public-facing                | Deploy Python on internal network only; .NET is the sole caller                         |
| R2  | Python error `detail` shape is **inconsistent** (string vs object)                                 | .NET DTO deserialization crash                                          | Use `JsonElement` / tolerant deserialization; check response status code before parsing |
| R3  | Investor agent uses **in-process `MemorySaver`** — thread memory lost on Python process restart    | Conversation context lost for all investors                             | Acceptable for MVP; document behavior to frontend; warn users on reconnect              |
| R4  | Python evaluation requires **Celery + Redis** to be running                                        | Evaluation silently queues but never completes if Celery worker is down | Add health check endpoint polling; surface degraded state to FE                         |
| R5  | `Document.FileURL` is a public Cloudinary URL — **must remain accessible** from Python service     | Evaluation fails if Cloudinary URL is restricted                        | Confirm Cloudinary resources are public OR implement signed URL generation              |

### Medium Priority (address in iteration 2)

| #   | Risk                                                                                             | Impact                                   | Mitigation                                                          |
| --- | ------------------------------------------------------------------------------------------------ | ---------------------------------------- | ------------------------------------------------------------------- |
| R6  | Recommendation storage is **file-backed JSON** — no transactional guarantees                     | Data corruption under concurrent reindex | Acceptable for pilot; plan migration to DB-backed storage in Python |
| R7  | **No webhook/callback** from Python on evaluation completion                                     | .NET must poll or FE polls .NET          | Frontend polling is sufficient for MVP; add webhooks in future      |
| R8  | Investor agent `/research` endpoint is **very slow** (10-30 s sync) and experimental             | Timeout and poor UX                      | Do not expose `/research` endpoint publicly for MVP; skip for now   |
| R9  | `StartupTag` enum-to-string mapping for `verification_label` field in reindex                    | Incorrect reindex data                   | Define explicit mapping table in `ReindexStartupRequest` builder    |
| R10 | `InvestorPreferences.PreferredStages` and `PreferredIndustries` stored as **JSON strings** in DB | Need deserialization before reindex      | Use `JsonSerializer.Deserialize<List<string>>()` in reindex builder |

### Low Priority / Informational

| #   | Note                                                                                                               |
| --- | ------------------------------------------------------------------------------------------------------------------ |
| N1  | Python `partial_completed` status appears in schema/checks but no explicit setter observed — handle defensively    |
| N2  | Python `DEFAULT_LLM_PROVIDER` defaults to `openai` but current flows use Gemini — Python team should verify config |
| N3  | `src/worker.py` (deprecated polling fallback) should NOT be used in production                                     |
| N4  | `X-Internal-Token` should be a strong secret (32+ chars) shared out-of-band between .NET and Python teams          |

---

## 10. Next Steps

### Iteration 1 — Foundation (implement first)

1. **Create `PythonAiOptions`** class + add section to `appsettings.json` and `appsettings.Development.json`
2. **Create `AiEvaluationRun` entity** + EF Core migration
3. **Implement `IPythonAiClient` / `PythonAiHttpClient`** — raw HTTP client for all Python endpoints
4. **Register typed HttpClient + options** in `Program.cs`
5. **Implement `AiEvaluationService`** with submit/poll/report + `AiEvaluationRun` DB persistence
6. **Create `AiEvaluationController`** with all 4 endpoints
7. **Add reindex trigger hooks** in `StartupService` (fire-and-forget, non-blocking)
8. **Implement `AiRecommendationService`** (reindex + public read)
9. **Create `AiRecommendationController`** with 2 GET endpoints

### Iteration 2 — Chatbot

10. **Implement `AiInvestorAgentService`** (non-stream chat)
11. **Create `AiInvestorAgentController`** with non-stream `/chat` endpoint
12. **Add SSE streaming proxy** for `/chat/stream`

### Iteration 3 — Hardening

13. **Add Polly retry policy** on `IPythonAiClient` HTTP calls
14. **Add Python AI health check** endpoint in .NET (`GET /api/ai/health` → calls Python `/health`)
15. **Add streaming timeout** separate `HttpClient` configuration
16. **Document `thread_id` convention** for frontend team
17. **Add unit tests** for DTO mapping (evaluation document type conversion, reindex payload builder)

### Pre-integration checklist

- [ ] Python AI service is deployed and `/health` returns 200
- [ ] `AISEP_INTERNAL_TOKEN` configured and shared between .NET and Python
- [ ] Celery worker + Redis running (required for evaluation)
- [ ] `GEMINI_API_KEY` configured on Python side
- [ ] Cloudinary URLs accessible from Python service network
- [ ] `PythonAi__BaseUrl` set in `.env` or environment
- [ ] New EF Core migration applied to DB

---

_End of integration plan. Proceed to implementation when pre-integration checklist is complete._
