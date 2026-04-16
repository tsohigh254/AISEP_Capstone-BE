using AISEP.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AISEP.Application.DTOs.Document;

// ──────────────────────────────────────────────
// Request DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Metadata sent alongside file upload (multipart/form-data).
/// The actual file is IFormFile bound separately in the controller.
/// </summary>
public class DocumentCreateRequest
{
    public IFormFile File { get; set; } = null!;
    /// <summary>PitchDeck | BusinessPlan | Financials | Legal | Other</summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>Optional: custom title. If null, the original file name is used.</summary>
    public string? Title { get; set; }

    /// <summary>Optional: explicit version string. If null, auto-incremented ("1", "2", …).</summary>
    public string? Version { get; set; }

    /// <summary>
    /// Optional: who can view this document. Flags enum — combine values.
    /// 0 = OwnerOnly (default), 1 = Investor, 2 = Advisor, 3 = Investor+Advisor, 4 = Public.
    /// </summary>
    public DocumentVisibility? Visibility { get; set; }
}

/// <summary>
/// Combined form model for Swagger-compatible multipart/form-data upload.
/// </summary>
public class DocumentUploadForm
{
    /// <summary>The file to upload (PDF, PPT/PPTX, DOC/DOCX — max 20 MB)</summary>
    public IFormFile File { get; set; } = null!;

    /// <summary>PitchDeck | BusinessPlan | Financials | Legal | Other</summary>
    public string DocumentType { get; set; } = null!;

    /// <summary>Optional: custom title. If null, the original file name is used.</summary>
    public string? Title { get; set; }

    /// <summary>Optional: explicit version string. If null, auto-incremented ("1", "2", …).</summary>
    public string? Version { get; set; }
}

/// <summary>
/// Update metadata for an existing document.
/// </summary>
public class DocumentUpdateMetadataRequest
{
    /// <summary>New title (optional).</summary>
    public string? Title { get; set; }

    /// <summary>Change document type (optional).</summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>Set archived state (optional).</summary>
    public bool? IsArchived { get; set; }

    /// <summary>
    /// Update visibility (optional). Flags enum — combine values.
    /// 0 = OwnerOnly, 1 = Investor, 2 = Advisor, 3 = Investor+Advisor, 4 = Public.
    /// </summary>
    public DocumentVisibility? Visibility { get; set; }
}

// ──────────────────────────────────────────────
// Response DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Full document metadata returned to the owner.
/// </summary>
public class DocumentDto
{
    public int DocumentID { get; set; }
    public int StartupID { get; set; }
    public string DocumentType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Version { get; set; }
    public string? FileUrl { get; set; }
    public bool IsArchived { get; set; }
    public bool IsAnalyzed { get; set; }
    public string? AnalysisStatus { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ProofStatus { get; set; }
    public string? FileHash { get; set; }
    public string? TransactionHash { get; set; }
    public DateTime? AnchoredAt { get; set; }
    public string? EtherscanUrl { get; set; }
    public string? ReviewStatus { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    /// <summary>Visibility flags value (int). Combine: 0=OwnerOnly, 1=Investor, 2=Advisor, 4=Public.</summary>
    public int Visibility { get; set; }

    /// <summary>Human-readable visibility label (e.g. "OwnerOnly", "Investor, Advisor", "Public").</summary>
    public string? VisibilityLabel { get; set; }

    // Convenience fields for frontend compatibility (serialized as camelCase)
    public string Id => DocumentID.ToString();
    public string Name => Title ?? (FileUrl ?? "");
    public string Type
    {
        get
        {
            var t = (DocumentType ?? string.Empty).ToUpperInvariant();
            // Normalize common typos (backend enum uses 'Bussiness_Plan') to expected frontend value
            if (t.Contains("BUSSINESS"))
                t = t.Replace("BUSSINESS", "BUSINESS");
            return t;
        }
    }
    public string UpdatedAt => UploadedAt == default ? "" : UploadedAt.ToString("dd/MM/yyyy");
    public bool Recommended => false;
}

// ──────────────────────────────────────────────
// Staff Review DTOs
// ──────────────────────────────────────────────

public class StaffReviewDocumentRequest
{
    public string? Notes { get; set; }
}

// ──────────────────────────────────────────────
// Version History DTOs
// ──────────────────────────────────────────────

public class DocumentUploadNewVersionRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Title { get; set; }
}

// ──────────────────────────────────────────────
// Download
// ──────────────────────────────────────────────

public class DocumentDownloadResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}

// ──────────────────────────────────────────────
// Access Log
// ──────────────────────────────────────────────

public class DocumentAccessLogDto
{
    public int LogID { get; set; }
    public int UserID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; }
}

public class DocumentVersionHistoryDto
{
    public int DocumentID { get; set; }
    public string? Version { get; set; }
    public string Title { get; set; } = null!;
    public string? FileUrl { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ReviewStatus { get; set; }
    public string? ProofStatus { get; set; }
    public string? FileHash { get; set; }
    public bool IsArchived { get; set; }
    public bool IsCurrent { get; set; }
}

