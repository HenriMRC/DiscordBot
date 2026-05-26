using discordbot.models;
using Range = discordbot.models.Range;

namespace DiscordBot.Tests;

public class ConfigTests
{
    [Test]
    public void DefaultConstructor_UsesExpectedDefaults()
    {
        Config config = new();

        Assert.Multiple(() =>
        {
            Assert.That(config.ChannelName, Is.EqualTo("bot-cambio"));
            Assert.That(config.Channels, Is.Not.Null);
            Assert.That(config.Channels, Is.Empty);
        });
    }

    [Test]
    public void Constructor_BlankChannelName_FallsBackToDefault()
    {
        Config config = new("   ", null);

        Assert.That(config.ChannelName, Is.EqualTo("bot-cambio"));
    }

    [Test]
    public void Constructor_WithValues_UsesProvidedData()
    {
        Dictionary<ulong, Range> channels = new()
        {
            [42ul] = new Range(1m, 2m)
        };

        Config config = new("custom-channel", channels);

        Assert.Multiple(() =>
        {
            Assert.That(config.ChannelName, Is.EqualTo("custom-channel"));
            Assert.That(config.Channels, Has.Count.EqualTo(1));
            Assert.That(config.Channels[42ul].Minimum, Is.EqualTo(1m));
            Assert.That(config.Channels[42ul].Maximum, Is.EqualTo(2m));
        });
    }
}
