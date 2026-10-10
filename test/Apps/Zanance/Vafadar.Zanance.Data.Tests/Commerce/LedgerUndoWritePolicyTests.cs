using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-129: actual-file ledger deletion, retry-safe refund relinking and SQLite serialization.</summary>
[Trait("AT", "AT-129")]
public sealed class LedgerUndoWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static readonly Guid Scope = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, Scope),
            shared ? new(Scope, member, host) : null);

    private ServiceProvider Provider(string path, AccessSource source, bool trigger = false)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(source).AddZananceData(path);
        if (trigger)
        {
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), source));
        }
        var provider = services.BuildServiceProvider(); provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider); return provider;
    }

    private async Task<Fixture> FixtureAsync(bool trigger = false)
    {
        var path = _directory.Combine(Guid.NewGuid() + ".db"); var source = new AccessSource(path);
        var provider = Provider(path, source, trigger); var store = provider.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Original EUR", CurrencyCode = "EUR", OpeningDate = Day, OpeningBalance = 100000 };
        var second = new Account { Name = "Other USD", CurrencyCode = "USD", OpeningDate = Day, OpeningBalance = 200000 };
        await store.SaveAccountAsync(account, Ct); await store.SaveAccountAsync(second, Ct);
        var entry = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = Day, Amount = 10000,
            Title = "Complete original purchase", Payee = "Original payee", Note = "Original note", Tags = ["retained", "original"] };
        Assert.True((await store.SaveEntryAsync(entry, Ct)).Succeeded);
        return new(path, source, provider, store, account, second, entry);
    }

    private sealed record Fixture(string Path, AccessSource Source, ServiceProvider Provider, ZananceStore Store,
        Account Account, Account Second, LedgerEntry Original)
    {
        public void Enable(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
            Source.Current = new(Path, Context(plan, shared, member, host));
        public LedgerEntry NewEntry(EntryKind kind = EntryKind.Expense, long amount = 1000) => new()
        { Kind = kind, AccountId = Account.Id, Date = Day, Amount = amount, Title = "New complete entry", Tags = ["new"] };
    }

    [Fact]
    public async Task Undo_restores_complete_receipt_bytes_metadata_and_ownership_with_the_deleted_entries()
    {
        var f = await FixtureAsync();
        var attachment = new EntryAttachment { EntryId = f.Original.Id, FileName = "owned-original.txt", ContentType = "text/plain", Data = [1, 2, 3, 4] };
        await f.Store.AddAttachmentAsync(attachment, Ct); var before = await SnapshotAsync(f.Provider);
        var deleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        Assert.Equal(attachment.Id, Assert.Single(await f.Store.GetAttachmentsAsync(f.Original.Id, Ct)).Id);
        await f.Store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(attachment.Id, Assert.Single(await f.Store.GetAttachmentsAsync(f.Original.Id, Ct)).Id);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Failed_restore_keeps_refund_links_for_a_successful_retry()
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 3000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var original = await SnapshotAsync(f.Provider);
        var deleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct); var before = await SnapshotAsync(f.Provider);
        await SqlAsync(f, "CREATE TRIGGER RejectUndo BEFORE INSERT ON Entries BEGIN SELECT RAISE(ABORT,'Owned Undo failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.RestoreEntriesAsync(deleted, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); await SqlAsync(f, "DROP TRIGGER RejectUndo");
        await f.Store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(refund.Id, Assert.Single(await f.Store.GetRefundsAsync(f.Original.Id, Ct)).Id);
        Assert.Equal(original, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Failed_delete_never_remembers_a_refund_link_that_was_later_explicitly_removed()
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 3000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var before = await SnapshotAsync(f.Provider);
        await SqlAsync(f, "CREATE TRIGGER RejectDelete BEFORE DELETE ON Entries BEGIN SELECT RAISE(ABORT,'Owned delete failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.DeleteEntryAsync(f.Original.Id, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); await SqlAsync(f, "DROP TRIGGER RejectDelete");
        refund.RefundOfId = null; Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var retained = await SnapshotAsync(f.Provider); var deleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        await f.Store.RestoreEntriesAsync(deleted, Ct);
        Assert.Null((await f.Store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);
        Assert.Equal(retained, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Refund_relink_memory_never_crosses_files_with_the_same_imported_ids()
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 3000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var original = await SnapshotAsync(f.Provider);
        var otherPath = _directory.Combine("other-owned-profile.db");
        await using (var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await db.Database.OpenConnectionAsync(Ct); using var target = new SqliteConnection("Data Source=" + otherPath);
            target.Open(); ((SqliteConnection)db.Database.GetDbConnection()).BackupDatabase(target);
        }
        var deleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath); f.Source.ExpectedPath = otherPath;
        refund.RefundOfId = null; Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var otherOriginal = await SnapshotAsync(f.Provider); var otherDeleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        await f.Store.RestoreEntriesAsync(otherDeleted, Ct);
        Assert.Null((await f.Store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);
        Assert.Equal(otherOriginal, await SnapshotAsync(f.Provider));
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(f.Path); f.Source.ExpectedPath = f.Path;
        await f.Store.RestoreEntriesAsync(deleted, Ct); Assert.Equal(original, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(ProductPlan.Free, false, true)]
    [InlineData(ProductPlan.Plus, false, true)]
    [InlineData(ProductPlan.Pro, false, true)]
    [InlineData(ProductPlan.Free, true, true)]
    [InlineData(ProductPlan.Free, true, false)]
    public async Task Owned_delete_and_Undo_remain_available_in_Free_paid_guest_and_expired_host_contexts(ProductPlan plan, bool shared, bool host)
    {
        var f = await FixtureAsync(); var refund = EntryActions.CreateRefund(f.Original, 3000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var before = await SnapshotAsync(f.Provider); f.Enable(plan, shared, host: host);
        var events = 0; f.Store.Changed += (_, _) => events++;
        var deleted = await f.Store.DeleteEntriesAsync([f.Original.Id, f.Original.Id], Ct); Assert.Single(deleted);
        Assert.Null((await f.Store.GetEntryAsync(refund.Id, Ct))!.RefundOfId);
        await f.Store.RestoreEntriesAsync(deleted, Ct); Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(2, events);
        await f.Store.RestoreEntriesAsync(deleted, Ct); Assert.Equal(2, events);
    }

    [Theory]
    [InlineData("single")]
    [InlineData("bulk")]
    [InlineData("restore")]
    [InlineData("existing")]
    public async Task Personal_Pro_never_replaces_exact_membership_for_delete_or_restore(string operation)
    {
        var f = await FixtureAsync(); IReadOnlyList<LedgerEntry> deleted = [f.Original];
        if (operation == "restore") deleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        f.Enable(ProductPlan.Pro, shared: true, member: false); var before = await SnapshotAsync(f.Provider); var events = 0;
        f.Store.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            if (operation == "single") await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
            else if (operation == "bulk") await f.Store.DeleteEntriesAsync([f.Original.Id, Guid.NewGuid()], Ct);
            else await f.Store.RestoreEntriesAsync(deleted, Ct);
        });
        Assert.Equal(FeaturePermission.RequiresMembership, error.Permission); Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cached_rights_for_another_file_reject_delete_or_restore_without_changes(bool restore)
    {
        var f = await FixtureAsync(); var deleted = restore ? await f.Store.DeleteEntryAsync(f.Original.Id, Ct) : new[] { f.Original };
        f.Source.Current = new(_directory.Combine("wrong-file.db"), Context(ProductPlan.Pro));
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        { if (restore) await f.Store.RestoreEntriesAsync(deleted, Ct); else await f.Store.DeleteEntryAsync(f.Original.Id, Ct); });
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Returned_deletion_batch_cannot_restore_into_another_selected_profile()
    {
        var f = await FixtureAsync(); var deleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        var otherPath = _directory.Combine("untouched-profile.db"); var other = Provider(otherPath, new AccessSource(otherPath));
        var before = await SnapshotAsync(other); f.Source.ExpectedPath = otherPath;
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.RestoreEntriesAsync(deleted, Ct));
        Assert.Equal(before, await SnapshotAsync(other));
        f.Source.ExpectedPath = f.Path; f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(f.Path);
        await f.Store.RestoreEntriesAsync(deleted, Ct); Assert.NotNull(await f.Store.GetEntryAsync(f.Original.Id, Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initial_database_stays_bound_when_the_profile_moves_during_access_capture(bool restore)
    {
        var f = await FixtureAsync(); var deleted = restore ? await f.Store.DeleteEntryAsync(f.Original.Id, Ct) : new[] { f.Original };
        var otherPath = _directory.Combine("untouched-profile.db"); var other = Provider(otherPath, new AccessSource(otherPath));
        var before = await SnapshotAsync(other); f.Enable();
        f.Source.OnCapture = () => f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath);
        if (restore) await f.Store.RestoreEntriesAsync(deleted, Ct); else await f.Store.DeleteEntryAsync(f.Original.Id, Ct);
        Assert.Equal(before, await SnapshotAsync(other)); f.Source.OnCapture = null;
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(f.Path);
        Assert.Equal(restore, await f.Store.GetEntryAsync(f.Original.Id, Ct) is not null);
        Assert.All(f.Source.Paths, path => Assert.Equal(f.Path, path));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SQL_or_post_SQL_access_failure_rolls_back_delete_restore_and_refund_links_for_retry(bool restore, bool retire)
    {
        var f = await FixtureAsync(trigger: retire); var refund = EntryActions.CreateRefund(f.Original, 3000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var original = await SnapshotAsync(f.Provider);
        IReadOnlyList<LedgerEntry> deleted = restore ? await f.Store.DeleteEntryAsync(f.Original.Id, Ct) : [];
        f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER FailLedgerUndo " + (retire ? "AFTER " : "BEFORE ") + (restore ? "INSERT" : "DELETE")
            + " ON Entries BEGIN SELECT " + (retire ? "RetireLedgerAccess()" : "RAISE(ABORT,'Owned operation failure')") + "; END;");
        f.Source.Retire = retire;
        async Task OperationAsync() { if (restore) await f.Store.RestoreEntriesAsync(deleted, Ct); else deleted = await f.Store.DeleteEntryAsync(f.Original.Id, Ct); }
        if (retire) await Assert.ThrowsAsync<InvalidOperationException>(OperationAsync);
        else await Assert.ThrowsAsync<DbUpdateException>(OperationAsync);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        await SqlAsync(f, "DROP TRIGGER FailLedgerUndo"); await OperationAsync();
        if (!restore) await f.Store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(original, await SnapshotAsync(f.Provider)); Assert.Equal(restore ? 1 : 2, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_writers_delete_or_restore_each_entry_once(bool restore)
    {
        var f = await FixtureAsync(); IReadOnlyList<LedgerEntry> deleted = restore ? await f.Store.DeleteEntryAsync(f.Original.Id, Ct) : [];
        var otherSource = new AccessSource(f.Path); var other = Provider(f.Path, otherSource).GetRequiredService<ZananceStore>();
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; otherSource.Rendezvous = barrier; f.Source.Captures = 0;
        var events = 0; f.Store.Changed += (_, _) => Interlocked.Increment(ref events); other.Changed += (_, _) => Interlocked.Increment(ref events);
        async Task<int> OperationAsync(ZananceStore store)
        { if (restore) { await store.RestoreEntriesAsync(deleted, Ct); return 0; } return (await store.DeleteEntryAsync(f.Original.Id, Ct)).Count; }
        var results = await Task.WhenAll(Task.Run(() => OperationAsync(f.Store), Ct), Task.Run(() => OperationAsync(other), Ct));
        Assert.Equal(1, events); Assert.Equal(restore ? 0 : 1, results.Sum());
        Assert.Equal(restore ? 1 : 0, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Deleting_one_split_part_returns_the_complete_group_and_its_explicit_refund_links()
    {
        var f = await FixtureAsync(); var (parts, removed) = EntryActions.Split([f.Original.Copy()], [(null, 6000), (null, 4000)]);
        await f.Store.SaveEntriesAsync(parts, removed, Ct); var refund = EntryActions.CreateRefund(parts[0], 3000, f.Account.Id, Day);
        await f.Store.SaveEntryAsync(refund, Ct); var before = await SnapshotAsync(f.Provider); f.Enable();
        var deleted = await f.Store.DeleteEntriesAsync([parts[1].Id, parts[1].Id], Ct); Assert.Equal(2, deleted.Count);
        Assert.Equal(refund.Id, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Id);
        await f.Store.RestoreEntriesAsync(deleted, Ct); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Derived_occurrence_failure_rolls_back_the_whole_delete_or_Undo(bool restore, bool partial)
    {
        var f = await FixtureAsync(); var plans = f.Provider.GetRequiredService<PlanStore>();
        var plan = new Schedule { Name = "Owned due expense", AccountId = f.Account.Id, Amount = 10000, Rule = new() { Start = Day } };
        await plans.SaveScheduleAsync(plan, Ct);
        var occurrence = Assert.Single(Occurrences.Between(plan, [], Day, Day, Day));
        var entry = Occurrences.CreateEntry(occurrence, partial ? 3000 : 10000, Day, ReviewState.Confirmed);
        Assert.True((partial ? await plans.PayPartAsync(occurrence, entry, Ct) : await plans.SettleAsync(occurrence, entry, Ct)).Succeeded);
        IReadOnlyList<LedgerEntry> deleted = restore ? await f.Store.DeleteEntryAsync(entry.Id, Ct) : [];
        f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RejectOccurrence BEFORE UPDATE ON OccurrenceStates BEGIN SELECT RAISE(ABORT,'Owned occurrence failure'); END;");
        async Task OperationAsync() { if (restore) await f.Store.RestoreEntriesAsync(deleted, Ct); else deleted = await f.Store.DeleteEntryAsync(entry.Id, Ct); }
        await Assert.ThrowsAsync<DbUpdateException>(OperationAsync); Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
        await SqlAsync(f, "DROP TRIGGER RejectOccurrence"); await OperationAsync();
        if (!restore) await f.Store.RestoreEntriesAsync(deleted, Ct);
        var state = Assert.Single(await plans.GetStatesAsync(plan.Id, Ct)); Assert.Equal(partial ? 3000 : 0, state.PaidAmount);
        Assert.Equal(partial ? OccurrenceStatus.Open : OccurrenceStatus.Settled, state.Status); Assert.Equal(entry.Amount, (await f.Store.GetEntryAsync(entry.Id, Ct))!.Amount);
    }

    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider, bool omitEntries = false)
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
            if (omitEntries && table == "Entries") continue;
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

    /// <summary>Synchronous independent cached facts, with coordination before the actual writer wait.</summary>
    private sealed class AccessSource(string path) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Barrier? Rendezvous = null;
        public int Captures;
        public bool Retire;
        public int Retired;
        public Action? OnCapture = null;
        public string ExpectedPath = System.IO.Path.GetFullPath(path);
        public readonly List<string> Paths = [];
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(ExpectedPath, databasePath); lock (Paths) Paths.Add(databasePath);
            OnCapture?.Invoke();
            if (Rendezvous is { } barrier && Interlocked.Increment(ref Captures) <= 2
                && !barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("Ledger writers did not rendezvous.");
            return Current;
        }
    }

    /// <summary>Changes only cached fixture rights from the production factory's actual native SQL connection.</summary>
    private sealed class TriggerConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireLedgerAccess", () =>
            {
                if (source.Retire)
                {
                    source.Current = new(source.Current.DatabasePath!, Context(ProductPlan.Free)); source.Retire = false; source.Retired++;
                }
                return 1;
            });
            return db;
        }
    }

    /// <summary>Stable audit time for complete stored-column comparisons.</summary>
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
