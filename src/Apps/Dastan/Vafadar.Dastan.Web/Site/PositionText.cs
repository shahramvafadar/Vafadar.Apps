using System.Globalization;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Web.Resources;

namespace Vafadar.Dastan.Web.Site;

/// <summary>Where a place is, as a page writes it: degrees north or south and east or west, as they are stated.</summary>
public static class PositionText
{
    /// <summary>Returns the words for a position.</summary>
    public static string Of(GeoPosition position, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        return string.Format(
            CultureInfo.CurrentCulture,
            text["Position_Format"],
            QuantityText.Number(Math.Abs(position.Latitude), visitor),
            text[position.Latitude < 0 ? "Position_South" : "Position_North"],
            QuantityText.Number(Math.Abs(position.Longitude), visitor),
            text[position.Longitude < 0 ? "Position_West" : "Position_East"]);
    }

    /// <summary>Returns the address of a position that a device opens with the map it has; nothing is sent to anybody by the page.</summary>
    public static string Address(GeoPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return string.Create(CultureInfo.InvariantCulture, $"geo:{position.Latitude},{position.Longitude}");
    }
}
