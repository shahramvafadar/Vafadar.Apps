using System.Globalization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-432: actual raw-money state used by native transaction and split fields.</summary>
[Trait("AT", "AT-432")]
[Collection("Entry validation units")]
public sealed class MoneyDraftInputTests
{
    /// <summary>A partial edit never acknowledges another currency/unit, while complete reentry does.</summary>
    [Theory][InlineData(false)][InlineData(true)]
    public void Changed_meaning_requires_complete_reentry(bool currency)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = new MoneyDraftInput(); var culture = CultureInfo.GetCultureInfo("en-US"); input.Initialize("12.34", "EUR", culture);
            if (!currency) DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            var code = currency ? "JPY" : "EUR"; input.ChangeText("12.340", code, culture);
            Assert.True(input.NeedsReview(code)); Assert.Equal("EUR", input.UnitName(code));
            input.ChangeText(string.Empty, code, culture); input.ChangeText(currency ? "12" : "0.1234", code, culture);
            Assert.False(input.NeedsReview(code)); Assert.True(input.TryParse(out var minor)); Assert.Equal(currency ? 12 : 1234, minor);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Loaded/receipt values initialize the exact subsequent parser, while original purchases remain ISO-only.</summary>
    [Theory][InlineData(false)][InlineData(true)]
    public void Authoritative_minor_amounts_keep_explicit_ISO_or_display_mode(bool display)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); var input = new MoneyDraftInput(display);
            Assert.Equal(display ? "12.3456" : "1234.56", input.Format(123456, "EUR", CultureInfo.GetCultureInfo("en-US")));
            Assert.True(input.TryParse(out var minor)); Assert.Equal(123456, minor);
            DisplayUnits.Set([new("EUR", "Renamed unit", 2)]); Assert.Equal(display, input.NeedsReview("EUR"));
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Returning to the same digits retains semantic equality but retires an older monetary writer.</summary>
    [Fact]
    public void Edit_and_return_keeps_a_distinct_pending_revision()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = new MoneyDraftInput(); var culture = CultureInfo.GetCultureInfo("en-US");
            input.Initialize("12.34", "EUR", culture); var stamp = input.Stamp; var revision = input.Revision;
            input.ChangeText("99", "EUR", culture); input.ChangeText("12.34", "EUR", culture);
            Assert.Equal(stamp, input.Stamp); Assert.True(input.Revision > revision);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Display-only format changes retain raw parsing until actual typing renews the format.</summary>
    [Fact]
    public void Original_group_separator_survives_display_but_actual_typing_uses_current_format()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = new MoneyDraftInput(); input.Initialize("1,234", "EUR", CultureInfo.GetCultureInfo("en-US"));
            Assert.NotNull(input.OriginalExample(CultureInfo.GetCultureInfo("de-DE"))); Assert.Equal("1,234", input.Text);
            Assert.True(input.TryParse(out var original)); Assert.Equal(123400, original);
            input.ChangeText("12,34", "EUR", CultureInfo.GetCultureInfo("de-DE")); Assert.True(input.TryParse(out var edited)); Assert.Equal(1234, edited);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Undo restores original interpretation without borrowing an older pending revision.</summary>
    [Fact]
    public void Detached_undo_restores_context_and_advances_revision()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = new MoneyDraftInput(); var culture = CultureInfo.GetCultureInfo("en-US");
            input.Initialize("12.34", "EUR", culture); var original = input.Capture(); var revision = input.Revision;
            input.ChangeText(string.Empty, "JPY", culture); input.ChangeText("12", "JPY", culture); input.Restore(original);
            Assert.Equal("12.34", input.Text); Assert.True(input.MatchesCurrent("EUR")); Assert.True(input.Revision > revision);
            Assert.Throws<ArgumentException>(() => new MoneyDraftInput(false).Restore(original));
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>An unknown original currency never becomes a guessed known monetary amount after a partial edit.</summary>
    [Fact]
    public void Unknown_currency_requires_explicit_clear_before_known_interpretation()
    {
        var input = new MoneyDraftInput(); var culture = CultureInfo.GetCultureInfo("en-US"); input.Initialize("10", "UNKNOWN", culture);
        input.ChangeText("100", "EUR", culture); Assert.True(input.NeedsReview("EUR")); Assert.False(input.TryParse(out _));
        input.ChangeText(string.Empty, "EUR", culture); input.ChangeText("10", "EUR", culture); Assert.False(input.NeedsReview("EUR"));
    }
}
