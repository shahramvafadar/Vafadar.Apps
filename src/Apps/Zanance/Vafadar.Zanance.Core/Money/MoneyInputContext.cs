using System.Globalization;

namespace Vafadar.Zanance.Core.Money;

/// <summary>Immutable original interpretation of one raw monetary input; formatting changes never replace its parser.</summary>
public sealed class MoneyInputContext
{
    private MoneyInputContext(Currency currency, CultureInfo culture, DisplayUnit? unit)
    {
        Currency = currency; Culture = culture.IsReadOnly ? culture : CultureInfo.ReadOnly((CultureInfo)culture.Clone()); Unit = unit;
    }
    /// <summary>Gets the original known ISO currency and minor precision.</summary>
    public Currency Currency { get; }
    /// <summary>Gets the original read-only parsing culture.</summary>
    public CultureInfo Culture { get; }
    /// <summary>Gets the original explicit unit, or null for ISO major units.</summary>
    public DisplayUnit? Unit { get; }
    /// <summary>Gets the original label that must remain beside a nonempty raw amount.</summary>
    public string UnitName => Unit?.Name ?? Currency.Code;
    /// <summary>Gets a direction-safe valid example without ambient unit lookup.</summary>
    public string Example => MoneyText.InputExample(Currency, Culture, Unit);
    /// <summary>Captures the current known currency, unit and immutable culture only for actual input initialization or editing.</summary>
    public static MoneyInputContext Capture(string currencyCode, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture); var currency = Currencies.Get(currencyCode);
        DisplayUnits.TryGet(currencyCode, out var unit); return new(currency, culture, unit);
    }
    /// <summary>Checks whether the selected currency and ambient unit still describe the original input.</summary>
    public bool MatchesCurrent(string currencyCode)
    {
        DisplayUnits.TryGet(currencyCode, out var unit);
        return string.Equals(Currency.Code, currencyCode, StringComparison.OrdinalIgnoreCase) && Unit == unit;
    }
    /// <summary>Checks meaningful separator changes without treating a language-only caption change as new money.</summary>
    public bool UsesFormatOf(CultureInfo culture) => Culture.NumberFormat.NumberDecimalSeparator == culture.NumberFormat.NumberDecimalSeparator
        && Culture.NumberFormat.NumberGroupSeparator == culture.NumberFormat.NumberGroupSeparator;
    /// <summary>Parses through the central monetary rules using the original immutable inputs.</summary>
    public bool TryParse(string? text, out long minor) => MoneyText.TryParse(text, Currency, Culture, Unit, out minor);
    /// <summary>Initializes a displayed saved value in this exact original input context.</summary>
    public string ForInput(long minor) => MoneyText.ForInput(minor, Currency.Code, Culture, Unit);
}
