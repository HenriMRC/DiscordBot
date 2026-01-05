using System.Collections.Generic;
using System;
using System.Dynamic;
using System.Text.Json;

namespace DiscordBot;

internal static class JsonExtension
{
    internal static ExpandoObject ToExpandoObject(this JsonDocument json)
    {
        return ParseElement(json.RootElement);
    }
    private static ExpandoObject ParseElement(JsonElement element)
    {
        ExpandoObject expando = new();
        IDictionary<string, object?> dictionary = expando;

        foreach (var property in element.EnumerateObject())
            dictionary[property.Name] = ParseValue(property.Value);

        return expando;
    }

    private static object? ParseValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ParseElement(element),
            JsonValueKind.Array => ParseArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long value) ? value : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new NotSupportedException($"Unsupported JsonValueKind: {element.ValueKind}"),
        };
    }

    private static List<object?> ParseArray(JsonElement element)
    {
        List<object?> list = new();

        foreach (var item in element.EnumerateArray())
            list.Add(ParseValue(item));

        return list;
    }
}
