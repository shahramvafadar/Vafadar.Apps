using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-122: actual goal/plan slots, whole batches and retained split history inside the SQLite writer.</summary>
[Trait("AT", "AT-122")]
public sealed class GoalPlanWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid AccountId = Guid.Parse("66666666-2222-3333-4444-555555555555");
    private static readonly DateOnly Start = new(2026, 1, 1);

    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private ServiceProvider Provider(string path, ICommercialWriteAccessSource? source = null)
    {
        var services = new ServiceCollection();
        if (source is not null) services.AddSingleton(source);
        var provider = services.AddZananceData(path).BuildServiceProvider();
        provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider);
        using var db = provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContext();
        if (!db.Accounts.Any(a => a.Id == AccountId))
        {
            var account = Account(AccountId);
            db.Entry(account).Property(a => a.Id).CurrentValue = AccountId;
            db.Accounts.Add(account);
            db.SaveChanges();
        }
        return provider;
    }

    private static Account Account(Guid id) => new()
    {
        Name = "Original account " + id, CurrencyCode = "EUR", OpeningDate = Start, OpeningBalance = 10000,
    };

    private static Schedule Plan(string name, ScheduleState state = ScheduleState.Active) => new()
    {
        Name = name, AccountId = AccountId, State = state, Amount = 1234, Note = "Original complete note",
        Icon = "Cart", ReminderEnabled = true, ReminderDaysBefore = 2, ReminderTime = new(8, 30),
        Rule = new() { Start = Start, Frequency = Frequency.Monthly },
    };

    private static async Task<Goal> GoalAsync(ServiceProvider provider, string name, GoalState state = GoalState.Active)
    {
        var accountId = Guid.NewGuid();
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var account = Account(accountId);
        db.Entry(account).Property(a => a.Id).CurrentValue = accountId;
        db.Accounts.Add(account);
        await db.SaveChangesAsync(Ct);
        return new() { Name = name, Type = GoalType.AccountBalance, State = state, AccountId = accountId,
            CurrencyCode = "EUR", TargetAmount = 12345, TargetDate = Start.AddYears(2), Note = "Original complete note" };
    }

    private static int Limit(bool goal) => goal ? 1 : 5;
    private static async Task SeedAsync(ServiceProvider provider, bool goal, int count, bool paused = false)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        for (var i = 0; i < count; i++)
        {
            if (goal) db.Goals.Add(await GoalAsync(provider, "Original " + i, paused ? GoalState.Paused : GoalState.Active));
            else db.Schedules.Add(Plan("Original " + i, paused ? ScheduleState.Paused : ScheduleState.Active));
        }
        await db.SaveChangesAsync(Ct);
    }

    private static async Task SaveNewAsync(ServiceProvider provider, bool goal, string name)
    {
        if (goal) await provider.GetRequiredService<GoalStore>().SaveGoalAsync(await GoalAsync(provider, name), Ct);
        else await provider.GetRequiredService<PlanStore>().SaveScheduleAsync(Plan(name), Ct);
    }

    // Compare every stored column, including audit, pins, occurrence metadata and unrelated ledger/settings rows.
    private static async Task<string> SnapshotAsync(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct);
        var connection = db.Database.GetDbConnection();
        var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct);
            while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        var result = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct);
            var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal);
            result[table] = rows;
        }
        return JsonSerializer.Serialize(result);
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Inactive_registration_keeps_creation_unrestricted_without_paid_context(bool goal)
    {
        var provider = Provider(_directory.Combine("inactive.db"));
        for (var i = 0; i <= Limit(goal); i++) await SaveNewAsync(provider, goal, "Unrestricted " + i);
        if (goal) Assert.Equal(2, (await provider.GetRequiredService<GoalStore>().GetGoalsAsync(Ct)).Count);
        else Assert.Equal(6, (await provider.GetRequiredService<PlanStore>().GetSchedulesAsync(Ct)).Count);
        Assert.Empty(await provider.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Paused_rows_keep_the_final_Free_slot_and_rejection_preserves_complete_stored_data(bool goal)
    {
        var path = _directory.Combine("paused.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, goal, Limit(goal), paused: true);
        var draft = goal ? await GoalAsync(provider, "Rejected") : null;
        var before = await SnapshotAsync(provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => goal
            ? provider.GetRequiredService<GoalStore>().SaveGoalAsync(draft!, Ct)
            : provider.GetRequiredService<PlanStore>().SaveScheduleAsync(Plan("Rejected"), Ct));
        Assert.Equal(goal ? QuotaKind.Goals : QuotaKind.RecurringPlans, rejected.Quota);
        Assert.Equal(Limit(goal), rejected.Current);
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retained_over_quota_edits_pause_and_end_are_allowed_without_posting_money(bool goal)
    {
        var path = _directory.Combine("edit.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, goal, Limit(goal) + 1);
        if (goal)
        {
            var store = provider.GetRequiredService<GoalStore>();
            var rows = await store.GetGoalsAsync(Ct);
            var retained = JsonSerializer.Serialize(rows[1]);
            var target = rows[0];
            var created = target.CreatedAt;
            target.Note = "Corrected note"; target.State = GoalState.Paused;
            await store.SaveGoalAsync(target, Ct);
            Assert.Equal(created, (await store.GetGoalAsync(target.Id, Ct))!.CreatedAt);
            Assert.Equal(retained, JsonSerializer.Serialize(await store.GetGoalAsync(rows[1].Id, Ct)));
            target.State = GoalState.Completed;
            await store.SaveGoalAsync(target, Ct);
        }
        else
        {
            var store = provider.GetRequiredService<PlanStore>();
            var rows = await store.GetSchedulesAsync(Ct);
            var retained = JsonSerializer.Serialize(rows.Skip(1));
            var target = rows[0];
            var created = target.CreatedAt;
            target.Note = "Corrected note"; PlanActions.Pause(target, Start.AddMonths(1));
            await store.SaveScheduleAsync(target, Ct);
            Assert.Equal(created, (await store.GetScheduleAsync(target.Id, Ct))!.CreatedAt);
            PlanActions.End(target, Start.AddMonths(1));
            await store.SaveScheduleAsync(target, Ct);
            Assert.Equal(retained, JsonSerializer.Serialize((await store.GetSchedulesAsync(Ct)).Where(s => s.Id != target.Id)));
        }
        Assert.Empty(await provider.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(GoalState.Completed)]
    [InlineData(GoalState.Archived)]
    public async Task Reopening_a_historical_goal_checks_capacity_before_protection_or_other_home_pins_change(GoalState state)
    {
        var path = _directory.Combine("reopen-goal.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, true, 2);
        await using (var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var rows = await db.Goals.OrderBy(g => g.Name).ToListAsync(Ct);
            rows[0].HomePin = 1; rows[1].HomePin = 2;
            var historical = await GoalAsync(provider, "Historical", state); historical.HomePin = null;
            db.Goals.Add(historical); await db.SaveChangesAsync(Ct);
        }
        var store = provider.GetRequiredService<GoalStore>();
        var target = (await store.GetGoalsAsync(Ct)).Single(g => g.Name == "Historical");
        target.State = GoalState.Active; target.HomePin = 3; target.Protect = true;
        var before = await SnapshotAsync(provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveGoalAsync(target, Ct));
        Assert.True(target.Protect); Assert.Equal(3, target.HomePin);
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Fact]
    public async Task Ended_plan_history_does_not_count_but_reactivation_at_capacity_is_atomic()
    {
        var path = _directory.Combine("reopen-plan.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, false, 5);
        var store = provider.GetRequiredService<PlanStore>();
        var historical = Plan("Historical", ScheduleState.Ended);
        await store.SaveScheduleAsync(historical, Ct);
        var before = await SnapshotAsync(provider);
        historical.State = ScheduleState.Paused;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveScheduleAsync(historical, Ct));
        Assert.Equal(before, await SnapshotAsync(provider));
        var active = (await store.GetSchedulesAsync(Ct)).First(s => s.State == ScheduleState.Active);
        PlanActions.End(active, Start); await store.SaveScheduleAsync(active, Ct);
        await store.SaveScheduleAsync(historical, Ct);
        Assert.Equal(5, (await store.GetSchedulesAsync(Ct)).Count(s => s.State != ScheduleState.Ended));
    }

    [Theory]
    [InlineData(false, ProductPlan.Plus, false)]
    [InlineData(true, ProductPlan.Plus, false)]
    [InlineData(false, ProductPlan.Pro, false)]
    [InlineData(true, ProductPlan.Pro, false)]
    [InlineData(false, ProductPlan.Free, true)]
    [InlineData(true, ProductPlan.Free, true)]
    public async Task Paid_or_exact_accepted_shared_context_allows_more_than_Free_capacity(bool goal, ProductPlan plan, bool shared)
    {
        var path = _directory.Combine("unlimited.db");
        var provider = Provider(path, new AccessSource(path, Context(plan, shared)));
        for (var i = 0; i <= Limit(goal); i++) await SaveNewAsync(provider, goal, "Allowed " + i);
    }

    [Fact]
    public async Task Whole_batch_rejection_preserves_original_edits_and_consumes_no_partial_slots_or_events()
    {
        var path = _directory.Combine("batch.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, false, 4);
        var store = provider.GetRequiredService<PlanStore>();
        var original = (await store.GetSchedulesAsync(Ct))[0]; original.Note = "Unsaved correction";
        var before = await SnapshotAsync(provider); var events = 0;
        store.Changed += (_, _) => events++;
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() =>
            store.SaveSchedulesAsync([Plan("New A"), original, Plan("New B")], Ct));
        Assert.Equal(2, rejected.Requested); Assert.Equal(4, rejected.Current);
        Assert.Equal(before, await SnapshotAsync(provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ending_and_creating_in_one_batch_uses_the_net_slots_independent_of_input_order(bool newFirst)
    {
        var path = _directory.Combine("net.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, false, 5);
        var store = provider.GetRequiredService<PlanStore>();
        var old = (await store.GetSchedulesAsync(Ct))[0]; PlanActions.End(old, Start);
        var next = Plan("New slot");
        await store.SaveSchedulesAsync(newFirst ? [next, old] : [old, next], Ct);
        Assert.Equal(5, (await store.GetSchedulesAsync(Ct)).Count(s => s.State != ScheduleState.Ended));
        Assert.Empty(await provider.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(7, false)]
    [InlineData(5, true)]
    [InlineData(7, true)]
    public async Task Split_or_resume_at_or_above_capacity_transfers_one_slot_and_preserves_actual_history(int count, bool resume)
    {
        var path = _directory.Combine("split.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, false, count);
        var store = provider.GetRequiredService<PlanStore>();
        var previous = (await store.GetSchedulesAsync(Ct))[0];
        var beforeDate = Start.AddMonths(1); var afterDate = Start.AddMonths(3);
        var early = new LedgerEntry { Kind = EntryKind.Expense, AccountId = AccountId, Amount = 1234, Date = beforeDate,
            ScheduleId = previous.Id, OccurrenceDate = beforeDate, Note = "Retained early metadata" };
        var late = new LedgerEntry { Kind = EntryKind.Expense, AccountId = AccountId, Amount = 1234, Date = afterDate,
            ScheduleId = previous.Id, OccurrenceDate = afterDate, Note = "Retained later metadata" };
        await using (var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            db.Entries.AddRange(early, late);
            db.OccurrenceStates.Add(new() { ScheduleId = previous.Id, OriginalDate = beforeDate, Status = OccurrenceStatus.Settled, EntryId = early.Id });
            db.OccurrenceStates.Add(new() { ScheduleId = previous.Id, OriginalDate = afterDate, Status = OccurrenceStatus.Settled, EntryId = late.Id });
            await db.SaveChangesAsync(Ct);
        }
        var ledgerBefore = await provider.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct);
        var statesBefore = await store.GetStatesAsync(cancellationToken: Ct);
        Schedule next;
        if (resume)
        {
            PlanActions.Pause(previous, Start.AddMonths(2)); await store.SaveScheduleAsync(previous, Ct);
            next = PlanActions.Resume(previous, afterDate)!;
        }
        else next = PlanActions.SplitFrom(previous, Start.AddMonths(2));
        Assert.True(await store.SaveSplitAsync(previous, next, Ct));
        Assert.Equal(count, (await store.GetSchedulesAsync(Ct)).Count(s => s.State != ScheduleState.Ended));
        var ledgerAfter = await provider.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct);
        var expectedLate = ledgerBefore.Single(e => e.Id == late.Id); expectedLate.ScheduleId = next.Id;
        Assert.Equal(JsonSerializer.Serialize(ledgerBefore.OrderBy(e => e.Id)), JsonSerializer.Serialize(ledgerAfter.OrderBy(e => e.Id)));
        statesBefore.Single(s => s.OriginalDate == afterDate).ScheduleId = next.Id;
        Assert.Equal(JsonSerializer.Serialize(statesBefore.OrderBy(s => s.Id)),
            JsonSerializer.Serialize((await store.GetStatesAsync(cancellationToken: Ct)).OrderBy(s => s.Id)));
        Assert.Equal(2, ledgerAfter.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_insert_failure_rolls_back_and_leaves_the_final_slot_available(bool goal)
    {
        var path = _directory.Combine("failure.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, goal, Limit(goal) - 1);
        var draft = goal ? await GoalAsync(provider, "New") : null;
        var plan = Plan("New"); var before = await SnapshotAsync(provider);
        if (goal) draft!.Name = null!; else plan.Name = null!;
        await Assert.ThrowsAsync<DbUpdateException>(() => goal ? provider.GetRequiredService<GoalStore>().SaveGoalAsync(draft!, Ct)
            : provider.GetRequiredService<PlanStore>().SaveScheduleAsync(plan, Ct));
        Assert.Equal(before, await SnapshotAsync(provider));
        if (goal) { draft!.Name = "Retry"; await provider.GetRequiredService<GoalStore>().SaveGoalAsync(draft, Ct); }
        else { plan.Name = "Retry"; await provider.GetRequiredService<PlanStore>().SaveScheduleAsync(plan, Ct); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_membership_rejects_existing_corrections_without_using_personal_Pro(bool goal)
    {
        var path = _directory.Combine("membership.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Pro, true, member: false)));
        await SeedAsync(provider, goal, 1); var before = await SnapshotAsync(provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            if (goal) await provider.GetRequiredService<GoalStore>().SaveGoalAsync(Assert.Single(await provider.GetRequiredService<GoalStore>().GetGoalsAsync(Ct)), Ct);
            else await provider.GetRequiredService<PlanStore>().SaveScheduleAsync(Assert.Single(await provider.GetRequiredService<PlanStore>().GetSchedulesAsync(Ct)), Ct);
        });
        Assert.Equal(FeaturePermission.RequiresMembership, rejected.Permission);
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_providers_competing_for_the_last_slot_commit_once(bool goal)
    {
        var path = _directory.Combine("contention.db"); using var barrier = new Barrier(2);
        var source = new AccessSource(path, Context(ProductPlan.Free), barrier);
        var first = Provider(path, source); var second = Provider(path, source);
        await SeedAsync(first, goal, Limit(goal) - 1);
        var draftA = goal ? await GoalAsync(first, "Contender A") : null;
        var draftB = goal ? await GoalAsync(second, "Contender B") : null;
        var events = 0;
        if (goal) { first.GetRequiredService<GoalStore>().Changed += (_, _) => Interlocked.Increment(ref events);
            second.GetRequiredService<GoalStore>().Changed += (_, _) => Interlocked.Increment(ref events); }
        else { first.GetRequiredService<PlanStore>().Changed += (_, _) => Interlocked.Increment(ref events);
            second.GetRequiredService<PlanStore>().Changed += (_, _) => Interlocked.Increment(ref events); }
        async Task<Exception?> Attempt(ServiceProvider provider, string name, Goal? draft)
        {
            try { if (goal) await provider.GetRequiredService<GoalStore>().SaveGoalAsync(draft!, Ct);
                else await provider.GetRequiredService<PlanStore>().SaveScheduleAsync(Plan(name), Ct); return null; }
            catch (Exception ex) { return ex; }
        }
        var results = await Task.WhenAll(Task.Run(() => Attempt(first, "Contender A", draftA), Ct), Task.Run(() => Attempt(second, "Contender B", draftB), Ct));
        Assert.Single(results, result => result is null);
        Assert.IsType<CommercialWriteRejectedException>(Assert.Single(results, result => result is not null));
        Assert.Equal(2, source.SynchronizedCaptures); Assert.Equal(1, events);
        if (goal) Assert.Single(await first.GetRequiredService<GoalStore>().GetGoalsAsync(Ct));
        else Assert.Equal(5, (await first.GetRequiredService<PlanStore>().GetSchedulesAsync(Ct)).Count);
    }

    [Theory]
    [InlineData(GoalType.Earmark)]
    [InlineData(GoalType.HoldingQuantity)]
    public async Task New_advanced_goal_kind_requires_Plus_before_any_other_goal_changes(GoalType type)
    {
        var path = _directory.Combine("advanced-goal.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var draft = await GoalAsync(provider, "Advanced"); draft.Type = type; draft.HomePin = 3;
        var before = await SnapshotAsync(provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => provider.GetRequiredService<GoalStore>().SaveGoalAsync(draft, Ct));
        Assert.Equal(CommercialFeature.AdvancedGoals, rejected.Feature); Assert.Equal(FeaturePermission.RequiresPlus, rejected.Permission);
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Theory]
    [InlineData("weekday")]
    [InlineData("lastweekday")]
    [InlineData("second")]
    [InlineData("weekend")]
    [InlineData("contract")]
    [InlineData("reference")]
    [InlineData("renews")]
    [InlineData("auto")]
    public async Task Adding_paid_plan_tools_is_rejected_but_correcting_retained_tools_preserves_complete_metadata(string tool)
    {
        var path = _directory.Combine("advanced-plan.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<PlanStore>(); var draft = Plan("Advanced");
        switch (tool)
        {
            case "weekday": draft.Rule.DayRule = MonthDayRule.NthWeekday; break;
            case "lastweekday": draft.Rule.DayRule = MonthDayRule.LastWeekday; break;
            case "second": draft.Rule.SecondDay = 15; break;
            case "weekend": draft.Rule.WeekendShift = WeekendShift.After; draft.Rule.HolidayRegion = "DE"; break;
            case "contract": draft.ContractProvider = "Fictitious provider"; break;
            case "reference": draft.ContractReference = "Fictitious reference"; break;
            case "renews": draft.ContractRenews = true; break;
            case "auto": draft.AutoPost = true; draft.AutoPostFrom = Start; break;
        }
        var before = await SnapshotAsync(provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveScheduleAsync(draft, Ct));
        Assert.Equal(CommercialFeature.AdvancedPlans, rejected.Feature); Assert.Equal(FeaturePermission.RequiresPlus, rejected.Permission);
        Assert.Equal(before, await SnapshotAsync(provider));
        // Prepare retained post-downgrade data directly, not by weakening the actual save gate.
        await using (var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        { db.Schedules.Add(draft); await db.SaveChangesAsync(Ct); }
        var retained = (await store.GetScheduleAsync(draft.Id, Ct))!; retained.Note = "Corrected retained note";
        await store.SaveScheduleAsync(retained, Ct);
        Assert.Equal(JsonSerializer.Serialize(retained), JsonSerializer.Serialize(await store.GetScheduleAsync(draft.Id, Ct)));
        var next = PlanActions.SplitFrom(retained, Start.AddMonths(1));
        Assert.True(await store.SaveSplitAsync(retained, next, Ct));
        Assert.Equal(1, (await store.GetSchedulesAsync(Ct)).Count(s => s.State != ScheduleState.Ended));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expired_host_keeps_existing_corrections_but_rejects_new_capacity(bool goal)
    {
        var path = _directory.Combine("expired.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Pro, true, host: false)));
        await SeedAsync(provider, goal, 1);
        if (goal)
        {
            var store = provider.GetRequiredService<GoalStore>();
            var target = Assert.Single(await store.GetGoalsAsync(Ct)); target.Note = "Retained correction";
            await store.SaveGoalAsync(target, Ct);
            var draft = await GoalAsync(provider, "New");
            var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveGoalAsync(draft, Ct));
            Assert.Equal(FeaturePermission.RequiresPro, rejected.Permission);
        }
        else
        {
            var store = provider.GetRequiredService<PlanStore>();
            var target = Assert.Single(await store.GetSchedulesAsync(Ct)); target.Note = "Retained correction";
            await store.SaveScheduleAsync(target, Ct);
            var next = PlanActions.SplitFrom(target, Start.AddMonths(1));
            Assert.True(await store.SaveSplitAsync(target, next, Ct));
            var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveScheduleAsync(Plan("New"), Ct));
            Assert.Equal(FeaturePermission.RequiresPro, rejected.Permission);
        }
    }

    [Fact]
    public async Task Enabling_advanced_automation_on_a_basic_plan_rejects_the_complete_edit()
    {
        var path = _directory.Combine("enable-auto.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, false, 1);
        var store = provider.GetRequiredService<PlanStore>();
        var target = Assert.Single(await store.GetSchedulesAsync(Ct)); var before = await SnapshotAsync(provider);
        target.AutoPost = true; target.AutoPostFrom = Start; target.Note = "Unsaved automation edit";
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveScheduleAsync(target, Ct));
        Assert.Equal(CommercialFeature.AdvancedPlans, rejected.Feature);
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Fact]
    public async Task Split_failure_after_occurrence_move_restores_all_rows_and_retry_keeps_one_slot()
    {
        var path = _directory.Combine("split-failure.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, false, 5);
        var store = provider.GetRequiredService<PlanStore>();
        var previous = (await store.GetSchedulesAsync(Ct))[0]; var date = Start.AddMonths(2);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = AccountId, Amount = 1234, Date = date,
            ScheduleId = previous.Id, OccurrenceDate = date };
        db.Entries.Add(entry);
        db.OccurrenceStates.Add(new() { ScheduleId = previous.Id, OriginalDate = date, Status = OccurrenceStatus.Settled, EntryId = entry.Id });
        await db.SaveChangesAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_move BEFORE UPDATE OF ScheduleId ON Entries BEGIN SELECT RAISE(ABORT, 'fixture failure'); END;", Ct);
        var before = await SnapshotAsync(provider); var events = 0;
        store.Changed += (_, _) => events++;
        var next = PlanActions.SplitFrom(previous, date);
        await Assert.ThrowsAsync<SqliteException>(() => store.SaveSplitAsync(previous, next, Ct));
        Assert.Equal(before, await SnapshotAsync(provider)); Assert.Equal(0, events);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_move;", Ct);
        Assert.True(await store.SaveSplitAsync(previous, next, Ct));
        Assert.Equal(1, events);
        Assert.Equal(5, (await store.GetSchedulesAsync(Ct)).Count(s => s.State != ScheduleState.Ended));
        Assert.Equal(next.Id, Assert.Single(await store.GetStatesAsync(cancellationToken: Ct)).ScheduleId);
    }

    [Fact]
    public async Task Conflicting_split_dates_still_return_false_with_no_stored_changes_or_events()
    {
        var path = _directory.Combine("split-conflict.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, false, 5);
        var store = provider.GetRequiredService<PlanStore>(); var previous = (await store.GetSchedulesAsync(Ct))[0];
        await using (var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            db.OccurrenceStates.Add(new() { ScheduleId = previous.Id, OriginalDate = Start.AddMonths(2), Status = OccurrenceStatus.Skipped });
            await db.SaveChangesAsync(Ct);
        }
        var before = await SnapshotAsync(provider); var events = 0; store.Changed += (_, _) => events++;
        var next = PlanActions.SplitFrom(previous, Start.AddMonths(2)); next.Rule.Start = Start.AddDays(1);
        Assert.False(await store.SaveSplitAsync(previous, next, Ct));
        Assert.Equal(before, await SnapshotAsync(provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Correcting_historical_paid_metadata_does_not_enable_new_work_or_require_payment(bool goal)
    {
        var path = _directory.Combine("historical-correction.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        if (goal)
        {
            var store = provider.GetRequiredService<GoalStore>();
            var target = await GoalAsync(provider, "Historical", GoalState.Completed);
            await store.SaveGoalAsync(target, Ct);
            target.Type = GoalType.Earmark; target.Note = "Corrected historical classification";
            await store.SaveGoalAsync(target, Ct);
            Assert.Equal(GoalType.Earmark, (await store.GetGoalAsync(target.Id, Ct))!.Type);
            Assert.Equal(GoalState.Completed, (await store.GetGoalAsync(target.Id, Ct))!.State);
        }
        else
        {
            var store = provider.GetRequiredService<PlanStore>();
            var target = Plan("Historical", ScheduleState.Ended);
            await store.SaveScheduleAsync(target, Ct);
            target.ContractReference = "Corrected historical reference";
            await store.SaveScheduleAsync(target, Ct);
            Assert.Equal(target.ContractReference, (await store.GetScheduleAsync(target.Id, Ct))!.ContractReference);
            Assert.Equal(ScheduleState.Ended, (await store.GetScheduleAsync(target.Id, Ct))!.State);
        }
        Assert.Empty(await provider.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Resolved fictitious access facts, with no fake quota/count algorithm.</summary>
    private sealed class AccessSource(string path, CapabilityContext context, Barrier? barrier = null) : ICommercialWriteAccessSource
    {
        private readonly CommercialWriteAccess _access = new(path, context);
        private int _captures;
        /// <summary>Gets the initial contenders observed before either acquires the SQLite writer.</summary>
        public int SynchronizedCaptures;
        /// <inheritdoc />
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(Path.GetFullPath(path), databasePath);
            if (barrier is not null && Interlocked.Increment(ref _captures) <= 2)
            {
                Interlocked.Increment(ref SynchronizedCaptures);
                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("The contenders did not rendezvous.");
            }
            return _access;
        }
    }
}
