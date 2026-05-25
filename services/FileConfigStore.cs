using discordbot.models;

namespace discordbot.services;

internal sealed class FileConfigStore : IConfigStore
{
    private readonly JsonHandler _jsonHandler;

    public FileConfigStore(JsonHandler jsonHandler)
    {
        _jsonHandler = jsonHandler;
    }

    public Config Current { get; private set; } = new();

    public void Load()
    {
        Current = _jsonHandler.ReadConfigFile() ?? new Config();
    }

    public void Save()
    {
        _jsonHandler.WriteConfigToFile(Current);
    }

    public Range GetOrCreateRange(ulong guildId, decimal defaultMin, decimal defaultMax)
    {
        if (!Current.Channels.TryGetValue(guildId, out Range? range))
        {
            range = new Range(defaultMin, defaultMax);
            Current.Channels[guildId] = range;
        }

        return range;
    }
}
