using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Forecasts;

/// <summary>Preserves original financial kinds when explaining a saved forecast's actual cash difference.</summary>
public sealed class SnapshotDifferenceKindTests
{
    private static readonly DateOnly BaseDate = new(2026, 10, 20);
    private static readonly DateTimeOffset SavedAt = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    [Theory, Trait("AT", "AT-170")]
    [InlineData(EntryKind.Expense, false, -1000, 0)]
    [InlineData(EntryKind.Refund, false, 1000, 0)]
    [InlineData(EntryKind.Income, false, 0, 1000)]
    [InlineData(EntryKind.IncomeReversal, false, 0, -1000)]
    [InlineData(EntryKind.Transfer, false, 0, 0)]
    [InlineData(EntryKind.Adjustment, false, 0, 0)]
    [InlineData(EntryKind.Adjustment, true, 0, 0)]
    [InlineData(EntryKind.AssetPurchase, false, 0, 0)]
    [InlineData(EntryKind.AssetSale, false, 0, 0)]
    public void Later_unplanned_cash_effects_keep_their_original_financial_kind(EntryKind kind, bool decrease, long spending, long income)
    {
        var account = Account("Original scope", "EUR", 10000);
        var outside = Account("Original outside", "EUR", 5000);
        var entry = new LedgerEntry { Kind=kind, AccountId=account.Id, ToAccountId=kind==EntryKind.Transfer ? outside.Id : null,
            Amount=1000, ToAmount=kind==EntryKind.Transfer ? 1000 : null, Date=BaseDate.AddDays(1),
            Direction=decrease ? AdjustmentDirection.Decrease : AdjustmentDirection.Increase, CreatedAt=SavedAt.AddHours(2) };
        var snapshot = Snapshot([account.Id]);
        var comparison = SnapshotComparer.Compare(snapshot, [account,outside], [entry], BaseDate.AddDays(1));
        Assert.Equal(spending, comparison.UnplannedSpending);
        Assert.Equal(income, comparison.UnplannedIncome);
        Assert.Equal(entry.EffectOn(account.Id), comparison.Difference);
        Assert.Equal(kind is EntryKind.Transfer or EntryKind.Adjustment or EntryKind.AssetPurchase or EntryKind.AssetSale
            ? entry.EffectOn(account.Id) : 0, comparison.OtherMovements);
        Assert.Equal(0, comparison.PlansChanged);
        Assert.Equal(comparison.Difference, comparison.RecordedLater+comparison.UnplannedSpending+comparison.UnplannedIncome+comparison.OtherMovements+comparison.PlansChanged);
        Assert.Equal("10000,10000", snapshot.Path);
        Assert.Equal(1000, entry.Amount);
    }

    [Fact, Trait("AT", "AT-170")]
    public void An_incoming_transfer_is_never_unplanned_income_even_with_a_legacy_mixed_currency_scope()
    {
        var eur = Account("Original EUR", "EUR", 10000); var usd = Account("Original USD", "USD", 5000);
        var entry = new LedgerEntry { Kind=EntryKind.Transfer, AccountId=usd.Id, ToAccountId=eur.Id,
            Amount=2000, ToAmount=1234, Date=BaseDate.AddDays(1), CreatedAt=SavedAt.AddHours(2) };
        var snapshot = Snapshot([eur.Id,usd.Id]);
        var comparison = SnapshotComparer.Compare(snapshot,[eur,usd],[entry],BaseDate.AddDays(1));
        Assert.Equal(1234,comparison.Difference);
        Assert.Equal(1234,comparison.OtherMovements);
        Assert.Equal(0,comparison.PlansChanged);
        Assert.Equal(0,comparison.UnplannedSpending);
        Assert.Equal(0,comparison.UnplannedIncome);
        Assert.Equal(new[] { eur.Id,usd.Id },snapshot.Scope());
    }

    [Fact, Trait("AT", "AT-170")]
    public void Mixed_financial_kinds_and_planned_changes_reconcile_without_double_counting()
    {
        var account=Account("Original mixed scope","EUR",10000); var outside=Account("Original outside","EUR",0);
        LedgerEntry Row(EntryKind kind,long amount) => new() { Kind=kind, AccountId=account.Id, Amount=amount,
            Date=BaseDate.AddDays(1), CreatedAt=SavedAt.AddHours(2) };
        var expense=Row(EntryKind.Expense,2000); var refund=Row(EntryKind.Refund,500);
        var income=Row(EntryKind.Income,1000); var reversal=Row(EntryKind.IncomeReversal,300);
        var transfer=Row(EntryKind.Transfer,700); transfer.ToAccountId=outside.Id; transfer.ToAmount=700;
        var increase=Row(EntryKind.Adjustment,200); increase.Direction=AdjustmentDirection.Increase;
        var decrease=Row(EntryKind.Adjustment,100); decrease.Direction=AdjustmentDirection.Decrease;
        var purchase=Row(EntryKind.AssetPurchase,1100); var sale=Row(EntryKind.AssetSale,1300);
        var planned=Row(EntryKind.Expense,900); planned.ScheduleId=Guid.NewGuid();
        var backdated=Row(EntryKind.Expense,400); backdated.Date=BaseDate;
        LedgerEntry[] entries=[expense,refund,income,reversal,transfer,increase,decrease,purchase,sale,planned,backdated];
        var snapshot=Snapshot([account.Id]); var original=System.Text.Json.JsonSerializer.Serialize(entries);
        var result=SnapshotComparer.Compare(snapshot,[account,outside],entries,BaseDate.AddDays(1));
        Assert.Equal((-400L,-1500L,700L,-400L,-900L,-2500L),
            (result.RecordedLater,result.UnplannedSpending,result.UnplannedIncome,result.OtherMovements,result.PlansChanged,result.Difference));
        Assert.Equal(result.Difference,result.RecordedLater+result.UnplannedSpending+result.UnplannedIncome+result.OtherMovements+result.PlansChanged);
        Assert.Equal(7500,result.Days[^1].Actual);
        Assert.Equal(original,System.Text.Json.JsonSerializer.Serialize(entries));
        Assert.Equal("10000,10000",snapshot.Path);
    }

    [Fact, Trait("AT", "AT-170")]
    public void Transfers_between_two_saved_accounts_cancel_in_all_explanation_categories()
    {
        var source=Account("First original","EUR",5000); var destination=Account("Second original","EUR",5000);
        var row=new LedgerEntry { Kind=EntryKind.Transfer, AccountId=source.Id, ToAccountId=destination.Id,
            Amount=1000,ToAmount=1000,Date=BaseDate.AddDays(1),CreatedAt=SavedAt.AddHours(2) };
        var result=SnapshotComparer.Compare(Snapshot([source.Id,destination.Id]),[source,destination],[row],BaseDate.AddDays(1));
        Assert.Equal((0L,0L,0L,0L),(result.UnplannedSpending,result.UnplannedIncome,result.OtherMovements,result.Difference));
        Assert.Equal(0,result.PlansChanged);
    }

    [Fact, Trait("AT", "AT-170")]
    public void A_backdated_transfer_is_recorded_later_only_once()
    {
        var source=Account("Original source","EUR",10000); var destination=Account("Outside","EUR",0);
        var row=new LedgerEntry { Kind=EntryKind.Transfer, AccountId=source.Id, ToAccountId=destination.Id,
            Amount=1000,ToAmount=1000,Date=BaseDate,CreatedAt=SavedAt.AddHours(2) };
        var result=SnapshotComparer.Compare(Snapshot([source.Id]),[source,destination],[row],BaseDate.AddDays(1));
        Assert.Equal(-1000,result.RecordedLater); Assert.Equal(-1000,result.Difference);
        Assert.Equal((0L,0L,0L,0L),(result.UnplannedSpending,result.UnplannedIncome,result.OtherMovements,result.PlansChanged));
    }

    [Fact, Trait("AT", "AT-170")]
    public void A_transfer_after_the_compared_horizon_is_not_an_explanation_for_today()
    {
        var source=Account("Original source","EUR",10000); var destination=Account("Outside","EUR",0);
        var row=new LedgerEntry { Kind=EntryKind.Transfer, AccountId=source.Id, ToAccountId=destination.Id,
            Amount=1000,ToAmount=1000,Date=BaseDate.AddDays(2),CreatedAt=SavedAt.AddHours(2) };
        var result=SnapshotComparer.Compare(Snapshot([source.Id]),[source,destination],[row],BaseDate.AddDays(1));
        Assert.Equal((0L,0L,0L,0L),(result.UnplannedSpending,result.UnplannedIncome,result.OtherMovements,result.Difference));
    }

    private static Account Account(string name, string currency, long opening) => new()
    { Name=name, CurrencyCode=currency, OpeningBalance=opening, OpeningDate=BaseDate.AddDays(-1) };

    private static ForecastSnapshot Snapshot(IEnumerable<Guid> scope) => new()
    { Name="Original saved scope", BaseDate=BaseDate, Horizon=BaseDate.AddDays(1), CurrencyCode="EUR",
        AccountIds=string.Join(',',scope), Path="10000,10000", CreatedAt=SavedAt };
}
