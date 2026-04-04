using AISEP.Application.DTOs.Auth;
using AISEP.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Extensions;

/// <summary>
/// Single source-of-truth for converting service results into the standard
/// <see cref="ApiEnvelope{T}"/> response format used by every endpoint.
/// </summary>
public static class ApiEnvelopeExtensions
{
    // ═══════════════════════════════════════════════════════════════
    //  From ApiResponse<T>  (generic service result with data)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Success → 200 OK envelope;  Fail → mapped-status error envelope.</summary>
    public static IActionResult ToEnvelope<T>(this ApiResponse<T> result, string? message = null)
    {
        if (result.Success)
        {
            var envelope = ApiEnvelope<T>.Success(
                result.Data,
                message ?? result.Message ?? "Success");
            return new OkObjectResult(envelope);
        }
        return result.ToErrorEnvelope<T>();
    }

    /// <summary>Success → 201 Created envelope;  Fail → mapped-status error envelope.</summary>
    public static IActionResult ToCreatedEnvelope<T>(this ApiResponse<T> result, string? message = null)
    {
        if (result.Success)
        {
            var envelope = ApiEnvelope<T>.Success(
                result.Data,
                message ?? result.Message ?? "Created",
                StatusCodes.Status201Created);
            return new ObjectResult(envelope) { StatusCode = StatusCodes.Status201Created };
        }
        return result.ToErrorEnvelope<T>();
    }

    /// <summary>Success → 200 OK envelope with data=null  (for delete/void ops).</summary>
    public static IActionResult ToDeletedEnvelope<T>(this ApiResponse<T> result, string? message = null)
    {
        if (result.Success)
        {
            var envelope = ApiEnvelope<object>.Success(
                null,
                message ?? result.Message ?? "Deleted");
            return new OkObjectResult(envelope);
        }
        return result.ToErrorEnvelope<T>();
    }

    /// <summary>Map error code to status and return error envelope.</summary>
    public static IActionResult ToErrorEnvelope<T>(this ApiResponse<T> result)
    {
        var statusCode = MapErrorCodeToStatus(result.Error?.Code);
        var msg = result.Error?.Message ?? result.Message ?? "An error occurred";
        var envelope = ApiEnvelope<T>.Error(msg, statusCode);
        return new ObjectResult(envelope) { StatusCode = statusCode };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Paged  –  ApiResponse<PagedResponse<T>>  →  ApiEnvelope<PagedData<T>>
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Convert a paged service result to the standard paged envelope.</summary>
    public static IActionResult ToPagedEnvelope<T>(
        this ApiResponse<PagedResponse<T>> result, string? message = null)
    {
        if (result.Success && result.Data is not null)
        {
            var pagedData = new PagedData<T>
            {
                Page = result.Data.Paging.Page,
                PageSize = result.Data.Paging.PageSize,
                Total = result.Data.Paging.TotalItems,
                Data = result.Data.Items
            };
            var envelope = ApiEnvelope<PagedData<T>>.Success(
                pagedData,
                message ?? result.Message ?? "Success");
            return new OkObjectResult(envelope);
        }

        var statusCode = MapErrorCodeToStatus(result.Error?.Code);
        var msg = result.Error?.Message ?? result.Message ?? "An error occurred";
        var err = ApiEnvelope<PagedData<T>>.Error(msg, statusCode);
        return new ObjectResult(err) { StatusCode = statusCode };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Auth  –  AuthResponse<AuthData>  →  ApiEnvelope<AuthPayload<UserProfileResponse>>
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Convert an auth service result containing <see cref="AuthData"/>
    /// to the standard auth envelope { data: { data: userDto, accessToken } }.
    /// </summary>
    public static IActionResult ToAuthEnvelope(
        this AuthResponse<AuthData> result,
        int successStatus = StatusCodes.Status200OK,
        string? message = null)
    {
        if (result.Success && result.Data is not null)
        {
            var payload = new AuthPayload<object>
            {
                Data = result.Data.Info,
                AccessToken = result.Data.AccessToken
            };
            var envelope = ApiEnvelope<AuthPayload<object>>.Success(
                payload,
                message ?? result.Message ?? "Success",
                successStatus);
            return new ObjectResult(envelope) { StatusCode = successStatus };
        }

        // Error
        var statusCode = StatusCodes.Status401Unauthorized; // default for auth errors
        var msg = result.Message ?? "Authentication failed";
        var err = ApiEnvelope<AuthPayload<object>>.Error(msg, statusCode);
        return new ObjectResult(err) { StatusCode = statusCode };
    }

    /// <summary>Overload for auth errors with custom code/message.</summary>
    public static IActionResult ToAuthErrorEnvelope(string msg, int statusCode = StatusCodes.Status401Unauthorized)
    {
        var err = ApiEnvelope<AuthPayload<object>>.Error(msg, statusCode);
        return new ObjectResult(err) { StatusCode = statusCode };
    }

    // ═══════════════════════════════════════════════════════════════
    //  From AuthResponse<string>  (register, forgot-password, etc.)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Convert AuthResponse&lt;string&gt; (message-only) to envelope.</summary>
    public static IActionResult ToMessageEnvelope(
        this AuthResponse<string> result,
        int successStatus = StatusCodes.Status200OK,
        string? message = null)
    {
        if (result.Success)
        {
            var envelope = ApiEnvelope<object>.Success(
                null,
                message ?? result.Message ?? result.Data ?? "Success",
                successStatus);
            return new ObjectResult(envelope) { StatusCode = successStatus };
        }

        var statusCode = StatusCodes.Status400BadRequest;
        var msg = result.Message ?? "An error occurred";
        var err = ApiEnvelope<object>.Error(msg, statusCode);
        return new ObjectResult(err) { StatusCode = statusCode };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Static helpers  (for controllers that build responses inline)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>200 OK envelope.</summary>
    public static ObjectResult OkEnvelope<T>(T? data, string message = "Success")
    {
        var envelope = ApiEnvelope<T>.Success(data, message);
        return new ObjectResult(envelope) { StatusCode = StatusCodes.Status200OK };
    }

    /// <summary>201 Created envelope.</summary>
    public static ObjectResult CreatedEnvelope<T>(T? data, string message = "Created")
    {
        var envelope = ApiEnvelope<T>.Success(data, message, StatusCodes.Status201Created);
        return new ObjectResult(envelope) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>Error envelope with arbitrary status.</summary>
    public static ObjectResult ErrorEnvelope(string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        var envelope = ApiEnvelope<object>.Error(message, statusCode);
        return new ObjectResult(envelope) { StatusCode = statusCode };
    }

    /// <summary>200 OK envelope with data=null (for delete/void operations).</summary>
    public static ObjectResult DeletedEnvelope(string message = "Deleted")
    {
        var envelope = ApiEnvelope<object>.Success(null, message);
        return new ObjectResult(envelope) { StatusCode = StatusCodes.Status200OK };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Error-code → HTTP status mapping  (single source of truth)
    // ═══════════════════════════════════════════════════════════════

    internal static int MapErrorCodeToStatus(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return StatusCodes.Status400BadRequest;

        // ── Exact matches ─────────────────────────────────────────
        switch (code)
        {
            case "INVALID_STATUS_TRANSITION":
                return StatusCodes.Status409Conflict;
            case "ACCESS_DENIED":
                return StatusCodes.Status403Forbidden;
            case "NOT_FOUND":
                return StatusCodes.Status404NotFound;
            case "VALIDATION_ERROR":
                return StatusCodes.Status400BadRequest;
        }

        // ── Suffix matches ────────────────────────────────────────
        if (code.EndsWith("_NOT_FOUND"))
            return StatusCodes.Status404NotFound;

        if (code.EndsWith("_ALREADY_EXISTS"))
            return StatusCodes.Status409Conflict;

        if (code.EndsWith("_NOT_OWNED"))
            return StatusCodes.Status403Forbidden;

        // ── Default ───────────────────────────────────────────────
        return StatusCodes.Status400BadRequest;
    }
}
