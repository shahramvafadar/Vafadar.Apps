using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-436: actual cost/share monetary input source, without fabricating protected household authority.</summary>
[Trait("AT", "AT-436")]
[Collection("Entry validation units")]
public sealed class HouseholdMoneyContextTests
{
    private static HouseholdCostsViewModel Create(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort environment) => new(
        f.Services.GetRequiredService<HouseholdCostStore>(), environment, f.Platform, f.Localization,
        new DateFormatter(f.Localization), f.Translator, f.Time);

    [Fact]
    public void Refused_household_amount_publishes_its_field_local_unit_error_without_a_translation_change()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
            f.Localization.SetFormattingCulture("en-US"); var vm = Create(f, environment); vm.AmountText = "12.34";
            var published = false;
            vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.AmountContextError)) published = true; };
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            Assert.False(vm.TryReadOriginalAmount(out _));
            Assert.NotNull(vm.AmountContextError); Assert.True(published);
            Assert.Equal("12.34", vm.AmountText); Assert.False(vm.CanWrite);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public void Original_household_amount_cannot_borrow_a_later_display_unit()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
            f.Localization.SetFormattingCulture("en-US"); var vm = Create(f, environment); vm.AmountText = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            Assert.False(vm.TryReadOriginalAmount(out _)); Assert.Equal("12.34", vm.AmountText);
            Assert.Equal("EUR", vm.AmountUnitName); Assert.NotNull(vm.AmountContextError); Assert.False(vm.CanWrite);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public void Original_household_share_cannot_borrow_a_later_display_unit()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
            var row = new HouseholdShareInput(new() { Name = "Original participant" }, f.Localization.CurrentCulture,
                "EUR", () => f.Localization.CurrentCulture, f.Translator) { Text = "12.34" };
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); row.RefreshMoneyTexts();
            Assert.False(row.TryRead("EUR", out _)); Assert.Equal("12.34", row.Text); Assert.Equal("EUR", row.UnitName);
            Assert.NotNull(row.Error);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public void Actual_household_amount_typing_captures_the_current_numeric_format()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var vm = Create(f, environment); f.Localization.SetFormattingCulture("de-DE"); vm.AmountText = "1.234";
        Assert.True(vm.TryReadOriginalAmount(out var amount)); Assert.Equal(123400, amount);
    }

    [Fact]
    public void Actual_household_share_typing_captures_the_current_numeric_format()
    {
        using var f = new FlowFixture(); f.Localization.SetFormattingCulture("en-US");
        var row = new HouseholdShareInput(new() { Name = "Original participant" }, f.Localization.CurrentCulture,
            "EUR", () => f.Localization.CurrentCulture, f.Translator);
        f.Localization.SetFormattingCulture("de-DE"); row.Text = "1.234";
        Assert.True(row.TryRead("EUR", out var amount)); Assert.Equal(123400, amount);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public void Partial_edit_does_not_renew_a_changed_household_unit_but_complete_reentry_does(bool allocation)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
            f.Localization.SetFormattingCulture("en-US"); var vm = Create(f, environment);
            var row = new HouseholdShareInput(new() { Name = "Original participant" }, f.Localization.CurrentCulture,
                "EUR", () => f.Localization.CurrentCulture, f.Translator);
            if (allocation) row.Text = "12.34"; else vm.AmountText = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
            if (allocation)
            {
                row.Text = "12.35"; Assert.False(row.TryRead("EUR", out _)); Assert.Equal("EUR", row.UnitName);
                row.Text = ""; row.Text = "12.35"; Assert.True(row.TryRead("EUR", out var value)); Assert.Equal(123500, value);
            }
            else
            {
                vm.AmountText = "12.35"; Assert.False(vm.TryReadOriginalAmount(out _)); Assert.Equal("EUR", vm.AmountUnitName);
                vm.AmountText = ""; vm.AmountText = "12.35"; Assert.True(vm.TryReadOriginalAmount(out var value)); Assert.Equal(123500, value);
            }
        }
        finally { DisplayUnits.Set(units); }
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public void Display_only_format_change_retains_household_original_group_separator(bool allocation)
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        f.Localization.SetFormattingCulture("de-DE"); var vm = Create(f, environment);
        var row = new HouseholdShareInput(new() { Name = "Original participant" }, f.Localization.CurrentCulture,
            "EUR", () => f.Localization.CurrentCulture, f.Translator);
        if (allocation) row.Text = "1.234"; else vm.AmountText = "1.234";
        f.Localization.SetFormattingCulture("en-US"); row.RefreshMoneyTexts();
        if (allocation) { Assert.True(row.TryRead("EUR", out var value)); Assert.Equal(123400, value); Assert.NotNull(row.FormatNote); }
        else { Assert.True(vm.TryReadOriginalAmount(out var value)); Assert.Equal(123400, value); Assert.NotNull(vm.AmountFormatNote); }
    }

    [Fact]
    public void Explicit_currency_selection_still_clears_amount_and_each_allocation_without_granting_write_authority()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var vm = Create(f, environment);
        vm.AmountText = "12.34";
        var row = new HouseholdShareInput(new() { Name = "Original participant" }, f.Localization.CurrentCulture) { Text = "12.34" };
        vm.Shares.Add(row); vm.CurrencyCode = "USD";
        Assert.Empty(vm.AmountText); Assert.Empty(row.Text); Assert.Equal("USD", vm.AmountUnitName); Assert.Equal("USD", row.UnitName);
        Assert.False(vm.CanWrite); Assert.False(vm.IsReady);
    }

    [Fact]
    public void Share_edit_and_return_keeps_monotonic_original_consent_ownership_and_does_not_edit_another_share()
    {
        using var f = new FlowFixture();
        var first = new HouseholdShareInput(new() { Name = "First participant" }, f.Localization.CurrentCulture) { Text = "5" };
        var second = new HouseholdShareInput(new() { Name = "Second participant" }, f.Localization.CurrentCulture) { Text = "7" };
        var original = first.Stamp; var other = second.Stamp;
        first.Text = "6"; first.Text = "5";
        Assert.NotEqual(original, first.Stamp); Assert.Equal(other, second.Stamp); Assert.Equal("7", second.Text);
    }
}
