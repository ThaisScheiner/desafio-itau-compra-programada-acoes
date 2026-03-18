using System.Diagnostics;

namespace MotorCompraService.Api.Infrastructure.Observability;

public static class LogScopeHelper
{
    public static Dictionary<string, object?> CreateTraceScope()
    {
        var activity = Activity.Current;

        return new Dictionary<string, object?>
        {
            ["service_name"] = Telemetry.ServiceName,
            ["trace_id"] = activity?.TraceId.ToString(),
            ["span_id"] = activity?.SpanId.ToString()
        };
    }
}