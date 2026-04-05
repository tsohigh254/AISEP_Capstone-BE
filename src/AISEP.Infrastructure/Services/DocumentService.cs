using AISEP.Application.Const;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Document;
using AISEP.Application.Extensions;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
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
        // 1. Lookup startup for current user
        var startup = await _context.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId, ct);

        if (startup == null)
            return ApiResponse<DocumentDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You must create a startup profile before uploading documents.");

        // 2. Upload document với hash tính sẵn
        var uploadResult = await _cloudinaryService.UploadDocumentWithHashAsync(
            request.File, 
            CloudinaryFolderSaving.DocumentStorage);

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

        // 4. Create document entity
        var document = new Document
        {
            StartupID = startup.StartupID,
            DocumentType = request.DocumentType,
            FileURL = uploadResult.FileUrl,
            Version = version,
            IsAnalyzed = false,
            IsArchived = false,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(ct);

        // 5. Tự động tạo blockchain proof với hash đã tính
        var proof = new DocumentBlockchainProof
        {
            DocumentID = document.DocumentID,
            FileHash = uploadResult.FileHash,
            HashAlgorithm = uploadResult.HashAlgorithm,
            ProofStatus = ProofStatus.HashComputed
        };

        _context.DocumentBlockchainProofs.Add(proof);
        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("UPLOAD_DOCUMENT", "Document", document.DocumentID,
            $"Uploaded ({request.DocumentType} v{version}) with hash {uploadResult.FileHash.Substring(0, 8)}... for startup {startup.StartupID}");

        _logger.LogInformation(
            "Document {DocumentID} uploaded for startup {StartupID} with hash computed: {Hash}", 
            document.DocumentID, 
            startup.StartupID, 
            uploadResult.FileHash);

        return ApiResponse<DocumentDto>.SuccessResponse(MapToDto(document), "Document uploaded successfully with hash computed");
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
                Title = d.Title,
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
            Title = d.Title,
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

    // ================================================================
    // STAFF REVIEW ENDPOINTS
    // ================================================================

    public async Task<ApiResponse<DocumentDto>> StaffVerifyAsync(int documentId, int staffId, string? notes, CancellationToken ct = default)
    {
        return await SetReviewStatus(documentId, staffId, DocumentReviewStatus.Verified, notes, ct);
    }

    public async Task<ApiResponse<DocumentDto>> StaffApproveAsync(int documentId, int staffId, string? notes, CancellationToken ct = default)
    {
        return await SetReviewStatus(documentId, staffId, DocumentReviewStatus.Approved, notes, ct);
    }

    public async Task<ApiResponse<DocumentDto>> StaffRejectAsync(int documentId, int staffId, string? notes, CancellationToken ct = default)
    {
        return await SetReviewStatus(documentId, staffId, DocumentReviewStatus.Rejected, notes, ct);
    }

    private async Task<ApiResponse<DocumentDto>> SetReviewStatus(
        int documentId, int staffId, DocumentReviewStatus status, string? notes, CancellationToken ct)
    {
        var doc = await _context.Documents
            .Include(d => d.BlockchainProof)
            .FirstOrDefaultAsync(d => d.DocumentID == documentId, ct);

        if (doc == null)
            return ApiResponse<DocumentDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found");

        doc.ReviewStatus = status;
        doc.ReviewedBy = staffId;
        doc.ReviewedAt = DateTime.UtcNow;
        doc.ReviewNotes = notes;

        await _context.SaveChangesAsync(ct);

        return ApiResponse<DocumentDto>.SuccessResponse(new DocumentDto
        {
            DocumentID = doc.DocumentID,
            StartupID = doc.StartupID,
            DocumentType = doc.DocumentType.ToString(),
            Title = doc.Title,
            Version = doc.Version,
            FileUrl = doc.FileURL,
            IsAnalyzed = doc.IsAnalyzed,
            IsArchived = doc.IsArchived,
            AnalysisStatus = doc.AnalysisStatus.ToString(),
            UploadedAt = doc.UploadedAt,
            ProofStatus = doc.BlockchainProof?.ProofStatus.ToString(),
            FileHash = doc.BlockchainProof?.FileHash,
            TransactionHash = doc.BlockchainProof?.TransactionHash,
            ReviewStatus = doc.ReviewStatus.ToString(),
            ReviewedBy = doc.ReviewedBy,
            ReviewedAt = doc.ReviewedAt,
            ReviewNotes = doc.ReviewNotes
        }, $"Document {status.ToString().ToLower()}");
    }
}
