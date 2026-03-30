using AISEP.Application.Const;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Document;
using AISEP.Application.Extensions;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly ILogger<DocumentService> _logger;
    private readonly ICloudinaryService _cloudinaryService;

    public DocumentService(
        ApplicationDbContext context,
        IAuditService audit,
        ILogger<DocumentService> logger,
        ICloudinaryService cloudinaryService)
    {
        _context = context;
        _audit = audit;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
    }

    // ================================================================
    // Upload
    // ================================================================
    public async Task<ApiResponse<DocumentDto>> UploadAsync(DocumentCreateRequest request, int userId, CancellationToken ct = default)
    {
        // 2. Lookup startup for current user
        var startup = await _context.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId, ct);

        if (startup == null)
            return ApiResponse<DocumentDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You must create a startup profile before uploading documents.");

        var fileUrl = await _cloudinaryService.UploadDocument(request.File, CloudinaryFolderSaving.DocumentStorage);
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
        await using var stream = request.File.OpenReadStream();

        var document = new Document
        {
            StartupID = startup.StartupID,
            DocumentType = request.DocumentType,
            FileURL = fileUrl,
            Version = version,
            IsAnalyzed = false,
            IsArchived = false,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("UPLOAD_DOCUMENT", "Document", document.DocumentID,
            $"Uploaded ({request.DocumentType} v{version}) for startup {startup.StartupID}");

        _logger.LogInformation("Document {DocumentID} uploaded for startup {StartupID}", document.DocumentID, startup.StartupID);

        return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(document), "Document uploaded successfully");
    }

    // ================================================================
    // List my documents
    // ================================================================
    public async Task<ApiResponse<IEnumerable<DocumentDto>>> GetMyDocumentsAsync(int userId, CancellationToken ct = default)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId, ct);

        if (startup == null)
            return ApiResponse<IEnumerable<DocumentDto>>.ErrorResponse(
                "STARTUP_PROFILE_NOT_FOUND", "You must create a startup profile first.");

        var query = _context.Documents
            .AsNoTracking()
            .Where(d => d.StartupID == startup.StartupID);

        var items = await query
            .Select(d => new DocumentDto
            {
                DocumentID = d.DocumentID,
                StartupID = d.StartupID,
                Title = d.Title ?? string.Empty,
                DocumentType = d.DocumentType.ToString(),
                Version = d.Version,
                FileUrl = d.FileURL ?? string.Empty,
                IsArchived = d.IsArchived,
                IsAnalyzed = d.IsAnalyzed,
                AnalysisStatus = d.AnalysisStatus.ToString(),
                UploadedAt = d.UploadedAt,
                ProofStatus = d.BlockchainProof != null ? d.BlockchainProof.ProofStatus.ToString() : string.Empty,
                FileHash = d.BlockchainProof != null ? d.BlockchainProof.FileHash : string.Empty,
                TransactionHash = d.BlockchainProof != null ? d.BlockchainProof.TransactionHash : null
            })
            .ToListAsync(ct);

        return ApiResponse<IEnumerable<DocumentDto>>.SuccessResponse(items, "Get documents successfully");
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
    // Update metadata
    // ================================================================
    public async Task<ApiResponse<DocumentDto>> UpdateMetadataAsync(
        int documentId, DocumentUpdateMetadataRequest request, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentTrackedAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<DocumentDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found.");

        if (request.Title != null) doc.Title = request.Title;
        if (request.IsArchived.HasValue)
        {
            doc.IsArchived = request.IsArchived.Value;
            doc.ArchivedAt = request.IsArchived.Value ? DateTime.UtcNow : null;
        }

        // NOTE: Document entity doesn't have UpdatedAt field.
        // If ERD adds UpdatedAt later, set it here. For now we skip.

        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("UPDATE_DOCUMENT_METADATA", "Document", doc.DocumentID,
            $"Updated metadata for '{doc.Title}'");

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
            $"Archived document '{doc.Title}'");

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
            .Include(d => d.BlockchainProof)
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
            Title = d.Title ?? string.Empty,
            FileUrl = d.FileURL ?? string.Empty,
            DocumentType = d.DocumentType.ToString(),
            Version = d.Version,
            IsArchived = d.IsArchived,
            IsAnalyzed = d.IsAnalyzed,
            AnalysisStatus = d.AnalysisStatus.ToString(),
            UploadedAt = d.UploadedAt,
            ProofStatus = d.BlockchainProof != null ? d.BlockchainProof.ProofStatus.ToString() : string.Empty,
            FileHash = d.BlockchainProof != null ? d.BlockchainProof.FileHash : string.Empty,
            TransactionHash = d.BlockchainProof != null ? d.BlockchainProof.TransactionHash : null
        };
    }

    public async Task<ApiResponse<PagedResponse<DocumentDto>>> GetAllDocumentByStaff(DocumentQueryParams documentQuery)
    {
        var documents = _context.Documents.AsQueryable();

        var documentsToDto = documents.Select(d => new DocumentDto
        {
            DocumentID = d.DocumentID,
            StartupID = d.StartupID,
            DocumentType = d.DocumentType.ToString(),
            Title = d.Title,
            Version = d.Version,
            FileUrl = d.FileURL,
            IsAnalyzed = d.IsAnalyzed,
            IsArchived = d.IsArchived,
            AnalysisStatus = d.AnalysisStatus.ToString(),
            UploadedAt = d.UploadedAt,
            ProofStatus = d.BlockchainProof != null ? d.BlockchainProof.ProofStatus.ToString() : string.Empty,
            FileHash = d.BlockchainProof != null ? d.BlockchainProof.FileHash : string.Empty,
            TransactionHash = d.BlockchainProof != null ? d.BlockchainProof.TransactionHash : null
        }).Paging(documentQuery.Page, documentQuery.PageSize);

        return ApiResponse<PagedResponse<DocumentDto>>.SuccessResponse(
            new PagedResponse<DocumentDto>
            {
                Items = await documentsToDto.ToListAsync(),
                Paging = new PagingInfo
                {
                    Page = documentQuery.Page,
                    PageSize = documentQuery.PageSize,
                    TotalItems = await documents.CountAsync()
                }
            });
    }
}
