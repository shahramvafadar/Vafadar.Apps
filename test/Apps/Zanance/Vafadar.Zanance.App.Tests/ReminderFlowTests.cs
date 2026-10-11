using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-142: actual reminder orchestration and lock routing with isolated SQLite and explicit native effects.</summary>
[Trait("AT", "AT-142")]
public sealed class ReminderFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Theory]
    [InlineData("missing", 0), InlineData("one", 1), InlineData("empty", 0), InlineData("plus", 6), InlineData("inactive", 6)]
    public async Task Actual_delivery_filters_before_grouping_without_modifying_complete_financial_rows(string access, int count)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); SetAccess(f, s, access);
        var before = await RowsAsync(f); await s.Service.RefreshAsync();
        var targets = s.Native.Pending.Where(n => n.CanSnooze).SelectMany(n => PlanReminderLink.Parse(n.Link)!.Targets).ToList();
        Assert.Equal(count, targets.Count); Assert.All(targets, t => Assert.Equal(Day, t.OriginalDate));
        Assert.All(s.Native.Pending, n => Assert.Equal(f.Translator["Reminder_Generic"], n.Body));
        Assert.Equal(before, await RowsAsync(f)); Assert.Empty(s.Native.Shown);
    }

    [Theory]
    [InlineData("missing", 0), InlineData("one", 0), InlineData("plus", 12), InlineData("inactive", 12)]
    public async Task Retained_contract_fields_require_separate_paid_rights_for_actual_notification_delivery(string access, int count)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); SetAccess(f, s, access);
        await SqlAsync(f, "UPDATE Schedules SET CancellationDeadline='2026-10-12', ReviewDate='2026-10-13'");
        var before = await RowsAsync(f); await s.Service.RefreshAsync();
        Assert.Equal(count, s.Native.Pending.Count(n => PlanReminderLink.Parse(n.Link)!.IsContract));
        Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("settled"), InlineData("skipped"), InlineData("paused"), InlineData("ended"), InlineData("disabled"), InlineData("removed"), InlineData("rule")]
    public async Task Snooze_rechecks_actual_original_members_and_never_adds_new_group_members(string change)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await s.Service.RefreshAsync();
        var original = Assert.Single(s.Native.Pending); Assert.Equal(6, PlanReminderLink.Parse(original.Link)!.Targets.Count);
        var changed = s.Plans[0];
        if (change is "settled" or "skipped")
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            db.OccurrenceStates.Add(new() { ScheduleId = changed.Id, OriginalDate = Day,
                Status = change == "settled" ? OccurrenceStatus.Settled : OccurrenceStatus.Skipped });
            await db.SaveChangesAsync(Ct);
        }
        else
        {
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            var stored = await db.Schedules.SingleAsync(p => p.Id == changed.Id, Ct);
            switch (change)
            {
                case "paused": stored.State = ScheduleState.Paused; break;
                case "ended": stored.State = ScheduleState.Ended; break;
                case "disabled": stored.ReminderEnabled = false; break;
                case "removed": db.Schedules.Remove(stored); break;
                default: stored.Rule.Start = new(2026, 11, 10); break;
            }
            await db.SaveChangesAsync(Ct);
        }
        var before = await RowsAsync(f);
        await s.Service.SnoozeAsync(new(original, SnoozeChoice.OneHour));
        var repeated = s.Native.Pending.Single(n => n.Id == ReminderSnoozes.IdFor(original.Id));
        var remaining = PlanReminderLink.Parse(repeated.Link)!.Targets;
        Assert.Equal(5, remaining.Count); Assert.DoesNotContain(remaining, t => t.PlanId == changed.Id);
        Assert.Equal(f.Time.Now.DateTime.AddHours(1), repeated.NotifyAt); Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Snooze_and_resume_rebuild_privacy_translation_amount_and_effective_date_from_current_data()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); SetAccess(f, s, "one");
        var settings = await f.Store.GetSettingsAsync(Ct); settings.NotificationsShowDetails = true; await f.Store.SaveSettingsAsync(settings, Ct);
        await s.Service.RefreshAsync(); var original = Assert.Single(s.Native.Pending);
        Assert.Equal(s.Plans[0].Name, original.Title);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var plan = await db.Schedules.SingleAsync(p => p.Id == s.Plans[0].Id, Ct);
            plan.Name = "Fresh name"; plan.Amount = 4321;
            db.OccurrenceStates.Add(new() { ScheduleId = s.Plans[0].Id, OriginalDate = Day, DueDate = Day.AddYears(2) });
            await db.SaveChangesAsync(Ct);
        }
        await s.Service.SnoozeAsync(new(original, SnoozeChoice.Tomorrow));
        var repeated = Assert.Single(s.Native.Pending); Assert.Equal("Fresh name", repeated.Title); Assert.Contains("43.21", repeated.Body);
        Assert.Contains("2028", repeated.Body); Assert.Equal(Day, PlanReminderLink.Parse(repeated.Link)!.Targets[0].OriginalDate);
        settings.NotificationsShowDetails = false; await f.Store.SaveSettingsAsync(settings, Ct);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == "de"));
        var before = await RowsAsync(f); await s.Service.RefreshAsync(); repeated = Assert.Single(s.Native.Pending);
        Assert.Equal(f.Translator["App_Name"], repeated.Title); Assert.Equal(f.Translator["Reminder_Generic"], repeated.Body);
        Assert.DoesNotContain("Fresh name", f.Preferences.Get("reminders.snoozed")); Assert.Equal(before, await RowsAsync(f));
    }

    [Theory]
    [InlineData("plans"), InlineData("occurrence|11111111-2222-3333-4444-555555555555|2026-10-10")]
    public async Task Unscoped_legacy_snoozes_are_not_guessed_and_normal_reminders_are_rebuilt(string link)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        f.Preferences.Set("reminders.snoozed", ReminderSnoozes.Serialize([new(123, link, "Old private name", "Old private amount", f.Time.Now.DateTime.AddHours(2))]));
        await s.Service.RefreshAsync(); Assert.Single(s.Native.Pending); Assert.Null(f.Preferences.Get("reminders.snoozed"));
        await s.Service.SnoozeAsync(new(new(123, "Old private name", "Old private amount", null, link, true), SnoozeChoice.OneHour));
        Assert.Single(s.Native.Pending); Assert.Null(f.Preferences.Get("reminders.snoozed"));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task Wrong_file_metadata_and_contract_actions_cannot_create_an_occurrence_snooze(bool contract)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var snapshot = await s.Reader.GetWorkSnapshotAsync(Ct);
        var link = PlanReminderLink.Format(contract ? snapshot.ReminderScope : new string('a', 64), [new(s.Plans[0].Id, contract ? null : Day)]);
        var before = await RowsAsync(f);
        await s.Service.SnoozeAsync(new(new(123, "Old title", "Old body", null, link, true), SnoozeChoice.OneHour));
        Assert.Null(f.Preferences.Get("reminders.snoozed")); Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Retired_choice_during_async_native_publication_cancels_the_now_stale_queue()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); SetAccess(f, s, "one");
        s.Native.OnReplace = () => { s.Access.Current = new(f.Directory.Combine("zanance.db"), s.Access.Current.Context!,
            planSelection: new(QuotaKind.RecurringPlans, new(QuotaScopeKind.PersonalProfile, ScopeId), [])); return Task.CompletedTask; };
        await s.Service.RefreshAsync(); Assert.Empty(s.Native.Pending); Assert.Equal(2, s.Native.Replacements);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Same_original_snoozed_again_keeps_one_deadline_and_the_shared_pending_queue_bound()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await s.Service.RefreshAsync(); var original = Assert.Single(s.Native.Pending);
        await s.Service.SnoozeAsync(new(original, SnoozeChoice.OneHour));
        var first = s.Native.Pending.Single(n => n.Id == ReminderSnoozes.IdFor(original.Id));
        await s.Service.SnoozeAsync(new(first, SnoozeChoice.Tomorrow));
        Assert.Single(ReminderSnoozes.Deserialize(f.Preferences.Get("reminders.snoozed")));
        Assert.Equal(f.Time.Now.DateTime.AddDays(1), s.Native.Pending.Single(n => n.Id == first.Id).NotifyAt);
        Assert.True(s.Native.Pending.Count <= ReminderPlanner.MaxPending); Assert.Empty(s.Native.Shown);
    }

    [Fact]
    public async Task Scoped_tap_resolves_after_unlock_and_rejects_another_file_without_posting_money()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await AppLockTests.EnableDeviceAsync(f); var security = AppLockTests.Create(f);
        var router = new AppLinkRouter(security.Lock, f.Platform, s.Reader); var snapshot = await s.Reader.GetWorkSnapshotAsync(Ct);
        await security.Lock.StartAsync();
        await router.OpenAsync(PlanReminderLink.Format(snapshot.ReminderScope, [new(s.Plans[0].Id, Day)]));
        await router.OpenAsync(PlanReminderLink.Format(new string('a', 64), [new(s.Plans[0].Id, Day)]));
        Assert.Empty(f.Platform.Routes); await security.Lock.UnlockedAsync();
        var opened = Assert.Single(f.Platform.Routes); Assert.Equal(AppRoutes.OccurrenceRoute, opened.Route);
        Assert.Equal(Day, opened.Parameters!["date"]); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Disabled_permission_produces_no_native_or_snooze_effect()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); s.Native.Enabled = false;
        await s.Service.RefreshAsync(); await s.Service.SnoozeAsync(new(new(1, "Private", "Private", null, "plans"), SnoozeChoice.OneHour));
        Assert.Equal(0, s.Native.Replacements); Assert.Empty(s.Native.Shown); Assert.Null(f.Preferences.Get("reminders.snoozed"));
    }

    [Theory]
    [InlineData("fa"), InlineData("en"), InlineData("de")]
    public async Task Receiver_initialization_registers_current_translated_actions_without_database_or_queue_work(string language)
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == language));
        var before = await RowsAsync(f); s.Service.WatchSnoozes();
        Assert.Equal((f.Translator["Reminder_SnoozeHour"], f.Translator["Reminder_SnoozeTomorrow"]), Assert.Single(s.Native.Labels));
        Assert.Equal(0, s.Access.Captures); Assert.Equal(0, s.Native.Replacements); Assert.Equal(before, await RowsAsync(f));
    }

    private sealed record Seed(List<Schedule> Plans, PlanStore Reader, AccessSource Access, NativeScheduler Native, ReminderService Service);

    [Fact]
    public async Task A_group_snooze_uses_the_current_explicit_choice_without_adding_a_later_plan()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await s.Service.RefreshAsync(); var original = Assert.Single(s.Native.Pending);
        var extra = new Schedule { Name = "Later separate plan", AccountId = s.Plans[0].AccountId, ReminderEnabled = true,
            ReminderDaysBefore = 0, Rule = new() { Start = Day } };
        await f.Services.GetRequiredService<PlanStore>().SaveSchedulesAsync([extra], Ct); SetAccess(f, s, "one");
        var before = await RowsAsync(f); await s.Service.SnoozeAsync(new(original, SnoozeChoice.OneHour));
        var repeated = s.Native.Pending.Single(n => n.Id == ReminderSnoozes.IdFor(original.Id));
        Assert.Equal(s.Plans[0].Id, Assert.Single(PlanReminderLink.Parse(repeated.Link)!.Targets).PlanId);
        Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Lost_shared_membership_removes_pending_delivery_without_changing_financial_rows()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await s.Service.RefreshAsync(); Assert.NotEmpty(s.Native.Pending);
        s.Access.Current = new(f.Directory.Combine("zanance.db"), new(ProductPlan.Pro,
            new(EntitlementScopeKind.SharedSpace, ScopeId), new(ScopeId, false, true)));
        var before = await RowsAsync(f); await s.Service.RefreshAsync(); Assert.Empty(s.Native.Pending); Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task Retirement_before_native_publication_removes_an_earlier_queue_without_financial_writes()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); await s.Service.RefreshAsync();
        Assert.NotEmpty(s.Native.Pending); var before = await RowsAsync(f); var retireAt = s.Access.Captures + 2;
        s.Access.OnCapture = count => { if (count == retireAt) SetAccess(f, s, "one"); };
        await s.Service.RefreshAsync();
        Assert.Empty(s.Native.Pending); Assert.Equal(before, await RowsAsync(f));
    }

    [Fact]
    public async Task File_guard_rejects_another_opened_database_and_a_changed_inactive_snapshot()
    {
        using var f = new FlowFixture(); var s = await SeedAsync(f); var snapshot = await s.Reader.GetWorkSnapshotAsync(Ct);
        await s.Reader.ValidateWorkSnapshotAsync(snapshot, Ct); SetAccess(f, s, "one");
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Reader.ValidateWorkSnapshotAsync(snapshot, Ct));
        using var other = new FlowFixture(); var otherPlans = other.Services.GetRequiredService<PlanStore>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => otherPlans.ValidateWorkSnapshotAsync(snapshot, Ct));
        Assert.NotEqual(snapshot.ReminderScope, (await otherPlans.GetWorkSnapshotAsync(Ct)).ReminderScope);
    }

    private static async Task<Seed> SeedAsync(FlowFixture f)
    {
        var account = new Account { Name = "Owned cash", CurrencyCode = "EUR", OpeningDate = Day.AddDays(-2), OpeningBalance = 100000 };
        await f.Store.SaveAccountAsync(account, Ct);
        var plans = Enumerable.Range(0, 6).Select(i => new Schedule { Name = "Owned bill " + i, AccountId = account.Id,
            Amount = 1234, ReminderEnabled = true, ReminderDaysBefore = 0, ReminderTime = new(9, 0), Rule = new() { Frequency = Frequency.Once, Start = Day } }).ToList();
        await f.Services.GetRequiredService<PlanStore>().SaveSchedulesAsync(plans, Ct);
        await f.Store.GetSettingsAsync(Ct); // Publish the established initial settings before taking no-write baselines.
        var access = new AccessSource(); var reader = new PlanStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Store, access);
        var native = new NativeScheduler();
        return new(plans, reader, access, native, new(native, f.Store, reader, f.Services.GetRequiredService<GoalStore>(),
            f.Services.GetRequiredService<HoldingStore>(), f.Preferences, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time));
    }
    private static void SetAccess(FlowFixture f, Seed s, string kind)
    {
        if (kind == "inactive") { s.Access.Current = CommercialWriteAccess.Inactive; return; }
        s.Access.Current = new(f.Directory.Combine("zanance.db"), new(kind == "plus" ? ProductPlan.Plus : ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, ScopeId)), planSelection: kind == "missing" ? null :
            new(QuotaKind.RecurringPlans, new(QuotaScopeKind.PersonalProfile, ScopeId), kind == "empty" ? [] : [s.Plans[0].Id]));
    }
    private static async Task SqlAsync(FlowFixture f, string sql)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }
    private static async Task<string> RowsAsync(FlowFixture f)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(24, tables.Count); var rows = new SortedDictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var records = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                records.Add(JsonSerializer.Serialize(values));
            }
            records.Sort(StringComparer.Ordinal); rows[table] = records;
        }
        return JsonSerializer.Serialize(rows);
    }
    /// <summary>Cached facts for the actual isolated file; no provider or platform emulation.</summary>
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal int Captures;
        internal Action<int>? OnCapture;
        public CommercialWriteAccess Capture(string databasePath) { Captures++; OnCapture?.Invoke(Captures); return Current; }
    }
    /// <summary>Explicit recording native-effect port, compiling the real reminder service without fake UI controls.</summary>
    private sealed class NativeScheduler : IReminderScheduler
    {
        public event EventHandler<string>? Tapped { add { } remove { } }
        public event EventHandler<SnoozeRequest>? SnoozeRequested { add { } remove { } }
        public bool IsSupported => true;
        internal bool Enabled = true;
        internal int Replacements;
        internal IReadOnlyList<ReminderNotification> Pending = [];
        internal List<ReminderNotification> Shown = [];
        internal Func<Task>? OnReplace;
        internal List<(string Hour, string Tomorrow)> Labels = [];
        public void SetSnoozeActions(string oneHour, string tomorrow) => Labels.Add((oneHour, tomorrow));
        public Task<bool> AreEnabledAsync() => Task.FromResult(Enabled);
        public Task<bool> RequestPermissionAsync() => Task.FromResult(Enabled);
        public async Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders)
        {
            Replacements++; Pending = reminders.ToArray(); if (OnReplace is not null) await OnReplace();
        }
        public Task ShowAsync(ReminderNotification reminder) { Shown.Add(reminder); return Task.CompletedTask; }
    }
}
