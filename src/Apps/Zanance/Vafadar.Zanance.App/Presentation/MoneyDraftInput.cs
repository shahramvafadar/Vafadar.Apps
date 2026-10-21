using System.Globalization;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>One detached raw monetary value and its immutable original parser, independent of native controls.</summary>
/// <param name="Text">Original raw text, never automatically converted.</param>
/// <param name="Context">Original known ISO currency, unit and read-only parse culture.</param>
/// <param name="UsesDisplayUnit">Whether this input uses display units rather than ISO-only purchase units.</param>
public sealed record MoneyDraftSnapshot(string Text, MoneyInputContext? Context, bool UsesDisplayUnit);

/// <summary>Owns one actual raw amount across display/currency changes; only complete reentry renews changed units.</summary>
public sealed class MoneyDraftInput(bool usesDisplayUnit = true)
{
    /// <summary>Gets the exact raw input, including an explicit empty draft.</summary>
    public string Text { get; private set; } = string.Empty;
    /// <summary>Gets its immutable original interpretation, or null until a known currency was supplied.</summary>
    public MoneyInputContext? Context { get; private set; }
    /// <summary>Gets the revision of actual edits/authoritative replacement, including edit-and-return.</summary>
    public long Revision { get; private set; }
    /// <summary>Gets the stable semantic draft signature without treating a harmless ambient display change as an edit.</summary>
    public string Stamp => DraftFingerprint.Create(Text, Context?.Currency.Code, usesDisplayUnit ? Context?.Unit : null,
        Context?.Culture.NumberFormat.NumberDecimalSeparator, Context?.Culture.NumberFormat.NumberGroupSeparator);
    /// <summary>Captures an authoritative loaded/receipt/template raw value in its actual current context.</summary>
    public void Initialize(string text, string currency, CultureInfo culture)
    {
        Context = Capture(currency, culture); Text = text; Revision++;
    }
    /// <summary>Updates actual typing without renewing a stale currency/unit by a partial edit.</summary>
    public void ChangeText(string text, string currency, CultureInfo culture)
    {
        if (text == Text) return;
        if (string.IsNullOrWhiteSpace(Text) || string.IsNullOrWhiteSpace(text) || MatchesCurrent(currency)) Context = Capture(currency, culture);
        Text = text; Revision++;
    }
    /// <summary>Formats an authoritative minor-unit value using the same context subsequently used for parsing.</summary>
    public string Format(long minor, string currency, CultureInfo culture)
    {
        Context = Capture(currency, culture) ?? throw new ArgumentException("An authoritative amount requires a known currency.", nameof(currency));
        Text = MoneyText.ForInput(minor, currency, Context.Culture, usesDisplayUnit ? Context.Unit : null); Revision++; return Text;
    }
    /// <summary>Checks the original currency and applicable display-unit identity without comparing mutable ambient culture.</summary>
    public bool MatchesCurrent(string currency) => Context is { } input && string.Equals(input.Currency.Code, currency, StringComparison.OrdinalIgnoreCase)
        && (!usesDisplayUnit || input.MatchesCurrent(currency));
    /// <summary>Gets whether a nonempty value needs complete explicit reentry for the selected currency/unit.</summary>
    public bool NeedsReview(string currency) => !string.IsNullOrWhiteSpace(Text) && !MatchesCurrent(currency);
    /// <summary>Parses the unchanged raw amount through its original context, without ambient unit/culture lookup.</summary>
    public bool TryParse(out long minor)
    {
        minor = 0;
        return Context is { } input && MoneyText.TryParse(Text, input.Currency, input.Culture, usesDisplayUnit ? input.Unit : null, out minor);
    }
    /// <summary>Gets the original label until clearing, or the new current label for an empty input.</summary>
    public string UnitName(string currency) => !string.IsNullOrWhiteSpace(Text) && Context is { } input
        ? usesDisplayUnit ? input.UnitName : input.Currency.Code : usesDisplayUnit ? MoneyText.UnitName(currency) : currency;
    /// <summary>Gets a direction-safe example of this exact original parser.</summary>
    public string? OriginalExample(CultureInfo current) => Context is { } input && !string.IsNullOrWhiteSpace(Text) && !input.UsesFormatOf(current)
        ? MoneyText.InputExample(input.Currency, input.Culture, usesDisplayUnit ? input.Unit : null) : null;
    /// <summary>Detaches raw/context meaning for an original nested return or undo.</summary>
    public MoneyDraftSnapshot Capture() => new(Text, Context, usesDisplayUnit);
    /// <summary>Restores the exact original interpretation without reviving an older pending writer.</summary>
    public void Restore(MoneyDraftSnapshot original)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (original.UsesDisplayUnit != usesDisplayUnit) throw new ArgumentException("Original ISO/display-unit mode differs.", nameof(original));
        Text = original.Text; Context = original.Context; Revision++;
    }
    private static MoneyInputContext? Capture(string currency, CultureInfo culture) => Currencies.TryGet(currency, out var known)
        ? MoneyInputContext.Capture(known.Code, culture) : null;
}
