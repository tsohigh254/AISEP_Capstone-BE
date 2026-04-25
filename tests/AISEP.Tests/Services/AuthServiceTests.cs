using AISEP.Application.Configuration;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Infrastructure.Settings;
using AISEP.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AISEP.Tests.Services;

public class AuthServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _db = TestDbContextFactory.Create();

        var jwt = Options.Create(new JwtSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SecretKey = "super-secret-test-signing-key-minimum-32-chars-long!",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });
        var emailSettings = Options.Create(new EmailSettings
        {
            ResendApiKey = "test",
            FromEmail = "test@aisep.local",
            FromName = "AISEP Test"
        });

        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<AuthService>>();
        var notificationService = new Mock<INotificationDeliveryService>();
        var serviceScopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();

        _sut = new AuthService(_db, jwt, emailService.Object, emailSettings, logger.Object, notificationService.Object, serviceScopeFactory.Object);
    }

    private User SeedUser(string email, string password, bool active = true, bool verified = true)
    {
        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            UserType = "Startup",
            IsActive = active,
            EmailVerified = verified,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private static HttpContext CreateHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        SeedUser("user@test.com", "Password123!");

        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "user@test.com", Password = "Password123!" },
            CreateHttpContext());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        SeedUser("user@test.com", "Password123!");

        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "user@test.com", Password = "WrongPass" },
            CreateHttpContext());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsFailure()
    {
        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "nobody@test.com", Password = "whatever" },
            CreateHttpContext());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WhenAccountDeactivated_ReturnsFailure()
    {
        SeedUser("user@test.com", "Password123!", active: false);

        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "user@test.com", Password = "Password123!" },
            CreateHttpContext());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Account is deactivated");
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotVerified_ReturnsFailure()
    {
        SeedUser("user@test.com", "Password123!", verified: false);

        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "user@test.com", Password = "Password123!" },
            CreateHttpContext());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Please verify your email before logging in");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_UpdatesHashAndReturnsTrue()
    {
        var user = SeedUser("user@test.com", "OldPass123!");

        var ok = await _sut.ChangePasswordAsync(user.UserID, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456!",
            ConfirmNewPassword = "NewPass456!"
        });

        ok.Should().BeTrue();
        var reloaded = await _db.Users.FindAsync(user.UserID);
        BCrypt.Net.BCrypt.Verify("NewPass456!", reloaded!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ReturnsFalse()
    {
        var user = SeedUser("user@test.com", "OldPass123!");

        var ok = await _sut.ChangePasswordAsync(user.UserID, new ChangePasswordRequest
        {
            CurrentPassword = "WrongOld",
            NewPassword = "NewPass456!",
            ConfirmNewPassword = "NewPass456!"
        });

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenUserNotFound_ReturnsFalse()
    {
        var ok = await _sut.ChangePasswordAsync(9999, new ChangePasswordRequest
        {
            CurrentPassword = "x",
            NewPassword = "y",
            ConfirmNewPassword = "y"
        });

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task AdminResetPasswordAsync_WhenUserExists_ResetsAndRevokesTokens()
    {
        var user = SeedUser("user@test.com", "OldPass123!");
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserID = user.UserID,
            Token = "tok-1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var ok = await _sut.AdminResetPasswordAsync(user.UserID, "BrandNew123!");

        ok.Should().BeTrue();
        var reloaded = await _db.Users.FindAsync(user.UserID);
        BCrypt.Net.BCrypt.Verify("BrandNew123!", reloaded!.PasswordHash).Should().BeTrue();
        _db.RefreshTokens.Single().RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminResetPasswordAsync_WhenUserMissing_ReturnsFalse()
    {
        var ok = await _sut.AdminResetPasswordAsync(42, "any");
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllTokensAsync_MarksAllActiveTokensRevoked()
    {
        var user = SeedUser("user@test.com", "p");
        _db.RefreshTokens.AddRange(
            new RefreshToken { UserID = user.UserID, Token = "a", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow },
            new RefreshToken { UserID = user.UserID, Token = "b", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var ok = await _sut.RevokeAllTokensAsync(user.UserID);

        ok.Should().BeTrue();
        _db.RefreshTokens.Should().OnlyContain(t => t.RevokedAt != null);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithUnknownEmail_StillReturnsSuccessMessage()
    {
        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "ghost@test.com" });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("If your email is registered");
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithKnownEmail_CreatesResetTokenAndOtp()
    {
        var user = SeedUser("user@test.com", "p");

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "user@test.com" });

        result.Success.Should().BeTrue();
        _db.PasswordResetTokens.Should().ContainSingle(t => t.UserID == user.UserID && t.UsedAt == null);
        _db.EmailOtps.Should().ContainSingle(o => o.UserId == user.UserID);
    }

    [Fact]
    public async Task LogoutAsync_WhenNoCookie_ReturnsFalse()
    {
        var ok = await _sut.LogoutAsync(CreateHttpContext());
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_WhenValidToken_RevokesAndReturnsTrue()
    {
        var user = SeedUser("user@test.com", "p");
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserID = user.UserID,
            Token = "cookie-tok",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var ctx = CreateHttpContext();
        ctx.Request.Headers["Cookie"] = "refreshToken=cookie-tok";

        var ok = await _sut.LogoutAsync(ctx);

        ok.Should().BeTrue();
        _db.RefreshTokens.Single().RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenNoCookie_ReturnsFailure()
    {
        var result = await _sut.RefreshTokenAsync(CreateHttpContext());

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenNotFound_ReturnsFailure()
    {
        var ctx = CreateHttpContext();
        ctx.Request.Headers["Cookie"] = "refreshToken=missing";

        var result = await _sut.RefreshTokenAsync(ctx);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenRevoked_ReturnsFailure()
    {
        var user = SeedUser("user@test.com", "p");
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserID = user.UserID,
            Token = "revoked-tok",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var ctx = CreateHttpContext();
        ctx.Request.Headers["Cookie"] = "refreshToken=revoked-tok";

        var result = await _sut.RefreshTokenAsync(ctx);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Refresh token is expired or revoked");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenEmailNotVerified_RevokesAndReturnsFailure()
    {
        var user = SeedUser("user@test.com", "p", verified: false);
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserID = user.UserID,
            Token = "unverified-tok",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var ctx = CreateHttpContext();
        ctx.Request.Headers["Cookie"] = "refreshToken=unverified-tok";

        var result = await _sut.RefreshTokenAsync(ctx);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("verify your email");
        _db.RefreshTokens.Single().RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserNotFound_ReturnsFailure()
    {
        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "ghost@test.com",
            NewPassword = "New123!",
            ConfirmNewPassword = "New123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("does not exists");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenPasswordMismatch_ReturnsFailure()
    {
        SeedUser("user@test.com", "Old123!");

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "user@test.com",
            NewPassword = "New123!",
            ConfirmNewPassword = "Different!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Passwords do not match");
    }

    // ── Boundary Tests ────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithMinLength8CharPassword_SucceedsAtLowerBoundary()
    {
        // Boundary: password exactly 8 chars (commonly-used minimum length)
        const string pwd = "Abcd1234";
        SeedUser("boundary@test.com", pwd);

        var result = await _sut.LoginAsync(
            new LoginRequest { Email = "boundary@test.com", Password = pwd },
            CreateHttpContext());

        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMinLength8CharNewPassword_SucceedsAtLowerBoundary()
    {
        // Boundary: newPassword exactly 8 chars
        var user = SeedUser("boundary@test.com", "OldPass1!");
        const string newPwd = "Xyzw5678";

        var ok = await _sut.ChangePasswordAsync(user.UserID, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            NewPassword = newPwd,
            ConfirmNewPassword = newPwd
        });

        ok.Should().BeTrue();
        var reloaded = await _db.Users.FindAsync(user.UserID);
        BCrypt.Net.BCrypt.Verify(newPwd, reloaded!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMinLength8CharNewPassword_SucceedsAtLowerBoundary()
    {
        // Boundary: reset with newPassword exactly 8 chars
        SeedUser("boundary@test.com", "Old123!");
        const string newPwd = "Reset123";

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "boundary@test.com",
            NewPassword = newPwd,
            ConfirmNewPassword = newPwd
        });

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidRequest_UpdatesPasswordAndRevokesTokens()
    {
        var user = SeedUser("user@test.com", "Old123!");
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserID = user.UserID,
            Token = "tok",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "user@test.com",
            NewPassword = "NewPass456!",
            ConfirmNewPassword = "NewPass456!"
        });

        result.Success.Should().BeTrue();
        var reloaded = await _db.Users.FindAsync(user.UserID);
        BCrypt.Net.BCrypt.Verify("NewPass456!", reloaded!.PasswordHash).Should().BeTrue();
        _db.RefreshTokens.Single().RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResendVerificationAsync_WhenUserNotFound_ReturnsFailure()
    {
        var result = await _sut.ResendVerificationAsync(new ResendEmailRequest { Email = "ghost@test.com" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("does not exists");
    }

    [Fact]
    public async Task ResendVerificationAsync_WhenAlreadyVerified_ReturnsFailure()
    {
        SeedUser("user@test.com", "p", verified: true);

        var result = await _sut.ResendVerificationAsync(new ResendEmailRequest { Email = "user@test.com" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already verified");
    }
}
