using System.Security.Claims;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.ToMessageEnvelope(message: "Registered successfully, open your email to get the otp code");
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiEnvelope<AuthPayload<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, HttpContext, ipAddress, userAgent);
        return result.ToAuthEnvelope(message: "Login successful");
    }

    /// <summary>Refresh access token using refresh token</summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiEnvelope<AuthPayload<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RefreshTokenAsync(HttpContext, ipAddress, userAgent);
        return result.ToAuthEnvelope(message: "Token refreshed");
    }

    /// <summary>Logout and revoke refresh token</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync(HttpContext);

        if (!result)
            return ApiEnvelopeExtensions.ErrorEnvelope("Logout failed", StatusCodes.Status400BadRequest);

        return ApiEnvelopeExtensions.OkEnvelope<object>(null, "Logged out successfully");
    }

    /// <summary>Revoke all refresh tokens for current user</summary>
    [HttpPost("revoke-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return ApiEnvelopeExtensions.ErrorEnvelope("User not authenticated", StatusCodes.Status401Unauthorized);

        await _authService.RevokeAllTokensAsync(userId.Value);
        return ApiEnvelopeExtensions.OkEnvelope<object>(null, "All tokens revoked successfully");
    }

    /// <summary>Change password for current user</summary>
    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return ApiEnvelopeExtensions.ErrorEnvelope("User not authenticated", StatusCodes.Status401Unauthorized);

        var result = await _authService.ChangePasswordAsync(userId.Value, request);

        if (!result)
            return ApiEnvelopeExtensions.ErrorEnvelope("Current password is incorrect", StatusCodes.Status400BadRequest);

        return ApiEnvelopeExtensions.OkEnvelope<object>(null, "Password changed successfully");
    }

    /// <summary>Admin reset password for any user</summary>
    [HttpPut("admin/reset-password")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminResetPassword([FromBody] AdminResetPasswordRequest request)
    {
        var result = await _authService.AdminResetPasswordAsync(request.UserId, request.NewPassword);

        if (!result)
            return ApiEnvelopeExtensions.ErrorEnvelope("User not found", StatusCodes.Status404NotFound);

        return ApiEnvelopeExtensions.OkEnvelope<object>(null, "Password reset successfully");
    }

    /// <summary>Request password reset email</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return result.ToMessageEnvelope();
    }

    /// <summary>Reset password using token from email</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        return result.ToMessageEnvelope(message: "Password reset successfully");
    }

    /// <summary>Verify email using otp</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(ApiEnvelope<AuthPayload<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerifyRequest emailVerifyRequest)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.VerifyEmailAsync(emailVerifyRequest, HttpContext, ipAddress, userAgent);
        return result.ToAuthEnvelope(message: "Email verified successfully");
    }

    /// <summary>Resend verification email using otp</summary>
    [HttpPost("resend")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendEmailRequest resendEmailRequest)
    {
        var result = await _authService.ResendVerificationAsync(resendEmailRequest);
        return result.ToMessageEnvelope(message: "Verification email sent");
    }

    #region helper method
    private string? GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString();
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }
    #endregion
}
