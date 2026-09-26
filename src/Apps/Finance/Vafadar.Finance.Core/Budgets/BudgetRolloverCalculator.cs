namespace Vafadar.Finance.Core.Budgets;

/// <summary>Limits and spending of one past budget month.</summary>
/// <param name="Rollover">The rollover setting of that month.</param>
/// <param name="TotalLimit">Its overall limit.</param>
/// <param name="TotalSpent">Net eligible expense within the budget's scope.</param>
/// <param name="CategoryLimits">Its category limits.</param>
/// <param name="CategorySpent">Net eligible expense per limited category.</param>
public sealed record BudgetMonth(
    BudgetRollover Rollover,
    long? TotalLimit,
    long TotalSpent,
    IReadOnlyDictionary<Guid, long> CategoryLimits,
    IReadOnlyDictionary<Guid, long> CategorySpent);

/// <summary>Money carried into a month.</summary>
/// <param name="Total">Carry for the overall limit.</param>
/// <param name="Categories">Carry per category limit.</param>
public sealed record BudgetCarry(long Total, IReadOnlyDictionary<Guid, long> Categories)
{
    /// <summary>Gets an empty carry.</summary>
    public static BudgetCarry None { get; } = new(0, new Dictionary<Guid, long>());

    /// <summary>Returns the carry of a category (0 when none).</summary>
    public long For(Guid categoryId) => Categories.GetValueOrDefault(categoryId);
}

/// <summary>
/// Rollover between consecutive budget months (§10.3). The rest of a month is its limit plus what it received minus
/// what was spent; the next month receives it only when that month has rollover switched on, and a surplus-only
/// rollover never passes on overspending. Carrying is a budget figure only: no money moves (BUD-11).
/// </summary>
public static class BudgetRolloverCalculator
{
    /// <summary>At most this many months are followed back.</summary>
    public const int MaxMonths = 24;

    /// <summary>
    /// Returns the carry into the month after <paramref name="previous"/>. The list holds consecutive months, oldest
    /// first, ending with the month right before; a gap without a budget ends the chain before the list starts.
    /// </summary>
    public static BudgetCarry CarryInto(IReadOnlyList<BudgetMonth> previous, BudgetRollover rollover)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (rollover == BudgetRollover.None || previous.Count == 0)
        {
            return BudgetCarry.None;
        }

        // Carry into previous[0] is zero: the month before it has no budget or lies beyond the limit.
        var carry = BudgetCarry.None;
        for (var i = 0; i < previous.Count; i++)
        {
            var month = previous[i];
            var nextRollover = i + 1 < previous.Count ? previous[i + 1].Rollover : rollover;
            carry = Next(month, carry, nextRollover);
        }

        return carry;
    }

    private static BudgetCarry Next(BudgetMonth month, BudgetCarry received, BudgetRollover nextRollover)
    {
        if (nextRollover == BudgetRollover.None)
        {
            return BudgetCarry.None;
        }

        long Pass(long rest) => nextRollover == BudgetRollover.Surplus ? Math.Max(0, rest) : rest;

        var total = month.TotalLimit is { } limit ? Pass(limit + received.Total - month.TotalSpent) : 0;
        var categories = month.CategoryLimits.ToDictionary(
            l => l.Key,
            l => Pass(l.Value + received.For(l.Key) - month.CategorySpent.GetValueOrDefault(l.Key)));
        return new BudgetCarry(total, categories);
    }
}