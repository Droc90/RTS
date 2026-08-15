using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RTS.Web.Health;

public static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(
        HttpContext httpContext,
        HealthReport report)
    {
        httpContext.Response.ContentType =
            "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMilliseconds =
                report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMilliseconds =
                    entry.Value.Duration.TotalMilliseconds
            })
        };

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            response,
            cancellationToken:
                httpContext.RequestAborted);
    }
}