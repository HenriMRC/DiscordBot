using Discord.WebSocket;
using System.Threading;
using System.Threading.Tasks;

namespace discordbot.services;

internal interface ICommandHandler
{
    Task HandleAsync(SocketMessage message, decimal lastRate, CancellationToken cancellationToken);
}
