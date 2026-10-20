using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-420: actual original place outcomes, stable retry and read-only choice recovery.</summary>
[Trait("AT", "AT-420")]
public sealed class HoldingLocationOutcomeFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory, InlineData(false), InlineData(true)]
    public async Task One_original_place_is_selected_without_resetting_the_nonfinancial_or_move_draft(bool transfer)
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        if (transfer) model.KindIndex = 2;
        var from = model.Location?.Id; var day = model.Date; f.Platform.Inputs.Enqueue("  New original safe  ");
        await model.AddLocationCommand.ExecuteAsync(null);
        Assert.NotNull(model.SavedLocationId); Assert.False(model.HasSavedLocationPending); Assert.True(model.CanSave);
        Assert.Null(model.LocationFeedback); Assert.Null(model.LocationError);
        var saved = (await store.GetLocationsAsync(Ct)).Single(l => l.Id == model.SavedLocationId);
        Assert.Equal("New original safe", saved.Name); Assert.False(saved.IsArchived);
        Assert.Equal(model.SavedLocationId, transfer ? model.ToLocation?.Id : model.Location?.Id);
        if (transfer) Assert.Equal(from, model.Location?.Id);
        Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText); Assert.Equal(day, model.Date);
        Assert.Equal("Original unchanged event draft", model.Note); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        if (!transfer)
        {
            await model.SaveCommand.ExecuteAsync(null); Assert.False(model.CanSave);
            var original = Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
            Assert.Equal(model.SavedLocationId, original.LocationId); Assert.Equal(2000, original.Quantity); Assert.Equal(5000, original.BasisAmount);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
    }

    [Fact]
    public async Task Failed_original_write_keeps_the_prompt_name_and_absent_identity_for_one_explicit_retry()
    {
        using var f = new FlowFixture(); var (model, store, _, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        factory.Before = _ => throw new IOException("Original transient place writer failure"); f.Platform.Inputs.Enqueue("Original retry safe");
        await model.AddLocationCommand.ExecuteAsync(null); var id = model.PendingLocationId;
        Assert.NotNull(id); Assert.Null(model.SavedLocationId); Assert.NotNull(model.LocationError); Assert.True(model.CanAddLocation);
        factory.Before = null; Assert.Single(await store.GetLocationsAsync(Ct)); f.Platform.Inputs.Enqueue("Original retry safe");
        await model.AddLocationCommand.ExecuteAsync(null);
        Assert.Equal(id, model.SavedLocationId); Assert.Equal("Original retry safe", f.Platform.PromptInitialValues.Last());
        Assert.Equal(2, (await store.GetLocationsAsync(Ct)).Count); Assert.True(model.CanSave); Assert.Null(model.PendingLocationId);
    }

    [Fact]
    public async Task Known_original_commit_survives_notification_failure_and_only_reads_its_choices_again()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        store.Changed += (_, _) => throw new IOException("Original post-commit place notification failure");
        f.Platform.Inputs.Enqueue("Known original saved safe"); await model.AddLocationCommand.ExecuteAsync(null);
        Assert.NotNull(model.SavedLocationId); Assert.True(model.HasSavedLocationPending); Assert.False(model.CanSave); Assert.False(model.CanAddLocation);
        Assert.Contains("Known original saved safe", model.LocationFeedback); Assert.Null(model.LocationError);
        var before = JsonSerializer.Serialize(await store.GetLocationsAsync(Ct)); var prompts = f.Platform.PromptInitialValues.Count;
        await model.AddLocationCommand.ExecuteAsync(null); Assert.Equal(prompts, f.Platform.PromptInitialValues.Count);
        await model.UseSavedLocationCommand.ExecuteAsync(null);
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetLocationsAsync(Ct)));
        Assert.False(model.HasSavedLocationPending); Assert.True(model.CanSave); Assert.Equal(model.SavedLocationId, model.Location?.Id);
        Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Known_commit_with_failed_choice_read_never_becomes_another_create()
    {
        using var f = new FlowFixture(); var (model, store, _, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        store.Changed += (_, _) => factory.Before = _ => throw new IOException("Original accepted place readback failure");
        f.Platform.Inputs.Enqueue("Original readback safe"); await model.AddLocationCommand.ExecuteAsync(null);
        Assert.NotNull(model.SavedLocationId); Assert.True(model.HasSavedLocationPending); Assert.False(model.CanSave); Assert.False(model.CanAddLocation);
        var id = model.SavedLocationId; Assert.Contains("Original readback safe", model.LocationFeedback);
        factory.Before = null; Assert.Equal(2, (await store.GetLocationsAsync(Ct)).Count);
        await model.UseSavedLocationCommand.ExecuteAsync(null);
        Assert.Equal(id, model.Location?.Id); Assert.True(model.CanSave); Assert.Equal(2, (await store.GetLocationsAsync(Ct)).Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_prompt_departure_never_creates_a_place_or_reports_over_another_view()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        f.Platform.PendingInput = () => { model.RetireReview(); return Task.FromResult<string?>("Departed original safe"); };
        await model.AddLocationCommand.ExecuteAsync(null);
        Assert.Single(await store.GetLocationsAsync(Ct)); Assert.Null(model.SavedLocationId); Assert.Null(model.LocationError);
        Assert.False(model.CanSave); Assert.Equal("2", model.QuantityText);
    }

    [Fact]
    public async Task A_commit_completed_after_departure_keeps_its_known_id_but_cannot_rearm_the_old_form()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        store.Changed += (_, _) => model.RetireReview(); f.Platform.Inputs.Enqueue("Original late accepted safe");
        await model.AddLocationCommand.ExecuteAsync(null); Assert.NotNull(model.SavedLocationId);
        Assert.True(model.HasSavedLocationPending); Assert.False(model.CanSave); Assert.False(model.CanUseSavedLocation);
        var before = JsonSerializer.Serialize(await store.GetLocationsAsync(Ct)); await model.UseSavedLocationCommand.ExecuteAsync(null);
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetLocationsAsync(Ct))); Assert.False(model.CanSave);
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Changed_raw_input_while_the_original_prompt_is_pending_refuses_metadata_and_preserves_the_input()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        f.Platform.PendingInput = () => { model.QuantityText = "3"; return Task.FromResult<string?>("Original changed raw safe"); };
        await model.AddLocationCommand.ExecuteAsync(null);
        Assert.Single(await store.GetLocationsAsync(Ct)); Assert.Equal("3", model.QuantityText); Assert.False(model.CanSave);
        Assert.Equal(f.Translator["AssetEvent_SourceChanged"], model.LocationError); Assert.Null(model.SavedLocationId);
    }
}
