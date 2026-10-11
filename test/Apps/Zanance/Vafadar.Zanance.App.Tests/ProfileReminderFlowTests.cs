using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-160: actual file-bound reminder publication and original catalogue events with complete SQLite data.</summary>
[Trait("AT", "AT-160")]
public sealed class ProfileReminderFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("inactive", 1), InlineData("free", 0), InlineData("plus", 1), InlineData("pro", 1), InlineData("unknown", 0)]
    public async Task Period_review_uses_the_original_file_paid_and_profile_rights_without_changing_rows(string rights, int expected)
    {
        await using var f = new Fixture(); await f.SeedAsync();
        if (rights != "inactive") f.Data.Enable(rights == "free" ? ProductPlan.Free : rights == "pro" ? ProductPlan.Pro : ProductPlan.Plus);
        if (rights == "unknown") f.Data.Preferences.Set("profiles.work.catalogue", "Fictitious unreadable original");
        var before = await f.RowsAsync(); await f.Service.RefreshAsync();
        var review = f.Native.Pending.Where(n => n.Link == "review").ToArray(); Assert.Equal(expected != 0, review.Length != 0);
        Assert.Equal(expected != 0, (await f.Data.Plans.GetWorkSnapshotAsync(Ct)).CanRemindPeriodReview);
        Assert.All(review, n => { Assert.Equal(9, n.NotifyAt!.Value.Hour); Assert.Equal(f.Data.Flow.Translator["Reminder_ReviewGeneric"], n.Body); Assert.False(n.CanSnooze); });
        Assert.Equal(before, await f.RowsAsync()); Assert.Empty(f.Native.Shown);
    }

    [Theory]
    [InlineData("en"), InlineData("fa"), InlineData("de"), InlineData("es"), InlineData("fr"), InlineData("it")]
    public async Task Eligible_review_preserves_original_fixed_time_generic_privacy_and_live_language(string language)
    {
        await using var f = new Fixture(); await f.SeedAsync(); f.Data.Enable(ProductPlan.Plus);
        f.Data.Flow.Localization.SetLanguage(f.Data.Flow.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var before = await f.RowsAsync(); await f.Service.RefreshAsync();
        var notification = f.Native.Pending.First(n => n.Link == "review");
        Assert.Equal(new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Local), notification.NotifyAt);
        Assert.Equal("Zanance", notification.Title); Assert.Equal(f.Data.Flow.Translator["Reminder_ReviewGeneric"], notification.Body);
        Assert.DoesNotContain("Fictitious", notification.Body); Assert.Equal(before, await f.RowsAsync());
    }

    [Fact]
    public async Task Actual_saved_catalogue_choice_withdraws_pending_plan_and_snooze_without_another_financial_action()
    {
        await using var f = new Fixture(); await f.SeedAsync(); var later = f.Data.AddOther(); f.Data.Enable(); f.Data.Choose("");
        await f.Service.RefreshAsync(); var original = Assert.Single(f.Native.Pending);
        await f.Service.SnoozeAsync(new(original, SnoozeChoice.OneHour)); Assert.NotNull(f.Data.Preferences.Get("reminders.snoozed"));
        var before = await f.RowsAsync(); f.Service.WatchChanges(f.Data.Profiles);
        f.Data.Choose(later.Id); f.Clock.Fire();
        await f.Service.PendingRefresh.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.Empty(f.Native.Pending); Assert.Null(f.Data.Preferences.Get("reminders.snoozed"));
        Assert.Equal(before, await f.RowsAsync()); Assert.False(f.Data.Profiles.Current.Id == later.Id);
    }

    [Fact]
    public async Task Original_catalogue_rename_refreshes_the_same_scoped_queue_without_changing_financial_packets()
    {
        await using var f = new Fixture(); await f.SeedAsync(); var later = f.Data.AddOther(); f.Data.Enable(); f.Data.Choose("");
        await f.Service.RefreshAsync(); var original = Assert.Single(f.Native.Pending); var before = await f.RowsAsync();
        f.Service.WatchChanges(f.Data.Profiles);
        f.Data.Profiles.Rename(later, "Fictitious corrected catalogue identity"); f.Clock.Fire();
        await f.Service.PendingRefresh.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        var current = Assert.Single(f.Native.Pending); Assert.Equal(original, current);
        Assert.Equal(f.Data.Plan.Id, Assert.Single(PlanReminderLink.Parse(current.Link)!.Targets).PlanId);
        Assert.Equal(before, await f.RowsAsync());
    }

    [Fact]
    public async Task Choice_retired_inside_native_publication_clears_only_the_reminder_queue_and_preserves_complete_money()
    {
        await using var f = new Fixture(); await f.SeedAsync(); var later = f.Data.AddOther(); f.Data.Enable(); f.Data.Choose("");
        var before = await f.RowsAsync();
        f.Native.AfterReplace = () => { f.Native.AfterReplace = null; f.Data.Choose(later.Id); return Task.CompletedTask; };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending); Assert.True(f.Native.Replacements >= 2);
        Assert.Equal(before, await f.RowsAsync());
    }

    [Fact]
    public async Task Disposing_the_watcher_cancels_delayed_reads_and_keeps_existing_native_reminders()
    {
        await using var f = new Fixture(); await f.SeedAsync(); var later = f.Data.AddOther(); f.Data.Enable(); f.Data.Choose("");
        await f.Service.RefreshAsync(); var original = f.Native.Pending.ToArray(); var replacements = f.Native.Replacements;
        var before = await f.RowsAsync(); f.Service.WatchChanges(f.Data.Profiles); f.Data.Profiles.Rename(later, "Fictitious pending rename");
        f.Service.Dispose(); f.Data.Profiles.Rename(f.Data.Profiles.All.Single(p => p.Id == later.Id), "Fictitious later rename"); Assert.Equal(0, f.Clock.Fire());
        await f.Service.RefreshAsync(); f.Service.RefreshSoon();
        Assert.Equal(replacements, f.Native.Replacements); Assert.Equal(original, f.Native.Pending); Assert.Equal(before, await f.RowsAsync());
    }

    [Fact]
    public void Watcher_registration_reads_no_device_preferences_and_does_not_accept_another_catalogue()
    {
        using var f = new Fixture(); using var foreign = new ProfileFileWorkFlowTests.Fixture();
        var original = f.Data.Profiles; var reads = f.Data.Preferences.Reads;
        f.Data.Preferences.FailRead = true; f.Data.Device.Fail = true;
        f.Service.WatchChanges(original); f.Service.WatchChanges(original);
        Assert.Equal(reads, f.Data.Preferences.Reads); Assert.Equal(0, f.Data.Device.Reads); Assert.Equal(0, f.Native.EnabledReads);
        Assert.Throws<InvalidOperationException>(() => f.Service.WatchChanges(foreign.Profiles));
        f.Service.WatchSnoozes(); f.Service.WatchSnoozes(); Assert.Equal(1, f.Native.ActionLabels);
        Assert.Equal(1, f.Native.SnoozeSubscribers); f.Service.Dispose(); Assert.Equal(0, f.Native.SnoozeSubscribers);
        Assert.Throws<ObjectDisposedException>(() => f.Service.WatchChanges(original));
    }

    [Theory, InlineData("privacy"), InlineData("review")]
    public async Task Original_review_settings_retired_inside_native_publication_cannot_leave_stale_reminders(string change)
    {
        await using var f = new Fixture(); await f.SeedAsync(); f.Data.Enable(ProductPlan.Plus);
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null;
            await f.Data.Store.UpdateSettingsAsync(s => { if (change == "privacy") s.NotificationsShowDetails = true; else s.ReviewReminderEnabled = false; }, Ct);
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending);
    }

    [Theory, InlineData("first-date"), InlineData("removed-settings"), InlineData("calendar"), InlineData("day")]
    public async Task Retired_period_inputs_display_and_day_withdraw_the_same_native_batch_without_creating_preferences(string change)
    {
        await using var f = new Fixture(); await f.SeedAsync(); f.Data.Enable(ProductPlan.Plus);
        f.Native.AfterReplace = async () =>
        {
            f.Native.AfterReplace = null;
            switch (change)
            {
                case "first-date":
                    f.Data.Account.OpeningDate = new(2026, 10, 3); await f.Data.Store.SaveAccountAsync(f.Data.Account, Ct); break;
                case "removed-settings":
                    await using (var db = await f.Data.Factory.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("DELETE FROM Settings", Ct); break;
                case "calendar": f.Data.Flow.Localization.SetCalendar(Vafadar.Localization.CalendarSystem.Persian); break;
                default: f.Clock.Now = f.Clock.Now.AddDays(1); break;
            }
        };
        await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending);
        if (change == "removed-settings")
        {
            await using var db = await f.Data.Factory.CreateDbContextAsync(Ct); Assert.Equal(0, await db.Settings.CountAsync(Ct));
        }
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Rejected_partial_native_batch_preserves_original_snooze_cache_and_remains_retryable(bool existing)
    {
        await using var f = new Fixture(); await f.SeedAsync(); f.Data.Enable();
        await f.Service.RefreshAsync(); var original = Assert.Single(f.Native.Pending);
        if (existing) await f.Service.SnoozeAsync(new(original, SnoozeChoice.OneHour));
        var cache = f.Data.Preferences.Get("reminders.snoozed"); var before = await f.RowsAsync();
        f.Native.AfterReplace = () => { f.Native.AfterReplace = null; throw new IOException("Fictitious native scheduling refusal."); };
        await f.Service.SnoozeAsync(new(original, SnoozeChoice.Tomorrow));
        Assert.Empty(f.Native.Pending); Assert.Equal(cache, f.Data.Preferences.Get("reminders.snoozed"));
        Assert.Equal(before, await f.RowsAsync()); await f.Service.RefreshAsync(); Assert.NotEmpty(f.Native.Pending);
    }

    [Fact]
    public async Task Display_change_during_preparation_cannot_mix_original_and_new_language_before_native_publication()
    {
        await using var f = new Fixture(); await f.SeedAsync(); f.Data.Enable(ProductPlan.Plus);
        f.Native.AfterLabels = () => f.Data.Flow.Localization.SetLanguage(f.Data.Flow.Localization.SupportedLanguages.First(l => l.CultureName == "de"));
        var before = await f.RowsAsync(); await f.Service.RefreshAsync(); Assert.Empty(f.Native.Pending);
        Assert.Equal(before, await f.RowsAsync());
    }

    internal sealed class Fixture : IDisposable, IAsyncDisposable
    {
        internal readonly ProfileFileWorkFlowTests.Fixture Data = new();
        internal readonly ManualClock Clock = new(); internal readonly NativeScheduler Native = new();
        internal readonly ReminderService Service;
        internal Fixture() => Service = new(Native, Data.Store, Data.Plans, Data.Goals, Data.Services.GetRequiredService<HoldingStore>(),
            Data.Preferences, Data.Flow.Translator, new DateFormatter(Data.Flow.Localization), Data.Flow.Localization, Clock,
            Data.Services.GetRequiredService<ResourceChoiceStore>(), Data.Services.GetRequiredService<BudgetChoiceStore>());
        internal async Task SeedAsync()
        {
            await Data.SeedAsync(); Data.Flow.Time.Now = Clock.Now;
            Data.Plan.Rule.Start = new(2026, 10, 10); Data.Plan.Rule.Frequency = Frequency.Once; Data.Plan.ReminderDaysBefore = 0;
            await Data.Plans.SaveScheduleAsync(Data.Plan, Ct); Data.Flow.Localization.SetCalendar(Vafadar.Localization.CalendarSystem.Gregorian);
            await Data.Store.UpdateSettingsAsync(s => { s.ReviewReminderEnabled = true; s.MonthStartDay = 10; s.NotificationsShowDetails = false; }, Ct);
        }
        internal Task<string> RowsAsync() => GoalChooserFlowTests.SnapshotAsync(Data.Flow);
        public void Dispose() { Service.Dispose(); Data.Dispose(); }
        public async ValueTask DisposeAsync() { await Service.DisposeAsync(); Data.Dispose(); }
    }

    /// <summary>An explicit native-effect port; actual planners, source, records and publication guards remain unchanged.</summary>
    internal sealed class NativeScheduler : IReminderScheduler
    {
        private EventHandler<SnoozeRequest>? _snoozes;
        public event EventHandler<string>? Tapped { add { } remove { } }
        public event EventHandler<SnoozeRequest>? SnoozeRequested { add { _snoozes += value; SnoozeSubscribers++; } remove { _snoozes -= value; SnoozeSubscribers--; } }
        internal int SnoozeSubscribers; internal int EnabledReads; internal int Replacements; internal int ActionLabels;
        internal IReadOnlyList<ReminderNotification> Pending = []; internal readonly List<ReminderNotification> Shown = [];
        internal Func<Task>? AfterReplace; internal Action? AfterLabels;
        public bool IsSupported => true;
        public void SetSnoozeActions(string oneHour, string tomorrow) { ActionLabels++; AfterLabels?.Invoke(); }
        public Task<bool> AreEnabledAsync() { EnabledReads++; return Task.FromResult(true); }
        public Task<bool> RequestPermissionAsync() => Task.FromResult(true);
        public async Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders)
        {
            Replacements++; Pending = reminders.ToArray(); if (AfterReplace is not null) await AfterReplace();
        }
        public Task ShowAsync(ReminderNotification reminder) { Shown.Add(reminder); return Task.CompletedTask; }
        public Task RemoveAsync(int notificationId) => Task.CompletedTask;
    }

    /// <summary>Controls only the injected timer boundary; it does not replace any reminder calculation or OS clock.</summary>
    internal sealed class ManualClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);
        private readonly List<ManualTimer> _timers = [];
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, dueTime); _timers.Add(timer); return timer;
        }
        internal int Fire() => _timers.ToArray().Count(timer => timer.Fire());
        private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
        {
            private bool _active = dueTime != Timeout.InfiniteTimeSpan;
            public bool Change(TimeSpan dueTime, TimeSpan period) { _active = dueTime != Timeout.InfiniteTimeSpan; return true; }
            internal bool Fire() { if (!_active) return false; _active = false; callback(state); return true; }
            public void Dispose() => _active = false;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
