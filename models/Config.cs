using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace discordbot.models;

public class Config
{
    public Config() : this("bot-cambio", null) { }

    [JsonConstructor]
    public Config(string? channelName, Dictionary<ulong, Range>? channels)
    {
        ChannelName = string.IsNullOrWhiteSpace(channelName) ? "bot-cambio" : channelName;
        Channels = channels ?? [];
    }

    [JsonPropertyName("channelName"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string ChannelName { get; set; }

    [JsonPropertyName("channels"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public Dictionary<ulong, Range> Channels { get; set; }
}
