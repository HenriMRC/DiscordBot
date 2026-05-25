

using System.Text.Json.Serialization;

namespace discordbot.models;

public class Range(decimal min, decimal max)
{
    public Range() : this(decimal.MinValue, decimal.MaxValue) { }

    [JsonPropertyName("min"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Minimum { get; set; } = min;

    [JsonPropertyName("max"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Maximum { get; set; } = max;

    internal bool Contains(decimal value) => Compare(value) == 0;
    internal int Compare(decimal value) => value < Minimum ? -1 : value > Maximum ? 1 : 0;
}