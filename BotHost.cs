using Discord;
using Discord.WebSocket;
using discordbot.log;
using discordbot.models;
using discordbot.services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Range = discordbot.models.Range;

namespace discordbot;

internal sealed class BotHost : IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly Logger _logger;
    private readonly IConfigStore _configStore;
    private readonly IRateProvider _rateProvider;
    private readonly INotificationService _notificationService;
    private readonly ICommandHandler _commandHandler;
    private readonly HashSet<ulong> _greetedGuilds = [];

    private decimal _lastRate = -1;
    private Task? _rateLoopTask;
    private readonly CancellationTokenSource _rateLoopCts = new();

    public BotHost(DiscordSocketClient client, Logger logger, IConfigStore configStore, IRateProvider rateProvider, INotificationService notificationService, ICommandHandler commandHandler)
    {
        _client = client;
        _logger = logger;
        _configStore = configStore;
        _rateProvider = rateProvider;
        _notificationService = notificationService;
        _commandHandler = commandHandler;

        _client.Ready += OnReady;
        _client.MessageReceived += OnMessageReceivedAsync;
        _client.Log += OnDiscordLog;
        _client.Connected += OnConnected;
        _client.Disconnected += OnDisconnected;
        _client.GuildAvailable += OnGuildAvailable;
    }

    public async Task RunAsync(string token, CancellationToken cancellationToken)
    {
        _configStore.Load();
        _logger.Log(LogSeverity.Info, "(App | Initialization): Initializing config.");
        _logger.Log(LogSeverity.Info, "(App | Initialization): Initializing bot.");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await StopAsync();
        }
    }

    public async Task StopAsync()
    {
        _logger.Log(LogSeverity.Info, "(App | OnExit): Exiting");

        if (_client.ConnectionState == ConnectionState.Connected)
        {
            const string message = "I am going to sleep.";
            await NotifyAllGuildsAsync(message, CancellationToken.None);
        }

        if (_rateLoopTask != null)
        {
            try
            {
            _rateLoopCts.Cancel();
            await _rateLoopTask;
            }
            catch (Exception exception)
            {
                _logger.Log(LogSeverity.Error, $"(App | Loop): {exception.Message}");
            }
        }

        await _client.StopAsync();
        await _client.LogoutAsync();

        _logger.Log(LogSeverity.Info, "(App | OnExit): Exited");
        _logger.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task OnReady()
    {
        _logger.Log(LogSeverity.Info, $"(App | Bot): Bot ({_client.CurrentUser.Username}) ready. {LogBot()}");
        _rateLoopTask ??= RunRateLoopAsync();
    }

    private async Task OnConnected()
    {
        _logger.Log(LogSeverity.Info, $"(App | Connection): Bot ({_client.CurrentUser.Username}) connected. {LogBot()}");
    }

    private async Task OnDisconnected(Exception exception)
    {
        string username = _client.CurrentUser?.Username ?? "unknown";
        _logger.Log(LogSeverity.Info, $"(App | Connection): Bot ({username}) disconnected. {LogBot()}");
    }

    private async Task OnGuildAvailable(SocketGuild guild)
    {
        _logger.Log(LogSeverity.Info, $"(App | Connection): Guild ({guild.Name}) connected.");
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
            await _notificationService.NotifyGuildAsync(guild, msg, _rateLoopCts.Token);
        }

        _configStore.Save();
    }

    private Task OnMessageReceivedAsync(SocketMessage message) => _commandHandler.HandleAsync(message, _lastRate, _rateLoopCts.Token);

    private async Task OnDiscordLog(LogMessage logMessage)
    {
        string message = logMessage.Exception == null
            ? $"(Discord | {logMessage.Source}): {logMessage.Message}"
            : $"(Discord | {logMessage.Source}: {logMessage.Message}\nException: {logMessage.Exception.Message}\nStackTrace: {logMessage.Exception.StackTrace}";

        _logger.Log(logMessage.Severity, message);
    }

    private async Task RunRateLoopAsync()
    {
        try
        {
            CancellationToken token = _rateLoopCts.Token;
            TimeSpan initialDelay = GetDelayUntilNextTick();
            if (initialDelay > TimeSpan.Zero)
                await Task.Delay(initialDelay, token);

            using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    if (_client.Guilds.Count > 0)
                    {
                        decimal rate = await _rateProvider.GetRateAsync();
                        if (rate != _lastRate)
                        {
                            _lastRate = rate;
                            await _notificationService.NotifyRateAsync(_client.Guilds, _lastRate, token);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _logger.Log(LogSeverity.Error, $"(App | Loop): {exception.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task NotifyAllGuildsAsync(string message, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];
        foreach (SocketGuild guild in _client.Guilds)
        {
            tasks.Add(_notificationService.NotifyGuildAsync(guild, message, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }

    private static TimeSpan GetDelayUntilNextTick()
    {
        DateTime now = DateTime.Now;
        const int minutes = 5;
        int roundedMinute = ((now.Minute / minutes) + 1) * minutes;
        DateTime next = new(now.Year, now.Month, now.Day, now.Hour, 0, 0);
        next = next.AddMinutes(roundedMinute);

        TimeSpan span = next - DateTime.Now;
        if (span.TotalMinutes < 0.5d)
            span = TimeSpan.FromMinutes(0.5d);

        return span;
    }

    private string LogBot()
    {
        return $"Bot status: [ {nameof(_client.Activity)}: {_client.Activity?.Name ?? "null"} | " +
               $"{nameof(_client.ConnectionState)}: {_client.ConnectionState} | {nameof(_client.LoginState)}: {_client.LoginState} | " +
               $"{nameof(_client.Status)}: {_client.Status} ]";
    }
}
