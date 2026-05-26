using discordbot.runtime;

namespace DiscordBot.Tests;

public class BotRuntimeSettingsTests
{
    [Test]
    public void Constructor_SetsToken()
    {
        BotRuntimeSettings settings = new("my-token");

        Assert.That(settings.Token, Is.EqualTo("my-token"));
    }
}
