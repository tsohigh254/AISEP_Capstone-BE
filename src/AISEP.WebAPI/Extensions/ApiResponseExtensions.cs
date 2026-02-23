using AISEP.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Extensions;

/// <summary>
/// Centralized mapping from <see cref="ApiResponse{T}"/> error codes to HTTP status codes.
/// All controllers should use <see cref="ToErrorResult{T}"/> for error responses so that
/// status codes are consistent across the entire API surface.
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Maps an <see cref="ApiResponse{T}"/> with <c>Success == false</c> to the
    /// appropriate HTTP status code based on <c>Error.Code</c>.
    /// <para>Call this only when <c>result.Success == false</c>.</para>
    /// </summary>
    public static IActionResult ToErrorResult<T>(this ApiResponse<T> result)
    {
        var statusCode = MapErrorCodeToStatus(result.Error?.Code);
        return new ObjectResult(result) { StatusCode = statusCode };
    }

    /// <summary>
    /// Convenience: returns <c>200 OK</c> on success, or the correct error status on failure.
    /// Controllers may still return 201/204 manually for create/delete success cases.
    /// </summary>
    public static IActionResult ToActionResult<T>(this ApiResponse<T> result)
    {
        if (result.Success)
            return new OkObjectResult(result);

        return result.ToErrorResult();
    }

    /// <summary>
    /// Central mapping rules — single source of truth for the entire API.
    /// </summary>
    internal static int MapErrorCodeToStatus(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return StatusCodes.Status400BadRequest;

        // ── Exact matches (highest priority) ──────────────────────────
        switch (code)
        {
            case "INVALID_STATUS_TRANSITION":
                return StatusCodes.Status409Conflict;
            case "ACCESS_DENIED":
                return StatusCodes.Status403Forbidden;
            case "VALIDATION_ERROR":
                return StatusCodes.Status400BadRequest;
        }

        // ── Pattern (suffix) matches ──────────────────────────────────
        if (code.EndsWith("_NOT_FOUND"))
            return StatusCodes.Status404NotFound;

        if (code.EndsWith("_ALREADY_EXISTS"))
            return StatusCodes.Status409Conflict;

        if (code.EndsWith("_NOT_OWNED"))
            return StatusCodes.Status403Forbidden;

        // ── Default ───────────────────────────────────────────────────
        return StatusCodes.Status400BadRequest;
    }
}
