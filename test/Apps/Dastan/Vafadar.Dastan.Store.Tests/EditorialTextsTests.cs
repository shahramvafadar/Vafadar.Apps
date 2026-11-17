using System.Text.Json.Nodes;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The texts of a record in the other languages (step 11.8): read for a record, and kept with the record in one
/// act – so that a record and what translates it are tried together and never disagree in the store.
/// </summary>
public sealed class EditorialTextsTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private static JsonObject Persian(EditorialStore store, string group, string id) => store.Texts(Collection, group, id).Single(text => text.Language == "fa").Texts;

    [Fact]
    public void The_texts_of_a_record_are_read_by_the_language()
    {
        using var store = _copy.Filled();

        var texts = store.Texts(Collection, "events", "event-01");

        var (language, persian) = Assert.Single(texts);
        Assert.Equal("fa", language);
        Assert.Equal("اسبی گم‌شده و پذیرایی", (string?)persian["label"]);
        Assert.Equal("جای پذیرایی", (string?)persian["roles"]!["place-samangan"]);
        Assert.Empty(Persian(store, "sources", "src-ganjoor-index"));
    }

    [Fact]
    public void A_text_is_changed_with_its_record_and_the_change_is_kept_with_who_and_why()
    {
        using var store = _copy.Filled();
        var texts = Persian(store, "entities", "character-rostam");
        texts["summary"] = "پهلوانی که در پی اسب گم‌شده‌اش می‌رود.";

        var result = store.Put(Collection, "entities", WorkingCopy.Stored(store, "entities", "character-rostam"), "a translator", "Shorter.", new Dictionary<string, JsonObject> { ["fa"] = texts });

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(1, result.Changed);
        Assert.Equal("پهلوانی که در پی اسب گم‌شده‌اش می‌رود.", (string?)Persian(store, "entities", "character-rostam")["summary"]);
        var change = store.History(Collection, EditorialStore.TranslationGroup, "fa")[^1];
        Assert.Equal((RecordChangeKind.Changed, "a translator", "Shorter."), (change.Kind, change.By, change.Reason));

        // The record itself was not touched, and no other record lost a text.
        Assert.Single(store.History(Collection, "entities", "character-rostam"));
        Assert.Equal("اسبی گم‌شده و پذیرایی", (string?)Persian(store, "events", "event-01")["label"]);
    }

    [Fact]
    public void A_part_is_taken_out_together_with_the_text_that_translates_it()
    {
        using var store = _copy.Filled();
        var record = WorkingCopy.Stored(store, "events", "event-01");
        record["entityAssociations"]!.AsArray().RemoveAt(4);

        // Alone the record is refused: the Persian text names a role that is no longer there.
        var alone = store.Put(Collection, "events", record.DeepClone().AsObject(), "an editor", "Not part of the step.");
        Assert.Contains("translation fa: events/event-01/roles/place-samangan translates nothing the sample has.", alone.Problems);

        var texts = Persian(store, "events", "event-01");
        texts["roles"]!.AsObject().Remove("place-samangan");
        var together = store.Put(Collection, "events", record, "an editor", "Not part of the step.", new Dictionary<string, JsonObject> { ["fa"] = texts });

        Assert.True(together.Done, string.Join('\n', together.Problems));
        Assert.Equal(2, together.Changed);
        Assert.Null(Persian(store, "events", "event-01")["roles"]!["place-samangan"]);
    }

    [Fact]
    public void A_language_named_with_no_texts_loses_the_texts_of_the_record_and_one_not_named_keeps_them()
    {
        using var store = _copy.Filled();
        var record = WorkingCopy.Stored(store, "relations", "relation-02");

        Assert.Equal(0, store.Put(Collection, "relations", record, "an editor", "Nothing.").Changed);
        Assert.NotEmpty(Persian(store, "relations", "relation-02"));

        Assert.True(store.Put(Collection, "relations", record, "an editor", "To be translated again.", new Dictionary<string, JsonObject> { ["fa"] = [] }).Done);
        Assert.Empty(Persian(store, "relations", "relation-02"));
    }

    [Fact]
    public void A_text_the_readers_do_not_know_and_a_language_the_collection_does_not_have_are_refused()
    {
        using var store = _copy.Filled();
        var record = WorkingCopy.Stored(store, "relations", "relation-02");
        var before = Persian(store, "relations", "relation-02");

        var unknown = store.Put(Collection, "relations", record, "an editor", "A try.", new Dictionary<string, JsonObject> { ["fa"] = new JsonObject { ["label"] = "پدرِ", ["summary"] = "متنی که جایی ندارد." } });
        var absent = store.Put(Collection, "relations", record, "an editor", "A try.", new Dictionary<string, JsonObject> { ["de"] = new JsonObject { ["label"] = "Vater von" } });

        Assert.False(unknown.Done);
        Assert.Equal(["de: the collection has no texts in that language; a language is added by importing its file."], absent.Problems);
        Assert.Contains(unknown.Problems, problem => problem.Contains("summary", StringComparison.Ordinal));
        Assert.True(JsonNode.DeepEquals(before, Persian(store, "relations", "relation-02")));
    }

    [Fact]
    public void The_readers_list_the_texts_a_translation_may_translate()
    {
        var translated = ContentVocabulary.Translated.ToDictionary(texts => texts.Group);

        Assert.Equal(["dated", "entities", "events", "media", "relations"], translated.Keys.Order());
        Assert.Equal(["label", "summary"], translated["events"].Fields);
        Assert.True(translated["events"].Roles);
        Assert.Equal(["label", "caption", "altText"], translated["media"].Fields);
        Assert.False(translated["media"].Roles);
    }
}
