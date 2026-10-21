using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-416: original type editor identity, review, raw draft and accepted result.</summary>
[Trait("AT", "AT-416")]
public sealed class HoldingTypeFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static AssetTypeEditorViewModel Editor(FlowFixture f) => new(f.Services.GetRequiredService<HoldingStore>(),
        f.Store, f.Translator, f.Localization, f.Platform);
    private static async Task<(HoldingStore Store, AssetType Type, AssetTypeEditorViewModel Editor)> ExistingAsync(FlowFixture f, bool history = false)
    {
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original", Note = "Original note", PriceCurrencyCode = "EUR",
            Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, SortOrder = 23, Icon = "Diamond" };
        await store.SaveTypeAsync(type, Ct);
        if (history)
        {
            var place = await store.EnsureDefaultLocationAsync("Original place", Ct);
            Assert.True((await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
                Date = new(2026, 10, 1), Kind = AssetEventKind.Opening, Quantity = 2000, BasisAmount = 5000 }, [], Ct)).Succeeded);
        }
        var editor = Editor(f); await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = type.Id });
        return (store, type, editor);
    }
    [Theory]
    [InlineData("note")]
    [InlineData("archive")]
    [InlineData("history")]
    [InlineData("price")]
    public async Task Original_edit_must_not_replace_concurrent_facts(string change)
    {
        using var f = new FlowFixture(); var (store, type, editor) = await ExistingAsync(f);
        editor.Name = "Reviewed rename"; editor.Note = "Raw user note";
        if (change == "note") { type.Note = "Concurrent note"; await store.SaveTypeAsync(type, Ct); }
        if (change == "archive") { type.IsArchived = true; await store.SaveTypeAsync(type, Ct); }
        if (change == "history")
        {
            var place = await store.EnsureDefaultLocationAsync("Original place", Ct);
            await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
                Date = new(2026, 10, 1), Kind = AssetEventKind.Opening, Quantity = 1000 }, [], Ct);
        }
        if (change == "price") await store.SaveValuationAsync(new AssetValuation { AssetTypeId = type.Id,
            Date = new(2026, 10, 2), PricePerUnitMilli = 3000000, CurrencyCode = "EUR" }, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.Committed); Assert.False(editor.CanSave);
        Assert.Equal("Reviewed rename", editor.Name); Assert.Equal("Raw user note", editor.Note); Assert.True(editor.IsDirty);
        Assert.Equal(f.Translator["AssetType_SourceChanged"], editor.Error);
        await editor.ApplyQueryAsync(new Dictionary<string, object>()); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal("Raw user note", editor.Note);
    }
    [Fact]
    public async Task New_type_retry_after_known_notification_must_not_duplicate()
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>(); var editor = Editor(f);
        await editor.ApplyQueryAsync(new Dictionary<string, object>()); editor.Name = "Original new";
        void Fail(object? sender, EventArgs args) => throw new IOException("Original post-commit notification");
        store.Changed += Fail; await editor.SaveCommand.ExecuteAsync(null); store.Changed -= Fail;
        Assert.True(editor.Committed); Assert.False(editor.CanSave); Assert.False(editor.IsDirty);
        Assert.Equal(f.Translator["AssetType_SavedNeedsReview"], editor.Error); Assert.Equal(f.Translator["Common_Close"], editor.CloseText);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.ApplyQueryAsync(new Dictionary<string, object>()); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Single(await store.GetTypesAsync(Ct)); Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(editor.AcceptedId, (await store.GetTypesAsync(Ct)).Single().Id); Assert.Empty(f.Platform.Routes);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Known_return_failure_keeps_the_original_saved_identity_closed_to_resubmission(bool existing)
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>(); AssetTypeEditorViewModel editor;
        if (existing) (_, _, editor) = await ExistingAsync(f); else { editor = Editor(f); await editor.ApplyQueryAsync(new Dictionary<string, object>()); }
        editor.Name = "Original accepted rename"; f.Platform.PendingNavigation = () => throw new IOException("Original return");
        await editor.SaveCommand.ExecuteAsync(null); Assert.True(editor.Committed); Assert.False(editor.CanSave); Assert.False(editor.CanEdit);
        Assert.Equal(f.Translator["AssetType_SavedNeedsReview"], editor.Error); Assert.False(editor.IsDirty);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Single(await store.GetTypesAsync(Ct));
        Assert.Single(f.Platform.Routes);
        if (!existing) Assert.Equal("Opening", f.Platform.Routes.Single().Parameters!["kind"]);
    }
    [Fact]
    public async Task Dimension_refusal_does_not_mutate_original_metadata_and_same_form_can_correct_it()
    {
        using var f = new FlowFixture(); var (store, type, editor) = await ExistingAsync(f, history: true);
        Assert.True(editor.DimensionLocked); editor.DimensionIndex = (int)AssetDimension.Mass; editor.Name = "Retained rename";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(editor.CanSave);
        Assert.Equal(f.Translator["AssetType_DimensionLocked"], editor.Error);
        editor.DimensionIndex = (int)AssetDimension.Count; await editor.SaveCommand.ExecuteAsync(null);
        Assert.True(editor.Committed); var saved = (await store.GetTypesAsync(Ct)).Single();
        Assert.Equal(type.Id, saved.Id); Assert.Equal("Retained rename", saved.Name); Assert.Equal(23, saved.SortOrder); Assert.Equal(type.Icon, saved.Icon);
        Assert.Single(await store.GetEventsAsync(type.Id, Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pending_departure_or_changed_raw_draft_rolls_back_original_save(bool raw)
    {
        using var f = new FlowFixture(); var (_, _, editor) = await ExistingAsync(f);
        editor.Name = "Pending rename"; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.BeforeRead = raw ? () => editor.Note = "Changed while pending" : editor.RetireReview;
        await editor.SaveCommand.ExecuteAsync(null); f.Time.BeforeRead = null;
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.False(editor.Committed); Assert.False(editor.CanSave);
        Assert.Equal("Pending rename", editor.Name); if (raw) Assert.Equal("Changed while pending", editor.Note);
    }
    [Fact]
    public async Task Failed_missing_existing_load_never_becomes_a_new_type_on_empty_redelivery()
    {
        using var f = new FlowFixture(); var editor = Editor(f);
        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() });
        await editor.ApplyQueryAsync(new Dictionary<string, object>()); editor.Name = "Do not create";
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.False(editor.CanSave); Assert.False(editor.Committed); Assert.NotNull(editor.Error);
        Assert.Empty(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
    }
    [Fact]
    public async Task Original_sql_failure_keeps_the_same_new_identity_available_for_explicit_retry()
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>(); var editor = Editor(f);
        await editor.ApplyQueryAsync(new Dictionary<string, object>()); editor.Name = "Retained new draft";
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RefuseType BEFORE INSERT ON AssetTypes BEGIN SELECT RAISE(ABORT, 'Original type persistence refusal'); END;", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(editor.CanSave); Assert.False(editor.Committed);
        Assert.Equal("Retained new draft", editor.Name);
        await using (var db = await factory.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RefuseType;", Ct);
        await editor.SaveCommand.ExecuteAsync(null); Assert.True(editor.Committed); Assert.Single(await store.GetTypesAsync(Ct));
    }
    [Fact]
    public async Task Foreign_mutated_consumed_or_occupied_original_review_cannot_save()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>();
        var foreign = other.Services.GetRequiredService<HoldingStore>(); var type = new AssetType { Name = "Original stable new", PriceCurrencyCode = "EUR" };
        var review = await store.ReadTypeReviewAsync(type.Id, false, Ct);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => foreign.SaveReviewedTypeAsync(review, type, () => true, Ct));
        await store.SaveTypeAsync(type, Ct);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.SaveReviewedTypeAsync(review, type, () => true, Ct));
        review = await store.ReadTypeReviewAsync(type.Id, true, Ct); review.Inputs.Type!.Name = "Mutated original";
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.SaveReviewedTypeAsync(review, type, () => true, Ct));
        review = await store.ReadTypeReviewAsync(type.Id, true, Ct); type.Name = "Accepted original";
        Assert.True(await store.SaveReviewedTypeAsync(review, type, () => true, Ct));
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.SaveReviewedTypeAsync(review, type, () => true, Ct));
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Fact]
    public async Task Dirty_cancel_decline_keeps_raw_then_explicit_discard_leaves_without_writing()
    {
        using var f = new FlowFixture(); var editor = Editor(f); await editor.ApplyQueryAsync(new Dictionary<string, object>());
        editor.Name = "Unsaved original"; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await editor.CancelCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes); Assert.True(editor.IsDirty);
        Assert.Equal("Unsaved original", editor.Name); f.Platform.Confirmations.Enqueue(true); await editor.CancelCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.Routes); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_full_type_draft_preserves_measurement_metadata_and_saved_identity(bool existing)
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>(); AssetTypeEditorViewModel editor;
        if (existing) (_, _, editor) = await ExistingAsync(f); else { editor = Editor(f); await editor.ApplyQueryAsync(new Dictionary<string, object>()); }
        editor.Name = "Original full draft"; editor.KindIndex = (int)AssetKind.CoinOrBar; editor.DimensionIndex = (int)AssetDimension.Count;
        editor.MetalIndex = 0; editor.PurityIndex = Purity.Common.Count; editor.PurityText = "18";
        editor.UnitWeightText = "8.133"; editor.CountUnitName = "Original coin"; editor.Divisible = true; editor.CurrencyCode = "USD";
        editor.Note = "Original complete note"; await editor.SaveCommand.ExecuteAsync(null);
        Assert.True(editor.Committed); var saved = (await store.GetTypesAsync(Ct)).Single();
        Assert.Equal(editor.AcceptedId, saved.Id); Assert.Equal(8133, saved.UnitWeightMg); Assert.Equal(7500, saved.PurityPer10000);
        Assert.Equal(Metal.Gold, saved.Metal); Assert.Equal(AssetDimension.Count, saved.Dimension); Assert.True(saved.Divisible);
        Assert.Equal("USD", saved.PriceCurrencyCode); Assert.Equal("Original coin", saved.CountUnitName); Assert.Equal(editor.Note, saved.Note);
        Assert.Equal(existing ? 23 : 0, saved.SortOrder); Assert.Equal(existing ? "Diamond" : null, saved.Icon);
        Assert.False(saved.IsArchived); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.False(editor.IsDirty);
    }
    [Theory]
    [InlineData("name")]
    [InlineData("purity")]
    [InlineData("weight")]
    public async Task Invalid_original_field_keeps_its_raw_input_for_same_form_correction(string field)
    {
        using var f = new FlowFixture(); var editor = Editor(f); await editor.ApplyQueryAsync(new Dictionary<string, object>());
        editor.Name = "Original validation"; editor.KindIndex = (int)AssetKind.CoinOrBar;
        editor.DimensionIndex = (int)AssetDimension.Count; editor.PurityIndex = Purity.Common.Count;
        if (field == "name") editor.Name = "  ";
        if (field == "purity") editor.PurityText = "invalid purity";
        if (field == "weight") editor.UnitWeightText = "invalid weight";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.True(editor.CanSave);
        var visibleError = field switch { "name" => editor.NameError, "purity" => editor.PurityError, _ => editor.UnitWeightError };
        var message = field switch { "name" => "AssetType_NameRequired", "purity" => "AssetType_PurityInvalid", _ => "AssetType_WeightInvalid" };
        Assert.Equal(f.Translator[message], visibleError);
        if (field == "purity") Assert.Equal("invalid purity", editor.PurityText);
        if (field == "weight") Assert.Equal("invalid weight", editor.UnitWeightText);
        editor.Name = "Corrected original"; editor.PurityText = "18"; editor.UnitWeightText = "8.133";
        await editor.SaveCommand.ExecuteAsync(null); Assert.True(editor.Committed); Assert.False(editor.CanSave);
    }
}
