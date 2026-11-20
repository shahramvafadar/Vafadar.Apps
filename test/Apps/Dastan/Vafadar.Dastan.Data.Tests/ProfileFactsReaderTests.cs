using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The profile of a node as a record writes it (step 19.5, ADR 0017): the facts of its kind and its picture
/// are read as stated, a fact says exactly what its kind of fact takes, and the table of the facts in the
/// product documents is the table of the code. The records are test data.
/// </summary>
public sealed partial class ProfileFactsReaderTests
{
    private const string Hero = "character-rostam";

    private static Action<JsonObject> With(string facts, string? picture = null) => sample =>
    {
        var entity = sample.Record("entities", Hero);
        entity["facts"] = JsonNode.Parse(facts);
        if (picture is not null)
        {
            entity["picture"] = picture;
        }
    };

    [Fact]
    public void Facts_and_a_picture_are_read_as_they_are_stated()
    {
        var catalogue = SampleFiles.Read(With(
            """
            [
              { "fact": "role", "text": "The champion of Iran", "sourceIds": ["src-ganjoor-index"] },
              { "fact": "origin", "entityId": "place-zabul", "spoilerLevel": 1 }
            ]
            """,
            "media-met-452419"));
        var hero = catalogue.Find(new EntityId(Hero))!;

        Assert.Equal(["role", "origin"], hero.Facts.Select(fact => fact.Kind.Key));
        Assert.Equal("The champion of Iran", hero.Facts[0].Words!.In("en")!.Value.Value);
        Assert.Equal("src-ganjoor-index", Assert.Single(hero.Facts[0].Sources).Value);
        Assert.Equal(hero.Level, hero.Facts[0].Level);
        Assert.Equal("place-zabul", hero.Facts[1].Node!.Value.Value);
        Assert.Equal(1, hero.Facts[1].Level.Value);
        Assert.Empty(hero.Facts[1].Sources);
        Assert.Equal("media-met-452419", hero.Picture!.Value.Value);
    }

    [Fact]
    public void An_entity_without_a_profile_reads_as_before()
    {
        var hero = SampleFiles.Read().Find(new EntityId("character-zal"))!;

        Assert.Empty(hero.Facts);
        Assert.Null(hero.Picture);
    }

    [Fact]
    public void A_time_is_read_as_an_expression_of_time_and_may_be_unknown()
    {
        var catalogue = SampleFiles.Read(sample =>
        {
            var entities = sample["entities"]!.AsArray();
            entities.Add(JsonNode.Parse(
                """
                {
                  "id": "person-poet", "type": "Person", "label": "A poet", "summary": "A poet of the tests.",
                  "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0,
                  "facts": [
                    { "fact": "born", "time": { "kind": "occurrence", "at": { "frame": "calendar", "calendar": "gregorian", "era": "commonEra", "year": 940, "approximate": true } } },
                    { "fact": "died", "time": { "kind": "unknown" } },
                    { "fact": "knownFor", "text": "A poet" }
                  ]
                }
                """));
        });
        var poet = catalogue.Find(new EntityId("person-poet"))!;

        Assert.IsType<Occurrence>(poet.Facts[0].Time);
        Assert.IsType<UnknownTime>(poet.Facts[1].Time);
    }

    [Theory]
    [InlineData("""[{ "fact": "born", "time": { "kind": "unknown" } }]""", "character-rostam, the fact born: a character has no such fact; it has role, origin.")]
    [InlineData("""[{ "fact": "role" }]""", "character-rostam, the fact role: text is missing.")]
    [InlineData("""[{ "fact": "role", "text": "A hero", "entityId": "place-zabul" }]""", "character-rostam, the fact role: this fact is said in words.")]
    [InlineData("""[{ "fact": "origin", "text": "Zabul" }]""", "character-rostam, the fact origin: entityId is missing.")]
    [InlineData("""[{ "fact": "origin", "entityId": "place-zabul", "since": 3 }]""", "character-rostam, the fact origin: the field since is not known to this reader.")]
    [InlineData("""[{ "fact": "origin", "entityId": "character-sohrab" }]""", "character-rostam, the fact origin: it names character-sohrab, which is no place.")]
    [InlineData("""[{ "fact": "role", "text": "A hero" }, { "fact": "role", "text": "A champion" }]""", "character-rostam: the fact role is stated 2 times; it is stated once.")]
    public void A_fact_that_does_not_fit_its_kind_refuses_the_collection(string facts, string problem)
    {
        Assert.Contains(problem, SampleFiles.Problems(With(facts)));
    }

    [Fact]
    public void A_picture_that_is_no_media_item_of_the_entity_refuses_the_collection()
    {
        Assert.Contains("character-rostam: its picture media-nothing is no media item of the collection.", SampleFiles.Problems(With("[]", "media-nothing")));
        Assert.Contains(
            "character-rakhsh: its picture media-met-453265 does not say that it depicts character-rakhsh.",
            SampleFiles.Problems(sample => sample.Record("entities", "character-rakhsh")["picture"] = "media-met-453265"));
    }

    [Fact]
    public void The_table_of_the_product_documents_is_the_table_of_the_code()
    {
        var document = File.ReadAllText(Path.Combine(SampleFiles.App, "docs", "product", "03-domain-model-and-ontology.md"));

        static string Keys(IEnumerable<FactKind> facts) => facts.Any() ? string.Join(", ", facts.Select(fact => $"`{fact.Key}`")) : "–";
        var kinds = Rows(document, "facts-of-kinds");
        Assert.Equal(
            EntityTypes.All.Select(type => $"| `{type.Key}` | {Keys(ProfileFacts.Of(type).Essential)} | {Keys(ProfileFacts.Of(type).Optional)} |").Order(StringComparer.Ordinal),
            kinds.Order(StringComparer.Ordinal));

        static string Says(FactKind fact) => (fact.Value switch { FactValue.Time => "a time", FactValue.Place => "a place", FactValue.Node => "another node", _ => "words" }) + (fact.Many ? ", several" : string.Empty);
        Assert.Equal(
            ProfileFacts.All.Select(fact => $"| `{fact.Key}` | {Says(fact)} | {fact.Meaning} |"),
            Rows(document, "kinds-of-fact"));
    }

    // The rows of a table of the document between its two marks, without the head.
    private static List<string> Rows(string document, string mark)
    {
        var table = Regex.Match(document, $"<!-- {mark} -->(.*?)<!-- /{mark} -->", RegexOptions.Singleline);
        Assert.True(table.Success, mark);
        return [.. table.Groups[1].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(line => line.StartsWith("| `", StringComparison.Ordinal))];
    }
}
