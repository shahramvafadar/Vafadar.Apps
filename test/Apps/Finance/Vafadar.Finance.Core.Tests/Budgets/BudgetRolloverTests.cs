using Vafadar.Finance.Core.Budgets;

namespace Vafadar.Finance.Core.Tests.Budgets;

public sealed class BudgetRolloverTests
{
    private static readonly Guid Food = Guid.CreateVersion7();

    [Fact]
    public void Unspent_money_is_carried_and_a_surplus_only_rollover_never_passes_on_overspending()
    {
        var under = Month(BudgetRollover.Surplus, limit: 500_00, spent: 420_00);
        var over = Month(BudgetRollover.Surplus, limit: 500_00, spent: 560_00);

        Assert.Equal(80_00, BudgetRolloverCalculator.CarryInto([under], BudgetRollover.Surplus).Total);
        Assert.Equal(0, BudgetRolloverCalculator.CarryInto([over], BudgetRollover.Surplus).Total);
        Assert.Equal(-60_00, BudgetRolloverCalculator.CarryInto([over], BudgetRollover.SurplusAndDeficit).Total);
    }

    [Fact]
    public void Carry_accumulates_over_consecutive_months_and_stops_where_rollover_is_off()
    {
        var january = Month(BudgetRollover.None, limit: 500_00, spent: 400_00);
        var february = Month(BudgetRollover.Surplus, limit: 500_00, spent: 450_00);

        // February received 100 from January, so 150 is left for March.
        Assert.Equal(150_00, BudgetRolloverCalculator.CarryInto([january, february], BudgetRollover.Surplus).Total);

        // March without rollover starts fresh; and a February without rollover would not have received January's rest.
        Assert.Equal(0, BudgetRolloverCalculator.CarryInto([january, february], BudgetRollover.None).Total);
        var februaryWithout = february with { Rollover = BudgetRollover.None };
        Assert.Equal(50_00, BudgetRolloverCalculator.CarryInto([january, februaryWithout], BudgetRollover.Surplus).Total);
    }

    [Fact]
    public void Category_limits_carry_their_own_rest_and_refunds_can_increase_it()
    {
        var month = new BudgetMonth(
            BudgetRollover.Surplus,
            TotalLimit: null,
            TotalSpent: 0,
            CategoryLimits: new Dictionary<Guid, long> { [Food] = 300_00 },
            CategorySpent: new Dictionary<Guid, long> { [Food] = -20_00 });

        var carry = BudgetRolloverCalculator.CarryInto([month], BudgetRollover.Surplus);

        Assert.Equal(0, carry.Total);
        Assert.Equal(320_00, carry.For(Food));
        Assert.Equal(0, carry.For(Guid.CreateVersion7()));
    }

    private static BudgetMonth Month(BudgetRollover rollover, long limit, long spent) =>
        new(rollover, limit, spent, new Dictionary<Guid, long>(), new Dictionary<Guid, long>());
}
