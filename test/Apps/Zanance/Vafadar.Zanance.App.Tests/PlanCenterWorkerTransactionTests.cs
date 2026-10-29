using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>The original review aggregate and all plan-center facts use one real worker/WAL snapshot.</summary>
public sealed class PlanCenterWorkerTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task Original_center_review_count_stays_with_original_plan_rows_after_a_real_concurrent_confirmation()
    {
        using var fixture = new FlowFixture(); await PlanCenterOriginalBoundaryTests.SeedAsync(fixture, "Original center", 1);
        var actual = AccountDetailOriginalBoundaryTests.Factory(fixture);
        await using (var db = await actual.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var observed = new ActualRead(actual);
        var factory = new ActualFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + fixture.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(observed).Options, fixture.Time);
        var plans = new PlanStore(factory, fixture.Store, fixture.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var issued = new TaskCompletionSource<Task<PlanCenterReadSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously); var caller = 0;
        var thread = new Thread(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            try { issued.SetResult(plans.ReadCenterSnapshotAsync(PlanDetailOriginalBoundaryTests.Today, ct: Ct)); }
            catch (Exception exception) { issued.SetException(exception); }
        });
        thread.Start(); var source = await (await issued.Task.WaitAsync(Ct));
        Assert.True(observed.Committed); Assert.True(observed.OneTransaction); Assert.True(observed.AggregateSeen);
        Assert.DoesNotContain(caller, observed.Threads);
        Assert.Equal("Original center", Assert.Single(source.Work.Schedules).Name); Assert.Equal(1, source.UnreviewedCount);
        Assert.Equal(0, await fixture.Store.CountUnreviewedAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => plans.ValidateCenterSnapshotAsync(source, Ct));
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
        internal bool Committed, AggregateSeen, OneTransaction = true;
        internal readonly HashSet<int> Threads = [];
        private void Inspect(DbCommand command)
        {
            Threads.Add(Environment.CurrentManagedThreadId); _transaction ??= command.Transaction; _connection ??= command.Connection;
            OneTransaction &= _transaction is not null && ReferenceEquals(_transaction, command.Transaction) && ReferenceEquals(_connection, command.Connection);
            AggregateSeen |= command.CommandText.Contains("COUNT(", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("\"Entries\"", StringComparison.Ordinal);
            if (Committed || !command.CommandText.Contains("FROM \"Accounts\"", StringComparison.Ordinal)) return;
            // The original reader already owns its SQLite snapshot. A distinct WAL writer now confirms the row.
            Committed = true; using var db = writer.CreateDbContext(); using var transaction = db.Database.BeginTransaction();
            db.Database.ExecuteSqlRaw("UPDATE Entries SET Review=0");
            db.Database.ExecuteSqlRaw("UPDATE Schedules SET Name='Changed original plan'"); transaction.Commit();
        }
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        { Inspect(command); return result; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        { Inspect(command); return ValueTask.FromResult(result); }
    }
}
