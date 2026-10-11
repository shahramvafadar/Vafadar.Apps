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

/// <summary>AT-135/136: retained occurrence corrections, links and new payments in actual SQLite writers.</summary>
[Trait("AT", "AT-135")]
public sealed class OccurrenceCorrectionWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static CapabilityContext Context(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private async Task<Fixture> FixtureAsync(bool trigger = false)
    {
        var path = _directory.Combine(Guid.NewGuid()+".db");
        var access = new AccessSource();
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path);
        if (trigger)
        {
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), access));
        }
        var provider = services.BuildServiceProvider();
        _providers.Add(provider); provider.MigrateLocalDatabase<ZananceDbContext>();
        var store = provider.GetRequiredService<ZananceStore>();
        await store.EnsureDefaultCategoriesAsync(Ct);
        var accounts = Enumerable.Range(1, 4).Select(i => new Account
        { Name = "Owned account "+i, CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 100000 }).ToArray();
        foreach (var account in accounts) await store.SaveAccountAsync(account, Ct);
        return new(path, access, provider, store, accounts);
    }

    private sealed record Fixture(string Path, AccessSource Access, ServiceProvider Provider, ZananceStore Store, Account[] Accounts)
    {
        public LedgerEntry Entry(int account, EntryKind kind = EntryKind.Expense) => new()
        { Kind = kind, AccountId = Accounts[account].Id, Amount = 1000, Date = Day, Note = "Retained fictitious input", Tags = ["owned"] };
        public void Enable(ProductPlan plan = ProductPlan.Free) => Access.Current = new(Path, Context(plan));
    }

    private static Schedule Schedule(Fixture f) => new()
    { Name = "Owned actual occurrence", AccountId = f.Accounts[0].Id, Kind = EntryKind.Expense, Amount = 1000,
        Rule = new() { Frequency = Frequency.Monthly, Start = Day.AddMonths(-1) }, Note = "Retained rule" };

    private static async Task<(PlanStore Plans, Schedule Plan, Occurrence Occurrence, LedgerEntry Entry)> SeedAsync(Fixture f)
    {
        var plans = f.Provider.GetRequiredService<PlanStore>(); var plan = Schedule(f);
        await plans.SaveScheduleAsync(plan, Ct); var entry = f.Entry(0); entry.Title = "Explicit linked actual expense";
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
        return (plans, plan, Occurrences.Between(plan, [], Day, Day, Day).Single(), entry);
    }

    [Theory]
    [InlineData("skip")]
    [InlineData("unskip")]
    [InlineData("change")]
    [InlineData("link")]
    [InlineData("repair")]
    [InlineData("delete")]
    public async Task Missing_membership_cannot_mutate_retained_occurrences_or_linked_entries(string action)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        if (action == "repair")
        {
            seed.Entry.ScheduleId = seed.Plan.Id; seed.Entry.OccurrenceDate = Day;
            Assert.True((await f.Store.SaveEntryAsync(seed.Entry, Ct)).Succeeded);
        }
        f.Access.Current = new(f.Path, Context(ProductPlan.Pro, true, false, true));
        var before = await SnapshotAsync(f.Provider); var events = 0; seed.Plans.Changed += (_, _) => events++;
        async Task Act()
        {
            switch (action)
            {
                case "skip": await seed.Plans.SkipAsync(seed.Occurrence, Ct); break;
                case "unskip": await seed.Plans.UnskipAsync(seed.Occurrence, Ct); break;
                case "change": await seed.Plans.ChangeOccurrenceAsync(seed.Occurrence, Day.AddDays(2), 1100, "Explicit correction", Ct); break;
                case "link": await seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct); break;
                case "repair": await seed.Plans.RepairSettlementsAsync(Ct); break;
                case "delete": await seed.Plans.DeleteScheduleAsync(seed.Plan.Id, Ct); break;
            }
        }
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(Act);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_failed_state_write_cannot_commit_the_entry_link_separately(bool existingState)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        if (existingState) await seed.Plans.ChangeOccurrenceAsync(seed.Occurrence, Day.AddDays(1), 1200, "Retained correction", Ct);
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var sql = existingState
            ? "CREATE TRIGGER OwnedStateFailure BEFORE UPDATE ON OccurrenceStates BEGIN SELECT RAISE(ABORT, 'Owned fixture state failure'); END"
            : "CREATE TRIGGER OwnedStateFailure BEFORE INSERT ON OccurrenceStates BEGIN SELECT RAISE(ABORT, 'Owned fixture state failure'); END";
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
        var before = await SnapshotAsync(f.Provider); var events = 0; seed.Plans.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<DbUpdateException>(() => seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData("skip", false)]
    [InlineData("unskip", false)]
    [InlineData("change", false)]
    [InlineData("link", false)]
    [InlineData("repair", false)]
    [InlineData("delete", false)]
    [InlineData("skip", true)]
    [InlineData("unskip", true)]
    [InlineData("change", true)]
    [InlineData("link", true)]
    [InlineData("repair", true)]
    [InlineData("delete", true)]
    public async Task Retained_corrections_need_no_selected_account_or_paid_host(string action, bool expiredHost)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        if (action == "unskip") await seed.Plans.SkipAsync(seed.Occurrence, Ct);
        if (action == "repair")
        {
            seed.Entry.ScheduleId = seed.Plan.Id; seed.Entry.OccurrenceDate = Day;
            Assert.True((await f.Store.SaveEntryAsync(seed.Entry, Ct)).Succeeded);
        }
        f.Access.Current = new(f.Path, Context(ProductPlan.Free, expiredHost, true, !expiredHost));
        var events = 0; seed.Plans.Changed += (_, _) => events++;
        switch (action)
        {
            case "skip": await seed.Plans.SkipAsync(seed.Occurrence, Ct); break;
            case "unskip": await seed.Plans.UnskipAsync(seed.Occurrence, Ct); break;
            case "change": await seed.Plans.ChangeOccurrenceAsync(seed.Occurrence, Day.AddDays(2), 1100, "Explicit correction", Ct); break;
            case "link": await seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct); break;
            case "repair": Assert.Equal(1, await seed.Plans.RepairSettlementsAsync(Ct)); break;
            case "delete": Assert.True(await seed.Plans.DeleteScheduleAsync(seed.Plan.Id, Ct)); break;
        }
        Assert.Equal(1, events);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        if (action == "delete") Assert.Null(await seed.Plans.GetScheduleAsync(seed.Plan.Id, Ct));
        else
        {
            var state = Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct));
            Assert.Equal(action == "skip" ? OccurrenceStatus.Skipped : action is "link" or "repair"
                ? OccurrenceStatus.Settled : OccurrenceStatus.Open, state.Status);
            if (action == "change")
            {
                Assert.Equal(Day.AddDays(2), state.DueDate); Assert.Equal(1100, state.Amount);
                Assert.Equal("Explicit correction", state.Note);
            }
        }
    }

    [Fact]
    public async Task Explicit_link_preserves_complete_actual_money_and_metadata_with_no_second_entry()
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        seed.Entry.Amount = 950; seed.Entry.Date = Day.AddDays(-2); seed.Entry.Payee = "Owned payee";
        Assert.True((await f.Store.SaveEntryAsync(seed.Entry, Ct)).Succeeded);
        var original = JsonSerializer.Serialize(await f.Store.GetEntryAsync(seed.Entry.Id, Ct));
        var events = 0; seed.Plans.Changed += (_, _) => events++; f.Enable();
        await seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct);
        var actual = Assert.IsType<LedgerEntry>(await f.Store.GetEntryAsync(seed.Entry.Id, Ct));
        Assert.Equal(seed.Plan.Id, actual.ScheduleId); Assert.Equal(Day, actual.OccurrenceDate);
        actual.ScheduleId = null; actual.OccurrenceDate = null;
        Assert.Equal(original, JsonSerializer.Serialize(actual));
        var state = Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct));
        Assert.Equal(OccurrenceStatus.Settled, state.Status); Assert.Equal(seed.Entry.Id, state.EntryId);
        Assert.Equal(0, state.PaidAmount); Assert.Equal(1, events);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        await seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct));
    }

    [Theory]
    [InlineData("missing-entry")]
    [InlineData("other-link")]
    [InlineData("other-date")]
    [InlineData("kind")]
    [InlineData("account")]
    [InlineData("partial")]
    [InlineData("missing-plan")]
    [InlineData("slice")]
    [InlineData("rule")]
    public async Task Changed_actual_relationships_reject_the_reviewed_link_without_any_write(string change)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f); var entryId = seed.Entry.Id;
        await using (var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            switch (change)
            {
                case "missing-entry": entryId = Guid.NewGuid(); break;
                case "other-link":
                    var other = Schedule(f); db.Schedules.Add(other); await db.SaveChangesAsync(Ct);
                    await db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(e => e.SetProperty(x => x.ScheduleId, other.Id).SetProperty(x => x.OccurrenceDate, Day), Ct); break;
                case "other-date":
                    await db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(e => e.SetProperty(x => x.ScheduleId, seed.Plan.Id).SetProperty(x => x.OccurrenceDate, Day.AddMonths(-1)), Ct); break;
                case "kind": await db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(e => e.SetProperty(x => x.Kind, EntryKind.Income), Ct); break;
                case "account": await db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(e => e.SetProperty(x => x.AccountId, f.Accounts[1].Id), Ct); break;
                case "partial": await db.Entries.Where(e => e.Id == entryId).ExecuteUpdateAsync(e => e.SetProperty(x => x.IsPartialPayment, true), Ct); break;
                case "missing-plan": await db.Schedules.Where(s => s.Id == seed.Plan.Id).ExecuteDeleteAsync(Ct); break;
                case "slice": await db.Schedules.Where(s => s.Id == seed.Plan.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ActiveUntil, Day.AddDays(-1)), Ct); break;
                case "rule":
                    seed.Plan.Rule.Start = Day.AddDays(1); db.Schedules.Update(seed.Plan); await db.SaveChangesAsync(Ct); break;
            }
        }
        f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; seed.Plans.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => seed.Plans.LinkAsync(seed.Occurrence, entryId, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Transfer_link_rechecks_its_actual_destination()
    {
        var f = await FixtureAsync(); var plans = f.Provider.GetRequiredService<PlanStore>();
        var plan = Schedule(f); plan.Kind = EntryKind.Transfer; plan.ToAccountId = f.Accounts[1].Id;
        await plans.SaveScheduleAsync(plan, Ct);
        var entry = f.Entry(0, EntryKind.Transfer); entry.ToAccountId = f.Accounts[2].Id;
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
        var occurrence = Occurrences.Between(plan, [], Day, Day, Day).Single();
        f.Enable(); var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => plans.LinkAsync(occurrence, entry.Id, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData("link", "entry")]
    [InlineData("link", "state")]
    [InlineData("skip", "state")]
    [InlineData("repair", "state")]
    [InlineData("delete", "plan")]
    public async Task Rights_retired_after_actual_sql_roll_back_complete_rows_before_events(string action, string boundary)
    {
        var f = await FixtureAsync(trigger: true); var seed = await SeedAsync(f);
        if (action == "repair")
        {
            seed.Entry.ScheduleId = seed.Plan.Id; seed.Entry.OccurrenceDate = Day;
            Assert.True((await f.Store.SaveEntryAsync(seed.Entry, Ct)).Succeeded);
        }
        await using (var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            var sql = boundary switch
            {
                "entry" => "CREATE TRIGGER OwnedRetire AFTER UPDATE ON Entries BEGIN SELECT AfterOccurrenceSql(); END",
                "plan" => "CREATE TRIGGER OwnedRetire AFTER DELETE ON Schedules BEGIN SELECT AfterOccurrenceSql(); END",
                _ => "CREATE TRIGGER OwnedRetire AFTER INSERT ON OccurrenceStates BEGIN SELECT AfterOccurrenceSql(); END",
            };
            await db.Database.ExecuteSqlRawAsync(sql, Ct);
        }
        f.Enable(); f.Access.AfterSql = () => f.Access.Current = new(f.Path, Context(ProductPlan.Pro));
        var before = await SnapshotAsync(f.Provider); var events = 0; seed.Plans.Changed += (_, _) => events++;
        async Task Act()
        {
            if (action == "link") await seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct);
            else if (action == "skip") await seed.Plans.SkipAsync(seed.Occurrence, Ct);
            else if (action == "repair") await seed.Plans.RepairSettlementsAsync(Ct);
            else await seed.Plans.DeleteScheduleAsync(seed.Plan.Id, Ct);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(Act);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        f.Access.AfterSql = null;
        await Act(); Assert.Equal(1, events);
    }

    [Fact]
    public async Task Repair_is_idempotent_and_plan_history_cannot_be_deleted()
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f); f.Enable();
        await seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct);
        var before = await SnapshotAsync(f.Provider); var events = 0; seed.Plans.Changed += (_, _) => events++;
        Assert.Equal(0, await seed.Plans.RepairSettlementsAsync(Ct));
        Assert.False(await seed.Plans.DeleteScheduleAsync(seed.Plan.Id, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task A_snapshot_for_another_database_cannot_link_retained_money()
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        f.Access.Current = new(_directory.Combine("another-owned.db"), Context());
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => seed.Plans.LinkAsync(seed.Occurrence, seed.Entry.Id, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Independent_writers_cannot_assign_the_same_payment_to_two_occurrences()
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f); f.Enable();
        var second = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(new AccessSource { Current = f.Access.Current })
            .AddZananceData(f.Path).BuildServiceProvider();
        _providers.Add(second);
        var otherPlans = second.GetRequiredService<PlanStore>();
        var otherDate = Day.AddMonths(-1);
        var otherOccurrence = Occurrences.Between(seed.Plan, [], otherDate, otherDate, Day).Single();
        var events = 0; seed.Plans.Changed += (_, _) => Interlocked.Increment(ref events);
        otherPlans.Changed += (_, _) => Interlocked.Increment(ref events);
        async Task<bool> Attempt(PlanStore plans, Occurrence occurrence)
        {
            try { await plans.LinkAsync(occurrence, seed.Entry.Id, Ct); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var results = await Task.WhenAll(Task.Run(() => Attempt(seed.Plans, seed.Occurrence), Ct),
            Task.Run(() => Attempt(otherPlans, otherOccurrence), Ct));
        Assert.Single(results, r => r); Assert.Equal(1, events);
        var state = Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct));
        var actual = Assert.IsType<LedgerEntry>(await f.Store.GetEntryAsync(seed.Entry.Id, Ct));
        Assert.Equal(state.EntryId, actual.Id); Assert.Equal(state.OriginalDate, actual.OccurrenceDate);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [Trait("AT", "AT-136")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_failed_new_settlement_state_write_rolls_back_money_and_all_notifications(bool partial)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER OwnedPaymentFailure BEFORE INSERT ON OccurrenceStates BEGIN SELECT RAISE(ABORT, 'Owned payment failure'); END", Ct);
        var before = await SnapshotAsync(f.Provider); var events = 0;
        seed.Plans.Changed += (_, _) => events++; f.Store.Changed += (_, _) => events++;
        var entry = Occurrences.CreateEntry(seed.Occurrence, partial ? 400 : 1000, Day.AddDays(-2), ReviewState.Confirmed);
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            if (partial) await seed.Plans.PayPartAsync(seed.Occurrence, entry, Ct);
            else await seed.Plans.SettleAsync(seed.Occurrence, entry, Ct);
        });
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    [Trait("AT", "AT-136")]
    public async Task Final_partial_payment_uses_actual_paid_money_instead_of_the_old_form_balance()
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        var first = Occurrences.CreateEntry(seed.Occurrence, 600, Day, ReviewState.Confirmed);
        Assert.True((await seed.Plans.PayPartAsync(seed.Occurrence, first, Ct)).Succeeded);
        var final = Occurrences.CreateEntry(seed.Occurrence, 400, Day.AddDays(1), ReviewState.Confirmed);
        Assert.True((await seed.Plans.PayPartAsync(seed.Occurrence, final, Ct)).Succeeded);
        var actual = Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct));
        Assert.Equal(OccurrenceStatus.Settled, actual.Status); Assert.Equal(final.Id, actual.EntryId);
        Assert.Equal(600, actual.PaidAmount);
        Assert.False(Assert.IsType<LedgerEntry>(await f.Store.GetEntryAsync(final.Id, Ct)).IsPartialPayment);
    }

    [Theory]
    [Trait("AT", "AT-136")]
    [InlineData("missing")]
    [InlineData("skipped")]
    [InlineData("kind")]
    [InlineData("account")]
    [InlineData("date")]
    [InlineData("existing")]
    public async Task Changed_actual_occurrence_relationships_reject_new_money_without_writes(string change)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        var entry = Occurrences.CreateEntry(seed.Occurrence, 700, Day, ReviewState.Confirmed);
        switch (change)
        {
            case "missing": Assert.True(await seed.Plans.DeleteScheduleAsync(seed.Plan.Id, Ct)); break;
            case "skipped": await seed.Plans.SkipAsync(seed.Occurrence, Ct); break;
            case "kind": entry.Kind = EntryKind.Income; break;
            case "account": entry.AccountId = f.Accounts[1].Id; break;
            case "date": seed.Plan.Rule.Start = Day.AddDays(1); await seed.Plans.SaveScheduleAsync(seed.Plan, Ct); break;
            case "existing": entry = seed.Entry; break;
        }
        var before = await SnapshotAsync(f.Provider); var events = 0;
        seed.Plans.Changed += (_, _) => events++; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => seed.Plans.SettleAsync(seed.Occurrence, entry, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [Trait("AT", "AT-136")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_post_sql_access_retirement_rolls_back_payment_and_all_state_before_retry(bool partial)
    {
        var f = await FixtureAsync(trigger: true); var seed = await SeedAsync(f); f.Enable(ProductPlan.Pro);
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER OwnedPaymentRetirement AFTER INSERT ON Entries BEGIN SELECT AfterOccurrenceSql(); END", Ct);
        f.Access.AfterSql = () => f.Access.Current = CommercialWriteAccess.Inactive;
        var before = await SnapshotAsync(f.Provider); var events = 0;
        seed.Plans.Changed += (_, _) => events++; f.Store.Changed += (_, _) => events++;
        var entry = Occurrences.CreateEntry(seed.Occurrence, partial ? 400 : 1000, Day.AddDays(-1), ReviewState.Confirmed);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (partial) await seed.Plans.PayPartAsync(seed.Occurrence, entry, Ct);
            else await seed.Plans.SettleAsync(seed.Occurrence, entry, Ct);
        });
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        f.Access.AfterSql = null; f.Enable(ProductPlan.Pro);
        Assert.True((partial ? await seed.Plans.PayPartAsync(seed.Occurrence, entry, Ct)
            : await seed.Plans.SettleAsync(seed.Occurrence, entry, Ct)).Succeeded);
        Assert.Equal(2, events);
    }

    [Theory]
    [Trait("AT", "AT-136")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_payment_keeps_all_complete_rows_and_notifications_unchanged(bool partial)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        var before = await SnapshotAsync(f.Provider); var events = 0;
        seed.Plans.Changed += (_, _) => events++; f.Store.Changed += (_, _) => events++;
        var entry = Occurrences.CreateEntry(seed.Occurrence, 0, Day, ReviewState.Confirmed);
        var result = partial ? await seed.Plans.PayPartAsync(seed.Occurrence, entry, Ct) : await seed.Plans.SettleAsync(seed.Occurrence, entry, Ct);
        Assert.False(result.Succeeded); Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    [Trait("AT", "AT-136")]
    public async Task Changed_actual_amount_and_moved_due_date_classify_partial_payment_without_losing_metadata()
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        await seed.Plans.ChangeOccurrenceAsync(seed.Occurrence, Day.AddDays(10), 2000, "Owned moved amount", Ct);
        var entry = Occurrences.CreateEntry(seed.Occurrence, 1000, Day.AddDays(-1), ReviewState.Confirmed);
        entry.Payee = "Owned actual payee"; entry.Note = "Owned actual note"; entry.Tags = ["owned"];
        Assert.True((await seed.Plans.PayPartAsync(seed.Occurrence, entry, Ct)).Succeeded);
        var state = Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct));
        Assert.Equal(OccurrenceStatus.Open, state.Status); Assert.Equal(1000, state.PaidAmount);
        Assert.Equal(2000, state.Amount); Assert.Equal(Day.AddDays(10), state.DueDate); Assert.Equal("Owned moved amount", state.Note);
        var actual = Assert.IsType<LedgerEntry>(await f.Store.GetEntryAsync(entry.Id, Ct));
        Assert.True(actual.IsPartialPayment); Assert.Equal(Day.AddDays(-1), actual.Date);
        Assert.Equal(entry.Payee, actual.Payee); Assert.Equal(entry.Note, actual.Note); Assert.Equal(entry.Tags, actual.Tags);
    }

    [Theory]
    [Trait("AT", "AT-136")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_writers_classify_actual_payments_without_two_final_settlements(bool partial)
    {
        var f = await FixtureAsync(); var seed = await SeedAsync(f);
        var provider = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(f.Access).AddZananceData(f.Path).BuildServiceProvider();
        _providers.Add(provider); var other = provider.GetRequiredService<PlanStore>();
        async Task<bool> Pay(PlanStore plans)
        {
            var entry = Occurrences.CreateEntry(seed.Occurrence, partial ? 600 : 1000, Day, ReviewState.Confirmed);
            try { return (partial ? await plans.PayPartAsync(seed.Occurrence, entry, Ct)
                : await plans.SettleAsync(seed.Occurrence, entry, Ct)).Succeeded; }
            catch (DbUpdateException) { return false; }
        }
        var results = await Task.WhenAll(Task.Run(() => Pay(seed.Plans), Ct), Task.Run(() => Pay(other), Ct));
        Assert.Equal(partial ? 2 : 1, results.Count(r => r));
        var entries = (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Where(e => e.ScheduleId == seed.Plan.Id).ToList();
        Assert.Single(entries, e => !e.IsPartialPayment);
        var state = Assert.Single(await seed.Plans.GetStatesAsync(seed.Plan.Id, Ct));
        Assert.Equal(OccurrenceStatus.Settled, state.Status); Assert.Equal(partial ? 600 : 0, state.PaidAmount);
        Assert.Equal(partial ? 1200 : 1000, entries.Sum(e => e.Amount));
        Assert.Equal(entries.Single(e => !e.IsPartialPayment).Id, state.EntryId);
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
        Assert.Equal(24, tables.Count); var all = new Dictionary<string, List<string>>();
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

    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Action? AfterSql = null;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    /// <summary>Only the fixture's actual SQLite connection can retire its cached choice from an insert trigger.</summary>
    private sealed class TriggerConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db=inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("AfterOccurrenceSql",()=>{source.AfterSql?.Invoke();return 1;});
            return db;
        }
    }
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose(); SqliteConnection.ClearAllPools(); _directory.Dispose();
    }
}
