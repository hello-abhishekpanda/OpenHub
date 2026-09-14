namespace OpenHub.Application.Contracts;

public sealed record LanguageShare(string Name, double Percentage);
public sealed record ProfileDto(Guid Id, string Username, string? DisplayName, string AvatarUrl, string? Bio,
    int TotalContributions, int Followers, int RepositoryCount, double Score, long? GlobalRank,
    string ActivityBadge, string SyncStatus, DateTimeOffset? LastSyncedAt, IReadOnlyList<LanguageShare> Languages);
public sealed record LeaderboardEntry(long Rank, string Username, string? DisplayName, string AvatarUrl, double Score, string ActivityBadge);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
public sealed record SyncRequest(string Username);
public sealed record SyncAccepted(string Username, string Status, DateTimeOffset QueuedAt);
public sealed record ConnectionStatus(bool Connected, string? Login, int Remaining, int Limit, DateTimeOffset? ResetAt, string? Error);
public sealed record RankingInput(int Stars, int Forks, int RecentCommitsLast30Days, DateTimeOffset? LastCommitAt);
