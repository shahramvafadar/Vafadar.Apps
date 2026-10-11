using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-140: automatic candidates competing with actual independent profile writers.</summary>
public sealed class AutomaticOccurrenceWriteTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 11);
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private async Task<Fixture> FixtureAsync()
    {
        var path = _directory.Combine(Guid.NewGuid()+".db"); var access = new AccessSource();
        var collection = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path);
        var original = collection.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
        collection.Remove(original);
        collection.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new PaymentBoundary(
            (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), access));
        var automatic = collection.BuildServiceProvider(); _providers.Add(automatic); automatic.MigrateLocalDatabase<ZananceDbContext>();
        var actor = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime()).AddZananceData(path).BuildServiceProvider(); _providers.Add(actor);
        var accounts = new[]
        {
            new Account { Name = "Owned source", CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 100000 },
            new Account { Name = "Owned destination", CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 100000 }
        };
        var store = actor.GetRequiredService<ZananceStore>(); foreach (var account in accounts) await store.SaveAccountAsync(account, Ct);
        var plan = new Schedule { Name = "Owned automatic occurrence", AccountId = accounts[0].Id, Amount = 1000,
            AutoPost = true, AutoPostFrom = Day, Rule = new() { Frequency = Frequency.Once, Start = Day } };
        await actor.GetRequiredService<PlanStore>().SaveScheduleAsync(plan, Ct);
        return new(path, access, automatic, actor, accounts, plan, (PaymentBoundary)automatic.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
    }

    [Fact, Trait("AT", "AT-140")]
    public async Task Manual_part_after_candidate_read_prevents_automatic_full_money()
    {
        var f = await FixtureAsync(); string? reviewed = null; var storeEvents = 0; var planEvents = 0;
        f.Store.Changed += (_, _) => storeEvents++; f.Plans.Changed += (_, _) => planEvents++;
        f.Boundary.Arm(async () =>
        {
            var occurrence = Occurrences.Between(f.Plan, [], Day, Day, Day).Single();
            Assert.True((await f.Actor.GetRequiredService<PlanStore>().PayPartAsync(occurrence,
                Occurrences.CreateEntry(occurrence, 400, Day, ReviewState.Confirmed), Ct)).Succeeded);
            reviewed = await SnapshotAsync(f.Actor);
        });
        var result = await f.Processor.RunAsync(Day, Ct);
        Assert.True(f.Boundary.Invoked); Assert.Equal(0, result.Posted); Assert.Equal(1, result.NeedsReview);
        Assert.Equal(reviewed, await SnapshotAsync(f.Automatic)); Assert.Equal(0, storeEvents); Assert.Equal(0, planEvents);
        Assert.Equal(400, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData("disabled", 1), InlineData("paused", 0), InlineData("ended", 0), InlineData("rule", 0)]
    [InlineData("skipped", 0), InlineData("future", 0), InlineData("deleted-part", 1), InlineData("legacy-part", 1)]
    [InlineData("archived-account", 1)]
    public async Task Actual_changes_after_discovery_do_not_write_stale_money(string change, int needsReview)
    {
        var f = await FixtureAsync(); string? reviewed = null; var events = 0; f.Store.Changed += (_, _) => events++;
        f.Boundary.Arm(async () =>
        {
            var plans = f.Actor.GetRequiredService<PlanStore>(); var store = f.Actor.GetRequiredService<ZananceStore>();
            var occurrence = Occurrences.Between(f.Plan, [], Day, Day, Day).Single();
            switch (change)
            {
                case "disabled": f.Plan.AutoPost = false; await plans.SaveScheduleAsync(f.Plan, Ct); break;
                case "paused": f.Plan.State = ScheduleState.Paused; f.Plan.PausedFrom = Day; await plans.SaveScheduleAsync(f.Plan, Ct); break;
                case "ended": f.Plan.State = ScheduleState.Ended; await plans.SaveScheduleAsync(f.Plan, Ct); break;
                case "rule": f.Plan.Rule.Start = Day.AddDays(1); await plans.SaveScheduleAsync(f.Plan, Ct); break;
                case "skipped": await plans.SkipAsync(occurrence, Ct); break;
                case "future": await plans.ChangeOccurrenceAsync(occurrence, Day.AddDays(1), null, "Owned future", Ct); break;
                case "archived-account": f.Accounts[0].IsArchived = true; await store.SaveAccountAsync(f.Accounts[0], Ct); break;
                default:
                    var part = Occurrences.CreateEntry(occurrence, 400, Day, ReviewState.Confirmed);
                    Assert.True((await plans.PayPartAsync(occurrence, part, Ct)).Succeeded);
                    if (change == "deleted-part") await store.DeleteEntryAsync(part.Id, Ct);
                    else
                    {
                        await using var db = await f.Actor.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
                        await db.Database.ExecuteSqlRawAsync("UPDATE OccurrenceStates SET PaidAmount=0,AutoPostSuppressed=0", Ct);
                    }
                    break;
            }
            reviewed = await SnapshotAsync(f.Actor);
        });
        var result = await f.Processor.RunAsync(Day, Ct); Assert.True(f.Boundary.Invoked);
        Assert.Equal(0, result.Posted); Assert.Equal(needsReview, result.NeedsReview);
        Assert.Equal(reviewed, await SnapshotAsync(f.Automatic)); Assert.Equal(0, events);
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData("manual"), InlineData("automatic")]
    public async Task A_settlement_by_an_independent_writer_after_discovery_is_neither_reposted_nor_needs_review(string mode)
    {
        var f = await FixtureAsync(); string? reviewed = null;
        f.Boundary.Arm(async () =>
        {
            if (mode == "automatic") Assert.Equal(1, (await f.Actor.GetRequiredService<AutoPostProcessor>().RunAsync(Day, Ct)).Posted);
            else
            {
                var occurrence = Occurrences.Between(f.Plan, [], Day, Day, Day).Single();
                Assert.True((await f.Actor.GetRequiredService<PlanStore>().SettleAsync(occurrence,
                    Occurrences.CreateEntry(occurrence, 900, Day, ReviewState.Confirmed), Ct)).Succeeded);
            }
            reviewed = await SnapshotAsync(f.Actor);
        });
        var result = await f.Processor.RunAsync(Day, Ct);
        Assert.True(f.Boundary.Invoked); Assert.Equal(new AutoPostResult(0, 0), result);
        Assert.Equal(reviewed, await SnapshotAsync(f.Automatic)); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData("amount"), InlineData("override"), InlineData("account"), InlineData("transfer")]
    public async Task Automated_money_uses_current_complete_plan_and_override_values(string change)
    {
        var f = await FixtureAsync(); var storeEvents = 0; var planEvents = 0;
        f.Store.Changed += (_, _) => storeEvents++; f.Plans.Changed += (_, _) => planEvents++;
        f.Boundary.Arm(async () =>
        {
            f.Plan.Name = "Fresh actual title"; f.Plan.Icon = "wallet"; f.Plan.Amount = 1500;
            if (change == "account") f.Plan.AccountId = f.Accounts[1].Id;
            if (change == "transfer") { f.Plan.Kind = EntryKind.Transfer; f.Plan.ToAccountId = f.Accounts[1].Id; f.Plan.ToAmount = 1500; }
            await f.Actor.GetRequiredService<PlanStore>().SaveScheduleAsync(f.Plan, Ct);
            if (change == "override")
            {
                var occurrence = Occurrences.Between(f.Plan, [], Day, Day, Day).Single();
                await f.Actor.GetRequiredService<PlanStore>().ChangeOccurrenceAsync(occurrence, Day.AddDays(-1), 1800, "Retained override note", Ct);
            }
        });
        Assert.Equal(new AutoPostResult(1, 0), await f.Processor.RunAsync(Day, Ct)); Assert.True(f.Boundary.Invoked);
        var entry = Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(change == "override" ? 1800 : 1500, entry.Amount); Assert.Equal(change == "override" ? Day.AddDays(-1) : Day, entry.Date);
        Assert.Equal(f.Plan.AccountId, entry.AccountId); Assert.Equal(f.Plan.Kind, entry.Kind);
        Assert.Equal(change == "transfer" ? f.Accounts[1].Id : (Guid?)null, entry.ToAccountId);
        Assert.Equal(change == "transfer" ? 1500 : (long?)null, entry.ToAmount);
        Assert.Equal("Fresh actual title", entry.Title); Assert.Equal("wallet", entry.Icon);
        Assert.Equal(ReviewState.Unreviewed, entry.Review); Assert.Equal(EntrySource.Schedule, entry.Source); Assert.False(entry.IsPartialPayment);
        Assert.Equal(f.Plan.Id, entry.ScheduleId); Assert.Equal(Day, entry.OccurrenceDate);
        var state = Assert.Single(await f.Plans.GetStatesAsync(f.Plan.Id, Ct)); Assert.Equal(entry.Id, state.EntryId); Assert.Equal(OccurrenceStatus.Settled, state.Status);
        if (change == "override") Assert.Equal("Retained override note", state.Note);
        Assert.Equal(1, storeEvents); Assert.Equal(1, planEvents); Assert.Equal(new AutoPostResult(0, 0), await f.Processor.RunAsync(Day, Ct));
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData(ProductPlan.Free, false, true, true, false)]
    [InlineData(ProductPlan.Plus, false, true, true, true)]
    [InlineData(ProductPlan.Pro, false, true, true, true)]
    [InlineData(ProductPlan.Free, true, true, true, true)]
    [InlineData(ProductPlan.Pro, true, true, false, false)]
    [InlineData(ProductPlan.Pro, true, false, true, false)]
    public async Task Explicit_selection_never_grants_advanced_automation_or_shared_rights(ProductPlan tier, bool shared, bool member, bool host, bool allowed)
    {
        var f = await FixtureAsync(); var context = new CapabilityContext(tier,
            new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId), shared ? new(ScopeId, member, host) : null);
        var scope = new QuotaScope(shared ? QuotaScopeKind.SharedSpace : QuotaScopeKind.PersonalProfile, ScopeId);
        f.Access.Current = new(f.Path, context, planSelection: new(QuotaKind.RecurringPlans, scope, [f.Plan.Id]));
        var before = await SnapshotAsync(f.Automatic);
        if (shared && !member)
        {
            // Existing repair and the new writer both demand exact membership before touching retained state.
            await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Processor.RunAsync(Day, Ct));
            await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Plans.TryPostAutomaticallyAsync(f.Plan.Id, Day, Day, Ct));
        }
        else Assert.Equal(allowed ? new AutoPostResult(1, 0) : new AutoPostResult(0, 1), await f.Processor.RunAsync(Day, Ct));
        if (!allowed) Assert.Equal(before, await SnapshotAsync(f.Automatic));
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData(false), InlineData(true)]
    public async Task An_unlimited_upgrade_ignores_a_retained_missing_or_stale_limited_plan_choice(bool stale)
    {
        var f = await FixtureAsync(); var context = new CapabilityContext(ProductPlan.Plus, new(EntitlementScopeKind.PersonalProfile, ScopeId));
        f.Access.Current = new(f.Path, context, planSelection: stale ? new(QuotaKind.RecurringPlans,
            new(QuotaScopeKind.PersonalProfile, ScopeId), [Guid.NewGuid()]) : null);
        Assert.Equal(new AutoPostResult(1, 0), await f.Processor.RunAsync(Day, Ct));
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData(QuotaKind.FinancialAccounts, false), InlineData(QuotaKind.RecurringPlans, true)]
    public async Task A_plan_choice_cannot_be_created_for_another_kind_or_scope(QuotaKind kind, bool otherScope)
    {
        var f = await FixtureAsync(); var context = new CapabilityContext(ProductPlan.Plus, new(EntitlementScopeKind.PersonalProfile, ScopeId));
        var selection = new ResourceSelection(kind, new(QuotaScopeKind.PersonalProfile, otherScope ? Guid.NewGuid() : ScopeId), [f.Plan.Id]);
        Assert.Throws<ArgumentException>(() => new CommercialWriteAccess(f.Path, context, planSelection: selection));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData(false), InlineData(true)]
    public async Task SQL_failure_or_a_retired_plan_choice_rolls_back_all_money_and_state_before_Changed(bool retire)
    {
        var f = await FixtureAsync(); var context = new CapabilityContext(ProductPlan.Plus, new(EntitlementScopeKind.PersonalProfile, ScopeId));
        var scope = new QuotaScope(QuotaScopeKind.PersonalProfile, ScopeId);
        f.Access.Current = new(f.Path, context, planSelection: new(QuotaKind.RecurringPlans, scope, [f.Plan.Id]));
        await using (var db = await f.Actor.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync(retire
                ? "CREATE TRIGGER FailOwnedAutomatic AFTER INSERT ON Entries BEGIN SELECT AfterAutomaticSql(); END"
                : "CREATE TRIGGER FailOwnedAutomatic AFTER INSERT ON Entries BEGIN SELECT RAISE(ABORT,'Owned automatic write failure'); END", Ct);
        f.Access.AfterSql = () => { f.Access.AfterSql = null; f.Access.Current = new(f.Path, context, planSelection: new(QuotaKind.RecurringPlans, scope, [Guid.NewGuid()])); };
        var before = await SnapshotAsync(f.Automatic); var events = 0; f.Store.Changed += (_, _) => events++; f.Plans.Changed += (_, _) => events++;
        if (retire) await Assert.ThrowsAsync<InvalidOperationException>(() => f.Processor.RunAsync(Day, Ct));
        else await Assert.ThrowsAsync<DbUpdateException>(() => f.Processor.RunAsync(Day, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Automatic)); Assert.Equal(0, events);
        await using (var db = await f.Actor.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailOwnedAutomatic", Ct);
        Assert.Equal(new AutoPostResult(1, 0), await f.Processor.RunAsync(Day, Ct)); Assert.Equal(2, events);
    }

    [Theory, Trait("AT", "AT-140")]
    [InlineData(false), InlineData(true)]
    public async Task Limited_plan_choices_never_post_extra_money_or_change_the_original_plan_states(bool explicitChoice)
    {
        var f = await FixtureAsync();
        for (var i = 0; i < 5; i++) await f.Actor.GetRequiredService<PlanStore>().SaveScheduleAsync(new Schedule
        { Name = "Owned retained plan " + i, AccountId = f.Accounts[0].Id, Amount = 1000,
            Rule = new() { Frequency = Frequency.Monthly, Start = Day.AddMonths(-1) }, State = i == 0 ? ScheduleState.Paused : ScheduleState.Active }, Ct);
        var context = new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, ScopeId));
        var scope = new QuotaScope(QuotaScopeKind.PersonalProfile, ScopeId);
        f.Access.Current = new(f.Path, context, planSelection: explicitChoice ? new(QuotaKind.RecurringPlans, scope, [f.Plan.Id]) : null);
        var before = await SnapshotAsync(f.Automatic);
        Assert.Equal(AutomaticPostStatus.NeedsReview, await f.Plans.TryPostAutomaticallyAsync(f.Plan.Id, Day, Day, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Automatic)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact, Trait("AT", "AT-140")]
    public async Task An_automatic_writer_cannot_use_a_cached_snapshot_for_another_file()
    {
        var f = await FixtureAsync(); var before = await SnapshotAsync(f.Automatic);
        f.Access.Current = new(_directory.Combine("other-owned-file.db"), new CapabilityContext(ProductPlan.Plus, new(EntitlementScopeKind.PersonalProfile, ScopeId)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Plans.TryPostAutomaticallyAsync(f.Plan.Id, Day, Day, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Automatic));
    }

    private sealed record Fixture(string Path, AccessSource Access, ServiceProvider Automatic, ServiceProvider Actor,
        Account[] Accounts, Schedule Plan, PaymentBoundary Boundary)
    {
        public ZananceStore Store => Automatic.GetRequiredService<ZananceStore>();
        public PlanStore Plans => Automatic.GetRequiredService<PlanStore>();
        public AutoPostProcessor Processor => Automatic.GetRequiredService<AutoPostProcessor>();
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(25, tables.Count); var all = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); all[table] = rows;
        }
        return JsonSerializer.Serialize(all);
    }

    /// <summary>Real factory boundary after repair/accounts/plans/states and before the candidate's money writer.</summary>
    private sealed class PaymentBoundary(IDbContextFactory<ZananceDbContext> inner, AccessSource access) : IDbContextFactory<ZananceDbContext>
    {
        private Func<Task>? _beforePayment;
        private int _reads;
        internal bool Invoked;
        internal void Arm(Func<Task> beforePayment) { _beforePayment = beforePayment; _reads = 0; }
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("AfterAutomaticSql", () => { access.AfterSql?.Invoke(); return 1; });
            return db;
        }
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            var db = CreateDbContext();
            if (_beforePayment is not null && ++_reads == 5)
            {
                var action = _beforePayment; _beforePayment = null; Invoked = true;
                try { await action(); } catch { await db.DisposeAsync(); throw; }
            }
            return db;
        }
    }
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Action? AfterSql;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    public void Dispose() { foreach (var provider in _providers) provider.Dispose(); SqliteTestPools.Clear(_directory); _directory.Dispose(); }
}
