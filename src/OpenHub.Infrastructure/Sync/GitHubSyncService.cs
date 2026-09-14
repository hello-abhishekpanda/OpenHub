using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenHub.Application.Abstractions;
using OpenHub.Application.Contracts;
using OpenHub.Domain.Entities;
using OpenHub.Infrastructure.Persistence;

namespace OpenHub.Infrastructure.Sync;

public sealed class GitHubSyncService(
    IHttpClientFactory clientFactory,
    OpenHubDbContext db,
    IRankingService ranking,
    TimeProvider clock,
    ILogger<GitHubSyncService> logger) : IGitHubSyncService
{
    public async Task SyncAsync(string username, CancellationToken cancellationToken)
    {
        username = username.Trim().ToLowerInvariant();
        var now = clock.GetUtcNow();
        var profile = await db.DeveloperProfiles.Include(x => x.Repositories)
            .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);

        try
        {
            if (profile is not null) { profile.SyncStatus = SyncStatus.Running; await db.SaveChangesAsync(cancellationToken); }
            var client = clientFactory.CreateClient("GitHub");
            var user = await GetRequiredAsync<GitHubUser>(client, $"users/{Uri.EscapeDataString(username)}", cancellationToken);
            var repositories = await GetAllRepositoriesAsync(client, username, cancellationToken);

            profile ??= new DeveloperProfile { Username = username, GitHubId = user.Id };
            profile.GitHubId = user.Id;
            profile.DisplayName = user.Name;
            profile.AvatarUrl = user.AvatarUrl;
            profile.Bio = user.Bio;
            profile.Location = user.Location;
            profile.Company = user.Company;
            profile.Followers = user.Followers;
            profile.PublicRepositoryCount = user.PublicRepos;

            var existing = profile.Repositories.ToDictionary(x => x.GitHubId);
            var activeIds = new HashSet<long>();
            foreach (var source in repositories)
            {
                activeIds.Add(source.Id);
                if (!existing.TryGetValue(source.Id, out var entity))
                {
                    entity = new RepositoryMetadata { GitHubId = source.Id, Name = source.Name };
                    profile.Repositories.Add(entity);
                }
                var since = now.AddDays(-30);
                var commits = await GetRecentCommitsAsync(client, source.FullName, since, cancellationToken);
                entity.Name = source.Name; entity.FullName = source.FullName; entity.HtmlUrl = source.HtmlUrl;
                entity.Description = source.Description; entity.PrimaryLanguage = source.Language;
                entity.Stars = source.Stars; entity.Forks = source.Forks; entity.OpenIssues = source.OpenIssues;
                entity.UpdatedAt = source.UpdatedAt; entity.IsFork = source.Fork; entity.IsArchived = source.Archived;
                entity.Topics = source.Topics ?? []; entity.RecentCommitsLast30Days = commits.Count;
                entity.LastCommitAt = commits.FirstOrDefault()?.Commit.Author.Date;
            }
            profile.Repositories.RemoveAll(x => !activeIds.Contains(x.GitHubId));
            profile.TotalContributions = profile.Repositories.Sum(x => x.RecentCommitsLast30Days);
            profile.Score = ranking.CalculateScore(profile.Repositories.Select(x =>
                new RankingInput(x.Stars, x.Forks, x.RecentCommitsLast30Days, x.LastCommitAt)), now);
            profile.ActivityBadge = profile.TotalContributions >= 20 ? ActivityBadge.Trending
                : profile.TotalContributions > 0 ? ActivityBadge.Steady : ActivityBadge.Inactive;
            profile.SyncStatus = SyncStatus.Completed; profile.LastSyncedAt = now; profile.LastSyncError = null;
            if (db.Entry(profile).State == EntityState.Detached) db.DeveloperProfiles.Add(profile);
            await db.SaveChangesAsync(cancellationToken);
            await ranking.UpsertAsync(username, profile.Score, cancellationToken);
        }
        catch (GitHubRateLimitException ex)
        {
            if (profile is not null) { profile.SyncStatus = SyncStatus.RateLimited; profile.LastSyncError = ex.Message; await db.SaveChangesAsync(cancellationToken); }
            logger.LogWarning("GitHub rate limit reached while syncing {Username}; resets at {ResetAt}", username, ex.ResetAt);
            throw;
        }
        catch (Exception ex)
        {
            if (profile is not null) { profile.SyncStatus = SyncStatus.Failed; profile.LastSyncError = ex.Message; await db.SaveChangesAsync(CancellationToken.None); }
            logger.LogError(ex, "GitHub sync failed for {Username}", username);
            throw;
        }
    }

    public async Task<ConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await clientFactory.CreateClient("GitHub").GetAsync("user", cancellationToken);
            var rate = ReadRateLimit(response);
            if (!response.IsSuccessStatusCode) return new(false, null, rate.Remaining, rate.Limit, rate.ResetAt, $"GitHub returned {(int)response.StatusCode}.");
            var user = await response.Content.ReadFromJsonAsync<GitHubUser>(cancellationToken);
            return new(true, user?.Login, rate.Remaining, rate.Limit, rate.ResetAt, null);
        }
        catch (Exception ex) { return new(false, null, 0, 0, null, ex.Message); }
    }

    private static async Task<T> GetRequiredAsync<T>(HttpClient client, string uri, CancellationToken ct)
    {
        using var response = await client.GetAsync(uri, ct);
        EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new InvalidOperationException("GitHub returned an empty response.");
    }

    private static async Task<List<GitHubRepository>> GetAllRepositoriesAsync(HttpClient client, string username, CancellationToken ct)
    {
        var all = new List<GitHubRepository>();
        for (var page = 1; ; page++)
        {
            var batch = await GetRequiredAsync<List<GitHubRepository>>(client,
                $"users/{Uri.EscapeDataString(username)}/repos?per_page=100&page={page}&sort=updated", ct);
            all.AddRange(batch);
            if (batch.Count < 100) return all;
        }
    }

    private static async Task<List<GitHubCommit>> GetRecentCommitsAsync(HttpClient client, string fullName, DateTimeOffset since, CancellationToken ct)
    {
        using var response = await client.GetAsync($"repos/{fullName}/commits?since={Uri.EscapeDataString(since.ToString("O"))}&per_page=100", ct);
        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound) return [];
        EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<List<GitHubCommit>>(cancellationToken: ct) ?? [];
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        var rate = ReadRateLimit(response);
        if (response.StatusCode == HttpStatusCode.Forbidden && rate.Remaining == 0)
            throw new GitHubRateLimitException(rate.ResetAt);
        response.EnsureSuccessStatusCode();
    }

    private static (int Remaining, int Limit, DateTimeOffset? ResetAt) ReadRateLimit(HttpResponseMessage response)
    {
        static int Header(HttpResponseMessage r, string name) => r.Headers.TryGetValues(name, out var v) && int.TryParse(v.FirstOrDefault(), out var n) ? n : 0;
        var reset = response.Headers.TryGetValues("X-RateLimit-Reset", out var values) && long.TryParse(values.FirstOrDefault(), out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix) : null;
        return (Header(response, "X-RateLimit-Remaining"), Header(response, "X-RateLimit-Limit"), reset);
    }
}
public sealed class GitHubRateLimitException(DateTimeOffset? resetAt) : Exception($"GitHub rate limit reached. Reset: {resetAt:O}") { public DateTimeOffset? ResetAt { get; } = resetAt; }
