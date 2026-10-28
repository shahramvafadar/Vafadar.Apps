using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual account/goal/earmark reads share one worker transaction while a second SQLite connection commits.</summary>
public sealed class AccountDetailTransactionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task One_worker_transaction_keeps_the_original_account_and_goal_allocation_coherent()
    {
        using var f = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f);
        var actual = AccountDetailOriginalBoundaryTests.Factory(f);
        await using (var db = await actual.CreateDbContextAsync(Ct))
        {
            var goal = new Goal { Name = "Original transaction goal", CurrencyCode = "EUR", TargetAmount = 5000 };
            db.Goals.Add(goal); db.GoalAllocations.Add(new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Amount = 1000, Date = new(2026, 10, 2) });
            await db.SaveChangesAsync(Ct); await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        }
        var commands = new ConcurrentSource(actual);
        var factory = new InterceptedFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(commands).Options, f.Time);
        var reader = new LoanWorkStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Time);
        var issued = new TaskCompletionSource<Task<AccountDetailSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously); var caller = 0;
        var thread = new Thread(() => { caller = Environment.CurrentManagedThreadId; try { issued.SetResult(reader.ReadAccountDetailAsync(Ct)); } catch (Exception ex) { issued.SetException(ex); } });
        thread.Start(); var snapshot = await (await issued.Task.WaitAsync(Ct)); thread.Join();
        Assert.True(commands.Committed); Assert.True(commands.OneConnectionAndTransaction); Assert.True(commands.Queries >= 6); Assert.DoesNotContain(caller, commands.WorkerThreads);
        Assert.Equal("Original detail", Assert.Single(snapshot.Inputs.Accounts).Name); Assert.Equal(1000, Assert.Single(snapshot.Inputs.Allocations).Amount);
        Assert.Null(snapshot.Inputs.Settings); Assert.Empty(snapshot.Inputs.Entries);
        await Assert.ThrowsAsync<AccountDetailSupersededException>(() => reader.ValidateAccountDetailAsync(snapshot, Ct));
        Assert.Equal("Committed new account", Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct)).Name);
        Assert.Equal(2000, Assert.Single(await f.Services.GetRequiredService<GoalStore>().GetAllocationsAsync(cancellationToken: Ct)).Amount);
    }

    /// <summary>Uses the original fictitious SQLite file with only command observation added.</summary>
    private sealed class InterceptedFactory(DbContextOptions<ZananceDbContext> options, TimeProvider time) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => new(options, time);
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(CreateDbContext()); }
    }

    /// <summary>Commits a real metadata/earmark pair while the original reader keeps its actual SQLite snapshot.</summary>
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
            db.Database.ExecuteSqlRaw("UPDATE Accounts SET Name='Committed new account'"); db.Database.ExecuteSqlRaw("UPDATE GoalAllocations SET Amount=2000"); transaction.Commit();
        }
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Inspect(command); return result; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Inspect(command); return ValueTask.FromResult(result); }
    }
}
