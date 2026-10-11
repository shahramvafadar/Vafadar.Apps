using System.Text.Json;
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

/// <summary>AT-141: actual-file selection reads and financial writers preserve complete retained rows.</summary>
public sealed class PlanWorkSnapshotTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = new(2026, 10, 11);
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static QuotaScope Scope => new(QuotaScopeKind.PersonalProfile, ScopeId);

    private async Task<Fixture> CreateAsync()
    {
        var path = _directory.Combine(Guid.NewGuid()+".db");
        var actor = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime()).AddZananceData(path).BuildServiceProvider();
        _providers.Add(actor); actor.MigrateLocalDatabase<ZananceDbContext>();
        var account = new Account { Name = "Owned cash", CurrencyCode = "EUR", OpeningDate = Today.AddMonths(-2), OpeningBalance = 100000 };
        await actor.GetRequiredService<ZananceStore>().SaveAccountAsync(account, Ct);
        var plans = Enumerable.Range(0, 6).Select(i => new Schedule
        {
            Name = "Owned plan " + i, AccountId = account.Id, Amount = 1000,
            Rule = new() { Frequency = Frequency.Monthly, Start = Today.AddMonths(-1) }
        }).ToList();
        await actor.GetRequiredService<PlanStore>().SaveSchedulesAsync(plans, Ct);
        var source = new AccessSource { Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, ScopeId))) };
        var reader = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime()).AddSingleton<ICommercialWriteAccessSource>(source)
            .AddZananceData(path).BuildServiceProvider(); _providers.Add(reader);
        return new(path, account, plans, source, actor, reader);
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData("inactive", 6), InlineData("missing", 0), InlineData("one", 1), InlineData("stale", 0)]
    [InlineData("plus", 6), InlineData("empty", 0)]
    public async Task A_work_snapshot_keeps_all_original_rows_and_projects_only_actual_new_work(string choice, int selected)
    {
        var f = await CreateAsync();
        if (choice == "inactive") f.Access.Current = CommercialWriteAccess.Inactive;
        else if (choice == "plus") f.Access.Current = new(f.Path, new(ProductPlan.Plus, f.Access.Current.Context!.Scope),
            planSelection: new(QuotaKind.RecurringPlans, Scope, [Guid.NewGuid()]));
        else if (choice != "missing") f.Access.Current = new(f.Path, f.Access.Current.Context!, planSelection:
            new(QuotaKind.RecurringPlans, Scope, choice == "empty" ? [] : [choice == "stale" ? Guid.NewGuid() : f.Plans[0].Id]));
        var before = await SnapshotAsync(f.Actor);
        var snapshot = await f.Reader.GetRequiredService<PlanStore>().GetWorkSnapshotAsync(Ct);
        Assert.Equal(6, snapshot.Schedules.Count); Assert.Equal(selected, snapshot.Schedules.Count(plan => snapshot.Work.CanGenerate(plan.Id)));
        Assert.Empty(snapshot.States); Assert.All(snapshot.Schedules, plan => Assert.Equal(ScheduleState.Active, plan.State));
        Assert.Equal(before, await SnapshotAsync(f.Actor)); Assert.All(f.Access.Paths, path => Assert.Equal(f.Path, path));
    }

    [Fact, Trait("AT", "AT-141")]
    public async Task Retired_read_rights_never_publish_a_work_snapshot_or_write_rows()
    {
        var f = await CreateAsync(); var before = await SnapshotAsync(f.Actor);
        f.Access.RetireAtCapture = 3;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Reader.GetRequiredService<PlanStore>().GetWorkSnapshotAsync(Ct));
        Assert.Equal(3, f.Access.Paths.Count); Assert.Equal(before, await SnapshotAsync(f.Actor));
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData(true), InlineData(false)]
    public async Task Missing_membership_rejects_reads_but_expired_host_preserves_history_without_new_work(bool member)
    {
        var f = await CreateAsync();
        f.Access.Current = new(f.Path, new(ProductPlan.Pro, new(EntitlementScopeKind.SharedSpace, ScopeId), new(ScopeId, member, false)));
        var before = await SnapshotAsync(f.Actor);
        if (!member) await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Reader.GetRequiredService<PlanStore>().GetWorkSnapshotAsync(Ct));
        else
        {
            var snapshot = await f.Reader.GetRequiredService<PlanStore>().GetWorkSnapshotAsync(Ct);
            Assert.Equal(6, snapshot.Schedules.Count); Assert.All(snapshot.Schedules, plan => Assert.False(snapshot.Work.CanGenerate(plan.Id)));
        }
        Assert.Equal(before, await SnapshotAsync(f.Actor));
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData("future", false), InlineData("today", false), InlineData("overdue", true)]
    [InlineData("moved-past", true), InlineData("moved-future", false)]
    public async Task Actual_effective_due_dates_separate_retained_payments_from_new_selected_plan_work(string when, bool allowed)
    {
        var f = await CreateAsync(); var plan = f.Plans[0];
        var original = when == "overdue" ? Today.AddMonths(-1) : when == "future" ? Today.AddMonths(1) : Today;
        var occurrence = Occurrences.Between(plan, [], original, original, Today).Single();
        if (when is "moved-past" or "moved-future")
        {
            await f.Actor.GetRequiredService<PlanStore>().ChangeOccurrenceAsync(occurrence,
                when == "moved-past" ? Today.AddDays(-1) : Today.AddDays(1), null, "Owned moved date", Ct);
            occurrence = occurrence with { DueDate = when == "moved-past" ? Today.AddDays(-1) : Today.AddDays(1) };
        }
        var before = await SnapshotAsync(f.Actor); var events = 0;
        f.Reader.GetRequiredService<ZananceStore>().Changed += (_, _) => events++;
        var entry = Occurrences.CreateEntry(occurrence, 900, Today, ReviewState.Confirmed);
        if (allowed)
        {
            Assert.True((await f.Reader.GetRequiredService<PlanStore>().SettleAsync(occurrence, entry, Ct)).Succeeded);
            Assert.Equal(1, events); Assert.Single(await f.Actor.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
        }
        else
        {
            await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Reader.GetRequiredService<PlanStore>().SettleAsync(occurrence, entry, Ct));
            Assert.Equal(before, await SnapshotAsync(f.Actor)); Assert.Equal(0, events);
        }
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData(false), InlineData(true)]
    public async Task A_generic_new_entry_writer_cannot_bypass_plan_choice_with_its_markers(bool partial)
    {
        var f = await CreateAsync(); var plan = f.Plans[0]; var day = Today.AddMonths(1);
        var occurrence = Occurrences.Between(plan, [], day, day, Today).Single();
        var entry = Occurrences.CreateEntry(occurrence, 400, Today, ReviewState.Confirmed); entry.IsPartialPayment = partial;
        var before = await SnapshotAsync(f.Actor);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Reader.GetRequiredService<ZananceStore>().SaveEntryAsync(entry, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Actor));
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData(false), InlineData(true)]
    public async Task Explicit_selected_future_full_and_partial_payments_still_record_actual_reviewed_money(bool partial)
    {
        var f = await CreateAsync(); var plan = f.Plans[0];
        f.Access.Current = new(f.Path, f.Access.Current.Context!, planSelection: new(QuotaKind.RecurringPlans, Scope, [plan.Id]));
        var day = Today.AddMonths(1); var occurrence = Occurrences.Between(plan, [], day, day, Today).Single();
        var entry = Occurrences.CreateEntry(occurrence, partial ? 400 : 900, Today, ReviewState.Confirmed);
        var plans = f.Reader.GetRequiredService<PlanStore>();
        var result = partial ? await plans.PayPartAsync(occurrence, entry, Ct) : await plans.SettleAsync(occurrence, entry, Ct);
        Assert.True(result.Succeeded); var saved = Assert.Single(await f.Actor.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(entry.Amount, saved.Amount); Assert.Equal(partial, saved.IsPartialPayment); Assert.Equal(ReviewState.Confirmed, saved.Review);
    }

    [Theory, Trait("AT", "AT-141")]
    [InlineData("kind"), InlineData("account"), InlineData("date"), InlineData("missing")]
    public async Task Fabricated_plan_markers_never_create_new_money_or_state(string changed)
    {
        var f = await CreateAsync(); f.Access.Current = new(f.Path, new(ProductPlan.Plus, f.Access.Current.Context!.Scope));
        var plan = f.Plans[0]; var day = Today.AddMonths(1);
        var entry = Occurrences.CreateEntry(Occurrences.Between(plan, [], day, day, Today).Single(), 900, Today, ReviewState.Confirmed);
        if (changed == "kind") entry.Kind = EntryKind.Income;
        else if (changed == "account") entry.AccountId = Guid.NewGuid();
        else if (changed == "date") entry.OccurrenceDate = day.AddDays(1);
        else entry.ScheduleId = Guid.NewGuid();
        var before = await SnapshotAsync(f.Actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Reader.GetRequiredService<ZananceStore>().SaveEntryAsync(entry, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Actor));
    }

    private sealed record Fixture(string Path, Account Account, List<Schedule> Plans, AccessSource Access, ServiceProvider Actor, ServiceProvider Reader);
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal int RetireAtCapture;
        internal List<string> Paths = [];
        public CommercialWriteAccess Capture(string databasePath)
        {
            Paths.Add(databasePath);
            if (RetireAtCapture == Paths.Count) Current = CommercialWriteAccess.Inactive;
            return Current;
        }
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
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    public void Dispose() { foreach (var provider in _providers) provider.Dispose(); SqliteTestPools.Clear(_directory); _directory.Dispose(); }
}
