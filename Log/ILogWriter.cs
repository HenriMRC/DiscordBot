namespace DiscordBot.Log;

public interface ILogWriter
{
    void Write(Logger.Message message);
}
