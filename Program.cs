using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Text.Json;

namespace DiscordBot;

internal class Program
{
    private static DiscordSocketClient _client;
    private static HashSet<ulong> _greetedGuilds = new();

    private const ConsoleColor DEFAULT_COLOR = ConsoleColor.Green;

    static Program()
    {
        DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.All };
        _client = new(socketConfig);
        _client.MessageReceived += MessageReceivedAsync;
        _client.Log += DiscordLog;
        _client.Connected += OnConnected;
        _client.Ready += OnReady;
        _client.GuildAvailable += OnGuildAvailabe;
        _client.Disconnected += OnDisconnected;
    }

    static void Main(string[] args)
    {
        Console.ForegroundColor = DEFAULT_COLOR;
        string token;
        if (args.Length == 0)
        {
            FileInfo configFileInfo = new("./config.json");
            if (configFileInfo.Exists)
            {
                using (StreamReader file = new(configFileInfo.OpenRead()))
                {
                    string configJSON = file.ReadToEnd();
                    Config? config = JsonSerializer.Deserialize<Config>(configJSON);
                    if (config == null)
                    {
                        Console.WriteLine("Could not deserialize config.json. Exiting...");
                        return;
                    }
                    token = config.token;
                }
            }
            else
            {
                Console.WriteLine("No argument or config file. Exiting...");
                return;
            }
        }
        else
            token = args[0];

        AppDomain.CurrentDomain.ProcessExit += OnExit;
        Console.WriteLine("Intializing bot...");

        Task task = _client.LoginAsync(TokenType.Bot, token);
        task.Wait();
        task = _client.StartAsync();
        task.Wait();

        // Block the program until it is closed
        task = Task.Delay(-1);
        task.Wait();
    }

    private static void OnExit(object? sender, EventArgs e)
    {
        if (_client == null)
            return;

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

    private static async Task OnConnected()
    {
        await Console.Out.WriteLineAsync($"Bot connected: {_client.CurrentUser.Username}.");
        await LogBot();
    }
    private static async Task OnDisconnected(Exception exception)
    {
        await Console.Out.WriteLineAsync($"Disconnected.");
        await LogBot();
    }

    private static async Task OnGuildAvailabe(SocketGuild guild)
    {
        const string MESSAGE = "I am awake.";
        Console.WriteLine($"Guild connected: {guild.Name}");
        if (!_greetedGuilds.Contains(guild.Id))
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
        //foreach (var guild in _client.Guilds)
        //{
        //    Task task;
        //    int count = 0;
        //    do
        //    {
        //        await Console.Out.WriteLineAsync($"Downloading({++count}) users for guild: {guild.Name}");
        //        task = guild.DownloadUsersAsync();
        //        await task;
        //    }
        //    while (task.Status != TaskStatus.RanToCompletion);
        //    await Console.Out.WriteLineAsync($"Downloaded users for guild: {guild.Name}");
        //}

        await Console.Out.WriteLineAsync($"Bot ready: {_client.CurrentUser.Username}.");
    }

    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        Console.WriteLine($"[{message}] Message received:");
        Console.WriteLine(message.Content);

        Task<RestUserMessage> sendTask = message.Channel.SendMessageAsync("Message received");
        await sendTask;
        Console.WriteLine($"Message sent: {sendTask.Status}.");
    }

    private static async Task LogBot()
    {
        await Console.Out.WriteLineAsync($"Bot status: [ {nameof(_client.Activity)}: {_client.Activity?.Name ?? "null"} | " +
                                         $"{nameof(_client.ConnectionState)}: {_client.ConnectionState} | {nameof(_client.LoginState)}: {_client.LoginState} | " +
                                         $"{nameof(_client.Status)}: {_client.Status} ]");
    }

    private static async Task DiscordLog(LogMessage message)
    {
        switch (message.Severity)
        {
            //
            // Summary:
            //     Logs that contain the most severe level of error. This type of error indicate
            //     that immediate attention may be required.
            case LogSeverity.Critical:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            //
            // Summary:
            //     Logs that highlight when the flow of execution is stopped due to a failure.
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                break;
            //
            // Summary:
            //     Logs that highlight an abnormal activity in the flow of execution.
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            //
            // Summary:
            //     Logs that track the general flow of the application.
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            //
            // Summary:
            //     Logs that are used for interactive investigation during development.
            case LogSeverity.Verbose:
            //
            // Summary:
            //     Logs that contain the most detailed messages.
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }

        await Console.Out.WriteLineAsync($"Discord ({message.Source} | {message.Severity}): {message.Message}");
        if (message.Exception != null)
            await Console.Out.WriteLineAsync($"Exception: {message.Exception.Message}\nStackTrace: {message.Exception.StackTrace}");

        Console.ForegroundColor = DEFAULT_COLOR;
    }
}
