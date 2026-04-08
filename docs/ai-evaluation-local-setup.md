# AI Evaluation Local Integration — Setup & Test Runbook

> Step A: Evaluation integration between .NET WebAPI and Python AI Service

---

## Prerequisites

- .NET 8 SDK
- PostgreSQL running locally (port 5432)
- Python 3.11+ (for the AI service)
- Redis (for Celery broker)

---

## 1. Run Redis Locally

### Docker (recommended)

```bash
docker run -d --name redis -p 6379:6379 redis:7-alpine
```

### WSL alternative

```bash
sudo apt install redis-server && sudo service redis-server start
```

Verify: `redis-cli ping` → `PONG`

---

## 2. Run the Python AI Service

```bash
cd <python-ai-repo>

# Create virtualenv & activate
python -m venv .venv
.venv\Scripts\activate          # Windows
# source .venv/bin/activate     # Linux/Mac

pip install -r requirements.txt

# Configure .env (minimum):
#   DATABASE_URL=postgresql://postgres:12345@localhost:5432/aisep_ai
#   CELERY_BROKER_URL=redis://localhost:6379/0
#   CELERY_RESULT_BACKEND=redis://localhost:6379/0
#   GEMINI_API_KEY=<your-key>
#
#   Optional webhook:
#   WEBHOOK_CALLBACK_URL=http://127.0.0.1:5294/api/ai/evaluation/callback
#   WEBHOOK_SIGNING_SECRET=dev-webhook-secret-change-me

# DB migrations
alembic upgrade head

# Start FastAPI
uvicorn src.main:app --host 127.0.0.1 --port 8000 --reload
```

Verify: `curl http://127.0.0.1:8000/health` → `{"status":"ok"}`

---

## 3. Run the Celery Worker

Separate terminal, same venv:

```bash
# Windows (solo pool):
celery -A src.modules.evaluation.workers.tasks worker --pool=solo --concurrency=1 --loglevel=info

# Linux/Mac:
celery -A src.modules.evaluation.workers.tasks worker --loglevel=info
```

---

## 4. Run .NET WebAPI

```bash
cd AISEP_Capstone-BE

# One-time: create EF migration for new AI tables
cd src/AISEP.WebAPI
dotnet ef migrations add AddAiEvaluationTables --project ../AISEP.Infrastructure

# Run
dotnet run --project src/AISEP.WebAPI --urls "http://localhost:5294"
```

The migration creates:

- `AiEvaluationRuns` — evaluation tracking
- `AiWebhookDeliveries` — webhook idempotency

Configuration in `appsettings.Development.json` (already set):

```json
{
  "PythonAi": {
    "BaseUrl": "http://127.0.0.1:8000",
    "InternalToken": "dev-internal-token",
    "TimeoutSeconds": 60,
    "WebhookCallbackUrl": "http://127.0.0.1:5294/api/ai/evaluation/callback",
    "WebhookSigningSecret": "dev-webhook-secret-change-me"
  }
}
```

---

## 5. End-to-End Test: Submit → Poll → Report

### Authenticate

```bash
curl -X POST http://localhost:5294/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"startup@test.com","password":"password123"}'

# Save the accessToken
TOKEN="<paste-token>"
```

### Submit Evaluation

```bash
curl -X POST http://localhost:5294/api/ai/evaluation/submit \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"startupId": 1}'
```

Expected:

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "data": { "runId": 1, "startupId": 1, "status": "queued" }
}
```

### Poll Status

```bash
curl http://localhost:5294/api/ai/evaluation/1/status \
  -H "Authorization: Bearer $TOKEN"
```

Poll until `status` is `completed` or `failed`.

### Get Report

```bash
curl http://localhost:5294/api/ai/evaluation/1/report \
  -H "Authorization: Bearer $TOKEN"
```

Returns full canonical report with `isReportValid: true|false`.

---

## 6. Test Webhook Callback

If Python has `WEBHOOK_CALLBACK_URL` configured, it will auto-POST to .NET when evaluation completes.

### Manual test (simulate):

```python
import hmac, hashlib, json
payload = json.dumps({
    "delivery_id": "test-001",
    "evaluation_run_id": 1,
    "startup_id": "1",
    "terminal_status": "completed",
    "overall_score": 7.5,
    "failure_reason": None,
    "timestamp": "2026-04-08T00:00:00Z",
    "correlation_id": "test-corr"
})
sig = hmac.new(b"dev-webhook-secret-change-me", payload.encode(), hashlib.sha256).hexdigest()
print(f"Signature: {sig}")
```

```bash
curl -X POST http://localhost:5294/api/ai/evaluation/callback \
  -H "Content-Type: application/json" \
  -H "X-Webhook-Signature: <paste-sig>" \
  -d '<paste-payload>'
```

Expected: HTTP 200

---

## 7. Failure Test Cases

| Scenario                           | Expected                                |
| ---------------------------------- | --------------------------------------- |
| Python service down → submit       | Error: AI service connection refused    |
| Invalid HMAC signature on callback | 401 Unauthorized                        |
| Duplicate `delivery_id` webhook    | 200 OK (silently idempotent)            |
| Submit for non-owned startup       | Error: ACCESS_DENIED                    |
| Poll non-existent runId            | Error: NOT_FOUND                        |
| Report before completion           | `isReportValid: false`, "not ready yet" |

---

## 8. Config Environment Variables

All configurable via `appsettings` or env vars (`PythonAi__BaseUrl` etc.):

| Key                             | Default                 | Required                 |
| ------------------------------- | ----------------------- | ------------------------ |
| `PythonAi:BaseUrl`              | `http://127.0.0.1:8000` | Yes                      |
| `PythonAi:InternalToken`        | `""`                    | For protected endpoints  |
| `PythonAi:TimeoutSeconds`       | `30`                    | No                       |
| `PythonAi:WebhookCallbackUrl`   | `""`                    | For webhook (optional)   |
| `PythonAi:WebhookSigningSecret` | `""`                    | For webhook verification |
| `PythonAi:MaxRetries`           | `3`                     | No                       |
| `PythonAi:RetryBaseDelayMs`     | `500`                   | No                       |

---

## 9. What's Next

- **Step B**: Recommendation integration (`IAiRecommendationService`)
- **Step C**: Investor Agent integration (`IAiInvestorAgentService`)
- **Staging**: Replace URLs, enable auth, add Polly resilience policies, health probes
