using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original detail/reference/work reads share one worker transaction despite a real concurrent WAL writer.</summary>
public sealed class PlanDetailWorkerTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task A_committed_currency_plan_pair_cannot_split_the_original_detail_and_work_snapshot()
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f);
        var actual = AccountDetailOriginalBoundaryTests.Factory(f);
        await using (var db = await actual.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var observation = new ActualRead(actual);
        var factory = new ActualFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(observation).Options, f.Time);
        var plans = new PlanStore(factory, f.Store, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var issued = new TaskCompletionSource<Task<PlanDetailSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously); var caller = 0;
        var thread = new Thread(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            try { issued.SetResult(plans.ReadDetailSnapshotAsync(ids.Plan, PlanDetailOriginalBoundaryTests.Today, ct: Ct)); }
            catch (Exception exception) { issued.SetException(exception); }
        });
        thread.Start(); var snapshot = await (await issued.Task.WaitAsync(Ct));
        Assert.True(observation.Committed); Assert.True(observation.OneTransaction); Assert.True(observation.Queries >= 7);
        Assert.DoesNotContain(caller, observation.Threads);
        Assert.Equal("EUR", Assert.Single(snapshot.Source.Inputs.Accounts).CurrencyCode);
        Assert.Equal(1000, snapshot.Source.Inputs.Schedule!.Amount); Assert.Equal(1000, Assert.Single(snapshot.Work.Schedules).Amount);
        await Assert.ThrowsAsync<PlanEditorSourceChangedException>(() => plans.ValidateDetailSnapshotAsync(snapshot, Ct));
        Assert.Equal("USD", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).CurrencyCode);
        Assert.Equal(2500, Assert.Single(await PlanDetailOriginalBoundaryTests.Plans(f).GetSchedulesAsync(Ct)).Amount);
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
            db.Database.ExecuteSqlRaw("UPDATE Schedules SET Amount=2500"); transaction.Commit();
        }
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        { Inspect(command); return result; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        { Inspect(command); return ValueTask.FromResult(result); }
    }
}
