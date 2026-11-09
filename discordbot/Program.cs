using Discord;
using Discord.WebSocket;
using discordbot.hosting;
using discordbot.logging;
using discordbot.runtime;
using discordbot.services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace discordbot;

internal class Program
{
    static async Task Main(string[] args)
    {
        ParsedArgs parsedArgs = ParseArgs(args);
        string? token = ResolveToken(parsedArgs);
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.Error.WriteLine("No Discord bot token. Pass --token, set DISCORD_BOT_TOKEN, or create discordbot.token.");
            return;
        }

        IHost host = Host
            .CreateDefaultBuilder(args)
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                LoggingConfiguration.Configure(loggerConfiguration, parsedArgs.LoggingLevelSwitch);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(new BotRuntimeSettings(token));

                {
                    DiscordSocketConfig config = new()
                    {
                        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
                    };
                    services.AddSingleton(config);
                }

                services.AddSingleton<DiscordSocketClient>();

                services.AddSingleton<AppState>();
                services.AddSingleton<IConfigStore, FileConfigStore>();
                services.AddSingleton<IRateProvider, WiseRateProvider>();
                services.AddSingleton<INotificationService, DiscordNotificationService>();
                services.AddSingleton<ICommandHandler, DiscordCommandHandler>();
                services.AddSingleton<BotHost>();
                services.AddHostedService<BotHostedService>();
            })
            .Build();

        try
        {
            await host.RunAsync();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal static ParsedArgs ParseArgs(string[] args)
    {
        string? token = null;
        LogEventLevel minimumLevel = LogEventLevel.Information;

        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--token", StringComparison.OrdinalIgnoreCase))
            {
                token = ReadOptionValue(args, ref i);
                continue;
            }

            if (string.Equals(args[i], "--log-level", StringComparison.OrdinalIgnoreCase))
            {
                string? logLevel = ReadOptionValue(args, ref i);
                minimumLevel = LoggingConfiguration.GetMinimumLevel(logLevel);
            }
        }

        return new ParsedArgs(token, new LoggingLevelSwitch(minimumLevel));
    }

    internal static string? ResolveToken(ParsedArgs parsedArgs)
    {
        return FirstNonWhiteSpace(
            parsedArgs.Token,
            Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"),
            ReadTokenFile());
    }

    private static string? ReadOptionValue(string[] args, ref int index)
    {
        int valueIndex = index + 1;
        if (valueIndex >= args.Length || args[valueIndex].StartsWith("--", StringComparison.Ordinal))
        {
            return null;
        }

        index = valueIndex;
        return args[valueIndex];
    }

    private static string? ReadTokenFile()
    {
        FileInfo configFileInfo = new("./discordbot.token");
        if (!configFileInfo.Exists)
        {
            return null;
        }

        using StreamReader file = new(configFileInfo.OpenRead());
        return file.ReadToEnd();
    }

    private static string? FirstNonWhiteSpace(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}

internal readonly struct ParsedArgs
{
    public ParsedArgs(string? token, LoggingLevelSwitch loggingLevelSwitch)
    {
        Token = token;
        LoggingLevelSwitch = loggingLevelSwitch;
    }

    public string? Token { get; }

    public LoggingLevelSwitch LoggingLevelSwitch { get; }
}
