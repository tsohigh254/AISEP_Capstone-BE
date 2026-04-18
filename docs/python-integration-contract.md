# AISEP — .NET ↔ Python AI Service Integration Contract

> **Mục đích:** Tài liệu này mô tả toàn bộ giao tiếp giữa repo .NET (AISEP_Capstone-BE) và repo Python AI Service.
> Dùng để kiểm tra tích hợp, debug lỗi, và đảm bảo hai repo không bị lệch schema.
>
> **Cập nhật lần cuối:** 2026-04-18 _(merged từ cả hai team)_

---

## 1. Cấu hình kết nối

### 1.1 appsettings.Development.json (.NET)

```json
"PythonAi": {
  "BaseUrl": "http://127.0.0.1:8000",
  "InternalToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9",
  "TimeoutSeconds": 60,
  "LongTimeoutSeconds": 180,
  "StreamTimeoutSeconds": 300,
  "WebhookCallbackUrl": "http://localhost:5294/api/ai/evaluation/callback",
  "WebhookSigningSecret": "dev-secret456"
}
```

### 1.2 .env của Python (local dev)

```env
AISEP_INTERNAL_TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
REQUIRE_INTERNAL_AUTH=true
WEBHOOK_CALLBACK_URL=http://localhost:5294/api/ai/evaluation/callback
WEBHOOK_SIGNING_SECRET=dev-secret456
WEBHOOK_VERIFY_SSL=false
```

> ⚠️ **Trạng thái hiện tại:** `REQUIRE_INTERNAL_AUTH=false` — auth đang bị **tắt** ở local dev.
> Phải set `REQUIRE_INTERNAL_AUTH=true` trong `.env` để test auth flow thực sự trước khi merge.
> Khi deploy staging/prod **bắt buộc** set `true`.

---

## 2. Ba giá trị bắt buộc phải khớp nhau

> Đây là 3 điều kiện **cần và đủ** để hai repo gọi nhau thành công.

| Python env var           | .NET appsettings                | Giá trị (local dev)                                |
| ------------------------ | ------------------------------- | -------------------------------------------------- |
| `AISEP_INTERNAL_TOKEN`   | `PythonAi:InternalToken`        | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9`             |
| `WEBHOOK_SIGNING_SECRET` | `PythonAi:WebhookSigningSecret` | `dev-secret456`                                    |
| `WEBHOOK_CALLBACK_URL`   | `PythonAi:WebhookCallbackUrl`   | `http://localhost:5294/api/ai/evaluation/callback` |

---

## 3. Auth — Header .NET phải gửi kèm mọi request

```
X-Internal-Token: <giá_trị_AISEP_INTERNAL_TOKEN>
X-Correlation-Id: <guid>   ← tùy chọn, nhưng nên gửi để trace log cross-service
```

Nếu thiếu hoặc sai token → Python trả:

```json
HTTP 401
{
  "code": "AUTH_FAILED",
  "message": "Invalid or missing internal token.",
  "detail": null,
  "retryable": false,
  "correlation_id": "..."
}
```

> Python đọc `X-Correlation-Id` từ request và đính kèm vào mọi log + error response liên quan — dễ trace lỗi cross-service.

---

## 4. Danh sách tất cả endpoint Python được gọi từ .NET

> **Lưu ý serialization:** .NET serialize/deserialize JSON với `snake_case` (`JsonNamingPolicy.SnakeCaseLower`).
> Tất cả field name trong JSON phải là `snake_case`.

| .NET Method                       | HTTP | Python Endpoint                                                             | Auth | Timeout    |
| --------------------------------- | ---- | --------------------------------------------------------------------------- | ---- | ---------- |
| `IsHealthyAsync`                  | GET  | `/health`                                                                   | ❌   | 5s         |
| `SubmitEvaluationAsync`           | POST | `/api/v1/evaluations/`                                                      | ✅   | 60s        |
| `GetEvaluationStatusAsync`        | GET  | `/api/v1/evaluations/{id}`                                                  | ✅   | 60s        |
| `GetEvaluationReportAsync`        | GET  | `/api/v1/evaluations/{id}/report`                                           | ✅   | 60s        |
| `ReindexStartupAsync`             | POST | `/internal/recommendations/reindex/startup/{startupId}`                     | ✅   | 60s        |
| `ReindexInvestorAsync`            | POST | `/internal/recommendations/reindex/investor/{investorId}`                   | ✅   | 60s        |
| `GetStartupRecommendationsAsync`  | GET  | `/api/v1/recommendations/startups?investor_id={id}&top_n={n}`               | ✅   | 60s        |
| `GetMatchExplanationAsync`        | GET  | `/api/v1/recommendations/startups/{startupId}/explanation?investor_id={id}` | ✅   | 60s        |
| `InvestorAgentChatStreamRawAsync` | POST | `/api/v1/investor-agent/chat/stream`                                        | ✅   | 300s (SSE) |

> ⚠️ **Trailing slash:** `POST /api/v1/evaluations/` có dấu `/` cuối — FastAPI trả **307 redirect** nếu thiếu.
> .NET đã hardcode đúng URL trong `PythonAiClient.cs`.

---

## 5. Schemas chi tiết

### 5.1 `GET /health` — Health Check

**Response:**

```json
{ "status": "ok" }
```

---

### 5.2 `POST /api/v1/evaluations/` — Submit Evaluation

**Request .NET gửi:**

```json
{
  "startup_id": "123",
  "documents": [
    {
      "document_id": "456",
      "document_type": "PitchDeck",
      "file_url_or_path": "https://res.cloudinary.com/..."
    }
  ]
}
```

> `document_type` Python chấp nhận: `PitchDeck`, `BusinessPlan`, `pitch_deck`, `business_plan` (case-insensitive).

**Response Python trả:**

```json
{
  "evaluation_run_id": 42,
  "startup_id": "123",
  "status": "queued",
  "message": "Evaluation submitted successfully",
  "evaluation_mode": "pitch_deck_only",
  "documents": [...]
}
```

> .NET chỉ đọc `evaluation_run_id` (kiểu **`int`**), `status`, `message`. Các field còn lại là extra.

---

### 5.3 `GET /api/v1/evaluations/{id}` — Status Polling

**Response Python trả:**

```json
{
  "id": 42,
  "evaluation_run_id": 42,
  "startup_id": "123",
  "status": "processing",
  "submitted_at": "2026-04-18T10:00:00",
  "failure_reason": null,
  "overall_score": null,
  "overall_confidence": null,
  "evaluation_mode": "pitch_deck_only",
  "documents": [
    {
      "id": 1,
      "document_type": "pitch_deck",
      "status": "processing",
      "extraction_status": "done",
      "summary": "..."
    }
  ],
  "has_pitch_deck_result": false,
  "has_business_plan_result": false,
  "has_merged_result": false,
  "merge_status": null
}
```

> `status` hợp lệ: `queued` | `processing` | `completed` | `failed`
> `id` và `evaluation_run_id` có cùng giá trị — giữ cả hai để backward compat.

---

### 5.4 `GET /api/v1/evaluations/{id}/report` — Fetch Report

- Nếu **chưa sẵn sàng** → `HTTP 202` với error envelope (`code: "EVALUATION_NOT_READY"`, `retryable: true`)
- Nếu **đã có report** → `HTTP 200`:

```json
{
  "report_mode": "pitch_deck_only",
  "evaluation_mode": "pitch_deck_only",
  "has_merged_result": false,
  "available_sources": ["pitch_deck"],
  "source_document_type": null,
  "merge_status": null,
  "report": {
    "startup_id": "123",
    "status": "completed",
    "overall_result": { "...": "..." },
    "criteria_results": { "...": "..." },
    "classification": { "...": "..." },
    "narrative": { "...": "..." },
    "effective_weights": { "...": "..." },
    "processing_warnings": ["..."]
  }
}
```

> ⚠️ .NET đọc `$.report` (object lồng trong wrapper), **không phải** trực tiếp root object.

---

### 5.5 `GET /api/v1/evaluations/{id}/report/source/{document_type}` — Source Report (Combined Mode)

Dùng khi run được submit với mode `both` — lấy riêng report cho từng loại document.

**Path param `document_type`:** `pitch_deck` | `business_plan` _(strict snake_case — Python không normalize PascalCase, trả 400 nếu sai)_

**Response (200):** Cùng `ReportEnvelope` wrapper như `/report`, nhưng `report_mode` luôn là `"source"`:

```json
{
  "report_mode": "source",
  "evaluation_mode": "both",
  "has_merged_result": true,
  "available_sources": ["pitch_deck", "business_plan"],
  "report": {
    "startup_id": "123",
    "status": "completed",
    "overall_result": { "...": "..." },
    "criteria_results": { "...": "..." }
  }
}
```

**Error cases:**

| HTTP | Code                    | Điều kiện                                                                              |
| ---- | ----------------------- | -------------------------------------------------------------------------------------- |
| 400  | `INVALID_DOCUMENT_TYPE` | `document_type` không phải `pitch_deck` / `business_plan`                              |
| 404  | `NOT_FOUND`             | `run_id` không tồn tại                                                                 |
| 404  | `DOCUMENT_NOT_FOUND`    | Document không có trong run này (ví dụ: gọi `business_plan` cho run `pitch_deck_only`) |
| 202  | `EVALUATION_NOT_READY`  | Run chưa completed                                                                     |

> ✅ .NET endpoint tương ứng: `GET /api/ai-evaluation/runs/{runId}/report/source/{documentType}`
> .NET validate `documentType` trước khi gọi Python — trả `INVALID_DOCUMENT_TYPE` ngay nếu sai.

---

### 5.6 `POST /internal/recommendations/reindex/startup/{startupId}` — Reindex Startup

**Request body (.NET gửi):**

```json
{
  "startup_id": "123",
  "profile_version": "v1.0",
  "source_updated_at": "2026-04-18T10:00:00Z",
  "startup_name": "TechViet",
  "tagline": "AI for everyone",
  "stage": "Seed",
  "primary_industry": "FinTech",
  "sub_industry": "Payments",
  "description": "...",
  "location": "Ho Chi Minh City",
  "country": "Vietnam",
  "market_scope": "Regional",
  "product_status": "Beta",
  "problem_statement": "...",
  "solution_summary": "...",
  "funding_amount_sought": 500000.0,
  "current_funding_raised": 100000.0,
  "team_size": "11-50",
  "is_profile_visible_to_investors": true,
  "verification_label": "Verified",
  "account_active": true,
  "ai_evaluation_status": "completed",
  "ai_overall_score": 7.5,
  "ai_summary": "...",
  "ai_strength_tags": ["strong_team", "clear_market"],
  "ai_weakness_tags": ["low_traction"],
  "ai_dimension_scores": {
    "team": 8.0,
    "market": 7.0,
    "product": 6.5
  }
}
```

**Response Python trả:**

```json
{ "status": "ok", "message": "Startup reindexed successfully" }
```

---

### 5.6 `POST /internal/recommendations/reindex/investor/{investorId}` — Reindex Investor

**Request body (.NET gửi):**

```json
{
  "investor_name": "John Nguyen",
  "investor_type": "Angel",
  "organization": "VietVC",
  "role_title": "Partner",
  "location": "Ho Chi Minh City",
  "website": "https://vietvc.com",
  "verification_label": "Verified",
  "logo_url": "https://...",
  "short_thesis_summary": "...",
  "preferred_industries": ["FinTech", "EdTech"],
  "preferred_stages": ["Seed", "Series A"],
  "preferred_geographies": ["Vietnam", "SEA"],
  "preferred_market_scopes": ["Regional", "Global"],
  "preferred_product_maturity": ["Beta", "Live"],
  "preferred_validation_level": ["Revenue"],
  "preferred_strengths": ["strong_team"],
  "support_offered": ["Mentorship", "Network"],
  "require_verified_startups": true,
  "require_visible_profiles": true,
  "tags": ["impact_investing"],
  "preferred_ai_score_range": { "min": 6.0, "max": 10.0 },
  "ai_score_importance": "high",
  "accepting_connections_status": "open",
  "recently_active_badge": true,
  "avoid_text": "gambling, tobacco"
}
```

**Response Python trả:**

```json
{ "status": "ok", "message": "Investor reindexed successfully" }
```

---

### 5.7 `GET /api/v1/recommendations/startups` — Get Recommendations

**Query params:** `?investor_id=1&top_n=10`

**Response Python trả:**

```json
{
  "investor_id": "1",
  "matches": [
    {
      "investor_id": "1",
      "startup_id": "123",
      "startup_name": "TechViet",
      "final_match_score": 8.5,
      "structured_score": 7.0,
      "semantic_score": 9.0,
      "combined_pre_llm_score": 8.0,
      "rerank_adjustment": 0.5,
      "match_band": "HIGH",
      "fit_summary_label": "Strong Fit",
      "breakdown": { "...": "..." },
      "match_reasons": ["INDUSTRY_MATCH", "STAGE_MATCH"],
      "positive_reasons": ["..."],
      "caution_reasons": ["..."],
      "warning_flags": [],
      "generated_at": "2026-04-18T10:00:00"
    }
  ],
  "warnings": [],
  "generated_at": "2026-04-18T10:00:00"
}
```

> `match_band` hợp lệ: `LOW` | `MEDIUM` | `HIGH` | `VERY_HIGH` **(uppercase)**.
> ⚠️ **Breaking rename (2026-04-18):** Field này trước đây là `items` → đã đổi thành **`matches`**. .NET đã dùng đúng `matches`.

---

### 5.8 `GET /api/v1/recommendations/startups/{startupId}/explanation` — Match Explanation

**Query params:** `?investor_id=1`

**Response Python trả:**

```json
{
  "investor_id": "1",
  "startup_id": "123",
  "explanation": { "...": "..." },
  "generated_at": "2026-04-18T10:00:00"
}
```

> ⚠️ **Breaking rename (2026-04-18):** Field này trước đây là `result` → đã đổi thành **`explanation`**. .NET đã dùng đúng `explanation`.

---

### 5.9 `POST /api/v1/investor-agent/chat/stream` — SSE Stream Chat

**Request body:**

```json
{
  "query": "Tell me about FinTech startups in Vietnam",
  "thread_id": "thread-abc-123"
}
```

> `thread_id`: 1–128 ký tự, chỉ `[a-zA-Z0-9_-]`. Nếu `null` → Python dùng `"default_thread"`.

**Request headers:**

```
Accept: text/event-stream
X-Internal-Token: ...
```

**Response:** `Content-Type: text/event-stream; charset=utf-8`

**Thứ tự SSE events Python gửi:**

```
data: {"type": "progress", "node": "planner"}

data: {"type": "answer_chunk", "content": "Hello "}

data: {"type": "answer_chunk", "content": "world"}

data: {"type": "final_answer", "content": "Hello world"}

data: {"type": "final_metadata", "references": [...], "caveats": [...], "writer_notes": [...], "processing_warnings": [...], "grounding_summary": {...}}

data: [DONE]
```

**Các `type` hợp lệ:**

| type             | Khi nào                  | Fields                                                                              |
| ---------------- | ------------------------ | ----------------------------------------------------------------------------------- |
| `progress`       | Mỗi node graph chạy      | `node` (string)                                                                     |
| `answer_chunk`   | Từng chunk câu trả lời   | `content` (string)                                                                  |
| `final_answer`   | Toàn bộ câu trả lời cuối | `content` (string)                                                                  |
| `final_metadata` | Metadata kèm sau         | `references`, `caveats`, `writer_notes`, `processing_warnings`, `grounding_summary` |
| `error`          | Lỗi trong stream         | `content` (string), `correlation_id`                                                |

**Schema `references` item:**

```json
{
  "title": "Article Title",
  "url": "https://...",
  "source_domain": "techcrunch.com"
}
```

**Schema `grounding_summary`:**

```json
{
  "verified_claim_count": 5,
  "weakly_supported_claim_count": 2,
  "conflicting_claim_count": 0,
  "unsupported_claim_count": 1,
  "reference_count": 8,
  "coverage_status": "adequate"
}
```

> **Timeout:** Python hard timeout là **240s** (nhỏ hơn .NET `StreamTimeoutSeconds=300s`) để Python có thể gửi `error` event trước khi .NET timeout.
> **Bắt buộc:** Stream luôn kết thúc bằng `data: [DONE]`.

> 📌 **Research endpoint:** Python **không có** endpoint `/api/v1/investor-agent/research` riêng biệt.
> .NET delegate research về `/api/v1/investor-agent/chat/stream` với `thread_id=null` là **đúng** — Python tự routing internally qua LangGraph node.

---

## 6. Webhook — Python gọi về .NET

Python POST callback về .NET khi evaluation kết thúc (`completed` / `failed` / `partial`).

### Endpoint .NET nhận: `POST /api/ai/evaluation/callback`

### Headers Python gửi:

```
Content-Type: application/json
X-Signature: sha256=<HMAC-SHA256-hex>
X-Delivery-Id: <uuid-hex>
```

### Body:

```json
{
  "delivery_id": "<deterministic-uuid-hex>",
  "evaluation_run_id": 42,
  "startup_id": "123",
  "terminal_status": "Completed",
  "overall_score": 7.5,
  "failure_reason": null,
  "timestamp": "2026-04-18T10:00:00Z",
  "correlation_id": "optional-uuid"
}
```

> `terminal_status` hợp lệ: `Completed` | `Failed` | `Partial`
> `delivery_id` là **deterministic** (UUID5 từ `run_id + status`) — .NET có thể dùng để dedup nếu Python retry.
> Python retry tối đa **3 lần** với exponential backoff: 1s → 2s → 4s, timeout mỗi attempt **10s**. Tổng tối đa ~**17s**.
> ⚠️ **.NET phải config receiver timeout ≥ 30s** cho endpoint `/api/ai/evaluation/callback` để không bị timeout trước khi nhận đủ retry.

### Cách tính `X-Signature` (Python ký, .NET verify):

```python
import hmac, hashlib, json

body = json.dumps(payload, separators=(',', ':'))  # compact JSON, không có space
sig = hmac.new(secret.encode(), body.encode(), hashlib.sha256).hexdigest()
header = f"sha256={sig}"
```

> ⚠️ .NET phải tính HMAC trên **raw byte body nhận được**, **không** reformat hay re-serialize lại.

---

## 7. Error Envelope chuẩn

Mọi lỗi từ Python đều theo format:

```json
{
  "code": "EVALUATION_NOT_FOUND",
  "message": "Evaluation run 42 not found.",
  "detail": null,
  "retryable": false,
  "correlation_id": "abc-123"
}
```

| Field            | Type               | Bắt buộc       |
| ---------------- | ------------------ | -------------- |
| `code`           | string             | ✅ (non-empty) |
| `message`        | string             | ✅             |
| `detail`         | object hoặc string | ❌             |
| `retryable`      | boolean            | ✅             |
| `correlation_id` | string             | ❌             |

**Tất cả `code` Python có thể trả:**

| Code                      | HTTP | Ý nghĩa                                   | Retryable |
| ------------------------- | ---- | ----------------------------------------- | --------- |
| `AUTH_FAILED`             | 401  | Token sai hoặc thiếu                      | ❌        |
| `EVALUATION_NOT_FOUND`    | 404  | Run ID không tồn tại                      | ❌        |
| `EVALUATION_NOT_READY`    | 202  | Report chưa sẵn sàng                      | ✅        |
| `EVALUATION_FAILED`       | 409  | Evaluation thất bại, không có report      | ❌        |
| `INVESTOR_NOT_FOUND`      | 404  | Investor chưa được reindex                | ❌        |
| `REINDEX_STARTUP_FAILED`  | 500  | Lỗi khi reindex startup                   | ✅        |
| `REINDEX_INVESTOR_FAILED` | 500  | Lỗi khi reindex investor                  | ✅        |
| `AGENT_STREAM_ERROR`      | —    | Lỗi trong SSE stream (event `type=error`) | ✅        |
| `RATE_LIMIT_EXCEEDED`     | 429  | Quá rate limit                            | ✅        |
| `PYTHON_AI_ERROR`         | any  | Generic fallback                          | ❌        |

---

## 8. Rate Limits (Python-side)

| Endpoint group    | Limit mặc định | Env var để thay đổi     |
| ----------------- | -------------- | ----------------------- |
| Evaluation submit | 20 req/phút    | `RATE_LIMIT_EVAL_RPM`   |
| Recommendations   | 60 req/phút    | `RATE_LIMIT_RECO_RPM`   |
| Chat stream       | 30 req/phút    | `RATE_LIMIT_STREAM_RPM` |

Khi bị throttle → `HTTP 429` với error envelope chuẩn (`code: "RATE_LIMIT_EXCEEDED"`, `retryable: true`).

---

## 9. Checklist tích hợp — Kiểm tra nhanh

### 9.1 Checklist bật tích hợp

```
[ ] AISEP_INTERNAL_TOKEN    == PythonAi:InternalToken
[ ] WEBHOOK_SIGNING_SECRET  == PythonAi:WebhookSigningSecret
[ ] WEBHOOK_CALLBACK_URL    == http://localhost:5294/api/ai/evaluation/callback
[ ] REQUIRE_INTERNAL_AUTH   = true  (staging/prod), false OK ở dev
[ ] WEBHOOK_VERIFY_SSL      = false (nếu .NET dùng self-signed cert ở dev)
```

### 9.2 Checklist curl kiểm tra từng luồng

**Health check:**

```bash
curl http://127.0.0.1:8000/health
# Expect: {"status": "ok"}
```

**Internal token:**

```bash
curl -H "X-Internal-Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" \
     "http://127.0.0.1:8000/api/v1/recommendations/startups?investor_id=1&top_n=5"
# Expect: 200, không phải 401/403
```

**Submit evaluation:**

```bash
curl -X POST http://127.0.0.1:8000/api/v1/evaluations/ \
  -H "Content-Type: application/json" \
  -H "X-Internal-Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" \
  -d '{"startup_id":"1","documents":[]}'
# Expect: {"evaluation_run_id": <int>, "status": "queued", ...}
```

**Reindex startup:**

```bash
curl -X POST http://127.0.0.1:8000/internal/recommendations/reindex/startup/1 \
  -H "Content-Type: application/json" \
  -H "X-Internal-Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" \
  -d '{"startup_id":"1","startup_name":"Test","source_updated_at":"2026-04-18T00:00:00Z","profile_version":"v1","account_active":true,"is_profile_visible_to_investors":true}'
# Expect: {"status": "ok"}
```

**SSE stream:**

```bash
curl -X POST http://127.0.0.1:8000/api/v1/investor-agent/chat/stream \
  -H "Content-Type: application/json" \
  -H "Accept: text/event-stream" \
  -H "X-Internal-Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" \
  -d '{"query":"hello","thread_id":null}'
# Expect: text/event-stream, dòng "data: {...}" và kết thúc "data: [DONE]"
```

**Webhook từ Python về .NET:**

```bash
# .NET phải đang chạy trên port 5294
curl -X POST http://localhost:5294/api/ai/evaluation/callback \
  -H "Content-Type: application/json" \
  -H "X-Signature: sha256=<tính theo section 6>" \
  -d '{"delivery_id":"test-1","evaluation_run_id":1,"startup_id":"1","terminal_status":"Completed","overall_score":7.5,"failure_reason":null,"timestamp":"2026-04-18T10:00:00Z"}'
# Expect: 200 OK
```

---

## 10. Những điểm hay bị sai khi tích hợp

| Vấn đề                                               | Nguyên nhân                                   | Cách fix                                                                                          |
| ---------------------------------------------------- | --------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `HTTP 401 AUTH_FAILED`                               | Token không khớp giữa hai repo                | So sánh `AISEP_INTERNAL_TOKEN` vs `PythonAi:InternalToken`                                        |
| `HTTP 307 Redirect` trên `POST /api/v1/evaluations/` | Gọi không có trailing slash                   | URL phải kết thúc bằng `/`                                                                        |
| `HTTP 422` từ Python                                 | Field name dùng camelCase thay vì snake_case  | .NET luôn gửi snake_case — kiểm tra JSON body thực tế                                             |
| Webhook bị reject `400`                              | HMAC signature sai                            | .NET tính HMAC trên **raw bytes nhận được**, không reformat. Python dùng `separators=(',', ':')`  |
| `evaluation_run_id` bị `null` ở .NET                 | Python trả string `"42"` thay vì int `42`     | Python phải trả kiểu `int`                                                                        |
| Report trả `null` ở .NET                             | .NET đọc root thay vì `$.report`              | .NET phải deserialize `response.report`, không phải root object                                   |
| SSE stream không kết thúc / timeout                  | Python không gửi `data: [DONE]`               | Python bắt buộc gửi `[DONE]` cuối stream. Python hard timeout 240s < .NET 300s                    |
| Reindex lỗi silent                                   | Python trả non-2xx nhưng .NET fire-and-forget | Kiểm tra Serilog log `logs/aisep-{date}.log` — tìm `ReindexStartupAsync` / `ReindexInvestorAsync` |
| `429 RATE_LIMIT_EXCEEDED`                            | Quá rate limit của Python                     | Giảm concurrency hoặc tăng giới hạn qua env var                                                   |
| `EVALUATION_NOT_READY` (202) bị throw exception      | .NET không check 202 trước khi deserialize    | `GetEvaluationReportAsync` đã handle: trả `(null, 202)` nếu status là 202                         |
