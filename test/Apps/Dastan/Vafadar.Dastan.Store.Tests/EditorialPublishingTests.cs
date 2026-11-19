using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// Publishing from the store (step 11.3): the next revision is made from what the store holds, by the rule of
/// what everybody may see, numbered after the last one and kept as it was published.
/// </summary>
public sealed class EditorialPublishingTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;
    private static readonly DateOnly Today = new(2026, 11, 17);

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    // The store with every statement of the collection reviewed.
    private EditorialStore Reviewed()
    {
        var store = _copy.Filled(files: true);
        foreach (var statement in store.Statements(Collection))
        {
            Assert.True(store.Review(Collection, statement.Statement, ReviewAct.Reviewed, "a reviewer", Today.AddDays(-1), "Read against the text.").Done);
        }

        return store;
    }

    // A revision as a site is given it: its file and its translations in a content folder with the media, and
    // nothing of the editors beside it.
    private string Deployed(ExportedCollection revision, string name)
    {
        var content = Path.Combine(_copy.Folder, "sites", name, "content");
        Directory.CreateDirectory(Path.Combine(content, "samples"));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_copy.Folder, "content", "media")))
        {
            Directory.CreateDirectory(Path.Combine(content, "media"));
            File.Copy(file, Path.Combine(content, "media", Path.GetFileName(file)));
        }

        var sample = Path.Combine(content, "samples", "shahnameh-pilot.json");
        File.WriteAllText(sample, revision.Json);
        foreach (var (language, json) in revision.Translations)
        {
            File.WriteAllText(Path.Combine(content, "samples", $"shahnameh-pilot.{language}.json"), json);
        }

        return sample;
    }

    [Fact]
    public void A_collection_with_statements_nobody_reviewed_is_not_published_and_each_of_them_is_named()
    {
        using var store = _copy.Filled();

        var result = store.Publish(Collection, Today, "an editor");

        Assert.Null(result.Revision);
        Assert.Equal(28, result.Problems.Length);
        Assert.Contains("relation-02: a published revision holds only reviewed statements; this one is SourcedReviewPending.", result.Problems);
        Assert.Empty(store.Revisions(Collection));
    }

    [Fact]
    public void The_first_revision_is_what_the_store_holds_marked_as_the_revision_it_is_and_the_store_keeps_it()
    {
        using var store = Reviewed();

        var result = store.Publish(Collection, Today, "an editor", "The first publication.");
        var revision = JsonNode.Parse(result.Revision!.Json)!;

        Assert.Empty(result.Problems);
        Assert.Equal(1, result.Number);
        Assert.Equal(("published_revision", true, 1), ((string?)revision["publication"]!["state"], (bool)revision["publication"]!["publicReleaseApproved"]!, (int)revision["publication"]!["revision"]!["number"]!));
        Assert.Single(revision["publication"]!["history"]!.AsArray());
        Assert.Equal(["fa"], result.Revision.Translations.Keys);
        Assert.Equal([new RevisionInfo(1, Today, "The first publication.", "an editor")], store.Revisions(Collection));
        Assert.Equal(result.Revision.Json, store.Revision(Collection, 1)!.Json);

        // The working state is still the working state: publishing changes no record.
        Assert.Equal("editorial_sample", (string?)JsonNode.Parse(store.Export(Collection)!.Json)!["publication"]!["state"]);
        Assert.Equal(2, store.History(Collection, "relations", "relation-02").Length);
    }

    [Fact]
    public void A_revision_of_the_store_starts_a_public_site_that_has_its_content_folder_and_nothing_else()
    {
        using var store = Reviewed();

        var sample = Deployed(store.Publish(Collection, Today, "an editor").Revision!, "first");
        var collection = PilotSample.Load(sample, reviewedTextsOnly: true);

        Assert.True(ContentCheck.File(sample).IsUsable);
        Assert.Equal(new PublicationRevision(1, Today), collection.Info.Publication.Revision);
        Assert.NotNull(Projection.Of(collection, ReadingLevel.Opening, Audience.Public));
    }

    [Fact]
    public void The_next_revision_follows_the_last_one_and_says_what_was_changed_and_what_was_withdrawn()
    {
        using var store = Reviewed();
        var first = store.Publish(Collection, Today, "an editor").Revision!;
        var rostam = WorkingCopy.Stored(store, "entities", "character-rostam");
        rostam["summary"] = "A corrected summary.";
        Assert.True(store.Put(Collection, "entities", rostam, "a writer", "The summary said too much.").Done);
        Assert.True(store.Review(Collection, "relation-18", ReviewAct.Withdrawn, "an editor", Today, "Not told in the selected part.").Done);

        var result = store.Publish(Collection, Today.AddDays(7), "an editor", "A correction.");
        var revision = JsonNode.Parse(result.Revision!.Json)!;
        var changes = revision["publication"]!["history"]![1]!["changes"]!.AsArray();

        Assert.Empty(result.Problems);
        Assert.Equal((2, (0, 1, 1)), (result.Number, result.Changes));
        Assert.Equal(
            [("changed", "character-rostam", null), ("withdrawn", "relation-18", "Not told in the selected part.")],
            changes.Select(change => ((string)change!["kind"]!, (string)change["record"]!, (string?)change["reason"])));

        // The revision does not hold the withdrawn statement, and neither does its translation; the store does.
        Assert.Equal(19, revision["relations"]!.AsArray().Count);
        Assert.False(JsonNode.Parse(result.Revision.Translations["fa"])!["relations"]!.AsObject().ContainsKey("relation-18"));
        Assert.Equal("withdrawn", (string?)WorkingCopy.Stored(store, "relations", "relation-18")["reviewStatus"]);

        // The site reads it with its history, and the revision before is as it was published.
        var read = PilotSample.Load(Deployed(result.Revision, "second"));
        Assert.Equal([1, 2], read.Info.History.Select(entry => entry.Number));
        Assert.Equal(RevisionChangeKind.Withdrawn, read.Info.History[1].Changes[1].Kind);
        Assert.Equal(first.Json, store.Revision(Collection, 1)!.Json);
        Assert.Equal([1, 2], store.Revisions(Collection).Select(info => info.Number));
    }

    [Fact]
    public void A_statement_changed_after_its_review_stops_the_next_revision_until_it_is_reviewed_again()
    {
        using var store = Reviewed();
        store.Publish(Collection, Today, "an editor");
        var relation = WorkingCopy.Stored(store, "relations", "relation-02");
        relation["label"] = "Father and teacher of";
        relation["reviewStatus"] = "source_backed_editorial_review_pending";
        relation.Remove("review");
        Assert.True(store.Put(Collection, "relations", relation, "a writer", "The passage says more.").Done);

        var refused = store.Publish(Collection, Today.AddDays(1), "an editor");
        store.Review(Collection, "relation-02", ReviewAct.Reviewed, "a reviewer", Today.AddDays(1), "Read again.");
        var published = store.Publish(Collection, Today.AddDays(1), "an editor");

        Assert.Equal(["relation-02: a published revision holds only reviewed statements; this one is SourcedReviewPending."], refused.Problems);
        Assert.Equal(2, published.Number);
        Assert.Equal([1, 2], store.Revisions(Collection).Select(info => info.Number));
    }

    [Fact]
    public void What_the_store_does_not_hold_is_not_published_and_not_written_again()
    {
        using var store = Reviewed();

        Assert.Equal(["another-collection: the store holds no collection of that name."], store.Publish("another-collection", Today, "an editor").Problems);
        Assert.Null(store.Revision(Collection, 1));
        Assert.Empty(store.Revisions("another-collection"));
    }

    // --- the commands ---

    private (int Exit, string Output) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        return (ContentCommands.Run(arguments, output, Today, _copy.Clock), output.ToString());
    }

    [Fact]
    public void The_commands_publish_a_revision_list_the_revisions_and_write_one_again()
    {
        var first = Path.Combine(_copy.Folder, "content", "revision-1", "shahnameh-pilot.json");
        var again = Path.Combine(_copy.Folder, "content", "revision-1-again", "shahnameh-pilot.json");
        Run("store", "import", _copy.Sample, "--store", _copy.Database, "--by", "an editor");

        var (early, why) = Run("store", "publish", Collection, "--store", _copy.Database, "--by", "an editor", "--date", "2026-11-17", "--out", first);
        using (var store = _copy.Open(files: true))
        {
            foreach (var statement in store.Statements(Collection))
            {
                store.Review(Collection, statement.Statement, ReviewAct.Reviewed, "a reviewer", Today, null);
            }
        }

        var (published, said) = Run("store", "publish", Collection, "--store", _copy.Database, "--by", "an editor", "--date", "2026-11-17", "--out", first, "--note", "The first publication.", "--files", _copy.Samples);
        var (over, refusal) = Run("store", "publish", Collection, "--store", _copy.Database, "--by", "an editor", "--date", "2026-11-18", "--out", first);
        var (listed, list) = Run("store", "revisions", Collection, "--store", _copy.Database);
        var (written, _) = Run("store", "revision", Collection, "1", "--store", _copy.Database, "--out", again);
        var (unknown, which) = Run("store", "revision", Collection, "7", "--store", _copy.Database, "--out", Path.Combine(_copy.Folder, "seven.json"));

        Assert.Equal(ContentCommands.NotUsable, early);
        Assert.Contains("No revision: 28 reasons. Nothing was published.", why, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, published);
        Assert.Contains("Revision 1 of 2026-11-17: 0 added, 0 changed, 0 removed against the revision before. The store keeps it.", said, StringComparison.Ordinal);
        Assert.Contains("and 1 translation beside it", said, StringComparison.Ordinal);
        Assert.Contains("The site can use it.", said, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUnderstood, over);
        Assert.Contains("never writes over a file", refusal, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, listed);
        Assert.Contains($"{Collection}: 1 revision published; the site shows none.", list, StringComparison.Ordinal);
        Assert.Contains("1  2026-11-17  by an editor  – The first publication.", list, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, written);
        Assert.Equal(File.ReadAllText(first), File.ReadAllText(again));
        Assert.Equal(ContentCommands.NotUsable, unknown);
        Assert.Contains("did not publish a revision 7", which, StringComparison.Ordinal);
    }

    [Fact]
    public void The_commands_release_a_revision_to_the_site_and_withdraw_it_and_the_list_says_which_is_shown()
    {
        Run("store", "import", _copy.Sample, "--store", _copy.Database, "--by", "an editor");
        using (var store = _copy.Open(files: true))
        {
            foreach (var statement in store.Statements(Collection))
            {
                store.Review(Collection, statement.Statement, ReviewAct.Reviewed, "a reviewer", Today, null);
            }

            Assert.Empty(store.Publish(Collection, Today, "an editor").Problems);
        }

        // Publishing released nothing.
        Assert.Equal(ContentCommands.NotUsable, Run("store", "withdraw", Collection, "--store", _copy.Database, "--by", "a publisher").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("store", "release", Collection, "4", "--store", _copy.Database, "--by", "a publisher").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("store", "release", "no-such-collection", "1", "--store", _copy.Database, "--by", "a publisher").Exit);

        var (released, said) = Run("store", "release", Collection, "1", "--store", _copy.Database, "--by", "a publisher");
        Assert.Equal(ContentCommands.Done, released);
        Assert.Contains($"The site shows revision 1 of {Collection} when it is started next.", said, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUsable, Run("store", "release", Collection, "1", "--store", _copy.Database, "--by", "a publisher").Exit);

        var list = Run("store", "revisions", Collection, "--store", _copy.Database).Output;
        Assert.Contains("1 revision published; the site shows revision 1.", list, StringComparison.Ordinal);
        Assert.Contains("by an editor  (shown by the site)", list, StringComparison.Ordinal);

        Assert.Equal(ContentCommands.Done, Run("store", "withdraw", Collection, "--store", _copy.Database, "--by", "a publisher").Exit);
        Assert.Contains("the site shows none", Run("store", "revisions", Collection, "--store", _copy.Database).Output, StringComparison.Ordinal);

        // Not asked for completely, nothing is said.
        Assert.Equal(ContentCommands.NotUnderstood, Run("store", "release", Collection, "one", "--store", _copy.Database, "--by", "a publisher").Exit);
        Assert.Equal(ContentCommands.NotUnderstood, Run("store", "release", Collection, "1", "--store", _copy.Database).Exit);
        Assert.Equal(ContentCommands.NotUnderstood, Run("store", "withdraw", Collection, "--store", _copy.Database).Exit);
    }

    [Fact]
    public void A_revision_is_kept_with_the_hash_of_what_was_published()
    {
        using var store = Reviewed();
        Assert.Empty(store.Publish(Collection, Today, "an editor").Problems);

        using var connection = new Npgsql.NpgsqlConnection(_copy.Database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """select "Json"::text, "Translations"::text, "Hash" from published."Revisions" """;
        using var row = command.ExecuteReader();
        Assert.True(row.Read());
        Assert.Equal(store.Revision(Collection, 1)!.Json, row.GetString(0));
        Assert.Equal(Vafadar.Dastan.Data.PublishedRevision.HashOf(row.GetString(0), row.GetString(1)), row.GetString(2));
        Assert.Equal(64, row.GetString(2).Length);
    }

    [Theory]
    [InlineData("store", "publish", "a-collection", "--store", "x.db", "--by", "an editor", "--out", "r.json")]
    [InlineData("store", "publish", "a-collection", "--store", "x.db", "--by", "an editor", "--date", "17.11.2026", "--out", "r.json")]
    [InlineData("store", "publish", "a-collection", "--store", "x.db", "--date", "2026-11-17", "--out", "r.json")]
    [InlineData("store", "publish", "a-collection", "--store", "x.db", "--by", "an editor", "--date", "2026-11-17", "--out", "r.json", "--number", "5")]
    [InlineData("store", "revision", "a-collection", "one", "--store", "x.db", "--out", "r.json")]
    [InlineData("store", "revision", "a-collection", "1", "--store", "x.db")]
    [InlineData("store", "revisions", "a-collection")]
    public void Publishing_that_is_not_asked_for_completely_is_not_done(params string[] arguments)
    {
        var (exit, text) = Run(arguments);

        Assert.Equal(ContentCommands.NotUnderstood, exit);
        Assert.Contains("store publish <collection>", text, StringComparison.Ordinal);
        Assert.False(File.Exists("x.db"));
    }
}
