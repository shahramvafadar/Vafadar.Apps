using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-437: physical values retain their independently original numeric interpretation.</summary>
[Trait("AT", "AT-437")]
public sealed class PhysicalNumericContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static async Task<AssetTypeEditorViewModel> CreateAsync(FlowFixture f)
    {
        var model = new AssetTypeEditorViewModel(f.Services.GetRequiredService<HoldingStore>(), f.Store,
            f.Translator, f.Localization, f.Platform);
        await model.ApplyQueryAsync(new Dictionary<string, object>());
        model.Name = "Fictitious original numeric type";
        model.KindIndex = (int)AssetKind.CoinOrBar;
        model.DimensionIndex = (int)AssetDimension.Count;
        model.PurityIndex = Purity.Common.Count;
        return model;
    }

    [Fact]
    public async Task Original_per_item_gram_weight_survives_a_later_numeric_culture()
    {
        using var f = new FlowFixture(); var model = await CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); model.UnitWeightText = "1,234";
        f.Localization.SetFormattingCulture("en-US"); await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.Committed);
        var saved = Assert.Single(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
        Assert.Equal(1234, saved.UnitWeightMg); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Original_free_purity_survives_a_later_numeric_culture()
    {
        using var f = new FlowFixture(); var model = await CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); model.PurityText = "18,5";
        f.Localization.SetFormattingCulture("en-US"); await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.Committed);
        var saved = Assert.Single(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
        Assert.Equal(Purity.FromKarat(18.5m), saved.PurityPer10000);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Purity_and_weight_keep_their_own_separate_original_formats()
    {
        using var f = new FlowFixture(); var model = await CreateAsync(f);
        f.Localization.SetFormattingCulture("en-US"); model.PurityText = "18.5";
        f.Localization.SetFormattingCulture("de-DE"); model.UnitWeightText = "1,234";
        f.Localization.SetFormattingCulture("en-US"); await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.Committed);
        var saved = Assert.Single(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
        Assert.Equal(Purity.FromKarat(18.5m), saved.PurityPer10000); Assert.Equal(1234, saved.UnitWeightMg);
    }
}
