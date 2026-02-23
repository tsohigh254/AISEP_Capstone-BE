namespace AISEP.WebAPI.Middlewares;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string RequestIdHeader = "X-Request-Id";

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[RequestIdHeader].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = requestId;
        context.Response.Headers[RequestIdHeader] = requestId;

        using (Serilog.Context.LogContext.PushProperty("RequestId", requestId))
        using (Serilog.Context.LogContext.PushProperty("UserId", context.User?.FindFirst("sub")?.Value))
        {
            await _next(context);
        }
    }
}

public static class RequestIdMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestIdMiddleware>();
    }
}
