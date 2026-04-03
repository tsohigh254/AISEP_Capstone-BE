using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class InvestorService : IInvestorService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<InvestorService> _logger;

    public InvestorService(ApplicationDbContext db, IAuditService audit, ILogger<InvestorService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    // ================================================================
    // PROFILE
    // ================================================================

    public async Task<ApiResponse<InvestorDto>> CreateProfileAsync(int userId, CreateInvestorRequest request)
    {
        // Check if profile already exists
        var exists = await _db.Investors.AnyAsync(i => i.UserID == userId);
        if (exists)
            return ApiResponse<InvestorDto>.ErrorResponse("INVESTOR_PROFILE_EXISTS",
                "Investor profile already exists for this user.");

        var investor = new Investor
        {
            UserID = userId,
            FullName = request.FullName,
            FirmName = request.FirmName,
            Title = request.Title,
            Bio = request.Bio,
            InvestmentThesis = request.InvestmentThesis,
            Location = request.Location,
            Country = request.Country,
            LinkedInURL = request.LinkedInURL,
            Website = request.Website,
            ProfileStatus = ProfileStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        _db.Investors.Add(investor);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_INVESTOR_PROFILE", "Investor", investor.InvestorID, null);
        _logger.LogInformation("Investor profile {InvestorId} created for user {UserId}", investor.InvestorID, userId);

        return ApiResponse<InvestorDto>.SuccessResponse(MapToDto(investor));
    }

    public async Task<ApiResponse<InvestorDto>> GetMyProfileAsync(int userId)
    {
        var investor = await _db.Investors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
            return ApiResponse<InvestorDto>.SuccessResponse(null, "Profile has not been created yet.");

        return ApiResponse<InvestorDto>.SuccessResponse(MapToDto(investor));
    }

    public async Task<ApiResponse<InvestorDto>> UpdateProfileAsync(int userId, UpdateInvestorRequest request)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND",
                "Investor profile not found.");

        if (request.FullName != null) investor.FullName = request.FullName;
        if (request.FirmName != null) investor.FirmName = request.FirmName;
        if (request.Title != null) investor.Title = request.Title;
        if (request.Bio != null) investor.Bio = request.Bio;
        if (request.InvestmentThesis != null) investor.InvestmentThesis = request.InvestmentThesis;
        if (request.Location != null) investor.Location = request.Location;
        if (request.Country != null) investor.Country = request.Country;
        if (request.LinkedInURL != null) investor.LinkedInURL = request.LinkedInURL;
        if (request.Website != null) investor.Website = request.Website;
        investor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_INVESTOR_PROFILE", "Investor", investor.InvestorID, null);
        _logger.LogInformation("Investor profile {InvestorId} updated", investor.InvestorID);

        return ApiResponse<InvestorDto>.SuccessResponse(MapToDto(investor));
    }

    // ================================================================
    // PREFERENCES
    // ================================================================

    public async Task<ApiResponse<PreferencesDto>> GetPreferencesAsync(int userId)
    {
        var investor = await _db.Investors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Preferences)
            .Include(i => i.StageFocus)
            .Include(i => i.IndustryFocus)
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
            return ApiResponse<PreferencesDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND",
                "Investor profile not found.");

        return ApiResponse<PreferencesDto>.SuccessResponse(MapPreferencesDto(investor));
    }

    public async Task<ApiResponse<PreferencesDto>> UpdatePreferencesAsync(int userId, UpdatePreferencesRequest request)
    {
        var investor = await _db.Investors
            .AsSplitQuery()
            .Include(i => i.Preferences)
            .Include(i => i.StageFocus)
            .Include(i => i.IndustryFocus)
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
            return ApiResponse<PreferencesDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND",
                "Investor profile not found.");

        // Upsert InvestorPreferences (1-to-1)
        if (investor.Preferences == null)
        {
            investor.Preferences = new InvestorPreferences
            {
                InvestorID = investor.InvestorID
            };
            _db.InvestorPreferences.Add(investor.Preferences);
        }

        investor.Preferences.MinInvestmentSize = request.TicketMin;
        investor.Preferences.MaxInvestmentSize = request.TicketMax;
        investor.Preferences.PreferredGeographies = request.PreferredGeographies;
        investor.Preferences.MinPotentialScore = request.MinPotentialScore;
        investor.Preferences.UpdatedAt = DateTime.UtcNow;

        // Store comma-separated in the text columns too (for backward compat)
        investor.Preferences.PreferredStages = request.PreferredStages != null
            ? string.Join(",", request.PreferredStages)
            : null;
        investor.Preferences.PreferredIndustries = request.PreferredIndustries != null
            ? string.Join(",", request.PreferredIndustries)
            : null;

        // Sync InvestorStageFocus table
        if (request.PreferredStages != null)
        {
            _db.InvestorStageFocuses.RemoveRange(investor.StageFocus);
            foreach (var stage in request.PreferredStages.Distinct())
            {
                if (Enum.TryParse<StartupStage>(stage, true, out var stageEnum))
                {
                    _db.InvestorStageFocuses.Add(new InvestorStageFocus
                    {
                        InvestorID = investor.InvestorID,
                        Stage = stageEnum
                    });
                }
            }
        }

        // Sync InvestorIndustryFocus table
        if (request.PreferredIndustries != null)
        {
            _db.InvestorIndustryFocuses.RemoveRange(investor.IndustryFocus);
            foreach (var industry in request.PreferredIndustries.Distinct())
            {
                _db.InvestorIndustryFocuses.Add(new InvestorIndustryFocus
                {
                    InvestorID = investor.InvestorID,
                    Industry = industry
                });
            }
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_INVESTOR_PREFERENCES", "InvestorPreferences",
            investor.Preferences.PreferenceID, null);
        _logger.LogInformation("Preferences updated for investor {InvestorId}", investor.InvestorID);

        // Re-read to get updated focus lists
        var updatedInvestor = await _db.Investors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Preferences)
            .Include(i => i.StageFocus)
            .Include(i => i.IndustryFocus)
            .FirstAsync(i => i.InvestorID == investor.InvestorID);

        return ApiResponse<PreferencesDto>.SuccessResponse(MapPreferencesDto(updatedInvestor));
    }

    // ================================================================
    // WATCHLIST
    // ================================================================

    public async Task<ApiResponse<WatchlistItemDto>> AddToWatchlistAsync(int userId, WatchlistAddRequest request)
    {
        var investor = await _db.Investors.AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
            return ApiResponse<WatchlistItemDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND",
                "Investor profile not found.");

        // Check startup exists
        var startup = await _db.Startups.AsNoTracking()
            .Include(s => s.Industry)
            .FirstOrDefaultAsync(s => s.StartupID == request.StartupId);

        if (startup == null)
            return ApiResponse<WatchlistItemDto>.ErrorResponse("STARTUP_NOT_FOUND",
                $"Startup with id {request.StartupId} not found.");

        // Check duplicate (active only)
        var duplicate = await _db.InvestorWatchlists
            .AnyAsync(w => w.InvestorID == investor.InvestorID
                        && w.StartupID == request.StartupId
                        && w.IsActive);

        if (duplicate)
            return ApiResponse<WatchlistItemDto>.ErrorResponse("WATCHLIST_EXISTS",
                "This startup is already in your watchlist.");

        var entry = new InvestorWatchlist
        {
            InvestorID = investor.InvestorID,
            StartupID = request.StartupId,
            WatchReason = request.WatchReason,
            Priority = !string.IsNullOrWhiteSpace(request.Priority) && Enum.TryParse<WatchlistPriority>(request.Priority, true, out var prioEnum)
                ? prioEnum : null,  // null → DB default (Medium=1)
            IsActive = true,
            AddedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.InvestorWatchlists.Add(entry);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("ADD_WATCHLIST", "InvestorWatchlist", entry.WatchlistID, $"StartupId={request.StartupId}");
        _logger.LogInformation("Investor {InvestorId} added startup {StartupId} to watchlist",
            investor.InvestorID, request.StartupId);

        return ApiResponse<WatchlistItemDto>.SuccessResponse(new WatchlistItemDto
        {
            WatchlistID = entry.WatchlistID,
            StartupID = startup.StartupID,
            CompanyName = startup.CompanyName,
            Industry = startup.Industry?.IndustryName,
            Stage = startup.Stage?.ToString(),
            LogoURL = startup.LogoURL,
            Priority = (entry.Priority ?? WatchlistPriority.Medium).ToString(),
            AddedAt = entry.AddedAt
        });
    }

    public async Task<ApiResponse<PagedResponse<WatchlistItemDto>>> GetWatchlistAsync(int userId, int page, int pageSize)
    {
        var investor = await _db.Investors.AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
            return ApiResponse<PagedResponse<WatchlistItemDto>>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND",
                "Investor profile not found.");

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _db.InvestorWatchlists
            .AsNoTracking()
            .Where(w => w.InvestorID == investor.InvestorID && w.IsActive)
            .OrderByDescending(w => w.AddedAt);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WatchlistItemDto
            {
                WatchlistID = w.WatchlistID,
                StartupID = w.StartupID,
                CompanyName = w.Startup.CompanyName,
                Industry = w.Startup.Industry != null ? w.Startup.Industry.IndustryName : null,
                Stage = w.Startup.Stage != null ? w.Startup.Stage.ToString() : null,
                LogoURL = w.Startup.LogoURL,
                Priority = (w.Priority ?? WatchlistPriority.Medium).ToString(),
                AddedAt = w.AddedAt
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<WatchlistItemDto>>.SuccessResponse(new PagedResponse<WatchlistItemDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
            }
        });
    }

    public async Task<ApiResponse<string>> RemoveFromWatchlistAsync(int userId, int startupId)
    {
        var investor = await _db.Investors.AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
            return ApiResponse<string>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND",
                "Investor profile not found.");

        var entry = await _db.InvestorWatchlists
            .FirstOrDefaultAsync(w => w.InvestorID == investor.InvestorID
                                   && w.StartupID == startupId
                                   && w.IsActive);

        if (entry == null)
            return ApiResponse<string>.ErrorResponse("WATCHLIST_NOT_FOUND",
                "Startup not found in your watchlist.");

        entry.IsActive = false;
        entry.RemovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("REMOVE_WATCHLIST", "InvestorWatchlist", entry.WatchlistID, $"StartupId={startupId}");
        _logger.LogInformation("Investor {InvestorId} removed startup {StartupId} from watchlist",
            investor.InvestorID, startupId);

        return ApiResponse<string>.SuccessResponse("Removed");
    }

    // ================================================================
    // SEARCH STARTUPS
    // ================================================================

    public async Task<ApiResponse<PagedResponse<StartupSearchItemDto>>> SearchStartupsAsync(
        string? q, int? industryId, string? stage, string? location,
        string? sortBy, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _db.Startups.AsNoTracking()
            .Where(s => s.ProfileStatus == ProfileStatus.Approved || s.ProfileStatus == ProfileStatus.PendingKYC)
            .AsQueryable();

        // Keyword filter (company name)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(s =>
                s.CompanyName.ToLower().Contains(keyword) ||
                (s.Description != null && s.Description.ToLower().Contains(keyword)));
        }

        // Industry filter by ID
        if (industryId.HasValue)
        {
            query = query.Where(s => s.IndustryID == industryId.Value);
        }

        // Stage filter
        if (!string.IsNullOrWhiteSpace(stage))
        {
            if (Enum.TryParse<StartupStage>(stage, true, out var stageEnum))
                query = query.Where(s => s.Stage == stageEnum);
        }

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "createdat" => query.OrderByDescending(s => s.CreatedAt),
            "name" => query.OrderBy(s => s.CompanyName),
            _ => query.OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt) // default: updatedAt desc
        };

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StartupSearchItemDto
            {
                StartupID = s.StartupID,
                CompanyName = s.CompanyName,
                Stage = s.Stage != null ? s.Stage.ToString() : null,
                IndustryName = s.Industry != null ? s.Industry.IndustryName : null,
                LogoURL = s.LogoURL,
                ProfileStatus = s.ProfileStatus.ToString(),
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<StartupSearchItemDto>>.SuccessResponse(new PagedResponse<StartupSearchItemDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
            }
        });
    }

    public async Task<ApiResponse<InvestorDto>> SubmitForApprovalAsync(int userId)
    {
        var investor = await _db.Investors
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
            return ApiResponse<InvestorDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

        if (investor.ProfileStatus == ProfileStatus.Pending)
            return ApiResponse<InvestorDto>.ErrorResponse("ALREADY_PENDING", "Profile is already pending approval.");

        investor.ProfileStatus = ProfileStatus.PendingKYC;
        investor.UpdatedAt = DateTime.UtcNow;

        _db.Investors.Update(investor);
        await _db.SaveChangesAsync();

        return ApiResponse<InvestorDto>.SuccessResponse(MapToDto(investor));
    }

    public async Task<ApiResponse<InvestorKYCStatusDto>> GetKYCStatusAsync(int userId)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor not found");

        var status = new InvestorKYCStatusDto
        {
            WorkflowStatus = GetWorkflowStatus(investor),
            VerificationLabel = investor.InvestorTag.ToString().ToUpper(),
            Explanation = GetKYCExplanation(investor),
            LastUpdated = investor.UpdatedAt ?? investor.CreatedAt,
            Remarks = investor.Remarks,
            SubmittedData = new SubmitInvestorKYCRequest
            {
                InvestorCategory = investor.InvestorType == InvestorType.Institutional ? "INSTITUTIONAL" : "INDIVIDUAL_ANGEL",
                FullName = investor.FullName,
                ContactEmail = investor.ContactEmail ?? string.Empty,
                OrganizationName = investor.CurrentOrganization,
                CurrentRoleTitle = investor.CurrentRoleTitle,
                Location = investor.Location,
                Website = investor.Website,
                LinkedInURL = investor.LinkedInURL,
                SubmitterRole = investor.SubmitterRole,
                TaxIdOrBusinessCode = investor.BusinessCode
            }
        };

        return ApiResponse<InvestorKYCStatusDto>.SuccessResponse(status);
    }

    private string GetWorkflowStatus(Investor investor)
    {
        if (investor.InvestorTag == InvestorTag.VerifiedInvestorEntity || 
            investor.InvestorTag == InvestorTag.VerifiedAngelInvestor || 
            investor.InvestorTag == InvestorTag.BasicVerified)
        {
            return "VERIFIED";
        }

        return investor.ProfileStatus switch
        {
            ProfileStatus.PendingKYC => "PENDING_REVIEW",
            ProfileStatus.Rejected => "VERIFICATION_FAILED",
            ProfileStatus.Draft => "DRAFT",
            _ => "NOT_STARTED"
        };
    }

    private string GetKYCExplanation(Investor investor)
    {
        if (investor.InvestorTag == InvestorTag.VerifiedInvestorEntity || 
            investor.InvestorTag == InvestorTag.VerifiedAngelInvestor || 
            investor.InvestorTag == InvestorTag.BasicVerified)
        {
            return "Chúc mừng! Hồ sơ của bạn đã được xác thực đầy đủ.";
        }

        return investor.ProfileStatus switch
        {
            ProfileStatus.PendingKYC => "Hồ sơ xác thực của bạn đang được duyệt. Quá trình này thường mất 1-3 ngày làm việc.",
            ProfileStatus.Rejected => "Hồ sơ của bạn đã bị từ chối xác thực. Vui lòng kiểm tra nhận xét và cập nhật lại.",
            ProfileStatus.Draft => "Bạn đang có bản nháp Onboarding chưa hoàn tất. Tiếp tục để hoàn thiện hồ sơ cơ bản.",
            ProfileStatus.Approved => "Hồ sơ cơ bản của bạn đã hoàn tất. Hãy xác thực KYC để tăng độ uy tín.",
            _ => "Chào mừng! Hãy bắt đầu thiết lập hồ sơ Investor của bạn."
        };
    }

    public async Task<ApiResponse<InvestorKYCStatusDto>> SubmitKYCAsync(int userId, SubmitInvestorKYCRequest request, string? idProofUrl, string? investmentProofUrl)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("NOT_FOUND", "Investor not found");

        investor.InvestorType = request.InvestorCategory == "INSTITUTIONAL" ? InvestorType.Institutional : InvestorType.IndividualAngel;
        investor.FullName = request.FullName;
        investor.ContactEmail = request.ContactEmail;
        investor.CurrentOrganization = request.OrganizationName;
        investor.CurrentRoleTitle = request.CurrentRoleTitle;
        investor.Location = request.Location;
        investor.Website = request.Website;
        investor.LinkedInURL = request.LinkedInURL;
        investor.SubmitterRole = request.SubmitterRole;
        investor.BusinessCode = request.TaxIdOrBusinessCode;
        
        if (!string.IsNullOrEmpty(idProofUrl)) investor.IDProofFileURL = idProofUrl;
        if (!string.IsNullOrEmpty(investmentProofUrl)) investor.InvestmentProofFileURL = investmentProofUrl;

        investor.ProfileStatus = ProfileStatus.PendingKYC;
        investor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetKYCStatusAsync(userId);
    }

    public async Task<ApiResponse<InvestorKYCStatusDto>> SaveKYCDraftAsync(int userId, SaveInvestorKYCDraftRequest request)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("NOT_FOUND", "Investor not found");

        investor.InvestorType = request.InvestorCategory == "INSTITUTIONAL" ? InvestorType.Institutional : InvestorType.IndividualAngel;
        investor.FullName = request.FullName;
        investor.ContactEmail = request.ContactEmail;
        investor.CurrentOrganization = request.OrganizationName;
        investor.CurrentRoleTitle = request.CurrentRoleTitle;
        investor.Location = request.Location;
        investor.Website = request.Website;
        investor.LinkedInURL = request.LinkedInURL;
        investor.SubmitterRole = request.SubmitterRole;
        investor.BusinessCode = request.TaxIdOrBusinessCode;

        // Only set to Draft if it's currently None or unknown (for initial onboarding)
        if (investor.ProfileStatus != ProfileStatus.Approved && investor.ProfileStatus != ProfileStatus.PendingKYC)
        {
            investor.ProfileStatus = ProfileStatus.Draft;
        }
        investor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetKYCStatusAsync(userId);
    }

    // ================================================================
    // MAPPING
    // ================================================================

    private static InvestorDto MapToDto(Investor i) => new()
    {
        InvestorID = i.InvestorID,
        FullName = i.FullName,
        FirmName = i.FirmName,
        Title = i.Title,
        Bio = i.Bio,
        ProfilePhotoURL = i.ProfilePhotoURL,
        InvestmentThesis = i.InvestmentThesis,
        Location = i.Location,
        Country = i.Country,
        LinkedInURL = i.LinkedInURL,
        Website = i.Website,
        ProfileStatus = i.ProfileStatus.ToString(),
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,

        // KYC Information
        InvestorType = i.InvestorType?.ToString(),
        ContactEmail = i.ContactEmail,
        CurrentOrganization = i.CurrentOrganization,
        CurrentRoleTitle = i.CurrentRoleTitle,
        BusinessCode = i.BusinessCode,
        SubmitterRole = i.SubmitterRole,
        IDProofFileURL = i.IDProofFileURL,
        InvestmentProofFileURL = i.InvestmentProofFileURL,
        Remarks = i.Remarks
    };

    private static PreferencesDto MapPreferencesDto(Investor investor)
    {
        var pref = investor.Preferences;
        return new PreferencesDto
        {
            TicketMin = pref?.MinInvestmentSize,
            TicketMax = pref?.MaxInvestmentSize,
            PreferredStages = investor.StageFocus.Select(sf => sf.Stage.ToString()).ToList(),
            PreferredIndustries = investor.IndustryFocus.Select(inf => inf.Industry).ToList(),
            PreferredGeographies = pref?.PreferredGeographies,
            MinPotentialScore = pref?.MinPotentialScore,
            UpdatedAt = pref?.UpdatedAt
        };
    }
}
