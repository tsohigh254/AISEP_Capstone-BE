using Xunit;
using FluentAssertions;
using AISEP.Domain.Entities;

namespace AISEP.Domain.UnitTests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void IsExpired_WhenExpiryDatePassed_ReturnsTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // 5 min ago
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
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
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30), // 30 min from now
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = token.IsExpired;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenRevokedAtNull_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        // Act
        var result = token.IsRevoked;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenRevokedAtSet_ReturnsTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow // Set to current time
        };

        // Act
        var result = token.IsRevoked;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        // Act
        var result = token.IsActive;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Already expired
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            RevokedAt = null
        };

        // Act
        var result = token.IsActive;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow // Revoked
        };

        // Act
        var result = token.IsActive;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_WithIPAddressAndUserAgent_StoresCorrectly()
    {
        // Arrange
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        var token = new RefreshToken
        {
            RefreshTokenID = 1,
            UserID = 1,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
            IPAddress = ipAddress,
            UserAgent = userAgent
        };

        // Assert
        token.IPAddress.Should().Be(ipAddress);
        token.UserAgent.Should().Be(userAgent);
    }
}
