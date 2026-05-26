using discordbot.services;

namespace DiscordBot.Tests;

public class FileConfigStoreTests
{
    private string _tempDirectory = null!;
    private string _configPath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"DiscordBot.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _configPath = Path.Combine(_tempDirectory, "Config.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void Load_WhenFileDoesNotExist_UsesDefaultConfig()
    {
        FileConfigStore store = new(_configPath);

        store.Load();

        Assert.Multiple(() =>
        {
            Assert.That(store.ChannelName, Is.EqualTo("bot-cambio"));
            Assert.That(store.Current.Channels, Is.Empty);
        });
    }

    [Test]
    public void Load_WhenChannelNameIsBlank_NormalizesToDefault()
    {
        const string json = """
            {
              "channelName": "   ",
              "channels": {}
            }
            """;
        File.WriteAllText(_configPath, json);
        FileConfigStore store = new(_configPath);

        store.Load();

        Assert.That(store.ChannelName, Is.EqualTo("bot-cambio"));
    }

    [Test]
    public void GetOrCreateRange_WhenMissing_CreatesAndReturnsNewRange()
    {
        FileConfigStore store = new(_configPath);
        store.Load();

        var range = store.GetOrCreateRange(10ul, 1.1m, 2.2m);

        Assert.Multiple(() =>
        {
            Assert.That(range.Minimum, Is.EqualTo(1.1m));
            Assert.That(range.Maximum, Is.EqualTo(2.2m));
            Assert.That(store.Current.Channels, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void GetOrCreateRange_WhenExists_ReturnsExistingRange()
    {
        FileConfigStore store = new(_configPath);
        store.Load();
        var first = store.GetOrCreateRange(10ul, 1m, 2m);

        var second = store.GetOrCreateRange(10ul, 3m, 4m);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(second.Minimum, Is.EqualTo(1m));
            Assert.That(second.Maximum, Is.EqualTo(2m));
        });
    }

    [Test]
    public void Save_ThenLoad_PersistsConfigAndRanges()
    {
        FileConfigStore store = new(_configPath);
        store.Load();
        store.Current.ChannelName = "alerts";
        var range = store.GetOrCreateRange(123ul, 5m, 6m);
        range.Minimum = 5.5m;
        range.Maximum = 6.5m;
        store.Save();

        FileConfigStore reloaded = new(_configPath);
        reloaded.Load();

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.ChannelName, Is.EqualTo("alerts"));
            Assert.That(reloaded.Current.Channels, Has.Count.EqualTo(1));
            Assert.That(reloaded.Current.Channels[123ul].Minimum, Is.EqualTo(5.5m));
            Assert.That(reloaded.Current.Channels[123ul].Maximum, Is.EqualTo(6.5m));
        });
    }
}
