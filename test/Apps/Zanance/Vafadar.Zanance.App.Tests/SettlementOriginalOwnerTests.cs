using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original bill read lifetimes cannot reactivate a departed form or follow a replacement profile.</summary>
public sealed class SettlementOriginalOwnerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Explicit_retry_must_not_move_the_original_bill_draft_into_a_replacement_file()
    {
        using var first = new FlowFixture(); using var other = new FlowFixture();
        var ids = await OccurrenceOriginFailureTests.SeedAsync(first, "EUR");
        await OccurrenceOriginFailureTests.SeedAsync(other, "USD", ids);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(OccurrenceOriginFailureTests.Factory(first));
        var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = new SettlementViewModel(store, first.Translator, first.Localization, first.Time, first.Platform);
        await model.LoadAsync(ids.Plan); Assert.True(model.CanEdit);
        model.ActualText = "123.4500";
        var beforeA = await GoalChooserFlowTests.SnapshotAsync(first); var beforeB = await GoalChooserFlowTests.SnapshotAsync(other);
        factory.Select = () => OccurrenceOriginFailureTests.Factory(other);
        await model.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(beforeA, await GoalChooserFlowTests.SnapshotAsync(first));
        Assert.Equal(beforeB, await GoalChooserFlowTests.SnapshotAsync(other));
        Assert.Equal("123.4500", model.ActualText); Assert.Empty(first.Platform.Routes);
        Assert.False(model.CanEdit);
        Assert.True(model.Loading.HasFailed);
    }

    [Fact]
    public async Task Cancel_during_the_original_pending_read_must_not_publish_an_editable_departed_form()
    {
        using var f = new FlowFixture(); var ids = await OccurrenceOriginFailureTests.SeedAsync(f, "EUR");
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(OccurrenceOriginFailureTests.Factory(f))
            { Before = async ct => { entered.TrySetResult(); await release.Task.WaitAsync(ct); } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = new SettlementViewModel(store, f.Translator, f.Localization, f.Time, f.Platform);
        var read = model.LoadAsync(ids.Plan);
        try { await entered.Task.WaitAsync(Ct); await model.CancelCommand.ExecuteAsync(null); }
        finally { release.TrySetResult(); }
        await read;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Single(f.Platform.Routes); Assert.Equal("..", f.Platform.Routes[0].Route);
        Assert.False(model.CanEdit);
        Assert.False(model.Loading.IsReady);
    }
}
