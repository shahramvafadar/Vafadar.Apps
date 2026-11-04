using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Read projections refuse unrepresentable money without rejecting exact cancelling intermediates.</summary>
public sealed class MonetaryReadOverflowTests
{
    private static readonly DateOnly Day = new(2026, 11, 4);
    private static Account Account(long opening = 0, AccountType type = AccountType.Checking) => new()
    { Name = "Money boundary", CurrencyCode = "EUR", Type = type, OpeningDate = Day.AddDays(-1), OpeningBalance = opening };
    private static LedgerEntry Entry(Account account, EntryKind kind, long amount) => new()
    { AccountId = account.Id, Kind = kind, Amount = amount, Date = Day };
    private static WealthChange Change(string currency = "EUR", long start = 0, long end = 0, long flows = 0, long price = 0, long corrections = 0) =>
        new(currency, Day, Day.AddDays(2), start, end, flows, price, 0, corrections, []);
    private static RateTable UnitRates() => new([
        new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 1, Date = Day },
        new ExchangeRate { FromCurrencyCode = "CHF", ToCurrencyCode = "EUR", Rate = 1, Date = Day }]);

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void Every_forecast_day_must_fit_even_if_a_later_day_recovers(bool negative, bool recovers)
    {
        var account = Account(negative ? long.MinValue : long.MaxValue);
        var first = Entry(account, negative ? EntryKind.Expense : EntryKind.Income, 1);first.Date = Day.AddDays(1);
        var entries = new List<LedgerEntry> { first };
        if (recovers) { var reverse = Entry(account, negative ? EntryKind.Income : EntryKind.Expense, 1);reverse.Date = Day.AddDays(2);entries.Add(reverse); }
        Assert.Throws<OverflowException>(() => ForecastCalculator.Compute([account], entries, [], [], Day, Day.AddDays(2)));
        Assert.Equal(negative ? long.MinValue : long.MaxValue, account.OpeningBalance);
        Assert.Equal(1, first.Amount);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public void Movement_subtotals_cannot_wrap_while_original_account_balances_still_fit(int shape)
    {
        var account = Account();var other = Guid.NewGuid();
        var positive = shape is 0 or 1 or 5 or 6 or 8;
        var kind = shape switch { 0 => EntryKind.Income, 1 => EntryKind.Refund, 2 => EntryKind.Expense,
            3 => EntryKind.IncomeReversal, 4 or 5 => EntryKind.Transfer, 6 or 7 => EntryKind.Adjustment,
            8 => EntryKind.AssetSale, _ => EntryKind.AssetPurchase };
        var first = Entry(account, kind, long.MaxValue);var last = Entry(account, kind, positive ? 1 : 2);
        if (shape is 4 or 5)
        {
            foreach (var entry in new[] { first, last })
            { entry.ToAmount = entry.Amount;entry.ToAccountId = shape == 4 ? other : account.Id;entry.AccountId = shape == 4 ? account.Id : other; }
        }
        if (shape is 6 or 7)foreach (var entry in new[] { first, last })entry.Direction = shape == 6 ? AdjustmentDirection.Increase : AdjustmentDirection.Decrease;
        var offset = Entry(account, positive ? EntryKind.Expense : EntryKind.Income, long.MaxValue);
        LedgerEntry[] entries = [first, offset, last];
        Assert.Equal(positive ? 1 : -2, LedgerCalculator.Balance(account, entries, Day));
        Assert.Throws<OverflowException>(() => ReportCalculator.AccountMovements([account], entries, Day, Day));
        Assert.Equal(long.MaxValue, first.Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Planned_and_settled_totals_refuse_unrepresentable_money(bool actual)
    {
        var plan = new Schedule { Name = "Two dates", AccountId = Guid.NewGuid(), Kind = EntryKind.Expense,
            AmountMode = AmountMode.Estimated, Amount = actual ? 1 : long.MaxValue,
            Rule = new RecurrenceRule { Start = Day, Frequency = Frequency.Daily } };
        LedgerEntry[] entries = actual ? [new() { AccountId = plan.AccountId, Kind = EntryKind.Expense, Amount = long.MaxValue, Date = Day, ScheduleId = plan.Id, OccurrenceDate = Day },
            new() { AccountId = plan.AccountId, Kind = EntryKind.Expense, Amount = 1, Date = Day.AddDays(1), ScheduleId = plan.Id, OccurrenceDate = Day.AddDays(1) }] : [];
        var states = entries.Select(e => new OccurrenceState { ScheduleId = plan.Id, OriginalDate = e.OccurrenceDate!.Value, Status = OccurrenceStatus.Settled, EntryId = e.Id }).ToArray();
        Assert.Throws<OverflowException>(() => ReportCalculator.PlanVsActual([plan], states, entries, Day, Day.AddDays(1), Day.AddDays(2)));
    }

    [Theory]
    [InlineData(0, false)] [InlineData(0, true)] [InlineData(1, false)] [InlineData(1, true)]
    [InlineData(2, false)] [InlineData(2, true)] [InlineData(3, false)] [InlineData(3, true)]
    public void Derived_report_values_refuse_out_of_range_results(int shape, bool negative)
    {
        var edge = negative ? long.MinValue : long.MaxValue;var opposite = negative ? 1 : -1;
        Assert.Throws<OverflowException>(() =>
        {
            _ = shape switch
            {
                0 => new AccountMovement(Guid.Empty, "EUR", opposite, 0, 0, 0, 0, 0, 0, 0, 0, edge).Change,
                1 => new MonthTotals(2026, 11, Day, Day, edge, opposite, false).Result,
                2 => Change(start: opposite, end: edge).Change,
                _ => new WealthTotal("EUR", edge, negative ? 0 : 1, 0, 0, negative ? 1 : 0).Total,
            };
        });
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Wealth_remainder_refuses_an_out_of_range_result(bool negative) =>
        Assert.Throws<OverflowException>(() => Change(end: negative ? long.MinValue : long.MaxValue, flows: negative ? 1 : -1).Remainder);

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Wide_read_expressions_keep_representable_cancellation(int shape)
    {
        var actual = shape switch
        {
            0 => new WealthTotal("EUR", long.MaxValue, long.MaxValue, 0, 0, long.MaxValue).Total,
            1 => Change(start: -1, end: long.MaxValue, flows: 1).Remainder,
            _ => WealthHistory.Convert([Change(start: long.MaxValue), Change("USD", start: 1), Change("CHF", start: -1)], UnitRates(), "EUR").Change.Start,
        };
        Assert.Equal(long.MaxValue, actual);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Composition_does_not_hide_wrapped_slices_or_debts(bool debt)
    {
        var group = debt ? WealthGroup.Debts : WealthGroup.Money;
        var result = new NetWorthResult(Day, [], [new WealthPart(group, Guid.NewGuid(), "First", "EUR", long.MaxValue), new WealthPart(group, Guid.NewGuid(), "Second", "EUR", 1)], []);
        Assert.Throws<OverflowException>(() => NetWorthCalculator.Compose(result, new RateTable([]), "EUR"));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Unrepresentable_debt_magnitude_is_not_zero_or_negative(int shape) =>
        Assert.Throws<OverflowException>(() =>
        {
            if (shape == 0) _ = LoanCalculator.Outstanding(AccountType.Loan, long.MinValue);
            else _ = NetWorthCalculator.Compute([Account(long.MinValue, shape == 1 ? AccountType.Loan : AccountType.CreditCard)], [], [], [], [], Day);
        });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Representable_signed_balances_keep_their_values(bool debt)
    {
        var amount = debt ? LoanCalculator.Outstanding(AccountType.Loan, long.MinValue + 1)
            : Assert.Single(NetWorthCalculator.Compute([Account(long.MinValue)], [], [], [], [], Day).Totals).Total;
        Assert.Equal(debt ? long.MaxValue : long.MinValue, amount);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void Converted_wealth_components_refuse_overflow(int field)
    {
        WealthChange Part(string currency, long amount) => field switch
        { 0 => Change(currency, start: amount), 1 => Change(currency, end: amount), 2 => Change(currency, flows: amount),
            3 => Change(currency, price: amount), _ => Change(currency, corrections: amount) };
        Assert.Throws<OverflowException>(() => WealthHistory.Convert([Part("EUR", long.MaxValue), Part("USD", 1)], UnitRates(), "EUR"));
    }

    [Fact]
    public void Converted_exchange_effect_cannot_wrap_even_when_start_totals_fit()
    {
        var rates = new RateTable([
            new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 1, Date = Day },
            new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 0.0000000000000000000000000001m, Date = Day.AddDays(2) },
            new ExchangeRate { FromCurrencyCode = "CHF", ToCurrencyCode = "EUR", Rate = 0.0000000000000000000000000001m, Date = Day },
            new ExchangeRate { FromCurrencyCode = "CHF", ToCurrencyCode = "EUR", Rate = 1, Date = Day.AddDays(2) }]);
        Assert.Throws<OverflowException>(() => WealthHistory.Convert([Change("USD", start: long.MaxValue), Change("CHF", start: -long.MaxValue)], rates, "EUR"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Wealth_explanation_components_cannot_wrap_behind_a_representable_change(bool price)
    {
        if (!price)
        {
            var money = Account(long.MaxValue);money.OpeningDate = Day;var asset = Account(1, AccountType.Asset);asset.OpeningDate = Day;
            var expense = Entry(money, EntryKind.Expense, 2);expense.Date = Day.AddDays(1);
            Assert.Throws<OverflowException>(() => WealthHistory.Explain([money, asset], [expense], [], [], [], Day.AddDays(-1), Day.AddDays(2)));
        }
        else
        {
            var first = new AssetType { Name = "First holding", PriceCurrencyCode = "EUR" };var second = new AssetType { Name = "Second holding", PriceCurrencyCode = "EUR" };
            AssetEvent[] events = [new() { AssetTypeId = first.Id, Kind = AssetEventKind.Opening, Quantity = 1_000_000, Date = Day },
                new() { AssetTypeId = second.Id, Kind = AssetEventKind.Opening, Quantity = 1_000_000, Date = Day },
                new() { AssetTypeId = first.Id, Kind = AssetEventKind.Outflow, Quantity = 1_000_000, Date = Day.AddDays(2) }];
            var values = new[] { first, second }.SelectMany(type => new[] {
                new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", PricePerUnitMilli = 1, Date = Day },
                new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", PricePerUnitMilli = long.MaxValue, Date = Day.AddDays(1) } }).ToArray();
            Assert.Throws<OverflowException>(() => WealthHistory.Explain([], [], [first, second], events, values, Day, Day.AddDays(2)));
        }
    }
}
