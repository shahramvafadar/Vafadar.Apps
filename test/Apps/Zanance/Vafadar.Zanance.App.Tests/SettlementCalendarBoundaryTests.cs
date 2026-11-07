using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original bill construction and explicit inclusive periods accept representable first-calendar dates without writing money.</summary>
public sealed class SettlementCalendarBoundaryTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 31)]
    public void First_year_clock_keeps_the_default_period_within_DateOnly(int month, int day)
    {
        using var f = new FlowFixture();
        f.Time.Now = new DateTimeOffset(1, month, day, 9, 0, 0, TimeSpan.Zero);
        var model = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        Assert.Equal(DateOnly.MinValue, model.From);
        Assert.Equal(new DateOnly(1, month, day), model.To);
        Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public void First_supported_editor_day_does_not_request_an_out_of_range_default()
    {
        using var f = new FlowFixture();
        f.Time.Now = new DateTimeOffset(1900, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var model = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        Assert.Equal(Vafadar.Localization.CalendarDates.MinDate, model.From);
        Assert.Equal(Vafadar.Localization.CalendarDates.MinDate, model.To);
        Assert.Empty(f.Platform.Routes);
    }

    [Theory]
    [InlineData(1900, 12, 31, 1900, 1, 1)]
    [InlineData(1901, 1, 1, 1900, 1, 2)]
    [InlineData(2000, 2, 29, 1999, 3, 1)]
    [InlineData(2199, 12, 31, 2199, 1, 1)]
    public void Supported_editor_clock_keeps_the_trailing_period_inside_the_existing_input_range(int year, int month, int day, int fromYear, int fromMonth, int fromDay)
    {
        using var f = new FlowFixture();
        f.Time.Now = new DateTimeOffset(year, month, day, 9, 0, 0, TimeSpan.Zero);
        var model = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        Assert.Equal(new DateOnly(fromYear, fromMonth, fromDay), model.From);
        Assert.Equal(new DateOnly(year, month, day), model.To);
        Assert.Empty(f.Platform.Routes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_first_day_is_a_real_inclusive_period_not_an_unset_field(bool firstDayEnd)
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken; Guid id;
        await using (var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(ct))
        {
            var account = new Account { Name = "Fictitious first-day bill", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = DateOnly.MinValue, OpeningBalance = 0 };
            var plan = new Schedule { Name = "Fictitious first-day advances", AccountId = account.Id, Kind = EntryKind.Expense, Amount = 123,
                Rule = new() { Frequency = Frequency.Once, Start = DateOnly.MinValue } };
            db.Accounts.Add(account); db.Schedules.Add(plan);
            db.Entries.Add(new LedgerEntry { AccountId = account.Id, ScheduleId = plan.Id, Kind = EntryKind.Expense, Amount = 123, Date = DateOnly.MinValue });
            await db.SaveChangesAsync(ct); id = plan.Id;
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var model = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        await model.LoadAsync(id); model.From = DateOnly.MinValue;
        model.To = firstDayEnd ? DateOnly.MinValue : DateOnly.FromDateTime(f.Time.GetLocalNow().DateTime);
        model.ActualText = "2.34";
        Assert.True(model.Loading.IsReady); Assert.True(model.CanEdit);
        Assert.Null(model.PeriodError); Assert.Null(model.AmountError); Assert.True(model.CanSave);
        Assert.False(string.IsNullOrWhiteSpace(model.AdvancesText)); Assert.False(string.IsNullOrWhiteSpace(model.ResultText));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(model.HasCommitted); Assert.Empty(f.Platform.Routes);
        // A preview is sufficient: do not invoke Save, Undo or repeat accepted financial checks.
    }
}
