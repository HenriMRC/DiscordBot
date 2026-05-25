using discordbot.models;

namespace discordbot.services;

internal interface IConfigStore
{
    Config Current { get; }
    string ChannelName { get; }
    void Load();
    void Save();
    Range GetOrCreateRange(ulong guildId, decimal defaultMin, decimal defaultMax);
}
