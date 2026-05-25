using Discord;
using Discord.Rest;
using Discord.WebSocket;
using discordbot.log;
using System;
using System.Threading.Tasks;

namespace discordbot.services;

internal sealed class DiscordCommandHandler : ICommandHandler
{
    private readonly Logger _logger;
    private readonly IConfigStore _configStore;

    public DiscordCommandHandler(Logger logger, IConfigStore configStore)
    {
        _logger = logger;
        _configStore = configStore;
    }

    public async Task HandleAsync(SocketMessage message, decimal lastRate)
    {
        const string cmdUpdate = "update";

        if (message.Author.IsBot || message.Channel is not SocketTextChannel channel)
        {
            return;
        }

        _logger.Log(LogSeverity.Info, $"(App | MessageReceived): {message.Content}");

        string content = message.Content;
        string response;

        if (content.Equals(cmdUpdate, StringComparison.CurrentCultureIgnoreCase))
        {
            response = lastRate < 0 ? "Rate not updated yet." : $"{1:n2}€ = {lastRate:n5}R$";
        }
        else if (content.StartsWith("min:"))
        {
            response = HandleMinCommand(content["min:".Length..], message);
        }
        else if (content.StartsWith("max:"))
        {
            response = HandleMaxCommand(content["max:".Length..], message);
        }
        else
        {
            response = $"Command unknown:\n{content}";
        }

        Task<RestUserMessage> sendTask = channel.SendMessageAsync(response, messageReference: new MessageReference(message.Id));
        await sendTask;

        _logger.Log(LogSeverity.Info, $"(App | MessageReceived): Message sent {sendTask.Status}");
    }

    private string HandleMinCommand(string content, SocketMessage message)
    {
        if (!decimal.TryParse(content, out decimal value))
        {
            return $"Could not parse value: [{content}]";
        }

        if (message.Channel is not SocketGuildChannel guildChannel)
        {
            _logger.Log(LogSeverity.Error, $"Channel type not expected: {message.Channel.Id} | {message.Channel.Name} | {message.Channel.GetType()}");
            return "Failed";
        }

        discordbot.models.Range range = _configStore.GetOrCreateRange(guildChannel.Guild.Id, value, decimal.MaxValue);
        range.Minimum = value;
        _configStore.Save();
        return $" - Lower bound: {value:n2}";
    }

    private string HandleMaxCommand(string content, SocketMessage message)
    {
        if (!decimal.TryParse(content, out decimal value))
        {
            return $"Could not parse value: [{content}]";
        }

        if (message.Channel is not SocketGuildChannel guildChannel)
        {
            _logger.Log(LogSeverity.Error, $"Channel type not expected: {message.Channel.Id} | {message.Channel.Name} | {message.Channel.GetType()}");
            return "Failed";
        }

        discordbot.models.Range range = _configStore.GetOrCreateRange(guildChannel.Guild.Id, decimal.MinValue, value);
        range.Maximum = value;
        _configStore.Save();
        return $" - Upper bound: {value:n2}";
    }
}
