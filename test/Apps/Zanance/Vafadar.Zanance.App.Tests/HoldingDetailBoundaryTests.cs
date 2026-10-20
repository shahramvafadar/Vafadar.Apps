using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-419: fresh source validation and original navigation cancellation over actual SQLite.</summary>
[Trait("AT", "AT-419")]
public sealed class HoldingDetailBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory, InlineData(false), InlineData(true)]
    public async Task Changed_original_settings_or_type_before_publication_stays_covered(bool settings)
    {
        using var f = new FlowFixture(); var (store, type) = await HoldingOverviewFlowTests.SeedAsync(f);
        var vm = HoldingDetailFlowTests.Create(f, store);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id });
        f.Time.BeforeRead = () =>
        {
            f.Time.BeforeRead = null;
            if (settings) f.Store.UpdateSettingsAsync(s => s.Mode = ExperienceMode.Advanced, Ct).GetAwaiter().GetResult();
            else { type.Note = "Original concurrent metadata"; store.SaveTypeAsync(type, Ct).GetAwaiter().GetResult(); }
        };
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(vm.LoadAsync);
        Assert.True(vm.Loading.HasFailed); Assert.Null(vm.Name); Assert.Empty(vm.Events);
        Assert.False(vm.PriceEditor.CanSubmit); Assert.False(vm.HistoryEditor.CanOpen);
        Assert.Empty(f.Platform.Routes); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Pending_native_navigation_retires_before_factory_authority_is_returned()
    {
        using var f = new FlowFixture(); var (store, type) = await HoldingOverviewFlowTests.SeedAsync(f);
        await f.Store.UpdateSettingsAsync(s => s.Mode = ExperienceMode.Advanced, Ct);
        var factory = new HoldingDetailPublicationTests.GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = HoldingDetailFlowTests.Create(f, new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async ct => { entered.SetResult(); await release.Task.WaitAsync(ct); };
        var navigation = vm.RecordCommand.ExecuteAsync("Purchase"); await entered.Task.WaitAsync(Ct); vm.Retire();
        await navigation; Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Failures);
        Assert.False(vm.Loading.IsReady); Assert.False(vm.PriceEditor.CanSubmit);
    }

    [Fact]
    public async Task Retained_raw_detail_never_reads_a_new_file_on_reappearance()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        var (store, type) = await HoldingOverviewFlowTests.SeedAsync(f);
        var factory = new HoldingDetailPublicationTests.GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = HoldingDetailFlowTests.Create(f, new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync();
        vm.PriceEditor.Raw = "77"; vm.Retire(); factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(vm.LoadAsync);
        Assert.Equal("77", vm.PriceEditor.Raw); Assert.True(vm.Loading.HasFailed); Assert.False(vm.PriceEditor.CanSubmit);
        Assert.Empty(f.Platform.Routes); Assert.Empty(await other.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
