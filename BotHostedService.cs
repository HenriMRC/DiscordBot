using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace discordbot;

internal sealed class BotHostedService : IHostedService
{
    private readonly BotHost _botHost;
    private readonly BotRuntimeSettings _settings;
    private Task? _runTask;
    private readonly CancellationTokenSource _shutdownCts = new();

    public BotHostedService(BotHost botHost, BotRuntimeSettings settings)
    {
        _botHost = botHost;
        _settings = settings;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runTask = _botHost.RunAsync(_settings.Token, _shutdownCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdownCts.Cancel();
        if (_runTask != null)
        {
            await _runTask;
        }
    }
}
