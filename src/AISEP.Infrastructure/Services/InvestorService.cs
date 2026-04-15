using AISEP.Application.Const;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class InvestorService : IInvestorService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<InvestorService> _logger;
    private readonly ICloudinaryService _cloudinaryService;

    public InvestorService(ApplicationDbContext db, IAuditService audit, ILogger<InvestorService> logger, ICloudinaryService cloudinaryService)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
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
            return ApiResponse<InvestorDto>.SuccessResponse(null!, "Profile has not been created yet.");

        var activeSubmission = await _db.InvestorKycSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InvestorID == investor.InvestorID && s.IsActive);

        return ApiResponse<InvestorDto>.SuccessResponse(MapToDto(investor, activeSubmission));
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

        var activeSubmission = await _db.InvestorKycSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InvestorID == investor.InvestorID && s.IsActive);

        return ApiResponse<InvestorDto>.SuccessResponse(MapToDto(investor, activeSubmission));
    }

    public async Task<ApiResponse<AcceptingConnectionsDto>> SetAcceptingConnectionsAsync(int userId, bool acceptingConnections)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<AcceptingConnectionsDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND",
                "Investor profile not found.");

        // Three-gate check — only enforced when enabling the toggle
        if (acceptingConnections)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null || !user.IsActive)
                return ApiResponse<AcceptingConnectionsDto>.ErrorResponse("INVESTOR_ACCOUNT_INACTIVE",
                    "Tài khoản đã bị vô hiệu hóa.");

            if (investor.ProfileStatus != ProfileStatus.Approved)
                return ApiResponse<AcceptingConnectionsDto>.ErrorResponse("INVESTOR_PROFILE_NOT_APPROVED",
                    "Hồ sơ nhà đầu tư chưa được duyệt.");

            var kyc = await _db.InvestorKycSubmissions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.InvestorID == investor.InvestorID && s.IsActive);
            if (kyc == null || kyc.WorkflowStatus != InvestorKycWorkflowStatus.Approved)
                return ApiResponse<AcceptingConnectionsDto>.ErrorResponse("INVESTOR_KYC_NOT_APPROVED",
                    "KYC chưa được duyệt.");
        }

        if (investor.AcceptingConnections == acceptingConnections)
            return ApiResponse<AcceptingConnectionsDto>.SuccessResponse(
                new AcceptingConnectionsDto { AcceptingConnections = investor.AcceptingConnections });

        investor.AcceptingConnections = acceptingConnections;
        investor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var detail = acceptingConnections ? "AcceptingConnections=true" : "AcceptingConnections=false";
        await _audit.LogAsync("SET_ACCEPTING_CONNECTIONS", "Investor", investor.InvestorID, detail);
        _logger.LogInformation("Investor {InvestorId} set AcceptingConnections={Value}", investor.InvestorID, acceptingConnections);

        return ApiResponse<AcceptingConnectionsDto>.SuccessResponse(
            new AcceptingConnectionsDto { AcceptingConnections = investor.AcceptingConnections });
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
        investor.Preferences.PreferredMarketScopes = request.PreferredMarketScopes != null
            ? string.Join(",", request.PreferredMarketScopes)
            : null;
        investor.Preferences.SupportOffered = request.SupportOffered != null
            ? string.Join(",", request.SupportOffered)
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

        // Check startup exists and is discoverable
        var startup = await _db.Startups.AsNoTracking()
            .Include(s => s.Industry)
            .FirstOrDefaultAsync(s => s.StartupID == request.StartupId);

        if (startup == null)
            return ApiResponse<WatchlistItemDto>.ErrorResponse("STARTUP_NOT_FOUND",
                $"Startup with id {request.StartupId} not found.");

        if (startup.ProfileStatus != ProfileStatus.Approved || !startup.IsVisible)
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

        var aiScore = await GetLatestStartupAiScoreAsync(startup.StartupID);

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
            AddedAt = entry.AddedAt,
            AiScore = aiScore
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
            .Where(w => w.InvestorID == investor.InvestorID
                     && w.IsActive
                     && w.Startup.ProfileStatus == ProfileStatus.Approved
                     && w.Startup.IsVisible)
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
                AddedAt = w.AddedAt,
                AiScore = _db.AiEvaluationRuns
                    .Where(r => r.StartupId == w.StartupID
                             && r.OverallScore.HasValue
                             && (r.Status == "completed"
                              || r.Status == "COMPLETED"
                              || r.Status == "partial_completed"
                              || r.Status == "PARTIAL_COMPLETED"))
                    .OrderByDescending(r => r.UpdatedAt)
                    .Select(r => r.OverallScore)
                    .FirstOrDefault()
                    ?? _db.StartupPotentialScores
                        .Where(p => p.StartupID == w.StartupID && p.IsCurrentScore)
                        .OrderByDescending(p => p.CalculatedAt)
                        .Select(p => (double?)p.OverallScore)
                        .FirstOrDefault()
                    ?? _db.StartupPotentialScores
                        .Where(p => p.StartupID == w.StartupID)
                        .OrderByDescending(p => p.CalculatedAt)
                        .Select(p => (double?)p.OverallScore)
                        .FirstOrDefault()
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
              .Where(s => s.ProfileStatus == ProfileStatus.Approved && s.IsVisible)
              .AsQueryable();

        // Keyword filter (company name)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(s =>
                s.CompanyName.ToLower().Contains(keyword) ||
                (s.Description != null && s.Description.ToLower().Contains(keyword)));
        }

        // Industry filter by ID — cascade: matches startups in the selected industry OR any of its sub-industries
        if (industryId.HasValue)
        {
            var subIndustryIds = _db.Industries
                .Where(i => i.ParentIndustryID == industryId.Value)
                .Select(i => (int?)i.IndustryID);

            query = query.Where(s => s.IndustryID == industryId.Value || subIndustryIds.Contains(s.IndustryID));
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
                ParentIndustryName = s.Industry != null && s.Industry.ParentIndustry != null
                    ? s.Industry.ParentIndustry.IndustryName
                    : null,
                LogoURL = s.LogoURL,
                ProfileStatus = s.ProfileStatus.ToString(),
                UpdatedAt = s.UpdatedAt,
                CreatedAt = s.CreatedAt,
                FundingAmountSought = s.FundingAmountSought,
                CurrentFundingRaised = s.CurrentFundingRaised,
                AiScore = _db.AiEvaluationRuns
                    .Where(r => r.StartupId == s.StartupID
                             && r.OverallScore.HasValue
                             && (r.Status == "completed"
                              || r.Status == "COMPLETED"
                              || r.Status == "partial_completed"
                              || r.Status == "PARTIAL_COMPLETED"))
                    .OrderByDescending(r => r.UpdatedAt)
                    .Select(r => r.OverallScore)
                    .FirstOrDefault()
                    ?? _db.StartupPotentialScores
                        .Where(p => p.StartupID == s.StartupID && p.IsCurrentScore)
                        .OrderByDescending(p => p.CalculatedAt)
                        .Select(p => (double?)p.OverallScore)
                        .FirstOrDefault()
                    ?? _db.StartupPotentialScores
                        .Where(p => p.StartupID == s.StartupID)
                        .OrderByDescending(p => p.CalculatedAt)
                        .Select(p => (double?)p.OverallScore)
                        .FirstOrDefault()
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

    private async Task<double?> GetLatestStartupAiScoreAsync(int startupId)
    {
        var runScore = await _db.AiEvaluationRuns
            .AsNoTracking()
            .Where(r => r.StartupId == startupId
                     && r.OverallScore.HasValue
                     && (r.Status == "completed"
                      || r.Status == "COMPLETED"
                      || r.Status == "partial_completed"
                      || r.Status == "PARTIAL_COMPLETED"))
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => r.OverallScore)
            .FirstOrDefaultAsync();

        if (runScore.HasValue)
            return runScore.Value;

        var currentPotential = await _db.StartupPotentialScores
            .AsNoTracking()
            .Where(p => p.StartupID == startupId && p.IsCurrentScore)
            .OrderByDescending(p => p.CalculatedAt)
            .Select(p => (double?)p.OverallScore)
            .FirstOrDefaultAsync();

        if (currentPotential.HasValue)
            return currentPotential.Value;

        return await _db.StartupPotentialScores
            .AsNoTracking()
            .Where(p => p.StartupID == startupId)
            .OrderByDescending(p => p.CalculatedAt)
            .Select(p => (double?)p.OverallScore)
            .FirstOrDefaultAsync();
    }

    public async Task<ApiResponse<InvestorDto>> UploadPhotoAsync(int userId, IFormFile photo)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

        if (photo == null || photo.Length == 0)
            return ApiResponse<InvestorDto>.ErrorResponse("INVALID_FILE", "Please provide a valid image file.");

        try
        {
            var profilePhotoUrl = await _cloudinaryService.UploadImage(photo, CloudinaryFolderSaving.Profile);
            
            if (!string.IsNullOrEmpty(investor.ProfilePhotoURL))
            {
                await _cloudinaryService.DeleteImage(investor.ProfilePhotoURL);
            }

            investor.ProfilePhotoURL = profilePhotoUrl;
            investor.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync("UPLOAD_INVESTOR_PHOTO", "Investor", investor.InvestorID, null);
            _logger.LogInformation("Investor {InvestorId} uploaded new profile photo", investor.InvestorID);

            var activeSubmission = await _db.InvestorKycSubmissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.InvestorID == investor.InvestorID && s.IsActive);

            return ApiResponse<InvestorDto>.SuccessResponse(MapToDto(investor, activeSubmission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading investor photo for user {UserId}", userId);
            return ApiResponse<InvestorDto>.ErrorResponse("UPLOAD_FAILED", "Failed to upload photo to storage service.");
        }
    }

    public async Task<ApiResponse<InvestorKYCStatusDto>> GetKYCStatusAsync(int userId)
    {
        var investor = await _db.Investors.AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor not found");

        var allSubmissions = await _db.InvestorKycSubmissions
            .AsNoTracking()
            .Include(s => s.EvidenceFiles)
            .Where(s => s.InvestorID == investor.InvestorID)
            .OrderByDescending(s => s.Version)
            .ToListAsync();

        if (allSubmissions.Count == 0)
        {
            return ApiResponse<InvestorKYCStatusDto>.SuccessResponse(new InvestorKYCStatusDto
            {
                WorkflowStatus = MapWorkflowStatus(InvestorKycWorkflowStatus.NotSubmitted),
                VerificationLabel = MapResultLabel(InvestorKycResultLabel.None),
                Explanation = GetDefaultExplanation(InvestorKycWorkflowStatus.NotSubmitted, InvestorKycResultLabel.None),
                RequiresNewEvidence = false,
                LastUpdated = investor.UpdatedAt ?? investor.CreatedAt
            });
        }

        var activeSubmission = allSubmissions.FirstOrDefault(s => s.IsActive) ?? allSubmissions[0];
        return ApiResponse<InvestorKYCStatusDto>.SuccessResponse(MapToKycStatusDto(investor, activeSubmission, allSubmissions));
    }

    public async Task<ApiResponse<InvestorKYCStatusDto>> SubmitKYCAsync(int userId, SubmitInvestorKYCRequest request)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor not found");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("FULL_NAME_REQUIRED", "FullName is required.");

        if (string.IsNullOrWhiteSpace(request.ContactEmail))
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("CONTACT_EMAIL_REQUIRED", "ContactEmail is required.");

        var now = DateTime.UtcNow;
        var latestDraft = await _db.InvestorKycSubmissions
            .Include(s => s.EvidenceFiles)
            .Where(s => s.InvestorID == investor.InvestorID && s.WorkflowStatus == InvestorKycWorkflowStatus.Draft && !s.IsActive)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync();

        var activeSubmission = await _db.InvestorKycSubmissions
            .Include(s => s.EvidenceFiles)
            .FirstOrDefaultAsync(s => s.InvestorID == investor.InvestorID && s.IsActive);
        var requiresNewEvidence = activeSubmission?.RequiresNewEvidence ?? false;

        if (request.EvidenceFiles.Count == 0)
        {
            var hasDraftEvidence = latestDraft?.EvidenceFiles.Count > 0;
            var hasActiveEvidence = activeSubmission?.EvidenceFiles.Count > 0;

            if (activeSubmission == null && !hasDraftEvidence)
                return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "At least one evidence file is required when submitting KYC.");

            if (requiresNewEvidence)
                return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "New evidence files are required before you can resubmit this KYC case.");

            if (!hasDraftEvidence && !hasActiveEvidence)
                return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "At least one evidence file is required when submitting KYC.");
        }

        InvestorKycSubmission submission;
        if (latestDraft != null)
        {
            submission = latestDraft;
        }
        else
        {
            submission = new InvestorKycSubmission
            {
                InvestorID = investor.InvestorID,
                Version = await GetNextSubmissionVersionAsync(investor.InvestorID),
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.InvestorKycSubmissions.Add(submission);
        }

        if (activeSubmission != null && activeSubmission.SubmissionID != submission.SubmissionID)
        {
            activeSubmission.IsActive = false;
            activeSubmission.UpdatedAt = now;
            if (activeSubmission.WorkflowStatus == InvestorKycWorkflowStatus.UnderReview
                || activeSubmission.WorkflowStatus == InvestorKycWorkflowStatus.PendingMoreInfo
                || activeSubmission.WorkflowStatus == InvestorKycWorkflowStatus.Draft)
            {
                activeSubmission.WorkflowStatus = InvestorKycWorkflowStatus.Superseded;
            }
        }

        ApplyKycSubmissionPayload(submission, request, now);
        submission.IsActive = true;
        submission.WorkflowStatus = InvestorKycWorkflowStatus.UnderReview;
        submission.ResultLabel = InvestorKycResultLabel.None;
        submission.SubmittedAt = now;
        submission.Explanation = "KYC submission is under review.";
        submission.Remarks = null;
        submission.RequiresNewEvidence = false;

        if (request.EvidenceFiles.Count > 0)
            await ReplaceEvidenceFilesAsync(submission, request.EvidenceFiles, request.EvidenceFileKinds, now);
        else if (submission.EvidenceFiles.Count == 0 && activeSubmission != null)
            CopyEvidenceFiles(activeSubmission, submission);

        investor.InvestorTag = InvestorTag.None;
        investor.ProfileStatus = ProfileStatus.PendingKYC;
        investor.UpdatedAt = now;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("SUBMIT_INVESTOR_KYC", "Investor", investor.InvestorID,
            $"Investor submitted KYC version {submission.Version}");

        return await GetKYCStatusAsync(userId);
    }

    public async Task<ApiResponse<InvestorKYCStatusDto>> SaveKYCDraftAsync(int userId, SaveInvestorKYCDraftRequest request)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<InvestorKYCStatusDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor not found");

        var now = DateTime.UtcNow;
        var draft = await _db.InvestorKycSubmissions
            .Include(s => s.EvidenceFiles)
            .Where(s => s.InvestorID == investor.InvestorID && s.WorkflowStatus == InvestorKycWorkflowStatus.Draft && !s.IsActive)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync();

        if (draft == null)
        {
            draft = new InvestorKycSubmission
            {
                InvestorID = investor.InvestorID,
                Version = await GetNextSubmissionVersionAsync(investor.InvestorID),
                CreatedAt = now,
                UpdatedAt = now,
                WorkflowStatus = InvestorKycWorkflowStatus.Draft,
                ResultLabel = InvestorKycResultLabel.None
            };
            _db.InvestorKycSubmissions.Add(draft);
        }

        ApplyDraftPayload(draft, request, now);
        draft.IsActive = false;
        draft.WorkflowStatus = InvestorKycWorkflowStatus.Draft;
        draft.ResultLabel = InvestorKycResultLabel.None;
        draft.Explanation = "KYC draft saved.";

        if (request.EvidenceFiles != null && request.EvidenceFiles.Count > 0)
            await ReplaceEvidenceFilesAsync(draft, request.EvidenceFiles, request.EvidenceFileKinds, now);

        investor.UpdatedAt = now;
        await _db.SaveChangesAsync();

        return await GetKYCStatusAsync(userId);
    }

    // ================================================================
    // KYC HELPER METHODS
    // ================================================================

    private async Task<int> GetNextSubmissionVersionAsync(int investorId)
    {
        var latestVersion = await _db.InvestorKycSubmissions
            .Where(s => s.InvestorID == investorId)
            .Select(s => (int?)s.Version)
            .MaxAsync();
        return (latestVersion ?? 0) + 1;
    }

    private async Task ReplaceEvidenceFilesAsync(InvestorKycSubmission submission, List<IFormFile> files, List<string>? kinds, DateTime now)
    {
        if (files.Count == 0) return;

        if (submission.EvidenceFiles.Count > 0)
        {
            _db.InvestorKycEvidenceFiles.RemoveRange(submission.EvidenceFiles);
            submission.EvidenceFiles.Clear();
        }

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var uploadedFile = await _cloudinaryService.UploadDocumentWithMetadata(file, CloudinaryFolderSaving.DocumentStorage);
            var kind = ParseEvidenceKind(kinds?.ElementAtOrDefault(index));

            submission.EvidenceFiles.Add(new InvestorKycEvidenceFile
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileUrl = uploadedFile.Url,
                StorageKey = uploadedFile.PublicId,
                Kind = kind,
                FileSize = file.Length,
                UploadedAt = now
            });
        }
    }

    private static void CopyEvidenceFiles(InvestorKycSubmission source, InvestorKycSubmission target)
    {
        if (target.EvidenceFiles.Count > 0) return;
        foreach (var file in source.EvidenceFiles.OrderBy(f => f.UploadedAt))
        {
            target.EvidenceFiles.Add(new InvestorKycEvidenceFile
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileUrl = file.FileUrl,
                StorageKey = file.StorageKey,
                Kind = file.Kind,
                FileSize = file.FileSize,
                UploadedAt = file.UploadedAt
            });
        }
    }

    private static void ApplyKycSubmissionPayload(InvestorKycSubmission submission, SubmitInvestorKYCRequest request, DateTime now)
    {
        submission.InvestorCategory = request.InvestorCategory;
        submission.FullName = request.FullName;
        submission.ContactEmail = request.ContactEmail;
        submission.OrganizationName = request.OrganizationName;
        submission.CurrentRoleTitle = request.CurrentRoleTitle;
        submission.Location = request.Location;
        submission.Website = request.Website;
        submission.LinkedInURL = request.LinkedInURL;
        submission.SubmitterRole = request.SubmitterRole;
        submission.TaxIdOrBusinessCode = request.TaxIdOrBusinessCode;
        submission.UpdatedAt = now;
    }

    private static void ApplyDraftPayload(InvestorKycSubmission submission, SaveInvestorKYCDraftRequest request, DateTime now)
    {
        if (request.InvestorCategory != null) submission.InvestorCategory = request.InvestorCategory;
        if (request.FullName != null) submission.FullName = request.FullName;
        if (request.ContactEmail != null) submission.ContactEmail = request.ContactEmail;
        if (request.OrganizationName != null) submission.OrganizationName = request.OrganizationName;
        if (request.CurrentRoleTitle != null) submission.CurrentRoleTitle = request.CurrentRoleTitle;
        if (request.Location != null) submission.Location = request.Location;
        if (request.Website != null) submission.Website = request.Website;
        if (request.LinkedInURL != null) submission.LinkedInURL = request.LinkedInURL;
        if (request.SubmitterRole != null) submission.SubmitterRole = request.SubmitterRole;
        if (request.TaxIdOrBusinessCode != null) submission.TaxIdOrBusinessCode = request.TaxIdOrBusinessCode;
        submission.UpdatedAt = now;
    }

    private InvestorKYCStatusDto MapToKycStatusDto(Investor investor, InvestorKycSubmission submission, List<InvestorKycSubmission> allSubmissions)
    {
        return new InvestorKYCStatusDto
        {
            WorkflowStatus = MapWorkflowStatus(submission.WorkflowStatus),
            VerificationLabel = submission.WorkflowStatus == InvestorKycWorkflowStatus.Approved
                ? MapResultLabel(submission.ResultLabel)
                : investor.InvestorTag != InvestorTag.None ? investor.InvestorTag.ToString().ToUpper() : MapResultLabel(submission.ResultLabel),
            Explanation = string.IsNullOrWhiteSpace(submission.Explanation)
                ? GetDefaultExplanation(submission.WorkflowStatus, submission.ResultLabel)
                : submission.Explanation,
            Remarks = submission.Remarks,
            RequiresNewEvidence = submission.RequiresNewEvidence,
            SubmissionId = submission.SubmissionID,
            Version = submission.Version,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt,
            LastUpdated = submission.UpdatedAt,
            SubmissionSummary = MapToSubmissionSummaryDto(submission),
            History = allSubmissions
                .Where(s => s.WorkflowStatus != InvestorKycWorkflowStatus.Draft)
                .Select(s => new InvestorKYCHistoryItemDto
                {
                    SubmissionId = s.SubmissionID,
                    Version = s.Version,
                    WorkflowStatus = MapWorkflowStatus(s.WorkflowStatus),
                    ResultLabel = MapResultLabel(s.ResultLabel),
                    SubmittedAt = s.SubmittedAt,
                    ReviewedAt = s.ReviewedAt,
                    Remarks = s.Remarks,
                    RequiresNewEvidence = s.RequiresNewEvidence
                })
                .ToList()
        };
    }

    private InvestorKYCSubmissionSummaryDto MapToSubmissionSummaryDto(InvestorKycSubmission submission)
    {
        return new InvestorKYCSubmissionSummaryDto
        {
            FullName = submission.FullName,
            InvestorCategory = submission.InvestorCategory,
            ContactEmail = submission.ContactEmail,
            OrganizationName = submission.OrganizationName,
            CurrentRoleTitle = submission.CurrentRoleTitle,
            Location = submission.Location,
            Website = submission.Website,
            LinkedInURL = submission.LinkedInURL,
            SubmitterRole = submission.SubmitterRole,
            TaxIdOrBusinessCode = submission.TaxIdOrBusinessCode,
            SubmittedAt = submission.SubmittedAt,
            Version = submission.Version,
            EvidenceFiles = submission.EvidenceFiles
                .OrderBy(f => f.UploadedAt)
                .Select(f => new InvestorKYCEvidenceFileDto
                {
                    Id = f.EvidenceFileID,
                    FileName = f.FileName,
                    FileType = f.ContentType,
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt,
                    Kind = MapEvidenceKind(f.Kind),
                    Url = _cloudinaryService.GenerateSignedDocumentUrl(f.StorageKey, f.FileUrl, f.FileName),
                    StorageKey = !string.IsNullOrWhiteSpace(f.StorageKey)
                        ? f.StorageKey
                        : _cloudinaryService.ExtractDocumentStorageKeyFromUrl(f.FileUrl)
                })
                .ToList()
        };
    }

    private static string MapWorkflowStatus(InvestorKycWorkflowStatus status) => status switch
    {
        InvestorKycWorkflowStatus.NotSubmitted => "NOT_STARTED",
        InvestorKycWorkflowStatus.Draft        => "DRAFT",
        InvestorKycWorkflowStatus.UnderReview  => "PENDING_REVIEW",
        InvestorKycWorkflowStatus.PendingMoreInfo => "PENDING_MORE_INFO",
        InvestorKycWorkflowStatus.Approved     => "VERIFIED",
        InvestorKycWorkflowStatus.Rejected     => "VERIFICATION_FAILED",
        _ => "UNKNOWN"
    };

    private static string MapResultLabel(InvestorKycResultLabel label) => label switch
    {
        InvestorKycResultLabel.VerifiedInvestorEntity => "VERIFIED_INVESTOR_ENTITY",
        InvestorKycResultLabel.VerifiedAngelInvestor  => "VERIFIED_ANGEL_INVESTOR",
        InvestorKycResultLabel.BasicVerified          => "BASIC_VERIFIED",
        InvestorKycResultLabel.PendingMoreInfo        => "PENDING_MORE_INFO",
        InvestorKycResultLabel.VerificationFailed     => "VERIFICATION_FAILED",
        _ => "NONE"
    };

    private static string GetDefaultExplanation(InvestorKycWorkflowStatus status, InvestorKycResultLabel label) => status switch
    {
        InvestorKycWorkflowStatus.NotSubmitted    => "Chào mừng! Hãy bắt đầu thiết lập hồ sơ xác thực Investor của bạn.",
        InvestorKycWorkflowStatus.Draft           => "Bạn đang lưu nháp hồ sơ xác thực. Hãy hoàn thiện và gửi để được xem xét.",
        InvestorKycWorkflowStatus.UnderReview     => "Hồ sơ xác thực của bạn đang được duyệt. Quá trình này thường mất 1-3 ngày làm việc.",
        InvestorKycWorkflowStatus.PendingMoreInfo => "Hồ sơ cần bổ sung thêm thông tin. Vui lòng xem ghi chú từ Staff và nộp lại.",
        InvestorKycWorkflowStatus.Approved        => "Chúc mừng! Hồ sơ của bạn đã được xác thực đầy đủ.",
        InvestorKycWorkflowStatus.Rejected        => "Hồ sơ không đáp ứng tiêu chuẩn xác thực. Vui lòng xem lại ghi chú và gửi lại.",
        _ => string.Empty
    };

    private static InvestorKycEvidenceKind ParseEvidenceKind(string? kind) => kind?.ToUpperInvariant() switch
    {
        "ID_PROOF"         => InvestorKycEvidenceKind.IDProof,
        "INVESTMENT_PROOF" => InvestorKycEvidenceKind.InvestmentProof,
        _ => InvestorKycEvidenceKind.Other
    };

    private static string MapEvidenceKind(InvestorKycEvidenceKind kind) => kind switch
    {
        InvestorKycEvidenceKind.IDProof         => "ID_PROOF",
        InvestorKycEvidenceKind.InvestmentProof => "INVESTMENT_PROOF",
        _ => "OTHER"
    };

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
        AcceptingConnections = i.AcceptingConnections,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt
    };

    private static InvestorDto MapToDto(Investor i, InvestorKycSubmission? submission)
    {
        var dto = MapToDto(i);
        if (submission == null) return dto;
        dto.KycStatus = MapWorkflowStatus(submission.WorkflowStatus);
        dto.InvestorType = submission.InvestorCategory;
        dto.ContactEmail = submission.ContactEmail;
        dto.CurrentOrganization = submission.OrganizationName;
        dto.CurrentRoleTitle = submission.CurrentRoleTitle;
        dto.BusinessCode = submission.TaxIdOrBusinessCode;
        dto.SubmitterRole = submission.SubmitterRole;
        dto.Remarks = submission.Remarks;
        return dto;
    }

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
            PreferredMarketScopes = pref?.PreferredMarketScopes != null
                ? pref.PreferredMarketScopes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new(),
            SupportOffered = pref?.SupportOffered != null
                ? pref.SupportOffered.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new(),
            UpdatedAt = pref?.UpdatedAt
        };
    }

    // ================================================================
    // INDUSTRY FOCUS
    // ================================================================

    public async Task<ApiResponse<List<IndustryFocusDto>>> GetIndustryFocusAsync(int userId)
    {
        var investor = await _db.Investors.AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<List<IndustryFocusDto>>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found");

        var items = await _db.InvestorIndustryFocuses
            .Where(f => f.InvestorID == investor.InvestorID)
            .AsNoTracking()
            .Select(f => new IndustryFocusDto { FocusId = f.FocusID, Industry = f.Industry })
            .ToListAsync();

        return ApiResponse<List<IndustryFocusDto>>.SuccessResponse(items);
    }

    public async Task<ApiResponse<IndustryFocusDto>> AddIndustryFocusAsync(int userId, AddIndustryFocusRequest request)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<IndustryFocusDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found");

        var exists = await _db.InvestorIndustryFocuses
            .AnyAsync(f => f.InvestorID == investor.InvestorID && f.Industry == request.Industry);
        if (exists)
            return ApiResponse<IndustryFocusDto>.ErrorResponse("INDUSTRY_FOCUS_ALREADY_EXISTS", "Industry focus already exists");

        var focus = new InvestorIndustryFocus
        {
            InvestorID = investor.InvestorID,
            Industry = request.Industry
        };
        _db.InvestorIndustryFocuses.Add(focus);
        await _db.SaveChangesAsync();

        return ApiResponse<IndustryFocusDto>.SuccessResponse(
            new IndustryFocusDto { FocusId = focus.FocusID, Industry = focus.Industry }, "Industry focus added");
    }

    public async Task<ApiResponse<string>> RemoveIndustryFocusAsync(int userId, int focusId)
    {
        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<string>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found");

        var focus = await _db.InvestorIndustryFocuses
            .FirstOrDefaultAsync(f => f.FocusID == focusId && f.InvestorID == investor.InvestorID);
        if (focus == null)
            return ApiResponse<string>.ErrorResponse("INDUSTRY_FOCUS_NOT_FOUND", "Industry focus not found");

        _db.InvestorIndustryFocuses.Remove(focus);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse("Removed");
    }

    // ================================================================
    // STAGE FOCUS
    // ================================================================

    public async Task<ApiResponse<List<StageFocusDto>>> GetStageFocusAsync(int userId)
    {
        var investor = await _db.Investors.AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<List<StageFocusDto>>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found");

        var items = await _db.InvestorStageFocuses
            .Where(f => f.InvestorID == investor.InvestorID)
            .AsNoTracking()
            .Select(f => new StageFocusDto { StageFocusId = f.StageFocusID, Stage = f.Stage.ToString() })
            .ToListAsync();

        return ApiResponse<List<StageFocusDto>>.SuccessResponse(items);
    }

    public async Task<ApiResponse<StageFocusDto>> AddStageFocusAsync(int userId, AddStageFocusRequest request)
    {
        var investor = await _db.Investors.FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<StageFocusDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found");

        var exists = await _db.InvestorStageFocuses
            .AnyAsync(f => f.InvestorID == investor.InvestorID && f.Stage == request.Stage);
        if (exists)
            return ApiResponse<StageFocusDto>.ErrorResponse("STAGE_FOCUS_ALREADY_EXISTS", "Stage focus already exists");

        var focus = new InvestorStageFocus
        {
            InvestorID = investor.InvestorID,
            Stage = request.Stage
        };
        _db.InvestorStageFocuses.Add(focus);
        await _db.SaveChangesAsync();

        return ApiResponse<StageFocusDto>.SuccessResponse(
            new StageFocusDto { StageFocusId = focus.StageFocusID, Stage = focus.Stage.ToString() }, "Stage focus added");
    }

    public async Task<ApiResponse<string>> RemoveStageFocusAsync(int userId, int stageFocusId)
    {
        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<string>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found");

        var focus = await _db.InvestorStageFocuses
            .FirstOrDefaultAsync(f => f.StageFocusID == stageFocusId && f.InvestorID == investor.InvestorID);
        if (focus == null)
            return ApiResponse<string>.ErrorResponse("STAGE_FOCUS_NOT_FOUND", "Stage focus not found");

        _db.InvestorStageFocuses.Remove(focus);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse("Removed");
    }

    // ================================================================
    // COMPARE STARTUPS
    // ================================================================

    public async Task<ApiResponse<List<StartupCompareDto>>> CompareStartupsAsync(List<int> startupIds)
    {
        if (startupIds.Count < 2 || startupIds.Count > 5)
            return ApiResponse<List<StartupCompareDto>>.ErrorResponse("VALIDATION_ERROR",
                "Please provide 2-5 startup IDs to compare");

        var startups = await _db.Startups
            .Include(s => s.Industry)
            .Include(s => s.TeamMembers)
            .Where(s => startupIds.Contains(s.StartupID))
            .AsNoTracking()
            .Select(s => new StartupCompareDto
            {
                StartupID = s.StartupID,
                CompanyName = s.CompanyName,
                OneLiner = s.OneLiner,
                Stage = s.Stage.ToString(),
                IndustryName = s.Industry != null ? s.Industry.IndustryName : null,
                FundingAmountSought = s.FundingAmountSought,
                CurrentFundingRaised = s.CurrentFundingRaised,
                Valuation = s.Valuation,
                TeamSize = s.TeamMembers.Count(),
                LogoURL = s.LogoURL,
                FoundedDate = s.FoundedDate,
                ProfileStatus = s.ProfileStatus.ToString()
            })
            .ToListAsync();

        return ApiResponse<List<StartupCompareDto>>.SuccessResponse(startups);
    }
}
