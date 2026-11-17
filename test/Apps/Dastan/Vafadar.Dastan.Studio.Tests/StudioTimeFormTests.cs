using System.Text.Json.Nodes;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// A time as a form in the tool of the editors (step 13.1): the time of a statement with a date and of a
/// relation of the world is filled in with inputs – once, between two times, or lasting – and what cannot be a
/// time is refused with the form kept.
/// </summary>
public sealed class StudioTimeFormTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Work = $"/en/{Collection}/record/entities/work-shahnameh";
    private const string Written = $"/en/{Collection}/record/relations/world-written";
    private static readonly (string, string?)[] WhoAndWhy = [("Filling.By", "a writer"), ("Filling.Reason", "Filled in.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioTimeFormTests()
    {
        _desk = new Desk(_studio);
        using var store = _studio.Store();
        var work = store.Record(Collection, "entities", "work-shahnameh")!.Body;
        work["dated"] = JsonNode.Parse(
            """
            [ {
              "key": "work-finished",
              "label": "The work is finished",
              "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
              "sourceIds": ["src-ganjoor-index"],
              "spoilerLevel": 0,
              "reviewStatus": "proposed"
            } ]
            """);
        Assert.True(store.Put(Collection, "entities", work, "a writer", "A date of the tests.").Done);
        Assert.True(store.Put(Collection, "entities", JsonNode.Parse(
            """
            { "id": "person-poet", "type": "Person", "label": "A poet of the tests", "summary": "Test data.", "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0 }
            """)!.AsObject(), "a writer", "A poet of the tests.").Done);
        Assert.True(store.Put(Collection, "relations", JsonNode.Parse(
            """
            {
              "id": "world-written", "from": "person-poet", "to": "work-shahnameh", "type": "authorOf", "label": "Author of",
              "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "source_backed_editorial_review_pending",
              "scope": { "kind": "outside_the_story", "eventIds": [] }, "directionality": "directed"
            }
            """)!.AsObject(), "a writer", "Who wrote it.").Done);
    }

    public void Dispose() => _studio.Dispose();

    private static Dictionary<string, string> Inputs(StudioPage page) =>
        Desk.Standing(page.Html).GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => input.First().Value, StringComparer.Ordinal);

    private JsonNode? TimeOfTheWork()
    {
        using var store = _studio.Store();
        return store.Record(Collection, "entities", "work-shahnameh")!.Body["dated"]![0]!["time"];
    }

    [Fact]
    public async Task The_time_of_a_statement_stands_in_inputs_and_sent_as_it_stands_changes_nothing()
    {
        var inputs = Inputs(await _desk.Get(Work));
        var same = await _desk.Fill(Work, "fields", WhoAndWhy);

        Assert.Equal("occurrence", inputs["f.dated[0].time.kind"]);
        Assert.Equal("commonEra", inputs["f.dated[0].time.at.era"]);
        Assert.Equal("1010", inputs["f.dated[0].time.at.year"]);
        Assert.Equal(string.Empty, inputs["f.dated[0].time.at.month"]);
        Assert.DoesNotContain("f.dated[0].time.at.about", inputs.Keys);
        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_year_is_corrected_and_a_day_is_stated()
    {
        var page = await _desk.Fill(Work, "fields",
            [("f.dated[0].time.at.year", "1011"), ("f.dated[0].time.at.month", "3"), ("f.dated[0].time.at.day", "8"), ("f.dated[0].time.at.about", "true"), .. WhoAndWhy]);

        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        var at = TimeOfTheWork()!["at"]!;
        Assert.Equal((1011, 3, 8, true), ((int)at["year"]!, (int)at["month"]!, (int)at["day"]!, (bool)at["approximate"]!));
    }

    [Fact]
    public async Task What_happened_once_becomes_what_lasted_from_about_a_year_to_an_end_nobody_states()
    {
        var page = await _desk.Fill(Work, "fields",
            [("f.dated[0].time.kind", "period"), ("f.dated[0].time.at.year", "977"), ("f.dated[0].time.at.about", "true"), ("f.dated[0].time.end", "unknown"), .. WhoAndWhy]);
        var again = Inputs(await _desk.Get(Work));

        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        var time = TimeOfTheWork()!;
        Assert.Equal("period", (string?)time["kind"]);
        Assert.Equal("unknown", (string?)time["end"]!["state"]);
        Assert.Equal(977, (int)time["start"]!["at"]!["year"]!);
        Assert.Equal(("period", "known", "unknown", "977"), (again["f.dated[0].time.kind"], again["f.dated[0].time.start"], again["f.dated[0].time.end"], again["f.dated[0].time.at.year"]));
        Assert.Equal("true", again["f.dated[0].time.at.about"]);
    }

    [Fact]
    public async Task What_lay_between_two_years_before_the_common_era_is_said_as_such()
    {
        var page = await _desk.Fill(Work, "fields",
            [("f.dated[0].time.kind", "between"), ("f.dated[0].time.at.era", "beforeCommonEra"), ("f.dated[0].time.at.year", "550"),
             ("f.dated[0].time.latest.era", "beforeCommonEra"), ("f.dated[0].time.latest.year", "530"), .. WhoAndWhy]);

        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        var again = Inputs(await _desk.Get(Work));
        Assert.Equal(("between", "beforeCommonEra", "550", "530"), (again["f.dated[0].time.kind"], again["f.dated[0].time.at.era"], again["f.dated[0].time.at.year"], again["f.dated[0].time.latest.year"]));
    }

    [Theory]
    [InlineData("f.dated[0].time.at.year", "ten", "time: at.year is a whole number.")]
    [InlineData("f.dated[0].time.at.year", "", "time: at.year is missing.")]
    [InlineData("f.dated[0].time.at.day", "8", "time: at.day needs a month.")]
    [InlineData("f.dated[0].time.kind", "none", "time is missing")]
    public async Task What_cannot_be_a_time_is_refused_and_the_form_keeps_what_was_filled_in(string input, string value, string problem)
    {
        var before = TimeOfTheWork()!.ToJsonString();

        var page = await _desk.Fill(Work, "fields", [(input, value), ("f.label", "The book of the tests"), .. WhoAndWhy]);

        Assert.DoesNotContain("?done=", page.Arrived, StringComparison.Ordinal);
        Assert.Contains(problem, page.Text, StringComparison.Ordinal);
        Assert.Equal("The book of the tests", Inputs(page)["f.label"]);
        Assert.Equal(before, TimeOfTheWork()!.ToJsonString());
    }

    [Fact]
    public async Task A_period_that_ends_before_it_begins_is_refused()
    {
        var page = await _desk.Fill(Work, "fields",
            [("f.dated[0].time.kind", "period"), ("f.dated[0].time.at.year", "1010"), ("f.dated[0].time.until.year", "977"), .. WhoAndWhy]);

        Assert.DoesNotContain("?done=", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("time", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_relation_of_the_world_that_states_no_time_is_given_one_and_a_relation_of_the_story_has_no_such_inputs()
    {
        var before = Inputs(await _desk.Get(Written));
        var story = Inputs(await _desk.Get($"/en/{Collection}/record/relations/relation-02"));
        var same = await _desk.Fill(Written, "fields", WhoAndWhy);
        var given = await _desk.Fill(Written, "fields", [("f.time.kind", "occurrence"), ("f.time.at.year", "1010"), .. WhoAndWhy]);
        var after = Inputs(await _desk.Get(Written));
        var taken = await _desk.Fill(Written, "fields", [("f.time.kind", "none"), .. WhoAndWhy]);

        Assert.Equal("none", before["f.time.kind"]);
        Assert.DoesNotContain("f.time.kind", story.Keys);
        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", given.Arrived, StringComparison.Ordinal);
        Assert.Equal(("occurrence", "1010"), (after["f.time.kind"], after["f.time.at.year"]));
        Assert.EndsWith("?done=changed", taken.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Null(store.Record(Collection, "relations", "world-written")!.Body["time"]);
    }
}
