using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-642: original asset-type publication, native consent and accepted raw drafts keep their original owner.</summary>
public sealed class AssetTypeEditorOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<(HoldingStore Store, AssetType Type, AssetTypeEditorViewModel Editor)> OriginalAsync(FlowFixture f)
    {
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "D642 original type", Note = "D642 original note", PriceCurrencyCode = "USD",
            Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, SortOrder = 23, Icon = "Diamond" };
        await store.SaveTypeAsync(type, Ct);
        return (store, type, new(store, f.Store, f.Translator, f.Localization, f.Platform));
    }

    /// <summary>Reentrant native departure after the original name must stop later currency/note and editable publication.</summary>
    [Fact]
    public async Task Retirement_during_original_type_name_never_publishes_later_fields_or_edit_authority()
    {
        using var f = new FlowFixture(); var (_, type, vm) = await OriginalAsync(f);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.Name)) vm.RetireReview(); };
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = type.Id });
        Assert.False(vm.CanEdit); Assert.NotEqual("USD", vm.CurrencyCode); Assert.Empty(vm.Note);
    }

    /// <summary>A native discard answer cannot close a newer original type draft.</summary>
    [Fact]
    public async Task Late_original_discard_response_never_closes_newer_asset_type_text()
    {
        using var f = new FlowFixture(); var (_, type, vm) = await OriginalAsync(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = type.Id }); vm.Note = "D642 first unsaved note";
        f.Platform.PendingConfirmation = () => { vm.Note = "D642 later unsaved note"; return Task.FromResult(true); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal("D642 later unsaved note", vm.Note); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Actual accepted type metadata cannot erase text delivered by its following notification.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Accepted_type_preserves_later_unsaved_note_and_its_own_discard(bool notificationFails)
    {
        using var f = new FlowFixture(); var (store, type, vm) = await OriginalAsync(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = type.Id }); vm.Name = "D642 accepted original name";
        store.Changed += (_, _) => { vm.Note = "D642 note delivered after acceptance"; if (notificationFails) throw new IOException("D642 accepted publication failure."); };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.Committed, vm.Error); Assert.False(vm.CanSave);
        var saved = (await store.GetTypesAsync(Ct)).Single(row => row.Id == type.Id);
        Assert.Equal("D642 accepted original name", saved.Name); Assert.Equal(type.Note, saved.Note);
        Assert.Equal("D642 note delivered after acceptance", vm.Note);
        Assert.True(vm.IsDirty, "The accepted original metadata erased the newer raw draft's dirty state.");
        Assert.Empty(f.Platform.Routes);
    }
}
