using AISEP.Application.Configuration;
using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// AI Evaluation endpoints — proxies to the Python AI Service.
/// Submit, poll status, fetch report, and receive webhook callbacks.
/// </summary>
[ApiController]
[Route("api/ai/evaluation")]
[Tags("AI Evaluation")]
public class AiEvaluationController : ControllerBase
{
    private readonly IAiEvaluationService _service;
    private readonly PythonAiOptions _options;
    private readonly ILogger<AiEvaluationController> _logger;

    public AiEvaluationController(
        IAiEvaluationService service,
        IOptions<PythonAiOptions> options,
        ILogger<AiEvaluationController> logger)
    {
        _service = service;
        _options = options.Value;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>
    /// Submit a new AI evaluation for a startup.
    /// The startup must belong to the current user.
    /// </summary>
    [HttpPost("submit")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiEnvelope<EvaluationSubmitResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitEvaluation([FromBody] SubmitEvaluationRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.SubmitEvaluationAsync(userId, request);
        return result.ToEnvelope();
    }

    /// <summary>
    /// Get the status of an evaluation run.
    /// Polls the Python AI Service if the run is not yet terminal.
    /// </summary>
    [HttpGet("{runId:int}/status")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<EvaluationStatusResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(int runId)
    {
        var userId = GetCurrentUserId();
        var userType = User.FindFirst("userType")?.Value ?? string.Empty;
        var result = await _service.GetEvaluationStatusAsync(runId, userId, userType);
        return result.ToEnvelope();
    }

    /// <summary>
    /// Get the full canonical evaluation report.
    /// Returns the report from cache if available, otherwise fetches from Python.
    /// </summary>
    [HttpGet("{runId:int}/report")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<EvaluationReportResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(int runId)
    {
        var userId = GetCurrentUserId();
        var userType = User.FindFirst("userType")?.Value ?? string.Empty;
        var result = await _service.GetEvaluationReportAsync(runId, userId, userType);
        return result.ToEnvelope();
    }

    /// <summary>
    /// Get the source-specific report for a single document type (combined-mode runs only).
    /// <paramref name="documentType"/> must be snake_case: <c>pitch_deck</c> or <c>business_plan</c>.
    /// Returns 404 if the document was not part of this run.
    /// </summary>
    [HttpGet("{runId:int}/report/source/{documentType}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<EvaluationReportResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSourceReport(int runId, string documentType)
    {
        var userId = GetCurrentUserId();
        var userType = User.FindFirst("userType")?.Value ?? string.Empty;
        var result = await _service.GetSourceReportAsync(runId, documentType, userId, userType);
        return result.ToEnvelope();
    }

    /// <summary>
    /// Get evaluation history for a startup.
    /// </summary>
    [HttpGet("history/{startupId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<List<EvaluationStatusResult>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(int startupId)
    {
        var userId = GetCurrentUserId();
        var userType = User.FindFirst("userType")?.Value ?? string.Empty;
        var result = await _service.GetEvaluationHistoryAsync(startupId, userId, userType);
        return result.ToEnvelope();
    }

    /// <summary>
    /// Webhook callback endpoint — called by the Python AI Service when an evaluation reaches terminal status.
    /// NOT for frontend use. Requires valid HMAC-SHA256 signature.
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> WebhookCallback()
    {
        // 1. Read raw body bytes for signature verification
        Request.EnableBuffering();
        byte[] bodyBytes;
        using (var ms = new MemoryStream())
        {
            await Request.Body.CopyToAsync(ms);
            bodyBytes = ms.ToArray();
        }
        Request.Body.Position = 0;

        // 2. Verify HMAC signature
        var signatureHeader = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(_options.WebhookSigningSecret))
        {
            _logger.LogWarning("Webhook received but WebhookSigningSecret is not configured. Rejecting.");
            return Unauthorized();
        }

        // Dev bypass: if secret starts with "dev-" skip signature check (local testing only)
        bool devBypass = _options.WebhookSigningSecret.StartsWith("dev-", StringComparison.OrdinalIgnoreCase);
        if (devBypass)
        {
            _logger.LogWarning("⚠️  Webhook signature check BYPASSED (dev mode). Do NOT use in production.");
        }
        else
        {
            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogWarning("Webhook received without X-Webhook-Signature header.");
                return Unauthorized();
            }

            if (!VerifyHmacSignature(bodyBytes, signatureHeader, _options.WebhookSigningSecret))
            {
                _logger.LogWarning("Webhook HMAC signature verification failed.");
                return Unauthorized();
            }
        }

        // 3. Deserialize payload
        EvaluationWebhookPayload? payload;
        try
        {
            var bodyStr = Encoding.UTF8.GetString(bodyBytes);
            payload = System.Text.Json.JsonSerializer.Deserialize<EvaluationWebhookPayload>(bodyStr,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize webhook payload.");
            return BadRequest("Invalid payload.");
        }

        if (payload == null || string.IsNullOrEmpty(payload.DeliveryId))
        {
            _logger.LogWarning("Webhook payload is null or missing delivery_id.");
            return BadRequest("Missing delivery_id.");
        }

        // 4. Process (idempotent)
        try
        {
            await _service.ProcessWebhookAsync(payload);
        }
        catch (Exception ex)
        {
            // Log but return 200 — Python should not retry on processing errors
            _logger.LogError(ex, "Error processing webhook delivery {DeliveryId}", payload.DeliveryId);
        }

        return Ok();
    }

    /// <summary>
    /// Constant-time HMAC-SHA256 signature verification.
    /// Expected signature format: hex digest of HMAC-SHA256(secret, rawBody).
    /// </summary>
    private static bool VerifyHmacSignature(byte[] body, string receivedSignature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = hmac.ComputeHash(body);
        var computedHex = Convert.ToHexString(computed).ToLowerInvariant();

        // Constant-time comparison
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(receivedSignature.ToLowerInvariant()));
    }
}
