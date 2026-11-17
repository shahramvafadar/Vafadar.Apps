using System.Text.Json.Nodes;
using System.Xml.Linq;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The whole vocabulary of things and relations the product documents name (step 12.1, D-047): companies and
/// brands, governments and dynasties, sport, events and periods, offices, subjects – each type with what it
/// means, a name in both languages and, for a relation, the kinds of thing it holds between. The records here
/// are test data made from the vocabulary itself, so a type that is added is tested without a new test.
/// </summary>
public sealed class WorldVocabularyTests
{
    // Everything but the story itself, of which a collection has one.
    private static IEnumerable<EntityType> World => EntityTypes.All.Where(type => type != EntityTypes.StoryArc);

    private static IEnumerable<RelationType> Bound => RelationTypes.All.Where(type => type.Meaning is not null);

    private static string FileName(EntityType type) =>
        ContentVocabulary.Collection.Single(choices => choices.Field == "entities[].type").Values.Single(value => value.Note == type.Meaning).Name;

    private static string Id(EntityType type, bool second = false) => (second ? "other-" : "thing-") + type.Key.ToLowerInvariant();

    private static JsonObject Entity(EntityType type, bool second) => new()
    {
        ["id"] = Id(type, second),
        ["type"] = FileName(type),
        ["label"] = "Test data " + type.Key,
        ["summary"] = "Test data.",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"),
        ["spoilerLevel"] = 0,
    };

    private static JsonObject Relation(RelationType type, EntityType from, EntityType to, string? id = null) => new()
    {
        ["id"] = id ?? "world-" + type.Key.ToLowerInvariant(),
        ["from"] = Id(from),
        ["to"] = Id(to, second: true),
        ["type"] = type.Key,
        ["label"] = "Test data",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"),
        ["spoilerLevel"] = 0,
        ["reviewStatus"] = "source_backed_editorial_review_pending",
        ["scope"] = new JsonObject { ["kind"] = "outside_the_story", ["storyId"] = "story-rostam-sohrab", ["eventIds"] = new JsonArray() },
        ["directionality"] = type.Directionality == Directionality.Symmetric ? "symmetric" : "directed",
    };

    // A kind of thing that may stand at that end. Of every kind there are two things, so that a relation
    // between two of a kind has two ends.
    private static EntityType End(RelationType type, bool first) => World.First(candidate => type.Fits(candidate.Nature, first));

    private static void Things(JsonObject sample)
    {
        var entities = sample["entities"]!.AsArray();
        foreach (var type in World)
        {
            entities.Add(Entity(type, second: false));
            entities.Add(Entity(type, second: true));
        }
    }

    [Fact]
    public void A_collection_names_a_thing_of_every_kind_and_says_everything_a_relation_type_can_say()
    {
        var catalogue = SampleFiles.Read(sample =>
        {
            Things(sample);
            foreach (var type in Bound)
            {
                sample["relations"]!.AsArray().Add(Relation(type, End(type, first: true), End(type, first: false)));
            }
        });

        Assert.All(World, type => Assert.Equal(type, catalogue.Find(new EntityId(Id(type)))!.Type));
        Assert.All(Bound, type => Assert.Equal(type, catalogue.Find(new RelationId("world-" + type.Key.ToLowerInvariant()))!.Type));
    }

    [Fact]
    public void A_relation_type_is_refused_between_kinds_of_thing_it_does_not_hold_between()
    {
        foreach (var type in Bound)
        {
            foreach (var first in new[] { true, false })
            {
                if ((first ? type.From : type.To).IsDefaultOrEmpty)
                {
                    continue;
                }

                var wrong = World.First(candidate => !type.Fits(candidate.Nature, first));
                var right = End(type, !first);
                var problems = SampleFiles.Problems(sample =>
                {
                    Things(sample);
                    sample["relations"]!.AsArray().Add(first ? Relation(type, wrong, right) : Relation(type, right, wrong));
                });

                var problem = Assert.Single(problems);
                Assert.StartsWith($"world-{type.Key.ToLowerInvariant()}: {type.Key} reads {(first ? "from" : "to")} ", problem, StringComparison.Ordinal);
                Assert.Contains(Id(wrong, second: !first), problem, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Every_type_says_what_it_means_and_every_relation_of_the_world_between_what_it_holds()
    {
        Assert.All(EntityTypes.All, type => Assert.False(string.IsNullOrWhiteSpace(type.Meaning)));
        Assert.Equal(EntityTypes.All.Length, EntityTypes.All.Select(type => type.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(EntityTypes.All.Length, EntityTypes.All.Select(type => type.Meaning).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(RelationTypes.All.Length, RelationTypes.All.Select(type => type.Key).Distinct(StringComparer.Ordinal).Count());

        // Only what says "the same or not the same" and what a work is about holds between anything.
        var open = Bound.Where(type => type.From.IsDefaultOrEmpty || type.To.IsDefaultOrEmpty).Select(type => type.Key);
        Assert.Equal(["about", "confusedWith", "possiblySameAs"], open.Order(StringComparer.Ordinal));

        // Every kind of thing can be written, and stands at an end of some relation.
        Assert.All(Enum.GetValues<EntityNature>(), nature => Assert.Contains(EntityTypes.All, type => type.Nature == nature));
        Assert.All(Enum.GetValues<EntityNature>(), nature => Assert.Contains(Bound, type => !type.From.IsDefaultOrEmpty && (type.From.Contains(nature) || (!type.To.IsDefaultOrEmpty && type.To.Contains(nature)))));
    }

    [Fact]
    public void What_is_alike_in_name_stays_apart_in_meaning()
    {
        // DOM-009, DOM-010, DOM-011: a brand is no company, a team is no club, a government is no land.
        Assert.NotEqual(EntityTypes.Company.Nature, EntityTypes.Brand.Nature);
        Assert.NotEqual(EntityTypes.SportsClub, EntityTypes.SportsTeam);
        Assert.NotEqual(EntityTypes.Government.Nature, EntityTypes.Country.Nature);
        Assert.NotEqual(EntityTypes.Competition, EntityTypes.CompetitionSeason);

        // "Possibly the same" and "taken for one another" are relations between two entities, the same from both sides.
        Assert.Equal(Directionality.Symmetric, RelationTypes.Find("possiblySameAs")!.Directionality);
        Assert.Equal(Directionality.Symmetric, RelationTypes.Find("confusedWith")!.Directionality);
        Assert.Equal(Directionality.Directed, RelationTypes.Find("portrayalOf")!.Directionality);

        // An adoption is said, and is no line of a family tree by birth.
        Assert.Equal(Kinship.None, RelationTypes.Find("adoptiveParentOf")!.Kinship);
    }

    [Fact]
    public void A_thing_of_the_world_that_is_no_place_and_no_thing_is_no_setting_of_a_step()
    {
        // A dynasty may be involved in a step of the story; a country takes part as its setting only.
        var catalogue = SampleFiles.Read(sample =>
        {
            Things(sample);
            sample.Record("events", "event-01")["entityAssociations"]!.AsArray().Add(new JsonObject { ["entityId"] = Id(EntityTypes.Dynasty), ["role"] = "Test data", ["presence"] = "mentioned_only" });
        });
        var problems = SampleFiles.Problems(sample =>
        {
            Things(sample);
            sample.Record("events", "event-01")["entityAssociations"]!.AsArray().Add(new JsonObject { ["entityId"] = Id(EntityTypes.Country), ["role"] = "Test data", ["presence"] = "mentioned_only" });
        });

        Assert.NotNull(catalogue.Find(new EntityId(Id(EntityTypes.Dynasty))));
        Assert.Contains(Id(EntityTypes.Country), Assert.Single(problems), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Ui.resx")]
    [InlineData("Ui.fa.resx")]
    public void Every_kind_of_thing_has_a_name_in_the_language_of_a_page(string file)
    {
        var texts = XDocument.Load(Path.Combine(SampleFiles.App, "Vafadar.Dastan.Web", "Resources", file)).Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string)data.Element("value")!, StringComparer.Ordinal);

        Assert.All(EntityTypes.All, type => Assert.False(string.IsNullOrWhiteSpace(texts.GetValueOrDefault("Type_" + type.Key)), type.Key));
        var names = EntityTypes.All.Select(type => texts["Type_" + type.Key]).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_list_for_writers_says_what_every_kind_of_thing_is()
    {
        var kinds = ContentVocabulary.Collection.Single(choices => choices.Field == "entities[].type").Values;

        Assert.Equal(EntityTypes.All.Length, kinds.Length);
        Assert.Equal("One team of a club: the first team, a youth team, the women's team", kinds.Single(kind => kind.Name == "SportsTeam").Note);
        Assert.Equal("A story inside a work, selected as one collection", kinds.Single(kind => kind.Name == "NarrativeArc").Note);

        var types = ContentVocabulary.Collection.Single(choices => choices.Field == "relations[].type").Values.ToDictionary(value => value.Name, value => value.Note);
        Assert.Equal("directed, from → to; the work is about the second; from a work to anything", types["about"]);
        Assert.Equal("symmetric; commonly taken for one another; they are not the same", types["confusedWith"]);
        Assert.Equal("directed, from → to; the first held the office, bore the title or received the honour; from a person, character or animal or an organisation to an office or honour", types["holderOf"]);
    }
}
