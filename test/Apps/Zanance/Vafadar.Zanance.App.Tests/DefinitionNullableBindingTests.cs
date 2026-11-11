using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Empty native binding values must remain raw drafts without bypassing required-text validation.</summary>
[Collection("Entry validation units")]
public sealed class DefinitionNullableBindingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Clearing a unit name cannot throw during preview refresh or a subsequent source read.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public async Task Empty_unit_binding_keeps_preview_empty_and_source_unchanged(string? raw)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]);
            using var f = new FlowFixture();
            await f.Store.GetSettingsAsync(Ct);
            var model = new DisplayUnitsViewModel(f.Store, f.Translator, f.Localization, f.Platform);
            try
            {
                await model.LoadAsync();
                model.CurrencyCode = "EUR"; model.FactorIndex = 2; model.Name = "Draft units";
                Assert.NotNull(model.PreviewText);
                var before = await GoalChooserFlowTests.SnapshotAsync(f);
                model.Name = raw!;
                Assert.Equal(raw, model.Name); Assert.Null(model.PreviewText); Assert.False(model.CanAdd);
                model.CurrencyCode = "USD"; model.FactorIndex = 1;
                await model.LoadAsync();
                Assert.Equal(raw, model.Name); Assert.Null(model.PreviewText); Assert.False(model.CanAdd);
                Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
                Assert.Empty(f.Platform.ConfirmationDialogs);
            }
            finally { model.Deactivate(); }
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>A null native rule draft follows the same required-text refusal as empty and whitespace values.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public async Task Empty_rule_binding_reports_required_text_without_writing(string? raw)
    {
        using var f = new FlowFixture();
        await f.Store.GetSettingsAsync(Ct);
        var model = new RulesViewModel(f.Store, f.Translator, f.Localization, f.Platform);
        model.SetActive(true);
        try
        {
            await model.LoadAsync(); Assert.True(model.CanInteract);
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            model.MatchText = raw!;
            await model.AddCommand.ExecuteAsync(null);
            Assert.Equal(raw, model.MatchText); Assert.Equal(f.Translator["Rule_TextRequired"], model.Error);
            Assert.False(model.Accepted); Assert.True(model.CanInteract);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Empty(f.Platform.ConfirmationDialogs);
        }
        finally { model.SetActive(false); }
    }
}
