using System.Diagnostics;

namespace Market.API.Middleware;

/// <summary>
/// Middleware that logs every HTTP request: method, path, status code and duration.
/// Slow requests are additionally logged as a warning and appended to Logs/slow-requests.log.
///
/// Request and response BODIES are intentionally NOT logged: they contain passwords,
/// access/refresh tokens and personal data, which must never end up in log files.
/// </summary>
public sealed class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger)
{
    private const int SlowRequestThresholdMs = 400; // 400 ms

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms",
                request.Method,
                request.Path,
                context.Response.StatusCode,
                elapsed);

            if (elapsed > SlowRequestThresholdMs)
            {
                logger.LogWarning("[SLOW REQUEST] {Method} {Path} took {Elapsed} ms", request.Method, request.Path, elapsed);

                // >>> Bugfix 27.10.2025: sigurno pisanje na disk (ne ruši response ako folder ne postoji)
                try
                {
                    var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                    Directory.CreateDirectory(logDir);
                    var logPath = Path.Combine(logDir, "slow-requests.log");
                    await File.AppendAllTextAsync(logPath, $"{DateTime.UtcNow:u} | {request.Method} {request.Path} | {elapsed} ms{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed writing slow-request log.");
                }
            }
        }
    }
}
