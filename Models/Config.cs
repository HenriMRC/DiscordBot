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
            //JsonObject.Parse(ref reader,)

            // Deserialize into the raw shape
            Dictionary<ulong, Range> dictionary = [];
            JsonNode raw = JsonSerializer.Deserialize<JsonNode>(ref reader, options);
            foreach (KeyValuePair<string, JsonNode?> node in raw["channels"].AsObject())
            {
                ulong key = ulong.Parse(node.Key);
                Range value;
                if (node.Value == null)
                    value = new Range(decimal.MinValue, decimal.MaxValue);
                else
                {
                    decimal min = node.Value["min"].Deserialize<decimal>();
                    decimal max = node.Value["max"].Deserialize<decimal>();
                    value = new Range(min, max);
                }
                dictionary.Add(key, value);
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

    internal bool Contains(decimal value) => Comparer(value) == 0;
    internal int Comparer(decimal value) => value < Minimum ? -1 : value > Maximum ? 1 : 0;
}
