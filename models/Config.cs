using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace discordbot.models;

public class Config(Dictionary<ulong, Range>? channels)
{
    public Config() : this(null) { }

    [JsonPropertyName("channels"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public Dictionary<ulong, Range> Channels { get; set; } = channels ?? [];
}