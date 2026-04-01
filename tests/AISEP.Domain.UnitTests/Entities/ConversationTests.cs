using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using FluentAssertions;

namespace AISEP.Domain.UnitTests.Entities;

public class ConversationTests
{
    [Fact]
    public void ConversationConstruction_StatusDefaultActive_ReturnsTrue()
    {
        // Arrange & Act
        var conversation = new Conversation();

        // Assert
        conversation.ConversationStatus.Should().Be(ConversationStatus.Active);
    }

    [Fact]
    public void ConversationConstruction_InitializesMessagesNotNull_ReturnsTrue()
    {
        // Arrange & Act
        var conversation = new Conversation();

        // Assert
        conversation.Messages.Should().NotBeNull();
    }

    [Fact]
    public void ConversationConstruction_InitializesMessagesEmpty_ReturnsTrue()
    {
        // Arrange & Act
        var conversation = new Conversation();

        // Assert
        conversation.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Conversation_WithConnectionID_StoresCorrectly()
    {
        // Arrange
        var conversation = new Conversation();
        int connectionId = 5;

        // Act
        conversation.ConnectionID = connectionId;

        // Assert
        conversation.ConnectionID.Should().Be(connectionId);
    }

    [Fact]
    public void Conversation_WithMentorshipID_StoresCorrectly()
    {
        // Arrange
        var conversation = new Conversation();
        int mentorshipId = 12;

        // Act
        conversation.MentorshipID = mentorshipId;

        // Assert
        conversation.MentorshipID.Should().Be(mentorshipId);
    }

    [Fact]
    public void Conversation_WithCreatedAndLastMessageAt_StoresCorrectly()
    {
        // Arrange
        var conversation = new Conversation();
        var createdTime = DateTime.UtcNow;
        var lastMessageTime = DateTime.UtcNow.AddHours(2);

        // Act
        conversation.CreatedAt = createdTime;
        conversation.LastMessageAt = lastMessageTime;

        // Assert
        conversation.CreatedAt.Should().Be(createdTime);
        conversation.LastMessageAt.Should().Be(lastMessageTime);
    }

    [Fact]
    public void Conversation_WithStatusChange_StoresCorrectly()
    {
        // Arrange
        var conversation = new Conversation();

        // Act
        conversation.ConversationStatus = ConversationStatus.Closed;

        // Assert
        conversation.ConversationStatus.Should().Be(ConversationStatus.Closed);
    }
}
