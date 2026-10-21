using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-437: complete original physical input, field validation and pending writer behaviour.</summary>
[Trait("AT", "AT-437")]
public sealed class PhysicalNumericLifecycleTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Partial_physical_edit_retains_original_format_until_complete_clearing(bool purity)
    {
        using var f = new FlowFixture(); var model = await PhysicalNumericContextTests.CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE");
        if (purity) model.PurityText = "18,5"; else model.UnitWeightText = "1,234";
        f.Localization.SetFormattingCulture("en-US");
        if (purity) model.PurityText = "18,6"; else model.UnitWeightText = "1,235";
        await model.SaveCommand.ExecuteAsync(null); Assert.True(model.Committed);
        var saved = Assert.Single(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
        if (purity) Assert.Equal(Purity.FromKarat(18.6m), saved.PurityPer10000); else Assert.Equal(1235, saved.UnitWeightMg);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task Complete_clearing_explicitly_adopts_the_new_physical_format()
    {
        using var f = new FlowFixture(); var model = await PhysicalNumericContextTests.CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); model.UnitWeightText = "1,234";
        f.Localization.SetFormattingCulture("en-US"); model.UnitWeightText = ""; model.UnitWeightText = "1.235";
        Assert.Null(model.UnitWeightFormatNote); await model.SaveCommand.ExecuteAsync(null);
        Assert.True(model.Committed); Assert.Equal(1235, Assert.Single(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct)).UnitWeightMg);
    }
    [Fact]
    public async Task All_physical_field_errors_are_visible_together_without_any_type_or_ledger_save()
    {
        using var f = new FlowFixture(); var model = await PhysicalNumericContextTests.CreateAsync(f);
        model.Name = ""; model.PurityText = "invalid purity"; model.UnitWeightText = "invalid grams";
        await model.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(model.NameError); Assert.NotNull(model.PurityError); Assert.NotNull(model.UnitWeightError);
        Assert.False(model.Committed); Assert.True(model.CanSave);
        Assert.Empty(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Theory]
    [InlineData("weight")]
    [InlineData("purity")]
    [InlineData("dimension")]
    [InlineData("kind")]
    [InlineData("purity-choice")]
    [InlineData("currency")]
    public async Task Changed_and_returned_physical_choice_retires_the_pending_original_writer(string field)
    {
        using var f = new FlowFixture(); var model = await PhysicalNumericContextTests.CreateAsync(f);
        model.PurityText = "18.5"; model.UnitWeightText = "1.234";
        f.Time.BeforeRead = () =>
        {
            f.Time.BeforeRead = null;
            switch (field)
            {
                case "weight": model.UnitWeightText = "2.345"; model.UnitWeightText = "1.234"; break;
                case "purity": model.PurityText = "18.6"; model.PurityText = "18.5"; break;
                case "dimension": model.DimensionIndex = (int)AssetDimension.Mass; model.DimensionIndex = (int)AssetDimension.Count; break;
                case "kind": model.KindIndex = (int)AssetKind.CountableItem; model.KindIndex = (int)AssetKind.CoinOrBar; break;
                case "purity-choice": model.PurityIndex = 0; model.PurityIndex = Purity.Common.Count; break;
                case "currency": model.CurrencyCode = "GBP"; model.CurrencyCode = "EUR"; break;
            }
        };
        await model.SaveCommand.ExecuteAsync(null); f.Time.BeforeRead = null;
        Assert.False(model.Committed); Assert.False(model.CanSave);
        Assert.Empty(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal("1.234", model.UnitWeightText); Assert.Equal("18.5", model.PurityText);
    }
    [Fact]
    public async Task Original_physical_notes_publish_after_display_change_without_replacing_raw_or_numeric_meaning()
    {
        using var f = new FlowFixture(); var model = await PhysicalNumericContextTests.CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); model.PurityText = "18,5"; model.UnitWeightText = "1,234";
        var properties = new List<string?>(); model.PropertyChanged += (_, args) => properties.Add(args.PropertyName);
        model.SetNumericPresentationActive(true); f.Localization.SetFormattingCulture("en-US"); model.SetNumericPresentationActive(false);
        Assert.Contains(nameof(model.PurityFormatNote), properties); Assert.Contains(nameof(model.UnitWeightFormatNote), properties);
        Assert.Contains("18,5", model.PurityFormatNote); Assert.Contains("1,234", model.UnitWeightFormatNote);
        Assert.Equal("18,5", model.PurityText); Assert.Equal("1,234", model.UnitWeightText);
    }
    [Fact]
    public async Task Monetary_display_unit_changes_never_scale_grams_or_purity()
    {
        var original = DisplayUnits.All.ToArray();
        try
        {
            using var f = new FlowFixture(); var model = await PhysicalNumericContextTests.CreateAsync(f);
            model.PurityText = "18.5"; model.UnitWeightText = "1.234";
            DisplayUnits.Set([new("EUR", "Fictitious hundred euro", 2)]);
            await model.SaveCommand.ExecuteAsync(null); Assert.True(model.Committed);
            var saved = Assert.Single(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
            Assert.Equal(1234, saved.UnitWeightMg); Assert.Equal(Purity.FromKarat(18.5m), saved.PurityPer10000);
        }
        finally { DisplayUnits.Set(original); }
    }
    [Fact]
    public async Task Returning_to_a_metal_kind_republishes_the_original_purity_guide_without_reentry()
    {
        using var f = new FlowFixture(); var model = await PhysicalNumericContextTests.CreateAsync(f);
        model.SetNumericPresentationActive(true);
        try
        {
            f.Localization.SetFormattingCulture("de-DE"); model.PurityText = "18,5";
            model.KindIndex = (int)AssetKind.CountableItem;
            string? displayed = null;
            model.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(model.PurityFormatNote)) displayed = model.PurityFormatNote; };
            f.Localization.SetFormattingCulture("en-US"); Assert.Null(displayed);
            model.KindIndex = (int)AssetKind.CoinOrBar;
            Assert.NotNull(displayed); Assert.Equal(model.PurityFormatNote, displayed); Assert.Equal("18,5", model.PurityText);
            Assert.Empty(await f.Services.GetRequiredService<HoldingStore>().GetTypesAsync(Ct));
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { model.SetNumericPresentationActive(false); }
    }

}
