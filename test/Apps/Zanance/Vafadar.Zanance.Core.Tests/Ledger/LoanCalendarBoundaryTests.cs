using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>Loan estimates retain only installments whose dates can actually be represented.</summary>
public sealed class LoanCalendarBoundaryTests
{
    /// <summary>The available final month is useful even when the remaining principal needs later months.</summary>
    [Theory]
    [InlineData(30)]
    [InlineData(31)]
    public void A_schedule_keeps_the_final_representable_installment_without_inventing_a_payoff(int day)
    {
        var schedule = LoanCalculator.Schedule(100, 12, 40, new(9999, 12, day));
        Assert.False(schedule.PaysOff);
        Assert.True(schedule.IsDateLimited);
        Assert.Null(schedule.LastDate);
        var row = Assert.Single(schedule.Installments);
        Assert.Equal(new LoanInstallment(1, new(9999, 12, day), 40, 1, 39, 61), row);
        Assert.Equal(1, schedule.TotalInterest);
    }

    /// <summary>Month-end clamping still refers to the first date, including the last supported year.</summary>
    [Fact]
    public void The_complete_available_prefix_retains_original_month_end_dates_and_money()
    {
        var schedule = LoanCalculator.Schedule(400, 0, 100, new(9999, 10, 31));
        Assert.False(schedule.PaysOff);
        Assert.True(schedule.IsDateLimited);
        Assert.Equal([new DateOnly(9999, 10, 31), new(9999, 11, 30), new(9999, 12, 31)], schedule.Installments.Select(row => row.Date));
        Assert.Equal([300L, 200L, 100L], schedule.Installments.Select(row => row.Remaining));
        Assert.Equal(0, schedule.TotalInterest);
        Assert.Null(schedule.LastDate);
    }

    /// <summary>A payoff on the last available date remains a complete estimate.</summary>
    [Theory]
    [InlineData(100, 100, 1)]
    [InlineData(200, 100, 2)]
    public void Payoff_exactly_at_the_calendar_end_is_not_date_limited(long principal, long payment, int months)
    {
        var schedule = LoanCalculator.Schedule(principal, 0, payment, new(9999, 13 - months, 30));
        Assert.True(schedule.PaysOff);
        Assert.False(schedule.IsDateLimited);
        Assert.Equal(new DateOnly(9999, 12, 30), schedule.LastDate);
        Assert.Equal(0, schedule.Installments[^1].Remaining);
    }

    /// <summary>No monthly estimate invents a next-month installment after the calendar ends.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(31)]
    public void A_positive_debt_with_no_next_month_has_no_rows_or_interest(int day)
    {
        var schedule = LoanCalculator.ScheduleFromNextMonth(100, 12, 40, new(9999, 12, day));
        Assert.True(schedule.IsDateLimited);
        Assert.False(schedule.PaysOff);
        Assert.Empty(schedule.Installments);
        Assert.Equal(0, schedule.TotalInterest);
        Assert.Null(schedule.LastDate);
    }

    /// <summary>Cleared debt and installments unable to reduce principal retain their original outcomes.</summary>
    [Theory]
    [InlineData(0, 40, true)]
    [InlineData(-1, 40, true)]
    [InlineData(100, 1, false)]
    [InlineData(100, 0, false)]
    public void Existing_non_date_outcomes_take_precedence_at_the_final_month(long principal, long payment, bool paysOff)
    {
        var schedule = LoanCalculator.ScheduleFromNextMonth(principal, 12, payment, DateOnly.MaxValue);
        Assert.Equal(paysOff, schedule.PaysOff);
        Assert.False(schedule.IsDateLimited);
        Assert.Empty(schedule.Installments);
    }

    /// <summary>The existing 600-month cap remains distinct when both limits would be crossed next.</summary>
    [Fact]
    public void The_fifty_year_guard_does_not_become_a_calendar_warning()
    {
        var schedule = LoanCalculator.Schedule(601, 0, 1, new(9950, 1, 31));
        Assert.False(schedule.PaysOff);
        Assert.False(schedule.IsDateLimited);
        Assert.Equal(600, schedule.Installments.Count);
        Assert.Equal(DateOnly.MaxValue, schedule.Installments[^1].Date);
        Assert.Equal(1, schedule.Installments[^1].Remaining);
    }

    /// <summary>Next-month anchoring still follows the existing month-end rule.</summary>
    [Fact]
    public void Next_month_start_keeps_its_own_clamped_day()
    {
        var schedule = LoanCalculator.ScheduleFromNextMonth(300, 0, 100, new(2028, 1, 31));
        Assert.True(schedule.PaysOff);
        Assert.False(schedule.IsDateLimited);
        Assert.Equal([new DateOnly(2028, 2, 29), new(2028, 3, 29), new(2028, 4, 29)], schedule.Installments.Select(row => row.Date));
    }
}
