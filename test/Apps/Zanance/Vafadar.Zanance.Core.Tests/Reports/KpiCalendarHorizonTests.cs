using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Supported calendar endpoints retain due plans without constructing an impossible horizon.</summary>
public sealed class KpiCalendarHorizonTests
{
    /// <summary>A forward day budget ends at the last supported day, including its outstanding payment.</summary>
    [Theory]
    [InlineData(9999, 12, 31, 30)]
    [InlineData(9999, 12, 1, 31)]
    [InlineData(2026, 11, 7, int.MaxValue)]
    public void Commitments_include_the_last_supported_day(int year, int month, int day, int days)
    {
        var account = new Account { Name = "Calendar scope", CurrencyCode = "EUR", Type = AccountType.Checking };
        var plan = Plan(account, DateOnly.MaxValue, 123);
        var state = new OccurrenceState { ScheduleId = plan.Id, OriginalDate = DateOnly.MaxValue, PaidAmount = 23 };

        var result = Assert.Single(KpiCatalog.Commitments([account], [plan], [state], new(year, month, day), days));

        Assert.Equal(100, result.Fixed);
        Assert.Equal(DateOnly.MaxValue, Assert.Single(result.Items).DueDate);
        Assert.Equal(23, state.PaidAmount);
    }

    /// <summary>The final partial calendar year includes its last day instead of hiding the final valid payment.</summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 1)]
    [InlineData(12, 31)]
    public void Yearly_includes_valid_plans_in_the_final_supported_year(int month, int day)
    {
        var account = new Account { Name = "Calendar scope", CurrencyCode = "EUR", Type = AccountType.Checking };
        var plan = Plan(account, DateOnly.MaxValue, 123);

        var result = Assert.Single(KpiCatalog.Yearly([account], [], [plan], [], new(9999, month, day)));

        Assert.Equal(123, result.Plans.Single().Total);
        Assert.Same(plan, result.Plans.Single().Schedule);
    }

    /// <summary>Builds one unpaid definition only; no ledger operation or settlement is performed.</summary>
    private static Schedule Plan(Account account, DateOnly date, long amount) => new()
    {
        Name = "Last supported payment", AccountId = account.Id, Kind = EntryKind.Expense,
        Amount = amount, AmountMode = AmountMode.Fixed,
        Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = date },
    };
}