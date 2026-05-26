using Discord;
using Discord.Rest;
using Discord.WebSocket;
using discordbot.models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Range = discordbot.models.Range;

namespace discordbot.services;

internal sealed class DiscordNotificationService : INotificationService
{
    private readonly ILogger<DiscordNotificationService> _logger;
    private readonly IConfigStore _configStore;

    public DiscordNotificationService(ILogger<DiscordNotificationService> logger, IConfigStore configStore)
    {
        _logger = logger;
        _configStore = configStore;
    }


    public async Task NotifyGuildAsync(SocketGuild guild, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string channelName = _configStore.ChannelName;
        SocketTextChannel[] channels = [.. guild.TextChannels.Where(t => t.Name == channelName)];
        if (channels.Length == 0)
        {
            _logger.LogWarning("{GuildName}({GuildId}) has no \"{ChannelName}\" channel", guild.Name, guild.Id, channelName);
            return;
        }

        List<Task> tasks = new List<Task>(channels.Length);
        for (int i = 0; i < channels.Length; i++)
            tasks.Add(SendAndLogAsync(channels[i], message, cancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task NotifyRateAsync(IReadOnlyCollection<SocketGuild> guilds, decimal rate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<Task> tasks = new(guilds.Count);

        foreach (SocketGuild guild in guilds)
        {
            if (!guild.IsConnected)
                continue;

            Range range = _configStore.GetOrCreateRange(guild.Id, decimal.MinValue, decimal.MaxValue);
            string? message = range.Compare(rate) switch
            {
                < 0 => $"Lower bound reached:\n    {1:n2}€ = {rate:n5}R$",
                > 0 => $"Upper bound reached:\n    {1:n2}€ = {rate:n5}R$",
                _ => null
            };

            if (message == null)
                continue;

            tasks.Add(NotifyGuildAsync(guild, message, cancellationToken));
        }

        if (tasks.Count == 0)
            return;

        await Task.WhenAll(tasks);
    }

    private async Task SendAndLogAsync(SocketTextChannel channel, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task<RestUserMessage> task = channel.SendMessageAsync(message);
        IUserMessage sent = await task;
        using IDisposable? scope =
            _logger.BeginScope(new Dictionary<string, object?> { ["Scope"] = $"[{task.Status}] " });
        _logger.LogInformation("Message sent:\n{Content}", sent.Content);
    }
}
