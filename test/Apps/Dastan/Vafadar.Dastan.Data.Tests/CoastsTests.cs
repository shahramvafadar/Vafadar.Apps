using Vafadar.Dastan.Core;
using System.Globalization;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The coasts under the drawing of the places (step 17.2, D-057): read from a text that comes with the site,
/// cut to the part of the earth a drawing shows, and never asked of anybody.
/// </summary>
public sealed class CoastsTests
{
    private static Coasts Read(params string[] lines) => Coasts.Read(new StringReader(string.Join('\n', lines)));

    // A drawing whose measure is the degrees themselves, north at the top.
    private static string[] Within(Coasts coasts, double west, double east, double south, double north) =>
        [.. coasts.Within(west, east, south, north, longitude => longitude - west, latitude => north - latitude)];

    [Fact]
    public void Coasts_are_read_line_by_line_and_what_says_something_about_the_file_is_left_out()
    {
        var coasts = Read("# Natural Earth, public domain.", string.Empty, "10,10 11,10 11,11", "20,20", "30,30 31,31");

        // A line of one point is no line.
        Assert.Equal(2, coasts.Count);
        Assert.Equal(0, Coasts.None.Count);
    }

    [Theory]
    [InlineData("10,10 eleven,10")]
    [InlineData("10,10 11")]
    [InlineData("10,10 181,10")]
    [InlineData("10,10 11,-91")]
    public void What_is_no_position_on_the_earth_is_refused(string line) =>
        Assert.Throws<FormatException>(() => Read(line));

    [Fact]
    public void A_coast_inside_the_drawing_is_one_path_in_its_measure()
    {
        var paths = Within(Read("12,18 14,18 14,16"), west: 10, east: 20, south: 10, north: 20);

        Assert.Equal(["M2 2L4 2L4 4"], paths);
    }

    [Fact]
    public void A_coast_far_from_the_drawing_is_not_in_it()
    {
        var coasts = Read("100,50 101,50 102,51", "12,18 14,18");

        Assert.Single(Within(coasts, west: 10, east: 20, south: 10, north: 20));
        Assert.Empty(Within(coasts, west: -50, east: -40, south: -20, north: -10));
    }

    [Fact]
    public void A_coast_that_leaves_the_drawing_is_cut_there_and_runs_to_its_edge()
    {
        // In at the west, across, out at the east, far away, and in again at the north.
        var paths = Within(Read("0,15 12,15 18,15 40,15 60,40 15,30 15,18"), west: 10, east: 20, south: 10, north: 20);

        Assert.Equal(2, paths.Length);
        Assert.StartsWith("M-10 5L2 5", paths[0], StringComparison.Ordinal);
        Assert.EndsWith("L30 5", paths[0], StringComparison.Ordinal);
        Assert.Equal("M5 -10L5 2", paths[1]);
    }

    [Fact]
    public void Points_the_drawing_cannot_tell_apart_are_one()
    {
        var paths = Within(Read("12,18 12.01,18 12.02,18 12.03,18 14,18"), west: 10, east: 20, south: 10, north: 20);

        Assert.Equal(["M2 2L4 2"], paths);
    }

    [Fact]
    public void A_drawing_across_the_line_of_the_date_has_the_coasts_of_both_sides_and_no_line_around_the_earth()
    {
        // Fiji: a coast east of the line and one west of it, and one that crosses it.
        var coasts = Read("178,-17 179,-17", "-179,-16 -178,-16", "179,-18 180,-18 -180,-18 -179,-18");

        var paths = Within(coasts, west: 175, east: 185, south: -20, north: -15);

        Assert.Equal(3, paths.Length);
        Assert.Equal("M3 2L4 2", paths[0]);
        Assert.Equal("M6 1L7 1", paths[1]);
        Assert.All(paths, path => Assert.All(
            path.Split('M', 'L', ' ').Where(part => part.Length > 0).Where((_, index) => index % 2 == 0),
            x => Assert.InRange(double.Parse(x, CultureInfo.InvariantCulture), -1, 11)));
    }

    [Fact]
    public void The_drawing_of_places_lies_over_the_coasts_it_was_given_and_over_none_otherwise()
    {
        var view = new PlacesView(
        [
            new(Entity("place-north"), new GeoPosition(40, 50, PositionPrecision.Exact), []),
            new(Entity("place-south"), new GeoPosition(30, 60, PositionPrecision.Exact), []),
        ],
        []);
        var coasts = Read("49,38 50,37 52,37 54,39", "-70,10 -69,11");

        Assert.Empty(PlacesMap.Of(view)!.Coasts);
        var map = PlacesMap.Of(view, coasts, Read("58,31 59,32 59,33"))!;

        // The same drawing, with the one coast that lies in it.
        Assert.Equal(PlacesMap.Of(view)!.Marks, map.Marks);
        var coast = Assert.Single(map.Coasts);
        Assert.StartsWith("M", coast, StringComparison.Ordinal);
        Assert.Equal(3, coast.Count(letter => letter == 'L'));

        // Rivers and lakes are lines of their own, beside the coasts.
        Assert.Equal(2, Assert.Single(map.Waters).Count(letter => letter == 'L'));
        Assert.Empty(PlacesMap.Of(view)!.Waters);
    }

    private static Entity Entity(string _) =>
        Projection.Of(SampleFiles.Read(_ => { }), ReadingLevel.Opening, Audience.LocalDemonstration)!.Overview().Subject!;
}
