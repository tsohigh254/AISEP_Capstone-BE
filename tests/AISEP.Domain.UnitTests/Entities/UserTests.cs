using Xunit;
using FluentAssertions;
using AISEP.Domain.Entities;

namespace AISEP.Domain.UnitTests.Entities;

public class UserTests
{
    [Fact]
    public void UserConstruction_InitializesCollectionsNotNull()
    {
        // Arrange & Act
        var user = new User
        {
            UserID = 1,
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            UserType = "Startup",
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        user.UserRoles.Should().NotBeNull();
        user.UserRoles.Should().BeEmpty();
        user.RefreshTokens.Should().NotBeNull();
        user.RefreshTokens.Should().BeEmpty();
        user.PasswordResetTokens.Should().NotBeNull();
        user.PasswordResetTokens.Should().BeEmpty();
        user.AuditLogs.Should().NotBeNull();
        user.AuditLogs.Should().BeEmpty();
        user.Notifications.Should().NotBeNull();
        user.Notifications.Should().BeEmpty();
        user.SentMessages.Should().NotBeNull();
        user.SentMessages.Should().BeEmpty();
        user.EmailOtps.Should().NotBeNull();
        user.EmailOtps.Should().BeEmpty();
    }

    [Fact]
    public void UserConstruction_HasDefaultEmptyStrings()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Email.Should().Be(string.Empty);
        user.PasswordHash.Should().Be(string.Empty);
        user.UserType.Should().Be(string.Empty);
    }

    [Fact]
    public void UserConstruction_EmailVerifiedDefaultFalse()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public void UserConstruction_IsActiveDefaultFalse()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UserConstruction_NavigationPropertiesAllowNull()
    {
        // Arrange & Act
        var user = new User
        {
            UserID = 1,
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            UserType = "Startup",
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        user.Startup.Should().BeNull();
        user.Advisor.Should().BeNull();
        user.Investor.Should().BeNull();
    }

    [Fact]
    public void UserConstruction_OptionalTimestampsCanBeNull()
    {
        // Arrange & Act
        var user = new User
        {
            UserID = 1,
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            UserType = "Startup",
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            LastLoginAt = null
        };

        // Assert
        user.UpdatedAt.Should().BeNull();
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void UserConstruction_TimestampsSetCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddMinutes(5);
        var lastLoginAt = DateTime.UtcNow.AddMinutes(10);

        // Act
        var user = new User
        {
            UserID = 1,
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            UserType = "Startup",
            IsActive = true,
            EmailVerified = false,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastLoginAt = lastLoginAt
        };

        // Assert
        user.CreatedAt.Should().Be(createdAt);
        user.UpdatedAt.Should().Be(updatedAt);
        user.LastLoginAt.Should().Be(lastLoginAt);
    }
}
