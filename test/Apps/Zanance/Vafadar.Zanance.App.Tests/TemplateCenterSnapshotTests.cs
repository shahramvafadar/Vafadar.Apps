using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original template-center worker snapshot, original-file Retry and complete packet authority.</summary>
public sealed class TemplateCenterSnapshotTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task One_original_worker_transaction_keeps_the_definition_before_a_real_WAL_writer_changes_it()
    {
        using var fixture = new FlowFixture(); await TemplateCenterOriginalActionTests.SeedAsync(fixture);
        var original = AccountDetailOriginalBoundaryTests.Factory(fixture);
        await using (var db = await original.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var observed = new ActualRead(original);
        var factory = new ActualFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + fixture.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(observed).Options, fixture.Time);
        var store = new ZananceStore(factory, fixture.Time, fixture.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var issued = new TaskCompletionSource<Task<QuickResourceReview>>(TaskCreationOptions.RunContinuationsAsynchronously); var caller = 0;
        new Thread(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            try { issued.SetResult(store.ReadTemplateCenterAsync(cancellationToken: Ct)); }
            catch (Exception exception) { issued.SetException(exception); }
        }).Start();
        var review = await (await issued.Task.WaitAsync(Ct));
        Assert.True(observed.Committed); Assert.True(observed.OneTransaction); Assert.DoesNotContain(caller, observed.Threads);
        Assert.Equal("Original definition", Assert.Single(review.Templates).Name);
        Assert.Equal("Changed by original WAL writer", Assert.Single(await fixture.Store.GetTemplatesAsync(Ct)).Name);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ValidateTemplateCenterAsync(review, Ct));
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task An_initial_original_read_failure_does_not_allow_Retry_to_adopt_another_file()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        await TemplateCenterOriginalActionTests.SeedAsync(first);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(first).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("DROP TABLE Templates", Ct);
        var switched = false; var controlled = new AccountDetailOriginalBoundaryTests.ControlledFactory(AccountDetailOriginalBoundaryTests.Factory(first))
        { Select = () => switched ? AccountDetailOriginalBoundaryTests.Factory(second) : AccountDetailOriginalBoundaryTests.Factory(first) };
        var store = new ZananceStore(controlled, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        string? originalFile = null;
        bool Accept(string path) { originalFile ??= path; return originalFile == path; }
        await Assert.ThrowsAsync<SqliteException>(() => store.ReadTemplateCenterAsync(Accept, Ct));
        Assert.NotNull(originalFile); switched = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReadTemplateCenterAsync(Accept, Ct));
        Assert.Empty(await second.Store.GetTemplatesAsync(Ct)); Assert.Empty(await second.Store.GetAccountsAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData("template")]
    [InlineData("account")]
    [InlineData("detached")]
    public async Task Complete_original_template_center_validation_refuses_changed_facts_without_replacing_the_packet(string change)
    {
        using var fixture = new FlowFixture(); await TemplateCenterOriginalActionTests.SeedAsync(fixture);
        var review = await fixture.Store.ReadTemplateCenterAsync(cancellationToken: Ct);
        if (change == "detached") Assert.Single(review.Accounts).Name = "Changed detached original account";
        else
        {
            await using var db = await AccountDetailOriginalBoundaryTests.Factory(fixture).CreateDbContextAsync(Ct);
            if (change == "template") (await db.Templates.SingleAsync(Ct)).Payee = "Changed original complete payee";
            else (await db.Accounts.SingleAsync(Ct)).CurrencyCode = "USD";
            await db.SaveChangesAsync(Ct);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Store.ValidateTemplateCenterAsync(review, Ct));
        Assert.Equal("Original definition", Assert.Single(review.Templates).Name);
        Assert.Equal(1000, Assert.Single(review.Templates).Amount); Assert.Null(review.SourceEntry);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: Ct));
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
        internal readonly HashSet<int> Threads = [];
        private void Inspect(DbCommand command)
        {
            Threads.Add(Environment.CurrentManagedThreadId); _transaction ??= command.Transaction; _connection ??= command.Connection;
            OneTransaction &= _transaction is not null && ReferenceEquals(_transaction, command.Transaction) && ReferenceEquals(_connection, command.Connection);
            if (Committed || !command.CommandText.Contains("FROM \"Accounts\"", StringComparison.Ordinal)) return;
            // Definitions have already established the original snapshot before the account reader reaches this point.
            Committed = true; using var db = writer.CreateDbContext(); using var transaction = db.Database.BeginTransaction();
            db.Database.ExecuteSqlRaw("UPDATE Templates SET Name='Changed by original WAL writer'"); transaction.Commit();
        }
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        { Inspect(command); return result; }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        { Inspect(command); return ValueTask.FromResult(result); }
    }
}
