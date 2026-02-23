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

        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            UnauthorizedAccessException => new ErrorResponse(
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                "You are not authorized to access this resource"),
            
            KeyNotFoundException => new ErrorResponse(
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                exception.Message),
            
            ArgumentException => new ErrorResponse(
                HttpStatusCode.BadRequest,
                "BAD_REQUEST",
                exception.Message),
            
            InvalidOperationException => new ErrorResponse(
                HttpStatusCode.Conflict,
                "CONFLICT",
                exception.Message),
            
            _ => new ErrorResponse(
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later.")
        };

        context.Response.StatusCode = (int)response.StatusCode;

        var result = new ApiResponse
        {
            Success = false,
            Error = new ErrorDetail
            {
                Code = response.Code,
                Message = response.Message,
                Details = new List<FieldError>
                {
                    new() { Field = "requestId", Message = requestId }
                }
            }
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, options));
    }
}

public record ErrorResponse(HttpStatusCode StatusCode, string Code, string Message);

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
