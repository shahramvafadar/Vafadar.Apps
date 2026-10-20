using System.Collections.Specialized;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-419: complete actual detail publication, draft retention and original route retirement.</summary>
[Trait("AT", "AT-419")]
public sealed class HoldingDetailPublicationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(HoldingDetailViewModel Model, GateFactory Factory, Guid Id)> ReadyAsync(FlowFixture f, bool advanced = true)
    {
        var (store, type) = await HoldingOverviewFlowTests.SeedAsync(f);
        await f.Store.UpdateSettingsAsync(s => s.Mode = advanced ? ExperienceMode.Advanced : ExperienceMode.Simple, Ct);
        var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = HoldingDetailFlowTests.Create(f, new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await vm.LoadAsync();
        return (vm, factory, type.Id);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Complete_original_detail_publishes_all_facts_before_native_actions(bool advanced)
    {
        using var f = new FlowFixture(); var (vm, _, id) = await ReadyAsync(f, advanced);
        Assert.True(vm.Loading.IsReady); Assert.False(vm.NotFound); Assert.Equal(advanced, vm.IsAdvanced);
        Assert.Equal("2 piece", vm.QuantityText); Assert.Contains("50.00", vm.ValueText);
        Assert.Single(vm.PerLocation); Assert.Single(vm.Events); Assert.Single(vm.Valuations);
        Assert.Equal(1, vm.Events[0].QuantitySign); Assert.False(vm.ValueUnknown);
        vm.Events.CollectionChanged += (_, args) =>
        {
            Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action); Assert.False(vm.Loading.IsReady);
            Assert.False(vm.PriceEditor.CanSubmit); Assert.False(vm.HistoryEditor.CanOpen); Assert.False(vm.ArchiveEditor.CanArchive);
        };
        await vm.LoadAsync();
        await vm.RecordCommand.ExecuteAsync("Purchase"); await vm.RecordCommand.ExecuteAsync("Correction");
        await vm.EditTypeCommand.ExecuteAsync(null);
        Assert.Equal(advanced ? 3 : 1, f.Platform.Routes.Count);
        Assert.Equal(id, f.Platform.Routes[0].Parameters!["type"]);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Missing_original_type_is_a_complete_read_only_state_with_no_old_actions()
    {
        using var f = new FlowFixture(); var (vm, _, _) = await ReadyAsync(f);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = Guid.NewGuid() });
        await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.True(vm.NotFound);
        Assert.Null(vm.Name); Assert.Null(vm.ValueText); Assert.Empty(vm.Events); Assert.Empty(vm.Valuations);
        Assert.False(vm.PriceEditor.CanSubmit); Assert.False(vm.HistoryEditor.CanOpen); Assert.False(vm.ArchiveEditor.CanArchive);
        await vm.RecordCommand.ExecuteAsync("Correction"); await vm.EditTypeCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Failed_original_reload_keeps_old_values_covered_and_explicit_retry_reopens_same_file()
    {
        using var f = new FlowFixture(); var (vm, factory, _) = await ReadyAsync(f);
        var original = vm.Events.ToArray(); vm.PriceEditor.Raw = "77";
        factory.Before = _ => throw new IOException("Original detail read refused.");
        await Assert.ThrowsAsync<IOException>(vm.LoadAsync);
        Assert.True(vm.Loading.HasFailed); Assert.Equal(original, vm.Events); Assert.Equal("77", vm.PriceEditor.Raw);
        Assert.False(vm.PriceEditor.CanSubmit); await vm.RecordCommand.ExecuteAsync("Purchase"); Assert.Empty(f.Platform.Routes);
        factory.Before = null; await vm.PriceEditor.RefreshCommand.ExecuteAsync(null);
        Assert.True(vm.Loading.IsReady); Assert.Equal("77", vm.PriceEditor.Raw); Assert.True(vm.PriceEditor.CanSubmit);
    }

    [Fact]
    public async Task Pending_original_read_is_shared_then_retired_without_publishing_over_replacement()
    {
        using var f = new FlowFixture(); var (vm, factory, _) = await ReadyAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async _ => { entered.TrySetResult(); await release.Task; throw new IOException("Original late read failure"); };
        var first = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); var shared = vm.LoadAsync();
        Assert.True(vm.Loading.IsLoading); Assert.False(vm.PriceEditor.CanSubmit);
        vm.Retire(); factory.Before = null; await vm.LoadAsync(); var ready = vm.Events.ToArray();
        release.SetResult(); await Task.WhenAll(first, shared); Assert.True(vm.Loading.IsReady); Assert.Equal(ready, vm.Events);
        Assert.Empty(f.Platform.Failures);
    }

    [Fact]
    public async Task Reentrant_departure_during_native_history_reset_keeps_detail_and_price_authority_covered()
    {
        using var f = new FlowFixture(); var (vm, _, _) = await ReadyAsync(f);
        vm.Events.CollectionChanged += (_, _) => vm.Retire();
        await vm.LoadAsync(); Assert.False(vm.Loading.IsReady); Assert.False(vm.PriceEditor.CanSubmit);
        Assert.False(vm.HistoryEditor.CanOpen); Assert.False(vm.ArchiveEditor.CanArchive);
        await vm.RecordCommand.ExecuteAsync("Correction"); Assert.Empty(f.Platform.Routes);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task A_retained_original_route_refuses_another_actual_file_even_with_inactive_policy(bool edit)
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var (vm, factory, _) = await ReadyAsync(f);
        factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (edit) await vm.EditTypeCommand.ExecuteAsync(null); else await vm.RecordCommand.ExecuteAsync("Purchase");
        Assert.Empty(f.Platform.Routes); Assert.True(vm.Loading.HasFailed); Assert.False(vm.PriceEditor.CanSubmit);
        Assert.Single(f.Platform.Failures);
    }

    [Fact]
    public async Task A_retained_raw_price_and_theme_change_keep_original_values_but_require_explicit_refresh()
    {
        using var f = new FlowFixture(); var (vm, _, _) = await ReadyAsync(f);
        vm.PriceEditor.Raw = "77"; vm.PriceEditor.ModeIndex = 1;
        var original = vm.PriceEditor.DisplayedReview; await ((IThemeAware)vm).RefreshThemeAsync();
        Assert.Same(original, vm.PriceEditor.DisplayedReview); Assert.Equal("77", vm.PriceEditor.Raw); Assert.True(vm.PriceEditor.CanSubmit);
        vm.Retire(); await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); Assert.Equal("77", vm.PriceEditor.Raw);
        Assert.Equal(1, vm.PriceEditor.ModeIndex); Assert.True(vm.PriceEditor.NeedsRefresh); Assert.False(vm.PriceEditor.CanSubmit);
        await vm.PriceEditor.RefreshCommand.ExecuteAsync(null); Assert.True(vm.PriceEditor.CanSubmit); Assert.Equal("77", vm.PriceEditor.Raw);
    }

    internal sealed class GateFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal Func<CancellationToken, Task>? Before;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { if (Before is { } before) await before(cancellationToken); return await Current.CreateDbContextAsync(cancellationToken); }
    }
}
