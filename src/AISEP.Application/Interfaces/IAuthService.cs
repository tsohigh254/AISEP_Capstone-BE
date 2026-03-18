using AISEP.Application.DTOs.Auth;

namespace AISEP.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse<string>> RegisterAsync(RegisterRequest request);
    Task<AuthResponse<AuthData>> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponse<AuthData>> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null);
    Task<bool> LogoutAsync(string refreshToken);
    Task<bool> RevokeAllTokensAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> AdminResetPasswordAsync(int userId, string newPassword);
    Task<AuthResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AuthResponse<string>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<AuthResponse<AuthData>> VerifyEmailAsync(EmailVerifyRequest emailVerifyRequest, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponse<string>> ResendVerificationAsync(ResendEmailRequest resendEmailRequest);
}
