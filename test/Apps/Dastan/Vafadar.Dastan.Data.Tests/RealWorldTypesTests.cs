using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// More than narrative things in a collection (step 10.6): a real person, an organisation, a real place and
/// region, and the relations of the world outside the story – each with its meaning and with the kinds of thing
/// it holds between. The records here are test data put into a copy of the sample in memory; the sample itself
/// has none of them.
/// </summary>
public sealed class RealWorldTypesTests
{
    private static JsonObject Entity(string id, string type, string label) => new()
    {
        ["id"] = id,
        ["type"] = type,
        ["label"] = label,
        ["summary"] = "Test data.",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"),
        ["spoilerLevel"] = 0,
    };

    private static JsonObject Relation(string id, string from, string to, string type, string directionality = "directed", string scope = "outside_the_story") => new()
    {
        ["id"] = id,
        ["from"] = from,
        ["to"] = to,
        ["type"] = type,
        ["label"] = "Test data",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"),
        ["spoilerLevel"] = 0,
        ["reviewStatus"] = "source_backed_editorial_review_pending",
        ["scope"] = new JsonObject { ["kind"] = scope, ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
        ["directionality"] = directionality,
    };

    // A poet, his teacher and brother, a court, a city and a land – with what is said about them.
    private static void World(JsonObject sample)
    {
        var entities = sample["entities"]!.AsArray();
        entities.Add(Entity("person-poet", "Person", "A poet"));
        entities.Add(Entity("person-teacher", "Person", "A teacher"));
        entities.Add(Entity("person-brother", "Person", "A brother"));
        entities.Add(Entity("organisation-court", "Organisation", "A court"));
        entities.Add(Entity("place-city", "Place", "A city"));
        entities.Add(Entity("region-land", "Region", "A land"));

        var relations = sample["relations"]!.AsArray();
        relations.Add(Relation("world-01", "person-poet", "work-shahnameh", "authorOf"));
        relations.Add(Relation("world-02", "person-poet", "place-city", "bornIn"));
        relations.Add(Relation("world-03", "person-poet", "region-land", "diedIn"));
        relations.Add(Relation("world-04", "person-teacher", "person-poet", "teacherOf"));
        relations.Add(Relation("world-05", "person-poet", "person-brother", "siblingOf", "symmetric"));
        relations.Add(Relation("world-06", "person-teacher", "organisation-court", "memberOf"));
        relations.Add(Relation("world-07", "person-teacher", "organisation-court", "founderOf"));
        relations.Add(Relation("world-08", "place-city", "region-land", "locatedIn"));
        relations.Add(Relation("world-09", "organisation-court", "place-city", "locatedIn"));
        relations.Add(Relation("world-10", "organisation-court", "work-shahnameh", "authorOf"));
    }

    [Fact]
    public void A_collection_tells_of_persons_organisations_and_real_places_beside_its_story()
    {
        var catalogue = SampleFiles.Read(World);

        Assert.Equal(EntityTypes.Person, catalogue.Find(new EntityId("person-poet"))!.Type);
        Assert.Equal(EntityNature.Being, catalogue.Find(new EntityId("person-poet"))!.Type.Nature);
        Assert.Equal(EntityNature.Group, catalogue.Find(new EntityId("organisation-court"))!.Type.Nature);
        Assert.Equal(EntityTypes.Place, catalogue.Find(new EntityId("place-city"))!.Type);
        Assert.Equal(EntityNature.Place, catalogue.Find(new EntityId("region-land"))!.Type.Nature);

        var written = catalogue.Find(new RelationId("world-01"))!;
        Assert.Equal(("authorOf", RelationScopeKind.OutsideTheStory), (written.Type.Key, written.Scope.Kind));
        Assert.Equal(Directionality.Symmetric, catalogue.Find(new RelationId("world-05"))!.Type.Directionality);

        // The story is still the one story of the collection, and a brother is not drawn into a family tree.
        Assert.Equal("story-rostam-sohrab", catalogue.Info.Story.Value);
        Assert.Equal(Kinship.None, catalogue.Find(new RelationId("world-05"))!.Type.Kinship);
    }

    [Theory]
    [InlineData("place-city", "region-land", "bornIn", "world-x: bornIn reads from somebody; place-city is a place.")]
    [InlineData("person-poet", "person-teacher", "bornIn", "world-x: bornIn reads to a place; person-teacher is somebody.")]
    [InlineData("person-poet", "character-rostam", "authorOf", "world-x: authorOf reads to a work; character-rostam is somebody.")]
    [InlineData("person-poet", "place-city", "memberOf", "world-x: memberOf reads to a group; place-city is a place.")]
    [InlineData("person-poet", "work-shahnameh", "founderOf", "world-x: founderOf reads to a group or a place; work-shahnameh is a work.")]
    [InlineData("work-shahnameh", "region-land", "locatedIn", "world-x: locatedIn reads from a place or a group; work-shahnameh is a work.")]
    [InlineData("person-poet", "organisation-court", "teacherOf", "world-x: teacherOf reads to somebody; organisation-court is a group.")]
    public void A_relation_between_the_wrong_kinds_of_thing_is_refused(string from, string to, string type, string expected)
    {
        var problems = SampleFiles.Problems(sample =>
        {
            World(sample);
            sample["relations"]!.AsArray().Add(Relation("world-x", from, to, type));
        });

        Assert.Equal([expected], problems);
    }

    [Fact]
    public void A_character_of_the_story_may_stand_where_somebody_stands()
    {
        // A relation type says what kind of thing, not whether it is of the story: Zal taught Rostam.
        var catalogue = SampleFiles.Read(sample => sample["relations"]!.AsArray().Add(Relation("world-x", "character-zal", "character-rostam", "teacherOf", scope: "background_lineage")));

        Assert.Equal("teacherOf", catalogue.Find(new RelationId("world-x"))!.Type.Key);
    }

    [Fact]
    public void The_relation_types_of_the_story_hold_between_whatever_they_held_between()
    {
        Assert.All(
            RelationTypes.All.Where(type => type.Meaning is null),
            type => Assert.True(type.From.IsDefaultOrEmpty && type.To.IsDefaultOrEmpty && type.Fits(EntityNature.Occasion, first: true)));
        Assert.Equal(17, RelationTypes.All.Count(type => type.Meaning is null));
    }

    [Fact]
    public void A_real_place_is_where_a_step_of_the_story_takes_place_or_it_is_refused_like_a_place_of_the_story()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            World(sample);
            sample.Record("events", "event-01")["entityAssociations"]!.AsArray().Add(new JsonObject { ["entityId"] = "place-city", ["role"] = "Test data", ["presence"] = "acts_in_selected_scenes" });
        });

        Assert.Single(problems);
        Assert.Contains("place-city", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void The_list_for_writers_says_what_a_new_relation_type_means_and_between_what_it_holds()
    {
        var types = ContentVocabulary.Collection.Single(choices => choices.Field == "relations[].type").Values.ToDictionary(value => value.Name, value => value.Note);
        var kinds = ContentVocabulary.Collection.Single(choices => choices.Field == "entities[].type").Values.Select(value => value.Name);
        var scopes = ContentVocabulary.Collection.Single(choices => choices.Field == "relations[].scope.kind").Values.Select(value => value.Name);

        Assert.Equal("directed, from → to; the place or region of birth; from a person, character or animal to a place or region", types["bornIn"]);
        Assert.Equal("symmetric; brothers or sisters; a family tree is drawn from parents, not from this; from a person, character or animal to a person, character or animal", types["siblingOf"]);
        Assert.Equal("directed, from → to", types["rides"]);
        Assert.Superset(new HashSet<string> { "Person", "Organisation", "Place", "Region" }, kinds.ToHashSet());
        Assert.Contains("outside_the_story", scopes);
    }
}
