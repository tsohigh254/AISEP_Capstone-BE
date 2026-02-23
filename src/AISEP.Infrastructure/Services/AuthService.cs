using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AISEP.Application.Configuration;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AISEP.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;

    public AuthService(
        ApplicationDbContext context, 
        IOptions<JwtSettings> jwtSettings,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, string? userAgent = null)
    {
        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existingUser != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already registered"
            };
        }

        // Validate user type
        var validUserTypes = new[] { "Startup", "Investor", "Advisor" };
        if (!validUserTypes.Contains(request.UserType))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid user type. Must be Startup, Investor, or Advisor"
            };
        }

        // Create new user
        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            UserType = request.UserType,
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assign default role based on user type
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.UserType);
        if (role != null)
        {
            var userRole = new UserRole
            {
                UserID = user.UserID,
                RoleID = role.RoleID,
                AssignedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        // Generate tokens
        var (accessToken, accessTokenExpires) = GenerateAccessToken(user);
        var (refreshToken, refreshTokenExpires) = await GenerateRefreshTokenAsync(user.UserID, ipAddress, userAgent);

        // Get user roles
        var roles = await GetUserRolesAsync(user.UserID);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            Data = new AuthData
            {
                UserID = user.UserID,
                Email = user.Email,
                UserType = user.UserType,
                Roles = roles,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpires = accessTokenExpires,
                RefreshTokenExpires = refreshTokenExpires
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Account is deactivated"
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate tokens
        var (accessToken, accessTokenExpires) = GenerateAccessToken(user);
        var (refreshToken, refreshTokenExpires) = await GenerateRefreshTokenAsync(user.UserID, ipAddress, userAgent);

        // Get user roles
        var roles = await GetUserRolesAsync(user.UserID);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            Data = new AuthData
            {
                UserID = user.UserID,
                Email = user.Email,
                UserType = user.UserType,
                Roles = roles,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpires = accessTokenExpires,
                RefreshTokenExpires = refreshTokenExpires
            }
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid refresh token"
            };
        }

        if (!token.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Refresh token is expired or revoked"
            };
        }

        // Revoke current token
        token.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var user = token.User;
        var (newAccessToken, accessTokenExpires) = GenerateAccessToken(user);
        var (newRefreshToken, refreshTokenExpires) = await GenerateRefreshTokenAsync(user.UserID, ipAddress, userAgent);

        // Link old token to new one
        token.ReplacedByToken = newRefreshToken;
        await _context.SaveChangesAsync();

        // Get user roles
        var roles = await GetUserRolesAsync(user.UserID);

        return new AuthResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            Data = new AuthData
            {
                UserID = user.UserID,
                Email = user.Email,
                UserType = user.UserType,
                Roles = roles,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpires = accessTokenExpires,
                RefreshTokenExpires = refreshTokenExpires
            }
        };
    }

    public async Task<bool> LogoutAsync(int userId, string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserID == userId && rt.Token == refreshToken);

        if (token == null) return false;

        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeAllTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserID == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Revoke all refresh tokens after password change
        await RevokeAllTokensAsync(userId);

        return true;
    }

    public async Task<bool> AdminResetPasswordAsync(int userId, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Revoke all refresh tokens after password reset
        await RevokeAllTokensAsync(userId);

        return true;
    }

    public async Task<(bool Success, string? Message)> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        
        // Always return success to prevent email enumeration attacks
        if (user == null)
        {
            return (true, "If your email is registered, you will receive a password reset link.");
        }

        // Invalidate any existing reset tokens
        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserID == user.UserID && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var existingToken in existingTokens)
        {
            existingToken.UsedAt = DateTime.UtcNow; // Mark as used
        }

        // Generate secure token
        var token = GenerateSecureToken();
        var resetToken = new PasswordResetToken
        {
            UserID = user.UserID,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Token valid for 1 hour
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        // Send email with reset link
        try
        {
            var resetUrl = $"{_emailSettings.BaseUrl}/reset-password";
            await _emailService.SendPasswordResetEmailAsync(user.Email, token, resetUrl);
        }
        catch (Exception)
        {
            // Log error but don't expose to user
            return (true, "If your email is registered, you will receive a password reset link.");
        }

        return (true, "If your email is registered, you will receive a password reset link.");
    }

    public async Task<(bool Success, string? Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return (false, "Passwords do not match");
        }

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token && t.UsedAt == null);

        if (resetToken == null)
        {
            return (false, "Invalid or expired reset token");
        }

        if (resetToken.IsExpired)
        {
            return (false, "Reset token has expired");
        }

        // Update password
        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        
        // Mark token as used
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Revoke all refresh tokens
        await RevokeAllTokensAsync(resetToken.UserID);

        return (true, "Password has been reset successfully");
    }

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private (string token, DateTime expires) GenerateAccessToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("userType", user.UserType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private async Task<(string token, DateTime expires)> GenerateRefreshTokenAsync(int userId, string? ipAddress, string? userAgent)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var refreshToken = new RefreshToken
        {
            UserID = userId,
            Token = token,
            ExpiresAt = expires,
            CreatedAt = DateTime.UtcNow,
            IPAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return (token, expires);
    }

    private async Task<List<string>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserID == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.RoleName)
            .ToListAsync();
    }
}
