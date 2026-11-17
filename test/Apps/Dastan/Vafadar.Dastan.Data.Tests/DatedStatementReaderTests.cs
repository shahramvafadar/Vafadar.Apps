using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// What a collection states about an entity with a time of the calendar (step 9.4), as its file writes it: the
/// optional list <c>dated</c> of an entity, with the time in the form a comparison uses. The statements here are
/// test data put into a copy of the sample in memory; the sample itself has none.
/// </summary>
public sealed class DatedStatementReaderTests
{
    private const string Work = "work-shahnameh";

    private static JsonObject Period(string key = "work-written") => JsonNode.Parse(
        $$"""
        {
          "key": "{{key}}",
          "label": "The work is written",
          "summary": "Test data: a period with an approximate beginning.",
          "time": {
            "kind": "period",
            "start": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 977, "approximate": true } },
            "end": { "state": "known", "at": { "frame": "calendar", "era": "commonEra", "year": 1010 } },
            "continuity": "notAsserted"
          },
          "sourceIds": ["src-ganjoor-index"],
          "spoilerLevel": 0,
          "reviewStatus": "proposed"
        }
        """)!.AsObject();

    private static JsonObject Event() => JsonNode.Parse(
        """
        {
          "key": "work-copied",
          "label": "A copy is made",
          "time": { "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 1330 } },
          "sourceIds": ["src-ganjoor-index"],
          "spoilerLevel": 0,
          "reviewStatus": "sourcedReviewPending"
        }
        """)!.AsObject();

    private static Action<JsonObject> With(params JsonObject[] dated) =>
        sample => sample.Record("entities", Work)["dated"] = new JsonArray([.. dated]);

    [Fact]
    public void The_sample_of_the_repository_states_nothing_with_a_date_about_its_entities()
    {
        Assert.All(PilotSample.Load(SampleFiles.PilotPath).Entities, entity => Assert.Empty(entity.Dated));
    }

    [Fact]
    public void A_period_and_an_event_are_read_with_their_time_as_it_is_stated()
    {
        var work = SampleFiles.Read(With(Period(), Event())).Find(new EntityId(Work))!;

        var (written, copied) = (work.Dated[0], work.Dated[1]);
        Assert.Equal(["work-written", "work-copied"], work.Dated.Select(dated => dated.Key));
        Assert.Equal("The work is written", written.Label.Resolve("en").Value);
        Assert.Equal("Test data: a period with an approximate beginning.", written.Summary!.Resolve("en").Value);
        Assert.Equal(ReviewStatus.Proposed, written.Review);
        Assert.Equal(ReviewStatus.SourcedReviewPending, copied.Review);
        Assert.Equal(new ReadingLevel(0), written.Level);

        var period = Assert.IsType<Period>(written.Time);
        var start = Assert.IsType<CalendarPosition>(period.Start.Earliest);
        Assert.Equal((Era.CommonEra, 977, true), (start.Era, start.Year, start.IsApproximate));
        Assert.Null(copied.Summary);
        Assert.IsType<CalendarPosition>(Assert.IsType<Occurrence>(copied.Time).At);
    }

    [Fact]
    public void A_translation_gives_a_dated_statement_its_texts_by_its_key()
    {
        const string Persian = """
            {
              "formatVersion": "dastan-translation-0.1",
              "datasetId": "dastan-shahnameh-rostam-sohrab-pilot-v0.5",
              "language": "fa",
              "review": "draft",
              "dated": { "work-written": { "label": "سروده شدنِ اثر", "summary": "دادهٔ آزمایشی." } }
            }
            """;

        var written = SampleFiles.Read(With(Period()), Persian).Find(new EntityId(Work))!.Dated.Single();

        Assert.Equal("سروده شدنِ اثر", written.Label.Resolve("fa").Value);
        Assert.Equal(TextReview.Draft, written.Label.Resolve("fa").Review);
        Assert.Equal("دادهٔ آزمایشی.", written.Summary!.Resolve("fa").Value);
    }

    [Fact]
    public void A_translation_that_names_a_dated_statement_the_sample_does_not_have_is_refused()
    {
        const string Persian = """
            {
              "formatVersion": "dastan-translation-0.1",
              "datasetId": "dastan-shahnameh-rostam-sohrab-pilot-v0.5",
              "language": "fa",
              "review": "draft",
              "dated": { "work-forgotten": { "label": "چیزی" } }
            }
            """;

        Assert.Contains("translation fa: dated/work-forgotten/label translates nothing the sample has.", SampleFiles.Problems(With(Period()), Persian));
    }

    [Fact]
    public void The_time_of_a_dated_statement_is_a_time_of_the_calendar_and_never_a_place_in_the_story()
    {
        var inStory = Event();
        inStory["time"] = JsonNode.Parse("""{ "kind": "occurrence", "at": { "frame": "narrative", "sequence": "story-rostam-sohrab", "order": "presentation", "ordinal": 3 } }""");
        var unreadable = Event();
        unreadable["time"] = JsonNode.Parse("""{ "kind": "occurrence", "at": { "frame": "calendar", "era": "commonEra", "year": 0 } }""");
        var missing = Event();
        missing.Remove("time");

        Assert.Contains(SampleFiles.Problems(With(inStory)), problem => problem.StartsWith("work-copied: time", StringComparison.Ordinal));
        Assert.Contains(SampleFiles.Problems(With(unreadable)), problem => problem.StartsWith("work-copied: time cannot be read", StringComparison.Ordinal));
        Assert.Contains("work-copied: time is missing.", SampleFiles.Problems(With(missing)));
    }

    [Theory]
    [InlineData("reviewStatus", "\"source_backed_editorial_review_pending\"", "work-copied: reviewStatus has the unknown value")]
    [InlineData("spoilerLevel", "-1", "work-copied: spoilerLevel is negative.")]
    [InlineData("sourceIds", "[\"src-nowhere\"]", "work-copied: the source src-nowhere does not exist.")]
    [InlineData("spoilerLevel", "9", "work-copied: the reading level 9 is not a stage of the collection.")]
    [InlineData("key", "\"character-rostam\"", "character-rostam: the identifier names records of two kinds.")]
    [InlineData("year", "1330", "work-copied: the field year is not known to this reader.")]
    public void What_is_wrong_with_a_dated_statement_is_named_with_its_key(string field, string value, string problem)
    {
        var statement = Event();
        statement[field] = JsonNode.Parse(value);

        Assert.Contains(SampleFiles.Problems(With(statement)), found => found.StartsWith(problem, StringComparison.Ordinal));
    }

    [Fact]
    public void A_statement_of_a_later_stage_reaches_no_visitor_before_it()
    {
        var late = Event();
        late["spoilerLevel"] = 3;
        late["sourceIds"] = new JsonArray("src-ganjoor-index");
        var collection = SampleFiles.Read(With(Period(), late));

        var early = Projection.Of(collection, new ReadingLevel(2), Audience.LocalDemonstration)!;
        var reader = Projection.Of(collection, new ReadingLevel(3), Audience.LocalDemonstration)!;

        Assert.Equal(["work-written"], early.Timeline().Subjects.Single().Dated.Select(dated => dated.Key));
        Assert.Equal(["work-written", "work-copied"], reader.Timeline().Subjects.Single().Dated.Select(dated => dated.Key));
        Assert.DoesNotContain("A copy is made", Vafadar.Dastan.Testing.Disclosure.Of(early, collection));
        Assert.Contains("A copy is made", Vafadar.Dastan.Testing.Disclosure.Of(reader, collection));
    }
}
