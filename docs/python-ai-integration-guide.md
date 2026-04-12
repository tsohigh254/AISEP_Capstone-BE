# AISEP Python AI Integration Guide

> Scope: integrate the existing Python AI service in `C:\Users\LENOVO\Desktop\AISEP_AI` into the ASP.NET Core backend in `C:\Users\LENOVO\Desktop\AISEP_Capstone-BE`.
>
> This guide is based on the actual code currently present in both repos. If something is not in code, it is marked as **not yet implemented**.

## 1. Codebase summary

### 1.1 .NET backend

**Solution / structure**

- `AISEP.sln`
- `src/AISEP.WebAPI` – controllers, middleware, startup, Swagger
- `src/AISEP.Application` – DTOs, query params, interfaces
- `src/AISEP.Domain` – entities, enums
- `src/AISEP.Infrastructure` – EF Core `ApplicationDbContext`, services, migrations

**Current architecture**

- ASP.NET Core Web API on .NET 8
- JWT Bearer authentication with policy-based authorization
- EF Core + PostgreSQL
- Serilog request logging
- `RequestIdMiddleware` and `GlobalExceptionMiddleware`
- `ApiEnvelope<T>` / `ApiResponse<T>` wrapper pattern used across controllers

**Relevant controllers already present**

- `AuthController`
- `DocumentsController`
- `StartupsController`
- `InvestorsController`
- `NotificationsController`
- `ModerationController`
- `PortfolioController`
- others unrelated to AI integration

**Important current API behavior**

- `DocumentsController` handles upload/list/detail/update/archive for documents.
- `InvestorsController` already has a placeholder `GET /api/investors/recommendations` that returns `501 NotImplemented`.
- `InvestorsController.SearchStartups()` accepts `minScore` but explicitly ignores it for now.
- There is **no AIController** in the current codebase.
- There is **no typed client** or service for calling the Python AI service yet.

**Existing data model relevant to AI**

- `Document`
- `Startup`
- `Investor`
- `InvestorPreferences`
- `InvestorWatchlist`
- `StartupPotentialScore`
- `ScoreImprovementRecommendation`
- `Notification`
- `SavedReport`

### 1.2 Python AI service

**Structure**

- `src/main.py` – FastAPI app
- `src/worker.py` – DB polling worker for evaluation jobs
- `src/modules/evaluation` – document evaluation pipeline
- `src/modules/investor_agent` – LangGraph research workflow
- `src/modules/recommendation` – folder exists but is empty
- `src/shared` – config, persistence, logging, LLM client

**Current Python APIs**

- `POST /api/v1/evaluations`
- `GET /api/v1/evaluations/{id}`
- `GET /api/v1/evaluations/{id}/report`
- `GET /health`
- `POST /api/v1/investor-agent/research`

**Current Python execution model**

- Evaluation is async-by-design:
  - submit job
  - worker polls queued runs
  - process document(s)
  - aggregate results
  - report endpoint reads canonical result from DB metadata
- Investor Agent is sync request/response from the API perspective
- Recommendation is **not implemented yet** in Python code

**Python AI dependencies / external services**

- Gemini via `GeminiClient`
- Tavily via `AsyncTavilyClient`
- SQLModel / SQLite by default
- `httpx` used to download URL-based documents for evaluation

**Grounding behavior in Investor Agent**

- `GroundingSummary` exists in `GraphState`
- final writer filters fake references and placeholder URLs
- this is the canonical final response shape for the agent

## 2. Recommended integration architecture

### 2.1 Recommended strategy

**Recommended integration:** `.NET WebAPI calls Python AI service over HTTP REST`

### 2.2 Why this fits the current code

- The Python service already exposes clean HTTP endpoints.
- The .NET backend already uses JWT, policies, envelopes, and structured error mapping.
- There is no queue infrastructure in either repo today.
- Evaluation is already modeled as async job + polling in Python, which maps naturally to REST.
- Investor Agent is already a synchronous HTTP flow.

### 2.3 Sync vs async split

**Sync**

- Investor Agent research
- Recommendation lookup only after a real implementation exists

**Async**

- AI Evaluation
- Any long-running document analysis or batch scoring

### 2.4 Recommended service boundaries

- `.NET` remains the user-facing API and auth boundary.
- Python AI remains internal-only behind `.NET`.
- `.NET` should call Python with an internal service token, not the end-user JWT.

## 3. Feature-by-feature integration guide

### 3.1 AI Evaluation

#### What exists today in .NET

- `POST /api/documents` uploads a document for a startup owner.
- `Document` stores:
  - `DocumentID`
  - `StartupID`
  - `DocumentType`
  - `FileURL`
  - `IsAnalyzed`
  - `AnalysisStatus`
- `AnalysisStatus` enum in code currently is:
  - `NOTANALYZE`
  - `COMPLETED`
  - `FAILED`

#### What exists today in Python

- `POST /api/v1/evaluations`
- `GET /api/v1/evaluations/{id}`
- `GET /api/v1/evaluations/{id}/report`
- background worker `src/worker.py`
- persistence through `EvaluationRun`, `EvaluationDocument`, `EvaluationLog`

#### Recommended flow

1. User uploads document to `.NET` using `POST /api/documents`.
2. `.NET` stores the file URL in DB.
3. `.NET` triggers Python evaluation submit with:
   - `startup_id`
   - document list
   - `file_url_or_path` = the stored file URL from .NET
4. Python returns `evaluation_run_id` and `queued`.
5. Python worker processes document(s).
6. `.NET` polls Python for status or stores the run id and exposes status to UI.
7. When Python is done, `.NET` updates local analysis fields and can create a notification.

#### What .NET should store

Recommended new entity/table:

- `AiEvaluationJob`
  - `JobID`
  - `DocumentID`
  - `PythonRunId`
  - `Status`
  - `FailureReason`
  - `SubmittedAt`
  - `CompletedAt`
  - `LastCheckedAt`

If you do not want a new table immediately, the minimum viable option is to add fields directly to `Document`, but a job table is cleaner.

#### Status lifecycle mapping

Python status | Suggested .NET status

- `queued` | `Queued`
- `processing` | `Processing`
- `completed` | `Completed`
- `partial_completed` | `PartialCompleted`
- `failed` | `Failed`

#### Polling approach

Because the current Python worker is DB-polling based and there is no broker/webhook, polling is the recommended first version.

### 3.2 AI Recommendation

#### What exists today in .NET

- `GET /api/investors/recommendations` exists but returns `501 NotImplemented`.
- `InvestorsController.SearchStartups()` accepts `minScore` but the current code ignores it.
- Domain entities already exist for scoring and recommendations:
  - `StartupPotentialScore`
  - `ScoreImprovementRecommendation`
  - `InvestorPreferences`
  - `InvestorWatchlist`

#### What exists today in Python

- No live recommendation API or service.
- `src/modules/recommendation` exists but is empty.

#### Recommended strategy for now

Implement recommendation inside `.NET` first, using existing DB entities and search data, because the Python repo has no recommendation implementation yet.

#### Recommended triggers

- When investor preferences are updated
- When a startup profile changes
- When a startup document analysis or score changes
- On-demand when the investor opens recommendations page

#### Recommended storage

- Current scoring snapshot: `StartupPotentialScore`
- Text recommendations: `ScoreImprovementRecommendation`
- User-visible list: expose from `.NET` endpoint

#### Practical endpoint plan

- Keep `GET /api/investors/recommendations`
- Replace the `501` placeholder with real logic
- Optionally add `POST /api/investors/recommendations/refresh`

### 3.3 Investor Agent

#### What exists today in Python

- `POST /api/v1/investor-agent/research`
- Request schema:
  - `query`
- Response schema:
  - `intent`
  - `final_answer`
  - `references`
  - `caveats`
  - `processing_warnings`
  - `grounding_summary`

#### What exists today in .NET

- No AI agent endpoint exists yet.
- Investors already have authenticated profile and search endpoints.

#### Recommended flow

1. FE calls `.NET` investor agent endpoint.
2. `.NET` checks JWT / `InvestorOnly` policy.
3. `.NET` forwards request to Python `POST /api/v1/investor-agent/research`.
4. Python returns grounded answer.
5. `.NET` wraps the response in its standard API envelope.

#### Response mapping

Python field | .NET field

- `intent` | `intent`
- `final_answer` | `finalAnswer`
- `references` | `references[]`
- `caveats` | `caveats[]`
- `processing_warnings` | `processingWarnings[]`
- `grounding_summary` | `groundingSummary`

#### Failure handling

- Python validation failure → `.NET 400`
- Python not reachable / timeout → `.NET 502`
- Python no evidence / weak grounding → still `200` if Python returns a grounded answer with warnings
- Python hard fail from Tavily/Gemini → `.NET 502` with request id

#### Streaming

Not present in current code. Do not assume streaming support.

## 4. API contract mapping

### 4.1 Python endpoints already available

#### Evaluation

- `POST /api/v1/evaluations`
- `GET /api/v1/evaluations/{id}`
- `GET /api/v1/evaluations/{id}/report`

#### Investor Agent

- `POST /api/v1/investor-agent/research`

### 4.2 .NET endpoints already available and relevant

#### Documents

- `POST /api/documents`
- `GET /api/documents`
- `GET /api/documents/{documentId}`
- `PUT /api/documents/{documentId}/metadata`
- `DELETE /api/documents/{documentId}`

#### Investors

- `GET /api/investors/search`
- `GET /api/investors/recommendations` (`501` placeholder)
- `GET /api/investors/me`
- `PUT /api/investors/me`
- `PUT /api/investors/me/preferences`
- `POST /api/investors/me/watchlist`

#### Notifications

- `GET /api/notifications`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

### 4.3 Request/response DTOs to add in .NET

#### Evaluation request DTO

```csharp
public sealed class PythonSubmitEvaluationRequest
{
    public string StartupId { get; set; } = default!;
    public List<PythonEvaluationDocumentInput> Documents { get; set; } = [];
}

public sealed class PythonEvaluationDocumentInput
{
    public string DocumentId { get; set; } = default!;
    public string DocumentType { get; set; } = default!;
    public string FileUrlOrPath { get; set; } = default!;
}
```

#### Investor Agent request DTO

```csharp
public sealed class PythonInvestorResearchRequest
{
    public string Query { get; set; } = default!;
}
```

#### Investor Agent response DTO

```csharp
public sealed class PythonInvestorResearchResponse
{
    public string Intent { get; set; } = default!;
    public string FinalAnswer { get; set; } = default!;
    public List<PythonReferenceItem> References { get; set; } = [];
    public List<string> Caveats { get; set; } = [];
    public List<string> ProcessingWarnings { get; set; } = [];
    public PythonGroundingSummary GroundingSummary { get; set; } = default!;
}
```

### 4.4 Status code mapping

Python / internal condition | .NET response

- Success | `200` / `201`
- Pending async job | `202`
- Missing resource | `404`
- Validation failure | `400`
- Upstream conflict / report unavailable | `409`
- Python unavailable / timeout | `502`
- Unauthorized / invalid internal token | `401` or `403`

## 5. Security and config

### 5.1 Current .NET auth model

- JWT Bearer auth
- policies in `Program.cs`:
  - `StartupOnly`
  - `InvestorOnly`
  - `AdvisorOnly`
  - `StaffOrAdmin`
  - `AdminOnly`
- Request user id is read from claim `sub` or `NameIdentifier`

### 5.2 Current Python auth model

- No user auth in code
- No internal service auth in code

### 5.3 Recommended service-to-service auth

Use an internal bearer token or shared secret header:

- `.NET` sends `X-Internal-Token: <secret>` or `Authorization: Bearer <service-token>`
- Python validates this token for all internal calls

This keeps end-user auth in `.NET` only.

### 5.4 .NET config to add

```json
{
  "PythonAi": {
    "BaseUrl": "http://localhost:8000",
    "InternalToken": "dev-internal-token",
    "TimeoutSeconds": 90,
    "RetryCount": 2
  }
}
```

### 5.5 Python config to add

```dotenv
AISEP_INTERNAL_TOKEN=dev-internal-token
PYTHON_AI_CORS_ORIGINS=http://localhost:3000
PYTHON_AI_TIMEOUT_SECONDS=90
```

### 5.6 Existing Python env vars already in code

- `DATABASE_URL`
- `GEMINI_API_KEY`
- `OPENAI_API_KEY`
- `TAVILY_API_KEY`
- `DEFAULT_LLM_PROVIDER`
- `ENABLE_PSEUDO_OCR_FALLBACK`

## 6. Files to change

### 6.1 .NET files

Priority 1:

- `src/AISEP.WebAPI/Program.cs`
- `src/AISEP.WebAPI/Controllers/AIController.cs` `new`
- `src/AISEP.Infrastructure/Services/PythonAiClient.cs` `new`
- `src/AISEP.Application/Interfaces/IAiService.cs` `new`
- `src/AISEP.Application/DTOs/AI/*.cs` `new`

Priority 2:

- `src/AISEP.Infrastructure/Data/ApplicationDbContext.cs`
- `src/AISEP.Domain/Entities/AiEvaluationJob.cs` `new`
- `src/AISEP.Infrastructure/Services/DocumentService.cs`
- `src/AISEP.Infrastructure/Services/InvestorService.cs`
- `src/AISEP.WebAPI/Controllers/InvestorsController.cs`

Priority 3:

- `src/AISEP.WebAPI/Controllers/NotificationsController.cs` if you want AI job notifications surfaced in a dedicated way

### 6.2 Python files

Priority 1:

- `src/modules/investor_agent/api/router.py` – add internal auth guard if desired
- `src/shared/config/settings.py` – add internal token/base URL settings

Priority 2:

- `src/modules/recommendation/api/*` `new`
- `src/modules/recommendation/application/*` `new`
- `src/modules/recommendation/infrastructure/*` `new`

Priority 3:

- `src/worker.py` and `src/modules/evaluation/workers/tasks.py` – production hardening if you move beyond simple polling

## 7. Example code snippets

### 7.1 .NET typed client

```csharp
using System.Net.Http.Json;

public sealed class PythonAiClient
{
    private readonly HttpClient _http;

    public PythonAiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PythonInvestorResearchResponse> ResearchAsync(
        PythonInvestorResearchRequest request,
        CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/investor-agent/research");
        message.Headers.Add("X-Internal-Token", "dev-internal-token");
        message.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PythonInvestorResearchResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty response from Python AI service.");
    }
}
```

### 7.2 .NET AI controller

```csharp
[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly PythonAiClient _client;

    public AIController(PythonAiClient client)
    {
        _client = client;
    }

    [HttpPost("investor-agent/research")]
    [Authorize(Policy = "InvestorOnly")]
    public async Task<IActionResult> Research([FromBody] PythonInvestorResearchRequest request, CancellationToken ct)
    {
        var result = await _client.ResearchAsync(request, ct);
        return Ok(new ApiEnvelope<PythonInvestorResearchResponse>
        {
            IsSuccess = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success",
            Data = result
        });
    }
}
```

### 7.3 Polling flow for evaluation

```csharp
public async Task<PythonEvaluationStatusResponse> WaitForCompletionAsync(int runId, CancellationToken ct)
{
    while (true)
    {
        var status = await _client.GetEvaluationAsync(runId, ct);

        if (status.Status is "completed" or "partial_completed" or "failed")
        {
            return status;
        }

        await Task.Delay(TimeSpan.FromSeconds(10), ct);
    }
}
```

### 7.4 Error handling sample

```csharp
try
{
    var result = await _client.ResearchAsync(request, ct);
    return Ok(result);
}
catch (TaskCanceledException)
{
    return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = "Python AI timeout" });
}
catch (HttpRequestException ex)
{
    return StatusCode(StatusCodes.Status502BadGateway, new { message = "Python AI unavailable", detail = ex.Message });
}
```

## 8. Run local

### 8.1 Python service

```powershell
cd C:\Users\LENOVO\Desktop\AISEP_AI
.\.venv\Scripts\Activate.ps1
python -m uvicorn src.main:app --reload
python src\worker.py
```

### 8.2 .NET backend

```powershell
cd C:\Users\LENOVO\Desktop\AISEP_Capstone-BE
dotnet restore
dotnet run --project src\AISEP.WebAPI\AISEP.WebAPI.csproj
```

### 8.3 Recommended local test flow

1. Create startup profile in `.NET`
2. Upload a document in `.NET`
3. Submit evaluation to Python
4. Poll evaluation status
5. Call investor research endpoint
6. Verify `GroundingSummary` and references on investor agent response

### 8.4 Failure simulation

- Remove `GEMINI_API_KEY` to verify fallback behavior in Python
- Remove `TAVILY_API_KEY` to verify grounding warnings
- Stop `src/worker.py` to verify queued evaluation does not complete
- Point `.NET` to a wrong Python base URL to verify `502` handling

## 9. Deploy

### 9.1 Recommended deployment shape

- Deploy `.NET` WebAPI and Python AI as **separate services**.
- Keep Python private/internal.
- Let `.NET` be the only public entry point if possible.

### 9.2 Production concerns

- Python worker is currently a simple polling loop, not a scalable queue worker.
- SQLite should be replaced with PostgreSQL in production Python.
- Add service-to-service auth before enabling external traffic.
- Add retries and timeout budgets on `.NET` outbound calls.

### 9.3 Scaling note

- Investor Agent can be synchronous with a longer timeout.
- Evaluation should stay async and poll-based.
- Recommendation should be cached or precomputed if it becomes expensive.

## 10. Observability

### 10.1 Existing observability in .NET

- `RequestIdMiddleware` sets `X-Request-Id`
- Serilog request logging is already enabled
- `GlobalExceptionMiddleware` converts unhandled errors to consistent envelopes

### 10.2 What to log end-to-end

- `requestId`
- `userId`
- `startupId`
- `documentId`
- `pythonRunId`
- `endpoint`
- `latencyMs`
- `retryCount`
- `statusCode`
- `modelName`
- `coverageStatus`
- `processingWarnings`

### 10.3 Alerting signals

- Python timeout rate increases
- repeated `502` from AI endpoints
- evaluation jobs stuck in `queued` or `processing`
- `grounding_summary.reference_count == 0`
- `coverage_status != sufficient`
- Tavily/Gemini failures spike

## 11. Open gaps / assumptions

- There is **no Python recommendation implementation yet**.
- There is **no .NET AIController yet**.
- There is **no internal service auth yet**.
- There is **no queue/broker** yet; evaluation uses simple polling.
- Python uses SQLite by default; production should move to PostgreSQL.
- `.NET` `minScore` on investor search is currently ignored.

## 12. Suggested next implementation slice

If you want the fastest useful path, implement in this order:

1. Add `.NET` typed client + `AIController` for investor research
2. Add `.NET` async job table for Python evaluation runs
3. Replace `GET /api/investors/recommendations` placeholder with DB-backed logic
4. Add internal token validation to Python
5. Add recommendation module to Python later
