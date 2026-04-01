using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using FluentAssertions;

namespace AISEP.Domain.UnitTests.Entities;

public class StartupInvestorConnectionTests
{
    [Fact]
    public void ConnectionConstruction_StatusDefaultRequested_ReturnsTrue()
    {
        // Arrange & Act
        var connection = new StartupInvestorConnection();

        // Assert
        connection.ConnectionStatus.Should().Be(ConnectionStatus.Requested);
    }

    [Fact]
    public void ConnectionConstruction_InitializesCollectionsNotNull_ReturnsTrue()
    {
        // Arrange & Act
        var connection = new StartupInvestorConnection();

        // Assert
        connection.InformationRequests.Should().NotBeNull();
        connection.Conversations.Should().NotBeNull();
    }

    [Fact]
    public void ConnectionConstruction_InitializesCollectionsEmpty_ReturnsTrue()
    {
        // Arrange & Act
        var connection = new StartupInvestorConnection();

        // Assert
        connection.InformationRequests.Should().BeEmpty();
        connection.Conversations.Should().BeEmpty();
    }

    [Fact]
    public void Connection_WithMatchScore_StoresCorrectly()
    {
        // Arrange
        var connection = new StartupInvestorConnection();
        var matchScore = 87.5f;

        // Act
        connection.MatchScore = matchScore;

        // Assert
        connection.MatchScore.Should().Be(matchScore);
    }

    [Fact]
    public void Connection_WithPersonalizedMessage_StoresCorrectly()
    {
        // Arrange
        var connection = new StartupInvestorConnection();
        var message = "Interested in your innovative solution";

        // Act
        connection.PersonalizedMessage = message;

        // Assert
        connection.PersonalizedMessage.Should().Be(message);
    }

    [Fact]
    public void Connection_WithTimestamps_StoresCorrectly()
    {
        // Arrange
        var connection = new StartupInvestorConnection();
        var requestedTime = DateTime.UtcNow;
        var respondedTime = DateTime.UtcNow.AddDays(2);

        // Act
        connection.RequestedAt = requestedTime;
        connection.RespondedAt = respondedTime;

        // Assert
        connection.RequestedAt.Should().Be(requestedTime);
        connection.RespondedAt.Should().Be(respondedTime);
    }

    [Fact]
    public void Connection_WithStatusAndInitiatedBy_StoresCorrectly()
    {
        // Arrange
        var connection = new StartupInvestorConnection();

        // Act
        connection.ConnectionStatus = ConnectionStatus.Accepted;
        connection.InitiatedBy = 10;

        // Assert
        connection.ConnectionStatus.Should().Be(ConnectionStatus.Accepted);
        connection.InitiatedBy.Should().Be(10);
    }
}
