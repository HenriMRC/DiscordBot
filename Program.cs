using Discord;
using Discord.WebSocket;
using discordbot.logging;
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
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext()
                    .Enrich.With<SourceContextClassNameEnricher>()
#if DEBUG
                    .WriteTo.Console(outputTemplate: outputTemplate)
#endif
                    .WriteTo.File(
                        path: "./Logs/log.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 20,
                        fileSizeLimitBytes: 1024 * 1024,
                        rollOnFileSizeLimit: true,
                        outputTemplate: outputTemplate);
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
