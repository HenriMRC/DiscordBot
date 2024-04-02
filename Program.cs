using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Text.Json;

namespace DiscordBot;

internal class Program
{
    private static DiscordSocketClient _client;

    static void Main(string[] args)
    {
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

        DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.All };
        _client = new(socketConfig);
        _client.MessageReceived += MessageReceivedAsync;
        //_client.Log += LogAsync;
        _client.Connected += OnConnected;
        _client.GuildAvailable += OnGuildAvailabe;

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
        await Console.Out.WriteLineAsync($"Bot initialized: {_client.CurrentUser.Username}.");
        await LogBot();
    }

    private static async Task OnGuildAvailabe(SocketGuild guild)
    {
        const string MESSAGE = "I am awake.";
        Console.WriteLine($"Guild connected: {guild.Name}");
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
    }

    private static async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        Console.WriteLine($"[{message.ToString()}] Message received:");
        Console.WriteLine(message.Content);

        await LogBot();

        Task<RestUserMessage> sendTask = message.Channel.SendMessageAsync("Message received");
        await sendTask;
        Console.WriteLine($"Message sent: {sendTask.Status}.");
    }

    private static async Task LogBot()
    {
        await Console.Out.WriteLineAsync($"Bot status:\n{nameof(_client.Activity)}: {_client.Activity}\n{nameof(_client.ConnectionState)}: {_client.ConnectionState}\n{nameof(_client.LoginState)}: {_client.LoginState}\n{nameof(_client.Status)}: {_client.Status}");
    }
}
