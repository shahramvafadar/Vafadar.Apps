using System.Globalization;
using Vafadar.Dastan.People;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The years photographs were taken in – a gallery by time (step 16.10). The years are those of the photographs
/// a viewer was given, so a year tells nothing about a photograph the viewer may not see (PRIV-012).
/// </summary>
public static class PhotoYears
{
    /// <summary>Returns the years in which the photographs that say when they were taken were taken, the latest first.</summary>
    public static int[] Of(IEnumerable<PhotoInfo> photos)
    {
        ArgumentNullException.ThrowIfNull(photos);
        return [.. photos.Where(photo => photo.Taken is not null).Select(photo => photo.Taken!.Value.Year).Distinct().OrderDescending()];
    }

    /// <summary>Returns a year as it is written in a language: without a separator of thousands, in the digits of the language.</summary>
    public static string Written(int year, string language)
    {
        var written = year.ToString(CultureInfo.InvariantCulture);
        return language == "fa" ? Vafadar.Core.Text.Digits.ToPersian(written) : written;
    }
}
