using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Verifies the bounds on settled versus planned totals in isolated synthetic reports.</summary>
public sealed class AiPlanReportContractTests
{
    /// <summary>Validates the settled subset without treating open amounts as part of its variance.</summary>
    [Theory]
    [InlineData(1201L)]
    [InlineData(-801L)]
    [InlineData(long.MinValue)]
    [InlineData(null)]
    public void Rejects_an_impossible_or_missing_settled_variance(long? variance)
    {
        var day = new DateOnly(2026, 11, 20);
        var period = new AiPeriod(day, day, 1, false, [new("EUR", 0, 0, 1200, 0)], 0);
        var row = new AiPlanDifference("EUR", EntryKind.Expense, 2000, 1200, 1, 1, 0, 200);
        var packet = new AiPayload(2, AiPurpose.PeriodExplanation, "en", period, null, new(null, [row]));
        AiProtocol.Validate(packet);
        AiProtocol.Validate(packet with { Report = new(null, [row with { Unknown = 1, Variance = null }]) });

        var inconsistent = packet with { Report = new(null, [row with { Variance = variance }]) };
        Assert.Throws<ArgumentException>(() => AiProtocol.Validate(inconsistent));
    }
}
