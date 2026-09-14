using System.Text.Json.Serialization;

namespace OpenHub.Infrastructure.Sync;

internal sealed record GitHubUser(
    long Id, string Login, string? Name,
    [property: JsonPropertyName("avatar_url")] string AvatarUrl,
    string? Bio, string? Location, string? Company, int Followers,
    [property: JsonPropertyName("public_repos")] int PublicRepos);
internal sealed record GitHubRepository(
    long Id, string Name, [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("html_url")] string HtmlUrl, string? Description, string? Language,
    [property: JsonPropertyName("stargazers_count")] int Stars,
    [property: JsonPropertyName("forks_count")] int Forks,
    [property: JsonPropertyName("open_issues_count")] int OpenIssues,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    bool Fork, bool Archived, string[]? Topics);
internal sealed record GitHubCommit([property: JsonPropertyName("commit")] GitHubCommitInfo Commit);
internal sealed record GitHubCommitInfo(GitHubSignature Author);
internal sealed record GitHubSignature(DateTimeOffset Date);
