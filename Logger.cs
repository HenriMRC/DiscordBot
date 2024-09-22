using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot;

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

            Task[] tasks = new Task[_writers.Length];
            for (int i = 0; i < _writers.Length; i++)
            {
                var writer = _writers[i];
                tasks[i] = Task.Run(() => writer.Write(message));
            }

            Task.WaitAll(tasks);
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

public interface ILogWriter
{
    void Write(Logger.Message message);
}

internal class ConsoleWriter : ILogWriter
{
    private const ConsoleColor DEFAULT_COLOR = ConsoleColor.Green;

    internal ConsoleWriter()
    {
        Console.ForegroundColor = DEFAULT_COLOR;
    }

    public void Write(Logger.Message message)
    {
        switch (message.Severity)
        {
            //
            // Summary:
            //     Logs that contain the most severe level of error. This type of error indicate
            //     that immediate attention may be required.
            case LogSeverity.Critical:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            //
            // Summary:
            //     Logs that highlight when the flow of execution is stopped due to a failure.
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                break;
            //
            // Summary:
            //     Logs that highlight an abnormal activity in the flow of execution.
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            //
            // Summary:
            //     Logs that track the general flow of the application.
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            //
            // Summary:
            //     Logs that are used for interactive investigation during development.
            case LogSeverity.Verbose:
            //
            // Summary:
            //     Logs that contain the most detailed messages.
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }

        Console.WriteLine(message.ToString());

        Console.ForegroundColor = DEFAULT_COLOR;
    }
}