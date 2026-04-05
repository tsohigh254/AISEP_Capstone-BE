using System.Net;
using System.Text.Json;
using AISEP.Application.DTOs.Common;

namespace AISEP.WebAPI.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        
        _logger.LogError(exception, "Unhandled exception occurred. RequestId: {RequestId}, Path: {Path}", 
            requestId, context.Request.Path);

        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "The response has already started for RequestId: {RequestId}, Path: {Path}. Skipping exception envelope write.",
                requestId,
                context.Request.Path);
            return;
        }

        context.Response.ContentType = "application/json";
        
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
                "You are not authorized to access this resource"),

            KeyNotFoundException => (StatusCodes.Status404NotFound,
                exception.Message),

            ArgumentException => (StatusCodes.Status400BadRequest,
                exception.Message),

            InvalidOperationException => (StatusCodes.Status409Conflict,
                exception.Message),

            _ => (StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later.")
        };

        context.Response.StatusCode = statusCode;

        var envelope = ApiEnvelope<object>.Error(message, statusCode);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(envelope, options));
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
