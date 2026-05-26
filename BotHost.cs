using Discord;
using Discord.WebSocket;
using discordbot.services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Range = discordbot.models.Range;

namespace discordbot;

internal sealed class BotHost
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<BotHost> _logger;
    private readonly AppState _state;
    private readonly IConfigStore _configStore;
    private readonly IRateProvider _rateProvider;
    private readonly INotificationService _notificationService;
    private readonly ICommandHandler _commandHandler;
    private Task? _rateLoopTask;
    private readonly CancellationTokenSource _rateLoopCts = new();

    public BotHost(DiscordSocketClient client, ILogger<BotHost> logger, AppState state, IConfigStore configStore, IRateProvider rateProvider, INotificationService notificationService, ICommandHandler commandHandler)
    {
        _client = client;
        _logger = logger;
        _state = state;
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
        _logger.LogInformation("Initializing config.");
        _logger.LogInformation("Initializing bot.");

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
        _logger.LogInformation("Exiting.");

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
                _logger.LogError(exception, "Error while stopping rate loop.");
            }
        }

        await _client.StopAsync();
        await _client.LogoutAsync();

        _logger.LogInformation("Exited.");
    }

    private async Task OnReady()
    {
        _logger.LogInformation("Bot ({Username}) ready. {Status}", _client.CurrentUser.Username, LogBot());
        _rateLoopTask ??= RunRateLoopAsync();
    }

    private async Task OnConnected()
    {
        _logger.LogInformation("Bot ({Username}) connected. {Status}", _client.CurrentUser.Username, LogBot());
    }

    private async Task OnDisconnected(Exception exception)
    {
        string username = _client.CurrentUser?.Username ?? "unknown";
        _logger.LogInformation("Bot ({Username}) disconnected. {Status}", username, LogBot());
    }

    private async Task OnGuildAvailable(SocketGuild guild)
    {
        _logger.LogInformation("Guild ({GuildName}) connected.", guild.Name);
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

        if (_state.MarkGuildAsGreeted(guild.Id))
        {
            await _notificationService.NotifyGuildAsync(guild, msg, _rateLoopCts.Token);
        }

        _configStore.Save();
    }

    private Task OnMessageReceivedAsync(SocketMessage message) => _commandHandler.HandleAsync(message, _state.GetLastRate(), _rateLoopCts.Token);

    private async Task OnDiscordLog(LogMessage logMessage)
    {
        LogLevel level = MapSeverity(logMessage.Severity);
        using IDisposable? scope =
            _logger.BeginScope(new Dictionary<string, object?> { ["Scope"] = $"[Discord.Net/{logMessage.Source}] " });

        if (logMessage.Exception == null)
            _logger.Log(level, logMessage.Message);
        else
            _logger.Log(level, logMessage.Exception, logMessage.Message);
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
                        if (_state.TryUpdateLastRate(rate))
                        {
                            await _notificationService.NotifyRateAsync(_client.Guilds, rate, token);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while polling rate.");
                }
            }
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError(exception, "Rate loop canceled.");
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

    private static LogLevel MapSeverity(LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };
    }
}
