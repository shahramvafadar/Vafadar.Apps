using Microsoft.EntityFrameworkCore;
using Vafadar.Backup;
using Vafadar.Core.Hosting;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original read-only budget actions and review rows at the first and last representable periods.</summary>
public sealed class BudgetReviewCalendarBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Valid source periods retain publication even when a neighboring copy target cannot exist.</summary>
    [Theory]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Gregorian, true)]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Persian, true)]
    [InlineData(BudgetPeriod.Month, PeriodCalendar.Hijri, true)]
    [InlineData(BudgetPeriod.Week, PeriodCalendar.Gregorian, false)]
    [InlineData(BudgetPeriod.Week, PeriodCalendar.Gregorian, true)]
    [InlineData(BudgetPeriod.TwoWeeks, PeriodCalendar.Gregorian, false)]
    [InlineData(BudgetPeriod.TwoWeeks, PeriodCalendar.Gregorian, true)]
    public async Task Unavailable_adjacent_period_never_becomes_a_copy_target(BudgetPeriod period, PeriodCalendar calendar, bool last)
    {
        using var f = new FlowFixture();
        var end = PeriodMath.MonthOf(DateOnly.MaxValue, calendar);
        var budget = new Budget { Year = period == BudgetPeriod.Month ? end.Year : 0, Month = period == BudgetPeriod.Month ? end.Month : 0,
            Calendar = calendar, Period = period, PeriodStart = last ? DateOnly.MaxValue : DateOnly.MinValue, TotalLimit = 123, CurrencyCode = "EUR" };
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        { db.Budgets.Add(budget); await db.SaveChangesAsync(Ct); }
        var review = await f.Store.GetBudgetEditorReviewAsync(Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var actions = new BudgetActionsViewModel(f.Store, f.Translator, new DateFormatter(f.Localization), f.Platform, _ => Task.CompletedTask);
        actions.Publish(review, new(budget.Year, budget.Month, calendar, period, budget.PeriodStart, "EUR"), budget.Id);
        Assert.True(actions.CanInteract);
        if (last) Assert.False(actions.CopyToNextCommand.CanExecute(null));
        else Assert.False(actions.CopyPreviousCommand.CanExecute(null));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Empty(f.Platform.Confirmations); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Reconciliation attention preserves its strict elapsed-day boundary without requiring a date before time.</summary>
    [Theory]
    [InlineData(PeriodCalendar.Gregorian, 2, 0)]
    [InlineData(PeriodCalendar.Gregorian, 3, 0)]
    public Task Original_review_keeps_earliest_completed_period(PeriodCalendar calendar, int month, int expected) =>
        CheckReviewAsync(calendar, month, expected);

    /// <summary>These already accepted later/other-calendar counter-cases retain their original source contract.</summary>
    [Theory]
    [InlineData(PeriodCalendar.Gregorian, 4, 1)]
    [InlineData(PeriodCalendar.Persian, 2, 1)]
    [InlineData(PeriodCalendar.Hijri, 2, 1)]
    public Task Original_review_later_period_counter_cases(PeriodCalendar calendar, int month, int expected) =>
        CheckReviewAsync(calendar, month, expected);

    /// <summary>The shared strict cutoff remains exclusive at60days and never flags a future reconciliation.</summary>
    [Theory]
    [InlineData(29, 0)]
    [InlineData(28, 1)]
    [InlineData(100, 0)]
    public Task Reconciliation_retains_exact_elapsed_day_semantics(int lastReconciledDay, int expected) =>
        CheckReviewAsync(PeriodCalendar.Gregorian, 4, expected, lastReconciledDay);

    /// <summary>Missing neighbors do not legalize an invalid current period or an unsupported navigation step.</summary>
    [Fact]
    public void Adjacent_periods_preserve_strict_current_period_validation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BudgetActionsViewModel.Adjacent(new(0, 1, PeriodCalendar.Gregorian, BudgetPeriod.Month, default, "EUR"), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BudgetActionsViewModel.Adjacent(new(1, 1, PeriodCalendar.Gregorian, BudgetPeriod.Month, default, "EUR"), 0, 1));
    }

    private static async Task CheckReviewAsync(PeriodCalendar calendar, int month, int expected, int? lastReconciledDay = null)
    {
        using var f = new FlowFixture();
        var day = PeriodMath.ToDate(1, month, 1, calendar);
        f.Time.Now = new DateTimeOffset(day.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        f.Localization.SetCalendar(calendar switch { PeriodCalendar.Persian => CalendarSystem.Persian, PeriodCalendar.Hijri => CalendarSystem.Hijri, _ => CalendarSystem.Gregorian });
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        { db.Accounts.Add(new Account { Name = "Epoch review source", CurrencyCode = "EUR", OpeningDate = DateOnly.MinValue, OpeningBalance = 0, LastReconciledOn = lastReconciledDay is { } dayNumber ? DateOnly.FromDayNumber(dayNumber) : null }); await db.SaveChangesAsync(Ct); }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var backup = new BackupService([], new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new(0, 1, 0), "Fictitious device", "Tests"), f.Preferences, f.Time, new());
        var model = new PeriodReviewViewModel(f.Store, backup, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, f.Platform);
        model.SetActive(true);
        try
        {
            await model.LoadAsync(); Assert.True(model.Loading.IsReady); Assert.True(model.CanInteract); Assert.Equal(5, model.Steps.Count);
            Assert.Equal(f.Translator.Format("Review_ReconcileDetail", expected), Assert.Single(model.Steps, row => row.Step == ReviewStep.Reconcile).Detail);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(f.Platform.Routes);
        }
        finally { model.SetActive(false); }
    }
}
