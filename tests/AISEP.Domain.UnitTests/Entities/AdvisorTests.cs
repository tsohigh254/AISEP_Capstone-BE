using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using FluentAssertions;

namespace AISEP.Domain.UnitTests.Entities;

public class AdvisorTests
{
    [Fact]
    public void AdvisorConstruction_ProfileStatusDefaultDraft_ReturnsTrue()
    {
        // Arrange & Act
        var advisor = new Advisor();

        // Assert
        advisor.ProfileStatus.Should().Be(ProfileStatus.Draft);
    }

    [Fact]
    public void AdvisorConstruction_IsVerifiedDefaultFalse_ReturnsTrue()
    {
        // Arrange & Act
        var advisor = new Advisor();

        // Assert
        advisor.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void AdvisorConstruction_InitializesCollectionsNotNull_ReturnsTrue()
    {
        // Arrange & Act
        var advisor = new Advisor();

        // Assert
        advisor.IndustryFocus.Should().NotBeNull();
        advisor.Testimonials.Should().NotBeNull();
        advisor.Mentorships.Should().NotBeNull();
    }

    [Fact]
    public void AdvisorConstruction_NumericPropertiesDefaultToZero_ReturnsTrue()
    {
        // Arrange & Act
        var advisor = new Advisor();

        // Assert
        advisor.TotalMentees.Should().Be(0);
        advisor.TotalSessionHours.Should().Be(0);
        advisor.ReviewCount.Should().Be(0);
        advisor.CompletedSessions.Should().Be(0);
    }

    [Fact]
    public void Advisor_WithStringPropertiesSet_StoresCorrectly()
    {
        // Arrange
        var advisor = new Advisor
        {
            FullName = "Jane Smith",
            Title = "Business Consultant",
            Bio = "Experienced in startups",
            Expertise = "Tech,Finance,Marketing",
            DomainTags = "B2B,SaaS",
            SuitableFor = "Early-stage,Growth",
            SupportedDurations = "30min,1hour",
        };

        // Act & Assert
        advisor.FullName.Should().Be("Jane Smith");
        advisor.Title.Should().Be("Business Consultant");
        advisor.Bio.Should().Be("Experienced in startups");
        advisor.Expertise.Should().Be("Tech,Finance,Marketing");
        advisor.DomainTags.Should().Be("B2B,SaaS");
        advisor.SuitableFor.Should().Be("Early-stage,Growth");
        advisor.SupportedDurations.Should().Be("30min,1hour");
    }

    [Fact]
    public void Advisor_WithRatingsAndReviews_StoresCorrectly()
    {
        // Arrange
        var advisor = new Advisor();
        var averageRating = 4.8f;
        var reviewCount = 25;

        // Act
        advisor.AverageRating = averageRating;
        advisor.ReviewCount = reviewCount;

        // Assert
        advisor.AverageRating.Should().Be(averageRating);
        advisor.ReviewCount.Should().Be(reviewCount);
    }

    [Fact]
    public void Advisor_WithTimestamps_StoresCorrectly()
    {
        // Arrange
        var advisor = new Advisor();
        var createdTime = DateTime.UtcNow;
        var updatedTime = DateTime.UtcNow.AddHours(1);

        // Act
        advisor.CreatedAt = createdTime;
        advisor.UpdatedAt = updatedTime;

        // Assert
        advisor.CreatedAt.Should().Be(createdTime);
        advisor.UpdatedAt.Should().Be(updatedTime);
    }
}
