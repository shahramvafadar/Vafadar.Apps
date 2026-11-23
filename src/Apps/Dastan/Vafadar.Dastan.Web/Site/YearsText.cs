using System.Globalization;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Resources;

namespace Vafadar.Dastan.Web.Site;

/// <summary>A stretch of years a visitor chose (D-084), in the words and the digits of the page.</summary>
public static class YearsText
{
    /// <summary>Returns the stretch as "1000 – 1050", with the era named where a year lies before the common era.</summary>
    /// <param name="years">The first and the last year: positive in the common era, negative before it.</param>
    /// <param name="visitor">Who reads: the language and the digits.</param>
    /// <param name="text">The texts of the interface.</param>
    public static string Of((int From, int To) years, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        var (from, to) = (Exploration.YearOf(years.From), Exploration.YearOf(years.To));
        return string.Format(CultureInfo.CurrentCulture, text["Date_Range"], TimeText.Year(from.Era, from.Number, visitor, text), TimeText.Year(to.Era, to.Number, visitor, text));
    }
}
