using AISEP.Application.Configuration;
using AISEP.Application.DTOs;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.DTOs.Notification;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Settings;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AuthService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuthService(
        ApplicationDbContext context, 
        IOptions<JwtSettings> jwtSettings,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        ILogger<AuthService> logger,
        INotificationDeliveryService notifications,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<AuthResponse<string>> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var validUserTypes = new[] { "Startup", "Investor", "Advisor" };

        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (existingUser != null)
        {
            return new AuthResponse<string>
            {
                Success = false,
                Message = "Email already registered"
            };
        }

        // Validate user type
        var normalizedUserType = validUserTypes.FirstOrDefault(x =>
            string.Equals(x, request.UserType, StringComparison.OrdinalIgnoreCase));

        if (normalizedUserType is null)
        {
            return new AuthResponse<string>
            {
                Success = false,
                Message = "Invalid user type. Must be Startup, Investor, or Advisor"
            };
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create new user
            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                UserType = normalizedUserType,
                IsActive = true,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default role based on user type
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == normalizedUserType);
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
            await transaction.CommitAsync();
            // Send welcome notification (fire-and-forget với scope riêng để tránh disposed DbContext)
            var userId = user.UserID;
            var userType = normalizedUserType;
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
                await notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = userId,
                    NotificationType = "WELCOME",
                    Title = "Chào mừng bạn đến với AISEP! 🎉",
                    Message = $"Xin chào! Tài khoản {userType} của bạn đã được tạo thành công. Hãy hoàn thiện hồ sơ để bắt đầu kết nối với hệ sinh thái khởi nghiệp.",
                    ActionUrl = "/dashboard"
                });
            });
            return new AuthResponse<string>
            {
                Success = true,
                Message = "Register successfully, open your email to get the otp code",
                Data = user.Email
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Registration failed for email {Email}", normalizedEmail);

            return new AuthResponse<string>
            {
                Success = false,
                Message = "Unable to complete registration because the verification email could not be sent. Please try again."
            };
        }
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

        if (!user.EmailVerified)
        {
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Please verify your email before logging in"
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate tokens
        var roles = await GetUserRolesAsync(user.UserID);
        var (accessToken, accessTokenExpires) = GenerateAccessToken(user, roles);
        var (refreshToken, refreshTokenExpires) = await GenerateRefreshTokenAsync(user.UserID, ipAddress, userAgent);

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

        if (!token.User.EmailVerified)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            SetupToken(context, DateTime.UnixEpoch, string.Empty);

            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Please verify your email before logging in"
            };
        }

        // Revoke current token
        token.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var user = token.User;
        var roles = await GetUserRolesAsync(user.UserID);
        var (newAccessToken, accessTokenExpires) = GenerateAccessToken(user, roles);
        var (newRefreshToken, refreshTokenExpires) = await GenerateRefreshTokenAsync(user.UserID, ipAddress, userAgent);

        // Link old token to new one
        token.ReplacedByToken = newRefreshToken;
        await _context.SaveChangesAsync();

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
                Success = true,
                Message = "If your email is registered, you will receive a reset link shortly"
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

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
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailVerifyRequest.Email.ToLower());

        if (user != null && !user.IsActive)      
            return new AuthResponse<AuthData>            {
                Success = false,
                Message = "Account is deactivated"
            };

        if (user == null)
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Invalid email or OTP"
            };

        var matchedOtp = user.EmailOtps.FirstOrDefault(otp => otp.Otp == emailVerifyRequest.Otp);
        if (matchedOtp == null)
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "Invalid email or OTP"
            };

        if (matchedOtp.IsUsed || matchedOtp.ExpiredAt < DateTime.UtcNow)
            return new AuthResponse<AuthData>
            {
                Success = false,
                Message = "OTP code has expired or already been used"
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
        var roles = await GetUserRolesAsync(user.UserID);
        var (accessToken, accessTokenExpires) = GenerateAccessToken(user, roles);
        var (refreshToken, refreshTokenExpires) = await GenerateRefreshTokenAsync(user.UserID, ipAddress, userAgent);

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
        const int ResendCooldownSeconds = 60;
        const int MaxResendPer24h = 5;

        var normalizedEmail = resendEmailRequest.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
            return new AuthResponse<string>
            {
                Success = false,
                Message = "User does not exists"
            };

        if (!user.IsActive)
        {
            return new AuthResponse<string>
            {
                Success = false,
                Message = "Account is locked or deactivated"
            };
        }

        if (user.EmailVerified)
        {
            return new AuthResponse<string>
            {
                Success = false,
                Message = "Email is already verified"
            };
        }

        var now = DateTime.UtcNow;

        var lastCreatedAt = await _context.EmailOtps
            .Where(o => o.UserId == user.UserID)
            .MaxAsync(o => (DateTime?)o.CreatedAt);

        if (lastCreatedAt.HasValue)
        {
            var elapsed = (now - lastCreatedAt.Value).TotalSeconds;
            if (elapsed < ResendCooldownSeconds)
            {
                var waitSeconds = (int)Math.Ceiling(ResendCooldownSeconds - elapsed);
                return new AuthResponse<string>
                {
                    Success = false,
                    Message = $"Please wait {waitSeconds}s before requesting another verification email"
                };
            }
        }

        var windowStart = now.AddHours(-24);
        var recentCount = await _context.EmailOtps
            .CountAsync(o => o.UserId == user.UserID && o.CreatedAt >= windowStart);

        if (recentCount >= MaxResendPer24h)
        {
            return new AuthResponse<string>
            {
                Success = false,
                Message = $"You have reached the maximum of {MaxResendPer24h} verification emails per 24 hours. Please try again later."
            };
        }

        var pendingOtps = await _context.EmailOtps
            .Where(otp => otp.UserId == user.UserID && !otp.IsUsed)
            .ToListAsync();

        foreach (var otp in pendingOtps)
        {
            otp.IsUsed = true;
        }

        await _context.SaveChangesAsync();

        try
        {
            var newOtp = await GenerateOtp(user.UserID);
            await SendEmail(user.UserID, user.Email, newOtp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);

            return new AuthResponse<string>
            {
                Success = false,
                Message = "Unable to send verification email right now. Please try again."
            };
        }

        return new AuthResponse<string>
        {
            Success = true,
            Message = "Email sent successfully"
        };
    }

    #region helper method
    private async Task<string> GenerateOtp(int userId)
    {
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        var now = DateTime.UtcNow;
        var emailOtp = new EmailOtp
        {
            UserId = userId,
            IsUsed = false,
            Otp = otp,
            CreatedAt = now,
            ExpiredAt = now.AddMinutes(5)
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

private (string token, DateTime expires) GenerateAccessToken(User user, IList<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("userType", user.UserType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

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
        var htmlBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Mã xác nhận OTP</title>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f9; font-family: Arial, Helvetica, sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6f9; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table width='560' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.08);'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 36px 40px; text-align:center;'>
                            <h1 style='margin:0; color:#ffffff; font-size:22px; font-weight:700; letter-spacing:0.5px;'>AISEP</h1>
                            <p style='margin:8px 0 0; color:#bfdbfe; font-size:13px;'>Hệ thống tư vấn học thuật thông minh</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding: 40px 48px;'>
                            <p style='margin:0 0 12px; font-size:16px; color:#374151;'>Xin chào,</p>
                            <p style='margin:0 0 28px; font-size:15px; color:#6b7280; line-height:1.6;'>
                                Chúng tôi đã nhận được yêu cầu xác nhận email của bạn. Vui lòng sử dụng mã OTP dưới đây:
                            </p>
                            <!-- OTP Box -->
                            <div style='background-color:#f0f4ff; border: 2px dashed #2563eb; border-radius:10px; padding: 28px 20px; text-align:center; margin-bottom:28px;'>
                                <p style='margin:0 0 8px; font-size:13px; color:#6b7280; text-transform:uppercase; letter-spacing:1px;'>Mã xác nhận của bạn</p>
                                <p style='margin:0; font-size:42px; font-weight:800; letter-spacing:10px; color:#2563eb;'>{otp}</p>
                            </div>
                            <p style='margin:0 0 8px; font-size:14px; color:#6b7280; line-height:1.6;'>
                                ⏱️ Mã có hiệu lực trong <strong style='color:#374151;'>5 phút</strong>.
                            </p>
                            <p style='margin:0; font-size:14px; color:#6b7280; line-height:1.6;'>
                                🔒 Không chia sẻ mã OTP này với bất kỳ ai, kể cả nhân viên AISEP.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f9fafb; padding: 20px 48px; border-top: 1px solid #e5e7eb;'>
                            <p style='margin:0; font-size:12px; color:#9ca3af; text-align:center; line-height:1.6;'>
                                Đây là email tự động từ hệ thống AISEP. Vui lòng không trả lời email này.<br>
                                Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        await _emailService.SendEmailAsync(email, "Mã xác nhận OTP - AISEP", htmlBody);
    }
    #endregion
}
