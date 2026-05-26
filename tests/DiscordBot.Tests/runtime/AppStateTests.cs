using discordbot.runtime;

namespace DiscordBot.Tests;

public class AppStateTests
{
    [Test]
    public void GetLastRate_DefaultValue_IsNegativeOne()
    {
        AppState state = new();

        Assert.That(state.GetLastRate(), Is.EqualTo(-1m));
    }

    [Test]
    public void TryUpdateLastRate_NewValue_UpdatesAndReturnsTrue()
    {
        AppState state = new();

        bool changed = state.TryUpdateLastRate(5.25m);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(state.GetLastRate(), Is.EqualTo(5.25m));
        });
    }

    [Test]
    public void TryUpdateLastRate_SameValue_ReturnsFalse()
    {
        AppState state = new();
        state.TryUpdateLastRate(5.25m);

        bool changed = state.TryUpdateLastRate(5.25m);

        Assert.That(changed, Is.False);
    }

    [Test]
    public void MarkGuildAsGreeted_FirstTimeTrue_SecondTimeFalse()
    {
        AppState state = new();
        const ulong guildId = 123ul;

        bool first = state.MarkGuildAsGreeted(guildId);
        bool second = state.MarkGuildAsGreeted(guildId);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        });
    }
}
