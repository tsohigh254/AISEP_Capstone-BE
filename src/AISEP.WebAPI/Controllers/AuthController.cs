using System.Security.Claims;
using AISEP.Application.DTOs.Auth;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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

        var result = await _authService.RegisterAsync(request, ipAddress, userAgent);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse("REGISTRATION_FAILED", result.Message ?? "Registration failed"));
        }

        return Ok(ApiResponse<AuthData>.SuccessResponse(result.Data!, result.Message));
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

        var result = await _authService.LoginAsync(request, ipAddress, userAgent);

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
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress, userAgent);

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
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse.ErrorResponse("UNAUTHORIZED", "User not authenticated"));
        }

        var result = await _authService.LogoutAsync(userId.Value, request.RefreshToken);

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
}
