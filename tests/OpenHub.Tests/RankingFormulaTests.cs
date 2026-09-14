using OpenHub.Application.Contracts;
using OpenHub.Application.Ranking;

namespace OpenHub.Tests;

public sealed class RankingFormulaTests
{
    [Fact]
    public void Calculate_UsesDocumentedFormula()
    {
        var now = new DateTimeOffset(2026, 9, 14, 0, 0, 0, TimeSpan.Zero);
        var score = RankingFormula.Calculate([new RankingInput(10, 3, 20, now.AddDays(-4))], now);
        Assert.Equal(20d, score, precision: 6);
    }

    [Fact]
    public void Calculate_SumsRepositories()
    {
        var now = DateTimeOffset.UtcNow;
        var score = RankingFormula.Calculate([new(1, 1, 0, null), new(2, 2, 0, null)], now);
        Assert.Equal(9d, score, precision: 6);
    }

    [Fact]
    public void Calculate_DoesNotRewardFutureTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        var score = RankingFormula.Calculate([new(0, 0, 5, now.AddMinutes(1))], now);
        Assert.Equal(5d, score, precision: 6);
    }
}
