using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-418: original overview native target/file ownership and real concurrent SQLite reads.</summary>
[Trait("AT", "AT-418")]
public sealed class HoldingOverviewBoundaryFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory, InlineData(false), InlineData(true)]
    public async Task Selected_file_change_refuses_a_retained_open_or_add_even_with_inactive_policy(bool add)
    {
        using var first = new FlowFixture(); using var next = new FlowFixture();
        await HoldingOverviewFlowTests.SeedAsync(first);
        await first.Store.UpdateSettingsAsync(settings => settings.Mode = ExperienceMode.Advanced, Ct);
        var factory = new RedirectFactory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = HoldingOverviewFlowTests.Create(first, new HoldingStore(factory, first.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        await vm.LoadAsync(); var original = Assert.Single(vm.Rows); factory.Current = next.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (add) await vm.AddCommand.ExecuteAsync(null); else await vm.OpenCommand.ExecuteAsync(original);
        Assert.Empty(first.Platform.Routes); Assert.Single(first.Platform.Failures); Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady);
        await vm.OpenCommand.ExecuteAsync(original); Assert.Single(first.Platform.Failures);
        await using var db = await factory.Current.CreateDbContextAsync(Ct);
        Assert.False(await db.Settings.AnyAsync(Ct)); Assert.False(await db.Entries.AnyAsync(Ct)); Assert.False(await db.AssetTypes.AnyAsync(Ct));
    }

    [Fact]
    public async Task Retained_navigation_waiting_for_its_actual_factory_cannot_open_after_departure()
    {
        using var f = new FlowFixture(); await HoldingOverviewFlowTests.SeedAsync(f);
        var factory = new RedirectFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = HoldingOverviewFlowTests.Create(f, new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        await vm.LoadAsync(); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async ct => { entered.SetResult(); await release.Task.WaitAsync(ct); };
        var opening = vm.OpenCommand.ExecuteAsync(Assert.Single(vm.Rows)); await entered.Task.WaitAsync(Ct); vm.Retire();
        factory.Before = null; await vm.LoadAsync(); release.TrySetResult(); await opening;
        Assert.True(vm.Loading.IsReady); Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Access_retirement_during_the_original_native_reset_keeps_totals_covered()
    {
        using var f = new FlowFixture(); await HoldingOverviewFlowTests.SeedAsync(f);
        var access = new Access(); var reader = new HoldingStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), access);
        var vm = HoldingOverviewFlowTests.Create(f, reader); var resets = 0;
        void Retire(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            resets++; access.Current = new(f.Directory.Combine("zanance.db"),
                new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        }
        vm.Rows.CollectionChanged += Retire; await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        Assert.Equal(1, resets); Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady); Assert.Null(vm.KnownValueText);
        vm.Rows.CollectionChanged -= Retire; access.Current = CommercialWriteAccess.Inactive; await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Contains("50.00", vm.KnownValueText);
    }

    [Fact]
    public async Task One_real_deferred_reader_never_mixes_a_concurrent_type_and_quantity_commit()
    {
        using var f = new FlowFixture(); var (_, type) = await HoldingOverviewFlowTests.SeedAsync(f);
        var actual = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var mode = await actual.CreateDbContextAsync(Ct)) await mode.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", Ct);
        var interceptor = new ConcurrentRead(actual, type.Id);
        var factory = new InterceptedFactory(new DbContextOptionsBuilder<ZananceDbContext>()
            .UseSqlite("Data Source=" + f.Directory.Combine("zanance.db") + ";Pooling=False").AddInterceptors(interceptor).Options, f.Time);
        var reader = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var snapshot = await reader.ReadOverviewSnapshotAsync(Ct);
        Assert.True(interceptor.Committed); Assert.Equal("Original", Assert.Single(snapshot.Inputs.Types).Name);
        Assert.Equal(2000, Assert.Single(snapshot.Inputs.Events).Quantity); Assert.Equal(5, interceptor.Queries);
        Assert.True(interceptor.OneConnectionAndTransaction);
        await Assert.ThrowsAsync<HoldingOverviewSupersededException>(() => reader.ValidateOverviewSnapshotAsync(snapshot, Ct));
        Assert.Equal("Concurrent actual type", Assert.Single((await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct))).Name);
        Assert.Equal(3000, Assert.Single(await f.Services.GetRequiredService<HoldingStore>().GetEventsAsync(cancellationToken: Ct)).Quantity);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Failed_old_reader_after_cancellation_never_reports_over_a_new_ready_lifetime()
    {
        using var f = new FlowFixture(); await HoldingOverviewFlowTests.SeedAsync(f);
        var factory = new RedirectFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = HoldingOverviewFlowTests.Create(f, new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async _ => { entered.SetResult(); await release.Task; throw new IOException("Original delayed file failure after departure"); };
        var old = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); vm.Retire(); factory.Before = null;
        await vm.LoadAsync(); var fresh = Assert.Single(vm.Rows); release.SetResult(); await old;
        Assert.True(vm.Loading.IsReady); Assert.Same(fresh, Assert.Single(vm.Rows)); Assert.Empty(f.Platform.Failures);
    }

    /// <summary>Controls only the currently opened actual factory and its cancellable lease wait.</summary>
    private sealed class RedirectFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal Func<CancellationToken, Task>? Before;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { if (Before is { } before) await before(cancellationToken); return await Current.CreateDbContextAsync(cancellationToken); }
    }
    /// <summary>Explicit cached source; no commercial activation or provider data is implied.</summary>
    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
    /// <summary>Adds an actual EF interceptor to the same real SQLite file.</summary>
    private sealed class InterceptedFactory(DbContextOptions<ZananceDbContext> options, TimeProvider time) : IDbContextFactory<ZananceDbContext>
    { public ZananceDbContext CreateDbContext() => new(options, time); }
    /// <summary>Commits a real atomic concurrent change between the original type and event SELECTs.</summary>
    private sealed class ConcurrentRead(IDbContextFactory<ZananceDbContext> writer, Guid id) : DbCommandInterceptor
    {
        private DbConnection? _connection;
        internal int Queries;
        internal bool Committed;
        internal bool OneConnectionAndTransaction = true;
        private void Inspect(DbCommand command)
        {
            Queries++; _connection ??= command.Connection;
            OneConnectionAndTransaction &= ReferenceEquals(_connection, command.Connection) && command.Transaction is not null;
            if (!Committed && command.CommandText.Contains("FROM \"AssetEvents\"", StringComparison.Ordinal))
            {
                Committed = true;
                using var db = writer.CreateDbContext(); using var transaction = db.Database.BeginTransaction();
                db.Database.ExecuteSqlInterpolated($"UPDATE AssetTypes SET Name = {"Concurrent actual type"} WHERE Id = {id}");
                db.Database.ExecuteSqlInterpolated($"UPDATE AssetEvents SET Quantity = {3000L} WHERE AssetTypeId = {id}");
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
