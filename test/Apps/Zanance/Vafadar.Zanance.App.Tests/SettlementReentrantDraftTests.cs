using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>A reentrant original error notification cannot silently replace the draft belonging to the attempted bill command.</summary>
public sealed class SettlementReentrantDraftTests
{
    [Fact]
    public async Task Changed_raw_input_during_save_validation_refuses_before_context_acquisition()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        var id = await SettlementOwnershipSnapshotTests.SeedAsync(f); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(OccurrenceOriginFailureTests.Factory(f))
            { Select = () => { calls++; return OccurrenceOriginFailureTests.Factory(f); } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = new SettlementViewModel(store, f.Translator, f.Localization, f.Time, f.Platform);
        await model.LoadAsync(id); model.From = OccurrenceOriginFailureTests.Day; model.To = OccurrenceOriginFailureTests.Day; model.ActualText = "2.34";
        Assert.True(model.CanSave); model.Error = "Previous original inline error"; var changed = false;
        model.PropertyChanged += (_, e) =>
        {
            if (!changed && e.PropertyName == nameof(model.Error) && model.Error is null)
            { changed = true; model.ActualText = "3.4500"; }
        };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); calls = 0;
        await model.SaveCommand.ExecuteAsync(null);
        Assert.True(changed); Assert.Equal("3.4500", model.ActualText); Assert.False(model.HasCommitted);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, calls);
    }
}
