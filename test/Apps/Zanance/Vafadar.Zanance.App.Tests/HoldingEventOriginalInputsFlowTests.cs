using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-421: actual-file validation, required default policy and original draft choices.</summary>
[Trait("AT", "AT-421")]
public sealed class HoldingEventOriginalInputsFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static AssetEventEditorViewModel Model(FlowFixture f, HoldingStore store) => new(store, f.Store,
        new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);

    [Fact]
    public async Task Original_file_switch_between_read_and_validation_refuses_publication_without_writing_to_the_next_file()
    {
        using var f = new FlowFixture(); using var next = new FlowFixture(); var (model, _, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        await next.Services.GetRequiredService<HoldingStore>().SaveTypeAsync(new AssetType(type.Id) { Name = type.Name, Kind = type.Kind, Dimension = type.Dimension, PriceCurrencyCode = type.PriceCurrencyCode }, Ct);
        var calls = 0; factory.Before = _ => { if (++calls == 2) factory.Current = next.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); return Task.CompletedTask; };
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanSave); Assert.Equal("2", model.QuantityText);
        Assert.Empty(await next.Services.GetRequiredService<HoldingStore>().GetLocationsAsync(Ct));
    }

    [Fact]
    public async Task A_known_default_place_with_failed_readback_is_reused_by_Retry_without_repeating_metadata()
    {
        using var f = new FlowFixture(); var initial = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original default recovery", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await initial.SaveTypeAsync(type, Ct);
        var factory = new HoldingLocationCreationFlowTests.Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()); var model = Model(f, store);
        var notifications = 0; store.Changed += (_, _) => { notifications++; factory.Before = _ => throw new IOException("Original default readback refused"); };
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id }); Assert.True(model.Loading.HasFailed);
        factory.Before = null; var accepted = JsonSerializer.Serialize(await store.GetLocationsAsync(Ct)); await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.Loading.IsReady); Assert.Equal(1, notifications); Assert.Equal(accepted, JsonSerializer.Serialize(await store.GetLocationsAsync(Ct)));
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_retired_default_prompt_owner_rolls_back_before_commit_and_cannot_enable_Retry()
    {
        using var f = new FlowFixture(); var initial = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original retired default", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await initial.SaveTypeAsync(type, Ct);
        var factory = new HoldingLocationCreationFlowTests.Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()); var model = Model(f, store); var calls = 0;
        factory.Before = _ => { if (++calls == 2) model.RetireReview(); return Task.CompletedTask; };
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id }); factory.Before = null;
        Assert.Empty(await store.GetLocationsAsync(Ct)); Assert.False(model.CanSave); Assert.False(model.CanReload);
    }

    [Fact]
    public async Task A_new_query_finishes_before_an_old_delayed_read_without_inheriting_its_fields_or_authority()
    {
        using var f = new FlowFixture(); var (model, store, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var next = new AssetType { Name = "Original winning query", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "USD" };
        await store.SaveTypeAsync(next, Ct);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0; factory.Before = async _ => { if (Interlocked.Increment(ref calls) == 1) { entered.SetResult(); await release.Task; } };
        var old = model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" }); await entered.Task.WaitAsync(Ct);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = next.Id, ["kind"] = "Opening" }); release.SetResult(); await old;
        Assert.True(model.Loading.IsReady); Assert.True(model.CanSave); Assert.Equal(next.Name, model.TypeName); Assert.Empty(model.QuantityText); Assert.Empty(model.Note);
        Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Free_corrections_can_prepare_the_missing_default_but_cannot_create_an_extra_managed_place()
    {
        using var f = new FlowFixture(); var initial = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Retained Free holding", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await initial.SaveTypeAsync(type, Ct);
        var source = new Access(new CommercialWriteAccess(f.Directory.Combine("zanance.db"), new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid()))));
        var store = new HoldingStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), source); var model = Model(f, store);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id });
        Assert.True(model.Loading.IsReady); Assert.True(model.CanSave); var original = Assert.Single(await store.GetLocationsAsync(Ct));
        f.Platform.Inputs.Enqueue("Denied extra managed place"); await model.AddLocationCommand.ExecuteAsync(null);
        Assert.Equal(original.Id, Assert.Single(await store.GetLocationsAsync(Ct)).Id); Assert.Null(model.SavedLocationId); Assert.NotNull(model.LocationError);
        var review = await store.ReadEventReviewAsync(type.Id, ct: Ct);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.CreateReviewedDefaultLocationAsync(review, Guid.NewGuid(), "Not missing", () => true, Ct));
    }

    [Fact]
    public async Task Historical_edit_keeps_its_archived_original_place_instead_of_silently_selecting_the_default()
    {
        using var f = new FlowFixture(); var (model, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        await model.SaveCommand.ExecuteAsync(null); var original = Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
        var place = Assert.Single(await store.GetLocationsAsync(Ct)); place.IsArchived = true; await store.SaveLocationAsync(place, Ct);
        var editor = Model(f, store); await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = original.Id });
        Assert.True(editor.Loading.IsReady); Assert.Equal(place.Id, editor.Location?.Id); Assert.Equal("2", editor.QuantityText); Assert.Equal("50.00", editor.BasisText);
        Assert.Contains(editor.Locations, l => l.Id == place.Id); Assert.Equal(2, (await store.GetLocationsAsync(Ct)).Count);
        Assert.Single(await store.GetEventsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Retry_never_reinterprets_an_unavailable_original_kind_or_creates_a_replacement_for_its_archived_place(bool archivedPlace)
    {
        using var f = new FlowFixture(); var (model, store, type, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        if (archivedPlace)
        {
            var place = Assert.Single(await store.GetLocationsAsync(Ct)); place.IsArchived = true; await store.SaveLocationAsync(place, Ct);
        }
        else await f.Store.UpdateSettingsAsync(settings => settings.Mode = ExperienceMode.Simple, Ct);
        var before = JsonSerializer.Serialize(await store.GetLocationsAsync(Ct));
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanSave); Assert.Equal("2", model.QuantityText); Assert.Equal("50", model.BasisText);
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetLocationsAsync(Ct))); Assert.Empty(await store.GetEventsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_removed_known_default_after_failed_readback_is_never_recreated_by_Retry()
    {
        using var f = new FlowFixture(); var initial = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original removed default", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await initial.SaveTypeAsync(type, Ct);
        var factory = new HoldingLocationCreationFlowTests.Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new HoldingStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()); var model = Model(f, store); var notifications = 0;
        store.Changed += (_, _) => { notifications++; factory.Before = _ => throw new IOException("Original known default readback failed"); };
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id }); Assert.True(model.Loading.HasFailed); factory.Before = null;
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.AssetLocations.ExecuteDeleteAsync(Ct);
        await model.ReloadCommand.ExecuteAsync(null); factory.Before = null;
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanSave); Assert.Equal(1, notifications); Assert.Empty(await store.GetLocationsAsync(Ct));
    }

    private sealed class Access(CommercialWriteAccess access) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Capture(string databasePath) => access;
    }
}
