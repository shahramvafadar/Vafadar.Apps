using System.Globalization;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Resources;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// A quantity as a page writes it: the number as it is stated – grouped, with the digits of the language of the
/// page – with its unit, and "about" where the source says so. Nothing is rounded and nothing is converted.
/// </summary>
public static class QuantityText
{
    /// <summary>Returns the words for a quantity.</summary>
    public static string Of(Quantity quantity, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        var written = string.Format(CultureInfo.CurrentCulture, text[$"Unit_{quantity.Unit}"], Number(quantity.Value, visitor), quantity.Currency);
        return quantity.IsApproximate ? string.Format(CultureInfo.CurrentCulture, text["Quantity_About"], written) : written;
    }

    /// <summary>Returns a number with every digit it was stated with, grouped by thousands.</summary>
    public static string Number(decimal value, Visitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        var digits = value.ToString("#,0.############################", CultureInfo.InvariantCulture);
        if (visitor.Language.Tag != "fa")
        {
            return digits;
        }

        // Persian digits, with the Persian marks for thousands and for the decimal point.
        return string.Concat(digits.Select(character => character switch
        {
            >= '0' and <= '9' => (char)('۰' + (character - '0')),
            ',' => '٬',
            '.' => '٫',
            _ => character,
        }));
    }
}
