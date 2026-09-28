using Vafadar.Zanance.Core.Money;
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// The note under an amount field whose currency has a display unit (FX-07), e.g. "Amounts in Toman (1 Toman = 10 IRR)",
/// so an amount is never typed in rials while the app reads tomans.
/// </summary>
internal static class DisplayUnitNote
{
    /// <summary>Returns the note, or <see langword="null"/> when amounts of the currency are entered in the currency itself.</summary>
    public static string? For(Translator translator, string? currencyCode)
    {
        ArgumentNullException.ThrowIfNull(translator);
        return DisplayUnits.TryGet(currencyCode, out var unit)
            ? translator.Format("Unit_AmountsIn", unit.Name, $"\u2066\u200E{unit.Factor:N0}\u00A0{unit.CurrencyCode}\u200E\u2069")
            : null;
    }
}