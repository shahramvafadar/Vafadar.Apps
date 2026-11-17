using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The real places of a collection and their drawing (step 14.1): which places are on it, what is said to be
/// there, and where a place lies on a grid of degrees. Test data.
/// </summary>
public sealed class PlacesViewTests
{
    private static JsonObject Place(string id, string type, string? position, int level = 0)
    {
        var place = new JsonObject
        {
            ["id"] = id, ["type"] = type, ["label"] = id, ["summary"] = "Test data.",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = level,
        };
        if (position is not null)
        {
            place["position"] = JsonNode.Parse(position);
        }

        return place;
    }

    private static PlacesView View(ReadingLevel? level = null, params JsonObject[] places)
    {
        var catalogue = SampleFiles.Read(sample =>
        {
            foreach (var place in places)
            {
                sample["entities"]!.AsArray().Add(place);
            }

            if (places.Any(place => (string?)place["id"] == "place-north"))
            {
                sample["relations"]!.AsArray().Add(new JsonObject
                {
                    ["id"] = "world-lies", ["from"] = "place-north", ["to"] = "region-around", ["type"] = "locatedIn", ["label"] = "Lies in",
                    ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
                    ["reviewStatus"] = "source_backed_editorial_review_pending",
                    ["scope"] = new JsonObject { ["kind"] = "outside_the_story", ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
                    ["directionality"] = "directed",
                });
            }
        });
        return Projection.Of(catalogue, level ?? ReadingLevel.Opening, Audience.LocalDemonstration)!.Places();
    }

    private static JsonObject[] Three =>
    [
        Place("place-north", "Place", """{ "latitude": 40, "longitude": 50, "precision": "exact" }"""),
        Place("region-around", "Region", """{ "latitude": 30, "longitude": 60, "precision": "centre_of_area" }"""),
        Place("country-unsaid", "Country", null),
    ];

    [Fact]
    public void The_collection_of_the_repository_names_no_real_place()
    {
        var view = View();

        Assert.True(view.IsEmpty);
        Assert.Null(PlacesMap.Of(view));
    }

    [Fact]
    public void Real_places_are_listed_with_what_lies_there_and_those_without_a_position_apart()
    {
        var view = View(null, Three);

        Assert.Equal(["place-north", "region-around"], view.Mapped.Select(place => place.Place.Id.Value));
        Assert.Equal(["country-unsaid"], view.Unmapped.Select(place => place.Id.Value));
        Assert.Empty(view.Mapped[0].Here);
        Assert.Equal("world-lies", Assert.Single(view.Mapped[1].Here).Relation.Id.Value);
    }

    [Fact]
    public void A_place_of_a_later_reading_stage_is_on_no_list()
    {
        var view = View(null, Place("place-late", "Place", """{ "latitude": 1, "longitude": 1, "precision": "exact" }""", level: 2));

        Assert.True(view.IsEmpty);
    }

    [Fact]
    public void The_drawing_has_north_at_the_top_and_east_at_the_right_and_keeps_every_place_inside()
    {
        var map = PlacesMap.Of(View(null, Three))!;
        var (north, around) = (map.Marks[0], map.Marks[1]);

        Assert.Equal([1, 2], map.Marks.Select(mark => mark.Number));
        Assert.True(north.Y < around.Y);
        Assert.True(north.X < around.X);
        Assert.All(map.Marks, mark => Assert.True(mark.X > 0 && mark.X < map.Width && mark.Y > 0 && mark.Y < map.Height));
        Assert.Equal(PlacesMap.Size, Math.Max(map.Width, map.Height), 6);

        // The grid: whole steps of degrees inside the drawing, meridians from the west and parallels from the north.
        Assert.True(map.Meridians.Length >= 3 && map.Parallels.Length >= 3);
        Assert.Equal(map.Meridians.OrderBy(line => line.Degrees), map.Meridians);
        Assert.Equal(map.Parallels.OrderByDescending(line => line.Degrees), map.Parallels);
        Assert.All(map.Meridians, line => Assert.InRange(line.At, 0, map.Width));
        Assert.All(map.Parallels, line => Assert.InRange(line.At, 0, map.Height));
        Assert.Contains(map.Parallels, line => line.Degrees == 40);
    }

    [Fact]
    public void One_place_alone_has_a_drawing_around_it()
    {
        var map = PlacesMap.Of(View(null, Place("place-alone", "Place", """{ "latitude": 36.3, "longitude": 59.6, "precision": "approximate" }""")))!;
        var mark = Assert.Single(map.Marks);

        Assert.True(map.Width >= 16 && map.Height >= 16);
        Assert.Equal(map.Width / 2, mark.X, 6);
        Assert.Equal(map.Height / 2, mark.Y, 6);
        Assert.NotEmpty(map.Meridians);
        Assert.NotEmpty(map.Parallels);
    }

    [Fact]
    public void Places_on_both_sides_of_the_line_of_the_date_lie_beside_each_other()
    {
        var map = PlacesMap.Of(View(
            null,
            Place("place-west-of-line", "Place", """{ "latitude": 0, "longitude": 179, "precision": "exact" }"""),
            Place("place-east-of-line", "Place", """{ "latitude": 0, "longitude": -179, "precision": "exact" }""")))!;

        // 179° east lies left of 179° west, two degrees apart – not a whole earth.
        Assert.True(map.Marks[0].X < map.Marks[1].X);
        Assert.All(map.Meridians, line => Assert.InRange(line.Degrees, -180, 180));
        Assert.Contains(map.Meridians, line => line.Degrees < 0);
        Assert.Contains(map.Meridians, line => line.Degrees > 0);
    }

    [Fact]
    public void Places_at_a_pole_stay_on_the_earth()
    {
        var map = PlacesMap.Of(View(null, Place("place-pole", "Place", """{ "latitude": 90, "longitude": 0, "precision": "exact" }""")))!;

        Assert.All(map.Parallels, line => Assert.InRange(line.Degrees, -90, 90));
        Assert.InRange(Assert.Single(map.Marks).Y, 0, map.Height);
    }
}
