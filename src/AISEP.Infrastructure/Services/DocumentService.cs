using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Document;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storage;
    private readonly IAuditService _audit;
    private readonly ILogger<DocumentService> _logger;

    // MVP file constraints
    private const long MaxFileSize = 20 * 1024 * 1024; // 20 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".ppt", ".pptx", ".doc", ".docx"
    };

    public DocumentService(
        ApplicationDbContext context,
        IStorageService storage,
        IAuditService audit,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _storage = storage;
        _audit = audit;
        _logger = logger;
    }

    // ================================================================
    // Upload
    // ================================================================
    public async Task<ApiResponse<DocumentDto>> UploadAsync(
        IFormFile file, DocumentCreateRequest request, int userId, CancellationToken ct = default)
    {
        // 1. Validate file
        var fileValidation = ValidateFile(file);
        if (fileValidation != null)
            return ApiResponse<DocumentDto>.ErrorResponse("INVALID_FILE", fileValidation);

        // 2. Lookup startup for current user
        var startup = await _context.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId, ct);

        if (startup == null)
            return ApiResponse<DocumentDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You must create a startup profile before uploading documents.");

        // 3. Auto-version if not provided
        var version = request.Version;
        if (string.IsNullOrWhiteSpace(version))
        {
            var maxVersion = await _context.Documents
                .Where(d => d.StartupID == startup.StartupID
                            && d.DocumentType == request.DocumentType
                            && !d.IsArchived)
                .Select(d => d.Version)
                .ToListAsync(ct);

            var maxNum = maxVersion
                .Select(v => int.TryParse(v, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            version = (maxNum + 1).ToString();
        }

        // 4. Save file to storage
        var folder = $"startups/{startup.StartupID}/documents";
        await using var stream = file.OpenReadStream();
        var stored = await _storage.SaveAsync(stream, file.FileName, folder, ct);

        // 5. Create DB record
        var title = string.IsNullOrWhiteSpace(request.Title) ? file.FileName : request.Title;
        var ext = Path.GetExtension(file.FileName);

        var document = new Document
        {
            StartupID = startup.StartupID,
            DocumentType = request.DocumentType,
            FileName = title,
            FileURL = stored.Key,
            FileSize = (int)stored.Size,
            FileFormat = stored.ContentType,
            Version = version,
            IsAnalyzed = false,
            AnalysisStatus = "NotAnalyzed",
            IsArchived = false,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("UPLOAD_DOCUMENT", "Document", document.DocumentID,
            $"Uploaded '{title}' ({request.DocumentType} v{version}) for startup {startup.StartupID}");

        _logger.LogInformation("Document {DocumentID} uploaded for startup {StartupID}", document.DocumentID, startup.StartupID);

        return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(document), "Document uploaded successfully");
    }

    // ================================================================
    // List my documents
    // ================================================================
    public async Task<ApiResponse<PagedResponse<DocumentListItemDto>>> GetMyDocumentsAsync(
        int userId, string? documentType, bool? isArchived, string? q,
        string sortBy, int page, int pageSize, CancellationToken ct = default)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId, ct);

        if (startup == null)
            return ApiResponse<PagedResponse<DocumentListItemDto>>.ErrorResponse(
                "STARTUP_PROFILE_NOT_FOUND", "You must create a startup profile first.");

        var query = _context.Documents
            .AsNoTracking()
            .Where(d => d.StartupID == startup.StartupID);

        // Filters
        if (!string.IsNullOrWhiteSpace(documentType))
            query = query.Where(d => d.DocumentType == documentType);

        if (isArchived.HasValue)
            query = query.Where(d => d.IsArchived == isArchived.Value);
        else
            query = query.Where(d => !d.IsArchived); // default: hide archived

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(d => EF.Functions.ILike(d.FileName, $"%{q}%"));

        // Sort
        query = sortBy?.ToLowerInvariant() switch
        {
            "title" => query.OrderBy(d => d.FileName),
            "version" => query.OrderBy(d => d.Version),
            _ => query.OrderByDescending(d => d.UploadedAt)
        };

        // Paging
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalItems = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentListItemDto
            {
                DocumentID = d.DocumentID,
                DocumentType = d.DocumentType,
                Title = d.FileName,
                Version = d.Version,
                FileSize = d.FileSize,
                ContentType = d.FileFormat,
                IsArchived = d.IsArchived,
                AnalysisStatus = d.AnalysisStatus,
                UploadedAt = d.UploadedAt
            })
            .ToListAsync(ct);

        return ApiResponse<PagedResponse<DocumentListItemDto>>.SuccessResponse(
            new PagedResponse<DocumentListItemDto>
            {
                Items = items,
                Paging = new PagingInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                }
            });
    }

    // ================================================================
    // Get single document (owner)
    // ================================================================
    public async Task<ApiResponse<DocumentDto>> GetMyDocumentAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<DocumentDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found.");

        return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(doc));
    }

    // ================================================================
    // Download
    // ================================================================
    public async Task<ApiResponse<(Stream Stream, string ContentType, string FileName)>> DownloadMyDocumentAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<(Stream, string, string)>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found.");

        try
        {
            var stream = await _storage.OpenReadAsync(doc.FileURL, ct);
            var contentType = doc.FileFormat ?? "application/octet-stream";
            var fileName = doc.FileName;
            return ApiResponse<(Stream, string, string)>.SuccessResponse((stream, contentType, fileName));
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Physical file missing for Document {DocumentID}, path: {Path}", doc.DocumentID, doc.FileURL);
            return ApiResponse<(Stream, string, string)>.ErrorResponse("FILE_MISSING",
                "The physical file could not be found. Please re-upload.");
        }
    }

    // ================================================================
    // Update metadata
    // ================================================================
    public async Task<ApiResponse<DocumentDto>> UpdateMetadataAsync(
        int documentId, DocumentUpdateMetadataRequest request, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentTrackedAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<DocumentDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found.");

        if (request.Title != null) doc.FileName = request.Title;
        if (request.DocumentType != null) doc.DocumentType = request.DocumentType;
        if (request.IsArchived.HasValue)
        {
            doc.IsArchived = request.IsArchived.Value;
            doc.ArchivedAt = request.IsArchived.Value ? DateTime.UtcNow : null;
        }

        // NOTE: Document entity doesn't have UpdatedAt field.
        // If ERD adds UpdatedAt later, set it here. For now we skip.

        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("UPDATE_DOCUMENT_METADATA", "Document", doc.DocumentID,
            $"Updated metadata for '{doc.FileName}'");

        return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(doc), "Document metadata updated.");
    }

    // ================================================================
    // Archive (soft delete)
    // ================================================================
    public async Task<ApiResponse<string>> ArchiveAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentTrackedAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<string>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found.");

        if (doc.IsArchived)
            return ApiResponse<string>.SuccessResponse("Document is already archived.");

        doc.IsArchived = true;
        doc.ArchivedAt = DateTime.UtcNow;
        // TODO: schedule physical file cleanup job for archived documents
        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("ARCHIVE_DOCUMENT", "Document", doc.DocumentID,
            $"Archived document '{doc.FileName}'");

        return ApiResponse<string>.SuccessResponse("Document archived successfully.");
    }

    // ================================================================
    // Private helpers
    // ================================================================

    /// <summary>Find a document owned by the user's startup (no tracking, for reads).</summary>
    private async Task<Document?> GetOwnedDocumentAsync(int documentId, int userId, CancellationToken ct)
    {
        return await _context.Documents
            .AsNoTracking()
            .Include(d => d.Startup)
            .Where(d => d.DocumentID == documentId && d.Startup.UserID == userId)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Find a document owned by the user's startup (with tracking, for writes).</summary>
    private async Task<Document?> GetOwnedDocumentTrackedAsync(int documentId, int userId, CancellationToken ct)
    {
        return await _context.Documents
            .Include(d => d.Startup)
            .Where(d => d.DocumentID == documentId && d.Startup.UserID == userId)
            .FirstOrDefaultAsync(ct);
    }

    private DocumentDto MapToDto(Document d)
    {
        return new DocumentDto
        {
            DocumentID = d.DocumentID,
            StartupID = d.StartupID,
            DocumentType = d.DocumentType,
            Title = d.FileName,
            Version = d.Version,
            DownloadUrl = $"/api/documents/{d.DocumentID}/download",
            FileSize = d.FileSize,
            ContentType = d.FileFormat,
            IsArchived = d.IsArchived,
            IsAnalyzed = d.IsAnalyzed,
            AnalysisStatus = d.AnalysisStatus,
            UploadedAt = d.UploadedAt,
            UpdatedAt = null // Document entity has no UpdatedAt — aligned with current ERD
        };
    }

    private static string? ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "File is required.";

        if (file.Length > MaxFileSize)
            return $"File size exceeds the maximum allowed ({MaxFileSize / (1024 * 1024)} MB).";

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return $"File type '{ext}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}.";

        return null; // valid
    }
}
