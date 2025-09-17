using System;

namespace DiscordBot.Log;

public interface ILogWriter : IDisposable
{
    void Write(Logger.Message message);
}
