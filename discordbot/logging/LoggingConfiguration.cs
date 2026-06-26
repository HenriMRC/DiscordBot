using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;

namespace discordbot.logging;

internal static class LoggingConfiguration
{
    private const string ApplicationDirectoryName = "DiscordBot";
    private const string OutputTemplate =
        "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}] {Level:u3} [{SourceContext}] {Scope}{Message:lj}{NewLine}{Exception}";

    public static void Configure(LoggerConfiguration loggerConfiguration, LoggingLevelSwitch loggingLevelSwitch)
    {
        loggerConfiguration
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .Enrich.FromLogContext()
            .Enrich.With<SourceContextClassNameEnricher>()
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.File(
                path: GetLogFilePath(),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 20,
                fileSizeLimitBytes: 1024 * 1024,
                rollOnFileSizeLimit: true,
                outputTemplate: OutputTemplate);
    }

    internal static LogEventLevel GetMinimumLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LogEventLevel.Information;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "verbose" or "trace" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" or "critical" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }

    private static string GetLogFilePath()
    {
        string logDirectory = GetLogDirectory();
        Directory.CreateDirectory(logDirectory);
        return Path.Combine(logDirectory, "log.txt");
    }

    private static string GetLogDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationDirectoryName, "Logs");
        }

        if (OperatingSystem.IsLinux())
        {
            string? stateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
            if (!string.IsNullOrWhiteSpace(stateHome))
            {
                return Path.Combine(stateHome, ApplicationDirectoryName.ToLowerInvariant(), "logs");
            }

            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(homeDirectory))
            {
                homeDirectory = GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            return Path.Combine(homeDirectory, ".local", "state", ApplicationDirectoryName.ToLowerInvariant(), "logs");
        }

        return Path.Combine(GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationDirectoryName, "Logs");
    }

    private static string GetFolderPath(Environment.SpecialFolder specialFolder)
    {
        string folderPath = Environment.GetFolderPath(specialFolder);
        return string.IsNullOrWhiteSpace(folderPath) ? AppContext.BaseDirectory : folderPath;
    }
}
