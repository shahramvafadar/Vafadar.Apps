using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// A published revision as a file (GOV-021, D-044): the state of the publication, the revision, the reviewed
/// statements, the media allowed for the public and the texts the public gets. The revisions here are made in
/// memory from the sample of the repository, which is an editorial sample and stays one.
/// </summary>
public sealed class PublishedRevisionReaderTests
{
    private static string Persian => File.ReadAllText(Path.Combine(SampleFiles.Samples, "shahnameh-pilot.fa.json"));

    // The sample as a published revision: approved, numbered, with every relation and event reviewed.
    private static void Publish(JsonObject sample)
    {
        var publication = sample["publication"]!.AsObject();
        publication["state"] = "published_revision";
        publication["publicReleaseApproved"] = true;
        publication["revision"] = new JsonObject { ["number"] = 3, ["publishedAt"] = "2026-11-17" };
        foreach (var group in new[] { "relations", "events" })
        {
            foreach (var record in sample[group]!.AsArray())
            {
                record!["reviewStatus"] = "reviewed";
                record["review"] = new JsonObject { ["by"] = "an editor", ["on"] = "2026-11-16" };
            }
        }
    }

    private static Action<JsonObject> Published(Action<JsonObject>? change = null) => sample =>
    {
        Publish(sample);
        change?.Invoke(sample);
    };

    [Fact]
    public void The_sample_of_the_repository_is_an_editorial_sample_and_no_revision()
    {
        var publication = PilotSample.Load(SampleFiles.PilotPath).Info.Publication;

        Assert.Equal(new PublicationState(LocalDemoAllowed: true, PublicReleaseApproved: false, Revision: null), publication);
    }

    [Fact]
    public void A_published_revision_is_read_with_its_number_and_its_day_and_is_shown_to_the_public()
    {
        var collection = SampleFiles.Read(Published());

        Assert.Equal(new PublicationRevision(3, new DateOnly(2026, 11, 17)), collection.Info.Publication.Revision);
        Assert.True(collection.Info.Publication.PublicReleaseApproved);
        Assert.All(collection.Relations, relation => Assert.Equal(ReviewStatus.Reviewed, relation.Review));
        Assert.NotNull(Projection.Of(collection, ReadingLevel.Opening, Audience.Public));
    }

    [Fact]
    public void An_editorial_sample_cannot_be_approved_and_states_no_revision()
    {
        var approved = SampleFiles.Problems(sample => sample["publication"]!["publicReleaseApproved"] = true);
        var numbered = SampleFiles.Problems(sample => sample["publication"]!["revision"] = new JsonObject { ["number"] = 1, ["publishedAt"] = "2026-11-17" });

        Assert.Contains("sample.publication: an editorial sample cannot be approved for public release.", approved);
        Assert.Contains("sample.publication: revision belongs to a published revision; an editorial sample is none.", numbered);
    }

    [Theory]
    [InlineData("state", "\"published\"", "sample.publication: the publication state published is not understood; a file is an editorial_sample or a published_revision.")]
    [InlineData("revision", "null", "sample.publication: revision is missing.")]
    [InlineData("revision", "{ \"number\": 3 }", "sample.publication.revision: publishedAt is missing.")]
    [InlineData("revision", "{ \"number\": 0, \"publishedAt\": \"2026-11-17\" }", "collection: the number of a published revision starts at 1.")]
    [InlineData("revision", "{ \"number\": 3, \"publishedAt\": \"17.11.2026\" }", "sample.publication.revision: publishedAt")]
    public void What_is_wrong_with_the_publication_of_a_revision_is_named(string field, string value, string problem)
    {
        var problems = SampleFiles.Problems(Published(sample => sample["publication"]![field] = JsonNode.Parse(value)));

        Assert.Contains(problems, found => found.StartsWith(problem, StringComparison.Ordinal));
    }

    [Fact]
    public void A_revision_with_a_statement_that_is_not_reviewed_is_refused_and_names_every_such_statement()
    {
        var problems = SampleFiles.Problems(Published(sample =>
        {
            foreach (var record in new[] { sample.Record("relations", "relation-02"), sample.Record("events", "event-01") })
            {
                record["reviewStatus"] = "source_backed_editorial_review_pending";
                record.Remove("review");
            }
        }));

        Assert.Equal(
            [
                "relation-02: a published revision holds only reviewed statements; this one is SourcedReviewPending.",
                "event-01: a published revision holds only reviewed statements; this one is SourcedReviewPending.",
            ],
            problems);
    }

    [Fact]
    public void A_disputed_statement_is_part_of_a_revision_as_disputed()
    {
        var collection = SampleFiles.Read(Published(sample =>
        {
            var relation = sample.Record("relations", "relation-02");
            relation["reviewStatus"] = "disputed";
            relation["review"]!["note"] = "Two manuscripts tell this differently.";
        }));

        Assert.Equal(ReviewStatus.Disputed, collection.Find(new RelationId("relation-02"))!.Review);
    }

    [Fact]
    public void For_the_public_a_draft_translation_is_checked_and_left_out()
    {
        var sample = SampleFiles.Pilot();
        var everybody = PilotSample.Read(sample.ToJsonString(), SampleFiles.Options(translations: Persian));
        var options = SampleFiles.Options(translations: Persian);
        var reviewedOnly = PilotSample.Read(sample.ToJsonString(), new PilotSampleOptions
        {
            SampleDirectory = options.SampleDirectory,
            ContentRoot = options.ContentRoot,
            ReviewRoot = options.ReviewRoot,
            Translations = options.Translations,
            ReviewedTextsOnly = true,
        });

        var rostam = new EntityId("character-rostam");
        Assert.NotNull(everybody.Find(rostam)!.Label.In("fa"));
        Assert.All(reviewedOnly.Entities, entity => Assert.Equal(["en"], entity.Label.Languages));
        Assert.All(reviewedOnly.Relations, relation => Assert.Equal(["en"], relation.Label.Languages));
        Assert.All(reviewedOnly.Events, storyEvent => Assert.Equal(["en"], storyEvent.Summary.Languages));
        Assert.All(reviewedOnly.Info.ReadingStages, stage => Assert.Equal(["en"], stage.Label.Languages));
    }

    [Fact]
    public void For_the_public_a_reviewed_translation_is_taken_and_a_wrong_draft_is_still_refused()
    {
        var reviewed = JsonNode.Parse(Persian)!.AsObject();
        reviewed["review"] = "reviewed";
        var wrong = JsonNode.Parse(Persian)!.AsObject();
        wrong["entities"]!["character-nobody"] = new JsonObject { ["label"] = "هیچ‌کس" };
        var options = SampleFiles.Options();
        PilotSampleOptions With(string translation) => new()
        {
            SampleDirectory = options.SampleDirectory,
            ContentRoot = options.ContentRoot,
            ReviewRoot = options.ReviewRoot,
            Translations = [translation],
            ReviewedTextsOnly = true,
        };

        var taken = PilotSample.Read(SampleFiles.Pilot().ToJsonString(), With(reviewed.ToJsonString()));
        var refused = Assert.Throws<SampleFormatException>(() => PilotSample.Read(SampleFiles.Pilot().ToJsonString(), With(wrong.ToJsonString())));

        Assert.Equal(TextReview.Reviewed, taken.Find(new EntityId("character-rostam"))!.Label.In("fa")!.Value.Review);
        Assert.Contains("translation fa: entities/character-nobody/label translates nothing the sample has.", refused.Problems);
    }

    [Fact]
    public void An_item_is_shown_to_the_public_only_when_the_file_says_so()
    {
        const string Diagram = "media-dastan-family-illustration";
        var plain = SampleFiles.Read();
        var allowed = SampleFiles.Read(sample => sample.Record("media", Diagram)["publicRenderEligibility"] = new JsonObject { ["allowed"] = true, ["reason"] = "Made by the project." });
        var museum = SampleFiles.Problems(sample =>
            sample["media"]!.AsArray().Select(node => node!.AsObject()).First(item => (string?)item["rights"]!["status"] == "permission_not_verified")["publicRenderEligibility"] = new JsonObject { ["allowed"] = true });

        Assert.All(plain.Media, item => Assert.False(item.PublicRenderAllowed));
        Assert.True(allowed.Find(new MediaId(Diagram))!.PublicRenderAllowed);
        Assert.Contains(museum, problem => problem.EndsWith(": only original project content and items with verified open access are shown to the public.", StringComparison.Ordinal));
    }

    // --- a comparison ---

    private static string ComparisonFile(string name) => Path.Combine(SampleFiles.Content, "comparisons", name);

    private static JsonObject Comparison(bool reviewed)
    {
        var sample = JsonNode.Parse(File.ReadAllText(ComparisonFile("iran-china-rome.json")))!.AsObject();
        sample["publication"]!["publicReleaseApproved"] = true;
        if (reviewed)
        {
            sample["status"] = "reviewed";
            sample["sources"] = new JsonArray(new JsonObject
            {
                ["id"] = "src-test",
                ["title"] = "A source of the tests",
                ["url"] = "https://example.org/source",
                ["kind"] = "scholarlyReference",
                ["accessedAt"] = "2026-11-17",
            });
            foreach (var lane in sample["lanes"]!.AsArray())
            {
                foreach (var item in lane!["periods"]!.AsArray().Concat(lane["events"]!.AsArray()))
                {
                    item!["reviewStatus"] = "reviewed";
                    item["sourceIds"] = new JsonArray("src-test");
                }
            }
        }

        return sample;
    }

    [Fact]
    public void A_comparison_is_approved_for_the_public_only_when_it_and_all_its_statements_are_reviewed_and_sourced()
    {
        var draft = Assert.Throws<SampleFormatException>(() => ComparisonSample.Read(Comparison(reviewed: false).ToJsonString())).Problems;
        var reviewed = ComparisonSample.Read(Comparison(reviewed: true).ToJsonString());

        Assert.Contains("a comparison that is approved for the public has the status reviewed.", draft);
        Assert.Contains(draft, problem => problem.EndsWith("a comparison that is approved for the public holds only reviewed statements; this one is proposed.", StringComparison.Ordinal));
        Assert.Contains(draft, problem => problem.EndsWith("a statement that is shown to the public cites a source.", StringComparison.Ordinal));
        Assert.True(reviewed.MayBeShownTo(Audience.Public));
    }

    [Fact]
    public void For_the_public_a_draft_translation_of_a_comparison_is_left_out()
    {
        var translation = File.ReadAllText(ComparisonFile("iran-china-rome.fa.json"));
        var sample = Comparison(reviewed: true).ToJsonString();

        var everybody = ComparisonSample.Read(sample, [translation]);
        var reviewedOnly = ComparisonSample.Read(sample, [translation], reviewedTextsOnly: true);

        Assert.Contains("fa", everybody.Title.Languages);
        Assert.Equal(["en"], reviewedOnly.Title.Languages);
        Assert.All(reviewedOnly.Lanes, lane => Assert.Equal(["en"], lane.Label.Languages));
    }
}
