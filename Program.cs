using Discord;
using Discord.WebSocket;
using discordbot.hosting;
using discordbot.logging;
using discordbot.runtime;
using discordbot.services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace discordbot;

internal class Program
{
    static async Task Main(string[] args)
    {
        string? token = ReadToken(args);
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.Error.WriteLine("No argument or config file.");
            return;
        }

        const string outputTemplate =
            "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}] {Level:u3} [{SourceContext}] {Scope}{Message:lj}{NewLine}{Exception}";

        IHost host = Host
            .CreateDefaultBuilder(args)
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
#if DEBUG
                    .MinimumLevel.Debug()
#else
                    .MinimumLevel.Information()
#endif
                    .Enrich.FromLogContext()
                    .Enrich.With<SourceContextClassNameEnricher>()
                    .WriteTo.Console(outputTemplate: outputTemplate)
#if LOG_TO_FILE
                    .WriteTo.File(
                        path: "./Logs/log.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 20,
                        fileSizeLimitBytes: 1024 * 1024,
                        rollOnFileSizeLimit: true,
                        outputTemplate: outputTemplate)
#endif
                    ;
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(new BotRuntimeSettings(token));
                services.AddSingleton(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });
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

    private static string? ReadToken(string[] args)
    {
        if (args.Length > 0)
        {
            return args[0];
        }

        FileInfo configFileInfo = new("./discordbot.token");
        if (!configFileInfo.Exists)
        {
            return null;
        }

        using StreamReader file = new(configFileInfo.OpenRead());
        return file.ReadToEnd();
    }
}
