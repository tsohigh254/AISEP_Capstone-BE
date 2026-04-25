using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.MasterData;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AISEP.Infrastructure.Services;

public class StaffMasterDataService : IStaffMasterDataService
{
    private readonly ApplicationDbContext _db;

    public StaffMasterDataService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ================================================================
    // INDUSTRY MANAGEMENT
    // ================================================================

    public async Task<ApiResponse<List<IndustryDto>>> GetAllIndustriesAsync()
    {
        // Fetch industries first to avoid complex subqueries in translation if possible, 
        // but EF Core handles Count() in Select fairly well. 
        // We ensure we don't include navigation properties to avoid cycles.
        var industries = await _db.Industries
            .AsNoTracking()
            .OrderBy(i => i.IndustryID)
            .Select(i => new IndustryDto
            {
                IndustryID = i.IndustryID,
                IndustryName = i.IndustryName,
                Description = i.Description,
                ParentIndustryID = i.ParentIndustryID,
                IsActive = i.IsActive,
                ParentIndustryName = i.ParentIndustry != null ? i.ParentIndustry.IndustryName : null,
                // Count startups where this is either the main industry or sub-industry
                StartupCount = _db.Startups.Count(s => s.IndustryID == i.IndustryID || s.SubIndustryID == i.IndustryID),
                InvestorCount = _db.InvestorIndustryFocuses.Count(f => f.IndustryID == i.IndustryID)
            })
            .ToListAsync();

        return ApiResponse<List<IndustryDto>>.SuccessResponse(industries);
    }

    public async Task<ApiResponse<IndustryDto>> CreateIndustryAsync(ManageIndustryRequest request)
    {
        // Unique check
        var nameExists = await _db.Industries.AnyAsync(i => 
            i.IndustryName.ToLower() == request.IndustryName.Trim().ToLower() && 
            i.ParentIndustryID == request.ParentIndustryID);
        
        if (nameExists)
            return ApiResponse<IndustryDto>.ErrorResponse("NAME_EXISTS", "Industry name already exists under this parent");

        if (request.ParentIndustryID.HasValue)
        {
            var parentExists = await _db.Industries.AnyAsync(i => i.IndustryID == request.ParentIndustryID.Value);
            if (!parentExists)
                return ApiResponse<IndustryDto>.ErrorResponse("PARENT_NOT_FOUND", "Parent industry not found");
        }

        var industry = new Industry
        {
            IndustryName = request.IndustryName,
            Description = request.Description,
            ParentIndustryID = request.ParentIndustryID,
            IsActive = request.IsActive
        };

        try
        {
            _db.Industries.Add(industry);
            await _db.SaveChangesAsync();
            return ApiResponse<IndustryDto>.SuccessResponse(MapToDto(industry), "Industry created successfully");
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            return ApiResponse<IndustryDto>.ErrorResponse("CONFLICT", "Dữ liệu bị trùng lặp hoặc lỗi Sequence. Hệ thống sẽ tự động sửa lỗi này trong lần khởi động tới.");
        }
    }

    public async Task<ApiResponse<IndustryDto>> UpdateIndustryAsync(int id, ManageIndustryRequest request)
    {
        var industry = await _db.Industries.FirstOrDefaultAsync(i => i.IndustryID == id);
        if (industry == null)
            return ApiResponse<IndustryDto>.ErrorResponse("NOT_FOUND", "Industry not found");

        // Unique check
        var nameExists = await _db.Industries.AnyAsync(i => 
            i.IndustryID != id &&
            i.IndustryName.ToLower() == request.IndustryName.Trim().ToLower() && 
            i.ParentIndustryID == request.ParentIndustryID);
        
        if (nameExists)
            return ApiResponse<IndustryDto>.ErrorResponse("NAME_EXISTS", "Another industry with this name already exists under this parent");

        if (request.ParentIndustryID.HasValue)
        {
            if (request.ParentIndustryID.Value == id)
                return ApiResponse<IndustryDto>.ErrorResponse("INVALID_PARENT", "An industry cannot be its own parent");

            var parentExists = await _db.Industries.AnyAsync(i => i.IndustryID == request.ParentIndustryID.Value);
            if (!parentExists)
                return ApiResponse<IndustryDto>.ErrorResponse("PARENT_NOT_FOUND", "Parent industry not found");
        }

        // Cascade isActive = false to sub-industries if needed
        if (industry.IsActive && !request.IsActive)
        {
            var subs = await _db.Industries.Where(i => i.ParentIndustryID == id).ToListAsync();
            foreach (var sub in subs) sub.IsActive = false;
        }

        industry.IndustryName = request.IndustryName.Trim();
        industry.Description = request.Description;
        industry.ParentIndustryID = request.ParentIndustryID;
        industry.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return ApiResponse<IndustryDto>.SuccessResponse(MapToDto(industry), "Industry updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteIndustryAsync(int id)
    {
        var industry = await _db.Industries
            .Include(i => i.SubIndustries)
            .FirstOrDefaultAsync(i => i.IndustryID == id);

        if (industry == null)
            return ApiResponse<string>.ErrorResponse("NOT_FOUND", "Industry not found");

        // Check if there are sub-industries
        if (industry.SubIndustries.Any())
            return ApiResponse<string>.ErrorResponse("HAS_SUBINDUSTRIES", "Cannot delete an industry that has sub-industries. Delete them first.");

        // Check if it's being used by startups
        var usedByStartups = await _db.Startups.AnyAsync(s => s.IndustryID == id || s.SubIndustryID == id);
        if (usedByStartups)
        {
            // If used, we should probably just deactivate it instead of deleting
            industry.IsActive = false;
            await _db.SaveChangesAsync();
            return ApiResponse<string>.SuccessResponse("Industry is in use by startups, so it was deactivated instead of deleted.");
        }

        _db.Industries.Remove(industry);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse("Industry deleted successfully");
    }

    // ================================================================
    // STAGE MANAGEMENT
    // ================================================================

    public async Task<ApiResponse<List<StartupStageDto>>> GetAllStagesAsync()
    {
        var stages = await _db.Stages
            .AsNoTracking()
            .OrderBy(s => s.OrderIndex)
            .Select(s => new StartupStageDto
            {
                StageID = s.StageID,
                StageName = s.StageName,
                Description = s.Description,
                OrderIndex = s.OrderIndex,
                IsActive = s.IsActive,
                StartupCount = _db.Startups.Count(st => st.StageID == s.StageID),
                InvestorCount = _db.InvestorStageFocuses.Count(f => f.StageID == s.StageID)
            })
            .ToListAsync();

        return ApiResponse<List<StartupStageDto>>.SuccessResponse(stages);
    }

    public async Task<ApiResponse<StartupStageDto>> CreateStageAsync(ManageStageRequest request)
    {
        // Unique check
        var nameExists = await _db.Stages.AnyAsync(s => s.StageName.ToLower() == request.StageName.Trim().ToLower());
        if (nameExists)
            return ApiResponse<StartupStageDto>.ErrorResponse("NAME_EXISTS", "Stage name already exists");

        var stage = new Stage
        {
            StageName = request.StageName,
            Description = request.Description,
            OrderIndex = request.OrderIndex,
            IsActive = request.IsActive
        };

        _db.Stages.Add(stage);
        await _db.SaveChangesAsync();

        return ApiResponse<StartupStageDto>.SuccessResponse(MapToStageDto(stage), "Stage created successfully");
    }

    public async Task<ApiResponse<StartupStageDto>> UpdateStageAsync(int id, ManageStageRequest request)
    {
        var stage = await _db.Stages.FirstOrDefaultAsync(s => s.StageID == id);
        if (stage == null)
            return ApiResponse<StartupStageDto>.ErrorResponse("NOT_FOUND", "Stage not found");

        // Unique check
        var nameExists = await _db.Stages.AnyAsync(s => s.StageID != id && s.StageName.ToLower() == request.StageName.Trim().ToLower());
        if (nameExists)
            return ApiResponse<StartupStageDto>.ErrorResponse("NAME_EXISTS", "Another stage with this name already exists");

        stage.StageName = request.StageName.Trim();
        stage.Description = request.Description;
        stage.OrderIndex = request.OrderIndex;
        stage.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return ApiResponse<StartupStageDto>.SuccessResponse(MapToStageDto(stage), "Stage updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteStageAsync(int id)
    {
        var stage = await _db.Stages.FirstOrDefaultAsync(s => s.StageID == id);
        if (stage == null)
            return ApiResponse<string>.ErrorResponse("NOT_FOUND", "Stage not found");

        // Check if used by startups
        var usedByStartups = await _db.Startups.AnyAsync(s => s.StageID == id);
        if (usedByStartups)
        {
            stage.IsActive = false;
            await _db.SaveChangesAsync();
            return ApiResponse<string>.SuccessResponse("Stage is in use by startups, so it was deactivated instead of deleted.");
        }

        _db.Stages.Remove(stage);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse("Stage deleted successfully");
    }

    public async Task<ApiResponse<string>> ReorderStagesAsync(ReorderStageRequest request)
    {
        var stageIds = request.Orders.Select(o => o.StageID).ToList();
        var stages = await _db.Stages.Where(s => stageIds.Contains(s.StageID)).ToListAsync();

        foreach (var order in request.Orders)
        {
            var stage = stages.FirstOrDefault(s => s.StageID == order.StageID);
            if (stage != null)
            {
                stage.OrderIndex = order.OrderIndex;
            }
        }

        await _db.SaveChangesAsync();
        return ApiResponse<string>.SuccessResponse("Stages reordered successfully");
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private static IndustryDto MapToDto(Industry i) => new()
    {
        IndustryID = i.IndustryID,
        IndustryName = i.IndustryName,
        Description = i.Description,
        ParentIndustryID = i.ParentIndustryID,
        SubIndustries = new List<IndustryDto>() // Usually not needed in single result, or loaded separately
    };

    private static StartupStageDto MapToStageDto(Stage s) => new()
    {
        StageID = s.StageID,
        StageName = s.StageName,
        Description = s.Description
    };
}
