using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Document;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Document management for Startup owners.
/// Upload, list, view, download, update metadata, and soft-delete (archive) documents.
/// </summary>
[ApiController]
[Route("api/documents")]
[Tags("Documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetCurrentUserType()
        => User.FindFirst("userType")?.Value ?? string.Empty;

    // ================================================================
    // 1) POST /api/documents — Upload document (multipart/form-data)
    // ================================================================

    /// <summary>
    /// Upload a document for the current startup.
    /// </summary>
    /// <remarks>
    /// Use multipart/form-data. Fields:
    /// - **file** (required): PDF, PPT/PPTX, DOC/DOCX — max 20 MB
    /// - **documentType** (required): PitchDeck | BusinessPlan | Financials | Legal | Other
    /// - **title** (optional): custom title; defaults to file name
    /// - **version** (optional): version string; auto-incremented if omitted
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = "StartupOnly")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(
        [FromForm] DocumentCreateRequest createRequest,
        CancellationToken ct = default)
    {     
        var userId = GetCurrentUserId();
        var result = await _documentService.UploadAsync(createRequest, userId, ct);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ================================================================
    // 2) GET /api/documents — List my documents
    // ================================================================

    /// <summary>
    /// List documents belonging to the current startup.
    /// </summary>
    /// <param name="documentType">Filter by type: PitchDeck, BusinessPlan, etc.</param>
    /// <param name="isArchived">Filter by archived state (default: false / non-archived only).</param>
    /// <param name="q">Keyword search in title/filename.</param>
    /// <param name="sortBy">Sort field: uploadedAt (default) | title | version.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    [HttpGet]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DocumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDocuments(
        [FromQuery] bool? isArchived = false,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetMyDocumentsAsync(userId, isArchived, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 3) GET /api/documents/{documentId} — Get document metadata
    // ================================================================

    /// <summary>
    /// Get full metadata for a specific document (owner only).
    /// </summary>
    [HttpGet("{documentId:int}")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(int documentId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetMyDocumentAsync(documentId, userId, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // GET /api/startups/{startupId}/documents — List startup's docs visible to caller
    // ================================================================

    /// <summary>
    /// List a startup's documents filtered by the caller's role against each document's Visibility flags
    /// (SRS UC54 — Investor/Advisor browse shared docs).
    /// Owner + Staff/Admin see all non-archived docs; other roles see only those sharing a visibility flag.
    /// </summary>
    [HttpGet("/api/startups/{startupId:int}/documents")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStartupDocuments(int startupId, CancellationToken ct = default)
    {
        var result = await _documentService.GetStartupDocumentsAsync(
            startupId, GetCurrentUserId(), GetCurrentUserType(), ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 4) GET /api/documents/{documentId}/view — Cross-role view (visibility-aware)
    // ================================================================

    /// <summary>
    /// Get a document as any authenticated user (Investor/Advisor/other Startup).
    /// Access is controlled by the document's Visibility flags (SRS §2584/2749).
    /// Returns 404 if not found; 403 wrapped if the user's role isn't allowed.
    /// </summary>
    [HttpGet("{documentId:int}/view")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewDocument(int documentId, CancellationToken ct = default)
    {
        var result = await _documentService.GetViewableDocumentAsync(
            documentId, GetCurrentUserId(), GetCurrentUserType(), ct);
        return result.ToActionResult();
    }

    // ================================================================
    // GET /api/documents/{documentId}/download — Download file (visibility-aware)
    // ================================================================

    /// <summary>
    /// Download a document's file. Access enforced by DocumentVisibility flags
    /// (owner + Staff/Admin bypass; Investor/Advisor/Public by flag match).
    /// </summary>
    [HttpGet("{documentId:int}/download")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int documentId, CancellationToken ct = default)
    {
        var result = await _documentService.DownloadDocumentAsync(
            documentId, GetCurrentUserId(), GetCurrentUserType(), ct);

        if (!result.Success) return result.ToErrorResult();

        var payload = result.Data!;
        return File(payload.Content, payload.ContentType, payload.FileName);
    }

    // ================================================================
    // GET /api/documents/{documentId}/content — Inline stream for in-app preview
    // ================================================================

    /// <summary>
    /// Stream a document's file inline (for in-app preview via authenticated fetch + blob URL).
    /// Same visibility rules as /download. Frontend MUST call with Bearer token —
    /// leaked URL alone returns 401, so the file never escapes the system.
    /// </summary>
    [HttpGet("{documentId:int}/content")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(int documentId, CancellationToken ct = default)
    {
        var result = await _documentService.DownloadDocumentAsync(
            documentId, GetCurrentUserId(), GetCurrentUserType(), ct);

        if (!result.Success) return result.ToErrorResult();

        var payload = result.Data!;
        // Omit fileName → ASP.NET Core sends Content-Disposition: inline,
        // letting browsers preview PDFs/images natively in iframe/blob.
        return File(payload.Content, payload.ContentType);
    }

    // ================================================================
    // 5) PUT /api/documents/{documentId}/metadata — Update metadata
    // ================================================================

    /// <summary>
    /// Update metadata (title, documentType, isArchived) for a document.
    /// </summary>
    [HttpPut("{documentId:int}/metadata")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMetadata(
        int documentId,
        [FromBody] DocumentUpdateMetadataRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.UpdateMetadataAsync(documentId, request, userId, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 6) DELETE /api/documents/{documentId} — Soft delete (archive)
    // ================================================================

    /// <summary>
    /// Soft-delete a document by marking it as archived.
    /// The physical file is retained for audit purposes.
    /// </summary>
    [HttpDelete("{documentId:int}")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int documentId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.ArchiveAsync(documentId, userId, ct);
        return result.ToDeletedEnvelope("Document archived");
    }

    // ================================================================
    // 7) POST /api/documents/{documentId}/versions — Upload new version
    // ================================================================

    /// <summary>
    /// Upload a new version of an existing document.
    /// The new version is linked to the original document and auto-increments the version number.
    /// </summary>
    [HttpPost("{documentId:int}/versions")]
    [Authorize(Policy = "StartupOnly")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadNewVersion(
        int documentId,
        [FromForm] DocumentUploadNewVersionRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.UploadNewVersionAsync(documentId, request, userId, ct);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ================================================================
    // 8) GET /api/documents/{documentId}/versions — Get version history
    // ================================================================

    /// <summary>
    /// Get all versions of a document, ordered by newest first.
    /// Pass any version's ID to see the full history chain.
    /// </summary>
    [HttpGet("{documentId:int}/versions")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentVersionHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentVersionHistoryDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionHistory(int documentId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetVersionHistoryAsync(documentId, userId, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 9) GET /api/documents/{documentId}/access-logs — Access logs (owner only)
    // ================================================================

    /// <summary>
    /// Get access logs for a document. Only the startup owner can view who has accessed their documents.
    /// (SRS p.5174 — "Access logs should be viewable by the startup")
    /// </summary>
    [HttpGet("{documentId:int}/access-logs")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentAccessLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentAccessLogDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccessLogs(int documentId, CancellationToken ct = default)
    {
        var result = await _documentService.GetDocumentAccessLogsAsync(documentId, GetCurrentUserId(), ct);
        return result.ToActionResult();
    }

    // ================================================================
    // Staff review endpoints
    // ================================================================

    /// <summary>Staff verifies a document.</summary>
    [HttpPost("/api/staff/documents/{documentId:int}/verify")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> StaffVerify(int documentId, [FromBody] StaffReviewDocumentRequest request, CancellationToken ct = default)
    {
        var result = await _documentService.StaffVerifyAsync(documentId, GetCurrentUserId(), request.Notes, ct);
        return result.ToActionResult();
    }

    /// <summary>Staff approves a document.</summary>
    [HttpPost("/api/staff/documents/{documentId:int}/approve")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> StaffApprove(int documentId, [FromBody] StaffReviewDocumentRequest request, CancellationToken ct = default)
    {
        var result = await _documentService.StaffApproveAsync(documentId, GetCurrentUserId(), request.Notes, ct);
        return result.ToActionResult();
    }

    /// <summary>Staff rejects a document.</summary>
    [HttpPost("/api/staff/documents/{documentId:int}/reject")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> StaffReject(int documentId, [FromBody] StaffReviewDocumentRequest request, CancellationToken ct = default)
    {
        var result = await _documentService.StaffRejectAsync(documentId, GetCurrentUserId(), request.Notes, ct);
        return result.ToActionResult();
    }
}
