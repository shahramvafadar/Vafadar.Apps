using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.Performance;

/// <summary>Measures the original complete ledger projection and original core facts without application startup or writes.</summary>
internal static class LedgerStageDiagnostics
{
    /// <summary>Uses the exact production projection, materializer/sort and financial methods on an existing factory.</summary>
    [DynamicDependency("ReadCompleteHomeLedgerAsync", typeof(ZananceStore))]
    internal static async Task<object> MeasureAsync(IDbContextFactory<ZananceDbContext> factory, int expectedRows,
        DateOnly today, CancellationToken ct)
    {
        var method = typeof(ZananceStore).GetMethod("ReadCompleteHomeLedgerAsync", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new IOException("Original complete Home ledger operation is missing.");
        var sql = OriginalProjection();
        var observations = new List<object>();
        for (var sample = 0; sample < 2; sample++)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var clock = Stopwatch.StartNew();
            await db.Database.OpenConnectionAsync(ct);
            var keyOpenMs = clock.Elapsed.TotalMilliseconds;
            // Preserve the original unpooled KDF and operation-local cache. Query-only applies only
            // to this owned financial connection; no persisted pragma or factory policy changes.
            await db.Database.ExecuteSqlRawAsync("PRAGMA query_only = ON", ct);
            await db.Database.ExecuteSqlRawAsync("PRAGMA cache_size = -32768", ct);
            var connection = (SqliteConnection)db.Database.GetDbConnection();
            await using var transaction = connection.BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            var rows = 0;
            clock.Restart();
            await using (var reader = await command.ExecuteReaderAsync(ct))
                while (await reader.ReadAsync(ct)) rows++;
            var nativeProjectionMs = clock.Elapsed.TotalMilliseconds;
            if (rows != expectedRows) throw new IOException("Original native projection row count differs.");
            clock.Restart();
            var entries = await ((Task<List<LedgerEntry>>)(method.Invoke(null, [db, ct])
                ?? throw new IOException("Original complete reader did not return its actual task.")));
            var completeReadSortMs = clock.Elapsed.TotalMilliseconds;
            if (entries.Count != expectedRows) throw new IOException("Complete original typed ledger row count differs.");
            var accounts = await db.Accounts.AsNoTracking().ToListAsync(ct);
            clock.Restart();
            var index = new AccountEntryIndex(entries);
            var indexMs = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            var balances = LedgerCalculator.TotalBalancesFromIndex(accounts, index, today);
            var totalsMs = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            var accountBalances = accounts.Where(account => !account.IsArchived).ToDictionary(account => account.Id,
                account => LedgerCalculator.Balance(account, index.For(account.Id), today));
            var accountsMs = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            var unreviewed = LedgerCalculator.UnreviewedEntryCount(accounts, entries);
            var unreviewedMs = clock.Elapsed.TotalMilliseconds;
            // Keep both the actual EF all-field control and the original unindexed financial oracle
            // outside every timer. No copied entity materializer or financial formula is an oracle.
            var control = await db.Entries.AsNoTracking().OrderByDescending(entry => entry.Date)
                .ThenByDescending(entry => entry.CreatedAt).ToListAsync(ct);
            var properties = db.Model.FindEntityType(typeof(LedgerEntry))!.GetProperties().ToArray();
            if (properties.Length != 32 || control.Count != entries.Count)
                throw new IOException("Complete original ledger contract or control count differs.");
            for (var row = 0; row < entries.Count; row++)
            foreach (var property in properties)
            {
                var getter = property.GetGetter();
                if (!property.GetValueComparer().Equals(getter.GetClrValue(control[row]), getter.GetClrValue(entries[row])))
                    throw new IOException("An original persisted typed ledger property differs from EF.");
            }
            foreach (var account in accounts.Where(account => !account.IsArchived))
                if (accountBalances[account.Id] != LedgerCalculator.Balance(account, control, today))
                    throw new IOException("Original account balance differs from its complete-history oracle.");
            if (!balances.OrderBy(item => item.Key, StringComparer.Ordinal).SequenceEqual(
                LedgerCalculator.TotalBalances(accounts, control, today).OrderBy(item => item.Key, StringComparer.Ordinal)))
                throw new IOException("Original currency balances differ from their complete-history control.");
            observations.Add(new
            {
                Sample = sample,
                Entries = entries.Count,
                Accounts = accounts.Count,
                ExistingKeyedOpenMilliseconds = keyOpenMs,
                NativeExactProjectionSteppingMilliseconds = nativeProjectionMs,
                CompleteProductionMaterializationAndSortMilliseconds = completeReadSortMs,
                AccountIndexMilliseconds = indexMs,
                TotalBalancesMilliseconds = totalsMs,
                AllActiveAccountBalancesMilliseconds = accountsMs,
                UnreviewedEntryCountMilliseconds = unreviewedMs,
                UnreviewedCount = unreviewed,
                All32PersistedPropertiesAndOrderMatchEf = true,
                OriginalCompleteHistoryBalanceOracleMatched = true,
            });
        }
        return new { Observations = observations, ActualDataModule = typeof(ZananceStore).Module.ModuleVersionId,
            ActualCoreModule = typeof(LedgerCalculator).Module.ModuleVersionId, CapturedDay = today,
            NativeProjectionRunsBeforeTypedReadOnSameConnection = true,
            ControlAndPreservationOutsideTimers = true, FinancialCommandsDispatched = 0,
            SourceMigrationPerformed = false, UiProjectionOrColdHomeMeasured = false };
    }

    /// <summary>Reads the actual source projection constant; never reconstructs or reduces its financial field list.</summary>
    [DynamicDependency("Sql", "Vafadar.Zanance.Data.CompleteHomeLedgerReader", "Vafadar.Zanance.Data")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The exact named production projection field is explicitly rooted by DynamicDependency.")]
    private static string OriginalProjection()
    {
        var type = typeof(ZananceStore).Assembly.GetType("Vafadar.Zanance.Data.CompleteHomeLedgerReader", throwOnError: true)!;
        return type.GetField("Sql", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string
            ?? throw new IOException("Original complete ledger SQL projection is missing.");
    }
}
