using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHub.Application.Abstractions;
using OpenHub.Application.Contracts;
using OpenHub.Domain.Entities;
using OpenHub.Infrastructure.Persistence;

namespace OpenHub.Api.Controllers;

[ApiController, Route("api/profiles")]
public sealed class ProfilesController(OpenHubDbContext db, ISyncQueue queue, IRankingService ranking) : ControllerBase
{
    [HttpGet("{username}")]
    public async Task<ActionResult<ProfileDto>> Get(string username, CancellationToken ct)
    {
        var profile = await db.DeveloperProfiles.AsNoTracking().Include(x => x.Repositories)
            .SingleOrDefaultAsync(x => x.Username == username.ToLower(), ct);
        return profile is null ? NotFound() : Ok(await Map(profile, ct));
    }

    [HttpGet("search")]
    public async Task<IReadOnlyList<ProfileDto>> Search([FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var profiles = await db.DeveloperProfiles.AsNoTracking().Include(x => x.Repositories)
            .Where(x => EF.Functions.ILike(x.Username, $"%{query}%") || (x.DisplayName != null && EF.Functions.ILike(x.DisplayName, $"%{query}%")))
            .OrderByDescending(x => x.Score).Take(12).ToListAsync(ct);
        var result = new List<ProfileDto>(profiles.Count);
        foreach (var profile in profiles) result.Add(await Map(profile, ct));
        return result;
    }

    [HttpPost("{username}/sync")]
    public async Task<ActionResult<SyncAccepted>> Sync(string username, CancellationToken ct)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,38})$"))
            return ValidationProblem("Invalid GitHub username.");
        await queue.QueueAsync(username, ct);
        return Accepted(new SyncAccepted(username, "Queued", DateTimeOffset.UtcNow));
    }

    private async Task<ProfileDto> Map(DeveloperProfile x, CancellationToken ct)
    {
        var languageCounts = x.Repositories.Where(r => r.PrimaryLanguage != null).GroupBy(r => r.PrimaryLanguage!)
            .Select(g => new { g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToArray();
        var total = Math.Max(1, languageCounts.Sum(x => x.Count));
        return new(x.Id, x.Username, x.DisplayName, x.AvatarUrl, x.Bio, x.TotalContributions, x.Followers,
            x.PublicRepositoryCount, x.Score, await ranking.GetRankAsync(x.Username, ct), x.ActivityBadge.ToString(),
            x.SyncStatus.ToString(), x.LastSyncedAt, languageCounts.Select(l => new LanguageShare(l.Key, l.Count * 100d / total)).ToArray());
    }
}
