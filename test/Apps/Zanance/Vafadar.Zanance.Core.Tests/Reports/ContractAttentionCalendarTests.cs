using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Home contract attention retains final supported deadlines without creating an impossible horizon.</summary>
public sealed class ContractAttentionCalendarTests
{
    /// <summary>Range exhaustion must not remove a valid deadline or change its original plan/date identity.</summary>
    [Theory]
    [InlineData(9999, 12, 31, 30)]
    [InlineData(9999, 12, 1, 31)]
    [InlineData(2026, 11, 7, int.MaxValue)]
    public void Attention_keeps_both_final_day_contract_dates(int year, int month, int day, int days)
    {
        var plan = new Schedule { Name = "Last supported contract", CancellationDeadline = DateOnly.MaxValue, ReviewDate = DateOnly.MaxValue };

        var result = ContractReminderPlanner.Upcoming([plan], new(year, month, day), days);

        Assert.Equal(2, result.Count);
        Assert.All(result, item => { Assert.Same(plan, item.Schedule); Assert.Equal(DateOnly.MaxValue, item.Date); });
        Assert.Equal([ContractDateKind.Cancellation, ContractDateKind.Review], result.Select(item => item.Kind));
    }
}