using Xunit;
using FluentAssertions;
using AISEP.Domain.Entities;

namespace AISEP.Domain.UnitTests.Entities;

public class PasswordResetTokenTests
{
    [Fact]
    public void IsExpired_WhenExpiryDatePassed_ReturnsTrue()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // 10 min ago
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };

        // Act
        var result = token.IsExpired;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiryDateInFuture_ReturnsFalse()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour from now
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = token.IsExpired;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsUsed_WhenUsedAtNull_ReturnsFalse()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UsedAt = null
        };

        // Act
        var result = token.IsUsed;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsUsed_WhenUsedAtSet_ReturnsTrue()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UsedAt = DateTime.UtcNow
        };

        // Act
        var result = token.IsUsed;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenNotUsedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UsedAt = null
        };

        // Act
        var result = token.IsValid;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Already expired
            CreatedAt = DateTime.UtcNow.AddMinutes(-15),
            UsedAt = null
        };

        // Act
        var result = token.IsValid;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenAlreadyUsed_ReturnsFalse()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            UsedAt = DateTime.UtcNow.AddMinutes(-1) // Already used
        };

        // Act
        var result = token.IsValid;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenBothExpiredAndUsed_ReturnsFalse()
    {
        // Arrange
        var token = new PasswordResetToken
        {
            PasswordResetTokenID = 1,
            UserID = 1,
            Token = "reset-token-123",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-30), // Expired
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UsedAt = DateTime.UtcNow.AddMinutes(-5) // Also used
        };

        // Act
        var result = token.IsValid;

        // Assert
        result.Should().BeFalse();
    }
}
