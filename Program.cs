using Discord;
using Discord.WebSocket;
using discordbot.log;
using discordbot.services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace discordbot;

internal class Program
{
    private const string ChannelName = "bot-cambio";
    private static readonly CancellationTokenSource ShutdownCts = new();

    static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        Logger logger = new(LogSeverity.Debug,
#if DEBUG
            new ConsoleWriter(),
#endif
            new FileWriter());

        string? token = ReadToken(args);
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.Log(LogSeverity.Critical, "(App | Initialization): No argument or config file.");
            logger.Dispose();
            return;
        }

        DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.All };
        DiscordSocketClient client = new(socketConfig);
        AppState state = new();
        IConfigStore configStore = new FileConfigStore(new JsonHandler());
        IRateProvider rateProvider = new WiseRateProvider();
        INotificationService notificationService = new DiscordNotificationService(ChannelName, logger, configStore);
        ICommandHandler commandHandler = new DiscordCommandHandler(logger, configStore);

        await using BotHost host = new(client, logger, state, configStore, rateProvider, notificationService, commandHandler);
        await host.RunAsync(token, ShutdownCts.Token);
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

    private static void OnExit(object? sender, EventArgs e)
    {
        ShutdownCts.Cancel();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.Error.WriteLine($"Unhandled exception: {e.ExceptionObject}");
        ShutdownCts.Cancel();
    }
}
