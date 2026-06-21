using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace GestionAlquileres.API.Configuration;

/// <summary>
/// Stamps every log event with the ambient Activity's TraceId/SpanId so logs can be correlated with
/// the OpenTelemetry traces emitted for the same request (audit A-9). No-op when there is no current
/// Activity (e.g. startup logs outside a request/job scope).
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
    }
}
