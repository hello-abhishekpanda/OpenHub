using OpenHub.Application.Contracts;

namespace OpenHub.Application.Abstractions;

public interface IRankingService
{
    double CalculateScore(IEnumerable<RankingInput> repositories, DateTimeOffset now);
    Task UpsertAsync(string username, double score, CancellationToken cancellationToken = default);
    Task<long?> GetRankAsync(string username, CancellationToken cancellationToken = default);
    Task<PageResult<(string Username, double Score)>> GetLeaderboardAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
public interface IGitHubSyncService
{
    Task SyncAsync(string username, CancellationToken cancellationToken);
    Task<ConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken);
}
public interface ISyncQueue
{
    ValueTask QueueAsync(string username, CancellationToken cancellationToken = default);
    ValueTask<string> DequeueAsync(CancellationToken cancellationToken);
}
