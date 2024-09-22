using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.Log;

public class Logger
{
    private readonly LogSeverity _severity;
    private readonly Queue<Message> _messageQueue;
    private readonly ILogWriter[] _writers;
    private Task? _logTask;
    private readonly object _lock;

    internal Logger(LogSeverity severity, params ILogWriter[] writers)
    {
        _severity = severity;
        _messageQueue = new();
        _writers = [.. writers];
        _logTask = null;
        _lock = new();
    }

    internal void Log(LogSeverity severity, string message)
    {
        if (_severity < severity)
            return;

        DateTime now = DateTime.Now;

        lock (_lock)
        {
            _messageQueue.Enqueue(new(now, severity, message));
            _logTask ??= Task.Run(ConsumeMessageQueue);
        }
    }

    private void ConsumeMessageQueue()
    {
        while (true)
        {
            Message? message;
            lock (_lock)
            {
                if (!_messageQueue.TryDequeue(out message))
                {
                    _logTask = null;
                    return;
                }
            }

            try
            {
                Task[] tasks = new Task[_writers.Length];
                for (int i = 0; i < _writers.Length; i++)
                {
                    var writer = _writers[i];
                    tasks[i] = Task.Run(() => writer.Write(message));
                }

                Task.WaitAll(tasks);
            }
            catch (Exception exception)
            {
                Log(LogSeverity.Error, $"Exception: {exception.Message}\n{exception.StackTrace}");
            }
        }
    }

    public record Message(DateTime Timestamp, LogSeverity Severity, string LogMessage)
    {
        public readonly DateTime Timestamp = Timestamp;
        public readonly LogSeverity Severity = Severity;
        public readonly string LogMessage = LogMessage;

        public override string ToString()
        {
            return $"[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}] {Severity.ToString()[..4].ToUpper()}: {LogMessage}";
        }
    }
}