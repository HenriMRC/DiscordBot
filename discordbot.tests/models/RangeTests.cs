using Range = discordbot.models.Range;

namespace DiscordBot.Tests;

public class RangeTests
{
    [Test]
    public void Constructor_DefaultValues_CoversFullDecimalRange()
    {
        Range range = new();

        Assert.Multiple(() =>
        {
            Assert.That(range.Minimum, Is.EqualTo(decimal.MinValue));
            Assert.That(range.Maximum, Is.EqualTo(decimal.MaxValue));
        });
    }

    [Test]
    public void Compare_BelowMinimum_ReturnsMinusOne()
    {
        Range range = new(10m, 20m);

        Assert.That(range.Compare(9.99m), Is.EqualTo(-1));
    }

    [Test]
    public void Compare_AboveMaximum_ReturnsOne()
    {
        Range range = new(10m, 20m);

        Assert.That(range.Compare(20.01m), Is.EqualTo(1));
    }

    [Test]
    public void Compare_WithinRange_ReturnsZero()
    {
        Range range = new(10m, 20m);

        Assert.That(range.Compare(15m), Is.EqualTo(0));
    }

    [Test]
    public void Contains_WithinRange_ReturnsTrue()
    {
        Range range = new(10m, 20m);

        Assert.That(range.Contains(15m), Is.True);
    }

    [Test]
    public void Contains_OutsideRange_ReturnsFalse()
    {
        Range range = new(10m, 20m);

        Assert.That(range.Contains(25m), Is.False);
    }
}
