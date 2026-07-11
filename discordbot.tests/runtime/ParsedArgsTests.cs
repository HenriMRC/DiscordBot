using discordbot;
using Serilog.Events;

namespace DiscordBot.Tests;

public class ParsedArgsTests
{
    private string? _originalDiscordToken;

    [SetUp]
    public void SetUp()
    {
        _originalDiscordToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", null);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", _originalDiscordToken);
    }

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

    [Test]
    public void ResolveToken_WhenArgumentProvided_UsesArgumentBeforeEnvironment()
    {
        Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", "environment-token");
        ParsedArgs args = Program.ParseArgs(["--token", "argument-token"]);

        string? token = Program.ResolveToken(args);

        Assert.That(token, Is.EqualTo("argument-token"));
    }

    [Test]
    public void ResolveToken_WhenEnvironmentProvided_UsesEnvironment()
    {
        Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", "environment-token");
        ParsedArgs args = Program.ParseArgs([]);

        string? token = Program.ResolveToken(args);

        Assert.That(token, Is.EqualTo("environment-token"));
    }

    [Test]
    public void ResolveToken_WhenValueHasWhitespace_TrimsToken()
    {
        ParsedArgs args = Program.ParseArgs(["--token", "  argument-token  "]);

        string? token = Program.ResolveToken(args);

        Assert.That(token, Is.EqualTo("argument-token"));
    }
}
