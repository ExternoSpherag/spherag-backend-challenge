using System.Diagnostics;

namespace SpheragBackendChallenge.Api.Middleware;

public sealed class HttpLoggingMiddleware(
    RequestDelegate next,
    ILogger<HttpLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var traceId = context.TraceIdentifier;

        logger.LogInformation(
            "Incoming request {Method} {Path}{QueryString} TraceId={TraceId}",
            method,
            path,
            queryString,
            traceId);

        try
        {
            await next(context);
            stopwatch.Stop();

            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    "Request failed {Method} {Path} StatusCode={StatusCode} DurationMs={DurationMs} TraceId={TraceId}",
                    method,
                    path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    traceId);

                return;
            }

            logger.LogInformation(
                "Outgoing response {Method} {Path} StatusCode={StatusCode} DurationMs={DurationMs} TraceId={TraceId}",
                method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                traceId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path} DurationMs={DurationMs} TraceId={TraceId}",
                method,
                path,
                stopwatch.ElapsedMilliseconds,
                traceId);

            throw;
        }
    }
}
