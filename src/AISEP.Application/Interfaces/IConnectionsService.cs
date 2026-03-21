using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Connection;

namespace AISEP.Application.Interfaces;

public interface IConnectionsService
{
    // ---- Connections ----
    Task<ApiResponse<ConnectionDto>> CreateConnectionAsync(int userId, CreateConnectionRequest request);
    Task<ApiResponse<PagedResponse<ConnectionListItemDto>>> GetSentAsync(int userId, string? status, int page, int pageSize);
    Task<ApiResponse<PagedResponse<ConnectionListItemDto>>> GetReceivedAsync(int userId, string? status, int? investorId, int page, int pageSize);
    Task<ApiResponse<ConnectionDetailDto>> GetDetailAsync(int userId, string userType, int connectionId);
    Task<ApiResponse<ConnectionDto>> UpdateAsync(int userId, int connectionId, UpdateConnectionRequest request);
    Task<ApiResponse<ConnectionDto>> WithdrawAsync(int userId, int connectionId);
    Task<ApiResponse<ConnectionDto>> AcceptAsync(int userId, int connectionId);
    Task<ApiResponse<ConnectionDto>> RejectAsync(int userId, int connectionId, string? reason);
    Task<ApiResponse<ConnectionDto>> CloseAsync(int userId, string userType, int connectionId);

    // ---- Information Requests ----
    Task<ApiResponse<InfoRequestDto>> CreateInfoRequestAsync(int userId, int connectionId, CreateInfoRequest request);
    Task<ApiResponse<List<InfoRequestDto>>> GetInfoRequestsAsync(int userId, string userType, int connectionId);
    Task<ApiResponse<InfoRequestDto>> FulfillInfoRequestAsync(int userId, int requestId, FulfillInfoRequest request);
    Task<ApiResponse<InfoRequestDto>> RejectInfoRequestAsync(int userId, int requestId, string? reason);

    // ---- Portfolio ----
    Task<ApiResponse<PagedResponse<PortfolioCompanyDto>>> GetPortfolioAsync(int userId, int page, int pageSize);
    Task<ApiResponse<PortfolioCompanyDto>> CreatePortfolioAsync(int userId, CreatePortfolioCompanyRequest request);
    Task<ApiResponse<PortfolioCompanyDto>> UpdatePortfolioAsync(int userId, int portfolioId, UpdatePortfolioCompanyRequest request);
    Task<ApiResponse<PortfolioCompanyDto>> DeletePortfolioAsync(int userId, int portfolioId);
}
