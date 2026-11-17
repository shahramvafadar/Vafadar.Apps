using System.Net;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// A new record and a new language without a file (step 13.5): a record of a kind starts as a form with the
/// fields of that kind; a language the collection is not translated into yet is added in the tool.
/// </summary>
public sealed class StudioNewRecordTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private static readonly (string, string?)[] WhoAndWhy = [("Adding.By", "a writer"), ("Adding.Reason", "New.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioNewRecordTests() => _desk = new Desk(_studio);

    public void Dispose() => _studio.Dispose();

    private static string New(string group) => $"/en/{Collection}/records/{group}/new";

    private static Dictionary<string, string> Inputs(StudioPage page) =>
        Desk.Standing(page.Html).GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => input.First().Value, StringComparer.Ordinal);

    [Theory]
    [InlineData("en", "A new record – Names")]
    [InlineData("fa", "رکورد تازه")]
    public async Task The_form_of_a_new_entity_has_its_identifier_and_the_fields_of_its_kind(string language, string title)
    {
        var page = await _desk.Get($"/{language}/{Collection}/records/entities/new");
        var list = await _desk.Get($"/{language}/{Collection}/records/entities");
        var inputs = Inputs(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Text, StringComparison.Ordinal);
        Assert.Equal((string.Empty, "Person", string.Empty, string.Empty, "0"), (inputs["id"], inputs["f.type"], inputs["f.label"], inputs["f.summary"], inputs["f.spoilerLevel"]));
        Assert.Contains("name=\"f.sourceIds\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"{Collection}/records/entities/new", list.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_new_entity_is_kept_with_who_and_why_and_its_page_says_so()
    {
        var page = await _desk.Fill(New("entities"), "new",
            [("id", "person-poet"), ("f.label", "A poet of the tests"), ("f.summary", "Test data."), ("f.sourceIds", "src-ganjoor-index"), .. WhoAndWhy]);

        Assert.EndsWith("record/entities/person-poet?done=added", page.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        var record = store.Record(Collection, "entities", "person-poet")!;
        Assert.Equal(("Person", "A poet of the tests", "a writer"), ((string?)record.Body["type"], (string?)record.Body["label"], record.Info.UpdatedBy));
        Assert.Equal("New.", store.History(Collection, "entities", "person-poet").Single().Reason);
    }

    [Fact]
    public async Task A_record_without_an_identifier_with_one_that_is_taken_or_that_the_site_cannot_read_is_refused_with_the_form_kept()
    {
        var nameless = await _desk.Fill(New("entities"), "new", [("f.label", "Kept in the form"), .. WhoAndWhy]);
        var taken = await _desk.Fill(New("entities"), "new", [("id", "character-rostam"), ("f.label", "Kept in the form"), .. WhoAndWhy]);
        var unsourced = await _desk.Fill(New("entities"), "new", [("id", "person-poet"), ("f.label", "Kept in the form"), ("f.summary", "Test data."), .. WhoAndWhy]);

        foreach (var page in new[] { nameless, taken, unsourced })
        {
            Assert.DoesNotContain("?done=", page.Arrived ?? string.Empty, StringComparison.Ordinal);
            Assert.Equal("Kept in the form", Inputs(page)["f.label"]);
        }

        Assert.Contains("character-rostam", taken.Text, StringComparison.Ordinal);
        Assert.Contains("no source supports the record", unsourced.Text, StringComparison.Ordinal);
        Assert.Equal("person-poet", Inputs(unsourced)["id"]);
        using var store = _studio.Store();
        Assert.Null(store.Record(Collection, "entities", "person-poet"));
    }

    [Fact]
    public async Task A_new_relation_of_the_world_is_added_with_the_time_it_held()
    {
        await _desk.Fill(New("entities"), "new", [("id", "person-poet"), ("f.label", "A poet of the tests"), ("f.summary", "Test data."), ("f.sourceIds", "src-ganjoor-index"), .. WhoAndWhy]);
        var start = Inputs(await _desk.Get(New("relations")));

        var page = await _desk.Fill(New("relations"), "new",
            [("id", "world-written"), ("f.from", "person-poet"), ("f.to", "work-shahnameh"), ("f.type", "authorOf"), ("f.label", "Author of"),
             ("f.sourceIds", "src-ganjoor-index"), ("f.time.kind", "occurrence"), ("f.time.at.year", "1010"), .. WhoAndWhy]);

        Assert.Equal(("memberOf", "directed", "outside_the_story", "none"), (start["f.type"], start["f.directionality"], start["f.scope.kind"], start["f.time.kind"]));
        Assert.EndsWith("record/relations/world-written?done=added", page.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        var relation = store.Record(Collection, "relations", "world-written")!.Body;
        Assert.Equal(("authorOf", "source_backed_editorial_review_pending", 1010), ((string?)relation["type"], (string?)relation["reviewStatus"], (int)relation["time"]!["at"]!["year"]!));
        Assert.Contains(store.Statements(Collection), statement => statement.Statement == "world-written");
    }

    [Fact]
    public async Task A_new_step_of_the_story_starts_after_the_last_one()
    {
        using var store = _studio.Store();
        var steps = store.Records(Collection, "events").Length;
        var start = Inputs(await _desk.Get(New("events")));

        var page = await _desk.Fill(New("events"), "new",
            [("id", "event-new"), ("f.label", "A further step"), ("f.summary", "Test data."), ("f.sourceIds", "src-ganjoor-index"),
             ("f.spoilerLevel", store.Stages(Collection)[^1].Level.ToString(System.Globalization.CultureInfo.InvariantCulture)),
             ("f.entityAssociations[0].entityId", "character-rostam"), ("f.entityAssociations[0].role", "Test data"), .. WhoAndWhy]);

        Assert.Equal((steps + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), start["f.order"]);
        Assert.EndsWith("record/events/event-new?done=added", page.Arrived, StringComparison.Ordinal);
        Assert.Equal(steps + 1, store.Records(Collection, "events").Length);
    }

    [Fact]
    public async Task A_group_that_is_none_and_a_collection_that_is_not_there_have_no_such_form()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _desk.Get(New("things"))).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await _desk.Get("/en/no-collection/records/entities/new")).Status);
    }

    [Fact]
    public async Task A_language_is_added_to_a_collection_and_its_texts_are_then_filled_in_beside_the_fields()
    {
        var added = await _desk.Fill($"/en/{Collection}", "language", ("language", "de"), ("Adding.By", "a translator"), ("Adding.Reason", "German begins."));
        var record = Inputs(await _desk.Get($"/en/{Collection}/record/entities/character-rostam"));
        var filled = await _desk.Fill($"/en/{Collection}/record/entities/character-rostam", "fields", ("t.de.label", "Rostam auf Deutsch"), ("Filling.By", "a translator"), ("Filling.Reason", "The first German text."));

        Assert.EndsWith($"{Collection}?done=language", added.Arrived, StringComparison.Ordinal);
        Assert.Contains("The language was added.", added.Text, StringComparison.Ordinal);
        Assert.Equal(string.Empty, record["t.de.label"]);
        Assert.EndsWith("?done=changed", filled.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal(["de", "fa"], store.Summary(Collection)!.Languages);
        Assert.Equal("Rostam auf Deutsch", (string?)store.Texts(Collection, "entities", "character-rostam").Single(text => text.Language == "de").Texts["label"]);
        Assert.Equal(("a translator", "German begins."), (store.History(Collection, EditorialStore.TranslationGroup, "de")[0].By, store.History(Collection, EditorialStore.TranslationGroup, "de")[0].Reason));
    }

    [Theory]
    [InlineData("fa", "already has texts in that language")]
    [InlineData("the language of poets", "language")]
    [InlineData("en", "language")]
    public async Task A_language_the_collection_has_or_that_is_none_is_refused(string language, string problem)
    {
        var page = await _desk.Fill($"/en/{Collection}", "language", ("language", language), ("Adding.By", "a translator"), ("Adding.Reason", "A try."));

        Assert.DoesNotContain("?done=", page.Arrived ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(problem, page.Text, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal(["fa"], store.Summary(Collection)!.Languages);
    }
}
