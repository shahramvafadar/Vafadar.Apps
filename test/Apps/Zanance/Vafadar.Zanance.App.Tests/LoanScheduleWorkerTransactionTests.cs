using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual complete original loan read stays on a worker transaction while another connection commits.</summary>
public sealed class LoanScheduleWorkerTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Original_loan_sources_share_one_worker_snapshot_before_fresh_validation_refuses_a_later_commit()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        var actual = AccountDetailOriginalBoundaryTests.Factory(f);
        await using (var db = await actual.CreateDbContextAsync(Ct))
        {
            await db.SaveChangesAsync(Ct); await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        }
        var commands = new ConcurrentSource(actual);
        var factory = new InterceptedFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(commands).Options, f.Time);
        var reader = new LoanWorkStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Time);
        var issued = new TaskCompletionSource<Task<LoanReadReview>>(TaskCreationOptions.RunContinuationsAsynchronously); var caller = 0;
        var thread = new Thread(() => { caller = Environment.CurrentManagedThreadId; try { issued.SetResult(reader.ReadAsync(Ct)); } catch (Exception ex) { issued.SetException(ex); } });
        thread.Start(); var snapshot = await (await issued.Task.WaitAsync(Ct)); thread.Join();
        Assert.True(commands.Committed); Assert.True(commands.OneConnectionAndTransaction); Assert.True(commands.Queries >= 4); Assert.DoesNotContain(caller, commands.WorkerThreads);
        Assert.Equal("Original detail", Assert.Single(snapshot.Accounts).Name);
        Assert.NotNull(snapshot.Settings); Assert.Empty(snapshot.Entries);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ValidateAsync(snapshot, Ct));
        Assert.Equal("Committed new account", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name);

    }

    /// <summary>Uses the original fictitious SQLite file with only command observation added.</summary>
    private sealed class InterceptedFactory(DbContextOptions<ZananceDbContext> options, TimeProvider time) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => new(options, time);
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(CreateDbContext()); }
    }

    /// <summary>Commits only an original fictitious account name while the reader keeps its actual SQLite snapshot.</summary>
    private sealed class ConcurrentSource(IDbContextFactory<ZananceDbContext> writer) : DbCommandInterceptor
    {
        private DbConnection? _connection;
        private DbTransaction? _transaction;
        internal bool Committed;
        internal bool OneConnectionAndTransaction = true;
        internal int Queries;
        internal readonly HashSet<int> WorkerThreads = [];
        private void Inspect(DbCommand command)
        {
            Queries++; WorkerThreads.Add(Environment.CurrentManagedThreadId); _connection ??= command.Connection; _transaction ??= command.Transaction;
            OneConnectionAndTransaction &= ReferenceEquals(_connection, command.Connection) && _transaction is not null && ReferenceEquals(_transaction, command.Transaction);
            if (Committed || !command.CommandText.Contains("FROM \"Accounts\"", StringComparison.Ordinal)) return;
            Committed = true; using var db = writer.CreateDbContext(); using var transaction = db.Database.BeginTransaction();
            db.Database.ExecuteSqlRaw("UPDATE Accounts SET Name='Committed new account'"); transaction.Commit();
        }
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Inspect(command); return result; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Inspect(command); return ValueTask.FromResult(result); }
    }
}
