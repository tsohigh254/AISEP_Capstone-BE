using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Readiness;

namespace AISEP.Application.Interfaces;

public interface IReadinessService
{
    /// <summary>
    /// Calculate readiness in real-time, upsert snapshot, and return the result.
    /// </summary>
    Task<ApiResponse<ReadinessResultDto>> GetReadinessAsync(int userId);

    /// <summary>
    /// Force recalculate readiness (same logic as GET, explicit intent).
    /// </summary>
    Task<ApiResponse<ReadinessResultDto>> RecalculateReadinessAsync(int userId);
}
