using System.Net;
using System.Text.Json.Nodes;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The texts of the other languages that belong to no record of their own, in the tool of the editors
/// (step 12.5): the names of the reading stages on a page of their own, and the texts of a statement with a
/// date in the form of its entity.
/// </summary>
public sealed class StudioStagesAndDatedTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Stages = $"/en/{Collection}/stages";
    private const string Work = $"/en/{Collection}/record/entities/work-shahnameh";
    private static readonly (string, string?)[] WhoAndWhy = [("Keeping.By", "an editor"), ("Keeping.Reason", "Clearer.")];
    private static readonly (string, string?)[] Filled = [("Filling.By", "a writer"), ("Filling.Reason", "Filled in.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioStagesAndDatedTests() => _desk = new Desk(_studio);

    public void Dispose() => _studio.Dispose();

    private void WithADate()
    {
        using var store = _studio.Store();
        var work = store.Record(Collection, "entities", "work-shahnameh")!.Body;
        work["dated"] = JsonNode.Parse(
            """
            [ {
              "key": "work-finished",
              "label": "The work is finished",
              "summary": "Test data.",
              "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
              "sourceIds": ["src-ganjoor-index"],
              "spoilerLevel": 0,
              "reviewStatus": "proposed"
            } ]
            """);
        Assert.True(store.Put(Collection, "entities", work, "a writer", "A date of the tests.").Done);
    }

    [Theory]
    [InlineData("en", "Reading stages")]
    [InlineData("fa", "مرحله‌های خواندن")]
    public async Task The_page_of_the_stages_has_every_name_in_both_languages_and_a_row_to_add_one(string language, string title)
    {
        using var store = _studio.Store();
        var count = store.Stages(Collection).Length;

        var page = await _desk.Get($"/{language}/{Collection}/stages");
        var collection = await _desk.Get($"/{language}/{Collection}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Text, StringComparison.Ordinal);
        Assert.Contains($"{Collection}/stages", collection.Html, StringComparison.Ordinal);
        var inputs = Desk.Standing(page.Html).GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => input.First().Value, StringComparer.Ordinal);
        Assert.Equal(store.Stages(Collection)[0].Label, inputs["s[0]"]);
        Assert.Equal(store.StageTexts(Collection).Single().Labels[0], inputs["t.fa[0]"]);
        Assert.Equal(string.Empty, inputs[$"s[{count}]"]);
        Assert.Equal(string.Empty, inputs[$"t.fa[{count}]"]);
        Assert.DoesNotContain($"s[{count + 1}]", inputs.Keys);
    }

    [Fact]
    public async Task The_stages_sent_as_they_stand_change_nothing_and_a_new_name_is_kept_under_the_editor()
    {
        var same = await _desk.Fill(Stages, "stages", WhoAndWhy);
        var changed = await _desk.Fill(Stages, "stages", [("t.fa[0]", "آغاز، به نام تازه"), .. WhoAndWhy]);

        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", changed.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal("آغاز، به نام تازه", store.StageTexts(Collection).Single().Labels[0]);
        var change = store.History(Collection, EditorialStore.TranslationGroup, "fa")[^1];
        Assert.Equal(("an editor", "Clearer."), (change.By, change.Reason));
    }

    [Fact]
    public async Task A_stage_is_added_in_the_last_row_and_its_change_stands_in_the_history_of_the_collection()
    {
        using var store = _studio.Store();
        var count = store.Stages(Collection).Length;

        var page = await _desk.Fill(Stages, "stages", [($"s[{count}]", "After the end"), ($"t.fa[{count}]", "پس از پایان"), .. WhoAndWhy]);
        var history = await _desk.Get($"/en/{Collection}/history");

        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Equal("After the end", store.Stages(Collection)[^1].Label);
        Assert.Equal("پس از پایان", store.StageTexts(Collection).Single().Labels[^1]);
        Assert.Contains("The collection", history.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_name_left_empty_in_the_middle_and_a_stage_in_use_that_is_taken_out_are_refused_with_the_form_kept()
    {
        using var store = _studio.Store();
        var count = store.Stages(Collection).Length;

        var hole = await _desk.Fill(Stages, "stages", [("s[1]", string.Empty), ("t.fa[1]", "چیزی که ماند"), .. WhoAndWhy]);
        var short1 = await _desk.Fill(Stages, "stages", [($"s[{count - 1}]", string.Empty), .. WhoAndWhy]);

        Assert.Contains("Every stage has a name", hole.Text, StringComparison.Ordinal);
        Assert.Contains("چیزی که ماند", Desk.Standing(hole.Html).First(input => input.Name == "t.fa[1]").Value, StringComparison.Ordinal);
        Assert.Contains("is not a stage of the collection", short1.Text, StringComparison.Ordinal);
        Assert.Equal(count, store.Stages(Collection).Length);
    }

    [Fact]
    public async Task The_form_of_an_entity_has_the_texts_of_its_dated_statements_below_their_row()
    {
        WithADate();

        var page = await _desk.Get(Work);
        var inputs = Desk.Standing(page.Html).GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => input.First().Value, StringComparer.Ordinal);

        Assert.Equal("The work is finished", inputs["f.dated[0].label"]);
        Assert.Equal(string.Empty, inputs["t.fa.dated[0].label"]);
        Assert.Equal(string.Empty, inputs["t.fa.dated[0].summary"]);
        Assert.Contains("t.fa.dated[1].label", inputs.Keys);
    }

    [Theory]
    [InlineData("media/media-met-452419")]
    [InlineData("media/media-umma-1963-1-46")]
    [InlineData("entities/work-shahnameh")]
    public async Task A_record_with_lists_of_parts_that_have_parts_of_their_own_is_sent_as_it_stands_and_changes_nothing(string record)
    {
        WithADate();

        var page = await _desk.Fill($"/en/{Collection}/record/{record}", "fields", Filled);

        Assert.EndsWith("?done=unchanged", page.Arrived, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_text_of_a_dated_statement_is_kept_with_its_entity_and_taken_out_with_its_row()
    {
        WithADate();

        var filled = await _desk.Fill(Work, "fields", [("t.fa.dated[0].label", "پایان سرودن"), .. Filled]);
        using var store = _studio.Store();
        var kept = (string?)store.DatedTexts(Collection, "work-shahnameh").Single().Texts["work-finished"]?["label"];
        var same = await _desk.Fill(Work, "fields", Filled);
        var removed = await _desk.Fill(Work, "fields", [("f.dated[0].key", string.Empty), ("f.dated[0].label", string.Empty), ("f.dated[0].summary", string.Empty), .. Filled]);

        Assert.EndsWith("?done=changed", filled.Arrived, StringComparison.Ordinal);
        Assert.Equal("پایان سرودن", kept);
        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", removed.Arrived, StringComparison.Ordinal);
        Assert.Empty(store.DatedTexts(Collection, "work-shahnameh").Single().Texts);
    }

    [Fact]
    public async Task A_hypothetical_placement_and_the_translation_of_its_explanation_are_carried_through_the_form()
    {
        // A placement (ADR 0028) has no inputs of its own: the form writes it as it stands, and keeps the
        // explanation of the translation, for which it has no input either.
        using (var store = _studio.Store())
        {
            var rostam = store.Record(Collection, "entities", "character-rostam")!.Body;
            rostam["dated"] = JsonNode.Parse(
                """
                [ {
                  "key": "placement-rostam-birth",
                  "label": "Hypothetical birth",
                  "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "beforeCommonEra", "year": 1300, "approximate": true } },
                  "placement": { "basis": "editorialHypothesis", "confidence": "veryLow", "explanation": "Test data." },
                  "sourceIds": ["src-ganjoor-index"],
                  "spoilerLevel": 0,
                  "reviewStatus": "proposed"
                } ]
                """);
            var persian = new Dictionary<string, JsonObject> { ["fa"] = JsonNode.Parse("""{ "placement-rostam-birth": { "label": "تولد فرضی", "explanation": "دادهٔ آزمایشی." } }""")!.AsObject() };
            Assert.True(store.Put(Collection, "entities", rostam, "a writer", "A placement of the tests.", dated: persian).Done);
        }

        var form = $"/en/{Collection}/record/entities/character-rostam";
        var same = await _desk.Fill(form, "fields", Filled);
        var changed = await _desk.Fill(form, "fields", [("t.fa.dated[0].label", "زادن فرضی"), .. Filled]);

        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", changed.Arrived, StringComparison.Ordinal);
        using var after = _studio.Store();
        var placement = after.Record(Collection, "entities", "character-rostam")!.Body["dated"]![0]!["placement"]!;
        Assert.Equal(("editorialHypothesis", "veryLow", "Test data."), ((string?)placement["basis"], (string?)placement["confidence"], (string?)placement["explanation"]));
        var texts = after.DatedTexts(Collection, "character-rostam").Single().Texts["placement-rostam-birth"]!;
        Assert.Equal(("زادن فرضی", "دادهٔ آزمایشی."), ((string?)texts["label"], (string?)texts["explanation"]));
    }
}
