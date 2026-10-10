using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-126: actual restore destination and retained import/recovery data across profile changes.</summary>
[Trait("AT", "AT-126")]
public sealed class RecoveryWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ServiceProvider Provider(string path, AccessSource? source = null, bool retireAfterSave = false)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime());
        if (source is not null) services.AddSingleton<ICommercialWriteAccessSource>(source);
        services.AddZananceData(path);
        if (retireAfterSave)
        {
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(provider => new RetiringConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!), source!));
        }
        var provider = services.BuildServiceProvider();
        provider.MigrateLocalDatabase<ZananceDbContext>(); _providers.Add(provider); return provider;
    }

    [Fact]
    public async Task Restore_remains_in_its_original_database_when_profile_moves_while_reading_stream()
    {
        var path = _directory.Combine("original.db"); var otherPath = _directory.Combine("other.db");
        var original = Provider(path); var other = Provider(otherPath); var replacement = Provider(_directory.Combine("replacement.db"));
        await original.GetRequiredService<ZananceStore>().SaveAccountAsync(new() { Name = "Original", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 10) }, Ct);
        await other.GetRequiredService<ZananceStore>().SaveAccountAsync(new() { Name = "Other profile", CurrencyCode = "USD", OpeningDate = new(2026, 10, 10) }, Ct);
        await replacement.GetRequiredService<ZananceStore>().SaveAccountAsync(new() { Name = "Restored", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 10) }, Ct);
        using var bytes = new MemoryStream();
        await replacement.GetRequiredService<IBackupSource>().WriteAsync(bytes, Ct);
        using var stream = new SwitchingStream(bytes.ToArray(), () => original.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath));
        await original.GetRequiredService<IBackupSource>().RestoreAsync(stream, Ct);
        Assert.Equal("Other profile", Assert.Single(await other.GetRequiredService<ZananceStore>().GetAccountsAsync(cancellationToken: Ct)).Name);
        Assert.Equal("Restored", Assert.Single(await Provider(path).GetRequiredService<ZananceStore>().GetAccountsAsync(cancellationToken: Ct)).Name);
    }


    private static readonly DateOnly Day = new(2026, 10, 1);
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private async Task<Fixture> FixtureAsync(bool retireAfterSave = false)
    {
        var path = _directory.Combine(Guid.NewGuid()+".db"); var source = new AccessSource(path);
        var provider = Provider(path, source, retireAfterSave); var store = provider.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Original account", CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 50000 };
        await store.SaveAccountAsync(account, Ct);
        var aggregate = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 10000,
            Date = Day.AddDays(29), IsAggregated = true, AggregatedFrom = Day, AggregatedTo = Day.AddDays(29),
            Title = "Original monthly total", Note = "Full original metadata", Tags = ["retained", "original"] };
        Assert.True((await store.SaveEntryAsync(aggregate, Ct)).Succeeded);
        return new(path, provider, source, store, account, aggregate);
    }

    private sealed record Fixture(string Path, ServiceProvider Provider, AccessSource Source, ZananceStore Store,
        Account Account, LedgerEntry Aggregate)
    {
        public IBackupSource Backup => Provider.GetRequiredService<IBackupSource>();
        public void Enable(ProductPlan product = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
            Source.Current = new(Path, Context(product, shared, member, host));
        public LedgerEntry Detail(long amount = 2500) => new() { AccountId = Account.Id, Kind = EntryKind.Expense,
            Amount = amount, Date = Day.AddDays(3), Title = "Imported detail", Note = "Complete detail", Tags = ["detail"] };
        public async Task<List<ImportAggregateChoice>> ChoicesAsync(IReadOnlyList<LedgerEntry> entries, bool link = true) =>
            AggregatedEntries.FindForImport(entries, await Store.GetEntriesAsync(cancellationToken: Ct)).Select(o => new ImportAggregateChoice(o, link)).ToList();
    }

    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Owned_import_and_durable_Undo_keep_all_metadata_above_Free_quota_and_after_host_expiry(bool enabled, bool expiredHost)
    {
        var f = await FixtureAsync();
        for (var i = 0; i < 4; i++) await f.Store.SaveAccountAsync(new() { Name = "Over-quota " + i, CurrencyCode = "EUR", OpeningDate = Day }, Ct);
        if (enabled) f.Enable(shared: expiredHost, host: !expiredHost);
        var before = await SnapshotAsync(f.Provider); var detail = f.Detail(); var choices = await f.ChoicesAsync([detail]);
        var events = 0; f.Store.Changed += (_, _) => events++;
        var result = await f.Store.ImportAsync([detail], choices, Ct); Assert.Null(result.Conflict); Assert.Empty(result.Errors);
        Assert.Equal(1, result.Imported); Assert.Equal(7500, (await f.Store.GetEntryAsync(f.Aggregate.Id, Ct))!.Amount);
        Assert.Equal(10000, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
        Assert.Equal(1, events);
        var restarted = Provider(f.Path, f.Source).GetRequiredService<ZananceStore>();
        Assert.False((await restarted.TryUndoImportAsync(result.BatchId!.Value, Ct)).Conflict);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        Assert.Equal(5, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Count);
    }

    [Theory]
    [InlineData("import")]
    [InlineData("undo")]
    [InlineData("write-backup")]
    [InlineData("restore")]
    public async Task Personal_Pro_cannot_replace_membership_at_recovery_boundaries(string operation)
    {
        var f = await FixtureAsync(); var detail = f.Detail(); var result = await f.Store.ImportAsync([detail], await f.ChoicesAsync([detail]), Ct);
        using var package = new MemoryStream(); await f.Backup.WriteAsync(package, Ct);
        f.Enable(ProductPlan.Pro, shared: true, member: false); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        using var destination = new MemoryStream(); var read = false;
        using var input = new SwitchingStream(package.ToArray(), () => read = true);
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            switch (operation)
            {
                case "import": await f.Store.ImportAsync([f.Detail(1000)], [], Ct); break;
                case "undo": await f.Store.TryUndoImportAsync(result.BatchId!.Value, Ct); break;
                case "write-backup": await f.Backup.WriteAsync(destination, Ct); break;
                case "restore": await f.Backup.RestoreAsync(input, Ct); break;
            }
        });
        Assert.Equal(FeaturePermission.RequiresMembership, error.Permission);
        Assert.False(read); Assert.Equal(0, destination.Length); Assert.Equal(0, events);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(ProductPlan.Free, false)]
    [InlineData(ProductPlan.Plus, false)]
    [InlineData(ProductPlan.Pro, false)]
    [InlineData(ProductPlan.Free, true)]
    public async Task Recovery_preserves_over_quota_accounts_goals_templates_and_settings_without_reviving_paid_facts(ProductPlan product, bool expiredHost)
    {
        var f = await FixtureAsync();
        var goals = f.Provider.GetRequiredService<GoalStore>();
        for (var i = 0; i < 4; i++)
        {
            await f.Store.SaveAccountAsync(new() { Name = "Preserved " + i, CurrencyCode = "EUR", OpeningDate = Day }, Ct);
            await f.Store.SaveTemplateAsync(new() { Name = "Template " + i, AccountId = f.Account.Id, Kind = EntryKind.Expense, Amount = 1000 }, Ct);
        }
        for (var i = 0; i < 2; i++) await goals.SaveGoalWithContributionPlanAsync(new() { Name = "Advanced goal " + i,
            CurrencyCode = "EUR", TargetAmount = 10000, Type = GoalType.Earmark }, new() { Amount = 1000, Rule = new() { Start = Day } }, Ct);
        await f.Store.UpdateSettingsAsync(s => { s.MonthStartDay = 5; s.DefaultCurrencyCode = "EUR"; }, Ct);
        var before = await SnapshotAsync(f.Provider); f.Enable(product, shared: expiredHost, host: !expiredHost); var facts = f.Source.Current;
        using var package = new MemoryStream(); await f.Backup.WriteAsync(package, Ct);
        // Make a real data difference without using an enforced creation path or changing cached paid facts.
        await SqlAsync(f, "UPDATE Accounts SET Name='Temporary changed name'");
        package.Position = 0; await f.Backup.RestoreAsync(package, Ct);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(facts, f.Source.Current);
        Assert.Equal(5, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Count);
        Assert.Equal(4, (await f.Store.GetTemplatesAsync(Ct)).Count); Assert.Equal(2, (await goals.GetGoalsAsync(Ct)).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Access_changed_during_restore_input_is_rechecked_before_destination_replacement(bool revokeMembership)
    {
        var f = await FixtureAsync(); using var package = new MemoryStream(); await f.Backup.WriteAsync(package, Ct);
        await SqlAsync(f, "UPDATE Accounts SET Name='Preserved before replacement'"); f.Enable(ProductPlan.Plus);
        var before = await SnapshotAsync(f.Provider);
        using var input = new SwitchingStream(package.ToArray(), () => f.Enable(ProductPlan.Pro, shared: revokeMembership, member: !revokeMembership));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Backup.RestoreAsync(input, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Binding_a_restore_to_the_first_file_does_not_consult_a_new_profiles_rights()
    {
        var f = await FixtureAsync(); using var package = new MemoryStream(); await f.Backup.WriteAsync(package, Ct);
        var other = Provider(_directory.Combine("untouched-other.db")); var otherPath = other.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        await other.GetRequiredService<ZananceStore>().SaveAccountAsync(new() { Name = "Other untouched", CurrencyCode = "USD", OpeningDate = Day }, Ct);
        var before = await SnapshotAsync(other); await SqlAsync(f, "UPDATE Accounts SET Name='Before recovery'"); f.Enable();
        using var input = new SwitchingStream(package.ToArray(), () => f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath));
        await f.Backup.RestoreAsync(input, Ct);
        Assert.Equal(before, await SnapshotAsync(other));
        Assert.Equal("Original account", Assert.Single(await Provider(f.Path).GetRequiredService<ZananceStore>().GetAccountsAsync(cancellationToken: Ct)).Name);
        Assert.All(f.Source.Paths, path => Assert.Equal(f.Path, path));
    }

    [Theory]
    [InlineData("import")]
    [InlineData("undo")]
    public async Task SQL_trigger_access_retirement_rolls_back_aggregate_details_and_durable_journal(string operation)
    {
        var f = await FixtureAsync(retireAfterSave: true); var detail = f.Detail(); Guid? batch = null;
        if (operation == "undo") batch = (await f.Store.ImportAsync([detail], await f.ChoicesAsync([detail]), Ct)).BatchId;
        f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, operation == "import"
            ? "CREATE TRIGGER RetireOnJournal AFTER INSERT ON ImportLinks BEGIN SELECT RetireRecoveryAccess(); END;"
            : "CREATE TRIGGER RetireOnUndo AFTER DELETE ON ImportLinks BEGIN SELECT RetireRecoveryAccess(); END;");
        f.Source.RetireAfterSave = true;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (operation == "import") await f.Store.ImportAsync([detail], await f.ChoicesAsync([detail]), Ct);
            else await f.Store.TryUndoImportAsync(batch!.Value, Ct);
        });
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events); Assert.Equal(1, f.Source.RetiredWrites);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_import_or_Undo_journal_write_rolls_back_all_stored_rows(bool undo)
    {
        var f = await FixtureAsync(); var detail = f.Detail(); Guid? batch = null;
        if (undo) batch = (await f.Store.ImportAsync([detail], await f.ChoicesAsync([detail]), Ct)).BatchId;
        f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RejectJournal BEFORE " + (undo ? "DELETE" : "INSERT") + " ON ImportLinks BEGIN SELECT RAISE(ABORT, 'Owned journal failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            if (undo) await f.Store.TryUndoImportAsync(batch!.Value, Ct);
            else await f.Store.ImportAsync([detail], await f.ChoicesAsync([detail]), Ct);
        });
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_duplicate_imports_under_the_writer_commit_each_identity_once(bool enabled)
    {
        var f = await FixtureAsync(); var second = Provider(f.Path, f.Source).GetRequiredService<ZananceStore>(); if (enabled) f.Enable();
        var detail = f.Detail(); var choices = await f.ChoicesAsync([detail]); using var barrier = new Barrier(2);
        f.Source.Rendezvous = barrier; f.Source.Captures = 0; var events = 0;
        f.Store.Changed += (_, _) => Interlocked.Increment(ref events); second.Changed += (_, _) => Interlocked.Increment(ref events);
        var results = await Task.WhenAll(Task.Run(() => f.Store.ImportAsync([detail], choices, Ct), Ct), Task.Run(() => second.ImportAsync([detail], choices, Ct), Ct));
        Assert.Equal(new[] { 0, 1 }, results.Select(r => r.Imported).Order().ToArray()); Assert.Equal(1, events);
        Assert.Equal(7500, (await f.Store.GetEntryAsync(f.Aggregate.Id, Ct))!.Amount);
        Assert.Single(await f.Store.GetImportBatchesAsync(Ct)); Assert.Equal(10000, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Sum(e => e.Amount));
    }

    [Fact]
    public async Task Mismatched_file_access_rejects_backup_import_and_Undo_without_consuming_input()
    {
        var f = await FixtureAsync(); var before = await SnapshotAsync(f.Provider);
        f.Source.Current = new(_directory.Combine("wrong.db"), Context(ProductPlan.Pro));
        using var output = new MemoryStream(); var read = false; using var input = new SwitchingStream([], () => read = true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Backup.WriteAsync(output, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Backup.RestoreAsync(input, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ImportAsync([f.Detail()], [], Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.TryUndoImportAsync(Guid.NewGuid(), Ct));
        Assert.False(read); Assert.Equal(0, output.Length); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Invalid_restore_snapshot_keeps_complete_destination_rows()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        using var input = new MemoryStream([1, 2, 3]);
        var error = await Assert.ThrowsAsync<BackupException>(() => f.Backup.RestoreAsync(input, Ct));
        Assert.Equal(BackupError.Corrupted, error.Error); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Backup_rechecks_before_native_snapshot_and_before_writing_output(int retireOnCapture)
    {
        var f = await FixtureAsync(); f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider);
        f.Source.AccessReads = 0;
        f.Source.OnCapture = count => { if (count == retireOnCapture) f.Enable(ProductPlan.Pro, shared: true, member: false); };
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Backup.WriteAsync(output, Ct));
        Assert.Equal(0, output.Length); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public void Database_source_replacement_preserves_other_backup_sources_and_has_one_database_entry()
    {
        var services = new ServiceCollection().AddSingleton<IBackupSource>(new AdditionalSource())
            .AddZananceData(_directory.Combine("registration.db")).BuildServiceProvider();
        _providers.Add(services); var sources = services.GetServices<IBackupSource>().ToList();
        Assert.Equal(2, sources.Count); Assert.Single(sources, s => s.Name == "additional.fixture");
        Assert.Single(sources, s => s.Name == SqliteDatabaseBackupSource<ZananceDbContext>.EntryName);
    }

    [Fact]
    public async Task Cancelled_restore_input_keeps_every_destination_row()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        using var input = new CancellingStream();
        await Assert.ThrowsAsync<OperationCanceledException>(() => f.Backup.RestoreAsync(input, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Old_schema_restore_migrates_the_same_initial_file_after_a_profile_move()
    {
        var f = await FixtureAsync(); var other = Provider(_directory.Combine("other-old-schema.db"));
        var otherBefore = await SnapshotAsync(other); var otherPath = other.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        var old = new ServiceCollection().AddZananceData(_directory.Combine("first-schema.db")).BuildServiceProvider(); _providers.Add(old);
        await using (var db = await old.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().First(), Ct);
            var accountId = Guid.NewGuid();
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO Accounts (Id, Name, Type, CurrencyCode, OpeningBalance, OpeningDate, OpeningBalanceKnown, IncludeInTotals, IsArchived, SortOrder, CreatedAt, UpdatedAt) VALUES ({accountId}, 'Checking', 0, 'EUR', 1000, '2026-01-01', 1, 1, 0, 0, 0, 0)", Ct);
        }
        using var package = new MemoryStream(); await old.GetRequiredService<IBackupSource>().WriteAsync(package, Ct); f.Enable();
        using var input = new SwitchingStream(package.ToArray(), () => f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath));
        await f.Backup.RestoreAsync(input, Ct); Assert.Equal(otherBefore, await SnapshotAsync(other));
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(f.Path);
        var saved = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal("Checking", saved.Name); Assert.Equal(1000, saved.OpeningBalance);
        await using var restored = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await restored.Database.GetPendingMigrationsAsync(Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteConnection.ClearAllPools(); _directory.Dispose();
    }

    /// <summary>A real readable snapshot whose asynchronous copy changes only the independent fixture's profile.</summary>
    private sealed class SwitchingStream(byte[] buffer, Action change) : MemoryStream(buffer)
    {
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            change(); return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }
    }

    /// <summary>A cancelled real input copy; no SQLite or application algorithm is substituted.</summary>
    private sealed class CancellingStream : MemoryStream
    {
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
            Task.FromException(new OperationCanceledException("Owned input cancelled."));
    }

    /// <summary>A separate registered entry used only to check that database registration preserves other sources.</summary>
    private sealed class AdditionalSource : IBackupSource
    {
        public string Name => "additional.fixture";
        public Task WriteAsync(Stream destination, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RestoreAsync(Stream source, CancellationToken cancellationToken) => Task.CompletedTask;
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
        var result = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \""+table+"\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); result[table] = rows;
        }
        return JsonSerializer.Serialize(result);
    }

    /// <summary>Cached exact-scope facts plus a rendezvous before actual writer acquisition, never fake recovery rows.</summary>
    private sealed class AccessSource(string path) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Barrier? Rendezvous;
        public int Captures;
        public Action<int>? OnCapture;
        public int AccessReads;
        public readonly List<string> Paths = [];
        public bool RetireAfterSave;
        public int RetiredWrites;
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(System.IO.Path.GetFullPath(path), databasePath);
            lock (Paths) Paths.Add(databasePath);
            OnCapture?.Invoke(Interlocked.Increment(ref AccessReads));
            if (Rendezvous is { } barrier && Interlocked.Increment(ref Captures) <= 2
                && !barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("The writers did not rendezvous.");
            return Current;
        }
    }

    /// <summary>Decorates actual SQLite connections for a trigger-driven cached-access change after a journal write.</summary>
    private sealed class RetiringConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireRecoveryAccess", () =>
            {
                if (source.RetireAfterSave)
                {
                    source.Current = new(source.Current.DatabasePath!, Context(ProductPlan.Free));
                    source.RetireAfterSave = false; source.RetiredWrites++;
                }
                return 1;
            });
            return db;
        }
    }

    /// <summary>Stable local time keeps full audit metadata comparable across delete and Undo.</summary>
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
