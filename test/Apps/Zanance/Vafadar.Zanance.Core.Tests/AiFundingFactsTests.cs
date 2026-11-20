using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Checks consistency of locally disclosed funding data without contacting a provider.</summary>
public sealed class AiFundingFactsTests
{
    /// <summary>The effective limit includes the signed carry.</summary>
    [Fact]
    public void Budget_limit_rejects_correct_difference_from_wrong_effective_limit()
    {
        var facts = AiFundingFacts.Limit("EUR", 1700, 1300, 1550, -400);
        AiProtocol.Validate(Packet(facts));

        var inconsistent = facts with { Baseline = 1200, Difference = 350 };
        Assert.Throws<ArgumentException>(() => AiProtocol.Validate(Packet(inconsistent)));
    }

    /// <summary>Only the original rounded amount is allowed when contribution dates exist.</summary>
    [Theory]
    [InlineData(3, 333L)]
    [InlineData(3, null)]
    public void Goal_gap_rejects_inconsistent_contribution_alternative(int dates, long? required)
    {
        var facts = AiFundingFacts.Goal("EUR", 1500, 500, 1000, dates, 200, dates > 0 ? 334 : null, 0);
        AiProtocol.Validate(Packet(facts));

        var inconsistent = facts with { RequiredContribution = required };
        Assert.Throws<ArgumentException>(() => AiProtocol.Validate(Packet(inconsistent)));
    }

    private static AiPayload Packet(AiFundingFacts facts)
    {
        var day = new DateOnly(2026, 11, 20);
        var period = new AiPeriod(day, day, 1, false, [new("EUR", 0, 0, 0, 0)], 0);
        return new AiPayload(3, AiPurpose.PeriodExplanation, "en", period, null, Funding: facts);
    }
}
