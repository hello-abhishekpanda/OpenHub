namespace OpenHub.Domain.Entities;

public sealed class RepositoryMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long GitHubId { get; set; }
    public Guid DeveloperProfileId { get; set; }
    public DeveloperProfile DeveloperProfile { get; set; } = null!;
    public required string Name { get; set; }
    public string FullName { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public string? Description { get; set; }
    public string? PrimaryLanguage { get; set; }
    public int Stars { get; set; }
    public int Forks { get; set; }
    public int OpenIssues { get; set; }
    public int RecentCommitsLast30Days { get; set; }
    public DateTimeOffset? LastCommitAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsFork { get; set; }
    public bool IsArchived { get; set; }
    public string[] Topics { get; set; } = [];
}
