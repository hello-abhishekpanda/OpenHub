using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenHub.Application.Abstractions;

namespace OpenHub.Infrastructure.Sync;

public sealed class GitHubSyncWorker(ISyncQueue queue, IServiceScopeFactory scopes, ILogger<GitHubSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GitHub sync worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var username = await queue.DequeueAsync(stoppingToken);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IGitHubSyncService>().SyncAsync(username, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Background sync failed for {Username}", username); }
        }
    }
}
