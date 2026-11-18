using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>A real place that says where it is, with what is stated to be there.</summary>
/// <param name="Place">The place.</param>
/// <param name="Position">Where it is.</param>
/// <param name="Here">The relations that end at the place – who was born there, what lies in it – as far as the visitor may see them.</param>
public sealed record PlaceOnMap(Entity Place, GeoPosition Position, ImmutableArray<Edge> Here);

/// <summary>The real places of a collection, as far as the visitor may see them.</summary>
/// <param name="Mapped">The places that say where they are, in the order of the collection.</param>
/// <param name="Unmapped">The real places that state no position. A place of a story is in neither list: it is no place on a map.</param>
public sealed record PlacesView(ImmutableArray<PlaceOnMap> Mapped, ImmutableArray<Entity> Unmapped)
{
    /// <summary>Gets a value indicating whether the collection names no real place the visitor may see.</summary>
    public bool IsEmpty => Mapped.IsEmpty && Unmapped.IsEmpty;
}

/// <summary>A place on the drawing of a map.</summary>
/// <param name="Number">The number the place has on the drawing and in the list beside it, from one.</param>
/// <param name="Place">The place.</param>
/// <param name="X">Its distance from the left side of the drawing.</param>
/// <param name="Y">Its distance from the upper side of the drawing.</param>
public sealed record MapMark(int Number, PlaceOnMap Place, double X, double Y);

/// <summary>A line of the grid of a map: a meridian or a parallel.</summary>
/// <param name="Degrees">The degrees the line stands for: east of Greenwich or north of the equator, negative to the west or south.</param>
/// <param name="At">Its distance from the left side (a meridian) or the upper side (a parallel) of the drawing.</param>
public sealed record MapLine(decimal Degrees, double At);

/// <summary>
/// The drawing of the places of a collection: the part of the earth they lie in, with a grid of degrees and –
/// where the site was given them – the coasts (D-057). It has no borders, and the site loads no map from
/// anybody: it says how the places lie to each other, and the grid and the coasts say where on the earth.
/// </summary>
/// <param name="Width">The width of the drawing.</param>
/// <param name="Height">The height of the drawing.</param>
/// <param name="Marks">The places, in the order of the view.</param>
/// <param name="Meridians">The lines from north to south, from the left.</param>
/// <param name="Parallels">The lines from west to east, from the top.</param>
public sealed record PlacesMap(double Width, double Height, ImmutableArray<MapMark> Marks, ImmutableArray<MapLine> Meridians, ImmutableArray<MapLine> Parallels)
{
    /// <summary>The length of the longer side of a drawing.</summary>
    public const double Size = 40;

    /// <summary>Gets the coasts inside the drawing, each the path of a line in the measure of the drawing; none when the site was given no coasts.</summary>
    public ImmutableArray<string> Coasts { get; init; } = [];

    /// <summary>
    /// Gets the rivers and the shores of the lakes inside the drawing, like the coasts. Far from the sea they
    /// are what tells a reader where the places are.
    /// </summary>
    public ImmutableArray<string> Waters { get; init; } = [];

    // The shorter side is never less than this, so that places on one line still have a drawing around them.
    private const double Least = 16;

    // The steps a grid can have, in degrees; the widest is taken that still gives a few lines.
    private static readonly decimal[] Steps = [0.01m, 0.02m, 0.05m, 0.1m, 0.2m, 0.5m, 1, 2, 5, 10, 15, 30, 45];

    /// <summary>Returns the drawing of the places of a view; <see langword="null"/> when none says where it is.</summary>
    public static PlacesMap? Of(PlacesView view) => Of(view, Core.Coasts.None, Core.Coasts.None);

    /// <summary>
    /// Returns the drawing of the places of a view over the coasts of the earth and over its rivers and lakes;
    /// <see langword="null"/> when no place says where it is.
    /// </summary>
    public static PlacesMap? Of(PlacesView view, Coasts coasts, Coasts waters)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(coasts);
        ArgumentNullException.ThrowIfNull(waters);
        if (view.Mapped.IsEmpty)
        {
            return null;
        }

        // Places on both sides of the line of the date lie close to each other: when the places span less of
        // the earth counted from 0 to 360 than from -180 to 180, the drawing is laid out that way.
        var plain = view.Mapped.Select(place => (double)place.Position.Longitude).ToArray();
        var turned = plain.Select(longitude => longitude < 0 ? longitude + 360 : longitude).ToArray();
        var longitudes = turned.Max() - turned.Min() < plain.Max() - plain.Min() ? turned : plain;
        var latitudes = view.Mapped.Select(place => (double)place.Position.Latitude).ToArray();

        // The area: what the places span, a tenth more at each side, and never less than two degrees.
        var (south, north) = Around(latitudes.Min(), latitudes.Max());
        var (west, east) = Around(longitudes.Min(), longitudes.Max());
        south = Math.Max(south, -90);
        north = Math.Min(north, 90);

        // A degree of longitude is shorter than one of latitude away from the equator; the drawing keeps that.
        var narrowing = Math.Max(0.2, Math.Cos((south + north) / 2 * Math.PI / 180));
        var scale = Size / Math.Max((east - west) * narrowing, north - south);
        if ((east - west) * narrowing * scale < Least)
        {
            var more = ((Least / (narrowing * scale)) - (east - west)) / 2;
            (west, east) = (west - more, east + more);
        }

        if ((north - south) * scale < Least)
        {
            var more = ((Least / scale) - (north - south)) / 2;
            (south, north) = (Math.Max(-90, south - more), Math.Min(90, north + more));
        }

        double Across(double longitude) => (longitude - west) * narrowing * scale;
        double Down(double latitude) => (north - latitude) * scale;

        return new(
            Across(east),
            Down(south),
            [.. view.Mapped.Select((place, index) => new MapMark(index + 1, place, Across(longitudes[index]), Down(latitudes[index])))],
            [.. Lines(west, east).Select(degrees => new MapLine(degrees > 180 ? degrees - 360 : degrees, Across((double)degrees)))],
            [.. Lines(south, north).Reverse().Select(degrees => new MapLine(degrees, Down((double)degrees)))])
        {
            Coasts = coasts.Within(west, east, south, north, Across, Down),
            Waters = waters.Within(west, east, south, north, Across, Down),
        };
    }

    private static (double From, double To) Around(double least, double most)
    {
        var span = Math.Max(2, (most - least) * 1.2);
        var middle = (least + most) / 2;
        return (middle - (span / 2), middle + (span / 2));
    }

    // The lines of the grid between two ends: at whole steps, with the widest step that gives at least three lines.
    private static IEnumerable<decimal> Lines(double from, double to)
    {
        var step = Steps.LastOrDefault(candidate => (to - from) / (double)candidate >= 3, Steps[0]);
        for (var line = Math.Ceiling((decimal)from / step) * step; line <= (decimal)to; line += step)
        {
            yield return line;
        }
    }
}
