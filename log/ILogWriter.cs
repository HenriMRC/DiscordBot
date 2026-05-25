using System;

namespace discordbot.log;

public interface ILogWriter : IDisposable
{
    void Write(Logger.Message message);
}
