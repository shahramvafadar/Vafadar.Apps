using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Where a real place is (step 12.8, DOM-011): degrees as they are stated and how exactly they are meant; a
/// place of a story is not a place on a map. Test data.
/// </summary>
public sealed class PlacePositionTests
{
    private static Action<JsonObject> Place(string type, string position) => sample =>
    {
        sample["entities"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "place-x", ["type"] = type, ["label"] = "Test data", ["summary"] = "Test data.",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0, ["position"] = JsonNode.Parse(position),
        });
    };

    [Theory]
    [InlineData("Place", "exact", PositionPrecision.Exact)]
    [InlineData("Region", "centre_of_area", PositionPrecision.CentreOfArea)]
    [InlineData("Country", "approximate", PositionPrecision.Approximate)]
    public void A_real_place_says_where_it_is_and_how_exactly(string type, string precision, PositionPrecision expected)
    {
        var catalogue = SampleFiles.Read(Place(type, $$"""{ "latitude": 36.2972, "longitude": -59.6067, "precision": "{{precision}}" }"""));

        Assert.Equal(new GeoPosition(36.2972m, -59.6067m, expected), catalogue.Find(new EntityId("place-x"))!.Position);
        Assert.Equal(expected, Projection.Of(catalogue, ReadingLevel.Opening, Audience.LocalDemonstration)!.Find(new EntityId("place-x"))!.Position!.Precision);
    }

    [Theory]
    [InlineData("NarrativePlace")]
    [InlineData("NarrativeRegion")]
    [InlineData("Person")]
    [InlineData("Organisation")]
    public void What_is_no_real_place_has_no_position(string type)
    {
        var problems = SampleFiles.Problems(Place(type, """{ "latitude": 1, "longitude": 1, "precision": "exact" }"""));

        Assert.Contains("place-x: only a real place has a position; a place of a story is not a place on a map.", problems);
    }

    [Fact]
    public void A_position_is_on_the_earth_and_says_how_exactly_it_is_meant()
    {
        Assert.Contains("place-x: its position is not on the earth.", SampleFiles.Problems(Place("Place", """{ "latitude": 91, "longitude": 0, "precision": "exact" }""")));
        Assert.Contains("place-x: its position is not on the earth.", SampleFiles.Problems(Place("Place", """{ "latitude": 0, "longitude": -181, "precision": "exact" }""")));
        Assert.Contains(SampleFiles.Problems(Place("Place", """{ "latitude": 0, "longitude": 0 }""")), problem => problem.Contains("precision", StringComparison.Ordinal));
        Assert.Contains(SampleFiles.Problems(Place("Place", """{ "latitude": 0, "longitude": 0, "precision": "near" }""")), problem => problem.Contains("precision", StringComparison.Ordinal));
        Assert.Contains(SampleFiles.Problems(Place("Place", """{ "longitude": 0, "precision": "exact" }""")), problem => problem.Contains("latitude", StringComparison.Ordinal));
    }

    [Fact]
    public void The_sample_of_the_repository_puts_nothing_on_a_map()
    {
        Assert.All(PilotSample.Load(SampleFiles.PilotPath).Entities, entity => Assert.Null(entity.Position));
    }
}
