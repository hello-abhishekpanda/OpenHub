namespace OpenHub.Domain.Entities;

public sealed class DeveloperProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long GitHubId { get; set; }
    public required string Username { get; set; }
    public string? DisplayName { get; set; }
    public string AvatarUrl { get; set; } = "";
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Company { get; set; }
    public int Followers { get; set; }
    public int PublicRepositoryCount { get; set; }
    public int TotalContributions { get; set; }
    public double Score { get; set; }
    public ActivityBadge ActivityBadge { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public DateTimeOffset? LastSyncedAt { get; set; }
    public string? LastSyncError { get; set; }
    public List<RepositoryMetadata> Repositories { get; set; } = [];
}
public enum ActivityBadge { Trending, Steady, Inactive }
public enum SyncStatus { Pending, Queued, Running, Completed, Failed, RateLimited }
