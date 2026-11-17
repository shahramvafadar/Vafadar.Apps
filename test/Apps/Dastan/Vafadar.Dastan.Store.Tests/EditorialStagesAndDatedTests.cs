using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// What a translation translates beside the records (step 12.5): the names of the reading stages and the texts
/// of the statements with a date of an entity – read by the language and kept with what they translate in one
/// act; and a change of what is not a record is kept with who, when and why like any other.
/// </summary>
public sealed class EditorialStagesAndDatedTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private const string Work = "work-shahnameh";

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private static JsonObject Dated(string key, string label) => JsonNode.Parse(
        $$"""
        {
          "key": "{{key}}",
          "label": "{{label}}",
          "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
          "sourceIds": ["src-ganjoor-index"],
          "spoilerLevel": 0,
          "reviewStatus": "proposed"
        }
        """)!.AsObject();

    private static JsonObject WorkWith(EditorialStore store, params JsonObject[] dated)
    {
        var work = WorkingCopy.Stored(store, "entities", Work);
        work["dated"] = new JsonArray([.. dated]);
        return work;
    }

    private static Dictionary<string, JsonObject> Persian(params (string Key, string Label)[] texts) => new()
    {
        ["fa"] = new JsonObject(texts.Select(text => KeyValuePair.Create(text.Key, (JsonNode?)new JsonObject { ["label"] = text.Label }))),
    };

    [Fact]
    public void The_texts_of_a_statement_with_a_date_are_kept_with_their_entity_and_read_by_its_key()
    {
        using var store = _copy.Filled();

        var result = store.Put(Collection, "entities", WorkWith(store, Dated("work-finished", "The work is finished"), Dated("work-copied", "A copy is made")), "a writer", "Two dates.",
            dated: Persian(("work-finished", "پایان سرودن"), ("work-nowhere", "چیزی")));

        Assert.True(result.Done, string.Join('\n', result.Problems));
        var (language, texts) = Assert.Single(store.DatedTexts(Collection, Work));
        Assert.Equal("fa", language);
        Assert.Equal("پایان سرودن", (string?)texts["work-finished"]!["label"]);

        // A text for a statement the entity does not have is not kept, and a statement without a text has none.
        Assert.Equal(["work-finished"], texts.Select(text => text.Key));
        Assert.Empty(store.DatedTexts(Collection, "character-rostam").Single().Texts);
        Assert.Equal("پایان سرودن", (string?)JsonNode.Parse(store.Export(Collection)!.Translations["fa"])!["dated"]!["work-finished"]!["label"]);
    }

    [Fact]
    public void A_statement_with_a_date_is_taken_out_together_with_the_texts_that_translate_it()
    {
        using var store = _copy.Filled();
        store.Put(Collection, "entities", WorkWith(store, Dated("work-finished", "The work is finished"), Dated("work-copied", "A copy is made")), "a writer", "Two dates.",
            dated: Persian(("work-finished", "پایان سرودن"), ("work-copied", "رونویسی")));

        var result = store.Put(Collection, "entities", WorkWith(store, Dated("work-copied", "A copy is made")), "a writer", "One was wrong.", dated: Persian(("work-copied", "رونویسی")));

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(["work-copied"], store.DatedTexts(Collection, Work).Single().Texts.Select(text => text.Key));
    }

    [Fact]
    public void A_text_left_empty_is_a_text_the_language_no_longer_has()
    {
        using var store = _copy.Filled();
        store.Put(Collection, "entities", WorkWith(store, Dated("work-finished", "The work is finished")), "a writer", "A date.", dated: Persian(("work-finished", "پایان سرودن")));

        var result = store.Put(Collection, "entities", WorkWith(store, Dated("work-finished", "The work is finished")), "a translator", "Not sure of it.", dated: new Dictionary<string, JsonObject> { ["fa"] = [] });

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(1, result.Changed);
        Assert.Empty(store.DatedTexts(Collection, Work).Single().Texts);
        Assert.Equal("Not sure of it.", store.History(Collection, EditorialStore.TranslationGroup, "fa")[^1].Reason);
    }

    [Fact]
    public void The_names_of_the_reading_stages_are_read_by_the_language_in_the_order_of_the_stages()
    {
        using var store = _copy.Filled();

        var stages = store.Stages(Collection);
        var (language, names) = Assert.Single(store.StageTexts(Collection));

        Assert.Equal("fa", language);
        Assert.Equal(stages.Length, names.Length);
        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    [Fact]
    public void A_name_of_a_stage_is_changed_in_both_languages_and_kept_with_who_and_why()
    {
        using var store = _copy.Filled();
        var labels = store.Stages(Collection).Select(stage => stage.Label).ToList();
        var persian = store.StageTexts(Collection).Single().Labels.ToList();
        labels[1] = "A new name of the second stage";
        persian[1] = "نام تازهٔ مرحلهٔ دوم";

        var result = store.PutStages(Collection, labels, new Dictionary<string, IReadOnlyList<string?>> { ["fa"] = persian }, "an editor", "Clearer names.");

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(2, result.Changed);
        Assert.Equal("A new name of the second stage", store.Stages(Collection)[1].Label);
        Assert.Equal("نام تازهٔ مرحلهٔ دوم", store.StageTexts(Collection).Single().Labels[1]);

        // What is not a record has a history too.
        var change = Assert.Single(store.History(Collection, EditorialStore.CollectionGroup, Collection));
        Assert.Equal((RecordChangeKind.Changed, "an editor", "Clearer names.", 1), (change.Kind, change.By, change.Reason, change.Version));
        Assert.Contains("A new name of the second stage", change.After, StringComparison.Ordinal);
        Assert.DoesNotContain("A new name of the second stage", change.Before, StringComparison.Ordinal);
    }

    [Fact]
    public void The_stages_sent_as_they_stand_change_nothing()
    {
        using var store = _copy.Filled();

        var result = store.PutStages(
            Collection,
            [.. store.Stages(Collection).Select(stage => stage.Label)],
            new Dictionary<string, IReadOnlyList<string?>> { ["fa"] = store.StageTexts(Collection).Single().Labels },
            "an editor", "Nothing.");

        Assert.True(result.Done);
        Assert.Equal(0, result.Changed);
        Assert.Empty(store.History(Collection, EditorialStore.CollectionGroup, Collection));
    }

    [Fact]
    public void A_stage_is_added_at_the_end_and_the_last_one_is_taken_out_only_when_nothing_is_told_at_it()
    {
        using var store = _copy.Filled();
        var labels = store.Stages(Collection).Select(stage => stage.Label).ToList();
        var none = new Dictionary<string, IReadOnlyList<string?>>();

        var added = store.PutStages(Collection, [.. labels, "After the end"], none, "an editor", "An epilogue will follow.");
        var without = store.PutStages(Collection, labels, none, "an editor", "No epilogue after all.");
        var tooFew = store.PutStages(Collection, labels[..^1], none, "an editor", "Shorter.");

        Assert.True(added.Done, string.Join('\n', added.Problems));
        Assert.True(without.Done, string.Join('\n', without.Problems));
        Assert.Equal(labels.Count, store.Stages(Collection).Length);

        // The last stage of the collection is in use: records are told at it.
        Assert.False(tooFew.Done);
        Assert.Contains(tooFew.Problems, problem => problem.Contains("is not a stage of the collection", StringComparison.Ordinal));
        Assert.Equal(labels.Count, store.Stages(Collection).Length);
    }

    [Fact]
    public void A_stage_without_a_name_and_a_language_the_collection_does_not_have_are_refused()
    {
        using var store = _copy.Filled();
        var labels = store.Stages(Collection).Select(stage => stage.Label).ToList();

        var nameless = store.PutStages(Collection, [labels[0], " ", .. labels[2..]], new Dictionary<string, IReadOnlyList<string?>>(), "an editor", "x");
        var german = store.PutStages(Collection, labels, new Dictionary<string, IReadOnlyList<string?>> { ["de"] = ["Anfang"] }, "an editor", "x");

        Assert.Equal(["a collection has at least one reading stage, and every stage has a name."], nameless.Problems);
        Assert.Equal(["de: the collection has no texts in that language; a language is added by importing its file."], german.Problems);
    }
}
