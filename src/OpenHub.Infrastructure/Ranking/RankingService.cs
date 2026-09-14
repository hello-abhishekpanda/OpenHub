using OpenHub.Application.Abstractions;
using OpenHub.Application.Contracts;
using OpenHub.Application.Ranking;
using StackExchange.Redis;

namespace OpenHub.Infrastructure.Ranking;

public sealed class RankingService(IConnectionMultiplexer redis) : IRankingService
{
    public const string GlobalLeaderboardKey = "opencode:leaderboard:global";
    private IDatabase Database => redis.GetDatabase();

    public double CalculateScore(IEnumerable<RankingInput> repositories, DateTimeOffset now) =>
        RankingFormula.Calculate(repositories, now);

    public Task UpsertAsync(string username, double score, CancellationToken cancellationToken = default) =>
        Database.SortedSetAddAsync(GlobalLeaderboardKey, username.ToLowerInvariant(), score);

    public async Task<long?> GetRankAsync(string username, CancellationToken cancellationToken = default)
    {
        var zeroBased = await Database.SortedSetRankAsync(GlobalLeaderboardKey, username.ToLowerInvariant(), Order.Descending);
        return zeroBased is null ? null : zeroBased.Value + 1;
    }

    public async Task<PageResult<(string Username, double Score)>> GetLeaderboardAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var start = (page - 1L) * pageSize;
        var values = await Database.SortedSetRangeByRankWithScoresAsync(
            GlobalLeaderboardKey, start, start + pageSize - 1, Order.Descending);
        var total = await Database.SortedSetLengthAsync(GlobalLeaderboardKey);
        return new(values.Select(x => ((string)x.Element!, x.Score)).ToArray(), page, pageSize, total);
    }
}
