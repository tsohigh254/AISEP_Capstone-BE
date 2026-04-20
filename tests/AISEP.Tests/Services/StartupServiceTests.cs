using AISEP.Application.DTOs.Startup;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AISEP.Tests.Services;

public class StartupServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<ICloudinaryService> _cloud = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly StartupService _sut;

    public StartupServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<StartupService>>();
        _sut = new StartupService(_db, _audit.Object, logger.Object, _cloud.Object, _scopeFactory.Object);
    }

    private Startup SeedStartup(int userId, ProfileStatus status = ProfileStatus.Approved, bool visible = false)
    {
        var s = new Startup
        {
            UserID = userId,
            CompanyName = "Acme",
            OneLiner = "We build things",
            ProfileStatus = status,
            IsVisible = visible,
            CreatedAt = DateTime.UtcNow
        };
        _db.Startups.Add(s);
        _db.SaveChanges();
        return s;
    }

    [Fact]
    public async Task GetMyStartupAsync_WhenNotCreated_ReturnsSuccessWithNullData()
    {
        var result = await _sut.GetMyStartupAsync(userId: 1);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("not been created");
    }

    [Fact]
    public async Task GetMyStartupAsync_WhenExists_ReturnsStartup()
    {
        SeedStartup(userId: 1);

        var result = await _sut.GetMyStartupAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task ToggleVisibilityAsync_WhenNoProfile_ReturnsNotFound()
    {
        var result = await _sut.ToggleVisibilityAsync(userId: 1, isVisible: true);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task ToggleVisibilityAsync_WhenStatusNotApproved_RejectsMakingVisible()
    {
        SeedStartup(userId: 1, status: ProfileStatus.PendingKYC);

        var result = await _sut.ToggleVisibilityAsync(1, isVisible: true);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_VISIBILITY_NOT_ALLOWED");
    }

    [Fact]
    public async Task ToggleVisibilityAsync_WhenApproved_TogglesSuccessfully()
    {
        var s = SeedStartup(userId: 1, status: ProfileStatus.Approved, visible: false);

        var result = await _sut.ToggleVisibilityAsync(1, isVisible: true);

        result.Success.Should().BeTrue();
        (await _db.Startups.FindAsync(s.StartupID))!.IsVisible.Should().BeTrue();
        _audit.Verify(a => a.LogAsync("TOGGLE_VISIBILITY", "Startup", s.StartupID, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetTeamMembersAsync_WhenNoProfile_ReturnsError()
    {
        var result = await _sut.GetTeamMembersAsync(userId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task GetTeamMembersAsync_ReturnsOnlyMembersOfCurrentStartup()
    {
        var a = SeedStartup(userId: 1);
        var b = SeedStartup(userId: 2);
        _db.TeamMembers.AddRange(
            new TeamMember { StartupID = a.StartupID, FullName = "Alice", CreatedAt = DateTime.UtcNow },
            new TeamMember { StartupID = a.StartupID, FullName = "Bob", CreatedAt = DateTime.UtcNow.AddMinutes(1) },
            new TeamMember { StartupID = b.StartupID, FullName = "Carol", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.GetTeamMembersAsync(userId: 1);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(m => m.FullName).Should().BeEquivalentTo(new[] { "Alice", "Bob" });
    }

    [Fact]
    public async Task AddTeamMemberAsync_WithoutProfile_ReturnsNotFound()
    {
        var result = await _sut.AddTeamMemberAsync(1, new CreateTeamMemberRequest { FullName = "x" });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task AddTeamMemberAsync_PersistsMemberAndLogsAudit()
    {
        var s = SeedStartup(userId: 1);

        var result = await _sut.AddTeamMemberAsync(1, new CreateTeamMemberRequest
        {
            FullName = "Alice",
            Role = "CEO",
            IsFounder = true
        });

        result.Success.Should().BeTrue();
        result.Data!.FullName.Should().Be("Alice");
        _db.TeamMembers.Should().ContainSingle(tm => tm.StartupID == s.StartupID && tm.FullName == "Alice");
        _audit.Verify(a => a.LogAsync("CREATE_TEAM_MEMBER", "TeamMember", It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTeamMemberAsync_WhenMemberBelongsToAnotherStartup_ReturnsNotFound()
    {
        var mine = SeedStartup(userId: 1);
        var other = SeedStartup(userId: 2);
        var foreignMember = new TeamMember { StartupID = other.StartupID, FullName = "Eve", CreatedAt = DateTime.UtcNow };
        _db.TeamMembers.Add(foreignMember);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteTeamMemberAsync(userId: 1, teamMemberId: foreignMember.TeamMemberID);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("TEAM_MEMBER_NOT_FOUND");
        _db.TeamMembers.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteTeamMemberAsync_WhenOwnMember_RemovesAndLogs()
    {
        var s = SeedStartup(userId: 1);
        var m = new TeamMember { StartupID = s.StartupID, FullName = "Alice", CreatedAt = DateTime.UtcNow };
        _db.TeamMembers.Add(m);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteTeamMemberAsync(1, m.TeamMemberID);

        result.Success.Should().BeTrue();
        _db.TeamMembers.Should().BeEmpty();
        _audit.Verify(a => a.LogAsync("DELETE_TEAM_MEMBER", "TeamMember", m.TeamMemberID, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateStartupAsync_WhenAlreadyExists_ReturnsError()
    {
        SeedStartup(userId: 1);

        var result = await _sut.CreateStartupAsync(1, new CreateStartupRequest
        {
            CompanyName = "New",
            OneLiner = "new co"
        });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_EXISTS");
    }

    [Fact]
    public async Task CreateStartupAsync_WithInvalidIndustry_ReturnsError()
    {
        var result = await _sut.CreateStartupAsync(1, new CreateStartupRequest
        {
            CompanyName = "New",
            OneLiner = "new co",
            IndustryID = 9999
        });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("INVALID_INDUSTRY");
    }

    [Fact]
    public async Task CreateStartupAsync_WhenValid_PersistsAndLogsAudit()
    {
        var result = await _sut.CreateStartupAsync(1, new CreateStartupRequest
        {
            CompanyName = "Acme",
            OneLiner = "We do stuff"
        });

        result.Success.Should().BeTrue();
        result.Data!.CompanyName.Should().Be("Acme");
        _db.Startups.Should().ContainSingle(s => s.UserID == 1 && s.CompanyName == "Acme");
        _audit.Verify(a => a.LogAsync("CREATE_STARTUP", "Startup", It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStartupAsync_WhenNoProfile_ReturnsNotFound()
    {
        var result = await _sut.UpdateStartupAsync(1, new UpdateStartupRequest { CompanyName = "X" });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateStartupAsync_WithInvalidIndustry_ReturnsError()
    {
        SeedStartup(userId: 1);

        var result = await _sut.UpdateStartupAsync(1, new UpdateStartupRequest { IndustryID = 9999 });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("INVALID_INDUSTRY");
    }

    [Fact]
    public async Task UpdateStartupAsync_WithPartialFields_UpdatesOnlyProvided()
    {
        var s = SeedStartup(userId: 1);
        s.Description = "old desc";
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateStartupAsync(1, new UpdateStartupRequest
        {
            CompanyName = "Renamed"
        });

        result.Success.Should().BeTrue();
        var reloaded = (await _db.Startups.FindAsync(s.StartupID))!;
        reloaded.CompanyName.Should().Be("Renamed");
        reloaded.Description.Should().Be("old desc");
    }

    [Fact]
    public async Task SubmitForApprovalAsync_WhenNoProfile_ReturnsNotFound()
    {
        var result = await _sut.SubmitForApprovalAsync(1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task SubmitForApprovalAsync_WhenAlreadyPending_ReturnsError()
    {
        SeedStartup(userId: 1, status: ProfileStatus.Pending);

        var result = await _sut.SubmitForApprovalAsync(1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("ALREADY_PENDING");
    }

    [Fact]
    public async Task SubmitForApprovalAsync_WhenApproved_MovesToPendingKYC()
    {
        var s = SeedStartup(userId: 1, status: ProfileStatus.Approved);

        var result = await _sut.SubmitForApprovalAsync(1);

        result.Success.Should().BeTrue();
        (await _db.Startups.FindAsync(s.StartupID))!.ProfileStatus.Should().Be(ProfileStatus.PendingKYC);
    }

    [Fact]
    public async Task GetKYCStatusAsync_WhenNoProfile_ReturnsError()
    {
        var result = await _sut.GetKYCStatusAsync(1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task GetKYCStatusAsync_WhenNoSubmission_ReturnsNotSubmitted()
    {
        SeedStartup(userId: 1);

        var result = await _sut.GetKYCStatusAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Explanation.Should().Contain("not been submitted");
    }

    [Fact]
    public async Task UpdateTeamMemberAsync_WhenNoProfile_ReturnsNotFound()
    {
        var result = await _sut.UpdateTeamMemberAsync(1, 5, new UpdateTeamMemberRequest { FullName = "x" });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateTeamMemberAsync_WhenNotOwned_ReturnsNotFound()
    {
        SeedStartup(userId: 1);
        var other = SeedStartup(userId: 2);
        var foreign = new TeamMember { StartupID = other.StartupID, FullName = "Eve", CreatedAt = DateTime.UtcNow };
        _db.TeamMembers.Add(foreign);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateTeamMemberAsync(1, foreign.TeamMemberID, new UpdateTeamMemberRequest { FullName = "Hacked" });

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("TEAM_MEMBER_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateTeamMemberAsync_WithPartialUpdate_ChangesOnlyProvided()
    {
        var s = SeedStartup(userId: 1);
        var m = new TeamMember { StartupID = s.StartupID, FullName = "Alice", Role = "CTO", CreatedAt = DateTime.UtcNow };
        _db.TeamMembers.Add(m);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateTeamMemberAsync(1, m.TeamMemberID, new UpdateTeamMemberRequest { Role = "CEO" });

        result.Success.Should().BeTrue();
        var reloaded = (await _db.TeamMembers.FindAsync(m.TeamMemberID))!;
        reloaded.Role.Should().Be("CEO");
        reloaded.FullName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetStartupByIdAsync_WhenNotApproved_ReturnsNotFound()
    {
        var s = SeedStartup(userId: 2, status: ProfileStatus.PendingKYC);

        var result = await _sut.GetStartupByIdAsync(s.StartupID, requestingUserId: 1, userType: "Startup");

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_NOT_FOUND");
    }

    [Fact]
    public async Task GetStartupByIdAsync_WhenNotVisibleAndNotStaff_ReturnsNotFound()
    {
        var s = SeedStartup(userId: 2, status: ProfileStatus.Approved, visible: false);

        var result = await _sut.GetStartupByIdAsync(s.StartupID, requestingUserId: 1, userType: "Startup");

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("STARTUP_NOT_FOUND");
    }

    [Fact]
    public async Task GetStartupByIdAsync_WhenVisibleAndApproved_ReturnsDto()
    {
        var s = SeedStartup(userId: 2, status: ProfileStatus.Approved, visible: true);

        var result = await _sut.GetStartupByIdAsync(s.StartupID, requestingUserId: 1, userType: "Startup");

        result.Success.Should().BeTrue();
        result.Data!.CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task GetStartupByIdAsync_WhenNotVisibleButStaff_ReturnsDto()
    {
        var s = SeedStartup(userId: 2, status: ProfileStatus.Approved, visible: false);

        var result = await _sut.GetStartupByIdAsync(s.StartupID, requestingUserId: 1, userType: "Staff");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SearchStartupsAsync_ReturnsOnlyApprovedAndVisible_ForNonStaff()
    {
        SeedStartup(userId: 1, status: ProfileStatus.Approved, visible: true);
        SeedStartup(userId: 2, status: ProfileStatus.Approved, visible: false);
        SeedStartup(userId: 3, status: ProfileStatus.PendingKYC, visible: true);

        var result = await _sut.SearchStartupsAsync(
            new StartupQueryParams { Page = 1, PageSize = 10 },
            userType: "Investor");

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Paging.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task SearchStartupsAsync_AsStaff_SeesInvisibleApproved()
    {
        SeedStartup(userId: 1, status: ProfileStatus.Approved, visible: true);
        SeedStartup(userId: 2, status: ProfileStatus.Approved, visible: false);

        var result = await _sut.SearchStartupsAsync(
            new StartupQueryParams { Page = 1, PageSize = 10 },
            userType: "Staff");

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
    }

    // ── Boundary Tests ────────────────────────────────────────────

    [Fact]
    public async Task CreateStartupAsync_WithMaxLength100CharCompanyName_SucceedsAtUpperBoundary()
    {
        // Boundary: CompanyName at max length 100 chars
        var longName = new string('A', 100);

        var result = await _sut.CreateStartupAsync(1, new CreateStartupRequest
        {
            CompanyName = longName,
            OneLiner = "boundary test"
        });

        result.Success.Should().BeTrue();
        result.Data!.CompanyName.Should().HaveLength(100);
        _db.Startups.Should().ContainSingle(s => s.CompanyName == longName);
    }

    [Fact]
    public async Task AddTeamMemberAsync_AddingFirstMember_SucceedsAtLowerBoundary()
    {
        // Boundary: team size grows from 0 to 1 (minimum non-empty team)
        var s = SeedStartup(userId: 1);
        _db.TeamMembers.Where(t => t.StartupID == s.StartupID).Should().BeEmpty();

        var result = await _sut.AddTeamMemberAsync(1, new CreateTeamMemberRequest
        {
            FullName = "Founder",
            Role = "CEO",
            IsFounder = true
        });

        result.Success.Should().BeTrue();
        _db.TeamMembers.Count(t => t.StartupID == s.StartupID).Should().Be(1);
    }

    [Fact]
    public async Task SearchStartupsAsync_WithPageSize1_ReturnsSingleItemAtLowerBoundary()
    {
        // Boundary: pageSize=1 (minimum valid page size)
        SeedStartup(userId: 1, status: ProfileStatus.Approved, visible: true);
        SeedStartup(userId: 2, status: ProfileStatus.Approved, visible: true);
        SeedStartup(userId: 3, status: ProfileStatus.Approved, visible: true);

        var result = await _sut.SearchStartupsAsync(
            new StartupQueryParams { Page = 1, PageSize = 1 },
            userType: "Investor");

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Paging.TotalItems.Should().Be(3);
    }
}
