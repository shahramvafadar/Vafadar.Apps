using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-419: actual original holding detail file, empty read and native navigation boundaries.</summary>
[Trait("AT", "AT-419")]
public sealed class HoldingDetailFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static HoldingDetailViewModel Create(FlowFixture f, HoldingStore store) => new(store,
        new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator,
        f.Localization, f.Time, f.Platform);

    [Fact]
    public async Task Original_detail_never_follows_a_new_file_after_its_type_check()
    {
        using var first = new FlowFixture(); using var next = new FlowFixture();
        var (store, type) = await HoldingOverviewFlowTests.SeedAsync(first);
        var other = next.Services.GetRequiredService<HoldingStore>();
        await other.SaveTypeAsync(new AssetType(type.Id) { Name = "Another profile type", Kind = type.Kind,
            Dimension = type.Dimension, PriceCurrencyCode = type.PriceCurrencyCode }, Ct);
        var factory = new SwitchingFactory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),
            next.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Create(first, new HoldingStore(factory, first.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id });
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(vm.LoadAsync);
        Assert.NotEqual("Another profile type", vm.Name);
        Assert.Empty(await next.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Opening_an_existing_type_does_not_initialize_missing_settings()
    {
        using var f = new FlowFixture();
        var (store, type) = await HoldingOverviewFlowTests.SeedAsync(f);
        var vm = Create(f, store); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id });
        await vm.LoadAsync();
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.False(await db.Settings.AnyAsync(Ct));
    }

    private sealed class SwitchingFactory(IDbContextFactory<ZananceDbContext> first,
        IDbContextFactory<ZananceDbContext> next) : IDbContextFactory<ZananceDbContext>
    {
        private int _reads;
        public ZananceDbContext CreateDbContext() => Interlocked.Increment(ref _reads) == 1 ? first.CreateDbContext() : next.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
