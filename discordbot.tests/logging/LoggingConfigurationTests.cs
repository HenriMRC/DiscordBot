using discordbot.logging;
using Serilog.Events;

namespace DiscordBot.Tests;

public class LoggingConfigurationTests
{
    [Test]
    public void GetMinimumLevel_WhenArgumentMissing_DefaultsToInformation()
    {
        LogEventLevel level = LoggingConfiguration.GetMinimumLevel(null);

        Assert.That(level, Is.EqualTo(LogEventLevel.Information));
    }

    [Test]
    public void GetMinimumLevel_WhenArgumentProvided_ReadsConfiguredLevel()
    {
        LogEventLevel level = LoggingConfiguration.GetMinimumLevel("debug");

        Assert.That(level, Is.EqualTo(LogEventLevel.Debug));
    }

    [Test]
    public void GetMinimumLevel_WhenArgumentUsesAlias_ReadsConfiguredLevel()
    {
        LogEventLevel level = LoggingConfiguration.GetMinimumLevel("info");

        Assert.That(level, Is.EqualTo(LogEventLevel.Information));
    }

    [Test]
    public void GetMinimumLevel_WhenArgumentIsUnknown_DefaultsToInformation()
    {
        LogEventLevel level = LoggingConfiguration.GetMinimumLevel("chatty");

        Assert.That(level, Is.EqualTo(LogEventLevel.Information));
    }
}
