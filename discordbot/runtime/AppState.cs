using System.Collections.Generic;
using System.Threading;

namespace discordbot.runtime;

internal sealed class AppState
{
    private readonly Lock _lock = new();
    private readonly HashSet<ulong> _greetedGuilds = [];
    private decimal _lastRate = -1m;

    public decimal GetLastRate()
    {
        using Lock.Scope scope = _lock.EnterScope();
        return _lastRate;
    }

    public bool TryUpdateLastRate(decimal rate)
    {
        using Lock.Scope scope = _lock.EnterScope();
        if (_lastRate == rate)
            return false;

        _lastRate = rate;
        return true;
    }

    public bool MarkGuildAsGreeted(ulong guildId)
    {
        using Lock.Scope scope = _lock.EnterScope();
        return _greetedGuilds.Add(guildId);
    }
}
