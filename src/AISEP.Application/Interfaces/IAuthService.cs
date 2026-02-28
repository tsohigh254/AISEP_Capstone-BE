using AISEP.Application.DTOs.Auth;
using Microsoft.AspNetCore.Http;

namespace AISEP.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse<string>> RegisterAsync(RegisterRequest request);
    Task<AuthResponse<AuthData>> LoginAsync(LoginRequest request, HttpContext context, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponse<AuthData>> RefreshTokenAsync(HttpContext context, string? ipAddress = null, string? userAgent = null);
    Task<bool> LogoutAsync(HttpContext context);
    Task<bool> RevokeAllTokensAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> AdminResetPasswordAsync(int userId, string newPassword);
    Task<AuthResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AuthResponse<string>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<AuthResponse<AuthData>> VerifyEmailAsync(EmailVerifyRequest emailVerifyRequest, HttpContext context, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponse<string>> ResendVerificationAsync(ResendEmailRequest resendEmailRequest);
}
