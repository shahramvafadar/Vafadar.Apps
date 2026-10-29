using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-642: original complete type source, explicit reload and retained draft authority.</summary>
public sealed class AssetTypeEditorContinuationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static AssetTypeEditorViewModel Editor(FlowFixture f) => new(f.Services.GetRequiredService<HoldingStore>(),
        f.Store, f.Translator, f.Localization, f.Platform);
    private static async Task SqlAsync(FlowFixture f, string sql)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }
    private static async Task<(HoldingStore Store, AssetType Type, AssetTypeEditorViewModel Vm)> ExistingAsync(FlowFixture f)
    {
        var store = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "D642 original", Note = "D642 saved note", PriceCurrencyCode = "EUR",
            Kind = AssetKind.CoinOrBar, Dimension = AssetDimension.Count, Metal = Metal.Gold };
        await store.SaveTypeAsync(type, Ct); var vm = Editor(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = type.Id }); return (store, type, vm);
    }
    /// <summary>Original reader and revalidation never initialize a missing preference row.</summary>
    [Fact]
    public async Task Complete_new_source_and_validation_do_not_write_defaults()
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>();
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var source = await store.ReadTypeEditorSnapshotAsync(Guid.NewGuid(), false, ct: Ct);
        Assert.Null(source.StoredSettings); await store.ValidateTypeEditorSnapshotAsync(source, Ct);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Detached fallback mutation cannot silently change the original form currency.</summary>
    [Fact]
    public async Task Mutated_nonpersisted_default_is_refused_without_writing()
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<HoldingStore>();
        var source = await store.ReadTypeEditorSnapshotAsync(Guid.NewGuid(), false, ct: Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); source.Settings.DefaultCurrencyCode = "USD";
        Assert.Throws<HoldingEventReviewChangedException>(source.EnsureCurrent);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => store.ValidateTypeEditorSnapshotAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>The captured original file must be checked before a foreign source is published.</summary>
    [Fact]
    public async Task Foreign_snapshot_cannot_validate_in_another_profile()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture();
        var source = await f.Services.GetRequiredService<HoldingStore>().ReadTypeEditorSnapshotAsync(Guid.NewGuid(), false, ct: Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(other);
        await Assert.ThrowsAsync<HoldingEventReviewChangedException>(() => other.Services.GetRequiredService<HoldingStore>().ValidateTypeEditorSnapshotAsync(source, Ct));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(other));
    }
    /// <summary>A completed source failure stays covered through appearance until explicit Retry.</summary>
    [Fact]
    public async Task Initial_failure_requires_explicit_retry_without_appearance_replay()
    {
        using var f = new FlowFixture(); var vm = Editor(f);
        await SqlAsync(f, "ALTER TABLE Settings RENAME TO D642HeldSettings");
        await vm.ApplyQueryAsync(new Dictionary<string, object>());
        Assert.True(vm.IsCovered); Assert.False(vm.CanSave); Assert.True(vm.ShowRetry); Assert.False(vm.IsLoading);
        Assert.Equal(f.Translator["AssetType_LoadFailed"], vm.Error);
        await SqlAsync(f, "ALTER TABLE D642HeldSettings RENAME TO Settings");
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        vm.SetViewActive(false); vm.SetViewActive(true); await vm.EnsureLoadedAsync();
        Assert.True(vm.IsCovered); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.IsReady); Assert.True(vm.CanSave); Assert.Null(vm.Error);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Changed saved values require review while dirty original fields remain byte-for-byte raw.</summary>
    [Fact]
    public async Task Reload_merges_saved_values_and_dirty_draft_only_after_explicit_review()
    {
        using var f = new FlowFixture(); var (store, type, vm) = await ExistingAsync(f);
        vm.Name = "  D642 unsaved name  "; vm.Note = "  D642 unsaved note  ";
        type.Note = "D642 newer saved note"; type.CountUnitName = "D642 newer saved unit";
        await store.SaveTypeAsync(type, Ct); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.IsReady); Assert.True(vm.RequiresReview); Assert.False(vm.CanSave);
        Assert.Equal("  D642 unsaved name  ", vm.Name); Assert.Equal("  D642 unsaved note  ", vm.Note);
        Assert.Equal(type.CountUnitName, vm.CountUnitName); Assert.Contains(type.Note, vm.ReloadedSavedSummary!);
        vm.AcceptReviewCommand.Execute(null); Assert.True(vm.CanSave); Assert.False(vm.RequiresReview);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Failed explicit rereads keep raw numeric contexts for the next successful read.</summary>
    [Fact]
    public async Task Failed_retry_preserves_independent_purity_and_weight_parse_contexts()
    {
        using var f = new FlowFixture(); var (_, _, vm) = await ExistingAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); vm.PurityText = "18,5";
        f.Localization.SetFormattingCulture("en-US"); vm.UnitWeightText = "1.234";
        vm.Note = "  D642 raw note  ";
        await SqlAsync(f, "ALTER TABLE Settings RENAME TO D642HeldSettings");
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.IsCovered); Assert.Equal("18,5", vm.PurityText);
        await SqlAsync(f, "ALTER TABLE D642HeldSettings RENAME TO Settings");
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.IsReady); Assert.Equal("18,5", vm.PurityText); Assert.Equal("1.234", vm.UnitWeightText);
        Assert.Equal("  D642 raw note  ", vm.Note); Assert.Contains("18,5", vm.PurityFormatNote!); Assert.Null(vm.UnitWeightFormatNote);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>A replaced native root cannot receive the old discard answer.</summary>
    [Fact]
    public async Task Replaced_root_refuses_discard_and_save_without_mutation()
    {
        using var f = new FlowFixture(); var (_, _, vm) = await ExistingAsync(f); var owns = true;
        vm.BindRootOwner(() => owns); vm.Note = "D642 unsaved root note";
        f.Platform.PendingConfirmation = () => { owns = false; return Task.FromResult(true); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.CancelCommand.ExecuteAsync(null);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes); Assert.False(vm.Committed);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Bad requested edit identities must never fall through to a new asset type.</summary>
    [Theory]
    [InlineData("not-an-id")]
    [InlineData("")]
    public async Task Invalid_requested_edit_is_covered_and_never_creates(string id)
    {
        using var f = new FlowFixture(); var vm = Editor(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = id });
        vm.Name = "D642 forbidden new"; await vm.EnsureLoadedAsync(); await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.IsCovered); Assert.False(vm.CanSave); Assert.False(vm.ShowRetry); Assert.False(vm.Committed);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Language changes update choice captions without changing selected physical identities or raw values.</summary>
    [Fact]
    public async Task Live_language_refresh_preserves_choices_and_raw_contexts()
    {
        using var f = new FlowFixture(); var (_, _, vm) = await ExistingAsync(f);
        vm.SetNumericPresentationActive(true); vm.MetalIndex = 2; vm.PurityIndex = Purity.Common.Count;
        f.Localization.SetFormattingCulture("de-DE"); vm.PurityText = "18,5"; vm.UnitWeightText = "1,234";
        var caption = vm.KindNames[vm.KindIndex]; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(x => x.CultureName == "de"));
        Assert.NotEqual(caption, vm.KindNames[vm.KindIndex]); Assert.Equal((int)AssetKind.CoinOrBar, vm.KindIndex);
        Assert.Equal((int)AssetDimension.Count, vm.DimensionIndex); Assert.Equal(2, vm.MetalIndex);
        Assert.Equal("18,5", vm.PurityText); Assert.Equal("1,234", vm.UnitWeightText); Assert.True(vm.CanSave);
        vm.SetNumericPresentationActive(false); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Stored settings changed after the read invalidate the same actual writer transaction.</summary>
    [Fact]
    public async Task Changed_stored_preferences_refuse_write_and_keep_raw_draft()
    {
        using var f = new FlowFixture(); var (_, _, vm) = await ExistingAsync(f);
        vm.Name = "D642 forbidden stale save";
        await f.Store.UpdateSettingsAsync(settings => settings.DefaultCurrencyCode = "USD", Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.Committed); Assert.True(vm.IsCovered); Assert.Equal("D642 forbidden stale save", vm.Name);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Partial saved-value publication cannot replace raw fields on a later same-form Retry.</summary>
    [Fact]
    public async Task Partial_reload_departure_retains_complete_original_dirty_draft()
    {
        using var f = new FlowFixture(); var (store, type, vm) = await ExistingAsync(f);
        vm.Name = "D642 retained raw name"; vm.Note = "D642 retained raw note";
        type.Name = "D642 newer saved name"; await store.SaveTypeAsync(type, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        void Depart(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        { if (args.PropertyName == nameof(vm.Name) && vm.Name == type.Name) vm.RetireReview(); }
        vm.PropertyChanged += Depart; await vm.ReloadCommand.ExecuteAsync(null); vm.PropertyChanged -= Depart;
        Assert.False(vm.CanEdit); vm.SetViewActive(true); await vm.EnsureLoadedAsync();
        Assert.True(vm.IsReady); Assert.Equal("D642 retained raw name", vm.Name); Assert.Equal("D642 retained raw note", vm.Note);
        Assert.True(vm.RequiresReview); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Acceptance remains known if a post-commit notification retires its actual root.</summary>
    [Fact]
    public async Task Accepted_write_under_retired_root_never_navigates_or_submits_again()
    {
        using var f = new FlowFixture(); var (store, type, vm) = await ExistingAsync(f); var owns = true;
        vm.BindRootOwner(() => owns); vm.Name = "D642 one accepted name";
        store.Changed += (_, _) => owns = false;
        await vm.SaveCommand.ExecuteAsync(null); Assert.True(vm.Committed); Assert.Empty(f.Platform.Routes);
        Assert.Equal(vm.Name, (await store.GetTypesAsync(Ct)).Single(x => x.Id == type.Id).Name);
        var accepted = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(accepted, await GoalChooserFlowTests.SnapshotAsync(f));
    }

}
