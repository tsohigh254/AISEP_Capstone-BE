using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Document;
using AISEP.Application.QueryParams;
using Microsoft.AspNetCore.Http;

namespace AISEP.Application.Interfaces;

public interface IDocumentService
{
    /// <summary>Upload a document file and create the DB record.</summary>
    Task<ApiResponse<DocumentDto>> UploadAsync(DocumentCreateRequest request, int userId, CancellationToken ct = default);

    /// <summary>List documents belonging to the current startup owner.</summary>
    Task<ApiResponse<IEnumerable<DocumentDto>>> GetMyDocumentsAsync(int userId, bool? isArchived = false, CancellationToken ct = default);

    /// <summary>Get full metadata for a single document (owner only).</summary>
    Task<ApiResponse<DocumentDto>> GetMyDocumentAsync(int documentId, int userId, CancellationToken ct = default);

    /// <summary>Get document for any authenticated user, enforcing DocumentVisibility flags.</summary>
    Task<ApiResponse<DocumentDto>> GetViewableDocumentAsync(int documentId, int userId, string userType, CancellationToken ct = default);

    /// <summary>List a startup's documents visible to the caller based on DocumentVisibility flags.</summary>
    Task<ApiResponse<IEnumerable<DocumentDto>>> GetStartupDocumentsAsync(int startupId, int userId, string userType, CancellationToken ct = default);

    /// <summary>Download a document's file bytes, enforcing DocumentVisibility flags.</summary>
    Task<ApiResponse<DocumentDownloadResult>> DownloadDocumentAsync(int documentId, int userId, string userType, CancellationToken ct = default);

    /// <summary>Update document metadata (owner only).</summary>
    Task<ApiResponse<DocumentDto>> UpdateMetadataAsync(int documentId, DocumentUpdateMetadataRequest request, int userId, CancellationToken ct = default);

    /// <summary>Soft-delete (archive) a document (owner only).</summary>
    Task<ApiResponse<string>> ArchiveAsync(int documentId, int userId, CancellationToken ct = default);

    Task<ApiResponse<PagedResponse<DocumentDto>>> GetAllDocumentByStaff(DocumentQueryParams documentQuery);

    // Version history
    Task<ApiResponse<DocumentDto>> UploadNewVersionAsync(int documentId, DocumentUploadNewVersionRequest request, int userId, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<DocumentVersionHistoryDto>>> GetVersionHistoryAsync(int documentId, int userId, CancellationToken ct = default);

    // Access logging
    Task LogAccessAsync(int documentId, int userId, string userType, string action, string? ipAddress, CancellationToken ct = default);
    Task<ApiResponse<IEnumerable<DocumentAccessLogDto>>> GetDocumentAccessLogsAsync(int documentId, int ownerUserId, CancellationToken ct = default);

    // Staff review
    Task<ApiResponse<DocumentDto>> StaffVerifyAsync(int documentId, int staffId, string? notes, CancellationToken ct = default);
    Task<ApiResponse<DocumentDto>> StaffApproveAsync(int documentId, int staffId, string? notes, CancellationToken ct = default);
    Task<ApiResponse<DocumentDto>> StaffRejectAsync(int documentId, int staffId, string? notes, CancellationToken ct = default);
}
