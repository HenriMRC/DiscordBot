using System.Collections.Generic;

namespace discordbot;

internal sealed class AppState
{
    private readonly object _lock = new();
    private readonly HashSet<ulong> _greetedGuilds = [];
    private decimal _lastRate = -1m;

    public decimal GetLastRate()
    {
        lock (_lock)
        {
            return _lastRate;
        }
    }

    public bool TryUpdateLastRate(decimal rate)
    {
        lock (_lock)
        {
            if (_lastRate == rate)
            {
                return false;
            }

            _lastRate = rate;
            return true;
        }
    }

    public bool MarkGuildAsGreeted(ulong guildId)
    {
        lock (_lock)
        {
            return _greetedGuilds.Add(guildId);
        }
    }
}
