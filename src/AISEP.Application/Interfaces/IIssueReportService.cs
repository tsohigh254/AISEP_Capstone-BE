using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.IssueReport;
using AISEP.Domain.Enums;

namespace AISEP.Application.Interfaces;

public interface IIssueReportService
{
    /// <summary>Any authenticated user submits an issue report.</summary>
    Task<ApiResponse<IssueReportSummaryDto>> CreateAsync(int userId, CreateIssueReportRequest request);

    /// <summary>Reporter views their own report.</summary>
    Task<ApiResponse<IssueReportDetailDto>> GetByIdAsync(int userId, string userType, int issueReportId);

    /// <summary>Reporter views their own reports (paginated).</summary>
    Task<ApiResponse<PagedResponse<IssueReportSummaryDto>>> GetMyReportsAsync(
        int userId, int page, int pageSize,
        IssueReportStatus? status,
        IssueCategory? category);

    /// <summary>Staff/Admin lists all reports with optional filters.</summary>
    Task<ApiResponse<PagedResponse<IssueReportDetailDto>>> GetListAsync(
        int page, int pageSize,
        IssueReportStatus? status,
        IssueCategory? category,
        int? reporterUserId);

    /// <summary>Staff/Admin updates status and note.</summary>
    Task<ApiResponse<IssueReportDetailDto>> UpdateStatusAsync(int staffUserId, int issueReportId, UpdateIssueReportStatusRequest request);

    /// <summary>Staff escalates an issue to a formal dispute.</summary>
    Task<ApiResponse<IssueReportDetailDto>> EscalateToDisputeAsync(int staffUserId, int issueReportId);
}
