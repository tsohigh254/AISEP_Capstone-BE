namespace AISEP.Application.DTOs.Connection;

// ============================= REQUEST DTOs =============================

/// <summary>
/// Create a connection request. Investor provides StartupId; Startup provides InvestorId.
/// </summary>
public class CreateConnectionRequest
{
    public int? StartupId { get; set; }   // used when investor initiates
    public int? InvestorId { get; set; }  // used when startup initiates
    public string? Message { get; set; }
}

/// <summary>
/// Update a pending connection (Investor only, status must be Sent).
/// </summary>
public class UpdateConnectionRequest
{
    public string? Message { get; set; }
}

/// <summary>
/// Reject a connection (Startup) with optional reason.
/// </summary>
public class RejectConnectionRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Create an information request within a connection (Investor).
/// </summary>
public class CreateInfoRequest
{
    public string RequestType { get; set; } = null!;
    public string? RequestMessage { get; set; }
}

/// <summary>
/// Fulfill an information request (Startup).
/// </summary>
public class FulfillInfoRequest
{
    public string? ResponseMessage { get; set; }
    public string? ResponseDocumentIDs { get; set; }
}

/// <summary>
/// Create a portfolio company record (Investor).
/// </summary>
public class CreatePortfolioCompanyRequest
{
    public string CompanyName { get; set; } = null!;
    public string? Industry { get; set; }
    public string? InvestmentStage { get; set; }
    public DateTime? InvestmentDate { get; set; }
    public decimal? InvestmentAmount { get; set; }
    public string? CurrentStatus { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Update a portfolio company record (Investor).
/// </summary>
public class UpdatePortfolioCompanyRequest
{
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
    public string? InvestmentStage { get; set; }
    public DateTime? InvestmentDate { get; set; }
    public decimal? InvestmentAmount { get; set; }
    public string? CurrentStatus { get; set; }
    public string? ExitType { get; set; }
    public DateTime? ExitDate { get; set; }
    public decimal? ExitValue { get; set; }
    public string? Description { get; set; }
}

// ============================= RESPONSE DTOs =============================

/// <summary>Basic connection DTO.</summary>
public class ConnectionDto
{
    public int ConnectionID { get; set; }
    public int StartupID { get; set; }
    public int InvestorID { get; set; }
    public string ConnectionStatus { get; set; } = string.Empty;
    public string? PersonalizedMessage { get; set; }
    public float? MatchScore { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

/// <summary>List item with participant names.</summary>
public class ConnectionListItemDto
{
    public int ConnectionID { get; set; }
    public int StartupID { get; set; }
    public string StartupName { get; set; } = string.Empty;
    public int InvestorID { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = string.Empty;
    public string? PersonalizedMessage { get; set; }
    public float? MatchScore { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

/// <summary>Detail DTO with information requests.</summary>
public class ConnectionDetailDto
{
    public int ConnectionID { get; set; }
    public int StartupID { get; set; }
    public string StartupName { get; set; } = string.Empty;
    public int InvestorID { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = string.Empty;
    public string? PersonalizedMessage { get; set; }
    public string? AttachedDocumentIDs { get; set; }
    public float? MatchScore { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public List<InfoRequestDto> InformationRequests { get; set; } = new();
}

/// <summary>Information request DTO.</summary>
public class InfoRequestDto
{
    public int RequestID { get; set; }
    public int ConnectionID { get; set; }
    public int InvestorID { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string? RequestMessage { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
    public string? ResponseMessage { get; set; }
    public string? ResponseDocumentIDs { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
}

/// <summary>Portfolio company DTO.</summary>
public class PortfolioCompanyDto
{
    public int PortfolioID { get; set; }
    public int InvestorID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? InvestmentStage { get; set; }
    public DateTime? InvestmentDate { get; set; }
    public decimal? InvestmentAmount { get; set; }
    public string? CurrentStatus { get; set; }
    public string? ExitType { get; set; }
    public DateTime? ExitDate { get; set; }
    public decimal? ExitValue { get; set; }
    public string? Description { get; set; }
    public string? CompanyLogoURL { get; set; }
}
