using System;
using System.Text.Json.Serialization;

namespace DiscordBot.Models;

public class Config
{
    [JsonPropertyName("token")]
    public readonly string Token;

    [JsonConstructor]
    public Config(string? token)
    {
        if (token == null)
            throw new NullReferenceException(nameof(token));
        Token = token;
    }
}
