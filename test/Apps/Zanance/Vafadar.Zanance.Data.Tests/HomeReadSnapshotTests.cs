using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Actual Home reads preserve one financial snapshot, original rights and drained factory ownership.</summary>
[Trait("AT", "AT-283")]
public sealed class HomeReadSnapshotTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = new(2026, 10, 15);
    private ServiceProvider Create()
    {
        var provider = new ServiceCollection().AddZananceData(_directory.Combine(Guid.NewGuid() + ".db")).BuildServiceProvider();
        _providers.Add(provider); provider.MigrateLocalDatabase<ZananceDbContext>(); return provider;
    }
    private static IDbContextFactory<ZananceDbContext> Factory(ServiceProvider provider) => provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
    private static ZananceStore Store(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource source) => new(factory, TimeProvider.System, source);
    private static async Task SeedAsync(ServiceProvider provider)
    {
        await using var db = await Factory(provider).CreateDbContextAsync(Ct);
        var account = new Account { Name = "Original bank", CurrencyCode = "EUR", OpeningDate = Today.AddMonths(-2), OpeningBalance = -123 };
        db.Accounts.Add(account);
        db.Entries.AddRange(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Income, Amount = 80000, Date = Today.AddMonths(-1) },
            new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 456, Date = Today });
        db.Settings.Add(new ZananceSettings { DefaultAccountId = account.Id });
        db.Budgets.AddRange(new Budget { Year = 2026, Month = 9, CurrencyCode = "EUR", TotalLimit = 10000, Rollover = BudgetRollover.Surplus },
            new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 12000, Rollover = BudgetRollover.Surplus });
        db.Schedules.Add(new Schedule { Name = "Original plan", AccountId = account.Id, Amount = 321,
            Rule = new() { Frequency = Frequency.Monthly, Start = Today } });
        db.Goals.Add(new Goal { Name = "Original goal", CurrencyCode = "EUR", TargetAmount = 4000, AccountId = account.Id });
        await db.SaveChangesAsync(Ct);
    }

    /// <summary>Empty display reads never become settings or selection writes and release both original leases.</summary>
    [Fact]
    public async Task Empty_home_uses_one_read_context_and_fresh_validation_without_creating_defaults()
    {
        var provider = Create(); var counted = new CountedFactory(Factory(provider));
        var store = Store(counted, provider.GetRequiredService<ICommercialWriteAccessSource>());
        var packet = await store.GetHomeSnapshotAsync(Today, 0, Ct);
        Assert.Equal(1, counted.Created); Assert.Empty(packet.Entries); Assert.Empty(packet.Goals.Goals);
        Assert.Empty(packet.Plans.Schedules); Assert.Null(packet.MonthBudget); Assert.Null(packet.QuickResources.Settings);
        await store.ValidateHomeSnapshotAsync(packet, Ct); Assert.Equal(2, counted.Created);
        await using (var db = await Factory(provider).CreateDbContextAsync(Ct))
        { Assert.False(await db.Settings.AnyAsync(Ct)); Assert.False(await db.ResourceSelections.AnyAsync(Ct)); }
        var location = provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct); timeout.CancelAfter(TimeSpan.FromSeconds(5));
        using var owner = await location.Access.AcquireExclusiveAsync(timeout.Token); location.Access.RequireDrainedOwner(owner);
    }

    /// <summary>Original monthly selection, rollover and complete ordered history match their existing public operations.</summary>
    [Theory, InlineData(0), InlineData(1)]
    public async Task Complete_home_preserves_original_sources_month_and_carry(int period)
    {
        var provider = Create(); await SeedAsync(provider); var store = provider.GetRequiredService<ZananceStore>();
        var packet = await store.GetHomeSnapshotAsync(Today, period, Ct);
        var month = period == 0 ? 10 : 9; var budget = await store.GetBudgetAsync(2026, month, PeriodCalendar.Gregorian, "EUR", Ct);
        Assert.Equal(budget!.Id, packet.MonthBudget!.Id);
        Assert.Equal(JsonSerializer.Serialize(await store.GetBudgetCarryAsync(budget, Ct)), JsonSerializer.Serialize(packet.Carry));
        Assert.Equal(JsonSerializer.Serialize(await store.GetEntriesAsync(cancellationToken: Ct)), JsonSerializer.Serialize(packet.Entries));
        Assert.Equal(JsonSerializer.Serialize(await store.GetBudgetsAsync(Ct)), JsonSerializer.Serialize(packet.Budgets));
        Assert.Equal(80000, packet.Entries.Single(e => e.Kind == EntryKind.Income).Amount);
        Assert.Equal(-123, Assert.Single(packet.QuickResources.Accounts).OpeningBalance);
        Assert.Single(packet.Plans.Schedules); Assert.Single(packet.Goals.Goals);
        await store.ValidateHomeSnapshotAsync(packet, Ct);
    }

    /// <summary>Preserves null legacy fallback, explicit reset and custom profile order in the original captured read.</summary>
    [Theory, InlineData(null), InlineData(""), InlineData("Budget,Recent,-Upcoming,-Accounts")]
    [Trait("AT", "AT-285")]
    public async Task Captured_home_layout_preserves_profile_default_and_legacy_distinction_without_a_second_settings_read(string? layout)
    {
        var provider = Create(); await SeedAsync(provider);
        await using (var db = await Factory(provider).CreateDbContextAsync(Ct))
        {
            (await db.Settings.SingleAsync(Ct)).HomeLayout = layout;
            await db.SaveChangesAsync(Ct);
        }
        var counted = new CountedFactory(Factory(provider));
        var store = Store(counted, provider.GetRequiredService<ICommercialWriteAccessSource>());
        var packet = await store.GetHomeSnapshotAsync(Today, 0, Ct);
        Assert.Equal(layout, packet.QuickResources.Settings!.HomeLayout);
        Assert.Equal(1, counted.Created);
        await store.ValidateHomeSnapshotAsync(packet, Ct);
        await using var original = await Factory(provider).CreateDbContextAsync(Ct);
        Assert.Equal(layout, (await original.Settings.SingleAsync(Ct)).HomeLayout);
    }

    /// <summary>Valuation reads the complete original rates only when the visible currency scope actually uses them.</summary>
    [Theory, InlineData(false, true), InlineData(true, true), InlineData(true, false)]
    public async Task Only_active_multi_currency_valuation_queries_rate_history(bool enabled, bool multiCurrency)
    {
        var provider = Create(); await SeedAsync(provider);
        await using (var db = await Factory(provider).CreateDbContextAsync(Ct))
        {
            (await db.Settings.SingleAsync(Ct)).ValuationCurrencyEnabled = enabled;
            if (multiCurrency) db.Accounts.Add(new Account { Name = "Original dollars", CurrencyCode = "USD", OpeningDate = Today, OpeningBalance = 1000 });
            db.ExchangeRates.Add(new ExchangeRate { FromCurrencyCode = "EUR", ToCurrencyCode = "USD", Date = Today, Rate = 1.2m });
            await db.SaveChangesAsync(Ct);
        }
        var path = provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        var recorded = new RecordedReads();
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite($"Data Source={path};Pooling=False")
            .UseModel(ZananceDbContextModel.Instance).AddInterceptors(recorded).Options;
        var store = Store(new DirectFactory(options), provider.GetRequiredService<ICommercialWriteAccessSource>());
        var packet = await store.GetHomeSnapshotAsync(Today, 0, Ct);
        Assert.Equal(enabled && multiCurrency ? 1 : 0, packet.Rates.Count); Assert.Equal(enabled && multiCurrency ? 1 : 0, recorded.RateQueries);
        await store.ValidateHomeSnapshotAsync(packet, Ct);
        await using var original = await Factory(provider).CreateDbContextAsync(Ct);
        Assert.Equal(1.2m, (await original.ExchangeRates.SingleAsync(Ct)).Rate);
    }

    /// <summary>Read-only and ordinary operations retain complete ordering without leaking a connection-local cache target.</summary>
    [Theory, InlineData(false, false), InlineData(true, false), InlineData(false, true)]
    [Trait("AT", "AT-286")]
    public async Task Home_page_cache_is_scoped_to_its_reader_and_preserves_complete_ordered_rows(bool readOnly, bool pooled)
    {
        var provider = Create(); await SeedAsync(provider);
        var originalStore = provider.GetRequiredService<ZananceStore>();
        var expected = JsonSerializer.Serialize(await originalStore.GetEntriesAsync(cancellationToken: Ct));
        var path = provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        var observation = new PageCacheObservation();
        var options = new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite($"Data Source={path};Pooling={pooled};Mode={(readOnly ? "ReadOnly" : "ReadWrite")}")
            .UseModel(ZananceDbContextModel.Instance).AddInterceptors(observation).Options;
        var factory = new DirectFactory(options);
        var store = Store(factory, provider.GetRequiredService<ICommercialWriteAccessSource>());
        var packet = await store.GetHomeSnapshotAsync(Today, 0, Ct);
        Assert.Equal(expected, JsonSerializer.Serialize(packet.Entries));
        var target = Assert.Single(observation.EntryReadTargets);
        if (pooled) Assert.NotEqual(-32768, target); else Assert.Equal(-32768, target);
        await store.ValidateHomeSnapshotAsync(packet, Ct);
        await using var fresh = factory.CreateDbContext();
        await fresh.Database.OpenConnectionAsync(Ct);
        using var command = fresh.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA cache_size";
        Assert.NotEqual(-32768, Convert.ToInt32(await command.ExecuteScalarAsync(Ct), System.Globalization.CultureInfo.InvariantCulture));
        if (pooled) Microsoft.Data.Sqlite.SqliteConnection.ClearPool((Microsoft.Data.Sqlite.SqliteConnection)fresh.Database.GetDbConnection());
    }

    /// <summary>Profile changes cannot revalidate a retained Home against a newly selected file.</summary>
    [Fact]
    public async Task Changed_factory_file_refuses_retained_home()
    {
        var first = Create(); var second = Create(); await SeedAsync(first);
        var counted = new CountedFactory(Factory(first)); var store = Store(counted, first.GetRequiredService<ICommercialWriteAccessSource>());
        var packet = await store.GetHomeSnapshotAsync(Today, 0, Ct); counted.Inner = Factory(second);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ValidateHomeSnapshotAsync(packet, Ct));
        Assert.Empty(await second.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Later rights activation and original plan edits invalidate work before native actions become available.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public async Task Retired_access_or_changed_original_plan_refuses_publication(bool editPlan)
    {
        var provider = Create(); await SeedAsync(provider); var source = new AccessSource(); var store = Store(Factory(provider), source);
        var packet = await store.GetHomeSnapshotAsync(Today, 0, Ct);
        if (editPlan)
        {
            await using var db = await Factory(provider).CreateDbContextAsync(Ct); (await db.Schedules.SingleAsync(Ct)).Amount++;
            await db.SaveChangesAsync(Ct);
        }
        else source.Current = new(provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path,
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ValidateHomeSnapshotAsync(packet, Ct));
    }

    /// <summary>A concurrent committed writer cannot mix old references with newer ledger rows inside Home's reader.</summary>
    [Fact]
    public async Task Concurrent_writer_leaves_one_complete_original_sqlite_snapshot()
    {
        var provider = Create(); await SeedAsync(provider);
        await using (var setup = await Factory(provider).CreateDbContextAsync(Ct))
            await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var path = provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().Path;
        var pause = new PausedRead();
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite($"Data Source={path};Pooling=False")
            .UseModel(ZananceDbContextModel.Instance).AddInterceptors(pause).Options;
        var store = Store(new DirectFactory(options), provider.GetRequiredService<ICommercialWriteAccessSource>());
        var pending = store.GetHomeSnapshotAsync(Today, 0, Ct);
        try
        {
            await pause.Captured.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
            await using var writer = await Factory(provider).CreateDbContextAsync(Ct);
            (await writer.Accounts.SingleAsync(Ct)).OpeningBalance = 999;
            (await writer.Entries.SingleAsync(e => e.Kind == EntryKind.Expense, Ct)).Amount = 888;
            await writer.SaveChangesAsync(Ct);
        }
        finally { pause.Release.TrySetResult(); }
        var packet = await pending;
        Assert.Equal(-123, Assert.Single(packet.QuickResources.Accounts).OpeningBalance);
        Assert.Equal(456, packet.Entries.Single(e => e.Kind == EntryKind.Expense).Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ValidateHomeSnapshotAsync(packet, Ct));
    }

    /// <inheritdoc />
    public void Dispose() { foreach (var provider in _providers) provider.Dispose(); SqliteTestPools.Clear(_directory); _directory.Dispose(); }
    private sealed class CountedFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        public IDbContextFactory<ZananceDbContext> Inner { get; set; } = inner;
        public int Created { get; private set; }
        public ZananceDbContext CreateDbContext() { Created++; return Inner.CreateDbContext(); }
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { Created++; return Inner.CreateDbContextAsync(cancellationToken); }
    }
    private sealed class DirectFactory(DbContextOptions<ZananceDbContext> options) : IDbContextFactory<ZananceDbContext>
    { public ZananceDbContext CreateDbContext() => new(options); }
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current { get; set; } = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    private sealed class RecordedReads : DbCommandInterceptor
    {
        public int RateQueries { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("\"ExchangeRates\"", StringComparison.Ordinal)) RateQueries++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
    private sealed class PageCacheObservation : DbCommandInterceptor
    {
        public List<int> EntryReadTargets { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Entries\"", StringComparison.Ordinal))
            {
                using var check = command.Connection!.CreateCommand(); check.Transaction = command.Transaction;
                check.CommandText = "PRAGMA cache_size";
                EntryReadTargets.Add(Convert.ToInt32(check.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
    private sealed class PausedRead : DbCommandInterceptor
    {
        private int _captured;
        public TaskCompletionSource Captured { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _captured, 1) == 0)
            { Captured.TrySetResult(); await Release.Task.WaitAsync(cancellationToken); }
            return result;
        }
    }
}
