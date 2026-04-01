using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using FluentAssertions;

namespace AISEP.Domain.UnitTests.Entities;

public class StartupTests
{
    [Fact]
    public void StartupConstruction_ProfileStatusDefaultDraft_ReturnsTrue()
    {
        // Arrange & Act
        var startup = new Startup();

        // Assert
        startup.ProfileStatus.Should().Be(ProfileStatus.Draft);
    }

    [Fact]
    public void StartupConstruction_InitializesCollectionsNotNull_ReturnsTrue()
    {
        // Arrange & Act
        var startup = new Startup();

        // Assert
        startup.TeamMembers.Should().NotBeNull();
        startup.Documents.Should().NotBeNull();
        startup.PotentialScores.Should().NotBeNull();
        startup.Mentorships.Should().NotBeNull();
        startup.InvestorConnections.Should().NotBeNull();
        startup.WatchedByInvestors.Should().NotBeNull();
        startup.AdvisorTestimonials.Should().NotBeNull();
    }

    [Fact]
    public void StartupConstruction_InitializesCollectionsEmpty_ReturnsTrue()
    {
        // Arrange & Act
        var startup = new Startup();

        // Assert
        startup.TeamMembers.Should().BeEmpty();
        startup.Documents.Should().BeEmpty();
        startup.PotentialScores.Should().BeEmpty();
        startup.Mentorships.Should().BeEmpty();
        startup.InvestorConnections.Should().BeEmpty();
        startup.WatchedByInvestors.Should().BeEmpty();
        startup.AdvisorTestimonials.Should().BeEmpty();
    }

    [Fact]
    public void StartupConstruction_StringPropertiesDefaultToEmpty_ReturnsTrue()
    {
        // Arrange & Act
        var startup = new Startup();

        // Assert
        startup.CompanyName.Should().Be(string.Empty);
        startup.OneLiner.Should().Be(string.Empty);
        startup.FullNameOfApplicant.Should().Be(string.Empty);
        startup.RoleOfApplicant.Should().Be(string.Empty);
        startup.ContactEmail.Should().Be(string.Empty);
    }

    [Fact]
    public void StartupConstruction_NullablePropertiesAreNull_ReturnsTrue()
    {
        // Arrange & Act
        var startup = new Startup();

        // Assert
        startup.Description.Should().BeNull();
        startup.IndustryID.Should().BeNull();
        startup.Stage.Should().BeNull();
        startup.FoundedDate.Should().BeNull();
        startup.Website.Should().BeNull();
        startup.LogoURL.Should().BeNull();
        startup.FundingAmountSought.Should().BeNull();
        startup.CurrentFundingRaised.Should().BeNull();
        startup.Valuation.Should().BeNull();
        startup.ContactPhone.Should().BeNull();
        startup.FileCertificateBusiness.Should().BeNull();
        startup.LinkedInURL.Should().BeNull();
    }

    [Fact]
    public void StartupConstruction_DecimalPropertiesCanBeSet_ReturnsTrue()
    {
        // Arrange
        var startup = new Startup();
        var fundingAmount = 100000m;
        var currentRaised = 50000m;
        var valuation = 500000m;

        // Act
        startup.FundingAmountSought = fundingAmount;
        startup.CurrentFundingRaised = currentRaised;
        startup.Valuation = valuation;

        // Assert
        startup.FundingAmountSought.Should().Be(fundingAmount);
        startup.CurrentFundingRaised.Should().Be(currentRaised);
        startup.Valuation.Should().Be(valuation);
    }

    [Fact]
    public void StartupConstruction_SetPropertiesCorrectly_ReturnsTrue()
    {
        // Arrange
        var startup = new Startup
        {
            CompanyName = "TechStartup Inc",
            OneLiner = "Revolutionizing tech",
            FullNameOfApplicant = "John Doe",
            RoleOfApplicant = "Founder",
            ContactEmail = "john@techstartup.com",
            BusinessCode = "TC-2024-001",
            Stage = StartupStage.Idea,
            FoundedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsVisible = true,
        };

        // Act & Assert
        startup.CompanyName.Should().Be("TechStartup Inc");
        startup.OneLiner.Should().Be("Revolutionizing tech");
        startup.FullNameOfApplicant.Should().Be("John Doe");
        startup.RoleOfApplicant.Should().Be("Founder");
        startup.ContactEmail.Should().Be("john@techstartup.com");
        startup.BusinessCode.Should().Be("TC-2024-001");
        startup.Stage.Should().Be(StartupStage.Idea);
        startup.FoundedDate.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        startup.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void Startup_WithApprovalWorkflow_StoresCorrectly()
    {
        // Arrange
        var startup = new Startup();
        var approvalTime = DateTime.UtcNow;

        // Act
        startup.ProfileStatus = ProfileStatus.Approved;
        startup.ApprovedAt = approvalTime;
        startup.ApprovedBy = 5;

        // Assert
        startup.ProfileStatus.Should().Be(ProfileStatus.Approved);
        startup.ApprovedAt.Should().Be(approvalTime);
        startup.ApprovedBy.Should().Be(5);
    }

    [Fact]
    public void Startup_WithCreatedAndUpdatedTimestamps_StoresCorrectly()
    {
        // Arrange
        var startup = new Startup();
        var createdTime = DateTime.UtcNow;
        var updatedTime = DateTime.UtcNow.AddHours(1);

        // Act
        startup.CreatedAt = createdTime;
        startup.UpdatedAt = updatedTime;

        // Assert
        startup.CreatedAt.Should().Be(createdTime);
        startup.UpdatedAt.Should().Be(updatedTime);
    }
}
