using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-421: original cover, read-only Retry, bootstrap and accepted callback ownership.</summary>
[Trait("AT", "AT-421")]
public sealed class HoldingEventPublicationBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static AssetEventEditorViewModel Model(FlowFixture f, HoldingStore store) => new(store, f.Store,
        new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);

    [Fact]
    public async Task Pending_original_reads_cover_old_fields_and_publish_every_choice_before_Save_is_enabled()
    {
        using var f = new FlowFixture(); var (model, _, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async _ => { entered.TrySetResult(); await release.Task; };
        var pending = model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        await entered.Task.WaitAsync(Ct); Assert.True(model.Loading.IsLoading); Assert.False(model.Loading.IsReady); Assert.False(model.CanSave);
        var completeBeforeUncover = false;
        model.Loading.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(model.Loading.IsReady) && model.Loading.IsReady)
                completeBeforeUncover = model.TypeName == type.Name && model.Locations.Count == 1 && model.QuantityText == "2" && model.BasisText == "50";
        };
        release.SetResult(); await pending; Assert.True(completeBeforeUncover); Assert.True(model.CanSave);
    }

    [Fact]
    public async Task A_failed_original_reload_keeps_the_raw_draft_and_explicit_read_only_Retry_does_not_clear_its_dirty_state()
    {
        using var f = new FlowFixture(); var (model, store, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var day = model.Date; var place = model.Location?.Id; Assert.True(model.IsDirty);
        factory.Before = _ => throw new IOException("Original reload refused");
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        Assert.True(model.Loading.HasFailed); Assert.True(model.CanReload); Assert.False(model.CanSave); Assert.NotNull(model.LoadError);
        factory.Before = null; await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.Loading.IsReady); Assert.True(model.CanSave); Assert.True(model.IsDirty);
        Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText); Assert.Equal(day, model.Date); Assert.Equal(place, model.Location?.Id);
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Departure_during_an_original_read_never_publishes_or_reenables_a_retained_Retry_target()
    {
        using var f = new FlowFixture(); var (model, _, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async _ => { entered.TrySetResult(); await release.Task; };
        var pending = model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        await entered.Task.WaitAsync(Ct); model.RetireReview(); release.SetResult(); await pending;
        Assert.False(model.CanSave); Assert.False(model.CanReload); Assert.False(model.Loading.IsReady);
    }

    [Fact]
    public async Task A_failed_original_form_cannot_retry_into_a_later_file_even_with_the_same_type_identity()
    {
        using var f = new FlowFixture(); using var next = new FlowFixture();
        var (model, _, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var foreign = next.Services.GetRequiredService<HoldingStore>();
        await foreign.SaveTypeAsync(new AssetType(type.Id) { Name = type.Name, Kind = type.Kind, Dimension = type.Dimension, PriceCurrencyCode = type.PriceCurrencyCode }, Ct);
        factory.Before = _ => throw new IOException("Original file unavailable");
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        factory.Before = null; factory.Current = next.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanSave); Assert.Equal("2", model.QuantityText);
        Assert.Empty(await foreign.GetLocationsAsync(Ct)); Assert.Empty(await foreign.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_valid_first_form_creates_only_its_original_default_place_without_initializing_display_settings_or_finance()
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original first event", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(type, Ct); store.Changed += (_, _) => throw new IOException("Original known default place notification failed");
        var model = Model(f, store); await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id });
        Assert.True(model.Loading.IsReady); Assert.True(model.CanSave); Assert.Single(model.Locations);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Single(await db.AssetLocations.ToListAsync(Ct)); Assert.Empty(await db.AssetEvents.ToListAsync(Ct));
        var id = model.Location?.Id; await model.ApplyQueryAsync(new Dictionary<string, object>()); Assert.Equal(id, model.Location?.Id);
    }

    [Fact]
    public async Task A_reentrant_departure_during_native_property_publication_keeps_the_form_covered_and_Save_closed()
    {
        using var f = new FlowFixture(); var (model, _, type, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        model.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(model.TypeName)) model.RetireReview(); };
        model.TypeName = "Before original publication";
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        Assert.False(model.CanSave); Assert.False(model.Loading.IsReady); Assert.False(model.CanReload);
    }

    [Fact]
    public async Task A_new_original_identity_after_a_known_financial_commit_has_its_own_blank_session_without_resubmitting_the_old_event()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        await model.SaveCommand.ExecuteAsync(null); Assert.False(model.CanSave);
        var next = new AssetType { Name = "Next item after accepted event", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(next, Ct); await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = next.Id, ["kind"] = "Opening" });
        Assert.True(model.CanSave); Assert.Empty(model.QuantityText); Assert.Empty(model.Note); Assert.Null(model.SavedLocationId);
        Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task An_old_accepted_financial_callback_cannot_close_or_reactivate_a_newly_routed_form()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var next = new AssetType { Name = "Replacement original form", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await store.SaveTypeAsync(next, Ct); Task? replacement = null;
        store.Changed += (_, _) => replacement = model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = next.Id, ["kind"] = "Opening" });
        await model.SaveCommand.ExecuteAsync(null); Assert.NotNull(replacement); await replacement;
        Assert.Empty(f.Platform.Routes); Assert.True(model.CanSave); Assert.Equal(next.Name, model.TypeName); Assert.Empty(model.QuantityText);
        Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
    }
}
