using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The years a visitor chose, honoured by the connections and the pictures of a name (D-084): what states a
/// time is part of the answer only when it lies in the years; what states no time is part of it always and is
/// told apart; what lies outside is counted, so that a page can say it was left out. The records are test data.
/// </summary>
public sealed class YearsOfANameTests
{
    private const string Poet = "person-poet";
    private const string Court = "organisation-court";
    private const string School = "organisation-school";
    private const string Illustration = "media-dastan-family-illustration";
    private const string Video = "media-dastan-parentage-video";

    private static string Years(int from, int to) => $$"""
        {
          "kind": "period",
          "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": {{from}} } },
          "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": {{to}} } },
          "continuity": "notAsserted"
        }
        """;

    private static JsonObject Entity(string id, string type) => new()
    {
        ["id"] = id, ["type"] = type, ["label"] = "Test data " + id, ["summary"] = "Test data.",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
    };

    private static JsonObject Relation(string id, string to, string? time)
    {
        var relation = new JsonObject
        {
            ["id"] = id, ["from"] = Poet, ["to"] = to, ["type"] = "memberOf", ["label"] = "Test data",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
            ["reviewStatus"] = "source_backed_editorial_review_pending",
            ["scope"] = new JsonObject { ["kind"] = "outside_the_story", ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
            ["directionality"] = "directed",
        };
        if (time is not null)
        {
            relation["time"] = JsonNode.Parse(time);
        }

        return relation;
    }

    /// <summary>
    /// Writes onto the sample: a poet at a court from 994 to 999 and, without a time, once more; the poet at a
    /// school from 1020 to 1030; and the illustration of the site as a picture of the year 1020.
    /// </summary>
    internal static void World(JsonObject sample)
    {
        var entities = sample["entities"]!.AsArray();
        entities.Add(Entity(Poet, "Person"));
        entities.Add(Entity(Court, "Organisation"));
        entities.Add(Entity(School, "Organisation"));
        var relations = sample["relations"]!.AsArray();
        relations.Add(Relation("relation-court-dated", Court, Years(994, 999)));
        relations.Add(Relation("relation-court-undated", Court, null));
        relations.Add(Relation("relation-school", School, Years(1020, 1030)));
        sample.Record("media", Illustration)["depictedTime"] = JsonNode.Parse("""{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1020 } }""");
    }

    private static Projection Reader() => Projection.Of(SampleFiles.Read(World), new ReadingLevel(4), Audience.LocalDemonstration)!;

    private static YearSpan Span(int from, int to) => Exploration.Read(new EntityId(Poet), ExplorationView.Connections, new ReadingLevel(4),
        [KeyValuePair.Create<string, string?>("from", from.ToString(System.Globalization.CultureInfo.InvariantCulture)), KeyValuePair.Create<string, string?>("to", to.ToString(System.Globalization.CultureInfo.InvariantCulture))]).Span!.Value;

    [Fact]
    public void Without_years_the_connections_are_what_they_were()
    {
        var reader = Reader();
        var all = reader.Connections(new EntityId(Poet))!;
        var asked = reader.Connections(new EntityId(Poet), null, null, 2026)!;

        Assert.Equal(all.Edges.Select(edge => edge.Relation.Id), asked.Edges.Select(edge => edge.Relation.Id));
        Assert.Equal(3, all.Edges.Length);
        Assert.Null(all.Years);
        Assert.Equal(0, all.LeftOut);
        Assert.Equal(all.Edges, all.InYears);
        Assert.Empty(all.WithoutTime);
    }

    [Fact]
    public void In_a_stretch_of_years_the_connections_are_what_held_in_it_and_what_states_no_time()
    {
        var view = Reader().Connections(new EntityId(Poet), null, Span(990, 1000), 2026)!;

        Assert.Equal(["relation-court-dated"], view.InYears.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(["relation-court-undated"], view.WithoutTime.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(1, view.LeftOut);
        Assert.Empty(view.Possibly);

        // What is reached only by a relation of another time is no node of the drawing.
        Assert.Equal([Poet, Court], view.Nodes.Select(node => node.Id.Value));
    }

    [Fact]
    public void A_relation_that_reaches_out_of_the_years_only_may_have_held_in_them()
    {
        var view = Reader().Connections(new EntityId(Poet), null, Span(1025, 1100), 2026)!;

        Assert.Equal(["relation-school"], view.InYears.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(1, view.LeftOut);
        Assert.Contains(School, view.Nodes.Select(node => node.Id.Value));
    }

    [Fact]
    public void Years_nothing_held_in_leave_what_states_no_time_and_count_the_rest()
    {
        var view = Reader().Connections(new EntityId(Poet), null, Span(1500, 1600), 2026)!;

        Assert.Empty(view.InYears);
        Assert.Equal(["relation-court-undated"], view.Edges.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(2, view.LeftOut);

        // A node cannot be opened through a relation of another time, and is not offered to be.
        var opened = Reader().Connections(new EntityId(Poet), [new EntityId(School)], Span(1500, 1600), 2026)!;
        Assert.Empty(opened.Expanded);
        Assert.DoesNotContain(School, opened.Nodes.Select(node => node.Id.Value));
    }

    [Fact]
    public void The_pictures_of_a_name_in_a_stretch_of_years_are_what_shows_it_with_what_states_no_time_apart()
    {
        var reader = Reader();
        var rostam = new EntityId("character-rostam");
        var whole = reader.Gallery(rostam)!;
        var then = reader.Gallery(rostam, Span(1000, 1050), 2026)!;

        Assert.Equal([Illustration], then.Items.Select(item => item.Id.Value));
        Assert.Equal([Video], then.WithoutTime.Select(item => item.Id.Value));
        Assert.Equal(0, then.LeftOut);
        Assert.Equal(whole.References.Select(item => item.Id), then.References.Select(item => item.Id));
        Assert.False(then.IsEmpty);

        // Other years: the picture of 1020 is left out and counted; what states no time stays.
        var other = reader.Gallery(rostam, Span(1100, 1200), 2026)!;
        Assert.Empty(other.Items);
        Assert.Equal([Video], other.WithoutTime.Select(item => item.Id.Value));
        Assert.Equal(1, other.LeftOut);

        // Arranged like every gallery: images only leaves no video among what states no time.
        Assert.Empty(then.Arranged(GalleryOrder.OldestFirst, MediaKind.Image).WithoutTime);
        Assert.Equal([Illustration], then.Arranged(GalleryOrder.OldestFirst, MediaKind.Image).Items.Select(item => item.Id.Value));

        // Without years the gallery is what it was.
        Assert.Equal(whole.Items.Select(item => item.Id), reader.Gallery(rostam, null, 2026)!.Items.Select(item => item.Id));
        Assert.Null(reader.Gallery(new EntityId("character-nobody"), Span(1000, 1050), 2026));
    }
}
