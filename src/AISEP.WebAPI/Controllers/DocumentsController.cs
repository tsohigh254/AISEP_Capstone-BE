using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Document;
using AISEP.Application.Interfaces;
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
        [FromForm] DocumentUploadForm form,
        CancellationToken ct = default)
    {
        var request = new DocumentCreateRequest
        {
            DocumentType = form.DocumentType,
            Title = form.Title,
            Version = form.Version
        };

        var userId = GetCurrentUserId();
        var result = await _documentService.UploadAsync(form.File, request, userId, ct);

        if (!result.Success)
            return BadRequest(result);

        return StatusCode(StatusCodes.Status201Created, result);
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
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DocumentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDocuments(
        [FromQuery] string? documentType = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] string? q = null,
        [FromQuery] string sortBy = "uploadedAt",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetMyDocumentsAsync(userId, documentType, isArchived, q, sortBy, page, pageSize, ct);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
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

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    // ================================================================
    // 4) GET /api/documents/{documentId}/download — Download file
    // ================================================================

    /// <summary>
    /// Download the physical file for a document (owner only).
    /// Returns the file stream with correct Content-Type and Content-Disposition.
    /// </summary>
    [HttpGet("{documentId:int}/download")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int documentId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.DownloadMyDocumentAsync(documentId, userId, ct);

        if (!result.Success)
            return NotFound(ApiResponse<string>.ErrorResponse(result.Error!.Code, result.Error.Message));

        var (stream, contentType, fileName) = result.Data;
        return File(stream, contentType, fileName);
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

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
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

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
