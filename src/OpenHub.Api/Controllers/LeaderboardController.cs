using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHub.Application.Abstractions;
using OpenHub.Application.Contracts;
using OpenHub.Infrastructure.Persistence;

namespace OpenHub.Api.Controllers;

[ApiController, Route("api/leaderboard")]
public sealed class LeaderboardController(IRankingService ranking, OpenHubDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PageResult<LeaderboardEntry>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var ranked = await ranking.GetLeaderboardAsync(page, pageSize, ct);
        var names = ranked.Items.Select(x => x.Username).ToArray();
        var profiles = await db.DeveloperProfiles.AsNoTracking().Where(x => names.Contains(x.Username)).ToDictionaryAsync(x => x.Username, ct);
        var start = (page - 1L) * pageSize;
        var items = ranked.Items.Select((x, i) =>
        {
            profiles.TryGetValue(x.Username, out var p);
            return new LeaderboardEntry(start + i + 1, x.Username, p?.DisplayName, p?.AvatarUrl ?? "", x.Score, p?.ActivityBadge.ToString() ?? "Inactive");
        }).ToArray();
        return new(items, ranked.Page, ranked.PageSize, ranked.Total);
    }
}
