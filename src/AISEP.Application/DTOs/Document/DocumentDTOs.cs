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
    public IFormFile File { get; set; }
    /// <summary>PitchDeck | BusinessPlan | Financials | Legal | Other</summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>Optional: custom title. If null, the original file name is used.</summary>
    public string Title { get; set; }

    /// <summary>Optional: explicit version string. If null, auto-incremented ("1", "2", …).</summary>
    public string? Version { get; set; }
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

    /// <summary>Set archived state (optional).</summary>
    public bool? IsArchived { get; set; }
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
    public string FileUrl { get; set; }
    public bool IsArchived { get; set; }
    public bool IsAnalyzed { get; set; }
    public string? AnalysisStatus { get; set; }
    public DateTime UploadedAt { get; set; }
    public string ProofStatus { get; set; } = null!;
    public string FileHash { get; set; }
    public string TransactionHash { get; set; }
}

