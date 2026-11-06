using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>Signed observed/recorded balances never wrap into a different reconciliation correction.</summary>
public sealed class ReconciliationDifferenceBoundaryTests
{
    private static readonly DateOnly Day = new(2026, 11, 6);

    [Theory]
    [InlineData(long.MinValue, long.MaxValue)]
    [InlineData(long.MaxValue, long.MinValue)]
    [InlineData(long.MinValue, 0)]
    [InlineData(1, long.MinValue)]
    [InlineData(-1, long.MaxValue)]
    public void Unrepresentable_difference_refuses_instead_of_becoming_an_adjustment(long recorded, long observed)
    {
        var result = new ReconciliationResult(recorded, observed, 0, 0);
        var account = new Account { Name = "Fictitious comparison", CurrencyCode = "EUR", OpeningBalance = recorded, OpeningDate = Day };
        Assert.Throws<OverflowException>(() => result.Difference);
        Assert.Throws<OverflowException>(() => Reconciliation.CreateAdjustment(account, result, Day, "New boundary"));
    }

    [Theory]
    [InlineData(0, long.MinValue, long.MinValue)]
    [InlineData(0, long.MaxValue, long.MaxValue)]
    [InlineData(long.MaxValue, long.MaxValue, 0)]
    [InlineData(long.MaxValue, 0, -long.MaxValue)]
    [InlineData(long.MinValue, long.MinValue, 0)]
    [InlineData(long.MinValue, -1, long.MaxValue)]
    [InlineData(-1, long.MinValue, -long.MaxValue)]
    public void Supported_signed_difference_keeps_source_and_positive_adjustment_bounds(long recorded, long observed, long expected)
    {
        var account = new Account { Name = "Fictitious comparison", CurrencyCode = "EUR", OpeningBalance = recorded, OpeningDate = Day };
        var before = JsonSerializer.Serialize(account);
        var result = Reconciliation.Analyze(account, [], observed, Day);
        Assert.Equal(expected, result.Difference);
        if (expected == long.MinValue)
            Assert.Throws<OverflowException>(() => Reconciliation.CreateAdjustment(account, result, Day, "New boundary"));
        else if (expected == 0)
            Assert.Null(Reconciliation.CreateAdjustment(account, result, Day, "New boundary"));
        else
        {
            var adjustment = Reconciliation.CreateAdjustment(account, result, Day, "New boundary")!;
            Assert.Equal(Math.Abs(expected), adjustment.Amount);
            Assert.Equal(expected > 0 ? AdjustmentDirection.Increase : AdjustmentDirection.Decrease, adjustment.Direction);
            Assert.Equal(observed, LedgerCalculator.Balance(account, [adjustment], Day));
        }
        Assert.Equal(before, JsonSerializer.Serialize(account));
    }
}
