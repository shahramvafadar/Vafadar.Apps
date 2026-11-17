using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The store of the editors (ADR 0011, step 11.1): the working file of a collection goes in and comes out
/// unchanged in meaning, a record that reads differently becomes a change with who, when and why, and a file the
/// site cannot use changes nothing. The collection is a copy of the one of the repository, in a folder of the test.
/// </summary>
public sealed class EditorialStoreTests : IDisposable
{
    private const string Collection = "dastan-shahnameh-rostam-sohrab-pilot-v0.5";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-store-" + Guid.NewGuid().ToString("N"));
    private readonly Clock _clock = new(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero));

    public EditorialStoreTests()
    {
        // The working files of the editors and, beside them, the documents a media item points to for reviewers.
        var app = Path.Combine(RepositoryRoot(), "src", "Apps", "Dastan");
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(_folder, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy);
            }
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Sample => Path.Combine(_folder, "content", "samples", "shahnameh-pilot.json");

    private string Persian => Path.Combine(_folder, "content", "samples", "shahnameh-pilot.fa.json");

    private string Database => Path.Combine(_folder, "store", "editors.db");

    private EditorialStore Open() => EditorialStore.Open(Database, _clock);

    private JsonObject Working() => JsonNode.Parse(File.ReadAllText(Sample))!.AsObject();

    private void Change(Action<JsonObject> change)
    {
        var sample = Working();
        change(sample);
        File.WriteAllText(Sample, sample.ToJsonString());
    }

    private static JsonObject Record(JsonObject sample, string group, string id) =>
        sample[group]!.AsArray().Select(node => node!.AsObject()).Single(record => (string?)record["id"] == id);

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Vafadar.Apps.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The root of the repository was not found.");
    }

    // --- in and out ---

    [Fact]
    public void A_collection_comes_out_of_the_store_as_it_went_in()
    {
        var original = Working();
        using var store = Open();

        var result = store.Import(Sample, "an editor", "The first import.");
        var exported = store.Export(Collection)!;

        Assert.Empty(result.Problems);
        Assert.Equal(Collection, result.CollectionId);
        var records = EditorialStore.Groups.Sum(group => original[group]!.AsArray().Count);
        Assert.Equal((records + 1, 0, 0), (result.Added, result.Changed, result.Removed));
        Assert.Equal([Collection], store.Collections);

        // The same file in meaning: every field, every record, the order of every group.
        Assert.True(JsonNode.DeepEquals(original, JsonNode.Parse(exported.Json)));
        Assert.Equal(["fa"], exported.Translations.Keys);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Persian)), JsonNode.Parse(exported.Translations["fa"])));
    }

    [Fact]
    public void What_comes_out_is_a_file_the_site_reads_with_its_translation()
    {
        using (var store = Open())
        {
            store.Import(Sample, "an editor");
            var exported = store.Export(Collection)!;
            var folder = Path.Combine(_folder, "content", "exported");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "shahnameh-pilot.json"), exported.Json);
            File.WriteAllText(Path.Combine(folder, "shahnameh-pilot.fa.json"), exported.Translations["fa"]);
        }

        var report = ContentCheck.File(Path.Combine(_folder, "content", "exported", "shahnameh-pilot.json"));
        var collection = PilotSample.Load(Path.Combine(_folder, "content", "exported", "shahnameh-pilot.json"));

        Assert.True(report.IsUsable, string.Join('\n', report.Problems));
        Assert.Equal(20, collection.Relations.Length);
        Assert.Equal(8, collection.Events.Length);
        Assert.NotNull(collection.Entities.Single(entity => entity.Id.Value == "character-rostam").Label.In("fa"));
    }

    [Fact]
    public void The_store_keeps_what_it_holds_when_it_is_closed_and_opened_again()
    {
        using (var store = Open())
        {
            store.Import(Sample, "an editor");
        }

        using var again = Open();

        Assert.Equal([Collection], again.Collections);
        Assert.True(JsonNode.DeepEquals(Working(), JsonNode.Parse(again.Export(Collection)!.Json)));
    }

    [Fact]
    public void A_collection_the_store_does_not_hold_does_not_come_out()
    {
        using var store = Open();

        Assert.Null(store.Export("another-collection"));
        Assert.Empty(store.Collections);
    }

    // --- changes and their history ---

    [Fact]
    public void Every_record_that_comes_in_has_the_first_entry_of_its_history()
    {
        using var store = Open();
        store.Import(Sample, "an editor", "The first import.");

        var change = Assert.Single(store.History(Collection, "relations", "relation-02"));

        Assert.Equal((RecordChangeKind.Added, 1, "an editor", "The first import."), (change.Kind, change.Version, change.By, change.Reason));
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, change.At);
        Assert.Null(change.Before);
        Assert.Equal("Father of", (string?)JsonNode.Parse(change.After!)!["label"]);
    }

    [Fact]
    public void Bringing_the_same_file_in_again_changes_nothing()
    {
        using var store = Open();
        store.Import(Sample, "an editor");
        var before = store.History(Collection).Length;

        var again = store.Import(Sample, "another editor", "Nothing new.");

        Assert.Equal((0, 0, 0), (again.Added, again.Changed, again.Removed));
        Assert.Equal(before, store.History(Collection).Length);
    }

    [Fact]
    public void A_record_that_reads_differently_becomes_a_change_with_who_when_why_and_the_record_before_and_after()
    {
        using var store = Open();
        store.Import(Sample, "an editor");
        Change(sample => Record(sample, "entities", "character-rostam")["summary"] = "A corrected summary.");
        _clock.Advance(TimeSpan.FromHours(3));

        var result = store.Import(Sample, "a second editor", "The summary said too much.");
        var history = store.History(Collection, "entities", "character-rostam");

        Assert.Equal((0, 1, 0), (result.Added, result.Changed, result.Removed));
        Assert.Equal([RecordChangeKind.Added, RecordChangeKind.Changed], history.Select(change => change.Kind));
        var change = history[1];
        Assert.Equal((2, "a second editor", "The summary said too much."), (change.Version, change.By, change.Reason));
        Assert.Equal(new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc), change.At);
        Assert.NotEqual("A corrected summary.", (string?)JsonNode.Parse(change.Before!)!["summary"]);
        Assert.Equal("A corrected summary.", (string?)JsonNode.Parse(change.After!)!["summary"]);

        // The store holds the record as it is now; everything else is as it was.
        Assert.True(JsonNode.DeepEquals(Working(), JsonNode.Parse(store.Export(Collection)!.Json)));
        Assert.Single(store.History(Collection, "entities", "character-zal"));
    }

    [Fact]
    public void A_review_written_into_the_file_is_a_change_of_its_statement_like_any_other()
    {
        using var store = Open();
        store.Import(Sample, "an editor");
        Change(sample =>
        {
            var relation = Record(sample, "relations", "relation-02");
            relation["reviewStatus"] = "reviewed";
            relation["review"] = new JsonObject { ["by"] = "a reviewer", ["on"] = "2026-11-17", ["note"] = "Read against the text." };
        });

        store.Import(Sample, "a reviewer", "Reviewed.");
        var change = store.History(Collection, "relations", "relation-02")[^1];

        Assert.Equal("source_backed_editorial_review_pending", (string?)JsonNode.Parse(change.Before!)!["reviewStatus"]);
        Assert.Equal("reviewed", (string?)JsonNode.Parse(change.After!)!["reviewStatus"]);
    }

    [Fact]
    public void A_record_the_file_no_longer_has_is_taken_out_and_its_history_stays()
    {
        using var store = Open();
        store.Import(Sample, "an editor");
        Change(sample => sample["relations"]!.AsArray().Remove(Record(sample, "relations", "relation-18")));

        // A translation names only what the collection has: the text of the relation goes with it.
        var persian = JsonNode.Parse(File.ReadAllText(Persian))!.AsObject();
        persian["relations"]!.AsObject().Remove("relation-18");
        File.WriteAllText(Persian, persian.ToJsonString());

        var result = store.Import(Sample, "an editor", "Not told in the selected part.");
        var history = store.History(Collection, "relations", "relation-18");

        Assert.Empty(result.Problems);
        Assert.Equal((0, 1, 1), (result.Added, result.Changed, result.Removed));
        Assert.Equal([RecordChangeKind.Added, RecordChangeKind.Removed], history.Select(change => change.Kind));
        Assert.Equal("Hunts near the border", (string?)JsonNode.Parse(history[1].Before!)!["label"]);
        Assert.Null(history[1].After);
        Assert.Equal(19, JsonNode.Parse(store.Export(Collection)!.Json)!["relations"]!.AsArray().Count);
    }

    [Fact]
    public void A_changed_translation_and_a_changed_text_of_the_collection_itself_come_out_as_they_went_in()
    {
        using var store = Open();
        store.Import(Sample, "an editor");
        var persian = JsonNode.Parse(File.ReadAllText(Persian))!.AsObject();
        persian["relations"]!["relation-02"]!["label"] = "پدرِ او";
        File.WriteAllText(Persian, persian.ToJsonString());
        Change(sample => sample["publication"]!["note"] = "A note of the editors.");

        var result = store.Import(Sample, "a translator");
        var exported = store.Export(Collection)!;

        Assert.Empty(result.Problems);
        Assert.Equal((0, 1, 0), (result.Added, result.Changed, result.Removed));
        Assert.Equal([RecordChangeKind.Added, RecordChangeKind.Changed], store.History(Collection, EditorialStore.TranslationGroup, "fa").Select(change => change.Kind));
        Assert.Equal("پدرِ او", (string?)JsonNode.Parse(exported.Translations["fa"])!["relations"]!["relation-02"]!["label"]);
        Assert.Equal("A note of the editors.", (string?)JsonNode.Parse(exported.Json)!["publication"]!["note"]);
    }

    // --- what is refused ---

    [Fact]
    public void A_file_the_site_cannot_use_is_refused_with_its_problems_and_changes_nothing()
    {
        using var store = Open();
        store.Import(Sample, "an editor");
        var before = store.Export(Collection)!.Json;
        Change(sample => Record(sample, "entities", "character-rostam")["type"] = "Hero");

        var result = store.Import(Sample, "an editor");

        Assert.Null(result.CollectionId);
        Assert.Contains(result.Problems, problem => problem.StartsWith("character-rostam: type has the unknown value \"Hero\"", StringComparison.Ordinal));
        Assert.Equal(before, store.Export(Collection)!.Json);
    }

    [Fact]
    public void A_published_revision_is_not_brought_into_the_store()
    {
        Change(sample =>
        {
            foreach (var group in new[] { "relations", "events" })
            {
                foreach (var record in sample[group]!.AsArray())
                {
                    record!["reviewStatus"] = "reviewed";
                    record["review"] = new JsonObject { ["by"] = "an editor", ["on"] = "2026-11-16" };
                }
            }
        });
        var revision = ContentRevision.Make(new RevisionRequest(Sample, 1, new DateOnly(2026, 11, 17)));
        var published = Path.Combine(_folder, "content", "published", "shahnameh-pilot.json");
        Directory.CreateDirectory(Path.GetDirectoryName(published)!);
        File.WriteAllText(published, revision.Json);
        using var store = Open();

        var result = store.Import(published, "an editor");

        Assert.Equal(["shahnameh-pilot.json: the store holds the working state of the editors; a published revision is made from it, not brought into it."], result.Problems);
        Assert.Empty(store.Collections);
    }

    // --- the commands ---

    private (int Exit, string Output) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        return (ContentCommands.Run(arguments, output, new DateOnly(2026, 11, 17), _clock), output.ToString());
    }

    [Fact]
    public void The_commands_bring_a_file_in_write_it_out_where_the_site_can_read_it_and_tell_the_history()
    {
        var target = Path.Combine(_folder, "content", "exported", "shahnameh-pilot.json");

        var (brought, said) = Run("store", "import", Sample, "--store", Database, "--by", "an editor", "--reason", "The first import.");
        Change(sample => Record(sample, "events", "event-01")["summary"] = "A corrected step.");
        var (changed, saidAgain) = Run("store", "import", Sample, "--store", Database, "--by", "a second editor");
        var (written, saidOut) = Run("store", "export", Collection, "--store", Database, "--out", target);
        var (again, refusal) = Run("store", "export", Collection, "--store", Database, "--out", target);
        var (told, history) = Run("store", "history", Collection, "--store", Database, "--record", "event-01");

        Assert.Equal(ContentCommands.Done, brought);
        Assert.EndsWith(" added, 0 changed, 0 removed." + Environment.NewLine, said, StringComparison.Ordinal);
        Assert.Equal((ContentCommands.Done, $"{Collection}: 0 added, 1 changed, 0 removed." + Environment.NewLine), (changed, saidAgain));
        Assert.Equal(ContentCommands.Done, written);
        Assert.Contains("and 1 translation beside it", saidOut, StringComparison.Ordinal);
        Assert.Contains("The site can use it.", saidOut, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_folder, "content", "exported", "shahnameh-pilot.fa.json")));
        Assert.Equal(ContentCommands.NotUnderstood, again);
        Assert.Contains("never writes over a file", refusal, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, told);
        Assert.Contains("event-01: 2 changes.", history, StringComparison.Ordinal);
        Assert.Contains("Added    events/event-01  version 1  by an editor  – The first import.", history, StringComparison.Ordinal);
        Assert.Contains("Changed  events/event-01  version 2  by a second editor", history, StringComparison.Ordinal);
    }

    [Fact]
    public void The_commands_say_what_they_cannot_do()
    {
        Change(sample => Record(sample, "entities", "character-rostam")["type"] = "Hero");

        var (refused, why) = Run("store", "import", Sample, "--store", Database, "--by", "an editor");
        var (unknown, which) = Run("store", "export", "another-collection", "--store", Database, "--out", Path.Combine(_folder, "out.json"));
        var (untold, none) = Run("store", "history", "another-collection", "--store", Database);

        Assert.Equal(ContentCommands.NotUsable, refused);
        Assert.Contains("Not brought in:", why, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUsable, unknown);
        Assert.Contains("It holds: none.", which, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUsable, untold);
        Assert.Contains("holds no collection of that name", none, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("store")]
    [InlineData("store", "import", "file.json")]
    [InlineData("store", "import", "file.json", "--store", "x.db")]
    [InlineData("store", "import", "file.json", "--by", "an editor")]
    [InlineData("store", "import", "file.json", "--store", "x.db", "--by", "an editor", "--approve", "yes")]
    [InlineData("store", "export", "a-collection", "--store", "x.db")]
    [InlineData("store", "history", "a-collection")]
    [InlineData("store", "publish", "a-collection", "--store", "x.db")]
    public void A_command_of_the_store_that_is_not_asked_for_completely_does_nothing(params string[] arguments)
    {
        var (exit, text) = Run(arguments);

        Assert.Equal(ContentCommands.NotUnderstood, exit);
        Assert.Contains("store import <file> --store <database> --by <name>", text, StringComparison.Ordinal);
        Assert.False(File.Exists("x.db"));
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
