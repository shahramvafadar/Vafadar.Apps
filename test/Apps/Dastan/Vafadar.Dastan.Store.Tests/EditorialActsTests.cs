using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The acts of an editor in the store (step 11.2): putting a record, taking one out, and recording the review of
/// a statement – each tried on the whole collection first, so that the store never holds a collection the site
/// could not use, and each kept with who, when and why.
/// </summary>
public sealed class EditorialActsTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private static readonly DateOnly Today = new(2026, 11, 17);

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    // --- reviewing ---

    [Fact]
    public void A_review_is_recorded_on_its_statement_with_who_when_and_what_it_was_checked_against()
    {
        using var store = _copy.Filled();
        var waiting = store.Statements(Collection, waitingOnly: true).Length;

        var result = store.Review(Collection, "relation-02", ReviewAct.Reviewed, "a reviewer", Today, "Read against the text.");
        var relation = WorkingCopy.Stored(store, "relations", "relation-02");
        var change = store.History(Collection, "relations", "relation-02")[^1];

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(1, result.Changed);
        Assert.Equal("reviewed", (string?)relation["reviewStatus"]);
        Assert.Equal(("a reviewer", "2026-11-17", "Read against the text."), ((string?)relation["review"]!["by"], (string?)relation["review"]!["on"], (string?)relation["review"]!["note"]));
        Assert.Equal((RecordChangeKind.Changed, 2, "a reviewer", "Read against the text."), (change.Kind, change.Version, change.By, change.Reason));
        Assert.Equal(waiting - 1, store.Statements(Collection, waitingOnly: true).Length);
    }

    [Fact]
    public void A_review_without_a_note_says_in_the_history_what_it_found()
    {
        using var store = _copy.Filled();

        store.Review(Collection, "event-01", ReviewAct.Reviewed, "a reviewer", Today);

        Assert.Equal("Review: reviewed.", store.History(Collection, "events", "event-01")[^1].Reason);
        Assert.Null(WorkingCopy.Stored(store, "events", "event-01")["review"]!["note"]);
    }

    [Fact]
    public void A_statement_is_disputed_only_with_what_the_sources_disagree_about()
    {
        using var store = _copy.Filled();
        var before = store.Export(Collection)!.Json;

        var without = store.Review(Collection, "relation-02", ReviewAct.Disputed, "a reviewer", Today);
        var unchanged = store.Export(Collection)!.Json;
        var with = store.Review(Collection, "relation-02", ReviewAct.Disputed, "a reviewer", Today, "Two manuscripts name different fathers.");

        Assert.Equal(["relation-02: the review of a disputed statement says in its note what the sources disagree about."], without.Problems);
        Assert.Equal(before, unchanged);
        Assert.True(with.Done);
        Assert.Equal("disputed", (string?)WorkingCopy.Stored(store, "relations", "relation-02")["reviewStatus"]);
    }

    [Fact]
    public void A_withdrawn_statement_stays_in_the_store_with_its_reason_and_is_no_part_of_what_the_site_reads()
    {
        using var store = _copy.Filled();

        var without = store.Review(Collection, "relation-18", ReviewAct.Withdrawn, "an editor", Today);
        var with = store.Review(Collection, "relation-18", ReviewAct.Withdrawn, "an editor", Today, "Not told in the selected part.");
        var exported = store.Export(Collection)!;

        Assert.Equal(["relation-18: a withdrawn statement says in its review who withdrew it and when, and in the note of the review why."], without.Problems);
        Assert.True(with.Done);
        Assert.Equal("withdrawn", (string?)WorkingCopy.Stored(store, "relations", "relation-18")["reviewStatus"]);
        var collection = PilotSample.Read(exported.Json, new PilotSampleOptions { SampleDirectory = _copy.Samples, ContentRoot = Path.GetDirectoryName(_copy.Samples)!, ReviewRoot = _copy.Folder, Translations = [exported.Translations["fa"]] });
        Assert.Equal(19, collection.Relations.Length);
    }

    [Fact]
    public void A_statement_something_still_points_to_is_not_withdrawn()
    {
        using var store = _copy.Filled();

        // Two media items explain the relation between Zal and Rostam.
        var result = store.Review(Collection, "relation-02", ReviewAct.Withdrawn, "an editor", Today, "A reason.");

        Assert.False(result.Done);
        Assert.Contains(result.Problems, problem => problem.Contains("relation-02", StringComparison.Ordinal));
        Assert.Equal("source_backed_editorial_review_pending", (string?)WorkingCopy.Stored(store, "relations", "relation-02")["reviewStatus"]);
    }

    [Fact]
    public void A_statement_with_a_date_is_reviewed_by_its_key()
    {
        using var store = _copy.Filled();
        var work = WorkingCopy.Stored(store, "entities", "work-shahnameh");
        work["dated"] = JsonNode.Parse(
            """
            [ { "key": "work-finished", "label": "The work is finished",
                "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
                "sourceIds": ["src-ganjoor-index"], "spoilerLevel": 0, "reviewStatus": "proposed" } ]
            """);
        Assert.True(store.Put(Collection, "entities", work, "a writer", "A date of the work.").Done);

        var waiting = store.Statements(Collection, waitingOnly: true);
        var result = store.Review(Collection, "work-finished", ReviewAct.Reviewed, "a reviewer", Today, "The colophon.");
        var dated = WorkingCopy.Stored(store, "entities", "work-shahnameh")["dated"]![0]!;

        Assert.Contains(waiting, statement => statement == new StatementState("entities", "work-shahnameh", "work-finished", "The work is finished", "proposed"));
        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(("reviewed", "a reviewer"), ((string?)dated["reviewStatus"], (string?)dated["review"]!["by"]));
        Assert.Equal(3, store.History(Collection, "entities", "work-shahnameh").Length);
    }

    [Fact]
    public void What_is_no_statement_is_not_reviewed()
    {
        using var store = _copy.Filled();

        var entity = store.Review(Collection, "character-rostam", ReviewAct.Reviewed, "a reviewer", Today);
        var nothing = store.Review("another-collection", "relation-02", ReviewAct.Reviewed, "a reviewer", Today);

        Assert.Equal(["character-rostam: the collection holds no statement of that name – a relation, an event or a statement with a date."], entity.Problems);
        Assert.Equal(["another-collection: the store holds no collection of that name."], nothing.Problems);
    }

    [Fact]
    public void The_statements_of_a_collection_are_its_relations_its_events_and_its_dated_statements()
    {
        using var store = _copy.Filled();

        var all = store.Statements(Collection);

        Assert.Equal(28, all.Length);
        Assert.Equal(28, store.Statements(Collection, waitingOnly: true).Length);
        Assert.Equal(new StatementState("relations", "relation-02", "relation-02", "Father of", "source_backed_editorial_review_pending"), all[1]);
        Assert.Empty(store.Statements("another-collection"));
    }

    // --- putting and taking out ---

    [Fact]
    public void A_record_is_put_in_place_of_the_one_with_its_identifier_and_a_new_one_at_the_end_of_its_group()
    {
        using var store = _copy.Filled();
        var rostam = WorkingCopy.Stored(store, "entities", "character-rostam");
        rostam["summary"] = "A corrected summary.";
        var source = JsonNode.Parse(
            """
            { "id": "src-new", "title": "A new source", "url": "https://example.org/a-new-source", "kind": "primary_literary_text_transcription",
              "accessedAt": "2026-11-17", "spoilerLevel": 0 }
            """)!.AsObject();

        var changed = store.Put(Collection, "entities", rostam, "a writer", "The summary said too much.");
        var added = store.Put(Collection, "sources", source, "a writer", "A source for a later statement.");
        var same = store.Put(Collection, "sources", source, "a writer", "Once more.");
        var exported = JsonNode.Parse(store.Export(Collection)!.Json)!;

        Assert.True(changed.Done && added.Done, string.Join('\n', changed.Problems.Concat(added.Problems)));
        Assert.Equal((1, 1, 0), (changed.Changed, added.Changed, same.Changed));
        Assert.Equal("A corrected summary.", (string?)WorkingCopy.Stored(store, "entities", "character-rostam")["summary"]);
        Assert.Equal("src-new", (string?)exported["sources"]!.AsArray()[^1]!["id"]);
        Assert.Equal((RecordChangeKind.Added, "a writer", "A source for a later statement."), (store.History(Collection, "sources", "src-new")[0].Kind, store.History(Collection, "sources", "src-new")[0].By, store.History(Collection, "sources", "src-new")[0].Reason));

        // The place of a record that was there does not change.
        Assert.Equal(
            _copy.Working()["entities"]!.AsArray().Select(entity => (string?)entity!["id"]),
            exported["entities"]!.AsArray().Select(entity => (string?)entity!["id"]));
    }

    [Fact]
    public void A_record_that_would_make_the_collection_unusable_is_refused_with_what_the_readers_say()
    {
        using var store = _copy.Filled();
        var before = store.Export(Collection)!.Json;
        var rostam = WorkingCopy.Stored(store, "entities", "character-rostam");
        rostam["type"] = "Hero";
        var early = WorkingCopy.Stored(store, "relations", "relation-04");
        early["spoilerLevel"] = 0;

        var unknown = store.Put(Collection, "entities", rostam, "a writer", "A try.");
        var givesAway = store.Put(Collection, "relations", early, "a writer", "A try.");
        var noGroup = store.Put(Collection, "people", rostam, "a writer", "A try.");
        var noName = store.Put(Collection, "entities", new JsonObject { ["label"] = "Nobody" }, "a writer", "A try.");

        Assert.Contains(unknown.Problems, problem => problem.StartsWith("character-rostam: type has the unknown value \"Hero\"", StringComparison.Ordinal));
        Assert.False(givesAway.Done);
        Assert.Contains(givesAway.Problems, problem => problem.StartsWith("relation-04:", StringComparison.Ordinal));
        Assert.Equal(["people: a collection has no such group of records; it has sources, entities, relations, events, media, threads."], noGroup.Problems);
        Assert.Equal(["a record names itself: it has an id."], noName.Problems);
        Assert.Equal(before, store.Export(Collection)!.Json);
        Assert.Single(store.History(Collection, "entities", "character-rostam"));
    }

    [Fact]
    public void A_record_is_taken_out_with_the_texts_that_translate_it()
    {
        using var store = _copy.Filled();

        var result = store.Remove(Collection, "relation-18", "an editor", "Not told in the selected part.");
        var exported = store.Export(Collection)!;

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(2, result.Changed);
        Assert.Equal(19, JsonNode.Parse(exported.Json)!["relations"]!.AsArray().Count);
        Assert.False(JsonNode.Parse(exported.Translations["fa"])!["relations"]!.AsObject().ContainsKey("relation-18"));
        Assert.Equal([RecordChangeKind.Added, RecordChangeKind.Removed], store.History(Collection, "relations", "relation-18").Select(change => change.Kind));
        Assert.Equal("Not told in the selected part.", store.History(Collection, EditorialStore.TranslationGroup, "fa")[^1].Reason);
    }

    [Fact]
    public void A_record_that_something_points_to_is_not_taken_out()
    {
        using var store = _copy.Filled();
        var before = store.Export(Collection)!.Json;

        var pointedTo = store.Remove(Collection, "character-zal", "an editor", "A try.");
        var unknown = store.Remove(Collection, "character-nobody", "an editor", "A try.");

        Assert.False(pointedTo.Done);
        Assert.Contains(pointedTo.Problems, problem => problem.Contains("character-zal", StringComparison.Ordinal));
        Assert.Equal(["character-nobody: the collection holds no record of that name."], unknown.Problems);
        Assert.Equal(before, store.Export(Collection)!.Json);
    }

    [Fact]
    public void Told_where_the_files_lie_the_store_refuses_a_media_item_whose_file_is_not_there()
    {
        JsonObject Moved(EditorialStore store)
        {
            var diagram = WorkingCopy.Stored(store, "media", "media-dastan-family-illustration");
            diagram["url"] = "../media/nothing-here.png";
            return diagram;
        }

        ActResult withFiles;
        using (var store = _copy.Filled(files: true))
        {
            withFiles = store.Put(Collection, "media", Moved(store), "a writer", "Another file.");
        }

        using var blind = _copy.Open();
        var withoutFiles = blind.Put(Collection, "media", Moved(blind), "a writer", "Another file.");

        Assert.Contains(withFiles.Problems, problem => problem.Contains("nothing-here.png", StringComparison.Ordinal));
        Assert.True(withoutFiles.Done, string.Join('\n', withoutFiles.Problems));
    }

    [Fact]
    public void An_act_needs_somebody_and_a_reason()
    {
        using var store = _copy.Filled();
        var rostam = WorkingCopy.Stored(store, "entities", "character-rostam");

        Assert.Throws<ArgumentException>(() => store.Put(Collection, "entities", rostam, " ", "A reason."));
        Assert.Throws<ArgumentException>(() => store.Put(Collection, "entities", rostam, "a writer", " "));
        Assert.Throws<ArgumentException>(() => store.Remove(Collection, "relation-18", "an editor", string.Empty));
        Assert.Throws<ArgumentException>(() => store.Review(Collection, "relation-02", ReviewAct.Reviewed, string.Empty, Today));
    }

    // --- the commands ---

    private (int Exit, string Output) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        return (ContentCommands.Run(arguments, output, Today, _copy.Clock), output.ToString());
    }

    [Fact]
    public void The_commands_list_what_waits_record_a_review_put_a_record_and_take_one_out()
    {
        Run("store", "import", _copy.Sample, "--store", _copy.Database, "--by", "an editor");
        var record = Path.Combine(_copy.Folder, "rostam.json");
        var rostam = WorkingCopy.Record(_copy.Working(), "entities", "character-rostam");
        rostam["summary"] = "A corrected summary.";
        File.WriteAllText(record, rostam.ToJsonString());

        var (listed, waiting) = Run("store", "waiting", Collection, "--store", _copy.Database);
        var (reviewed, said) = Run("store", "review", Collection, "relation-02", "--store", _copy.Database, "--by", "a reviewer", "--as", "reviewed", "--note", "Read against the text.", "--files", _copy.Samples);
        var (disputed, why) = Run("store", "review", Collection, "event-01", "--store", _copy.Database, "--by", "a reviewer", "--as", "disputed");
        var (put, saidPut) = Run("store", "put", Collection, "entities", "--from", record, "--store", _copy.Database, "--by", "a writer", "--reason", "Too much.");
        var (again, saidAgain) = Run("store", "put", Collection, "entities", "--from", record, "--store", _copy.Database, "--by", "a writer", "--reason", "Too much.");
        var (removed, saidRemoved) = Run("store", "remove", Collection, "relation-18", "--store", _copy.Database, "--by", "an editor", "--reason", "Not told.");
        var (_, after) = Run("store", "waiting", Collection, "--store", _copy.Database);

        Assert.Equal(ContentCommands.Done, listed);
        Assert.Contains($"{Collection}: 28 of 28 statements wait for a review.", waiting, StringComparison.Ordinal);
        Assert.Contains("relation-02  (relations)  Father of  – source_backed_editorial_review_pending", waiting, StringComparison.Ordinal);
        Assert.Equal((ContentCommands.Done, "relation-02: reviewed." + Environment.NewLine), (reviewed, said));
        Assert.Equal(ContentCommands.NotUsable, disputed);
        Assert.Contains("Not done: 1 reason. Nothing was changed.", why, StringComparison.Ordinal);
        Assert.Equal((ContentCommands.Done, "character-rostam: put into entities." + Environment.NewLine), (put, saidPut));
        Assert.Equal((ContentCommands.Done, "character-rostam: put into entities. Nothing had to be changed." + Environment.NewLine), (again, saidAgain));
        Assert.Equal((ContentCommands.Done, "relation-18: taken out." + Environment.NewLine), (removed, saidRemoved));
        Assert.Contains($"{Collection}: 26 of 27 statements wait for a review.", after, StringComparison.Ordinal);

        // The day of a review is the day of the clock when none is named.
        using var store = _copy.Open();
        Assert.Equal("2026-11-17", (string?)WorkingCopy.Stored(store, "relations", "relation-02")["review"]!["on"]);
    }

    [Fact]
    public void The_command_takes_one_record_from_a_file_and_says_when_the_file_holds_none()
    {
        Run("store", "import", _copy.Sample, "--store", _copy.Database, "--by", "an editor");
        var list = Path.Combine(_copy.Folder, "list.json");
        File.WriteAllText(list, "[1, 2]");

        var (noRecord, said) = Run("store", "put", Collection, "entities", "--from", list, "--store", _copy.Database, "--by", "a writer", "--reason", "A try.");
        var (noFile, _) = Run("store", "put", Collection, "entities", "--from", Path.Combine(_copy.Folder, "nothing.json"), "--store", _copy.Database, "--by", "a writer", "--reason", "A try.");

        Assert.Equal(ContentCommands.NotUsable, noRecord);
        Assert.Contains("does not hold one record", said, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUsable, noFile);
    }

    [Theory]
    [InlineData("store", "review", "a-collection", "relation-02", "--store", "x.db", "--by", "a reviewer")]
    [InlineData("store", "review", "a-collection", "relation-02", "--store", "x.db", "--by", "a reviewer", "--as", "approved")]
    [InlineData("store", "review", "a-collection", "relation-02", "--store", "x.db", "--by", "a reviewer", "--as", "1")]
    [InlineData("store", "review", "a-collection", "relation-02", "--store", "x.db", "--by", "a reviewer", "--as", "reviewed", "--on", "17.11.2026")]
    [InlineData("store", "review", "a-collection", "relation-02", "--store", "x.db", "--as", "reviewed")]
    [InlineData("store", "review", "a-collection", "--store", "x.db", "--by", "a reviewer", "--as", "reviewed")]
    [InlineData("store", "put", "a-collection", "entities", "--store", "x.db", "--by", "a writer", "--from", "r.json")]
    [InlineData("store", "remove", "a-collection", "relation-02", "--store", "x.db", "--by", "an editor")]
    [InlineData("store", "remove", "a-collection", "relation-02", "--by", "an editor", "--reason", "A reason.")]
    [InlineData("store", "waiting", "a-collection", "--store", "x.db", "--by", "an editor")]
    public void An_act_that_is_not_asked_for_completely_is_not_done(params string[] arguments)
    {
        var (exit, text) = Run(arguments);

        Assert.Equal(ContentCommands.NotUnderstood, exit);
        Assert.Contains("store review <collection> <statement>", text, StringComparison.Ordinal);
        Assert.False(File.Exists("x.db"));
    }
}
