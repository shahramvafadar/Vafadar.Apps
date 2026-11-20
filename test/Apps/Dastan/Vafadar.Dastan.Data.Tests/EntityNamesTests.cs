using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The other names of an entity and what it is called in the catalogues of others (step 12.3, DOM-003): read
/// as stated, found by the search, never shown before their stage, and a likeness of names joins nothing.
/// The records are test data.
/// </summary>
public sealed class EntityNamesTests
{
    private const string Hero = "character-rostam";

    private static Action<JsonObject> With(string names, string? identifiers = null) => sample =>
    {
        var entity = sample.Record("entities", Hero);
        entity["names"] = JsonNode.Parse(names);
        if (identifiers is not null)
        {
            entity["identifiers"] = JsonNode.Parse(identifiers);
        }
    };

    private static Projection At(Catalogue catalogue, int level) => Projection.Of(catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    [Fact]
    public void Other_names_and_identifiers_are_read_as_they_are_stated()
    {
        var catalogue = SampleFiles.Read(With(
            """
            [
              { "name": "Tahamtan", "language": "en", "kind": "title", "sourceIds": ["src-ganjoor-index"] },
              { "name": "رستم دستان", "language": "fa", "kind": "original_script" },
              { "name": "Rustam", "language": "en", "kind": "former",
                "time": { "kind": "period", "start": { "state": "unknown" }, "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1900, "approximate": true } }, "continuity": "notAsserted" } }
            ]
            """,
            """
            [
              { "issuer": "Wikidata", "value": "Q1", "url": "https://www.wikidata.org/wiki/Q1", "match": "exact" },
              { "issuer": "A library", "value": "n 123", "match": "uncertain" }
            ]
            """));
        var hero = catalogue.Find(new EntityId(Hero))!;

        Assert.Equal(["Tahamtan", "رستم دستان", "Rustam"], hero.Names.Select(name => name.Name));
        Assert.Equal([NameKind.Title, NameKind.OriginalScript, NameKind.Former], hero.Names.Select(name => name.Kind));
        Assert.Equal("fa", hero.Names[1].Language);
        Assert.IsType<Period>(hero.Names[2].Time);
        Assert.Equal(hero.Level, hero.Names[0].Level);
        Assert.Equal("src-ganjoor-index", Assert.Single(hero.Names[0].Sources).Value);
        Assert.Equal(("Wikidata", "Q1", IdentifierMatch.Exact), (hero.Identifiers[0].Issuer, hero.Identifiers[0].Value, hero.Identifiers[0].Match));
        Assert.Equal("https://www.wikidata.org/wiki/Q1", hero.Identifiers[0].Url!.AbsoluteUri);
        Assert.Null(hero.Identifiers[1].Url);
    }

    [Theory]
    [InlineData("tahamtan", "Tahamtan", "en")]
    [InlineData("دستان", "رستم دستان", "fa")]
    [InlineData("RUSTAM", "Rustam", "en")]
    public void The_search_finds_an_entity_by_any_of_its_names(string typed, string name, string language)
    {
        var catalogue = SampleFiles.Read(With(
            """
            [
              { "name": "Tahamtan", "language": "en", "kind": "title" },
              { "name": "رستم دستان", "language": "fa", "kind": "original_script" },
              { "name": "Rustam", "language": "en", "kind": "alternative" }
            ]
            """));

        var hit = Assert.Single(At(catalogue, 0).Search(typed), found => found.Entity.Id.Value == Hero);
        Assert.Equal((name, language), (hit.Name, hit.Language));
    }

    [Fact]
    public void A_name_that_gives_the_story_away_is_not_shown_and_not_found_before_its_stage()
    {
        var catalogue = SampleFiles.Read(With("""[ { "name": "The grieving father", "language": "en", "kind": "title", "spoilerLevel": 3 } ]"""));

        Assert.Empty(At(catalogue, 0).Entities.Single(entity => entity.Id.Value == Hero).Names);
        Assert.DoesNotContain(At(catalogue, 0).Search("grieving"), hit => hit.Entity.Id.Value == Hero);
        Assert.Single(At(catalogue, 3).Entities.Single(entity => entity.Id.Value == Hero).Names);
        Assert.Contains(At(catalogue, 3).Search("grieving"), hit => hit.Entity.Id.Value == Hero);
    }

    [Fact]
    public void What_a_name_or_an_identifier_must_say_is_checked()
    {
        IReadOnlyList<string> Of(string names, string? identifiers = null) => SampleFiles.Problems(With(names, identifiers));

        Assert.Contains("the name X: language English is no language tag; write en, fa, ar or the like.", Of("""[ { "name": "X", "language": "English", "kind": "alternative" } ]"""));
        Assert.Contains(Of("""[ { "name": "X", "language": "en", "kind": "nickname" } ]"""), problem => problem.StartsWith("the name X: kind", StringComparison.Ordinal));
        Assert.Contains($"{Hero}: the name X is given twice in en.", Of("""[ { "name": "X", "language": "en", "kind": "alternative" }, { "name": "X", "language": "en", "kind": "title" } ]"""));
        Assert.Contains($"{Hero}, the name X: the source src-nowhere does not exist.", Of("""[ { "name": "X", "language": "en", "kind": "alternative", "sourceIds": ["src-nowhere"] } ]"""));
        Assert.Contains(
            "the name X: time is a place in a telling: a name is not dated by a place in the story.",
            Of("""[ { "name": "X", "language": "en", "kind": "former", "time": { "kind": "occurrence", "at": { "frame": "narrative", "sequence": "story-rostam-sohrab", "order": "presentation", "ordinal": 3 } } } ]"""));
        Assert.Contains($"{Hero}: the identifier Q1 of Wikidata is given twice.", Of("[]", """[ { "issuer": "Wikidata", "value": "Q1", "match": "exact" }, { "issuer": "Wikidata", "value": "Q1", "match": "close" } ]"""));
        Assert.Contains(Of("[]", """[ { "issuer": "Wikidata", "value": "Q1", "url": "http://www.wikidata.org/wiki/Q1", "match": "exact" } ]"""), problem => problem.Contains("url", StringComparison.Ordinal));
        Assert.Contains(Of("[]", """[ { "issuer": "Wikidata", "value": "Q1" } ]"""), problem => problem.Contains("match", StringComparison.Ordinal));
    }

    [Fact]
    public void A_name_of_an_entity_of_a_later_stage_is_never_shown_before_the_entity()
    {
        var late = SampleFiles.Read().Entities.First(entity => entity.Level.Value > 0);
        var problems = SampleFiles.Problems(sample => sample.Record("entities", late.Id.Value)["names"] =
            JsonNode.Parse("""[ { "name": "X", "language": "en", "kind": "alternative", "spoilerLevel": 0 } ]"""));

        Assert.Contains($"{late.Id.Value}, the name X: a name of {late.Id.Value} cannot be shown before {late.Id.Value} itself.", problems);
    }

    [Fact]
    public void Two_entities_with_one_name_stay_two()
    {
        // DOM-003: similarity does not establish identity. Both are found; nothing is merged.
        var catalogue = SampleFiles.Read(sample =>
        {
            sample.Record("entities", Hero)["names"] = JsonNode.Parse("""[ { "name": "The champion", "language": "en", "kind": "title" } ]""");
            sample.Record("entities", "character-zal")["names"] = JsonNode.Parse("""[ { "name": "The champion", "language": "en", "kind": "title" } ]""");
        });

        var found = At(catalogue, 0).Search("champion").Select(hit => hit.Entity.Id.Value).ToHashSet();
        Assert.Superset(new HashSet<string> { Hero, "character-zal" }, found);
    }

    [Fact]
    public void The_sample_of_the_repository_has_no_other_names_and_no_identifiers()
    {
        Assert.All(PilotSample.Load(SampleFiles.PilotPath).Entities, entity => Assert.True(entity.Names.IsEmpty && entity.Identifiers.IsEmpty));
    }
}
