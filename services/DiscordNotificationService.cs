using Discord;
using Discord.Rest;
using Discord.WebSocket;
using discordbot.log;
using discordbot.models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace discordbot.services;

internal sealed class DiscordNotificationService : INotificationService
{
    private readonly string _channelName;
    private readonly Logger _logger;
    private readonly IConfigStore _configStore;

    public DiscordNotificationService(string channelName, Logger logger, IConfigStore configStore)
    {
        _channelName = channelName;
        _logger = logger;
        _configStore = configStore;
    }


    public Task NotifyGuildAsync(SocketGuild guild, string message)
    {
        SocketTextChannel[] channels = [.. guild.TextChannels.Where(t => t.Name == _channelName)];
        if (channels.Length == 0)
        {
            return Task.Run(() => _logger.Log(LogSeverity.Warning, $"(App | SendMessage): {guild.Name}({guild.Id}) has no \"{_channelName}\" channel"));
        }

        Task[] tasks = new Task[channels.Length];
        for (int i = 0; i < channels.Length; i++)
        {
            tasks[i] = channels[i].SendMessageAsync(message).ContinueWith(OnContinueWith);
        }

        return Task.Run(() => Task.WaitAll(tasks));

        void OnContinueWith(Task<RestUserMessage> task)
        {
            _logger.Log(LogSeverity.Info, $"(App | SendMessage): {task.Status} Message sent: {task.Result.Content}");
        }
    }

    public Task NotifyRateAsync(IEnumerable<SocketGuild> guilds, decimal rate)
    {
        Task[] tasks = new Task[guilds.Count()];
        int count = 0;

        foreach (SocketGuild guild in guilds)
        {
            if (!guild.IsConnected)
            {
                continue;
            }

            Range range = _configStore.GetOrCreateRange(guild.Id, decimal.MinValue, decimal.MaxValue);
            string? message = range.Compare(rate) switch
            {
                < 0 => $"Lower bound reached:\n    {1:n2}€ = {rate:n5}R$",
                > 0 => $"Upper bound reached:\n    {1:n2}€ = {rate:n5}R$",
                _ => null
            };

            if (message == null)
            {
                continue;
            }

            tasks[count] = NotifyGuildAsync(guild, message);
            count++;
        }

        if (count == 0)
        {
            return Task.CompletedTask;
        }

        if (count < tasks.Length)
        {
            System.Array.Resize(ref tasks, count);
        }

        return Task.Run(() => Task.WaitAll(tasks));
    }
}
