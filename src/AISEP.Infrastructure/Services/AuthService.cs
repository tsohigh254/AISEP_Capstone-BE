using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AISEP.Application.Configuration;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AISEP.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
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
