using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-412: original holding input failures retain raw drafts, rows and correction authority.</summary>
[Trait("AT", "AT-412")]
public sealed class HoldingQuantityInputFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("79228162514264337593543950335")]
    [InlineData("9223372036854775")]
    public async Task Out_of_range_original_quantity_keeps_the_form_and_accepts_one_explicit_valid_correction(string raw)
    {
        using var f = new FlowFixture();
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var settings = await f.Store.GetSettingsAsync(Ct); settings.Mode = ExperienceMode.Advanced;
        await f.Store.SaveSettingsAsync(settings, Ct);
        var type = new AssetType { Name = "Original fictitious count", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await holdings.SaveTypeAsync(type, Ct); var place = await holdings.EnsureDefaultLocationAsync("Original place", Ct);
        var original = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Kind = AssetEventKind.Opening,
            Date = new(2026, 10, 1), Quantity = 1000, BasisAmount = 2500, Note = "Original retained basis" };
        Assert.True((await holdings.SaveEventAsync(original, [], Ct)).Succeeded);
        var vm = new AssetEventEditorViewModel(holdings, f.Store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)), f.Translator, f.Localization, f.Time, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        vm.Date = new(2026, 10, 20); vm.Note = "Original raw range draft";
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var error = Record.Exception(() => vm.QuantityText = raw);
        Assert.Null(error); Assert.Null(vm.EffectText); Assert.Equal(raw, vm.QuantityText);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.True(vm.CanSave); Assert.True(vm.CanEdit); Assert.True(vm.IsDirty);
        Assert.Equal(raw, vm.QuantityText); Assert.Equal("Original raw range draft", vm.Note);
        Assert.Equal(f.Translator["AssetEvent_QuantityTooLarge"], vm.Error);
        Assert.Empty(f.Platform.Routes); Assert.Empty(f.Platform.Failures);
        vm.QuantityText = "2"; await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.CanSave); Assert.False(vm.CanEdit); Assert.False(vm.IsDirty);
        Assert.Equal("..", Assert.Single(f.Platform.Routes).Route);
        var events = await holdings.GetEventsAsync(type.Id, Ct); Assert.Equal(2, events.Count);
        Assert.Equal(3000, HoldingsLedger.Quantity(events, type.Id, vm.Date));
        var kept = events.Single(e => e.Id == original.Id); Assert.Equal(1000, kept.Quantity); Assert.Equal(2500, kept.BasisAmount); Assert.Equal(original.Note, kept.Note);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
