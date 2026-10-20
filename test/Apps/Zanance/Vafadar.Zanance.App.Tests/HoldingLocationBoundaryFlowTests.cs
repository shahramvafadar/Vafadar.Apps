using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-420: original metadata transaction, queued query and native publication boundaries.</summary>
[Trait("AT", "AT-420")]
public sealed class HoldingLocationBoundaryFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_retired_pending_owner_rolls_back_the_saved_row_before_commit_and_can_retry_one_absent_identity()
    {
        using var f = new FlowFixture(); var (_, store, type, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var review = await store.ReadEventReviewAsync(type.Id, ct: Ct);
        var id = Guid.CreateVersion7(); var calls = 0;
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.CreateReviewedLocationAsync(review, id, "Original rollback safe", () => ++calls == 1, Ct));
        Assert.Single(await store.GetLocationsAsync(Ct));
        var accepted = await store.CreateReviewedLocationAsync(review, id, "Original rollback safe", () => true, Ct);
        Assert.Equal(id, accepted.Id); Assert.Equal(2, (await store.GetLocationsAsync(Ct)).Count);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.CreateReviewedLocationAsync(review, Guid.NewGuid(), "Consumed original safe", () => true, Ct));
        Assert.Equal(2, (await store.GetLocationsAsync(Ct)).Count); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_new_explicit_original_query_waits_for_its_retired_place_writer_without_replaying_it()
    {
        using var f = new FlowFixture(); var (model, store, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async _ => { entered.TrySetResult(); await release.Task; };
        f.Platform.Inputs.Enqueue("Original retired writer safe"); var creating = model.AddLocationCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(Ct);
        var query = model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        release.SetResult(); await Task.WhenAll(creating, query).WaitAsync(Ct); factory.Before = null;
        Assert.Single(await store.GetLocationsAsync(Ct)); Assert.True(model.CanSave); Assert.Null(model.SavedLocationId);
        Assert.Null(model.LocationFeedback); Assert.Equal(type.Name, model.TypeName); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Reentrant_departure_during_choice_publication_retains_known_metadata_without_rearming_the_view()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        model.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(model.Locations)) model.RetireReview(); };
        f.Platform.Inputs.Enqueue("Original departed publication safe"); await model.AddLocationCommand.ExecuteAsync(null);
        Assert.NotNull(model.SavedLocationId); Assert.True(model.HasSavedLocationPending); Assert.False(model.CanSave); Assert.False(model.CanUseSavedLocation);
        Assert.Equal(2, (await store.GetLocationsAsync(Ct)).Count); Assert.Contains("Original departed publication safe", model.LocationFeedback);
    }

    [Fact]
    public async Task Failed_native_choice_publication_keeps_the_accepted_id_and_an_explicit_read_only_retry()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        void Fail(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        { if (args.PropertyName == nameof(model.Locations)) throw new IOException("Original native choice publication refused"); }
        model.PropertyChanged += Fail; f.Platform.Inputs.Enqueue("Original unpublished safe"); await model.AddLocationCommand.ExecuteAsync(null); model.PropertyChanged -= Fail;
        Assert.True(model.HasSavedLocationPending); Assert.False(model.CanSave); Assert.True(model.CanUseSavedLocation);
        var before = JsonSerializer.Serialize(await store.GetLocationsAsync(Ct)); await model.UseSavedLocationCommand.ExecuteAsync(null);
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetLocationsAsync(Ct))); Assert.True(model.CanSave);
        Assert.Equal(model.SavedLocationId, model.Location?.Id); Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText);
    }

    [Fact]
    public async Task Renaming_a_known_accepted_place_refuses_read_only_adoption_without_guessing_a_replacement()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        void Fail(object? sender, EventArgs args) => throw new IOException("Original known place notification failed");
        store.Changed += Fail; f.Platform.Inputs.Enqueue("Original accepted name"); await model.AddLocationCommand.ExecuteAsync(null); store.Changed -= Fail;
        var accepted = (await store.GetLocationsAsync(Ct)).Single(l => l.Id == model.SavedLocationId);
        accepted.Name = "Changed accepted name"; await store.SaveLocationAsync(accepted, Ct);
        var before = JsonSerializer.Serialize(await store.GetLocationsAsync(Ct)); await model.UseSavedLocationCommand.ExecuteAsync(null);
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetLocationsAsync(Ct))); Assert.True(model.HasSavedLocationPending); Assert.False(model.CanSave);
        Assert.Equal(f.Translator["AssetEvent_SourceChanged"], model.LocationError); Assert.Contains("Original accepted name", model.LocationFeedback);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Current_denied_or_retired_access_refuses_new_metadata_and_closes_original_financial_authority(bool retire)
    {
        using var f = new FlowFixture(); var (_, _, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var denied = new CommercialWriteAccess(f.Directory.Combine("zanance.db"),
            new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        var access = new Access { Current = retire ? CommercialWriteAccess.Inactive : denied };
        var holdings = new HoldingStore(factory, access);
        var model = new AssetEventEditorViewModel(holdings, f.Store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" }); model.QuantityText = "2";
        f.Platform.PendingInput = () => { access.Current = denied; return Task.FromResult<string?>("Denied original safe"); };
        await model.AddLocationCommand.ExecuteAsync(null);
        Assert.Single(await holdings.GetLocationsAsync(Ct)); Assert.Null(model.SavedLocationId); Assert.False(model.CanSave); Assert.NotNull(model.LocationError);
        Assert.Empty(await holdings.GetEventsAsync(cancellationToken: Ct));
    }

    private sealed class Access : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
}
