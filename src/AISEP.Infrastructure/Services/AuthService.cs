using AISEP.Application.Configuration;
using AISEP.Application.DTOs;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

    public async Task<AuthResponse<string>> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existingUser != null)
        {
            return new AuthResponse<string>
            {
                Success = false,
                Message = "Email already registered"
            };
        }

        // Validate user type
        var validUserTypes = new[] { "Startup", "Investor", "Advisor" };
        if (!validUserTypes.Contains(request.UserType))
        {
            return new AuthResponse<string>
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

        var newOtp = await GenerateOtp(user.UserID);

        await SendEmail(user.UserID, user.Email, newOtp);

        return new AuthResponse<string>
        {
            Success = true,
            Message = "Register successfully, open your email to get the otp code",
            Data = user.Email
        };
    }

    public async Task<AuthResponse<AuthData>> LoginAsync(LoginRequest request, HttpContext context, string? ipAddress = null, string? userAgent = null)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponse<AuthData>
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

        SetupToken(context, refreshTokenExpires, refreshToken);

        return new AuthResponse<AuthData>
        {
            Success = true,
            Message = "Login successful",
            Data = new AuthData
            {
                Info = new UserProfileResponse(user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified, user.CreatedAt, user.LastLoginAt, roles),          
                AccessToken = accessToken,
                AccessTokenExpires = accessTokenExpires,
            }
        };
    }

    public async Task<AuthResponse<AuthData>> RefreshTokenAsync(HttpContext context, string? ipAddress = null, string? userAgent = null)
    {
        var refreshToken = context.Request.Cookies["refreshToken"];

        if (refreshToken == null)
        {
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Invalid refresh token"
            };
        }

        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null)
        {
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Invalid refresh token"
            };
        }

        if (!token.IsActive)
        {
            return new AuthResponse<AuthData>
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

        SetupToken(context, refreshTokenExpires, newRefreshToken);

        return new AuthResponse<AuthData>
        {
            Success = true,
            Message = "Token refreshed successfully",
            Data = new AuthData
            {
                Info = new UserProfileResponse(user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified, user.CreatedAt, user.LastLoginAt, roles),
                AccessToken = newAccessToken,
                AccessTokenExpires = accessTokenExpires,
            }
        };
    }

    public async Task<bool> LogoutAsync(HttpContext context)
    {
        var refreshToken = context.Request.Cookies["refreshToken"];

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null) return false;

        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        SetupToken(context, DateTime.UnixEpoch, string.Empty);

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

    public async Task<AuthResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        // Always return success to prevent email enumeration attacks
        if (user == null)
            return new AuthResponse<string>
            {
                Success = false,
                Message = "User does not exists"
            };

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
        var newOtp = await GenerateOtp(user.UserID);

        await SendEmail(user.UserID, user.Email, newOtp);

        return new AuthResponse<string>
        {
            Success = true,
            Message = "Email sent successfully"
        };
    }

    public async Task<AuthResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return new AuthResponse<string>
            {
                Success = false,
                Message = "User does not exists"
            };
        }

        if (request.NewPassword != request.ConfirmNewPassword)
            return new AuthResponse<string>
            {
                Success = false,
                Message = "Passwords do not match"
            };       

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Revoke all refresh tokens
        await RevokeAllTokensAsync(user.UserID);

        return new AuthResponse<string>
        {
            Success = true,
            Message = "Password has been reset successfully"
        };
    }

    public async Task<AuthResponse<AuthData>> VerifyEmailAsync(EmailVerifyRequest emailVerifyRequest, HttpContext context, string? ipAddress = null, string? userAgent = null)
    {
        var user = await _context.Users
            .Include(u => u.EmailOtps)
            .FirstOrDefaultAsync(u => u.Email == emailVerifyRequest.Email);

        if (user != null && !user.IsActive)      
            return new AuthResponse<AuthData>            {
                Success = false,
                Message = "Account is deactivated"
            };

        if (user!.EmailOtps.Any(otp => otp.Otp == emailVerifyRequest.Otp && otp.IsUsed || otp.Otp == emailVerifyRequest.Otp && otp.ExpiredAt < DateTime.UtcNow))
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Otp code expired"
            };
     
        // Update last login
        user.LastLoginAt = DateTime.UtcNow;

        foreach (var otp in user.EmailOtps)
        {
            if (otp.Otp == emailVerifyRequest.Otp)
            {
                user.EmailVerified = true;
                _context.EmailOtps.Remove(otp);
                break;
            }
        }

        await _context.SaveChangesAsync();

        // Generate tokens
        var (accessToken, accessTokenExpires) = GenerateAccessToken(user);
        var (refreshToken, refreshTokenExpires) = await GenerateRefreshTokenAsync(user.UserID, ipAddress, userAgent);

        // Get user roles
        var roles = await GetUserRolesAsync(user.UserID);

        SetupToken(context, refreshTokenExpires, refreshToken);

        return new AuthResponse<AuthData>
        {
            Success = true,
            Message = "Email verified successfully",
            Data = new AuthData
            {
                Info = new UserProfileResponse(user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified, user.CreatedAt, user.LastLoginAt, roles),
                AccessToken = accessToken,
                AccessTokenExpires = accessTokenExpires,
            }
        };
    }

    public async Task<AuthResponse<string>> ResendVerificationAsync(ResendEmailRequest resendEmailRequest)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == resendEmailRequest.Email);

        if (user == null)
            return new AuthResponse<string>
            {
                Success = false,
                Message = "User does not exists"
            };

        foreach (var otp in user.EmailOtps)
        {
            if (!otp.IsUsed)
                otp.IsUsed = true;
        }

        var newOtp = await GenerateOtp(user.UserID);

        await SendEmail(user.UserID, user.Email, newOtp);

        return new AuthResponse<string>
        {
            Success = true,
            Message = "Email sent successfully"
        };
    }

    #region helper method
    private async Task<string> GenerateOtp(int userId)
    {
        var otp = new Random().Next(100000, 999999).ToString();

        var emailOtp = new EmailOtp
        {
            UserId = userId,
            IsUsed = false,
            Otp = otp
        };

        _context.EmailOtps.Add(emailOtp);
        await _context.SaveChangesAsync();

        return otp;
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
        var token = Guid.NewGuid() + "-" + Guid.NewGuid();
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

    private void SetupToken(HttpContext context, DateTime dateTime, string refreshToken)
    {
        context.Response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                Expires = dateTime,
                Path = ""
            });
    }

    private async Task SendEmail(int userId, string email, string otp)
    {
        var htmlBody = $"<p>M� x�c nh?n email c?a b?n l�:</p> " +
            $" <p class=\"otp\">{otp}</p> " +
            $"<p>M� s? h?t h?n trong {5} ph�t. Kh�ng chia s? m� otp n�y cho b?t k� ai</p>";

        await _emailService.SendEmailAsync(email, "M?t email ?� g?i ??n email c?a b?n . H�y nh?p m� x�c nh?n", htmlBody);
    }
    #endregion
}
