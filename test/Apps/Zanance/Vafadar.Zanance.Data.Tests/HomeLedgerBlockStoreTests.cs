using System.Text.Json;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vafadar.Zanance.Data.CompiledModel;
using Vafadar.Zanance.Performance;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Original SQLite writers/transactions and complete scalar reads govern optional derived transport.</summary>
[Trait("AT", "AT-82")]
public sealed class HomeLedgerBlockStoreTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private ServiceProvider? provider;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Read_timing_observes_only_owned_contexts_without_source_values_or_changing_transport()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!;
        using var measured = new IsolatedHomeReadFactory(services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),
            TimeProvider.System, useDerivedTransport: true);
        await using (var foreign = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            Assert.Equal(2, await foreign.Entries.CountAsync(Ct));
        Assert.Empty(measured.CaptureReadEvents().Events);

        await using (var read = await measured.CreateDbContextAsync(Ct))
        {
            Assert.True(read.CanUseHomeLedgerBlocks);
            await using var transaction = await read.Database.BeginTransactionAsync(Ct);
            Assert.Equal(2, (await ZananceStore.ReadCompleteHomeLedgerAsync(read, Ct)).Count);
        }
        var trace = measured.CaptureReadEvents();
        Assert.Equal(1, trace.OwnedContexts); Assert.Equal(0, trace.DroppedEvents);
        Assert.True(trace.FirstTimestampTicks > 0);
        Assert.Contains(trace.Events, value => value.Phase == "connection-opening");
        Assert.Contains(trace.Events, value => value.Phase == "connection-opened");
        Assert.Contains(trace.Events, value => value.Phase == "command-executing");
        Assert.Contains(trace.Events, value => value.Phase == "command-executed");
        Assert.Contains(trace.Events, value => value.Phase == "connection-closed");
        Assert.All(trace.Events, value => { Assert.Equal(1, value.Context); Assert.True(value.Milliseconds >= 0); });
        Assert.Equal(trace.Events.OrderBy(value => value.Milliseconds), trace.Events);
        var json = JsonSerializer.Serialize(trace);
        Assert.DoesNotContain(services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path, json, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Entries", json, StringComparison.Ordinal);
        Assert.Equal(1, measured.DerivedBlockReads); Assert.Equal(0, measured.CompleteScalarReads);
    }

    /// <summary>Actual EF command families never copy query parameters, arbitrary SQL or foreign activity into the trace.</summary>
    [Fact]
    public async Task Owned_query_labels_keep_parameters_and_foreign_queries_outside_trace()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!;
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        using var measured = new IsolatedHomeReadFactory(factory, TimeProvider.System, useDerivedTransport: true);
        await using (var foreign = await factory.CreateDbContextAsync(Ct))
            _ = await foreign.Budgets.CountAsync(Ct);
        Assert.Empty(measured.CaptureReadEvents().Events);

        const string parameter = "Fictitious parameter with FROM \"Budgets\" and no retained value";
        await using (var read = await measured.CreateDbContextAsync(Ct))
        {
            Assert.Empty(await read.Accounts.Where(account => account.Name == parameter).Select(account => account.Id).ToListAsync(Ct));
            _ = await read.Database.ExecuteSqlRawAsync("SELECT 1", Ct);
        }
        var trace = measured.CaptureReadEvents();
        Assert.Equal(1, trace.OwnedContexts); Assert.Equal(0, trace.DroppedEvents);
        Assert.Contains(trace.Events, value => value.Phase == "command-executed" && value.Kind == "accounts");
        Assert.Contains(trace.Events, value => value.Phase == "command-executed" && value.Kind == "other");
        Assert.DoesNotContain(trace.Events, value => value.Kind == "budgets");
        var json = JsonSerializer.Serialize(trace);
        Assert.DoesNotContain(parameter, json, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Budgets", json, StringComparison.Ordinal);
        Assert.DoesNotContain(services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Independent_candidate_observation_keeps_eligibility_and_proves_the_actual_block_read()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!;
        using var measured = new IsolatedHomeReadFactory(services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),
            TimeProvider.System, useDerivedTransport: true);
        await using (var read = await measured.CreateDbContextAsync(Ct))
        {
            Assert.True(read.CanUseHomeLedgerBlocks);
            await using var transaction = await read.Database.BeginTransactionAsync(Ct);
            Assert.Equal(2, (await ZananceStore.ReadCompleteHomeLedgerAsync(read, Ct)).Count);
        }
        Assert.Equal(0, measured.CompleteScalarReads); Assert.Equal(1, measured.DerivedBlockReads);
        Assert.True(measured.DerivedTransportReads > 0);
    }

    [Fact]
    public async Task Independent_schema_retirement_preserves_original_rows_and_recovers_without_repeating_Down()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!; var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = location.Access.AcquireExclusive();
        await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, Ct);
        await using var before = await CopyAsync(db);
        var comparison = Guid.NewGuid().ToString("N"); var request = directory.Combine("retire-request.json");
        var result = await ReadBlockSchemaRetirement.RetireAsync(db, before, request, comparison, "original-scope", owner.Validate, SaveRequestAsync, Ct);
        Assert.False(result.AlreadyRetired); Assert.True(result.CompleteOriginalSourceVerified); Assert.Equal(0, result.FinancialCommands);
        Assert.Equal(ReadBlockSchemaRetirement.Target, (await db.Database.GetAppliedMigrationsAsync(Ct)).Last());
        Assert.Equal(2, await db.Entries.CountAsync(Ct));
        await using var after = await CopyAsync(db);
        var schemaBeforeRecovery = await SchemaCookieAsync(db);
        var recovered = await ReadBlockSchemaRetirement.RetireAsync(db, after, request, comparison, "original-scope", owner.Validate,
            (_, _, _) => throw new InvalidOperationException("Never replace the original request."), Ct);
        Assert.True(recovered.AlreadyRetired); Assert.Equal(result.ExpectedFingerprint, recovered.ExpectedFingerprint);
        Assert.Equal(schemaBeforeRecovery, await SchemaCookieAsync(db));
    }

    [Fact]
    public async Task Newer_original_source_refuses_a_pending_retirement_request_without_overwriting_it()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!; var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = location.Access.AcquireExclusive();
        await using var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, Ct);
        await using var before = await CopyAsync(db);
        var comparison = Guid.NewGuid().ToString("N"); var request = directory.Combine("retire-request.json");
        await Assert.ThrowsAsync<IOException>(() => ReadBlockSchemaRetirement.RetireAsync(db, before, request, comparison, "original-scope", owner.Validate,
            async (path, json, token) => { await SaveRequestAsync(path, json, token); throw new IOException("Fictitious durability observation failure."); }, Ct));
        Assert.Equal(ReadBlockSchemaRetirement.Current, (await db.Database.GetAppliedMigrationsAsync(Ct)).Last());
        var requestBytes = await File.ReadAllBytesAsync(request, Ct);
        (await db.Entries.FirstAsync(Ct)).Title = "Newer original title"; await db.SaveChangesAsync(Ct);
        await using var newer = await CopyAsync(db);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadBlockSchemaRetirement.RetireAsync(db, newer, request, comparison, "original-scope", owner.Validate, SaveRequestAsync, Ct));
        Assert.Equal(requestBytes, await File.ReadAllBytesAsync(request, Ct));
        Assert.Equal(ReadBlockSchemaRetirement.Current, (await db.Database.GetAppliedMigrationsAsync(Ct)).Last());
        Assert.Contains(await db.Entries.ToListAsync(Ct), entry => entry.Title == "Newer original title");
    }

    [Fact]
    public async Task An_undeclared_schema_object_refuses_retirement_before_touching_the_original_database()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER ExtraCacheObject AFTER INSERT ON HomeLedgerReadBlocks BEGIN SELECT 1; END;", Ct);
        await using var before = await CopyAsync(db);
        var request = directory.Combine("retire-request.json");
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadBlockSchemaRetirement.RetireAsync(db, before, request,
            Guid.NewGuid().ToString("N"), "original-scope", () => { }, SaveRequestAsync, Ct));
        Assert.False(File.Exists(request));
        Assert.Equal(ReadBlockSchemaRetirement.Current, (await db.Database.GetAppliedMigrationsAsync(Ct)).Last());
        Assert.Equal(2, await db.Entries.CountAsync(Ct));
    }

    private static async Task<SqliteConnection> CopyAsync(ZananceDbContext db)
    {
        await db.Database.OpenConnectionAsync(Ct);
        var copy = new SqliteConnection("Data Source=:memory:;Pooling=False"); await copy.OpenAsync(Ct);
        ((SqliteConnection)db.Database.GetDbConnection()).BackupDatabase(copy); return copy;
    }

    private static async Task<long> SchemaCookieAsync(ZananceDbContext db)
    {
        using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = "PRAGMA schema_version";
        return (long)(await command.ExecuteScalarAsync(Ct))!;
    }

    private static async Task SaveRequestAsync(string path, string json, CancellationToken ct)
    {
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await file.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json), ct); await file.FlushAsync(ct); file.Flush(flushToDisk: true);
    }

    [Fact]
    public async Task Independent_oracle_uses_actual_scalar_reads_retains_factory_ownership_and_refuses_writes()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var services = provider!;
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using (var scalar = new IsolatedHomeReadFactory(factory, TimeProvider.System))
        {
            await using (var read = await scalar.CreateDbContextAsync(Ct))
            {
                Assert.False(read.CanUseHomeLedgerBlocks);
                Assert.Throws<InvalidOperationException>(() => scalar.Dispose());
                await using var transaction = await read.Database.BeginTransactionAsync(Ct);
                Assert.Equal(2, (await ZananceStore.ReadCompleteHomeLedgerAsync(read, Ct)).Count);
                await Assert.ThrowsAsync<InvalidOperationException>(() => read.Database.ExecuteSqlRawAsync("DELETE FROM Entries", Ct));
            }
            Assert.Equal(1, scalar.CompleteScalarReads); Assert.Equal(0, scalar.DerivedTransportReads);
            Assert.Throws<InvalidOperationException>(() => location.Access.AcquireExclusive());
        }
        using var owner = location.Access.AcquireExclusive();
        await using var current = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, Ct);
        Assert.Equal(2, await current.Entries.CountAsync(Ct));
    }

    [Fact]
    public async Task Derived_reads_keep_the_original_maintenance_publication_guard_on_every_command()
    {
        var seeded = await SeedAsync(); await seeded.DisposeAsync();
        var location = provider!.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        await using var db = await provider!.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner, Ct);
        Assert.True(db.CanUseHomeLedgerBlocks);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.NotNull(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        owner.Dispose();
        await Assert.ThrowsAsync<InvalidOperationException>(() => HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => HomeLedgerReadBlocks.MaintainAsync(db, Ct));
    }

    [Fact]
    public async Task Cancellation_after_a_derived_block_write_rolls_back_original_money_and_transport_together()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        using var observer = new BlockWriteLogger();
        await using var seeded = await SeedAsync();
        var path = provider!.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        using var logs = LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Information).AddProvider(observer));
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite($"Data Source={path};Pooling=False")
            .UseModel(ZananceDbContextModel.Instance).UseLoggerFactory(logs).Options;
        await using var db = new ZananceDbContext(options);
        var before = await StateAsync(db);
        await using (var transaction = await db.Database.BeginTransactionAsync(Ct))
        {
            (await db.Entries.FirstAsync(Ct)).Amount = 678;
            await db.SaveChangesAsync(Ct);
            observer.AfterWrite = cancellation.Cancel;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transaction.CommitAsync(cancellation.Token));
            observer.AfterWrite = null;
            Assert.Equal(1, observer.ObservedWrites);
            await transaction.RollbackAsync(Ct);
        }
        Assert.Equal(before, await StateAsync(db)); db.ChangeTracker.Clear();
        Assert.DoesNotContain(await db.Entries.ToListAsync(Ct), entry => entry.Amount == 678);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.NotNull(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
    }

    [Fact]
    public async Task Unknown_command_observer_keeps_the_original_complete_scalar_path()
    {
        await using var seeded = await SeedAsync();
        var path = provider!.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        var observer = new OriginalLedgerObserver();
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite($"Data Source={path};Pooling=False")
            .UseModel(ZananceDbContextModel.Instance).AddInterceptors(observer).Options;
        await using var db = new ZananceDbContext(options);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.False(db.CanUseHomeLedgerBlocks); Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Equal(2, (await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)).Count);
        Assert.Equal(1, observer.OriginalReads);
    }

    [Fact]
    public async Task Matching_digest_cannot_promote_an_unsupported_block_header()
    {
        await using var db = await SeedAsync();
        await db.Database.OpenConnectionAsync(Ct);
        using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT Payload FROM HomeLedgerReadBlocks LIMIT 1";
            var payload = (byte[])(await command.ExecuteScalarAsync(Ct))!;
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4), 999);
            command.CommandText = "UPDATE HomeLedgerReadBlocks SET Payload=$payload, Digest=$digest";
            command.Parameters.Add(new SqliteParameter("$payload", SqliteType.Blob) { Value = payload });
            command.Parameters.Add(new SqliteParameter("$digest", SqliteType.Blob) { Value = SHA256.HashData(payload) });
            await command.ExecuteNonQueryAsync(Ct);
        }
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        var before = await StateAsync(db);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Equal(2, (await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)).Count);
        Assert.Equal(before, await StateAsync(db));
    }

    [Fact]
    public async Task Actual_additive_migration_prepares_existing_history_before_the_first_Home_getter()
    {
        provider = new ServiceCollection().AddZananceData(directory.Combine("existing.db")).BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var old = await factory.CreateDbContextAsync(Ct))
        {
            await old.GetService<IMigrator>().MigrateAsync("20261024115108_HomeFinancialRevision", Ct);
            var account = new Account { Name = "Existing", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            old.Accounts.Add(account);
            old.Entries.Add(new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 2), Amount = 543, Kind = EntryKind.Income });
            await old.SaveChangesAsync(Ct);
        }
        provider.MigrateLocalDatabase<ZananceDbContext>();
        await using var db = await factory.CreateDbContextAsync(Ct);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        var rows = await HomeLedgerReadBlocks.TryReadAsync(db, Ct); Assert.NotNull(rows);
        Assert.Equal(543, Assert.Single(rows).Amount);
    }

    [Fact]
    public async Task Original_outer_commit_refreshes_changed_values_before_publishing()
    {
        await using var db = await SeedAsync();
        await using (var transaction = await db.Database.BeginTransactionAsync(Ct))
        {
            var entry = await db.Entries.FirstAsync(Ct); entry.Amount = 876; entry.Tags.Add("retained edit");
            await db.SaveChangesAsync(Ct); Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
            await transaction.CommitAsync(Ct);
        }
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        var cached = await HomeLedgerReadBlocks.TryReadAsync(db, Ct); Assert.NotNull(cached);
        Assert.Contains(cached, entry => entry.Amount == 876 && entry.Tags.Contains("retained edit"));
        Assert.Equal(JsonSerializer.Serialize(await CompleteHomeLedgerReader.ReadAsync(db, Ct)), JsonSerializer.Serialize(cached));
    }

    [Fact]
    public async Task Unsupported_original_tag_storage_declines_transport_without_rejecting_an_unrelated_writer()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE Entries SET Tags='malformed original JSON' WHERE rowid=1", Ct);
        (await db.Accounts.SingleAsync(Ct)).Name = "Original renamed";
        await db.SaveChangesAsync(Ct);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.NotNull(await Record.ExceptionAsync(() => CompleteHomeLedgerReader.ReadAsync(db, Ct)));
        Assert.Equal("Original renamed", (await db.Accounts.SingleAsync(Ct)).Name);
    }

    [Fact]
    public async Task Retired_trigger_prevents_republication_after_an_original_writer()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER HomeLedgerReadBlocks_Entries_INSERT", Ct);
        (await db.Entries.FirstAsync(Ct)).Amount = 765;
        await db.SaveChangesAsync(Ct);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Contains(await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct), entry => entry.Amount == 765);
    }

    [Fact]
    public void Scoped_pool_cleanup_removes_the_finished_empty_fixture_without_replaying_financial_writes()
    {
        var empty = new TemporaryDirectory(); var path = empty.Path;
        using (var services = new ServiceCollection().AddZananceData(empty.Combine("empty.db")).BuildServiceProvider())
            services.MigrateLocalDatabase<ZananceDbContext>();
        SqliteTestPools.Clear(empty); empty.Dispose(); Assert.False(Directory.Exists(path));
    }

    [Theory]
    [InlineData("UPDATE HomeLedgerReadBlocks SET Payload='wrong storage type';")]
    [InlineData("UPDATE HomeLedgerReadBlocks SET Digest='12345678901234567890123456789012';")]
    [InlineData("UPDATE HomeLedgerReadState SET SchemaVersion='unsupported';")]
    [InlineData("UPDATE HomeLedgerReadState SET CodecVersion=x'01';")]
    public async Task Incorrect_optional_storage_types_use_the_original_complete_source(string damage)
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync(damage, Ct);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Equal(2, (await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)).Count);
    }

    [Fact]
    public async Task Actual_vacuum_retires_the_old_schema_bound_transport()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("VACUUM", Ct);
        await using (var read = await db.Database.BeginTransactionAsync(Ct)) Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        await using (var writer = await db.Database.BeginTransactionAsync(Ct)) await writer.CommitAsync(Ct);
        await using var final = await db.Database.BeginTransactionAsync(Ct);
        var rows = await HomeLedgerReadBlocks.TryReadAsync(db, Ct); Assert.NotNull(rows);
        Assert.Equal(JsonSerializer.Serialize(await CompleteHomeLedgerReader.ReadAsync(db, Ct)), JsonSerializer.Serialize(rows));
    }

    [Fact]
    public async Task Missing_dirty_keys_force_complete_rebuild_before_republication()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE Entries SET Amount=654 WHERE rowid=1; DELETE FROM HomeLedgerDirtyBlocks;", Ct);
        await using (var read = await db.Database.BeginTransactionAsync(Ct)) Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        await using (var writer = await db.Database.BeginTransactionAsync(Ct)) await writer.CommitAsync(Ct);
        await using var final = await db.Database.BeginTransactionAsync(Ct);
        var rows = await HomeLedgerReadBlocks.TryReadAsync(db, Ct); Assert.NotNull(rows);
        Assert.Contains(rows, entry => entry.Amount == 654);
    }

    [Fact]
    public async Task An_unreviewed_derived_table_trigger_is_never_invoked_by_maintenance()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER UnsafeDerivedEffect AFTER INSERT ON HomeLedgerReadBlocks BEGIN UPDATE Entries SET Amount=999; END;", Ct);
        (await db.Entries.FirstAsync(Ct)).Amount = 321;
        await db.SaveChangesAsync(Ct);
        Assert.DoesNotContain(await db.Entries.ToListAsync(Ct), entry => entry.Amount == 999);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
    }

    [Fact]
    public async Task Corrupt_empty_block_metadata_cannot_hide_existing_financial_rows()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM HomeLedgerReadBlocks; UPDATE HomeLedgerReadState SET RowCount=0;", Ct);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Equal(2, (await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)).Count);
    }

    [Fact]
    public async Task Original_commit_publishes_complete_independent_rows_and_getters_do_not_repair_storage()
    {
        await using var db = await SeedAsync();
        Assert.True(db.CanUseHomeLedgerBlocks);
        await using var transaction = await db.Database.BeginTransactionAsync(Ct);
        var scalar = await CompleteHomeLedgerReader.ReadAsync(db, Ct);
        var first = await HomeLedgerReadBlocks.TryReadAsync(db, Ct); Assert.NotNull(first);
        var second = await HomeLedgerReadBlocks.TryReadAsync(db, Ct); Assert.NotNull(second);
        Assert.Equal(JsonSerializer.Serialize(scalar), JsonSerializer.Serialize(first));
        Assert.Equal(2, first.Count); Assert.NotSame(first[0], second[0]); Assert.NotSame(first[0].Tags, second[0].Tags);
        var before = await StateAsync(db);
        _ = await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct);
        Assert.Equal(before, await StateAsync(db));
    }

    [Fact]
    public async Task Borrowed_writer_rollback_restores_source_and_previously_published_blocks_together()
    {
        await using var db = await SeedAsync();
        var before = await StateAsync(db);
        await using (var transaction = await db.Database.BeginTransactionAsync(Ct))
        {
            (await db.Entries.FirstAsync(Ct)).Amount = 987;
            await db.SaveChangesAsync(Ct);
            Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
            await HomeLedgerReadBlocks.MaintainAsync(db, Ct);
            Assert.NotNull(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
            await transaction.RollbackAsync(Ct);
        }
        Assert.Equal(before, await StateAsync(db)); db.ChangeTracker.Clear();
        Assert.DoesNotContain(await db.Entries.ToListAsync(Ct), entry => entry.Amount == 987);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        Assert.NotNull(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
    }

    [Fact]
    public async Task Raw_native_commit_invalidates_blocks_until_an_original_transaction_republishes()
    {
        await using var db = await SeedAsync();
        await db.Database.OpenConnectionAsync(Ct);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        await using (var raw = connection.BeginTransaction())
        {
            using var command = connection.CreateCommand(); command.Transaction = raw;
            command.CommandText = "UPDATE Entries SET Amount=42 WHERE rowid=1"; await command.ExecuteNonQueryAsync(Ct);
            await raw.CommitAsync(Ct);
        }
        await using (var read = await db.Database.BeginTransactionAsync(Ct))
        {
            Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
            Assert.Contains(await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct), entry => entry.Amount == 42);
        }
        await using (var writer = await db.Database.BeginTransactionAsync(Ct)) await writer.CommitAsync(Ct);
        await using var final = await db.Database.BeginTransactionAsync(Ct);
        var rows = await HomeLedgerReadBlocks.TryReadAsync(db, Ct); Assert.NotNull(rows);
        Assert.Contains(rows, entry => entry.Amount == 42);
    }

    [Theory]
    [InlineData("UPDATE HomeLedgerReadBlocks SET Payload=x'0102';")]
    [InlineData("DELETE FROM HomeLedgerReadBlocks;")]
    [InlineData("UPDATE HomeLedgerReadState SET CodecVersion=999;")]
    [InlineData("UPDATE HomeLedgerReadState SET PublishedRevision=NULL;")]
    public async Task Missing_stale_or_corrupt_transport_falls_back_to_complete_source_without_writing(string damage)
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync(damage, Ct);
        await using var read = await db.Database.BeginTransactionAsync(Ct);
        var expected = (await CompleteHomeLedgerReader.ReadAsync(db, Ct)).OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.CreatedAt).ToList();
        var before = await StateAsync(db);
        Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(await ZananceStore.ReadCompleteHomeLedgerAsync(db, Ct)));
        Assert.Equal(before, await StateAsync(db));
    }

    [Fact]
    public async Task New_schema_cookie_declines_old_physical_blocks_and_original_commit_rebuilds_them()
    {
        await using var db = await SeedAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE IndependentMetadata(Id INTEGER PRIMARY KEY)", Ct);
        await using (var read = await db.Database.BeginTransactionAsync(Ct)) Assert.Null(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
        await using (var writer = await db.Database.BeginTransactionAsync(Ct)) await writer.CommitAsync(Ct);
        await using var final = await db.Database.BeginTransactionAsync(Ct);
        Assert.NotNull(await HomeLedgerReadBlocks.TryReadAsync(db, Ct));
    }

    private async Task<ZananceDbContext> SeedAsync()
    {
        var services = new ServiceCollection().AddZananceData(directory.Combine("original.db"));
        provider = services.BuildServiceProvider();
        provider.MigrateLocalDatabase<ZananceDbContext>();
        var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
        db.Accounts.Add(account);
        db.Entries.AddRange(new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 2), Amount = 101, Kind = EntryKind.Income, Tags = ["original", "متن"] },
            new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 3), Amount = 12, Kind = EntryKind.Expense, Note = "independent", Title = "retained" });
        await db.SaveChangesAsync(Ct); return db;
    }

    private static async Task<string> StateAsync(ZananceDbContext db)
    {
        await db.Database.OpenConnectionAsync(Ct);
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT Revision || ':' || coalesce(PublishedRevision,'unpublished') || ':' || (SELECT count(*) FROM HomeLedgerDirtyBlocks) || ':' || (SELECT count(*) FROM HomeLedgerReadBlocks) FROM HomeLedgerReadState";
        return (string)(await command.ExecuteScalarAsync(Ct))!;
    }

    public void Dispose() { provider?.Dispose(); SqliteTestPools.Clear(directory); directory.Dispose(); }

    private sealed class OriginalLedgerObserver : DbCommandInterceptor
    {
        internal int OriginalReads;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Entries\" NOT INDEXED", StringComparison.Ordinal)) OriginalReads++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class BlockWriteLogger : ILoggerProvider, ILogger
    {
        internal Action? AfterWrite;
        internal int ObservedWrites;
        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (AfterWrite is { } action && eventId.Id == RelationalEventId.CommandExecuted.Id
                && formatter(state, exception).Contains("INSERT INTO HomeLedgerReadBlocks(", StringComparison.Ordinal))
            { AfterWrite = null; ObservedWrites++; action(); }
        }
        public void Dispose() { }
    }
}
