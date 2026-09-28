using Vafadar.Finance.Core.Budgets;

namespace Vafadar.Finance.Core.Tests.Budgets;

public sealed class EnvelopeCalculatorTests
{
    [Fact]
    [Trait("Requirement", "BUD-12")]
    public void Unassigned_money_counts_every_amount_once()
    {
        // 2,000 at hand, 300 set aside for goals; food 400 assigned with 150 spent, rent 800 assigned and paid.
        var summary = EnvelopeCalculator.Summarize(2_000_00, 300_00, [new BudgetStatus(400_00, 150_00), new BudgetStatus(800_00, 800_00)]);

        Assert.Equal(250_00, summary.InEnvelopes);
        Assert.Equal(0, summary.Overspent);
        Assert.Equal(1_450_00, summary.Unassigned);
        Assert.False(summary.IsOverAssigned);
    }

    [Fact]
    public void Overspending_is_shown_but_not_subtracted_again()
    {
        // Leisure: 100 assigned, 160 spent – the extra 60 has already left the balance.
        var summary = EnvelopeCalculator.Summarize(500_00, 0, [new BudgetStatus(100_00, 160_00), new BudgetStatus(200_00, 50_00)]);

        Assert.Equal(150_00, summary.InEnvelopes);
        Assert.Equal(60_00, summary.Overspent);
        Assert.Equal(350_00, summary.Unassigned);
    }

    [Fact]
    public void Assigning_more_than_the_balance_is_reported()
    {
        var summary = EnvelopeCalculator.Summarize(300_00, 100_00, [new BudgetStatus(500_00, 0)]);

        Assert.Equal(-300_00, summary.Unassigned);
        Assert.True(summary.IsOverAssigned);

        // Refunds larger than spending leave more in the envelope than was assigned.
        Assert.Equal(120_00, EnvelopeCalculator.Summarize(0, 0, [new BudgetStatus(100_00, -20_00)]).InEnvelopes);
    }
}