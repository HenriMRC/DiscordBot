﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Log;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot;

internal class Program
{
    private readonly static DiscordSocketClient _client;
    private readonly static HashSet<ulong> _greetedGuilds;
    private readonly static Logger _logger;

    private static decimal _lastRate = -1;
    private static decimal _lowerBound = 6.1m;
    private static decimal _upperBound = 6.2m;

    static Program()
    {
        DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.All };
        _client = new DiscordSocketClient(socketConfig);
        _client.MessageReceived += MessageReceivedAsync;
        _client.Log += DiscordLog;
        _client.Connected += OnConnected;
        _client.Ready += OnReady;
        _client.GuildAvailable += OnGuildAvailable;
        _client.Disconnected += OnDisconnected;

        _greetedGuilds = [];

        _logger = new(LogSeverity.Debug,
#if DEBUG
            new ConsoleWriter(),
#endif
            new FileWriter());
    }

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

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

        const string URL = "https://wise.com/gateway/v3/quotes";
        const string CONTENT = @"{""sourceAmount"":1000,""sourceCurrency"":""EUR"",""targetCurrency"":""BRL"",""guaranteedTargetAmount"":false,""type"":""REGULAR""}";

        HttpClient httpClient = new();
        StringContent content = new(CONTENT, Encoding.UTF8, "application/json");

        while (true)
        {
            if (_client.Guilds.Count > 0)
            {
                Task<HttpResponseMessage> post = httpClient.PostAsync(URL, content);
                post.Wait();

                Task<string> postContent = post.Result.Content.ReadAsStringAsync();
                postContent.Wait();

                JsonDocument jsonDocument = JsonDocument.Parse(postContent.Result);
                if (jsonDocument == null)
                {
                    _logger.Log(LogSeverity.Error, $"(App | WiseRequest): Could not deserialize\n{postContent.Result}");
                    continue;
                }

                dynamic jsonObject = jsonDocument.ToExpandoObject();

                _lastRate = (decimal)jsonObject.rate;

                string? message = null;
                if (_lastRate <= _lowerBound)
                    message =
                        $"""
                        Lower bound reached:
                            {1:n2}€ = {(decimal)jsonObject.rate:n5}R$
                        """;
                else if (_lastRate >= _upperBound)
                    message =
                        $"""
                        Upper bound reached:
                            {1:n2}€ = {(decimal)jsonObject.rate:n5}R$
                        """;

                if (message != null)
                {
                    Task[] tasks = new Task[_client.Guilds.Count];
                    int count = 0;
                    foreach (SocketGuild guild in _client.Guilds)
                    {
                        tasks[count] = MessageGuild(guild, message);
                        count++;
                    }
                    Task.WaitAll(tasks);
                }

                Thread.Sleep(300_000);
            }
            else
                Thread.Sleep(1_000);
        }
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

        string msg =
            $"""
            I am awake.

                Variables:
                 - Environment: {Environment.CurrentDirectory}
            - AppContext: {AppContext.BaseDirectory}
            
                Configuration:
                 - Lower bound: {_lowerBound:n2}
            - Upper bound: {_upperBound:n2}
            """;
        if (_greetedGuilds.Add(guild.Id))
            await MessageGuild(guild, msg);
    }

    private static async Task MessageGuild(SocketGuild guild, string message)
    {
        if (guild.DefaultChannel.GetChannelType() == ChannelType.Text)
            guild.DefaultChannel.SendMessageAsync(message).Wait();
        else if (guild.TextChannels.Count > 0)
        {
            using IEnumerator<SocketTextChannel> textChannels = guild.TextChannels.OrderBy(c => c.CreatedAt).GetEnumerator();
            while (textChannels.MoveNext())
            {
                if (textChannels.Current.GetChannelType() == ChannelType.Text)
                {
                    await textChannels.Current.SendMessageAsync(message)
                        .ContinueWith(m => _logger.Log(LogSeverity.Info, $"(App | SendMessage): {m.Status} Message sent: {m.Result.Content}"));
                    break;
                }
            }
        }
    }

    private static async Task OnReady()
    {
        await Task.Run(() => _logger.Log(LogSeverity.Info, $"(App | Bot): Bot ({_client.CurrentUser.Username}) ready. {LogBot()}"));
    }

    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        const string CMD_UPDATE = "update";

        if (message.Author.IsBot || message.Channel is not SocketTextChannel channel)
            return;

        _logger.Log(LogSeverity.Info, $"(App | MessageReceived): {message.Content}");

        string content = message.Content;
        string response;
        if (content == CMD_UPDATE)
        {
            if (_lastRate < 0)
                response = "Rate not updated yet.";
            else
                response = $"{1:n2}€ = {_lastRate:n5}R$";
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

        await Task.Run(() => _logger.Log(logMessage.Severity, message));
    }
}
