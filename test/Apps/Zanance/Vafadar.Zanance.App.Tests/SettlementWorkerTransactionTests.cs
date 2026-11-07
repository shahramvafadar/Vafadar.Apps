using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>The original bill account, plan and complete ledger share one worker/connection/deferred transaction.</summary>
public sealed class SettlementWorkerTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task A_concurrent_currency_and_history_commit_cannot_split_the_original_bill_review()
    {
        using var f = new FlowFixture(); var id = await SettlementOwnershipSnapshotTests.SeedAsync(f);
        var actual = OccurrenceOriginFailureTests.Factory(f);
        await using (var db = await actual.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var observation = new ActualRead(actual);
        var factory = new ActualFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(observation).Options, f.Time);
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var issued = new TaskCompletionSource<Task<AdvanceSettlementDraft>>(TaskCreationOptions.RunContinuationsAsynchronously); var caller = 0;
        var thread = new Thread(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            try { issued.SetResult(store.ReadAdvanceSettlementDraftAsync(id, Ct)); }
            catch (Exception exception) { issued.SetException(exception); }
        });
        thread.Start(); var source = await (await issued.Task.WaitAsync(Ct));
        Assert.True(observation.Committed, "The independent WAL metadata writer must commit during the original read.");
        Assert.True(observation.OneTransaction, "Every observed query must share the original transaction and connection.");
        Assert.Equal(3, observation.Queries); // Original plan, original account and complete history only.
        Assert.DoesNotContain(caller, observation.Threads);
        Assert.Equal("EUR", source.Account.CurrencyCode);
        Assert.Equal("Original note", Assert.Single(source.Entries).Note);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ValidateAdvanceSettlementDraftAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    private sealed class ActualFactory(DbContextOptions<ZananceDbContext> options, TimeProvider time) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => new(options, time);
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(CreateDbContext()); }
    }
    private sealed class ActualRead(IDbContextFactory<ZananceDbContext> writer) : DbCommandInterceptor
    {
        private DbTransaction? _transaction;
        private DbConnection? _connection;
        internal bool Committed, OneTransaction = true;
        internal int Queries;
        internal readonly HashSet<int> Threads = [];
        private void Inspect(DbCommand command)
        {
            Queries++; Threads.Add(Environment.CurrentManagedThreadId);
            _transaction ??= command.Transaction; _connection ??= command.Connection;
            OneTransaction &= _transaction is not null && ReferenceEquals(_transaction, command.Transaction) && ReferenceEquals(_connection, command.Connection);
            if (Committed || !command.CommandText.Contains("FROM \"Accounts\"", StringComparison.Ordinal)) return;
            // ExecuteReader has acquired the original SQLite snapshot before this distinct writer commits.
            Committed = true; using var db = writer.CreateDbContext(); using var transaction = db.Database.BeginTransaction();
            db.Database.ExecuteSqlRaw("UPDATE Accounts SET CurrencyCode='USD'");
            db.Database.ExecuteSqlRaw("UPDATE Entries SET Note='Committed candidate note'"); transaction.Commit();
        }
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        { Inspect(command); return result; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        { Inspect(command); return ValueTask.FromResult(result); }
    }
}
