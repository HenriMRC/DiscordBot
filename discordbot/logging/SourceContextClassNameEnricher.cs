using Serilog.Core;
using Serilog.Events;
using System;

namespace discordbot.logging;

internal sealed class SourceContextClassNameEnricher : ILogEventEnricher
{
    private const string SourceContextPropertyName = "SourceContext";
    private const string UnknownClassName = "Unknown";
    private const int FixedLength = 12;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string className = UnknownClassName;

        if (logEvent.Properties.TryGetValue(SourceContextPropertyName, out LogEventPropertyValue? sourceContextValue)
            && sourceContextValue is ScalarValue { Value: string sourceContext }
            && !string.IsNullOrWhiteSpace(sourceContext))
        {
            int lastDot = sourceContext.LastIndexOf('.');
            className = lastDot >= 0 ? sourceContext[(lastDot + 1)..] : sourceContext;
        }

        if (className.Length > FixedLength)
        {
            className = className[..FixedLength];
        }
        else if (className.Length < FixedLength)
        {
            className = className.PadRight(FixedLength, ' ');
        }

        LogEventProperty property = propertyFactory.CreateProperty(SourceContextPropertyName, className);
        logEvent.AddOrUpdateProperty(property);
    }
}
