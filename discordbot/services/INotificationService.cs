using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace discordbot.services;

internal interface INotificationService
{
    Task NotifyGuildAsync(SocketGuild guild, string message, CancellationToken cancellationToken);
    Task NotifyRateAsync(IReadOnlyCollection<SocketGuild> guilds, decimal rate, CancellationToken cancellationToken);
}
