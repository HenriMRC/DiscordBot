using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Log;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot;

internal class Program
{
    private const string CHANNEL_NAME = "bot-cambio";

    private static Task? _loop;

    private readonly static List<SocketTextChannel> _socketTextChannels = [];

    private readonly static DiscordSocketClient _client;
    private readonly static HashSet<ulong> _greetedGuilds;
    private readonly static Logger _logger;
    private readonly static JsonHandler _jsonHandler;

    private static decimal _lastRate = -1;
    private static Config _config = new();


    static Program()
    {
        _jsonHandler = new JsonHandler();

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
        //_client.GuildUnavailable
        //_client.JoinedGuild += OnGuildJoined;
        //_client.ChannelCreated
        //_client.ChannelUpdated
        //_client.ChannelDestroyed

        _greetedGuilds = [];

        _logger = new(LogSeverity.Debug,
#if DEBUG
            new ConsoleWriter(),
#endif
            new FileWriter());
    }

    static void Main(string[] args)
    {
        _config = _jsonHandler.ReadConfigFile() ?? _config;

        _logger.Log(LogSeverity.Info, $"(App | Initialization): Initializing config.");
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
            const string MESSAGE = "I am going to sleep.";
            Task[] tasks = new Task[_client.Guilds.Count];
            int count = 0;
            foreach (SocketGuild guild in _client.Guilds)
            {
                tasks[count] = MessageGuild(guild, MESSAGE);
                count++;
            }
            Task.WaitAll(tasks);
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
        if (!_config.Channels.TryGetValue(guild.Id, out Models.Range? channel))
        {
            channel = new(6.1m, 6.2m);
            _config.Channels.Add(guild.Id, channel);
        }

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
            await MessageGuild(guild, msg);

        _jsonHandler.WriteConfigToFile(_config);
    }

    private static Task MessageGuild(SocketGuild guild, string message)
    {
        SocketTextChannel[] channels = [.. guild.TextChannels.Where(t => t.Name == CHANNEL_NAME)];

        if (channels.Length == 0)
            return Task.Run(() => _logger.Log(LogSeverity.Warning, $"(App | SendMessage): {guild.Name}({guild.Id}) has no \"{CHANNEL_NAME}\" channel"));
        else
        {
            Task[] tasks = new Task[channels.Length];
            for (int i = 0; i < channels.Length; i++)
                tasks[i] = channels[i].SendMessageAsync(message).ContinueWith(OnContinueWith);
            return Task.Run(() => Task.WaitAll(tasks));
        }

        static void OnContinueWith(Task<RestUserMessage> task)
        {
            _logger.Log(LogSeverity.Info, $"(App | SendMessage): {task.Status} Message sent: {task.Result.Content}");
        }
    }

    private static async Task OnReady()
    {
        await Task.Run(() => _logger.Log(LogSeverity.Info, $"(App | Bot): Bot ({_client.CurrentUser.Username}) ready. {LogBot()}"));
        _loop = Task.Run(Loop);
    }

    private static void Loop()
    {
        WiseClient client = new();
        while (true)
        {
            if (_client.Guilds.Count > 0)
            {
                Task<string> result = client.Request();
                result.Wait();

                JsonDocument jsonDocument = JsonDocument.Parse(result.Result);
                if (jsonDocument == null)
                {
                    _logger.Log(LogSeverity.Error, $"(App | WiseRequest): Could not deserialize\n{result.Result}");
                    continue;
                }

                dynamic jsonObject = jsonDocument.ToExpandoObject();

                //_client.GetGuild();
                decimal rate = (decimal)jsonObject.rate;

                if (rate != _lastRate)
                {
                    _lastRate = rate;
                    BroadcastRate();
                }
            }

            DateTime now = DateTime.Now;

            const int MINUTES = 5;
            int hour = now.Hour;
            int minute = now.Minute;
            minute /= MINUTES;
            minute++;
            minute *= MINUTES;

            hour += minute / 60;
            minute %= 60;

            DateTime next = new(now.Year, now.Month, now.Day, hour, minute, 0);

            TimeSpan span = next - DateTime.Now;
            if (span.TotalMinutes < 0.5d)
                span = TimeSpan.FromMinutes(0.5d);

            Thread.Sleep(span);
        }
    }

    private static void BroadcastRate()
    {
        Task[] tasks = new Task[_client.Guilds.Count];
        int count = 0;
        foreach (SocketGuild guild in _client.Guilds)
        {
            if (!_config.Channels.TryGetValue(guild.Id, out Models.Range? range))
            {
                range = new(decimal.MinValue, decimal.MaxValue);
                _config.Channels.Add(guild.Id, range);
            }

            if (!guild.IsConnected)
                continue;

            string? message = null;
            switch (range.Compare(_lastRate))
            {
                case < 0:
                    message = $"""
                        Lower bound reached:
                            {1:n2}€ = {_lastRate:n5}R$
                        """;

                    break;
                case > 0:
                    message = $"""
                        Upper bound reached:
                            {1:n2}€ = {_lastRate:n5}R$
                        """;
                    break;
                default:
                    continue;
            }

            tasks[count] = MessageGuild(guild, message);
            count++;
        }

        if (count > 0)
        {
            Array.Resize(ref tasks, count);
            Task.WaitAll(tasks);
        }
    }

    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        const string CMD_UPDATE = "update";

        if (message.Author.IsBot || message.Channel is not SocketTextChannel channel)
            return;

        _logger.Log(LogSeverity.Info, $"(App | MessageReceived): {message.Content}");

        string content = message.Content;
        string response;
        if (content.Equals(CMD_UPDATE, StringComparison.CurrentCultureIgnoreCase))
        {
            if (_lastRate < 0)
                response = "Rate not updated yet.";
            else
                response = $"{1:n2}€ = {_lastRate:n5}R$";
        }
        else if (content.StartsWith("min:"))
        {
            content = content["min:".Length..];
            if (!decimal.TryParse(content, out decimal value))
                response = $"Could not parse value: [{content}]";
            else
            {
                if (message.Channel is not SocketGuildChannel guildChannel)
                {
                    _logger.Log(LogSeverity.Error, $"Channel type not expected: {message.Channel.Id} | {message.Channel.Name} | {message.Channel.GetType()}");
                    response = $"Failed";
                }
                else
                {
                    SocketGuild guild = guildChannel.Guild;
                    if (!_config.Channels.TryGetValue(guild.Id, out Models.Range? range))
                    {
                        range = new(value, decimal.MaxValue);
                        _config.Channels.Add(guild.Id, range);
                    }
                    else
                        range.Minimum = value;
                    response = $" - Lower bound: {value:n2}";

                    _jsonHandler.WriteConfigToFile(_config);
                }
            }
        }
        else if (content.StartsWith("max:"))
        {
            content = content["max:".Length..];
            if (!decimal.TryParse(content, out decimal value))
                response = $"Could not parse value: [{content}]";
            else
            {
                if (message.Channel is not SocketGuildChannel guildChannel)
                {
                    _logger.Log(LogSeverity.Error, $"Channel type not expected: {message.Channel.Id} | {message.Channel.Name} | {message.Channel.GetType()}");
                    response = $"Failed";
                }
                else
                {
                    SocketGuild guild = guildChannel.Guild;
                    if (!_config.Channels.TryGetValue(guild.Id, out Models.Range? range))
                    {
                        range = new(decimal.MinValue, value);
                        _config.Channels.Add(guild.Id, range);
                    }
                    else
                        range.Maximum = value;
                    response = $" - Upper bound: {value:n2}";

                    _jsonHandler.WriteConfigToFile(_config);
                }
            }
        }
        else
        {
            response = $"Command unknown:\n{content}";
        }

        //Task<SocketThreadChannel> threadCreationTask = channel.CreateThreadAsync(content, message: message);
        //await threadCreationTask;
        //SocketThreadChannel thread = threadCreationTask.Result;

        Task<RestUserMessage> sendTask = channel.SendMessageAsync(response, messageReference: new(message.Id));
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

        _logger.Log(logMessage.Severity, message);
    }
}
