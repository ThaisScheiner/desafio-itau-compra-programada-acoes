using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Observability;

public sealed class TelemetryHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public TelemetryHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var activity = Activity.Current;

            if (activity is not null)
            {
                context.Response.Headers["X-Trace-Id"] = activity.TraceId.ToString();
                context.Response.Headers["X-Span-Id"] = activity.SpanId.ToString();
            }

            if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
            {
                context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}