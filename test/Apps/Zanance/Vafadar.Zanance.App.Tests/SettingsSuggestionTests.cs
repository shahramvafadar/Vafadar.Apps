using System.Text.Json;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual settings suggestions over complete SQLite history and the exact required date window.</summary>
public sealed class SettingsSuggestionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory, Trait("AT", "AT-111")]
    [InlineData("2026-10-09", 1, "2026-07-01", "2026-09-30")]
    [InlineData("2026-10-09", 25, "2026-06-25", "2026-09-24")]
    [InlineData("2026-01-09", 25, "2025-09-25", "2025-12-24")]
    public void The_read_window_excludes_the_current_partial_month_and_crosses_years_correctly(
        string today, int startDay, string from, string to)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Equal((DateOnly.Parse(from, culture), DateOnly.Parse(to, culture)),
            LiquidityCalculator.SuggestionRange(DateOnly.Parse(today, culture), PeriodCalendar.Gregorian, startDay));
    }

    [Theory, Trait("AT", "AT-111")]
    [InlineData(CalendarSystem.Gregorian, 1, "2026-10-09")]
    [InlineData(CalendarSystem.Gregorian, 25, "2026-10-09")]
    [InlineData(CalendarSystem.Gregorian, 28, "2026-10-09")]
    [InlineData(CalendarSystem.Persian, 1, "2026-10-09")]
    [InlineData(CalendarSystem.Persian, 25, "2026-10-09")]
    [InlineData(CalendarSystem.Persian, 28, "2026-10-09")]
    [InlineData(CalendarSystem.Hijri, 1, "2026-10-09")]
    [InlineData(CalendarSystem.Hijri, 25, "2026-10-09")]
    [InlineData(CalendarSystem.Hijri, 28, "2026-10-09")]
    [InlineData(CalendarSystem.Gregorian, 25, "2026-01-09")]
    [InlineData(CalendarSystem.Gregorian, 1, "2024-05-07")]
    public async Task Every_complete_financial_month_matches_full_history_without_saving_the_suggestion(
        CalendarSystem calendar, int startDay, string todayText)
    {
        using var f = new FlowFixture();
        var today = DateOnly.Parse(todayText, System.Globalization.CultureInfo.InvariantCulture);
        f.Time.Now = new(today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        f.Localization.SetCalendar(calendar);
        await f.Store.UpdateSettingsAsync(s =>
        {
            s.MonthStartDay = startDay; s.DefaultCurrencyCode = "USD"; s.EssentialEstimateCurrency = "EUR";
        }, Ct);
        await PopulateAsync(f, Calendars.ToPeriod(calendar), startDay, today, false);
        var accounts = await f.Store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        var originalSettings = JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));
        var originalEntries = JsonSerializer.Serialize(entries);
        var originalAccounts = JsonSerializer.Serialize(accounts);
        var full = LiquidityCalculator.SuggestPerDay(accounts, entries, "EUR", today, Calendars.ToPeriod(calendar), startDay);
        Assert.Equal(53, full);
        var snapshot = await ReadAsync(f);
        Assert.Equal(full, snapshot.EssentialSuggestion);
        Assert.Null(snapshot.Settings.EssentialEstimate);
        Assert.Equal(originalSettings, JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
        Assert.Equal(originalEntries, JsonSerializer.Serialize(await f.Store.GetEntriesAsync(cancellationToken: Ct)));
        Assert.Equal(originalAccounts, JsonSerializer.Serialize(await f.Store.GetAccountsAsync(cancellationToken: Ct)));
    }

    [Theory, InlineData(false), InlineData(true), Trait("AT", "AT-111")]
    public async Task Refunds_can_exceed_spending_and_default_currency_is_used_only_without_an_explicit_estimate_currency(bool negative)
    {
        using var f = new FlowFixture(); var today = DateOnly.FromDateTime(f.Time.Now.DateTime);
        await f.Store.UpdateSettingsAsync(s => { s.DefaultCurrencyCode = "EUR"; s.EssentialEstimateCurrency = null; }, Ct);
        await PopulateAsync(f, PeriodCalendar.Gregorian, 1, today, negative);
        Assert.Equal(negative ? -53 : 53, (await ReadAsync(f)).EssentialSuggestion);
        Assert.Null((await f.Store.GetSettingsAsync(Ct)).EssentialEstimate);
    }

    [Theory, InlineData(0), InlineData(1), InlineData(2), InlineData(3), Trait("AT", "AT-111")]
    public async Task Insufficient_or_ineligible_account_history_still_has_no_suggestion(int reason)
    {
        using var f = new FlowFixture(); var today = DateOnly.FromDateTime(f.Time.Now.DateTime);
        var range = LiquidityCalculator.SuggestionRange(today, PeriodCalendar.Gregorian);
        var account = new Account
        {
            Name = "Fictitious ineligible history", CurrencyCode = reason == 3 ? "USD" : "EUR",
            OpeningDate = reason == 0 ? range.From.AddDays(1) : range.From.AddDays(-1),
            UsableForPayments = reason != 2,
        };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        await f.Store.UpdateSettingsAsync(s => s.DefaultCurrencyCode = "EUR", Ct);
        await SaveAsync(f, new() { AccountId = account.Id, Date = range.From.AddDays(2), Kind = EntryKind.Expense, Amount = 50000 });
        if (reason == 1) { account.IsArchived = true; Assert.True(await f.Store.SaveAccountAsync(account, Ct)); }
        Assert.Null((await ReadAsync(f)).EssentialSuggestion);
    }

    [Fact, Trait("AT", "AT-111")]
    public async Task A_fresh_read_uses_changed_calendar_month_start_currency_and_clock_without_reusing_an_old_suggestion()
    {
        using var f = new FlowFixture(); var today = DateOnly.FromDateTime(f.Time.Now.DateTime);
        await f.Store.UpdateSettingsAsync(s => s.DefaultCurrencyCode = "EUR", Ct);
        await PopulateAsync(f, PeriodCalendar.Gregorian, 1, today, false);
        Assert.Equal(53, (await ReadAsync(f)).EssentialSuggestion);
        f.Time.Now = f.Time.Now.AddDays(30);
        f.Localization.SetCalendar(CalendarSystem.Persian);
        await f.Store.UpdateSettingsAsync(s => { s.MonthStartDay = 25; s.EssentialEstimateCurrency = "USD"; }, Ct);
        var before = JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));
        var accounts = await f.Store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        var expected = LiquidityCalculator.SuggestPerDay(accounts, entries, "USD", DateOnly.FromDateTime(f.Time.Now.DateTime),
            PeriodCalendar.Persian, 25);
        Assert.NotEqual(53, expected);
        Assert.Equal(expected, (await ReadAsync(f)).EssentialSuggestion);
        Assert.Equal(before, JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
    }

    private static Task<SettingsSnapshot> ReadAsync(FlowFixture f) => SettingsSnapshot.ReadAsync(f.Store, f.Localization,
        f.Time, () => Task.FromResult(false), false, () => throw new InvalidOperationException("No permission read"), Ct);

    private static async Task PopulateAsync(FlowFixture f, PeriodCalendar calendar, int startDay, DateOnly today, bool negative)
    {
        var range = LiquidityCalculator.SuggestionRange(today, calendar, startDay);
        var source = new Account { Name = "Fictitious usable", CurrencyCode = "EUR", OpeningDate = range.From.AddYears(-2), IncludeInTotals = false };
        var target = new Account { Name = "Fictitious transfer", CurrencyCode = "EUR", OpeningDate = source.OpeningDate };
        var archived = new Account { Name = "Fictitious archived", CurrencyCode = "EUR", OpeningDate = source.OpeningDate };
        var unusable = new Account { Name = "Fictitious unusable", CurrencyCode = "EUR", OpeningDate = source.OpeningDate, UsableForPayments = false };
        var foreign = new Account { Name = "Fictitious foreign", CurrencyCode = "USD", OpeningDate = source.OpeningDate };
        foreach (var account in new[] { source, target, archived, unusable, foreign }) { Assert.True(await f.Store.SaveAccountAsync(account, Ct)); }
        var current = PeriodMath.MonthOf(today, calendar, startDay);
        var month = PeriodMath.Previous(current.Year, current.Month);
        foreach (var rate in new[] { 91, 53, 17 })
        {
            var (first, last) = PeriodMath.MonthRange(month.Year, month.Month, calendar, startDay);
            var days = last.DayNumber - first.DayNumber + 1;
            // Both inclusive boundaries matter; the last-day refund materially changes the median.
            await SaveAsync(f, new() { AccountId = source.Id, Date = first, Kind = EntryKind.Expense, Amount = days * (negative ? 29 : rate + 29) });
            await SaveAsync(f, new() { AccountId = source.Id, Date = last, Kind = EntryKind.Refund, Amount = days * (negative ? rate + 29 : 29), Review = ReviewState.Unreviewed });
            await SaveAsync(f, new() { AccountId = source.Id, Date = first, Kind = EntryKind.Expense, Amount = 9000000,
                ScheduleId = Guid.NewGuid(), OccurrenceDate = first, Source = EntrySource.Schedule });
            await SaveAsync(f, new() { AccountId = source.Id, ToAccountId = target.Id, ToAmount = 9000000, Date = last, Kind = EntryKind.Transfer, Amount = 9000000 });
            await SaveAsync(f, new() { AccountId = source.Id, Date = first, Kind = EntryKind.Income, Amount = 9000000 });
            foreach (var account in new[] { archived, unusable, foreign })
            { await SaveAsync(f, new() { AccountId = account.Id, Date = first, Kind = EntryKind.Expense, Amount = 9000000 }); }
            month = PeriodMath.Previous(month.Year, month.Month);
        }
        foreach (var date in new[] { range.From.AddDays(-1), range.To.AddDays(1), today.AddYears(1) })
        { await SaveAsync(f, new() { AccountId = source.Id, Date = date, Kind = EntryKind.Expense, Amount = 9000000 }); }
        // Historical entries remain after archiving; new entries into an archived account are correctly rejected.
        archived.IsArchived = true;
        Assert.True(await f.Store.SaveAccountAsync(archived, Ct));
    }

    private static async Task SaveAsync(FlowFixture f, LedgerEntry entry) => Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
}
