using System.Text.Json.Serialization;

namespace DiscordBot;

[method: JsonConstructor]
public class Config(string token)
{
    [JsonPropertyName("token")]
    public readonly string Token = token;
}
