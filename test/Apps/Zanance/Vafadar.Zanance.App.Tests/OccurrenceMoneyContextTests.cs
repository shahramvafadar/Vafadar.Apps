using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-436: actual original occurrence inputs, without native list/control replicas.</summary>
[Trait("AT", "AT-436")]
[Collection("Entry validation units")]
public sealed class OccurrenceMoneyContextTests
{
    [Theory, InlineData(false), InlineData(true)]
    public void Original_occurrence_payment_and_override_refuse_a_later_display_unit(bool payment)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var inputs = new OccurrenceMoneyInputs(f.Localization); inputs.Initialize(1000, null);
            if (payment) inputs.ChangePayment("12.34"); else inputs.ChangeOverride("12.34");
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); var result = inputs.Validate(payment);
            Assert.Null(result.Amount); Assert.NotNull(result.ErrorKey);
            Assert.Equal("12.34", payment ? inputs.Payment.Text : inputs.Override.Text);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Theory, InlineData(false), InlineData(true)]
    public void Display_only_format_changes_retain_each_original_occurrence_parser(bool payment)
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture("de-DE");
        var inputs = new OccurrenceMoneyInputs(f.Localization); inputs.Initialize(1000, null);
        if (payment) inputs.ChangePayment("12,34"); else inputs.ChangeOverride("12,34");
        f.Localization.SetFormattingCulture("en-US");
        Assert.Equal(1234, inputs.Validate(payment).Amount);
        Assert.Equal("12,34", payment ? inputs.Payment.Text : inputs.Override.Text);
    }

    [Theory, InlineData(false, false), InlineData(true, false), InlineData(false, true), InlineData(true, true)]
    public void A_changed_currency_or_unit_requires_complete_original_occurrence_reentry(bool payment, bool currencyChanged)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var inputs = new OccurrenceMoneyInputs(f.Localization); inputs.Initialize(1000, null);
            if (payment) inputs.ChangePayment("12.34"); else inputs.ChangeOverride("12.34");
            if (currencyChanged) inputs.SelectCurrency("USD"); else DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            if (payment) inputs.ChangePayment("12.35"); else inputs.ChangeOverride("12.35");
            Assert.Equal("Plan_InputUnitChanged", inputs.Validate(payment).ErrorKey);
            if (payment) { inputs.ChangePayment(""); inputs.ChangePayment("12.35"); }
            else { inputs.ChangeOverride(""); inputs.ChangeOverride("12.35"); }
            Assert.Null(inputs.Validate(payment).ErrorKey); Assert.Equal(currencyChanged ? 1235 : 123500, inputs.Validate(payment).Amount);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public void Optional_blank_override_keeps_the_plan_even_when_the_other_payment_needs_unit_review()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var inputs = new OccurrenceMoneyInputs(f.Localization);
            inputs.Initialize(1000, null); DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            var replacement = inputs.Validate(false); Assert.Null(replacement.Amount); Assert.Null(replacement.ErrorKey);
            Assert.Null(inputs.Validate(true).Amount); Assert.Equal("Plan_InputUnitChanged", inputs.Validate(true).ErrorKey);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public void One_occurrence_field_cannot_renew_the_other_fields_original_unit()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var inputs = new OccurrenceMoneyInputs(f.Localization); inputs.Initialize(1000, 1200);
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); inputs.ChangePayment(""); inputs.ChangePayment("0.10");
            Assert.Equal(1000, inputs.Validate(true).Amount); Assert.Null(inputs.Validate(true).ErrorKey);
            Assert.Equal("Plan_InputUnitChanged", inputs.Validate(false).ErrorKey); Assert.Equal("12.00", inputs.Override.Text);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public void Canonical_occurrence_initialization_formats_and_parses_the_same_authoritative_unit()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var inputs = new OccurrenceMoneyInputs(f.Localization); inputs.Initialize(123400, 125000);
            Assert.Equal("12.34", inputs.Payment.Text); Assert.Equal("12.5", inputs.Override.Text);
            Assert.Equal(123400, inputs.Validate(true).Amount); Assert.Equal(125000, inputs.Validate(false).Amount);
        }
        finally { DisplayUnits.Set(units); }
    }
}
