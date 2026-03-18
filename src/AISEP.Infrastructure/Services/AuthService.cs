using AISEP.Application.Configuration;
using AISEP.Application.DTOs;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AISEP.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailService _emailService;

    public AuthService(
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IEmailService emailService)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _emailService = emailService;
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

    public async Task<AuthResponse<AuthData>> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null)
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

        return new AuthResponse<AuthData>
        {
            Success = true,
            Message = "Login successful",
            Data = new AuthData
            {
                Info = new UserProfileResponse(user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified, user.CreatedAt, user.LastLoginAt, roles),
                AccessToken = accessToken,
                AccessTokenExpires = accessTokenExpires,
                RefreshToken = refreshToken,
                RefreshTokenExpires = refreshTokenExpires,
            }
        };
    }

    public async Task<AuthResponse<AuthData>> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null)
    {
        if (string.IsNullOrEmpty(refreshToken))
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

        return new AuthResponse<AuthData>
        {
            Success = true,
            Message = "Token refreshed successfully",
            Data = new AuthData
            {
                Info = new UserProfileResponse(user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified, user.CreatedAt, user.LastLoginAt, roles),
                AccessToken = newAccessToken,
                AccessTokenExpires = accessTokenExpires,
                RefreshToken = newRefreshToken,
                RefreshTokenExpires = refreshTokenExpires,
            }
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return false;

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

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

    public async Task<AuthResponse<AuthData>> VerifyEmailAsync(EmailVerifyRequest emailVerifyRequest, string? ipAddress = null, string? userAgent = null)
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

        return new AuthResponse<AuthData>
        {
            Success = true,
            Message = "Email verified successfully",
            Data = new AuthData
            {
                Info = new UserProfileResponse(user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified, user.CreatedAt, user.LastLoginAt, roles),
                AccessToken = accessToken,
                AccessTokenExpires = accessTokenExpires,
                RefreshToken = refreshToken,
                RefreshTokenExpires = refreshTokenExpires,
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

    private async Task SendEmail(int userId, string email, string otp)
    {
        var otpChars = string.Join("", otp.Select(c =>
            $"<td style='width:48px;height:56px;background-color:#FFFDE7;border:2px solid #FFD54F;border-radius:12px;text-align:center;vertical-align:middle;font-family:Arial,Helvetica,sans-serif;font-size:28px;font-weight:700;color:#F9A825;letter-spacing:2px;'>{c}</td>"));

        var htmlBody = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width,initial-scale=1.0"">
    <title>AISEP - Email Verification</title>
</head>
<body style=""margin:0;padding:0;background-color:#FFF9C4;font-family:Arial,Helvetica,sans-serif;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FFF9C4;padding:40px 0;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FFFFFF;border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,0.08);overflow:hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background:linear-gradient(135deg,#FFD54F,#FFECB3);padding:32px 40px;text-align:center;"">
                            <h1 style=""margin:0;font-size:28px;font-weight:700;color:#F57F17;letter-spacing:1px;"">AISEP</h1>
                            <p style=""margin:8px 0 0;font-size:14px;color:#F9A825;font-weight:500;"">AI-powered Startup Ecosystem Platform</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""padding:36px 40px 20px;"">
                            <h2 style=""margin:0 0 8px;font-size:22px;font-weight:700;color:#333333;text-align:center;"">X&#225;c nh&#7853;n email c&#7911;a b&#7841;n</h2>
                            <p style=""margin:0 0 28px;font-size:15px;color:#666666;text-align:center;line-height:1.6;"">
                                Vui l&#242;ng s&#7917; d&#7909;ng m&#227; OTP b&#234;n d&#432;&#7899;i &#273;&#7875; ho&#224;n t&#7845;t x&#225;c minh email.
                            </p>
                            <!-- OTP Code -->
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""8"" style=""margin:0 auto 28px;"">
                                <tr>
                                    {otpChars}
                                </tr>
                            </table>
                            <!-- Timer notice -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td style=""background-color:#FFFDE7;border-left:4px solid #FFD54F;border-radius:0 8px 8px 0;padding:14px 18px;"">
                                        <p style=""margin:0;font-size:14px;color:#F57F17;font-weight:600;"">&#9200; M&#227; s&#7869; h&#7871;t h&#7841;n trong 5 ph&#250;t</p>
                                        <p style=""margin:6px 0 0;font-size:13px;color:#999999;"">Kh&#244;ng chia s&#7867; m&#227; n&#224;y cho b&#7845;t k&#7923; ai.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""padding:20px 40px 32px;"">
                            <hr style=""border:none;border-top:1px solid #FFF3C4;margin:0 0 20px;"">
                            <p style=""margin:0;font-size:12px;color:#BDBDBD;text-align:center;line-height:1.6;"">
                                &#272;&#226;y l&#224; email t&#7921; &#273;&#7897;ng t&#7915; AISEP. Vui l&#242;ng kh&#244;ng tr&#7843; l&#7901;i email n&#224;y.<br>
                                &copy; 2026 AISEP. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await _emailService.SendEmailAsync(email, "AISEP - Ma xac nhan email cua ban", htmlBody);
    }
    #endregion
}
