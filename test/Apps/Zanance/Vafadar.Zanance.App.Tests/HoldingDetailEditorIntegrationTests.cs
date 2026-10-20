using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-419: existing accepted editor outcomes remain usable through the new complete detail cover.</summary>
[Trait("AT", "AT-419")]
public sealed class HoldingDetailEditorIntegrationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(HoldingDetailViewModel Model, HoldingStore Store)> PrepareAsync(FlowFixture f)
    {
        var (store, type) = await HoldingOverviewFlowTests.SeedAsync(f);
        await f.Store.UpdateSettingsAsync(s => s.Mode = ExperienceMode.Advanced, Ct);
        var model = HoldingDetailFlowTests.Create(f, store);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await model.LoadAsync(); return (model, store);
    }

    [Fact]
    public async Task Original_price_acceptance_refreshes_complete_detail_without_losing_accepted_identity()
    {
        using var f = new FlowFixture(); var (model, store) = await PrepareAsync(f);
        model.PriceEditor.Raw = "60"; await model.PriceEditor.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(model.PriceEditor.AcceptedId); Assert.False(model.PriceEditor.LastWasDeletion);
        Assert.True(model.Loading.IsReady); Assert.Contains("120.00", model.ValueText); Assert.Equal(string.Empty, model.PriceEditor.Raw);
        Assert.True(model.PriceEditor.CanSubmit); Assert.Equal(2, model.Valuations.Count);
        Assert.Equal(2, (await store.GetValuationsAsync(cancellationToken: Ct)).Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Original_history_delete_and_short_undo_refresh_the_same_complete_detail()
    {
        using var f = new FlowFixture(); var (model, _) = await PrepareAsync(f);
        var original = Assert.Single(model.Events).Id;
        f.Platform.Choices.Enqueue(f.Translator["Common_Delete"]); f.Platform.Confirmations.Enqueue(true);
        await model.OpenEventCommand.ExecuteAsync(original);
        Assert.True(model.HistoryEditor.DeleteCommitted); Assert.True(model.Loading.IsReady); Assert.Empty(model.Events);
        Assert.True(model.HistoryEditor.CanUndo); await model.HistoryEditor.UndoCommand.ExecuteAsync(null);
        Assert.True(model.HistoryEditor.UndoCommitted); Assert.True(model.Loading.IsReady);
        Assert.Equal(original, Assert.Single(model.Events).Id); Assert.Equal("2 piece", model.QuantityText);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Original_archive_confirmation_still_consumes_its_accepted_result_under_the_view_gate()
    {
        using var f = new FlowFixture(); var (model, store) = await PrepareAsync(f);
        f.Platform.Confirmations.Enqueue(true); await model.ArchiveCommand.ExecuteAsync(null);
        Assert.True(model.ArchiveEditor.Committed); Assert.False(model.ArchiveEditor.CanArchive);
        Assert.True(Assert.Single(await store.GetTypesAsync(Ct)).IsArchived);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        Assert.Single(await store.GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Delivering_another_type_never_exposes_the_previous_types_short_undo_or_raw_draft()
    {
        using var f = new FlowFixture(); var (model, store) = await PrepareAsync(f);
        var original = Assert.Single(model.Events).Id;
        f.Platform.Choices.Enqueue(f.Translator["Common_Delete"]); f.Platform.Confirmations.Enqueue(true);
        await model.OpenEventCommand.ExecuteAsync(original); Assert.True(model.HistoryEditor.CanUndo);
        var oldHistory = model.HistoryEditor;
        model.PriceEditor.Raw = "77";
        var next = new Vafadar.Zanance.Core.Holdings.AssetType { Name = "Another original detail", PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(next, Ct);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = next.Id }); await model.LoadAsync();
        Assert.True(model.Loading.IsReady); Assert.Equal(next.Name, model.Name);
        Assert.False(model.HistoryEditor.HasUndo); Assert.False(model.HistoryEditor.CanUndo); Assert.Equal(string.Empty, model.PriceEditor.Raw);
        Assert.False(oldHistory.CanUndo); await oldHistory.UndoCommand.ExecuteAsync(null);
        await model.HistoryEditor.UndoCommand.ExecuteAsync(null);
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_known_price_commit_after_departure_never_reopens_the_retired_detail_to_refresh()
    {
        using var f = new FlowFixture(); var (model, store) = await PrepareAsync(f);
        var editor = model.PriceEditor; editor.Raw = "60";
        store.Changed += (_, _) => model.Retire();
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(editor.AcceptedId); Assert.False(model.Loading.IsReady); Assert.False(editor.CanSubmit);
        Assert.Equal(string.Empty, editor.Raw); Assert.NotNull(editor.Error);
        Assert.Equal(2, (await store.GetValuationsAsync(cancellationToken: Ct)).Count);
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(2, (await store.GetValuationsAsync(cancellationToken: Ct)).Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
