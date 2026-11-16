using System.Globalization;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// How a visitor looks at the chart of a story: the run of steps, the number of lanes and whether the steps carry
/// their names. It lives in the address (ADR 0008) and never decides what may be seen: the chart is cut out of the
/// lanes the projection gives for the reading position, whatever the address asks for.
/// </summary>
public sealed record StoryChartState
{
    // The most steps and lanes an address may name; a story with more shows them all.
    private const int Most = 999;

    /// <summary>Gets the place of the first step shown, counted from 1, when the visitor closed earlier ones.</summary>
    public int? From { get; init; }

    /// <summary>Gets the place of the last step shown, when the visitor closed later ones.</summary>
    public int? To { get; init; }

    /// <summary>Gets the number of lanes shown, counted from the first, when the visitor closed some.</summary>
    public int? Lanes { get; init; }

    /// <summary>Gets a value indicating whether the steps carry their names; otherwise their numbers alone.</summary>
    public bool Titles { get; init; } = true;

    /// <summary>Reads the state from the values of a query. A value that is not one of the choices is not a choice.</summary>
    public static StoryChartState Read(string? from, string? to, string? lanes, string? heads) => new()
    {
        From = Count(from),
        To = Count(to),
        Lanes = Count(lanes),
        Titles = heads != "numbers",
    };

    /// <summary>Returns the query of the state, without what is the default.</summary>
    public string Query()
    {
        var parts = new List<string>(4);
        if (From is { } from)
        {
            parts.Add("from=" + from.ToString(CultureInfo.InvariantCulture));
        }

        if (To is { } to)
        {
            parts.Add("to=" + to.ToString(CultureInfo.InvariantCulture));
        }

        if (Lanes is { } lanes)
        {
            parts.Add("lanes=" + lanes.ToString(CultureInfo.InvariantCulture));
        }

        if (!Titles)
        {
            parts.Add("heads=numbers");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    private static int? Count(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count is >= 1 and <= Most ? count : null;
}
