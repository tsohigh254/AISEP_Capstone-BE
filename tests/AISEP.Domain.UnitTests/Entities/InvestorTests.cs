using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using FluentAssertions;

namespace AISEP.Domain.UnitTests.Entities;

public class InvestorTests
{
    [Fact]
    public void InvestorConstruction_ProfileStatusDefaultDraft_ReturnsTrue()
    {
        // Arrange & Act
        var investor = new Investor();

        // Assert
        investor.ProfileStatus.Should().Be(ProfileStatus.Draft);
    }

    [Fact]
    public void InvestorConstruction_InitializesCollectionsNotNull_ReturnsTrue()
    {
        // Arrange & Act
        var investor = new Investor();

        // Assert
        investor.Watchlists.Should().NotBeNull();
        investor.IndustryFocus.Should().NotBeNull();
        investor.StageFocus.Should().NotBeNull();
        investor.PortfolioCompanies.Should().NotBeNull();
        investor.StartupConnections.Should().NotBeNull();
        investor.InformationRequests.Should().NotBeNull();
    }

    [Fact]
    public void InvestorConstruction_InitializesCollectionsEmpty_ReturnsTrue()
    {
        // Arrange & Act
        var investor = new Investor();

        // Assert
        investor.Watchlists.Should().BeEmpty();
        investor.IndustryFocus.Should().BeEmpty();
        investor.StageFocus.Should().BeEmpty();
        investor.PortfolioCompanies.Should().BeEmpty();
        investor.StartupConnections.Should().BeEmpty();
        investor.InformationRequests.Should().BeEmpty();
    }

    [Fact]
    public void Investor_WithProfileProperties_StoresCorrectly()
    {
        // Arrange
        var investor = new Investor
        {
            FullName = "Michael Johnson",
            FirmName = "Tech Ventures Fund",
            Title = "Managing Partner",
            Bio = "10+ years in venture capital",
            Location = "San Francisco",
            Country = "USA",
        };

        // Act & Assert
        investor.FullName.Should().Be("Michael Johnson");
        investor.FirmName.Should().Be("Tech Ventures Fund");
        investor.Title.Should().Be("Managing Partner");
        investor.Bio.Should().Be("10+ years in venture capital");
        investor.Location.Should().Be("San Francisco");
        investor.Country.Should().Be("USA");
    }

    [Fact]
    public void Investor_WithWebAndLinkedInURLs_StoresCorrectly()
    {
        // Arrange
        var investor = new Investor();
        var linkedInURL = "https://linkedin.com/in/mjohnson";
        var website = "https://techventuresfund.com";

        // Act
        investor.LinkedInURL = linkedInURL;
        investor.Website = website;

        // Assert
        investor.LinkedInURL.Should().Be(linkedInURL);
        investor.Website.Should().Be(website);
    }

    [Fact]
    public void Investor_WithTimestamps_StoresCorrectly()
    {
        // Arrange
        var investor = new Investor();
        var createdTime = DateTime.UtcNow;
        var updatedTime = DateTime.UtcNow.AddDays(1);

        // Act
        investor.CreatedAt = createdTime;
        investor.UpdatedAt = updatedTime;

        // Assert
        investor.CreatedAt.Should().Be(createdTime);
        investor.UpdatedAt.Should().Be(updatedTime);
    }
}
