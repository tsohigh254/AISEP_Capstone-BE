using AISEP.Application.DTOs.Admin;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

public interface IAiLogsService
{
    Task<ApiResponse<AiLogsResponseDto>> GetLogsAsync(
        int tail,
        string? level,
        string? search,
        string? correlationId,
        CancellationToken ct);
}
