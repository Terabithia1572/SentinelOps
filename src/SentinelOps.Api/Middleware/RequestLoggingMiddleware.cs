using System.Diagnostics;

namespace SentinelOps.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                method, path, statusCode, elapsedMs);
        }
    }
}
