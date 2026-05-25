using Discord;
using Discord.WebSocket;
using discordbot.log;
using discordbot.services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            Console.Error.WriteLine("(App | Initialization): No argument or config file.");
            return;
        }

        IHost host = Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton(new BotRuntimeSettings(token));
                services.AddSingleton(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });
                services.AddSingleton<DiscordSocketClient>(provider =>
                    new DiscordSocketClient(provider.GetRequiredService<DiscordSocketConfig>()));

                services.AddSingleton<Logger>(_ =>
                    new Logger(
                        LogSeverity.Debug,
#if DEBUG
                        new ConsoleWriter(),
#endif
                        new FileWriter()));

                services.AddSingleton<AppState>();
                services.AddSingleton<IConfigStore, FileConfigStore>();
                services.AddSingleton<IRateProvider, WiseRateProvider>();
                services.AddSingleton<INotificationService, DiscordNotificationService>();
                services.AddSingleton<ICommandHandler, DiscordCommandHandler>();
                services.AddSingleton<BotHost>();
                services.AddHostedService<BotHostedService>();
            })
            .Build();

        await host.RunAsync();
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
