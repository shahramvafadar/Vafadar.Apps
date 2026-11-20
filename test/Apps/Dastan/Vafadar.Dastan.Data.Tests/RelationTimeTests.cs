using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// When a relation held (step 12.2, DOM-007): a relation of the world outside the story may say its time as it
/// is stated; two tenures are two relations; nothing of the story is dated. The records are test data.
/// </summary>
public sealed class RelationTimeTests
{
    private const string First = """
        {
          "kind": "period",
          "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 994 } },
          "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 999 } },
          "continuity": "notAsserted"
        }
        """;

    private const string Second = """
        {
          "kind": "period",
          "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1004, "approximate": true } },
          "end": { "state": "unknown" },
          "continuity": "notAsserted"
        }
        """;

    private const string Once = """{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } }""";

    private static JsonObject Entity(string id, string type) => new()
    {
        ["id"] = id, ["type"] = type, ["label"] = "Test data " + id, ["summary"] = "Test data.",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
    };

    private static JsonObject Relation(string id, string from, string to, string type, string? time, string scope = "outside_the_story")
    {
        var relation = new JsonObject
        {
            ["id"] = id, ["from"] = from, ["to"] = to, ["type"] = type, ["label"] = "Test data",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
            ["reviewStatus"] = "source_backed_editorial_review_pending",
            ["scope"] = new JsonObject { ["kind"] = scope, ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
            ["directionality"] = "directed",
        };
        if (time is not null)
        {
            relation["time"] = JsonNode.Parse(time);
        }

        return relation;
    }

    private static Action<JsonObject> World(params JsonObject[] relations) => sample =>
    {
        sample["entities"]!.AsArray().Add(Entity("person-poet", "Person"));
        sample["entities"]!.AsArray().Add(Entity("organisation-court", "Organisation"));
        foreach (var relation in relations)
        {
            sample["relations"]!.AsArray().Add(relation);
        }
    };

    [Fact]
    public void A_relation_of_the_world_says_when_it_held_as_it_is_stated()
    {
        var catalogue = SampleFiles.Read(World(
            Relation("world-1", "person-poet", "organisation-court", "memberOf", First),
            Relation("world-2", "person-poet", "work-shahnameh", "authorOf", Once),
            Relation("world-3", "person-poet", "organisation-court", "founderOf", null)));

        var held = Assert.IsType<Period>(catalogue.Find(new RelationId("world-1"))!.Time);
        Assert.Equal(994, Assert.IsType<CalendarPosition>(held.Start.Earliest).Year);
        Assert.IsType<Occurrence>(catalogue.Find(new RelationId("world-2"))!.Time);
        Assert.Null(catalogue.Find(new RelationId("world-3"))!.Time);
    }

    [Fact]
    public void Somebody_who_came_back_is_two_relations_and_nothing_joins_them()
    {
        var catalogue = SampleFiles.Read(World(
            Relation("world-1", "person-poet", "organisation-court", "memberOf", First),
            Relation("world-2", "person-poet", "organisation-court", "memberOf", Second)));
        var view = Projection.Of(catalogue, catalogue.Info.DefaultLevel, Audience.LocalDemonstration)!.Timeline();

        // Two bars in the lane of the person, the second with an end nobody states – never one bar from 994 on.
        Assert.Equal(["world-1", "world-2"], view.Held.Select(edge => edge.Relation.Id.Value));
        Assert.All(view.Held, edge => Assert.Equal("person-poet", edge.From.Id.Value));
        Assert.Contains(view.Subjects, subject => subject.Id.Value == "person-poet");
        var second = Assert.IsType<Period>(view.Held[1].Relation.Time);
        Assert.Equal(PeriodEndState.Unknown, second.End.State);
        Assert.True(Assert.IsType<CalendarPosition>(second.Start.Earliest).IsApproximate);
    }

    [Fact]
    public void Nothing_the_story_tells_is_dated()
    {
        var inStory = SampleFiles.Problems(sample => sample.Record("relations", "relation-01")["time"] = JsonNode.Parse(First));
        var byPlace = SampleFiles.Problems(World(Relation("world-1", "person-poet", "organisation-court", "memberOf",
            """{ "kind": "occurrence", "at": { "frame": "narrative", "sequence": "story-rostam-sohrab", "order": "presentation", "ordinal": 3 } }""")));
        var unreadable = SampleFiles.Problems(World(Relation("world-1", "person-poet", "organisation-court", "memberOf",
            """{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 0 } }""")));

        Assert.Equal(["relation-01: time dates a relation of the story; only a relation with the scope outside_the_story says when it held."], inStory);
        Assert.Equal(["world-1: time is a place in a telling: a relation is not dated by a place in the story."], byPlace);
        Assert.StartsWith("world-1: time cannot be read", Assert.Single(unreadable), StringComparison.Ordinal);
    }

    [Fact]
    public void A_visitor_before_the_stage_of_a_relation_does_not_learn_its_time()
    {
        var late = Relation("world-1", "person-poet", "organisation-court", "memberOf", First);
        late["spoilerLevel"] = 3;
        var catalogue = SampleFiles.Read(World(late));

        Assert.Empty(Projection.Of(catalogue, ReadingLevel.Opening, Audience.LocalDemonstration)!.Timeline().Held);
        Assert.Single(Projection.Of(catalogue, new ReadingLevel(3), Audience.LocalDemonstration)!.Timeline().Held);
    }

    [Fact]
    public void The_sample_of_the_repository_dates_no_relation()
    {
        Assert.All(PilotSample.Load(SampleFiles.PilotPath).Relations, relation => Assert.Null(relation.Time));
    }
}
