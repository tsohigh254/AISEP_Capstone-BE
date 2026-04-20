using AISEP.Application.DTOs.Moderation;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AISEP.Tests.Services;

public class ModerationServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IAuditService> _audit = new();
    private readonly ModerationService _sut;

    public ModerationServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<ModerationService>>();
        _sut = new ModerationService(_db, _audit.Object, logger.Object);
    }

    private FlaggedContent SeedFlag(ModerationStatus status = ModerationStatus.None, int? relatedUserId = null, int? reviewedBy = null)
    {
        var f = new FlaggedContent
        {
            ContentType = "Message",
            ContentID = 1,
            RelatedUserID = relatedUserId,
            FlagReason = "Spam",
            FlagSource = "UserReport",
            Severity = "Medium",
            ModerationStatus = status,
            FlaggedAt = DateTime.UtcNow,
            ReviewedBy = reviewedBy
        };
        _db.FlaggedContents.Add(f);
        _db.SaveChanges();
        return f;
    }

    private User SeedUser(int? forceId = null)
    {
        var u = new User
        {
            Email = $"user{forceId ?? 0}@test.com",
            PasswordHash = "hash",
            UserType = "Startup",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(u);
        _db.SaveChanges();
        return u;
    }

    // ── GetFlagDetailAsync ───────────────────────────────────────

    [Fact]
    public async Task GetFlagDetailAsync_WhenNotFound_ReturnsError()
    {
        var result = await _sut.GetFlagDetailAsync(999);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("FLAG_NOT_FOUND");
    }

    [Fact]
    public async Task GetFlagDetailAsync_WhenExists_ReturnsDetail()
    {
        var f = SeedFlag();

        var result = await _sut.GetFlagDetailAsync(f.FlagID);

        result.Success.Should().BeTrue();
        result.Data!.FlagReason.Should().Be("Spam");
        result.Data.ContentType.Should().Be("Message");
    }

    // ── AssignAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AssignAsync_WhenNotFound_ReturnsError()
    {
        var result = await _sut.AssignAsync(staffUserId: 1, flagId: 999, note: null);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("FLAG_NOT_FOUND");
    }

    [Fact]
    public async Task AssignAsync_WhenAlreadyAssigned_ReturnsError()
    {
        var f = SeedFlag(status: ModerationStatus.None, reviewedBy: 5);

        var result = await _sut.AssignAsync(staffUserId: 1, f.FlagID, null);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("INVALID_STATUS_TRANSITION");
    }

    [Fact]
    public async Task AssignAsync_WhenAlreadyResolved_ReturnsError()
    {
        var f = SeedFlag(status: ModerationStatus.Approve);

        var result = await _sut.AssignAsync(staffUserId: 1, f.FlagID, null);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("INVALID_STATUS_TRANSITION");
    }

    [Fact]
    public async Task AssignAsync_WhenValid_SetsReviewerAndCreatesAction()
    {
        var f = SeedFlag();

        var result = await _sut.AssignAsync(staffUserId: 10, f.FlagID, "I'll handle this");

        result.Success.Should().BeTrue();
        var reloaded = await _db.FlaggedContents.FindAsync(f.FlagID);
        reloaded!.ReviewedBy.Should().Be(10);
        reloaded.ModeratorNotes.Should().Be("I'll handle this");
        _db.ModerationActions.Should().ContainSingle(a => a.FlagID == f.FlagID && a.ActionType == "Assign");
        _audit.Verify(a => a.LogAsync("MODERATION_ASSIGN", "FlaggedContent", f.FlagID, It.IsAny<string>()), Times.Once);
    }

    // ── ResolveAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_WhenNotFound_ReturnsError()
    {
        var result = await _sut.ResolveAsync(1, 999, "Approve", null);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("FLAG_NOT_FOUND");
    }

    [Fact]
    public async Task ResolveAsync_WhenAlreadyResolved_ReturnsError()
    {
        var f = SeedFlag(status: ModerationStatus.Approve);

        var result = await _sut.ResolveAsync(1, f.FlagID, "Reject", null);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("INVALID_STATUS_TRANSITION");
    }

    [Fact]
    public async Task ResolveAsync_WithApproveDecision_SetsStatusAndLogsAudit()
    {
        var f = SeedFlag();

        var result = await _sut.ResolveAsync(staffUserId: 1, f.FlagID, "Approve", "Looks fine");

        result.Success.Should().BeTrue();
        var reloaded = await _db.FlaggedContents.FindAsync(f.FlagID);
        reloaded!.ModerationStatus.Should().Be(ModerationStatus.Approve);
        reloaded.ReviewedAt.Should().NotBeNull();
        reloaded.ReviewedBy.Should().Be(1);
        reloaded.ModeratorNotes.Should().Be("Looks fine");
        _audit.Verify(a => a.LogAsync("MODERATION_RESOLVE", "FlaggedContent", f.FlagID, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WithRejectReportDecision_MapsToRejectStatus()
    {
        var f = SeedFlag();

        var result = await _sut.ResolveAsync(1, f.FlagID, "RejectReport", null);

        result.Success.Should().BeTrue();
        (await _db.FlaggedContents.FindAsync(f.FlagID))!.ModerationStatus.Should().Be(ModerationStatus.Reject);
    }

    // ── CreateActionAsync ────────────────────────────────────────

    [Fact]
    public async Task CreateActionAsync_WhenFlagNotFound_ReturnsError()
    {
        var result = await _sut.CreateActionAsync(1, 999, new CreateModerationActionRequest { ActionType = "Warn" });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("FLAG_NOT_FOUND");
    }

    [Fact]
    public async Task CreateActionAsync_WithInvalidActionType_ReturnsValidationError()
    {
        var f = SeedFlag();

        var result = await _sut.CreateActionAsync(1, f.FlagID, new CreateModerationActionRequest { ActionType = "Explode" });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateActionAsync_WithWarn_RecordsAction()
    {
        var f = SeedFlag();

        var result = await _sut.CreateActionAsync(1, f.FlagID, new CreateModerationActionRequest
        {
            ActionType = "Warn",
            ActionNote = "First warning"
        });

        result.Success.Should().BeTrue();
        result.Data!.ActionType.Should().Be("Warn");
        _db.ModerationActions.Should().ContainSingle(a => a.ActionType == "Warn" && a.FlagID == f.FlagID);
    }

    [Fact]
    public async Task CreateActionAsync_LockUser_DeactivatesUser()
    {
        var targetUser = SeedUser();
        var f = SeedFlag(relatedUserId: targetUser.UserID);

        var result = await _sut.CreateActionAsync(1, f.FlagID, new CreateModerationActionRequest { ActionType = "LockUser" });

        result.Success.Should().BeTrue();
        (await _db.Users.FindAsync(targetUser.UserID))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateActionAsync_UnlockUser_ReactivatesUser()
    {
        var targetUser = SeedUser();
        targetUser.IsActive = false;
        await _db.SaveChangesAsync();
        var f = SeedFlag(relatedUserId: targetUser.UserID);

        var result = await _sut.CreateActionAsync(1, f.FlagID, new CreateModerationActionRequest { ActionType = "UnlockUser" });

        result.Success.Should().BeTrue();
        (await _db.Users.FindAsync(targetUser.UserID))!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateActionAsync_BanUser_DeactivatesUser()
    {
        var targetUser = SeedUser();
        var f = SeedFlag(relatedUserId: targetUser.UserID);

        var result = await _sut.CreateActionAsync(1, f.FlagID, new CreateModerationActionRequest { ActionType = "BanUser" });

        result.Success.Should().BeTrue();
        (await _db.Users.FindAsync(targetUser.UserID))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateActionAsync_WithDurationDays_SetsExpiresAt()
    {
        var f = SeedFlag();

        var result = await _sut.CreateActionAsync(1, f.FlagID, new CreateModerationActionRequest
        {
            ActionType = "Warn",
            DurationDays = 30
        });

        result.Success.Should().BeTrue();
        result.Data!.ExpiresAt.Should().NotBeNull();
        result.Data.ExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
    }

    // ── GetActionsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetActionsAsync_WhenFlagNotFound_ReturnsError()
    {
        var result = await _sut.GetActionsAsync(999);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("FLAG_NOT_FOUND");
    }

    [Fact]
    public async Task GetActionsAsync_ReturnsActionsForFlag()
    {
        var f = SeedFlag();
        _db.ModerationActions.Add(new ModerationAction
        {
            FlagID = f.FlagID,
            ActionType = "Warn",
            ActionTakenBy = 1,
            ActionTakenAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetActionsAsync(f.FlagID);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].ActionType.Should().Be("Warn");
    }

    // ── CreateFlagAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateFlagAsync_PersistsFlagAndLogsAudit()
    {
        var result = await _sut.CreateFlagAsync(reporterUserId: 1, new CreateFlagRequest
        {
            EntityType = "Message",
            EntityId = 42,
            Reason = "Inappropriate",
            Description = "Bad language"
        });

        result.Success.Should().BeTrue();
        result.Data!.ContentType.Should().Be("Message");
        result.Data.FlagReason.Should().Be("Inappropriate");
        _db.FlaggedContents.Should().ContainSingle();
        _audit.Verify(a => a.LogAsync("CREATE_REPORT", "FlaggedContent", It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    // ── GetMyReportsAsync ────────────────────────────────────────

    [Fact]
    public async Task GetMyReportsAsync_ReturnsNotImplemented()
    {
        var result = await _sut.GetMyReportsAsync(userId: 1, page: 1, pageSize: 10);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("NOT_IMPLEMENTED");
    }

    // ── GetFlagsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetFlagsAsync_ReturnsAllFlags()
    {
        SeedFlag();
        SeedFlag();

        var result = await _sut.GetFlagsAsync(null, null, null, null, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFlagsAsync_FilterByEntityType_ReturnsMatching()
    {
        SeedFlag(); // ContentType = "Message"
        var f2 = new FlaggedContent
        {
            ContentType = "Profile",
            ContentID = 2,
            FlagReason = "Fake",
            ModerationStatus = ModerationStatus.None,
            FlaggedAt = DateTime.UtcNow
        };
        _db.FlaggedContents.Add(f2);
        _db.SaveChanges();

        var result = await _sut.GetFlagsAsync(null, entityType: "Profile", null, null, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].ContentType.Should().Be("Profile");
    }

    [Fact]
    public async Task GetFlagsAsync_FilterByStatus_ReturnsMatching()
    {
        SeedFlag(status: ModerationStatus.None);
        SeedFlag(status: ModerationStatus.Approve);

        var result = await _sut.GetFlagsAsync(status: "Approve", null, null, null, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFlagsAsync_SearchByQuery_FiltersOnReason()
    {
        var f1 = SeedFlag(); // FlagReason = "Spam"
        var f2 = new FlaggedContent
        {
            ContentType = "Message",
            ContentID = 2,
            FlagReason = "Harassment",
            ModerationStatus = ModerationStatus.None,
            FlaggedAt = DateTime.UtcNow
        };
        _db.FlaggedContents.Add(f2);
        _db.SaveChanges();

        var result = await _sut.GetFlagsAsync(null, null, null, q: "Spam", 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }

    // ── Boundary Tests ────────────────────────────────────────────

    [Fact]
    public async Task GetFlagsAsync_WithPageSize100_ReturnsAllItemsAtUpperBoundary()
    {
        // Boundary: pageSize=100 (upper boundary of standard paging API)
        for (int i = 0; i < 100; i++) SeedFlag();

        var result = await _sut.GetFlagsAsync(null, null, null, null, page: 1, pageSize: 100);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(100);
        result.Data.Paging.TotalItems.Should().Be(100);
    }
}
