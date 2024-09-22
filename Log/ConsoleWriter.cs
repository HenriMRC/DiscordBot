using Discord;
using System;

namespace DiscordBot.Log;

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