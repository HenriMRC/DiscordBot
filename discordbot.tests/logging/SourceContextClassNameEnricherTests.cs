using discordbot.logging;
using Serilog.Core;
using Serilog.Events;

namespace DiscordBot.Tests;

public class SourceContextClassNameEnricherTests
{
    private readonly SourceContextClassNameEnricher _enricher = new();
    private readonly ILogEventPropertyFactory _propertyFactory = new TestLogEventPropertyFactory();

    [Test]
    public void Enrich_WhenSourceContextMissing_UsesUnknownWithPadding()
    {
        LogEvent logEvent = CreateLogEvent();

        _enricher.Enrich(logEvent, _propertyFactory);

        string value = GetSourceContext(logEvent);
        Assert.That(value, Is.EqualTo("Unknown     "));
        Assert.That(value.Length, Is.EqualTo(12));
    }

    [Test]
    public void Enrich_WhenNamespaceExists_UsesOnlyClassName()
    {
        LogEvent logEvent = CreateLogEvent("discordbot.services.DiscordNotificationService");

        _enricher.Enrich(logEvent, _propertyFactory);

        Assert.That(GetSourceContext(logEvent), Is.EqualTo("DiscordNotif"));
    }

    [Test]
    public void Enrich_WhenClassNameShort_PadsToFixedLength()
    {
        LogEvent logEvent = CreateLogEvent("discordbot.hosting.BotHost");

        _enricher.Enrich(logEvent, _propertyFactory);

        string value = GetSourceContext(logEvent);
        Assert.That(value, Is.EqualTo("BotHost     "));
        Assert.That(value.Length, Is.EqualTo(12));
    }

    [Test]
    public void Enrich_WhenClassNameLong_TruncatesToFixedLength()
    {
        LogEvent logEvent = CreateLogEvent("VeryLongClassNameForTesting");

        _enricher.Enrich(logEvent, _propertyFactory);

        string value = GetSourceContext(logEvent);
        Assert.That(value, Is.EqualTo("VeryLongClas"));
        Assert.That(value.Length, Is.EqualTo(12));
    }

    private static LogEvent CreateLogEvent(string? sourceContext = null)
    {
        List<LogEventProperty> properties = [];
        if (sourceContext != null)
        {
            properties.Add(new LogEventProperty("SourceContext", new ScalarValue(sourceContext)));
        }

        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            properties);
    }

    private static string GetSourceContext(LogEvent logEvent)
    {
        return (string)((ScalarValue)logEvent.Properties["SourceContext"]).Value!;
    }

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
