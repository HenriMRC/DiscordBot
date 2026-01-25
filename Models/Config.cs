using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DiscordBot.Models;

[JsonConverter(typeof(JsonConverter))]
public class Config(Dictionary<ulong, Range>? channels)
{
    [JsonPropertyName("channels"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public readonly Dictionary<ulong, Range> Channels = channels ?? [];

    [JsonConstructor()]
    public Config() : this(null) { }

    public sealed class JsonConverter : JsonConverter<Config>
    {
        public override Config Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Dictionary<ulong, Range> dictionary = [];
            JsonNode? raw = JsonSerializer.Deserialize<JsonNode>(ref reader, options);
            if (raw != null)
            {
                JsonObject array = raw.AsObject();
                foreach (KeyValuePair<string, JsonNode?> item in array)
                {
                    ulong key = ulong.Parse(item.Key);
                    Range value;
                    if (item.Value == null)
                        value = new Range(decimal.MinValue, decimal.MaxValue);
                    else
                    {
                        decimal min = item.Value["min"].Deserialize<decimal>();
                        decimal max = item.Value["max"].Deserialize<decimal>();
                        value = new Range(min, max);
                    }
                    dictionary.Add(key, value);
                }
            }

            return new(dictionary);
        }

        public override void Write(Utf8JsonWriter writer, Config value, JsonSerializerOptions options)
        {
            Dictionary<string, Range> raw = value.Channels.ToDictionary(p => p.Key.ToString(), p => p.Value);
            JsonSerializer.Serialize(writer, raw, options);
        }
    }
}

[method: JsonConstructor]
public class Range(decimal min, decimal max)
{
    [JsonPropertyName("min"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Minimum = min;

    [JsonPropertyName("max"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Maximum = max;

    internal bool Contains(decimal value) => Compare(value) == 0;
    internal int Compare(decimal value) => value < Minimum ? -1 : value > Maximum ? 1 : 0;
}
