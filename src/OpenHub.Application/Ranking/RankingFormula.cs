using OpenHub.Application.Contracts;

namespace OpenHub.Application.Ranking;

public static class RankingFormula
{
    public static double Calculate(IEnumerable<RankingInput> repositories, DateTimeOffset now) =>
        repositories.Sum(repo =>
        {
            var days = repo.LastCommitAt is null ? 3650 : Math.Max(0, (now - repo.LastCommitAt.Value).TotalDays);
            return repo.Stars + (repo.Forks * 2d) + (repo.RecentCommitsLast30Days / (days + 1d));
        });
}
