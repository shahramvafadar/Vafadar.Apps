using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>The original occurrence source and candidate ledger share one worker/connection/deferred transaction.</summary>
public sealed class OccurrenceWorkerTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task A_concurrent_currency_and_candidate_note_commit_cannot_split_the_original_occurrence_review()
    {
        using var f = new FlowFixture(); var seed = await OccurrenceOwnershipSnapshotTests.CandidateAsync(f);
        var actual = OccurrenceOriginFailureTests.Factory(f);
        await using (var db = await actual.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var observation = new ActualRead(actual);
        var factory = new ActualFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(observation).Options, f.Time);
        var plans = new PlanStore(factory, f.Store, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var issued = new TaskCompletionSource<Task<OccurrenceReadSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously); var caller = 0;
        var thread = new Thread(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            try { issued.SetResult(plans.ReadOccurrenceSnapshotAsync(seed.Plan, OccurrenceOriginFailureTests.Day, OccurrenceOriginFailureTests.Day, ct: Ct)); }
            catch (Exception exception) { issued.SetException(exception); }
        });
        thread.Start(); var source = await (await issued.Task.WaitAsync(Ct));
        Assert.True(observation.Committed, "The independent WAL metadata writer must commit during the original read.");
        Assert.True(observation.OneTransaction, "Every observed query must share the original transaction and connection.");
        // The inactive private work policy needs no extra query: six source reads
        // plus the candidate read are the seven required original queries.
        Assert.True(observation.Queries >= 7, $"Observed only {observation.Queries} original queries.");
        Assert.DoesNotContain(caller, observation.Threads);
        Assert.Equal("EUR", Assert.Single(source.Source.Inputs.Accounts).CurrencyCode);
        Assert.Equal("Original note", Assert.Single(source.Candidates).Note);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<PlanEditorSourceChangedException>(() => plans.ValidateOccurrenceSnapshotAsync(source, Ct));
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
