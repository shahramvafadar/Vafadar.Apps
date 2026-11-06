using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Completed independent currency reads retain valid cancellation and refuse genuine final overflow.</summary>
public sealed class CompletedCurrencyReadTests
{
    private static readonly DateOnly Day = new(2026,11,6);
    private static readonly DateTimeOffset SavedAt = new(2026,11,6,9,0,0,TimeSpan.Zero);
    private static readonly int[][] Orders = [[0,1,2],[0,2,1],[1,0,2],[1,2,0],[2,0,1],[2,1,0]];
    /// <summary>Both signed boundaries in every independent source order.</summary>
    public static IEnumerable<object[]> SignedOrders() { for(var i=0;i<6;i++)foreach(var negative in new[]{false,true})yield return [i,negative]; }
    private static Account Account(long opening) => new(){Name="Fictitious completed currency",Type=AccountType.Checking,CurrencyCode="EUR",OpeningDate=Day.AddMonths(-6),OpeningBalance=opening};
    private static ForecastSnapshot Snapshot(IEnumerable<Account> accounts, string path) => new(){Name="Fictitious saved comparison",CurrencyCode="EUR",BaseDate=Day,Horizon=Day.AddDays(path.Split(',').Length-1),Path=path,AccountIds=string.Join(',',accounts.Select(a=>a.Id)),CreatedAt=SavedAt};
    private static LedgerEntry Adjustment(Account account,long amount,bool decrease,DateOnly date,bool later=true) => new(){AccountId=account.Id,Kind=EntryKind.Adjustment,Direction=decrease?AdjustmentDirection.Decrease:AdjustmentDirection.Increase,Amount=amount,Date=date,CreatedAt=later?SavedAt.AddHours(1):SavedAt.AddHours(-1),Review=ReviewState.Unreviewed};
    private static string Source(params object[] values)=>JsonSerializer.Serialize(values);

    [Theory,MemberData(nameof(SignedOrders))]
    public void Independent_account_balances_finish_before_currency_bounds(int order,bool negative)
    {
        long[] values=negative?[long.MinValue,-1,1]:[long.MaxValue,1,-1];var accounts=Orders[order].Select(i=>Account(values[i])).ToArray();var before=Source(accounts);
        Assert.Equal(negative?long.MinValue:long.MaxValue,LedgerCalculator.TotalBalances(accounts,[],Day)["EUR"]);
        Assert.Equal(before,Source(accounts));
    }

    [Theory,MemberData(nameof(SignedOrders))]
    public void Review_currency_effects_finish_before_the_final_boundary(int order,bool negative)
    {
        var accounts=new[]{Account(0),Account(0),Account(0)};
        var entries=new[]{Adjustment(accounts[0],long.MaxValue,negative,Day),Adjustment(accounts[1],2,negative,Day),Adjustment(accounts[2],2,!negative,Day)};
        var ordered=Orders[order].Select(i=>entries[i]).ToArray();var before=Source(accounts,ordered);
        Assert.Equal(new UnreviewedSummary("EUR",3,negative?-long.MaxValue:long.MaxValue),Assert.Single(LedgerCalculator.Unreviewed(accounts,ordered)));
        Assert.Equal(before,Source(accounts,ordered));
    }

    [Theory,InlineData(false),InlineData(true)]
    public void Final_currency_overflow_still_refuses(bool negative)
    {
        var accounts=new[]{Account(negative?long.MinValue:long.MaxValue),Account(negative?-1:1)};
        Assert.Throws<OverflowException>(()=>LedgerCalculator.TotalBalances(accounts,[],Day));
        var clean=new[]{Account(0),Account(0)};
        var entries=new[]{Adjustment(clean[0],long.MaxValue,negative,Day),Adjustment(clean[1],2,negative,Day)};
        Assert.Throws<OverflowException>(()=>LedgerCalculator.Unreviewed(clean,entries));
    }

    [Theory,InlineData(false),InlineData(true)]
    public void A_period_result_outside_money_bounds_refuses(bool negative)
    {
        var result=negative?new PeriodTotals("EUR",0,long.MaxValue,long.MaxValue,0):new PeriodTotals("EUR",long.MaxValue,0,0,long.MaxValue);
        Assert.Throws<OverflowException>(()=>result.Result);
    }

    [Theory,InlineData(false),InlineData(true)]
    public void A_representable_period_result_stays_exact(bool negative)
    {
        var result=new PeriodTotals("EUR",negative?0:long.MaxValue,negative?long.MaxValue:0,long.MaxValue,long.MaxValue);
        Assert.Equal(negative?-long.MaxValue:long.MaxValue,result.Result);
    }

    [Fact]
    public void Essential_month_finishes_category_nets_before_taking_the_median()
    {
        var big=Account(long.MaxValue);var small=Account(3);
        var categories=new[]{new Category{Name="Fictitious principal consumption",Kind=CategoryKind.Expense,IsEssential=true},new Category{Name="Fictitious other consumption",Kind=CategoryKind.Expense,IsEssential=true},new Category{Name="Fictitious earlier purchase",Kind=CategoryKind.Expense,IsEssential=true}};
        var date=Day.AddMonths(-1);
        var purchase=new LedgerEntry{AccountId=small.Id,Kind=EntryKind.Expense,Amount=3,Date=Day.AddMonths(-4),CategoryId=categories[2].Id};
        var entries=new[]{purchase,new LedgerEntry{AccountId=big.Id,Kind=EntryKind.Expense,Amount=long.MaxValue,Date=date,CategoryId=categories[0].Id},new LedgerEntry{AccountId=small.Id,Kind=EntryKind.Expense,Amount=1,Date=date,CategoryId=categories[1].Id},new LedgerEntry{AccountId=small.Id,Kind=EntryKind.Refund,Amount=1,Date=date.AddDays(1),CategoryId=categories[2].Id,RefundOfId=purchase.Id}};
        var before=Source(big,small,categories,entries);var result=KpiCatalog.Coverage([big,small],entries,categories,[],"EUR",Day,PeriodCalendar.Gregorian);
        Assert.Equal(CoverageStatus.NotAvailable,result.Status);Assert.Equal(0,result.MonthlyEssential);Assert.Equal(0,result.UsableMoney);
        Assert.Equal(before,Source(big,small,categories,entries));
    }

    [Theory,InlineData(0),InlineData(1),InlineData(2),InlineData(3),InlineData(4),InlineData(5)]
    public void Saved_comparison_finishes_independent_opening_balances(int order)
    {
        long[] values=[long.MaxValue,1,-1];var accounts=Orders[order].Select(i=>Account(values[i])).ToArray();var saved=Snapshot(accounts,long.MaxValue.ToString(CultureInfo.InvariantCulture));var before=Source(accounts,saved);
        var result=SnapshotComparer.Compare(saved,accounts,[],Day);
        Assert.Equal(long.MaxValue,Assert.Single(result.Days).Actual);Assert.Equal(0,result.Difference);Assert.Equal(0,result.PlansChanged);
        Assert.Equal(before,Source(accounts,saved));
    }

    private static (Account[] Accounts,LedgerEntry[] Entries) Cash(int bucket,DateOnly date)
    {
        var accounts=new[]{Account(0),Account(0),Account(0)};
        var a=Adjustment(accounts[0],long.MaxValue,false,date);var b=Adjustment(accounts[1],2,false,date);var c=Adjustment(accounts[2],2,true,date);
        if(bucket==0){a.Kind=EntryKind.Expense;b.Kind=EntryKind.Expense;c.Kind=EntryKind.Refund;c.AccountId=accounts[0].Id;c.RefundOfId=a.Id;}
        if(bucket==1){a.Kind=EntryKind.Income;b.Kind=EntryKind.Income;c.Kind=EntryKind.IncomeReversal;c.AccountId=accounts[0].Id;c.RefundOfId=a.Id;}
        return (accounts,[a,b,c]);
    }

    [Theory,InlineData(0),InlineData(1),InlineData(2)]
    public void Saved_comparison_keeps_completed_cash_categories_and_daily_net(int bucket)
    {
        var (accounts,entries)=Cash(bucket,Day.AddDays(1));var saved=Snapshot(accounts,"0,0");var before=Source(accounts,entries,saved);
        var result=SnapshotComparer.Compare(saved,accounts,entries,Day.AddDays(1));var net=bucket==0?-long.MaxValue:long.MaxValue;
        Assert.Equal(net,result.Days[^1].Actual);Assert.Equal(net,result.Difference);Assert.Equal(0,result.PlansChanged);
        Assert.Equal(bucket==0?net:0,result.UnplannedSpending);Assert.Equal(bucket==1?net:0,result.UnplannedIncome);Assert.Equal(bucket==2?net:0,result.OtherMovements);
        Assert.Equal(before,Source(accounts,entries,saved));
    }

    [Fact]
    public void Saved_comparison_keeps_the_completed_later_recording_net()
    {
        var (accounts,entries)=Cash(1,Day);var saved=Snapshot(accounts,"0");var before=Source(accounts,entries,saved);
        var result=SnapshotComparer.Compare(saved,accounts,entries,Day);Assert.Equal(long.MaxValue,result.RecordedLater);Assert.Equal(long.MaxValue,result.Difference);Assert.Equal(0,result.PlansChanged);
        Assert.Equal(before,Source(accounts,entries,saved));
    }

    [Theory,InlineData(false),InlineData(true)]
    public void An_unrepresentable_actual_day_refuses_instead_of_wrapping(bool negative)
    {
        var accounts=new[]{Account(0),Account(0)};var saved=Snapshot(accounts,"0,0");var entries=new[]{Adjustment(accounts[0],long.MaxValue,negative,Day.AddDays(1),false),Adjustment(accounts[1],negative?2:1,negative,Day.AddDays(1),false)};
        Assert.Throws<OverflowException>(()=>SnapshotComparer.Compare(saved,accounts,entries,Day.AddDays(1)));
    }

    [Theory,InlineData(false),InlineData(true)]
    public void An_unrepresentable_saved_difference_refuses(bool negative)
    {
        var result=new SnapshotDay(Day,negative?long.MaxValue:long.MinValue,negative?long.MinValue:long.MaxValue);
        Assert.Throws<OverflowException>(()=>result.Difference);
    }

    [Theory,InlineData(false),InlineData(true)]
    public void An_unrepresentable_residual_refuses(bool negative)
    {
        var result=new SnapshotComparison([new SnapshotDay(Day,0,negative?0:long.MaxValue)],negative?long.MaxValue:-1,negative?2:0,0);
        Assert.Throws<OverflowException>(()=>result.PlansChanged);
    }

    [Fact]
    public void A_representable_residual_uses_the_complete_subtraction()
    {
        var result=new SnapshotComparison([new SnapshotDay(Day,0,-1)],long.MaxValue,1,-1);
        Assert.Equal(long.MinValue,result.PlansChanged);
    }

    [Fact]
    public void Saved_scope_still_excludes_another_currency_and_preopening_effects()
    {
        var account=Account(100);var future=Account(long.MaxValue);future.OpeningDate=Day.AddDays(10);var dollar=Account(long.MaxValue);dollar.CurrencyCode="USD";var accounts=new[]{account,future,dollar};var saved=Snapshot(accounts,"100");var entry=Adjustment(future,3,false,Day);
        var result=SnapshotComparer.Compare(saved,accounts,[entry],Day);Assert.Equal(100,Assert.Single(result.Days).Actual);Assert.Equal(0,result.RecordedLater);Assert.Equal(0,result.PlansChanged);
    }
}
