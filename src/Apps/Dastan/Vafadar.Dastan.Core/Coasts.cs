using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Vafadar.Dastan.Core;

/// <summary>
/// Lines on the earth, as lines of points (D-057) – its coasts, or its rivers and the shores of its lakes: what
/// a drawing of places lays under its grid, so that a reader sees where on the earth the places are. The lines come with the site – Natural Earth, public domain –
/// and nothing is asked of anybody when a page is drawn. They are coasts and nothing else: no border of a
/// country is drawn.
/// </summary>
public sealed class Coasts
{
    /// <summary>No coasts: a drawing without them is a grid of degrees.</summary>
    public static readonly Coasts None = new([]);

    private readonly ImmutableArray<ImmutableArray<(double Longitude, double Latitude)>> _lines;

    private Coasts(ImmutableArray<ImmutableArray<(double Longitude, double Latitude)>> lines) => _lines = lines;

    /// <summary>Gets how many lines there are.</summary>
    public int Count => _lines.Length;

    /// <summary>
    /// Reads coasts: one line of the coast per line of the text, as <c>longitude,latitude</c> pairs in degrees
    /// with a space between them. A line that begins with <c>#</c> says something about the file.
    /// </summary>
    public static Coasts Read(TextReader text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = ImmutableArray.CreateBuilder<ImmutableArray<(double, double)>>();
        while (text.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var points = ImmutableArray.CreateBuilder<(double, double)>();
            foreach (var pair in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var comma = pair.IndexOf(',', StringComparison.Ordinal);
                if (comma <= 0
                    || !double.TryParse(pair.AsSpan(0, comma), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                    || !double.TryParse(pair.AsSpan(comma + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                    || longitude is < -180 or > 180 || latitude is < -90 or > 90)
                {
                    throw new FormatException($"'{pair}' is no position on the earth.");
                }

                points.Add((longitude, latitude));
            }

            if (points.Count > 1)
            {
                lines.Add(points.ToImmutable());
            }
        }

        return new(lines.ToImmutable());
    }

    /// <summary>
    /// Returns the coasts inside a part of the earth as the paths of a drawing: each a line of
    /// <c>M x y L x y …</c> in the measure of the drawing. A coast that leaves the part is cut there; points
    /// closer together than the drawing can show are left out.
    /// </summary>
    /// <param name="west">The western end, in degrees; it may lie beyond 180 when the part spans the line of the date.</param>
    /// <param name="east">The eastern end.</param>
    /// <param name="south">The southern end.</param>
    /// <param name="north">The northern end.</param>
    /// <param name="across">Where a longitude lies in the drawing.</param>
    /// <param name="down">Where a latitude lies in the drawing.</param>
    public ImmutableArray<string> Within(double west, double east, double south, double north, Func<double, double> across, Func<double, double> down)
    {
        ArgumentNullException.ThrowIfNull(across);
        ArgumentNullException.ThrowIfNull(down);

        // A point just outside is kept, so that a coast runs to the edge of the drawing and not short of it.
        var (around, middle) = (Math.Max(east - west, north - south) * 0.05, (west + east) / 2);
        var paths = ImmutableArray.CreateBuilder<string>();
        var path = new StringBuilder();
        foreach (var line in _lines)
        {
            var (inside, count, lastX, lastY, before) = (false, 0, 0d, 0d, default((double X, double Y)?));
            double? previous = null;
            foreach (var (longitude, latitude) in line)
            {
                // The longitude is counted the way the drawing counts it; a coast that crosses the far side of
                // the earth from the drawing is two lines.
                var turned = longitude;
                while (turned < middle - 180)
                {
                    turned += 360;
                }

                while (turned >= middle + 180)
                {
                    turned -= 360;
                }

                var jumped = previous is { } was && Math.Abs(turned - was) > 180;
                previous = turned;
                var (x, y) = (across(turned), down(latitude));
                var within = turned >= west - around && turned <= east + around && latitude >= south - around && latitude <= north + around;
                if (jumped)
                {
                    End();
                    before = null;
                }

                if (within)
                {
                    if (!inside)
                    {
                        if (before is { } outside)
                        {
                            Add(outside.X, outside.Y, first: true);
                            Add(x, y, first: false);
                        }
                        else
                        {
                            Add(x, y, first: true);
                        }

                        inside = true;
                    }
                    else if (Math.Abs(x - lastX) + Math.Abs(y - lastY) >= Finest)
                    {
                        Add(x, y, first: false);
                    }
                }
                else if (inside)
                {
                    Add(x, y, first: false);
                    End();
                }

                before = (x, y);

                void Add(double atX, double atY, bool first)
                {
                    path.Append(first ? 'M' : 'L').Append(atX.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ').Append(atY.ToString("0.##", CultureInfo.InvariantCulture));
                    (lastX, lastY, count) = (atX, atY, count + 1);
                }
            }

            End();

            void End()
            {
                if (count > 1)
                {
                    paths.Add(path.ToString());
                }

                path.Clear();
                (inside, count) = (false, 0);
            }
        }

        return paths.ToImmutable();
    }

    // Two points of a coast closer together than this, in the measure of a drawing, are one for the eye.
    private const double Finest = 0.08;
}
