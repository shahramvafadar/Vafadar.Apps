using System.Net;
using System.Text.Json.Nodes;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// A comparison as a form (step 13.6b), and the lists of an entity that begin with their first row: what a
/// comparison says as a whole, its lanes one at a time with their periods and events as rows, each with the
/// inputs of its time.
/// </summary>
public sealed class StudioComparisonFormTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Id = "iran-china-rome";
    private const string Page = $"/en/comparisons/{Id}";
    private const string China = $"/en/comparisons/{Id}/lanes/china";
    private static readonly (string, string?)[] Filled = [("Filling.By", "a writer"), ("Filling.Reason", "Filled in.")];
    private static readonly (string, string?)[] Kept = [("Keeping.By", "a writer"), ("Keeping.Reason", "Filled in.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioComparisonFormTests()
    {
        _desk = new Desk(_studio);
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan", "content", "comparisons"));
        using var store = _studio.Store();
        Assert.True(store.ImportComparison(Path.Combine(folder, $"{Id}.json"), "an editor").Done);
    }

    public void Dispose() => _studio.Dispose();

    private static Dictionary<string, string> Inputs(StudioPage page, string form)
    {
        var markup = page.Html[page.Html.IndexOf($"name=\"_handler\" value=\"{form}\"", StringComparison.Ordinal)..];
        markup = markup[..markup.IndexOf("</form>", StringComparison.Ordinal)];
        return Desk.Standing(markup).GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => input.First().Value, StringComparer.Ordinal);
    }

    private JsonObject File()
    {
        using var store = _studio.Store();
        return JsonNode.Parse(store.Comparison(Id)!.Json)!.AsObject();
    }

    private JsonObject Persian()
    {
        using var store = _studio.Store();
        return JsonNode.Parse(store.Comparison(Id)!.Translations["fa"])!.AsObject();
    }

    private static JsonObject Lane(JsonObject file, string key) => file["lanes"]!.AsArray().Single(lane => (string?)lane!["key"] == key)!.AsObject();

    [Fact]
    public async Task A_comparison_is_released_to_the_site_and_withdrawn_on_its_page()
    {
        var before = await _desk.Get(Page);
        Assert.Contains("The site does not show this comparison.", before.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"_handler\" value=\"withdraw\"", before.Html, StringComparison.Ordinal);

        var released = await _desk.Submit(Page, "release", ("Releasing.By", "a publisher"));
        Assert.EndsWith("?done=comparisonreleased", released.Arrived, StringComparison.Ordinal);
        Assert.Contains("The site shows this comparison as it is here.", released.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"_handler\" value=\"release\"", released.Html, StringComparison.Ordinal);

        var withdrawn = await _desk.Submit(Page, "withdraw", ("Withdrawing.By", "a publisher"));
        Assert.EndsWith("?done=comparisonwithdrawn", withdrawn.Arrived, StringComparison.Ordinal);
        Assert.Contains("The site does not show this comparison.", withdrawn.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_a_comparison_says_as_a_whole_is_a_form_and_sent_as_it_stands_changes_nothing()
    {
        var page = await _desk.Get(Page);
        var inputs = Inputs(page, "fields");
        var same = await _desk.Fill(Page, "fields", Filled);

        Assert.Equal((string?)File()["title"], inputs["f.title"]);
        Assert.Equal("commonEraYears", inputs["f.frame"]);
        Assert.Equal("draft", inputs["f.status"]);
        Assert.DoesNotContain("f.lanes", inputs.Keys);
        Assert.Contains($"comparisons/{Id}/lanes/china", page.Html, StringComparison.Ordinal);
        Assert.Contains($"comparisons/{Id}/new-lane", page.Html, StringComparison.Ordinal);
        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_title_is_changed_and_a_first_source_is_added_and_can_then_be_pointed_to()
    {
        var page = await _desk.Fill(Page, "fields",
            [("f.title", "Three lands side by side"), ("f.sources[0].id", "src-first"), ("f.sources[0].title", "A source of the tests"), ("f.sources[0].url", "https://example.org/source"), ("f.sources[0].accessedAt", "2026-11-17"), .. Filled]);
        var lane = Inputs(await _desk.Get(China), "lane");
        var pointed = await _desk.Fill(China, "lane", [("f.periods[0].sourceIds", "src-first"), .. Kept]);

        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Equal("Three lands side by side", (string?)File()["title"]);
        Assert.Equal("src-first", (string?)File()["sources"]![0]!["id"]);
        Assert.DoesNotContain("f.periods[0].sourceIds", lane.Keys);
        Assert.EndsWith("?done=changed", pointed.Arrived, StringComparison.Ordinal);
        Assert.Equal("src-first", (string?)Lane(File(), "china")["periods"]![0]!["sourceIds"]![0]);
    }

    [Fact]
    public async Task A_lane_is_a_form_with_its_periods_and_events_as_rows_with_the_inputs_of_their_time()
    {
        var before = Lane(File(), "china");
        var inputs = Inputs(await _desk.Get(China), "lane");
        var same = await _desk.Fill(China, "lane", Kept);

        Assert.Equal((string?)before["label"], inputs["f.label"]);
        Assert.Equal((string?)before["periods"]![0]!["key"], inputs["f.periods[0].key"]);
        Assert.Equal("period", inputs["f.periods[0].time.kind"]);
        Assert.Equal("beforeCommonEra", inputs["f.periods[0].time.at.era"]);
        Assert.Equal("770", inputs["f.periods[0].time.at.year"]);
        Assert.Equal("proposed", inputs["f.periods[0].reviewStatus"]);
        Assert.Equal("occurrence", inputs["f.events[0].time.kind"]);
        Assert.DoesNotContain("f.key", inputs.Keys);
        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_year_of_a_period_is_corrected_a_period_is_added_in_the_last_row_and_one_is_taken_out_with_its_Persian_text()
    {
        var before = Lane(File(), "china");
        var count = before["periods"]!.AsArray().Count;
        var first = (string)before["periods"]![0]!["key"]!;
        var translated = Persian()["items"]!.AsObject().ContainsKey(first);

        var corrected = await _desk.Fill(China, "lane", [("f.periods[0].time.at.year", "771"), .. Kept]);
        var added = await _desk.Fill(China, "lane",
            [($"f.periods[{count}].key", "a-new-period"), ($"f.periods[{count}].label", "A period of the tests"), ($"f.periods[{count}].time.kind", "period"),
             ($"f.periods[{count}].time.at.year", "1900"), ($"f.periods[{count}].time.end", "ongoing"), .. Kept]);
        var removed = await _desk.Fill(China, "lane", [("f.periods[0].key", string.Empty), ("f.periods[0].label", string.Empty), .. Kept]);

        Assert.True(translated);
        Assert.EndsWith("?done=changed", corrected.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", added.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", removed.Arrived, StringComparison.Ordinal);
        var periods = Lane(File(), "china")["periods"]!.AsArray();
        Assert.Equal(count, periods.Count);
        Assert.DoesNotContain(periods, period => (string?)period!["key"] == first);
        Assert.Equal("ongoing", (string?)periods.Single(period => (string?)period!["key"] == "a-new-period")!["time"]!["end"]!["state"]);
        Assert.False(Persian()["items"]!.AsObject().ContainsKey(first));
    }

    [Fact]
    public async Task What_cannot_be_a_lane_is_refused_with_the_form_kept()
    {
        var count = Lane(File(), "china")["periods"]!.AsArray().Count;

        var timeless = await _desk.Fill(China, "lane", [($"f.periods[{count}].key", "no-time"), ($"f.periods[{count}].label", "A period without a time"), .. Kept]);
        var twice = await _desk.Fill(China, "lane", [($"f.periods[{count}].key", (string)Lane(File(), "china")["periods"]![0]!["key"]!), ($"f.periods[{count}].label", "A second one of that key"),
            ($"f.periods[{count}].time.kind", "period"), ($"f.periods[{count}].time.at.year", "1"), ($"f.periods[{count}].time.until.year", "2"), .. Kept]);

        Assert.DoesNotContain("?done=", timeless.Arrived ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("A period without a time", Inputs(timeless, "lane")[$"f.periods[{count}].label"]);
        Assert.DoesNotContain("?done=", twice.Arrived ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal(count, Lane(File(), "china")["periods"]!.AsArray().Count);
    }

    [Fact]
    public async Task A_lane_is_added_with_its_key_and_taken_out_with_what_translates_it()
    {
        const string New = $"/en/comparisons/{Id}/new-lane";
        var nameless = await _desk.Fill(New, "lane", [("f.label", "India"), .. Kept]);
        var taken = await _desk.Fill(New, "lane", [("key", "china"), ("f.label", "China again"), .. Kept]);
        var added = await _desk.Fill(New, "lane",
            [("key", "india"), ("f.label", "India"), ("f.periods[0].key", "maurya"), ("f.periods[0].label", "Maurya"), ("f.periods[0].time.kind", "period"),
             ("f.periods[0].time.at.era", "beforeCommonEra"), ("f.periods[0].time.at.year", "322"), ("f.periods[0].time.until.era", "beforeCommonEra"), ("f.periods[0].time.until.year", "185"), .. Kept]);
        var lanes = File()["lanes"]!.AsArray().Select(lane => (string?)lane!["key"]).ToList();
        var removed = await _desk.Fill(China, "remove", ("Removing.By", "an editor"), ("Removing.Reason", "Not ready."));

        Assert.Contains("A lane has a key", nameless.Text, StringComparison.Ordinal);
        Assert.Contains("china", taken.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", taken.Arrived ?? string.Empty, StringComparison.Ordinal);
        Assert.EndsWith($"comparisons/{Id}/lanes/india?done=added", added.Arrived, StringComparison.Ordinal);
        Assert.Equal("india", lanes[^1]);
        Assert.EndsWith($"comparisons/{Id}?done=changed", removed.Arrived, StringComparison.Ordinal);
        Assert.DoesNotContain(File()["lanes"]!.AsArray(), lane => (string?)lane!["key"] == "china");
        Assert.False(Persian()["lanes"]!.AsObject().ContainsKey("china"));
        Assert.Equal(HttpStatusCode.NotFound, (await _desk.Get(China)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await _desk.Get($"/en/comparisons/nothing/lanes/china")).Status);
    }

    [Fact]
    public async Task An_entity_without_other_names_identifiers_or_statements_gets_its_first_of_each_with_the_form()
    {
        // A work, not a character: a character of a story has no date of history (ADR 0028).
        const string Work = $"/en/{Collection}/record/entities/work-shahnameh";
        var inputs = Inputs(await _desk.Get(Work), "fields");
        var same = await _desk.Fill(Work, "fields", Filled);

        var page = await _desk.Fill(Work, "fields",
            [("f.names[0].name", "Book of Kings"), ("f.names[0].language", "en"), ("f.names[0].kind", "title"),
             ("f.identifiers[0].issuer", "A catalogue of the tests"), ("f.identifiers[0].value", "Z-1"),
             ("f.dated[0].key", "work-told"), ("f.dated[0].label", "First told"), ("f.dated[0].sourceIds", "src-ganjoor-index"),
             ("f.dated[0].time.kind", "occurrence"), ("f.dated[0].time.at.year", "1010"), .. Filled]);

        Assert.Equal((string.Empty, string.Empty, "alternative"), (inputs["f.names[0].name"], inputs["f.names[0].language"], inputs["f.names[0].kind"]));
        Assert.Equal(("exact", "proposed"), (inputs["f.identifiers[0].match"], inputs["f.dated[0].reviewStatus"]));
        Assert.Equal("none", inputs["f.dated[0].time.kind"]);
        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        var work = store.Record(Collection, "entities", "work-shahnameh")!.Body;
        Assert.Equal(("Book of Kings", "en", "title"), ((string?)work["names"]![0]!["name"], (string?)work["names"]![0]!["language"], (string?)work["names"]![0]!["kind"]));
        Assert.Equal(("A catalogue of the tests", "Z-1", "exact"), ((string?)work["identifiers"]![0]!["issuer"], (string?)work["identifiers"]![0]!["value"], (string?)work["identifiers"]![0]!["match"]));
        Assert.Equal(1010, (int)work["dated"]![0]!["time"]!["at"]!["year"]!);
        Assert.Contains(store.Statements(Collection), statement => statement.Statement == "work-told");
    }
}
