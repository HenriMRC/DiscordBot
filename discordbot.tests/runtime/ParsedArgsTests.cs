using discordbot;
using Serilog.Events;

namespace DiscordBot.Tests;

public class ParsedArgsTests
{
    [Test]
    public void ParseArgs_WhenTokenArgumentProvided_ReadsToken()
    {
        ParsedArgs args = Program.ParseArgs(["--token", "abc"]);

        Assert.That(args.Token, Is.EqualTo("abc"));
    }

    [Test]
    public void ParseArgs_WhenTokenIsPositional_DoesNotReadToken()
    {
        ParsedArgs args = Program.ParseArgs(["abc"]);

        Assert.That(args.Token, Is.Null);
    }

    [Test]
    public void ParseArgs_WhenLogLevelArgumentProvided_ConfiguresLoggingLevelSwitch()
    {
        ParsedArgs args = Program.ParseArgs(["--token", "abc", "--log-level", "debug"]);

        Assert.That(args.LoggingLevelSwitch.MinimumLevel, Is.EqualTo(LogEventLevel.Debug));
    }

    [Test]
    public void ParseArgs_WhenLogLevelArgumentMissing_DefaultsToInformation()
    {
        ParsedArgs args = Program.ParseArgs(["--token", "abc"]);

        Assert.That(args.LoggingLevelSwitch.MinimumLevel, Is.EqualTo(LogEventLevel.Information));
    }
}
