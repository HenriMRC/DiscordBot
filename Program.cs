using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot;

internal class Program
{
    private readonly static DiscordSocketClient _client;
    private readonly static HashSet<ulong> _greetedGuilds;
    private readonly static Logger _logger;

    static Program()
    {
        DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.All };
        _client = new(socketConfig);
        _client.MessageReceived += MessageReceivedAsync;
        _client.Log += DiscordLog;
        _client.Connected += OnConnected;
        _client.Ready += OnReady;
        _client.GuildAvailable += OnGuildAvailable;
        _client.Disconnected += OnDisconnected;

        _greetedGuilds = [];

        _logger = new(LogSeverity.Debug, new ConsoleWriter());
    }

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;

        _logger.Log(LogSeverity.Info, $"(App | Initialization): Initializing config.");
        string token;
        if (args.Length == 0)
        {
            FileInfo configFileInfo = new("./config.json");
            if (configFileInfo.Exists)
            {
                using StreamReader file = new(configFileInfo.OpenRead());
                string configJson = file.ReadToEnd();
                JsonSerializerOptions jsonOptions = new(JsonSerializerOptions.Default) { IncludeFields = true };
                Config? config = JsonSerializer.Deserialize<Config>(configJson, jsonOptions);
                if (config == null)
                {
                    _logger.Log(LogSeverity.Critical, $"(App | Initialization): Could not deserialize config.json:\n{configFileInfo}");
                    return;
                }
                token = config.Token;
            }
            else
            {
                _logger.Log(LogSeverity.Critical, $"(App | Initialization): No argument or config file.");
                return;
            }
        }
        else
            token = args[0];

        _logger.Log(LogSeverity.Info, $"(App | Initialization): Initializing bot.");

        Task task = _client.LoginAsync(TokenType.Bot, token);
        task.Wait();
        task = _client.StartAsync();
        task.Wait();

        Thread.Sleep(Timeout.Infinite);
    }

    private static void OnExit(object? sender, EventArgs e)
    {
        _logger.Log(LogSeverity.Info, "(App | OnExit): Exiting");
        if (_client != null)
        {
            const string MESSAGE = "I am going to sleep.";
            foreach (var guild in _client.Guilds)
            {
                if (guild.DefaultChannel.GetChannelType() == ChannelType.Text)
                    guild.DefaultChannel.SendMessageAsync(MESSAGE).Wait();
                else if (guild.TextChannels.Count > 0)
                {
                    var textChannels = guild.TextChannels.GetEnumerator();
                    while (textChannels.MoveNext())
                    {
                        if (textChannels.Current.GetChannelType() == ChannelType.Text)
                        {
                            textChannels.Current.SendMessageAsync(MESSAGE).Wait();
                            break;
                        }
                    }
                }
            }
        }
        _logger.Log(LogSeverity.Info, "(App | OnExit): Exited");
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

        const string MESSAGE = "I am awake.";
        if (!_greetedGuilds.Contains(guild.Id))
        {
            if (guild.DefaultChannel.GetChannelType() == ChannelType.Text)
                guild.DefaultChannel.SendMessageAsync(MESSAGE).Wait();
            else if (guild.TextChannels.Count > 0)
            {
                using IEnumerator<SocketTextChannel> textChannels = guild.TextChannels.GetEnumerator();
                while (textChannels.MoveNext())
                {
                    if (textChannels.Current.GetChannelType() == ChannelType.Text)
                    {
                        await textChannels.Current.SendMessageAsync(MESSAGE);
                        break;
                    }
                }
            }
            _greetedGuilds.Add(guild.Id);
        }
    }


    private static async Task OnReady()
    {
        await Task.Run(() => _logger.Log(LogSeverity.Info, $"(App | Bot): Bot ({_client.CurrentUser.Username}) ready. {LogBot()}"));
    }

    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        _logger.Log(LogSeverity.Info, $"(App | MessageReceived): {message.Content}");

        Task<RestUserMessage> sendTask = message.Channel.SendMessageAsync("Message received");
        await sendTask;
        _logger.Log(LogSeverity.Info, $"(App | MessageReceived): Message sent {sendTask.Status}");
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
            message = $"(Discord | {logMessage.Source}): {logMessage.Message}";
        else
            message =
                $"""
                (Discord | {logMessage.Source}: {logMessage.Message}
                Exception: {logMessage.Exception.Message}
                StackTrace: {logMessage.Exception.StackTrace}
                """;

        await Task.Run(() => _logger.Log(logMessage.Severity, message));
    }
}
