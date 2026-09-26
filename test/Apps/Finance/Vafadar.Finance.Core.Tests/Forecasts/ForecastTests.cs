using Vafadar.Finance.Core.Forecasts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;

namespace Vafadar.Finance.Core.Tests.Forecasts;

public sealed class ForecastTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);
    private static readonly DateOnly EndOfMonth = new(2026, 10, 31);
    private readonly LedgerBuilder _ledger = new();

    [Fact]
    [Trait("AT", "AT-44")]
    public void Minimum_shows_a_shortfall_before_salary_even_when_the_month_ends_positive()
    {
        var checking = _ledger.Account("Checking", 300);
        var rent = Plan("Rent", EntryKind.Expense, checking.Id, 800, new DateOnly(2026, 10, 15));
        var salary = Plan("Salary", EntryKind.Income, checking.Id, 2_000, new DateOnly(2026, 10, 28));

        var forecast = Compute([rent, salary]).Single();

        Assert.Equal(LedgerBuilder.Minor(300), forecast.StartBalance);
        Assert.Equal(LedgerBuilder.Minor(1_500), forecast.EndBalance);
        Assert.Equal(LedgerBuilder.Minor(-500), forecast.Minimum);
        Assert.Equal(new DateOnly(2026, 10, 15), forecast.MinimumDate);
        Assert.True(forecast.GoesNegative);
        Assert.Equal(22, forecast.Path.Count);
    }

    [Fact]
    [Trait("AT", "AT-42")]
    public void An_occurrence_already_recorded_is_not_counted_again()
    {
        var checking = _ledger.Account("Checking", 1_000);
        var rent = Plan("Rent", EntryKind.Expense, checking.Id, 800, new DateOnly(2026, 10, 15));
        var paid = _ledger.Add(EntryKind.Expense, checking, 800, date: new DateOnly(2026, 10, 9));
        paid.ScheduleId = rent.Id;
        paid.OccurrenceDate = new DateOnly(2026, 10, 15);
        var settled = new OccurrenceState { ScheduleId = rent.Id, OriginalDate = new DateOnly(2026, 10, 15), Status = OccurrenceStatus.Settled, EntryId = paid.Id };

        var forecast = Compute([rent], [settled]).Single();

        Assert.Equal(LedgerBuilder.Minor(200), forecast.StartBalance);
        Assert.Equal(LedgerBuilder.Minor(200), forecast.EndBalance);
        Assert.Empty(forecast.Items);
    }

    [Fact]
    [Trait("AT", "AT-43")]
    public void An_unknown_amount_makes_the_forecast_incomplete_instead_of_zero()
    {
        var checking = _ledger.Account("Checking", 1_000);
        var bill = Plan("Electricity", EntryKind.Expense, checking.Id, null, new DateOnly(2026, 10, 20));
        bill.AmountMode = AmountMode.Unknown;

        var forecast = Compute([bill]).Single();

        Assert.True(forecast.IsIncomplete);
        Assert.Equal(1, forecast.UnknownCount);
        Assert.Null(forecast.Items.Single().Effect);
        Assert.Equal(LedgerBuilder.Minor(1_000), forecast.EndBalance);
    }

    [Fact]
    public void Overdue_occurrences_are_assumed_at_the_base_date()
    {
        var checking = _ledger.Account("Checking", 1_000);
        var phone = Plan("Phone", EntryKind.Expense, checking.Id, 30, new DateOnly(2026, 10, 5));

        var forecast = Compute([phone]).Single();

        var overdue = forecast.Items.Single();
        Assert.Equal(ForecastSource.OverduePlan, overdue.Source);
        Assert.Equal(Today, overdue.Date);
        Assert.Equal(LedgerBuilder.Minor(970), forecast.Path[0].Balance);
    }

    [Fact]
    public void Transfers_inside_the_scope_cancel_out_and_transfers_out_of_it_reduce_it()
    {
        var checking = _ledger.Account("Checking", 1_000);
        var savings = _ledger.Account("Savings", 0);
        var outside = _ledger.Account("Loan", 0);
        outside.IncludeInTotals = false;
        var inside = Plan("To savings", EntryKind.Transfer, checking.Id, 100, new DateOnly(2026, 10, 12));
        inside.ToAccountId = savings.Id;
        var leaving = Plan("Loan payment", EntryKind.Transfer, checking.Id, 200, new DateOnly(2026, 10, 13));
        leaving.ToAccountId = outside.Id;

        var forecast = Compute([inside, leaving]).Single();

        Assert.Equal(LedgerBuilder.Minor(800), forecast.EndBalance);
    }

    [Fact]
    public void Entries_recorded_for_later_dates_are_included_at_their_date()
    {
        var checking = _ledger.Account("Checking", 1_000);
        _ledger.Add(EntryKind.Expense, checking, 250, date: new DateOnly(2026, 10, 20));

        var forecast = Compute([]).Single();

        Assert.Equal(LedgerBuilder.Minor(1_000), forecast.StartBalance);
        Assert.Equal(LedgerBuilder.Minor(750), forecast.EndBalance);
        Assert.Equal(ForecastSource.FutureEntry, forecast.Items.Single().Source);
    }

    private IReadOnlyList<CurrencyForecast> Compute(IEnumerable<Schedule> plans, IEnumerable<OccurrenceState>? states = null) =>
        ForecastCalculator.Compute(_ledger.Accounts, _ledger.Entries, plans, states ?? [], Today, EndOfMonth);

    private static Schedule Plan(string name, EntryKind kind, Guid accountId, decimal? amount, DateOnly date) => new()
    {
        Name = name,
        Kind = kind,
        AccountId = accountId,
        Amount = amount is { } value ? LedgerBuilder.Minor(value) : null,
        Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = date },
    };
}
