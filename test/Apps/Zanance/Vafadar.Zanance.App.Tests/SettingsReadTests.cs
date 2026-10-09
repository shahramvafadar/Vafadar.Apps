using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual settings reads and choice captions with real SQLite and explicit availability answers, without GUI substitutes.</summary>
public sealed class SettingsReadTests
{
    [Theory, InlineData("en"), InlineData("fa"), InlineData("de"), InlineData("es"), InlineData("fr"), InlineData("it"), Trait("AT", "AT-86")]
    public void Choices_follow_the_new_language_on_the_same_open_form(string language)
    {
        using var f = new FlowFixture();
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == "de"));
        var previous = SettingsChoiceLabels.Create(f.Translator, f.Localization.CurrentCulture);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var actual = SettingsChoiceLabels.Create(f.Translator, f.Localization.CurrentCulture);
        Assert.Equal(f.Translator["Mode_Simple"], actual.Modes[0]);
        Assert.Equal(f.Translator["Mode_Advanced"], actual.Modes[1]);
        Assert.Equal(f.Translator["Theme_System"], actual.Themes[0]);
        Assert.Equal(f.Translator["Theme_Light"], actual.Themes[1]);
        Assert.Equal(f.Translator["Theme_Dark"], actual.Themes[2]);
        Assert.Equal(f.Translator["Settings_FreshnessNever"], actual.Freshness[3]);
        Assert.Equal(f.Translator["Settings_MonthStartCalendar"], actual.StartDays[0]);
        Assert.Equal(f.Translator["Settings_PerWeek"], actual.EssentialPeriods[1]);
        if (language != "de") { Assert.NotEqual(previous.Themes[0], actual.Themes[0]); }
    }

    [Fact, Trait("AT", "AT-86")]
    public async Task The_form_stays_covered_until_late_device_availability_and_notification_reads_finish()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        await f.Store.GetSettingsAsync(ct);
        var device = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var notifications = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var enteredDevice = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enteredNotifications = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var load = new SnapshotLoadState(); SettingsSnapshot? snapshot = null; var published = false;
        var first = load.RunAsync(async () => snapshot = await SettingsSnapshot.ReadAsync(f.Store, f.Localization, f.Time,
            () => { enteredDevice.SetResult(); return device.Task; }, true,
            () => { enteredNotifications.SetResult(); return notifications.Task; }, ct), () => published = true);
        await enteredDevice.Task.WaitAsync(ct);
        Assert.False(load.IsReady); Assert.Null(snapshot); Assert.False(published);
        var duplicate = load.RunAsync(() => throw new InvalidOperationException("Duplicate reads must not start"), () => Assert.Fail("Duplicate publication"));
        Assert.Same(first, duplicate);
        device.SetResult(true); await enteredNotifications.Task.WaitAsync(ct);
        Assert.False(load.IsReady); Assert.Null(snapshot); Assert.False(published);
        notifications.SetResult(false); await first;
        Assert.True(load.IsReady); Assert.True(published); Assert.NotNull(snapshot);
        Assert.True(snapshot.LockAvailable); Assert.False(snapshot.NotificationsEnabled);
    }

    [Fact, Trait("AT", "AT-86")]
    public async Task Failed_availability_keeps_the_whole_form_covered_and_a_new_read_can_retry()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        await f.Store.GetSettingsAsync(ct); var load = new SnapshotLoadState(); SettingsSnapshot? snapshot = null;
        await Assert.ThrowsAsync<IOException>(() => load.RunAsync(async () =>
            snapshot = await SettingsSnapshot.ReadAsync(f.Store, f.Localization, f.Time,
                () => Task.FromException<bool>(new IOException("Fictitious device read failure")), false, () => Task.FromResult(false), ct),
            () => Assert.Fail("Partial preferences cannot publish")));
        Assert.False(load.IsReady); Assert.True(load.HasFailed); Assert.Null(snapshot);
        await load.RunAsync(async () => snapshot = await SettingsSnapshot.ReadAsync(f.Store, f.Localization, f.Time,
            () => Task.FromResult(false), false, () => throw new InvalidOperationException("Unsupported notifications must not be queried"), ct), () => { });
        Assert.True(load.IsReady); Assert.NotNull(snapshot); Assert.False(snapshot.LockAvailable);
    }

    [Fact, Trait("AT", "AT-86")]
    public async Task Reading_defaults_preserves_financial_rows_and_never_posts_or_saves_a_suggestion()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken; var day = new DateOnly(2026, 10, 9);
        var source = new Account { Name = "Fictitious source", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = day };
        var destination = new Account { Name = "Fictitious destination", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = day };
        var archived = new Account { Name = "Fictitious archived", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = day, IsArchived = true };
        var asset = new Account { Name = "Fictitious asset", Type = AccountType.Asset, CurrencyCode = "EUR", OpeningDate = day };
        foreach (var account in new[] { source, destination, archived, asset }) { Assert.True(await f.Store.SaveAccountAsync(account, ct)); }
        var transfer = new LedgerEntry { AccountId = source.Id, ToAccountId = destination.Id, Amount = 1234, ToAmount = 1234, Kind = EntryKind.Transfer, Date = day };
        Assert.True((await f.Store.SaveEntryAsync(transfer, ct)).Succeeded);
        await f.Store.UpdateSettingsAsync(s => { s.DefaultAccountId = destination.Id; s.MonthStartDay = 25; }, ct);
        var before = await f.Store.GetSettingsAsync(ct);
        var snapshot = await SettingsSnapshot.ReadAsync(f.Store, f.Localization, f.Time, () => Task.FromResult(false), false,
            () => throw new InvalidOperationException("No permission request/read on an unsupported platform"), ct);
        Assert.Equal(destination.Id, snapshot.Settings.DefaultAccountId); Assert.Equal(25, snapshot.Settings.MonthStartDay);
        Assert.Equal(new[] { destination.Id, source.Id }, snapshot.DefaultAccounts.Select(a => a.Id));
        var after = await f.Store.GetSettingsAsync(ct); Assert.Null(after.EssentialEstimate); Assert.Equal(before.UpdatedAt, after.UpdatedAt);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: ct); var retained = Assert.Single(entries);
        Assert.Equal((transfer.Id, transfer.Kind, transfer.Amount, transfer.ToAmount, transfer.ToAccountId),
            (retained.Id, retained.Kind, retained.Amount, retained.ToAmount, retained.ToAccountId));
        Assert.Equal(4, (await f.Store.GetAccountsAsync(cancellationToken: ct)).Count);
        Assert.Equal(10000, LedgerCalculator.TotalBalances(await f.Store.GetAccountsAsync(cancellationToken: ct), entries, day)["EUR"]);
        Assert.Empty(LedgerCalculator.Totals(await f.Store.GetAccountsAsync(cancellationToken: ct), entries, new LedgerFilter(day, day)));
    }

    [Fact, Trait("AT", "AT-86")]
    public async Task An_existing_device_lock_does_not_request_authentication_or_change_the_lock_preference()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        await f.Store.UpdateSettingsAsync(s => s.AppLockEnabled = true, ct);
        var snapshot = await SettingsSnapshot.ReadAsync(f.Store, f.Localization, f.Time,
            () => throw new InvalidOperationException("Existing lock needs no availability probe"), false, () => Task.FromResult(false), ct);
        Assert.True(snapshot.LockAvailable); Assert.True(snapshot.Settings.AppLockEnabled);
        Assert.True((await f.Store.GetSettingsAsync(ct)).AppLockEnabled);
    }
}
