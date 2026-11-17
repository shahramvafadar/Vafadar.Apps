using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// Two entities that turn out to be one (step 13.6, DOM-003): one goes into the other with everything that
/// named it, its name stays findable, every touched record has its change in the history – and a merge the
/// collection could not bear is refused and changes nothing. The records are test data.
/// </summary>
public sealed class EntityMergeTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private static JsonObject Entity(string id, string type, string label) => new()
    {
        ["id"] = id, ["type"] = type, ["label"] = label, ["summary"] = "Test data.",
        ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
    };

    private static JsonObject Relation(string id, string from, string to, string type) => new()
    {
        ["id"] = id, ["from"] = from, ["to"] = to, ["type"] = type, ["label"] = "Test data", ["sourceIds"] = new JsonArray("src-ganjoor-index"),
        ["spoilerLevel"] = 0, ["reviewStatus"] = "source_backed_editorial_review_pending",
        ["scope"] = new JsonObject { ["kind"] = "outside_the_story", ["eventIds"] = new JsonArray() }, ["directionality"] = "directed",
    };

    // A poet under two names: one record wrote the work, the other was born in a city.
    private EditorialStore WithTwoPoets()
    {
        var store = _copy.Filled();
        var ferdowsi = Entity("person-ferdowsi", "Person", "Ferdowsi");
        ferdowsi["identifiers"] = JsonNode.Parse("""[ { "issuer": "A catalogue", "value": "F-1", "match": "exact" } ]""");
        var abolqasem = Entity("person-abolqasem", "Person", "Abolqasem of Tus");
        abolqasem["names"] = JsonNode.Parse("""[ { "name": "The sage of Tus", "language": "en", "kind": "title" } ]""");
        abolqasem["identifiers"] = JsonNode.Parse("""[ { "issuer": "A catalogue", "value": "F-1", "match": "exact" }, { "issuer": "Another", "value": "A-9", "match": "close" } ]""");
        abolqasem["dated"] = JsonNode.Parse(
            """
            [ { "key": "abolqasem-born", "label": "Born", "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 940, "approximate": true } },
                "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "proposed" } ]
            """);
        foreach (var record in new[] { ferdowsi, abolqasem, Entity("place-tus", "Place", "Tus") })
        {
            Assert.True(store.Put(Collection, "entities", record, "a writer", "Test data.").Done);
        }

        Assert.True(store.Put(Collection, "relations", Relation("world-wrote", "person-ferdowsi", "work-shahnameh", "authorOf"), "a writer", "Test data.").Done);
        Assert.True(store.Put(Collection, "relations", Relation("world-born", "person-abolqasem", "place-tus", "bornIn"), "a writer", "Test data.",
            new Dictionary<string, JsonObject> { ["fa"] = new JsonObject { ["label"] = "زادهٔ" } }).Done);
        Assert.True(store.Put(Collection, "entities", WorkingCopy.Stored(store, "entities", "person-abolqasem"), "a translator", "Test data.",
            new Dictionary<string, JsonObject> { ["fa"] = new JsonObject { ["label"] = "ابوالقاسم طوسی" } }).Done);
        return store;
    }

    [Fact]
    public void One_entity_goes_into_the_other_with_everything_that_named_it()
    {
        using var store = WithTwoPoets();

        var result = store.MergeEntities(Collection, "person-ferdowsi", "person-abolqasem", "an editor", "Both are the poet of the work.");

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Null(store.Record(Collection, "entities", "person-abolqasem"));
        Assert.Equal("person-ferdowsi", (string?)store.Record(Collection, "relations", "world-born")!.Body["from"]);
        Assert.Equal("person-ferdowsi", (string?)store.Record(Collection, "relations", "world-wrote")!.Body["from"]);

        var kept = store.Record(Collection, "entities", "person-ferdowsi")!.Body;
        Assert.Equal("Ferdowsi", (string?)kept["label"]);
        Assert.Equal(
            ["Abolqasem of Tus/en/alternative", "ابوالقاسم طوسی/fa/alternative", "The sage of Tus/en/title"],
            kept["names"]!.AsArray().Select(name => $"{(string?)name!["name"]}/{(string?)name["language"]}/{(string?)name["kind"]}"));
        Assert.Equal("abolqasem-born", (string?)kept["dated"]![0]!["key"]);

        // An identifier both had is there once; the other one came over.
        Assert.Equal(["F-1", "A-9"], kept["identifiers"]!.AsArray().Select(identifier => (string?)identifier!["value"]));

        // The texts that translated the merged record are gone with it; the collection still reads.
        Assert.Empty(store.Texts(Collection, "entities", "person-abolqasem").Single().Texts);
        Assert.NotNull(store.Export(Collection));
    }

    [Fact]
    public void Every_record_a_merge_touches_has_its_change_with_who_and_why_and_the_record_before()
    {
        using var store = WithTwoPoets();
        _copy.Clock.Advance(TimeSpan.FromMinutes(1));

        var result = store.MergeEntities(Collection, "person-ferdowsi", "person-abolqasem", "an editor", "Both are the poet of the work.");

        const string Why = "Both are the poet of the work. (person-abolqasem merged into person-ferdowsi)";
        var gone = store.History(Collection, "entities", "person-abolqasem")[^1];
        var kept = store.History(Collection, "entities", "person-ferdowsi")[^1];
        var relation = store.History(Collection, "relations", "world-born")[^1];
        Assert.Equal(4, result.Changed);
        Assert.Equal((RecordChangeKind.Removed, "an editor", Why), (gone.Kind, gone.By, gone.Reason));
        Assert.Contains("Abolqasem of Tus", gone.Before, StringComparison.Ordinal);
        Assert.Equal((RecordChangeKind.Changed, Why), (kept.Kind, kept.Reason));
        Assert.Equal((RecordChangeKind.Changed, Why), (relation.Kind, relation.Reason));
        Assert.Contains("person-abolqasem", relation.Before, StringComparison.Ordinal);

        // The relation that never named the merged entity was not touched.
        Assert.Single(store.History(Collection, "relations", "world-wrote"));
    }

    [Fact]
    public void A_merge_the_collection_could_not_bear_is_refused_and_changes_nothing()
    {
        using var store = WithTwoPoets();
        store.Put(Collection, "relations", Relation("world-taught", "person-ferdowsi", "person-abolqasem", "teacherOf"), "a writer", "Test data.");
        var changes = store.History(Collection).Length;

        var itself = store.MergeEntities(Collection, "person-ferdowsi", "person-ferdowsi", "an editor", "x");
        var unknown = store.MergeEntities(Collection, "person-ferdowsi", "person-nobody", "an editor", "x");
        var nowhere = store.MergeEntities(Collection, "person-nobody", "person-ferdowsi", "an editor", "x");
        var related = store.MergeEntities(Collection, "person-ferdowsi", "person-abolqasem", "an editor", "x");
        var together = store.MergeEntities(Collection, "character-rostam", "character-tahmineh", "an editor", "x");
        var otherKind = store.MergeEntities(Collection, "place-tus", "person-ferdowsi", "an editor", "x");

        Assert.Equal(["an entity is not merged into itself."], itself.Problems);
        Assert.Equal(["person-nobody: the collection has no entity of that name."], unknown.Problems);
        Assert.Equal(["person-nobody: the collection has no entity of that name."], nowhere.Problems);
        Assert.Equal(["world-taught: after the merge the relation would connect person-ferdowsi with itself; change the relation or take it out first."], related.Problems);
        Assert.False(together.Done);
        Assert.Contains(together.Problems, problem => problem.Contains("with itself", StringComparison.Ordinal) || problem.Contains("both entities take part in this step", StringComparison.Ordinal));

        // A person is no place: what was said of the person does not hold of a city, and the readers say so.
        Assert.False(otherKind.Done);
        Assert.Contains(otherKind.Problems, problem => problem.StartsWith("world-wrote: authorOf reads from", StringComparison.Ordinal));
        Assert.Equal(changes, store.History(Collection).Length);
        Assert.NotNull(store.Record(Collection, "entities", "person-abolqasem"));
    }

    [Fact]
    public void A_step_follows_the_entity_that_stays_also_in_the_other_language()
    {
        using var store = _copy.Filled();
        Assert.True(store.Put(Collection, "entities", Entity("character-horse", "AnimalCharacter", "The horse of the tests"), "a writer", "Test data.").Done);
        var step = WorkingCopy.Stored(store, "events", "event-01");
        var parts = step["entityAssociations"]!.AsArray();
        var who = (string)parts[0]!["entityId"]!;
        var role = (string?)store.Texts(Collection, "events", "event-01").Single().Texts["roles"]?[who];

        var result = store.MergeEntities(Collection, "character-horse", who, "an editor", "The same horse.");

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal("character-horse", (string?)store.Record(Collection, "events", "event-01")!.Body["entityAssociations"]![0]!["entityId"]);
        Assert.Equal(role, (string?)store.Texts(Collection, "events", "event-01").Single().Texts["roles"]?["character-horse"]);
        Assert.Null(store.Texts(Collection, "events", "event-01").Single().Texts["roles"]?[who]);
    }
}
