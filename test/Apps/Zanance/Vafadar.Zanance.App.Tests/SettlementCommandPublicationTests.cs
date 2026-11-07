using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Busy/disabled-field notifications cannot replace a bill attempt or grant a departed form native consent.</summary>
public sealed class SettlementCommandPublicationTests
{
    [Fact]
    public async Task Changed_input_during_busy_publication_refuses_before_context_acquisition()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        var id = await SettlementOwnershipSnapshotTests.SeedAsync(f); var calls = 0;
        var factory = new AccountDetailOriginalBoundaryTests.ControlledFactory(OccurrenceOriginFailureTests.Factory(f))
            { Select = () => { calls++; return OccurrenceOriginFailureTests.Factory(f); } };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = new SettlementViewModel(store, f.Translator, f.Localization, f.Time, f.Platform);
        await model.LoadAsync(id); model.From = OccurrenceOriginFailureTests.Day; model.To = OccurrenceOriginFailureTests.Day; model.ActualText = "2.34";
        Assert.True(model.CanSave); var changed = false;
        model.PropertyChanged += (_, e) =>
        {
            if (!changed && e.PropertyName == nameof(model.IsBusy) && model.IsBusy)
            { changed = true; model.ActualText = "3.45"; }
        };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); calls = 0;
        await model.SaveCommand.ExecuteAsync(null);
        var after = await GoalChooserFlowTests.SnapshotAsync(f);
        var rows = await f.Store.GetEntriesAsync(cancellationToken: ct);
        Assert.True(!model.HasCommitted && calls == 0,
            $"Accepted={model.HasCommitted}; contexts={calls}; entryCount={rows.Count}; amounts={string.Join(',', rows.Select(e => e.Amount).Order())}");
        Assert.True(changed); Assert.Equal("3.45", model.ActualText); Assert.Equal(before, after);
    }

    [Fact]
    public async Task Retirement_during_busy_publication_cannot_open_discard_in_a_replacement_owner()
    {
        using var f = new FlowFixture(); var id = await SettlementOwnershipSnapshotTests.SeedAsync(f);
        var model = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        await model.LoadAsync(id); model.ActualText = "2.34"; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        model.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(model.IsBusy) && model.IsBusy) model.SetActive(false); };
        await model.CancelCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal("2.34", model.ActualText);
        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.ConfirmationDialogs);
    }
}
