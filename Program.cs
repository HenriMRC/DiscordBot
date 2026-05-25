using Discord;
using Discord.WebSocket;
using discordbot.log;
using discordbot.models;
using discordbot.services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Range = discordbot.models.Range;

namespace discordbot;

internal class Program
{
    private const string CHANNEL_NAME = "bot-cambio";
    private static Task? _loop;

    private readonly static DiscordSocketClient _client;
    private readonly static HashSet<ulong> _greetedGuilds;
    private readonly static Logger _logger;
    private readonly static IConfigStore _configStore;
    private readonly static IRateProvider _rateProvider;
    private readonly static INotificationService _notificationService;
    private readonly static ICommandHandler _commandHandler;

    private static decimal _lastRate = -1;

    static Program()
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.All };
        _client = new DiscordSocketClient(socketConfig);
        _client.Ready += OnReady;
        _client.MessageReceived += MessageReceivedAsync;
        _client.Log += DiscordLog;
        _client.Connected += OnConnected;
        _client.Disconnected += OnDisconnected;
        _client.GuildAvailable += OnGuildAvailable;

        _greetedGuilds = [];

        _logger = new(LogSeverity.Debug,
#if DEBUG
            new ConsoleWriter(),
#endif
            new FileWriter());

        _configStore = new FileConfigStore(new JsonHandler());
        _rateProvider = new WiseRateProvider();
        _notificationService = new DiscordNotificationService(CHANNEL_NAME, _logger, _configStore);
        _commandHandler = new DiscordCommandHandler(_logger, _configStore);
    }

    static void Main(string[] args)
    {
        _configStore.Load();

        _logger.Log(LogSeverity.Info, "(App | Initialization): Initializing config.");
        string token;
        if (args.Length == 0)
        {
            FileInfo configFileInfo = new("./discordbot.token");
            if (configFileInfo.Exists)
            {
                using StreamReader file = new(configFileInfo.OpenRead());
                token = file.ReadToEnd();
            }
            else
            {
                _logger.Log(LogSeverity.Critical, "(App | Initialization): No argument or config file.");
                return;
            }
        }
        else
        {
            token = args[0];
        }

        _logger.Log(LogSeverity.Info, "(App | Initialization): Initializing bot.");

        Task task = _client.LoginAsync(TokenType.Bot, token);
        task.Wait();
        task = _client.StartAsync();
        task.Wait();

        Task.Delay(Timeout.Infinite).Wait();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.Log(LogSeverity.Critical, $"(App | OnUnhandledException): {e}");
    }

    private static void OnExit(object? sender, EventArgs e)
    {
        _logger.Log(LogSeverity.Info, "(App | OnExit): Exiting");
        if (_client != null)
        {
            const string message = "I am going to sleep.";
            Task[] tasks = new Task[_client.Guilds.Count];
            int count = 0;
            foreach (SocketGuild guild in _client.Guilds)
            {
                tasks[count] = _notificationService.NotifyGuildAsync(guild, message);
                count++;
            }
            Task.WaitAll(tasks);
        }
        _logger.Log(LogSeverity.Info, "(App | OnExit): Exited");
        _logger.Dispose();
    }

    private static async Task OnConnected()
    {
        await Task.Run(() => _logger.Log(LogSeverity.Info, $"(App | Connection): Bot ({_client.CurrentUser.Username}) connected. {LogBot()}"));
    }

    private static async Task OnDisconnected(Exception exception)
    {
        await Task.Run(() => _logger.Log(LogSeverity.Info, $"(App | Connection): Bot ({_client.CurrentUser.Username}) disconnected. {LogBot()}"));
    }

    private static async Task OnGuildAvailable(SocketGuild guild)
    {
        await Task.Run(() => _logger.Log(LogSeverity.Info, $"(App | Connection): Guild ({guild.Name}) connected."));
        Range channel = _configStore.GetOrCreateRange(guild.Id, 6.1m, 6.2m);

        string msg =
            $"""
            I am awake.

                Variables:
                 - Environment: {Environment.CurrentDirectory}
            - AppContext: {AppContext.BaseDirectory}

                Configuration:
                 - Lower bound: {channel.Minimum:n2}
            - Upper bound: {channel.Maximum:n2}
            """;

        if (_greetedGuilds.Add(guild.Id))
        {
            await _notificationService.NotifyGuildAsync(guild, msg);
        }

        _configStore.Save();
    }

    private static async Task OnReady()
    {
        await Task.Run(() => _logger.Log(LogSeverity.Info, $"(App | Bot): Bot ({_client.CurrentUser.Username}) ready. {LogBot()}"));
        _loop = Task.Run(Loop);
    }

    private static void Loop()
    {
        while (true)
        {
            if (_client.Guilds.Count > 0)
            {
                Task<decimal> result = _rateProvider.GetRateAsync();
                result.Wait();
                decimal rate = result.Result;

                if (rate != _lastRate)
                {
                    _lastRate = rate;
                    Task task = _notificationService.NotifyRateAsync(_client.Guilds, _lastRate);
                    task.Wait();
                }
            }

            DateTime now = DateTime.Now;

            const int minutes = 5;
            int hour = now.Hour;
            int minute = now.Minute;
            minute /= minutes;
            minute++;
            minute *= minutes;

            hour += minute / 60;
            minute %= 60;

            DateTime next = new(now.Year, now.Month, now.Day, hour, minute, 0);

            TimeSpan span = next - DateTime.Now;
            if (span.TotalMinutes < 0.5d)
            {
                span = TimeSpan.FromMinutes(0.5d);
            }

            Thread.Sleep(span);
        }
    }

    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        await _commandHandler.HandleAsync(message, _lastRate);
    }

    private static string LogBot()
    {
        return $"Bot status: [ {nameof(_client.Activity)}: {_client.Activity?.Name ?? "null"} | " +
               $"{nameof(_client.ConnectionState)}: {_client.ConnectionState} | {nameof(_client.LoginState)}: {_client.LoginState} | " +
               $"{nameof(_client.Status)}: {_client.Status} ]";
    }

    private static async Task DiscordLog(LogMessage logMessage)
    {
        string message;
        if (logMessage.Exception == null)
        {
            message = $"(Discord | {logMessage.Source}): {logMessage.Message}";
        }
        else
        {
            message =
                $"""
                (Discord | {logMessage.Source}: {logMessage.Message}
                Exception: {logMessage.Exception.Message}
                StackTrace: {logMessage.Exception.StackTrace}
                """;
        }

        _logger.Log(logMessage.Severity, message);
    }
}
