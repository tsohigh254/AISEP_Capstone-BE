using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Document;
using Microsoft.AspNetCore.Http;

namespace AISEP.Application.Interfaces;

public interface IDocumentService
{
    /// <summary>Upload a document file and create the DB record.</summary>
    Task<ApiResponse<DocumentDto>> UploadAsync(DocumentCreateRequest request, int userId, CancellationToken ct = default);

    /// <summary>List documents belonging to the current startup owner.</summary>
    Task<ApiResponse<IEnumerable<DocumentDto>>> GetMyDocumentsAsync(int userId,  CancellationToken ct = default);

    /// <summary>Get full metadata for a single document (owner only).</summary>
    Task<ApiResponse<DocumentDto>> GetMyDocumentAsync(int documentId, int userId, CancellationToken ct = default);

    /// <summary>Update document metadata (owner only).</summary>
    Task<ApiResponse<DocumentDto>> UpdateMetadataAsync(int documentId, DocumentUpdateMetadataRequest request, int userId, CancellationToken ct = default);

    /// <summary>Soft-delete (archive) a document (owner only).</summary>
    Task<ApiResponse<string>> ArchiveAsync(int documentId, int userId, CancellationToken ct = default);
}
