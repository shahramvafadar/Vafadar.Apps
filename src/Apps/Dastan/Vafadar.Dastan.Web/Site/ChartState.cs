using System.Collections.Immutable;
using System.Globalization;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Web.Site;

/// <summary>Which way a chart of time runs on the page.</summary>
public enum ChartDirection
{
    /// <summary>Whatever fits the screen: across the page on a wide screen, down it on a narrow one.</summary>
    Fit = 0,

    /// <summary>Time runs across the page; the chart scrolls sideways.</summary>
    Across = 1,

    /// <summary>Time runs down the page; the page scrolls.</summary>
    Down = 2,
}

/// <summary>
/// How a visitor looks at a chart of time: its direction, how far it is opened, the stretch of time it shows, and
/// whether its single events and all of its lanes are shown.
/// All of it lives in the address (ADR 0008): a link changes it, the Back button undoes it, and a chart that is
/// sent to somebody arrives as it was seen. Nothing here is a statement about history – a year that cannot be
/// read is simply not a choice, and the chart shows what its comparison is about.
/// </summary>
public sealed record ChartState
{
    /// <summary>The scales a chart can be drawn at, as the length of a year in em, from closest together to furthest open.</summary>
    public static readonly ImmutableArray<double> Scales = [0.015, 0.03, 0.06, 0.12, 0.25, 0.5, 1.0];

    /// <summary>The scale a chart starts at.</summary>
    public const int DefaultScale = 2;

    // The most lanes an address may ask for; a comparison with more shows them all.
    private const int MostLanes = 99;

    // A year an address may name. Further out there is nothing a comparison of the site is about.
    private const int FurthestYear = Vafadar.Dastan.Core.AskedTime.FurthestYear;

    /// <summary>Gets the direction.</summary>
    public ChartDirection Direction { get; init; }

    /// <summary>Gets the index of the scale in <see cref="Scales"/>.</summary>
    public int Scale { get; init; } = DefaultScale;

    /// <summary>Gets the first year shown, when the visitor chose a stretch: positive in the common era, negative before it.</summary>
    public int? From { get; init; }

    /// <summary>Gets the last year shown, when the visitor chose a stretch.</summary>
    public int? To { get; init; }

    /// <summary>Gets a value indicating whether the single events are shown.</summary>
    public bool Events { get; init; } = true;

    /// <summary>Gets the number of lanes shown, counted from the first, when the visitor closed some; otherwise all.</summary>
    public int? Lanes { get; init; }

    /// <summary>Gets the length of a year in em.</summary>
    public double UnitsPerYear => Scales[Scale];

    /// <summary>Reads the state from the values of a query. A value that is not one of the choices is not a choice.</summary>
    public static ChartState Read(string? view, string? scale, string? from, string? to, string? events = null, string? lanes = null)
    {
        var direction = view switch
        {
            "across" => ChartDirection.Across,
            "down" => ChartDirection.Down,
            _ => ChartDirection.Fit,
        };
        var index = int.TryParse(scale, NumberStyles.None, CultureInfo.InvariantCulture, out var chosen) && chosen < Scales.Length ? chosen : DefaultScale;

        // A stretch is both of its ends, in order. There is no year zero.
        var stretch = Year(from) is { } first && Year(to) is { } last && last > first ? (first, last) : default((int, int)?);
        return new ChartState
        {
            Direction = direction,
            Scale = index,
            From = stretch?.Item1,
            To = stretch?.Item2,
            Events = events != "off",
            Lanes = int.TryParse(lanes, NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count is >= 1 and <= MostLanes ? count : null,
        };
    }

    /// <summary>Returns the query of the state, without what is the default: the address of a chart nobody changed has none.</summary>
    public string Query()
    {
        var parts = new List<string>(4);
        if (Direction != ChartDirection.Fit)
        {
            parts.Add(Direction == ChartDirection.Across ? "view=across" : "view=down");
        }

        if (Scale != DefaultScale)
        {
            parts.Add("scale=" + Scale.ToString(CultureInfo.InvariantCulture));
        }

        if (From is { } from && To is { } to)
        {
            parts.Add("from=" + from.ToString(CultureInfo.InvariantCulture));
            parts.Add("to=" + to.ToString(CultureInfo.InvariantCulture));
        }

        if (!Events)
        {
            parts.Add("events=off");
        }

        if (Lanes is { } lanes)
        {
            parts.Add("lanes=" + lanes.ToString(CultureInfo.InvariantCulture));
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    /// <summary>Returns the place on the axis of years where a year of an address begins.</summary>
    public static decimal Place(int year) => year > 0 ? YearAxis.StartOf(Era.CommonEra, year) : YearAxis.StartOf(Era.BeforeCommonEra, -year);

    /// <summary>Returns the year of an address a place on the axis lies in, kept inside what an address may name.</summary>
    public static int YearOf(decimal place)
    {
        var (era, year) = YearAxis.YearAt(Math.Clamp(place, Place(-FurthestYear), Place(FurthestYear)));
        return era == Era.CommonEra ? year : -year;
    }

    private static int? Year(string? value) =>
        int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var year) && year != 0 && year is >= -FurthestYear and <= FurthestYear
            && value![0] != '+'
            ? year
            : null;
}
