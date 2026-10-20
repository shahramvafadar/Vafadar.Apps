using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Core.Commerce;
using System.Collections.Specialized;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-418: original actual-file holding list reads and complete native publication boundaries.</summary>
[Trait("AT", "AT-418")]
public sealed class HoldingOverviewFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal static HoldingsViewModel Create(FlowFixture f, HoldingStore holdings) => new(holdings,
        new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Time, f.Platform);

    [Fact]
    public async Task Original_list_never_combines_a_type_with_another_files_history()
    {
        using var original = new FlowFixture(); using var next = new FlowFixture();
        var store = original.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original counted holding", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(type, Ct);
        var place = await store.EnsureDefaultLocationAsync("Original safe", Ct);
        Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
            Date = new(2026, 10, 1), Kind = AssetEventKind.Opening, Quantity = 2000, BasisAmount = 5000 }, [], Ct)).Succeeded);
        var factory = new SwitchingFactory(original.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),
            next.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Create(original, new HoldingStore(factory, original.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        await Assert.ThrowsAsync<InvalidOperationException>(vm.LoadAsync);
        Assert.False(vm.Loading.IsReady); Assert.True(vm.Loading.HasFailed); Assert.Empty(vm.Rows);
        Assert.Empty(await next.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Reading_an_empty_list_never_creates_default_financial_settings()
    {
        using var f = new FlowFixture();
        await Create(f, f.Services.GetRequiredService<HoldingStore>()).LoadAsync();
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.False(await db.Settings.AnyAsync(Ct));
        Assert.False(await db.AssetTypes.AnyAsync(Ct)); Assert.False(await db.AssetEvents.AnyAsync(Ct));
        Assert.False(await db.AssetLocations.AnyAsync(Ct)); Assert.False(await db.Entries.AnyAsync(Ct));
    }

    internal static async Task<(HoldingStore Store, AssetType Type)> SeedAsync(FlowFixture f, string name = "Original", string currency = "EUR")
    {
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = name, Dimension = AssetDimension.Count, Kind = AssetKind.CountableItem, PriceCurrencyCode = currency };
        await store.SaveTypeAsync(type, Ct); var place = await store.EnsureDefaultLocationAsync("Original safe", Ct);
        Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
            Kind = AssetEventKind.Opening, Date = new(2026, 10, 1), Quantity = 2000, Note = "Original quantity" }, [], Ct)).Succeeded);
        await store.SaveValuationAsync(new AssetValuation { AssetTypeId = type.Id, CurrencyCode = currency,
            Date = new(2026, 10, 1), PricePerUnitMilli = 2500000 }, Ct);
        return (store, type);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Complete_original_list_preserves_quantity_value_unknown_archive_and_mode(bool advanced)
    {
        using var f = new FlowFixture(); var (store, type) = await SeedAsync(f);
        await f.Store.UpdateSettingsAsync(settings => settings.Mode = advanced ? ExperienceMode.Advanced : ExperienceMode.Simple, Ct);
        var unknown = new AssetType { Name = "Unknown original", Dimension = AssetDimension.Count, PriceCurrencyCode = "USD" };
        await store.SaveTypeAsync(unknown, Ct); var place = Assert.Single(await store.GetLocationsAsync(Ct));
        Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = unknown.Id, LocationId = place.Id,
            Date = new(2026, 10, 1), Kind = AssetEventKind.Opening, Quantity = 1000 }, [], Ct)).Succeeded);
        var archived = new AssetType { Name = "Archived original", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", IsArchived = true };
        await store.SaveTypeAsync(archived, Ct);
        var vm = Create(f, store); var resets = 0;
        vm.Rows.CollectionChanged += (_, e) => { Assert.False(vm.Loading.IsReady); Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action); resets++; };
        await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.False(vm.IsEmpty); Assert.Equal(advanced, vm.IsAdvanced);
        Assert.Equal(1, resets); Assert.Equal(2, vm.Rows.Count); Assert.DoesNotContain(vm.Rows, r => r.Id == archived.Id);
        var known = Assert.Single(vm.Rows, r => r.Id == type.Id); Assert.Equal("2 piece", known.QuantityText);
        Assert.Contains("Original safe", known.DetailText); Assert.Contains("50.00", known.ValueText); Assert.False(known.ValueUnknown);
        Assert.True(Assert.Single(vm.Rows, r => r.Id == unknown.Id).ValueUnknown); Assert.Contains("1", vm.IncompleteText);
        Assert.Contains("50.00", vm.KnownValueText); Assert.DoesNotContain("USD", vm.KnownValueText); Assert.Empty(vm.FineMetals);
        await vm.AddCommand.ExecuteAsync(null); Assert.Equal(advanced ? 1 : 0, f.Platform.Routes.Count);
        await vm.OpenCommand.ExecuteAsync(known); Assert.Equal(advanced ? 2 : 1, f.Platform.Routes.Count);
        Assert.Equal(type.Id, f.Platform.Routes[^1].Parameters!["id"]);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Pending_reload_shares_one_read_and_retained_or_copied_rows_cannot_navigate()
    {
        using var f = new FlowFixture(); var (store, _) = await SeedAsync(f);
        await f.Store.UpdateSettingsAsync(settings => settings.Mode = ExperienceMode.Advanced, Ct);
        var factory = new ControlledFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Create(f, new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        await vm.LoadAsync(); var original = Assert.Single(vm.Rows); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.BeforeCreate = async ct => { entered.SetResult(); await release.Task.WaitAsync(ct); };
        var loading = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); var shared = vm.LoadAsync();
        Assert.True(vm.Loading.IsLoading); Assert.False(vm.Loading.IsReady);
        await vm.OpenCommand.ExecuteAsync(original); await vm.AddCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        factory.BeforeCreate = null; release.SetResult(); await Task.WhenAll(loading, shared);
        Assert.True(vm.Loading.IsReady); var fresh = Assert.Single(vm.Rows); Assert.NotSame(original, fresh);
        await vm.OpenCommand.ExecuteAsync(original); await vm.OpenCommand.ExecuteAsync(fresh with { }); Assert.Empty(f.Platform.Routes);
        await vm.OpenCommand.ExecuteAsync(fresh); Assert.Single(f.Platform.Routes);
    }

    [Fact]
    public async Task Departed_original_read_cannot_publish_or_fail_a_new_lifetime()
    {
        using var f = new FlowFixture(); var (store, _) = await SeedAsync(f);
        var factory = new ControlledFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Create(f, new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.BeforeCreate = async ct => { entered.SetResult(); await release.Task.WaitAsync(ct); };
        var old = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); var originalCover = vm.Loading; vm.Retire();
        Assert.NotSame(originalCover, vm.Loading); Assert.False(vm.Loading.IsReady);
        factory.BeforeCreate = null; await vm.LoadAsync(); var fresh = Assert.Single(vm.Rows);
        release.TrySetResult(); await old; Assert.True(vm.Loading.IsReady); Assert.Same(fresh, Assert.Single(vm.Rows));
        Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Reentrant_departure_during_native_reset_cannot_publish_later_totals()
    {
        using var f = new FlowFixture(); var (store, _) = await SeedAsync(f); var vm = Create(f, store);
        NotifyCollectionChangedEventHandler retire = (_, _) => vm.Retire(); vm.Rows.CollectionChanged += retire;
        await vm.LoadAsync(); Assert.False(vm.Loading.IsReady); Assert.Null(vm.KnownValueText); Assert.False(vm.IsAdvanced);
        vm.Rows.CollectionChanged -= retire; await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.Contains("50.00", vm.KnownValueText);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Same_file_source_supersession_is_bounded_under_the_original_cover(bool continuous)
    {
        using var f = new FlowFixture(); var (store, type) = await SeedAsync(f); var vm = Create(f, store); var edits = 0;
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        f.Time.BeforeRead = () =>
        {
            if (!continuous) f.Time.BeforeRead = null;
            using var db = factory.CreateDbContext(); edits++;
            db.Database.ExecuteSqlInterpolated($"UPDATE AssetTypes SET Name = {"Changed " + edits} WHERE Id = {type.Id}");
        };
        if (continuous)
        {
            await Assert.ThrowsAsync<HoldingOverviewSupersededException>(vm.LoadAsync);
            Assert.Equal(3, edits); Assert.False(vm.Loading.IsReady); Assert.True(vm.Loading.HasFailed); Assert.Empty(vm.Rows);
            f.Time.BeforeRead = null; await vm.LoadAsync();
        }
        else { await vm.LoadAsync(); Assert.Equal(1, edits); }
        Assert.True(vm.Loading.IsReady); Assert.Equal("Changed " + edits, Assert.Single(vm.Rows).Name);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Failed_reload_preserves_covered_original_rows_and_retry_publishes_a_complete_result()
    {
        using var f = new FlowFixture(); var (store, _) = await SeedAsync(f); var vm = Create(f, store); await vm.LoadAsync();
        var original = Assert.Single(vm.Rows); var originalTotal = vm.KnownValueText; var calls = 0;
        f.Time.BeforeRead = () => { calls++; throw new IOException("Original overview clock/read boundary"); };
        await Assert.ThrowsAsync<IOException>(vm.LoadAsync); Assert.Equal(1, calls); Assert.True(vm.Loading.HasFailed);
        Assert.False(vm.Loading.IsReady); Assert.Same(original, Assert.Single(vm.Rows)); Assert.Equal(originalTotal, vm.KnownValueText);
        await vm.OpenCommand.ExecuteAsync(original); Assert.Empty(f.Platform.Routes);
        f.Time.BeforeRead = null; await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.NotSame(original, Assert.Single(vm.Rows));
    }

    [Fact]
    public async Task Total_overflow_never_replaces_original_rows_with_a_partial_result()
    {
        using var f = new FlowFixture(); var (store, _) = await SeedAsync(f); var vm = Create(f, store); await vm.LoadAsync();
        var original = Assert.Single(vm.Rows); var originalTotal = vm.KnownValueText; var place = Assert.Single(await store.GetLocationsAsync(Ct));
        for (var i = 0; i < 2; i++)
        {
            var type = new AssetType { Name = "Large " + i, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
            await store.SaveTypeAsync(type, Ct);
            Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
                Kind = AssetEventKind.Opening, Date = new(2026, 10, 1), Quantity = 1000000 }, [], Ct)).Succeeded);
            await store.SaveValuationAsync(new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR",
                Date = new(2026, 10, 1), PricePerUnitMilli = long.MaxValue }, Ct);
        }
        await Assert.ThrowsAsync<OverflowException>(vm.LoadAsync); Assert.Same(original, Assert.Single(vm.Rows));
        Assert.Equal(originalTotal, vm.KnownValueText); Assert.False(vm.Loading.IsReady); Assert.True(vm.Loading.HasFailed);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Changed_packet_or_retired_access_is_not_a_same_file_source_retry(bool retireAccess)
    {
        using var f = new FlowFixture(); var (store, type) = await SeedAsync(f);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var access = new MutableAccess();
        var reader = new HoldingStore(factory, access); var packet = await reader.ReadOverviewSnapshotAsync(Ct);
        type.Name = "New persisted source"; await store.SaveTypeAsync(type, Ct);
        if (retireAccess) access.Current = new(packet.DatabasePath, new CapabilityContext(ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        else packet.Inputs.Types[0].Name = "Changed original memory";
        var failure = await Record.ExceptionAsync(() => reader.ValidateOverviewSnapshotAsync(packet, Ct));
        Assert.IsType<InvalidOperationException>(failure); Assert.False(failure is HoldingOverviewSupersededException);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Controls only the actual context wait, retaining real SQLite and cancellation.</summary>
    private sealed class ControlledFactory(IDbContextFactory<ZananceDbContext> actual) : IDbContextFactory<ZananceDbContext>
    {
        internal Func<CancellationToken, Task>? BeforeCreate;
        public ZananceDbContext CreateDbContext() => actual.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { if (BeforeCreate is { } before) await before(cancellationToken); return await actual.CreateDbContextAsync(cancellationToken); }
    }

    /// <summary>Publishes explicit cached read-authority changes without a provider or fake persistence.</summary>
    private sealed class MutableAccess : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }

    /// <summary>Changes only the actual factory's opened file after its first context, without replacing persistence.</summary>
    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> first,
        IDbContextFactory<ZananceDbContext> next) : IDbContextFactory<ZananceDbContext>
    {
        private int _calls;
        public ZananceDbContext CreateDbContext() =>
            (Interlocked.Increment(ref _calls) == 1 ? first : next).CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            (Interlocked.Increment(ref _calls) == 1 ? first : next).CreateDbContextAsync(cancellationToken);
    }
}
