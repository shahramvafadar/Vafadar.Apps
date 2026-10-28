using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual concurrent SQLite writes cannot split the captured account and ledger source or run its SQL on the caller.</summary>
public sealed class AccountOverviewTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task One_deferred_worker_transaction_preserves_the_original_account_and_ledger_pair()
    {
        using var f = new FlowFixture(); await AccountOverviewFlowTests.SeedAsync(f);
        var actual = AccountOverviewFlowTests.Factory(f);
        await using (var db = await actual.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var commands = new ConcurrentRead(actual);
        var factory = new InterceptedFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(commands).Options, f.Time);
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var issued = new TaskCompletionSource<Task<AccountOverviewSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var caller = 0;
        var thread = new Thread(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            try { issued.SetResult(store.ReadAccountOverviewAsync(Ct)); }
            catch (Exception ex) { issued.SetException(ex); }
        });
        thread.Start();
        var snapshot = await (await issued.Task.WaitAsync(Ct));
        Assert.True(commands.Committed); Assert.True(commands.OneConnectionAndTransaction); Assert.True(commands.Queries >= 4);
        Assert.DoesNotContain(caller, commands.WorkerThreads);
        Assert.Equal("Original checking", Assert.Single(snapshot.Inputs.Accounts).Name);
        Assert.Equal(1000, Assert.Single(snapshot.Inputs.Entries).Amount); Assert.Null(snapshot.Inputs.Settings);
        await Assert.ThrowsAsync<AccountOverviewSupersededException>(() => store.ValidateAccountOverviewAsync(snapshot, Ct));
        Assert.Equal("Concurrent original account", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name);
        Assert.Equal(2000, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
    }

    /// <summary>Opens the real plain fictitious database with only an actual command interceptor added.</summary>
    private sealed class InterceptedFactory(DbContextOptions<ZananceDbContext> options, TimeProvider time) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => new(options, time);
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(CreateDbContext()); }
    }

    /// <summary>Commits a distinct account/ledger pair through real original tables while the reader holds its snapshot.</summary>
    private sealed class ConcurrentRead(IDbContextFactory<ZananceDbContext> writer) : DbCommandInterceptor
    {
        private DbConnection? _connection;
        private DbTransaction? _transaction;
        internal bool Committed;
        internal bool OneConnectionAndTransaction = true;
        internal int Queries;
        internal readonly HashSet<int> WorkerThreads = [];

        private void Inspect(DbCommand command)
        {
            Queries++; WorkerThreads.Add(Environment.CurrentManagedThreadId);
            _connection ??= command.Connection; _transaction ??= command.Transaction;
            OneConnectionAndTransaction &= ReferenceEquals(_connection, command.Connection)
                && _transaction is not null && ReferenceEquals(_transaction, command.Transaction);
            if (!Committed && command.CommandText.Contains("FROM \"Accounts\"", StringComparison.Ordinal))
            {
                Committed = true;
                using var db = writer.CreateDbContext(); using var transaction = db.Database.BeginTransaction();
                db.Database.ExecuteSqlRaw("UPDATE Accounts SET Name='Concurrent original account'");
                db.Database.ExecuteSqlRaw("UPDATE Entries SET Amount=2000");
                transaction.Commit();
            }
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Inspect(command); return result; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Inspect(command); return ValueTask.FromResult(result); }
    }
}
