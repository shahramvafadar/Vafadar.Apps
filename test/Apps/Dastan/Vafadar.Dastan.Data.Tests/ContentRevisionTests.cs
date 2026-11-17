using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Making a published revision out of the working file of the editors (step 10.1, ADR 0009): what is refused,
/// what the revision holds, what it says it changed – and the command that does it. The collections here are
/// made from the template of the writers, in a folder of the test.
/// </summary>
public sealed class ContentRevisionTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 17);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-revision-" + Guid.NewGuid().ToString("N"));

    public ContentRevisionTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Sample => Path.Combine(_folder, "samples", "tale.json");

    private string Revision(int number) => Path.Combine(_folder, "published", $"tale-r{number}.json");

    // The template with its two statements reviewed, as the editors hand it over to be published.
    private JsonObject Reviewed()
    {
        var sample = JsonNode.Parse(ContentTemplates.Collection("tale", Today))!.AsObject();
        foreach (var group in new[] { "relations", "events" })
        {
            foreach (var record in sample[group]!.AsArray())
            {
                record!["reviewStatus"] = "reviewed";
                record["review"] = new JsonObject { ["by"] = "an editor", ["on"] = "2026-11-16", ["note"] = "Read against the text." };
            }
        }

        return sample;
    }

    private void Write(string file, JsonObject sample)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, sample.ToJsonString());
    }

    private RevisionResult Make(JsonObject sample, int number = 1, int? previous = null, string? note = null)
    {
        Write(Sample, sample);
        return ContentRevision.Make(new RevisionRequest(Sample, number, Today, previous is { } before ? Revision(before) : null, note));
    }

    private void Publish(JsonObject sample, int number, int? previous = null)
    {
        var result = Make(sample, number, previous);
        Assert.True(result.Json is not null, string.Join('\n', result.Problems));
        Directory.CreateDirectory(Path.GetDirectoryName(Revision(number))!);
        File.WriteAllText(Revision(number), result.Json);
    }

    [Fact]
    public void A_file_with_statements_that_are_not_reviewed_becomes_no_revision_and_each_of_them_is_named()
    {
        var result = Make(JsonNode.Parse(ContentTemplates.Collection("tale", Today))!.AsObject());

        Assert.Null(result.Json);
        Assert.Equal(
            [
                "relation-01: a published revision holds only reviewed statements; this one is SourcedReviewPending.",
                "event-01: a published revision holds only reviewed statements; this one is SourcedReviewPending.",
            ],
            result.Problems);
    }

    [Fact]
    public void A_reviewed_statement_without_the_record_of_its_review_stops_the_revision()
    {
        var sample = Reviewed();
        sample["relations"]![0]!.AsObject().Remove("review");

        var result = Make(sample);

        Assert.Equal(["relation-01: a published revision says who reviewed a statement and when."], result.Problems);
    }

    [Fact]
    public void The_first_revision_is_the_file_of_the_editors_marked_as_the_revision_it_is()
    {
        var result = Make(Reviewed(), note: "The first publication of the tale.");
        var revision = JsonNode.Parse(result.Json!)!;
        var publication = revision["publication"]!;

        Assert.Empty(result.Problems);
        Assert.Equal("published_revision", (string?)publication["state"]);
        Assert.True((bool)publication["publicReleaseApproved"]!);
        Assert.Equal((1, "2026-11-17"), ((int)publication["revision"]!["number"]!, (string?)publication["revision"]!["publishedAt"]));
        var entry = Assert.Single(publication["history"]!.AsArray());
        Assert.Equal("The first publication of the tale.", (string?)entry!["note"]);
        Assert.Empty(entry["changes"]!.AsArray());
        Assert.Equal((0, 0, 0), result.Changes);

        // Everything else is as the editors wrote it, and the working file itself is not touched.
        Assert.True(JsonNode.DeepEquals(Reviewed()["entities"], revision["entities"]));
        Assert.Equal("editorial_sample", (string?)JsonNode.Parse(File.ReadAllText(Sample))!["publication"]!["state"]);
    }

    [Fact]
    public void A_revision_is_a_file_the_site_reads_and_shows_to_everybody()
    {
        Publish(Reviewed(), 1);

        var report = ContentCheck.File(Revision(1));
        var collection = PilotSample.Load(Revision(1));

        Assert.True(report.IsUsable, string.Join('\n', report.Problems));
        Assert.Contains(("released to", "everybody"), report.Summary);
        Assert.Equal(new PublicationRevision(1, Today), collection.Info.Publication.Revision);
        Assert.Equal(new ReviewRecord("an editor", new DateOnly(2026, 11, 16), "Read against the text."), collection.Relations[0].Reviewed);
        Assert.NotNull(Projection.Of(collection, ReadingLevel.Opening, Audience.Public));
    }

    [Fact]
    public void The_next_revision_keeps_the_history_and_says_what_it_adds_changes_and_removes()
    {
        Publish(Reviewed(), 1);
        var next = Reviewed();
        next["entities"]![2]!["summary"] = "A corrected summary.";
        next["entities"]!.AsArray().Add(JsonNode.Parse(
            """
            { "id": "character-second", "type": "Character", "label": "A second name", "summary": "New in this revision.",
              "sourceIds": ["src-tale-text"], "spoilerLevel": 0 }
            """));
        next["events"]![0]!["entityAssociations"]!.AsArray().RemoveAt(1);
        next["entities"]!.AsArray().RemoveAt(3);

        var result = Make(next, 2, previous: 1, note: "A summary corrected, a name added, the place removed.");
        var history = JsonNode.Parse(result.Json!)!["publication"]!["history"]!.AsArray();

        Assert.Empty(result.Problems);
        Assert.Equal((1, 2, 1), result.Changes);
        Assert.Equal([1, 2], history.Select(entry => (int)entry!["number"]!));
        Assert.Equal(
            [
                ("changed", "character-first", "entity"),
                ("added", "character-second", "entity"),
                ("removed", "place-first", "entity"),
                ("changed", "event-01", "event"),
            ],
            history[1]!["changes"]!.AsArray().Select(change => ((string)change!["kind"]!, (string)change["record"]!, (string)change["recordKind"]!)));
        Assert.Equal("A second name", (string?)history[1]!["changes"]![1]!["label"]);

        // The revision reads back with its history, as the pages will show it.
        File.WriteAllText(Revision(2), result.Json);
        var read = PilotSample.Load(Revision(2)).Info.History;
        Assert.Equal(2, read.Length);
        Assert.Equal([RevisionChangeKind.Changed, RevisionChangeKind.Added, RevisionChangeKind.Removed, RevisionChangeKind.Changed], read[1].Changes.Select(change => change.Kind));
        Assert.Equal("A summary corrected, a name added, the place removed.", read[1].Note);
    }

    [Fact]
    public void A_revision_follows_the_one_before_it_and_no_other()
    {
        Publish(Reviewed(), 1);

        var skipped = Make(Reviewed(), 3, previous: 1);
        var alone = Make(Reviewed(), 2);
        var zero = Make(Reviewed(), 0);
        Write(Revision(9), JsonNode.Parse(ContentTemplates.Collection("tale", Today))!.AsObject());
        var fromSample = Make(Reviewed(), 2, previous: 9);
        var other = Reviewed();
        other["datasetId"] = "another-tale";
        var foreign = Make(other, 2, previous: 1);

        Assert.Equal(["revision 3 does not follow revision 1 of tale-r1.json."], skipped.Problems);
        Assert.Equal(["revision 2 follows revision 1: name the file of that revision."], alone.Problems);
        Assert.Equal(["the number of a revision starts at 1."], zero.Problems);
        Assert.Equal(["tale-r9.json: the file before is not a published revision."], fromSample.Problems);
        Assert.Equal(["tale-r1.json: it is a revision of tale, not of another-tale."], foreign.Problems);
    }

    [Fact]
    public void A_revision_is_made_from_the_working_file_and_not_from_a_revision()
    {
        Publish(Reviewed(), 1);

        var result = ContentRevision.Make(new RevisionRequest(Revision(1), 2, Today, Revision(1)));

        Assert.Equal(["tale-r1.json: a revision is made from the working file of the editors, an editorial_sample; this file is a revision already."], result.Problems);
    }

    [Fact]
    public void A_working_file_the_site_cannot_read_becomes_no_revision()
    {
        var broken = Reviewed();
        broken["entities"]![2]!["type"] = "Hero";

        var result = Make(broken);

        Assert.Null(result.Json);
        Assert.Contains(result.Problems, problem => problem.StartsWith("character-first: type has the unknown value \"Hero\"", StringComparison.Ordinal));
    }

    // --- withdrawn statements (step 10.2, GOV-004) ---

    private static JsonObject Withdrawn(JsonObject sample, string? reason = "The source does not say this.")
    {
        var relation = sample["relations"]![0]!.AsObject();
        relation["reviewStatus"] = "withdrawn";
        relation["review"] = new JsonObject { ["by"] = "an editor", ["on"] = "2026-11-16" };
        if (reason is not null)
        {
            relation["review"]!["note"] = reason;
        }

        return sample;
    }

    [Fact]
    public void A_withdrawn_statement_stays_in_the_working_file_and_is_no_part_of_the_collection()
    {
        Write(Sample, Withdrawn(Reviewed()));

        var report = ContentCheck.File(Sample);
        var collection = PilotSample.Load(Sample);

        Assert.True(report.IsUsable, string.Join('\n', report.Problems));
        Assert.Empty(collection.Relations);
        Assert.Single(collection.Events);
    }

    [Fact]
    public void A_statement_is_not_withdrawn_without_saying_why()
    {
        Write(Sample, Withdrawn(Reviewed(), reason: null));

        var report = ContentCheck.File(Sample);

        Assert.Equal(["relation-01: a withdrawn statement says in its review who withdrew it and when, and in the note of the review why."], report.Problems);
    }

    [Fact]
    public void A_revision_names_the_statement_it_withdraws_with_the_reason_and_does_not_hold_it()
    {
        Publish(Reviewed(), 1);

        var result = Make(Withdrawn(Reviewed()), 2, previous: 1);
        var revision = JsonNode.Parse(result.Json!)!;
        var change = Assert.Single(revision["publication"]!["history"]![1]!["changes"]!.AsArray())!;

        Assert.Empty(result.Problems);
        Assert.Empty(revision["relations"]!.AsArray());
        Assert.Equal(("withdrawn", "relation-01", "relation", "The source does not say this."), ((string)change["kind"]!, (string)change["record"]!, (string)change["recordKind"]!, (string)change["reason"]!));
        Assert.Equal((0, 0, 1), result.Changes);

        // The revision reads back: the withdrawal is part of its history, the statement is not part of it.
        File.WriteAllText(Revision(2), result.Json);
        var read = PilotSample.Load(Revision(2));
        Assert.Empty(read.Relations);
        Assert.Equal((RevisionChangeKind.Withdrawn, "The source does not say this."), (read.Info.History[1].Changes[0].Kind, read.Info.History[1].Changes[0].Reason));

        // The working file still holds the statement, with who withdrew it.
        Assert.Equal("withdrawn", (string?)JsonNode.Parse(File.ReadAllText(Sample))!["relations"]![0]!["reviewStatus"]);
    }

    [Fact]
    public void A_statement_withdrawn_before_it_was_ever_published_leaves_no_trace_in_a_revision()
    {
        var result = Make(Withdrawn(Reviewed()));
        var revision = JsonNode.Parse(result.Json!)!;

        Assert.Empty(revision["relations"]!.AsArray());
        Assert.Empty(revision["publication"]!["history"]![0]!["changes"]!.AsArray());
    }

    [Fact]
    public void A_published_revision_does_not_hold_a_withdrawn_statement()
    {
        Publish(Reviewed(), 1);
        var revision = JsonNode.Parse(File.ReadAllText(Revision(1)))!.AsObject();
        Write(Revision(1), Withdrawn(revision));

        var report = ContentCheck.File(Revision(1));

        Assert.Contains("relation-01: a withdrawn statement is no part of a published revision; its history names it with the reason.", report.Problems);
    }

    // --- the command ---

    private static (int Exit, string Output) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        return (ContentCommands.Run(arguments, output, Today), output.ToString());
    }

    [Fact]
    public void The_command_writes_a_revision_to_a_new_file_and_never_over_one()
    {
        Write(Sample, Reviewed());

        var (written, text) = Run("revision", Sample, "--number", "1", "--date", "2026-11-17", "--out", Revision(1), "--note", "First.");
        var (again, refusal) = Run("revision", Sample, "--number", "1", "--date", "2026-11-17", "--out", Revision(1));

        Assert.Equal(ContentCommands.Done, written);
        Assert.Contains("Revision 1 of 2026-11-17: 0 added, 0 changed, 0 removed", text, StringComparison.Ordinal);
        Assert.True(ContentCheck.File(Revision(1)).IsUsable);
        Assert.Equal(ContentCommands.NotUnderstood, again);
        Assert.Contains("never written over a file", refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void The_command_says_why_there_is_no_revision_and_writes_nothing()
    {
        Write(Sample, JsonNode.Parse(ContentTemplates.Collection("tale", Today))!.AsObject());

        var (exit, text) = Run("revision", Sample, "--number", "1", "--date", "2026-11-17", "--out", Revision(1));

        Assert.Equal(ContentCommands.NotUsable, exit);
        Assert.Contains("No revision: 2 reasons.", text, StringComparison.Ordinal);
        Assert.Contains("relation-01: a published revision holds only reviewed statements", text, StringComparison.Ordinal);
        Assert.False(File.Exists(Revision(1)));
    }

    [Theory]
    [InlineData("revision")]
    [InlineData("revision", "file.json")]
    [InlineData("revision", "file.json", "--number", "1", "--date", "2026-11-17")]
    [InlineData("revision", "file.json", "--number", "one", "--date", "2026-11-17", "--out", "x.json")]
    [InlineData("revision", "file.json", "--number", "1", "--date", "17.11.2026", "--out", "x.json")]
    [InlineData("revision", "file.json", "--number", "1", "--number", "2", "--date", "2026-11-17", "--out", "x.json")]
    [InlineData("revision", "file.json", "--number", "1", "--date", "2026-11-17", "--out", "x.json", "--approve")]
    public void A_revision_that_is_not_asked_for_completely_is_not_made(params string[] arguments)
    {
        var (exit, text) = Run(arguments);

        Assert.Equal(ContentCommands.NotUnderstood, exit);
        Assert.Contains("revision <file> --number <n>", text, StringComparison.Ordinal);
    }
}
