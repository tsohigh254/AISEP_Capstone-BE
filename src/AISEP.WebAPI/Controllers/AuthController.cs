using System.Security.Claims;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
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

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse("REGISTRATION_FAILED", result.Message ?? "Registration failed"));
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request,HttpContext, ipAddress, userAgent);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse.ErrorResponse("LOGIN_FAILED", result.Message ?? "Login failed"));
        }

        return Ok(ApiResponse<AuthData>.SuccessResponse(result.Data!, result.Message));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RefreshTokenAsync(HttpContext, ipAddress, userAgent);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse.ErrorResponse("REFRESH_FAILED", result.Message ?? "Token refresh failed"));
        }

        return Ok(ApiResponse<AuthData>.SuccessResponse(result.Data!, result.Message));
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        //var userId = GetCurrentUserId();
        //if (userId == null)
        //{
        //    return Unauthorized(ApiResponse.ErrorResponse("UNAUTHORIZED", "User not authenticated"));
        //}

        var result = await _authService.LogoutAsync(HttpContext);

        if (!result)
        {
            return BadRequest(ApiResponse.ErrorResponse("LOGOUT_FAILED", "Logout failed"));
        }

        return Ok(ApiResponse.SuccessResponse("Logged out successfully"));
    }

    /// <summary>
    /// Revoke all refresh tokens for current user
    /// </summary>
    [HttpPost("revoke-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse.ErrorResponse("UNAUTHORIZED", "User not authenticated"));
        }

        await _authService.RevokeAllTokensAsync(userId.Value);
        return Ok(ApiResponse.SuccessResponse("All tokens revoked successfully"));
    }

    /// <summary>
    /// Change password for current user
    /// </summary>
    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse.ErrorResponse("UNAUTHORIZED", "User not authenticated"));
        }

        var result = await _authService.ChangePasswordAsync(userId.Value, request);

        if (!result)
        {
            return BadRequest(ApiResponse.ErrorResponse("CHANGE_PASSWORD_FAILED", "Current password is incorrect"));
        }

        return Ok(ApiResponse.SuccessResponse("Password changed successfully"));
    }

    /// <summary>
    /// Admin reset password for any user
    /// </summary>
    [HttpPut("admin/reset-password")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminResetPassword([FromBody] AdminResetPasswordRequest request)
    {
        var result = await _authService.AdminResetPasswordAsync(request.UserId, request.NewPassword);

        if (!result)
        {
            return NotFound(ApiResponse.ErrorResponse("USER_NOT_FOUND", "User not found"));
        }

        return Ok(ApiResponse.SuccessResponse("Password reset successfully"));
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Reset password using token from email
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse("RESET_PASSWORD_FAILED", result.Message ?? "Password reset failed"));
        }

        return Ok(result);
    }

    /// <summary>
    /// Verify email using otp
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerifyRequest emailVerifyRequest)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.VerifyEmailAsync(emailVerifyRequest, HttpContext, ipAddress, userAgent);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse("VERIFY_EMAIL_FAILED", result.Message ?? "Verify email failed"));
        }

        return Ok(result);
    }

    /// <summary>
    /// Resend verification email using otp
    /// </summary>
    [HttpPost("resend")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendEmailRequest resendEmailRequest)
    {

        var result = await _authService.ResendVerificationAsync(resendEmailRequest);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse("RESEND_EMAIL_FAILED", result.Message ?? "Resend email failed"));
        }

        return Ok(result);
    }


    #region helper method
    private string? GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
    #endregion
}
