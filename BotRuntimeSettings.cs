namespace discordbot;

internal sealed class BotRuntimeSettings
{
    public BotRuntimeSettings(string token)
    {
        Token = token;
    }

    public string Token { get; }
}
