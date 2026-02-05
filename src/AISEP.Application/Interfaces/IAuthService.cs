using AISEP.Application.DTOs.Auth;

namespace AISEP.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null);
    Task<bool> LogoutAsync(int userId, string refreshToken);
    Task<bool> RevokeAllTokensAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
