using AISEP.Domain.Entities;
using FluentAssertions;

namespace AISEP.Domain.UnitTests.Entities;

public class MentorshipSessionTests
{
    [Fact]
    public void SessionConstruction_InitializesCollectionsNotNull_ReturnsTrue()
    {
        // Arrange & Act
        var session = new MentorshipSession();

        // Assert
        session.Reports.Should().NotBeNull();
        session.Feedbacks.Should().NotBeNull();
    }

    [Fact]
    public void SessionConstruction_InitializesCollectionsEmpty_ReturnsTrue()
    {
        // Arrange & Act
        var session = new MentorshipSession();

        // Assert
        session.Reports.Should().BeEmpty();
        session.Feedbacks.Should().BeEmpty();
    }

    [Fact]
    public void Session_WithScheduleDetails_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();
        var scheduledTime = DateTime.UtcNow.AddDays(7);
        int duration = 60;
        var format = "Video Call";

        // Act
        session.ScheduledStartAt = scheduledTime;
        session.DurationMinutes = duration;
        session.SessionFormat = format;

        // Assert
        session.ScheduledStartAt.Should().Be(scheduledTime);
        session.DurationMinutes.Should().Be(duration);
        session.SessionFormat.Should().Be(format);
    }

    [Fact]
    public void Session_WithMeetingURL_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();
        var meetingUrl = "https://meet.example.com/session123";

        // Act
        session.MeetingURL = meetingUrl;

        // Assert
        session.MeetingURL.Should().Be(meetingUrl);
    }

    [Fact]
    public void Session_WithSessionStatus_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();

        // Act
        session.SessionStatus = "Completed";

        // Assert
        session.SessionStatus.Should().Be("Completed");
    }

    [Fact]
    public void Session_WithConfirmationTimestamps_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();
        var advisorConfirmedTime = DateTime.UtcNow.AddDays(1);
        var startupConfirmedTime = DateTime.UtcNow.AddDays(2);

        // Act
        session.AdvisorConfirmedConductedAt = advisorConfirmedTime;
        session.StartupConfirmedConductedAt = startupConfirmedTime;

        // Assert
        session.AdvisorConfirmedConductedAt.Should().Be(advisorConfirmedTime);
        session.StartupConfirmedConductedAt.Should().Be(startupConfirmedTime);
    }

    [Fact]
    public void Session_WithSessionContent_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();
        var topics = "Business Strategy, Marketing";
        var insights = "Focus on product-market fit";
        var actions = "Develop GTM strategy";

        // Act
        session.TopicsDiscussed = topics;
        session.KeyInsights = insights;
        session.ActionItems = actions;

        // Assert
        session.TopicsDiscussed.Should().Be(topics);
        session.KeyInsights.Should().Be(insights);
        session.ActionItems.Should().Be(actions);
    }

    [Fact]
    public void Session_WithFollowUp_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();
        var nextSteps = "Schedule follow-up in 2 weeks";
        var resources = "Recommended reading on growth hacking";

        // Act
        session.NextSteps = nextSteps;
        session.RecommendedResources = resources;

        // Assert
        session.NextSteps.Should().Be(nextSteps);
        session.RecommendedResources.Should().Be(resources);
    }

    [Fact]
    public void Session_WithNotes_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();
        var advisorNotes = "Founder is very engaged";
        var startupNotes = "Advisor provided valuable insights";

        // Act
        session.AdvisorInternalNotes = advisorNotes;
        session.StartupNotes = startupNotes;

        // Assert
        session.AdvisorInternalNotes.Should().Be(advisorNotes);
        session.StartupNotes.Should().Be(startupNotes);
    }

    [Fact]
    public void Session_WithTimestamps_StoresCorrectly()
    {
        // Arrange
        var session = new MentorshipSession();
        var createdTime = DateTime.UtcNow;
        var updatedTime = DateTime.UtcNow.AddDays(1);

        // Act
        session.CreatedAt = createdTime;
        session.UpdatedAt = updatedTime;

        // Assert
        session.CreatedAt.Should().Be(createdTime);
        session.UpdatedAt.Should().Be(updatedTime);
    }
}
