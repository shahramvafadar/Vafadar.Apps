using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original settings read, validation and guarded metadata writes remain usable without representable history.</summary>
public sealed class EarliestSettingsHistoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Missing and first complete three-month windows in every supported calendar.</summary>
    public static IEnumerable<object[]> Contexts()
    {
        foreach (var calendar in Enum.GetValues<PeriodCalendar>())
        foreach (var start in new[] { 1, 28 })
        foreach (var month in new[] { 1, 3, 4 })
            yield return [calendar, start, month];
    }

    [Theory, MemberData(nameof(Contexts))]
    public async Task Settings_reads_and_validates_only_the_available_complete_window(PeriodCalendar calendar, int start, int month)
    {
        using var f = new FlowFixture();
        var today = PeriodMath.ToDate(1, month, start, calendar);
        f.Time.Now = new(today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        await f.Store.UpdateSettingsAsync(s => s.MonthStartDay = start, Ct);
        var account = new Account { Name = "Fictitious earliest settings", CurrencyCode = "EUR",
            OpeningDate = PeriodMath.ToDate(1, 1, 1, calendar) };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        var beforeAccounts = JsonSerializer.Serialize(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        var beforeEntries = JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var original = await f.Store.ReadSettingsWorkSnapshotAsync(calendar, cancellationToken: Ct);
        Assert.Single(original.Accounts); Assert.Empty(original.Entries);
        Assert.Equal(month == 4 ? 0L : (long?)null,
            LiquidityCalculator.SuggestPerDay(original.Accounts, original.Entries, "EUR", today, calendar, start));
        var range = LiquidityCalculator.AvailableSuggestionRange(today, calendar, start);
        Assert.Equal(month == 4, range is not null);
        if (range is { } actual)
        {
            Assert.Equal(PeriodMath.MonthRange(1, 1, calendar, start).First, actual.From);
            Assert.Equal(PeriodMath.MonthRange(1, 3, calendar, start).Last, actual.To);
        }
        await f.Store.ValidateSettingsWorkSnapshotAsync(original, Ct);
        var expected = original.Settings;
        await f.Store.UpdateSettingsWorkAsync(original, s => s.RateFreshnessDays = 7, () => true, Ct);
        var changed = await f.Store.GetSettingsAsync(Ct);
        expected.RateFreshnessDays = 7; expected.UpdatedAt = f.Time.Now;
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(changed));
        Assert.Equal(beforeAccounts, JsonSerializer.Serialize(await f.Store.GetAccountsAsync(cancellationToken: Ct)));
        Assert.Equal(beforeEntries, JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public async Task Missing_history_does_not_waive_original_account_reference_guards(PeriodCalendar calendar)
    {
        using var f = new FlowFixture();
        var today = PeriodMath.ToDate(1, 1, 1, calendar);
        f.Time.Now = new(today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var original = await f.Store.ReadSettingsWorkSnapshotAsync(calendar, cancellationToken: Ct);
        Assert.True(await f.Store.SaveAccountAsync(new Account { Name = "Fictitious later reference", CurrencyCode = "EUR", OpeningDate = today }, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateSettingsWorkSnapshotAsync(original, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.UpdateSettingsWorkAsync(original, s => s.RateFreshnessDays = 7, () => true, Ct));
        Assert.Equal(30, (await f.Store.GetSettingsAsync(Ct)).RateFreshnessDays);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
