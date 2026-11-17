using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// What a collection is about (step 12.4): the one story it tells, one of several stories, or something that is
/// no story at all – a company, a club, a person – and the language its texts are written in. Test data.
/// </summary>
public sealed class CollectionSubjectTests
{
    private static readonly DateOnly Today = new(2026, 11, 17);

    private static JsonObject Subject() => JsonNode.Parse(ContentTemplates.Subject("a-company", Today))!.AsObject();

    private static Catalogue Read(JsonObject sample, params string[] translations) =>
        PilotSample.Read(sample.ToJsonString(), SampleFiles.Options(checkFiles: false, translations));

    private static IReadOnlyList<string> Problems(JsonObject sample, params string[] translations) =>
        Assert.Throws<SampleFormatException>(() => Read(sample, translations)).Problems;

    private static Projection At(Catalogue catalogue, int level = 0) => Projection.Of(catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    // The sample of the repository with a second story of two steps beside the first.
    private static void TwoStories(JsonObject sample)
    {
        sample["subjectId"] = "story-rostam-sohrab";
        sample["entities"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "story-second", ["type"] = "NarrativeArc", ["label"] = "A second story", ["summary"] = "Test data.",
            ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
        });
        foreach (var storyEvent in sample["events"]!.AsArray())
        {
            storyEvent!["storyId"] = "story-rostam-sohrab";
        }

        for (var order = 1; order <= 2; order++)
        {
            sample["events"]!.AsArray().Add(new JsonObject
            {
                ["id"] = $"second-0{order}", ["storyId"] = "story-second", ["label"] = $"Second story, step {order}", ["summary"] = "Test data.",
                ["order"] = order, ["orderBasis"] = "narrative", ["sourceIds"] = new JsonArray("src-ganjoor-index"), ["spoilerLevel"] = 0,
                ["temporalExpression"] = new JsonObject { ["kind"] = "narrative_position" },
                ["reviewStatus"] = "source_backed_editorial_review_pending",
                ["entityAssociations"] = new JsonArray(new JsonObject { ["entityId"] = "character-zal", ["role"] = "Test data", ["presence"] = "acts_in_selected_scenes" }),
            });
        }
    }

    [Fact]
    public void A_collection_can_be_about_something_that_is_no_story()
    {
        var catalogue = Read(Subject());
        var projection = At(catalogue);
        var overview = projection.Overview();

        Assert.Equal("subject-a-company", catalogue.Info.Subject.Value);
        Assert.Empty(catalogue.Info.Stories);
        Assert.Null(catalogue.Info.Edition);
        Assert.Equal(EntityTypes.Organisation, overview.Subject.Type);
        Assert.Null(overview.Work);
        Assert.Null(overview.FirstStep);
        Assert.Empty(overview.Stories);

        // No story: no steps, no lanes – and still everything else: what is said, with its time.
        Assert.Null(projection.Story());
        Assert.Null(projection.Lanes());
        Assert.Empty(projection.AllSteps);
        Assert.Single(projection.Timeline().Held);
        Assert.Null(projection.Evidence(new EntityId("person-first"))!.Edition);
        Assert.Null(catalogue.Find(new RelationId("relation-01"))!.Scope.Story);
    }

    [Fact]
    public void A_collection_without_a_story_has_no_steps_and_says_what_it_is_about()
    {
        var withStep = Subject();
        withStep["events"]!.AsArray().Add(JsonNode.Parse(ContentTemplates.Collection("x", Today))!["events"]![0]!.DeepClone());
        var nameless = Subject();
        nameless.Remove("subjectId");
        var unknown = Subject();
        unknown["subjectId"] = "subject-nobody";
        var ofStory = Subject();
        ofStory["relations"]![0]!["scope"]!["kind"] = "observed_narrative_context";
        ofStory["relations"]![0]!.AsObject().Remove("time");

        Assert.Contains("event-01: the collection tells no story; an event is a step of the telling of one.", Problems(withStep));
        Assert.Contains("sample: a collection that tells no story says in subjectId what it is about.", Problems(nameless));
        Assert.Contains(Problems(unknown), problem => problem.StartsWith("collection: the subject subject-nobody does not exist", StringComparison.Ordinal));
        Assert.Contains(Problems(ofStory), problem => problem.Contains("storyId is missing", StringComparison.Ordinal));
    }

    [Fact]
    public void A_collection_that_tells_a_story_says_which_text_it_follows()
    {
        var sample = SampleFiles.Pilot();
        sample.Remove("editionContext");

        Assert.Contains("sample: editionContext is missing: a collection that tells a story says which text it follows.", Problems(sample));
    }

    [Fact]
    public void A_collection_tells_several_stories_each_with_its_own_steps()
    {
        var catalogue = SampleFiles.Read(TwoStories);
        var projection = At(catalogue, catalogue.Info.ReadingStages[^1].Level.Value);
        var first = projection.Story()!;
        var second = projection.Story(new EntityId("story-second"))!;

        Assert.Equal(["story-rostam-sohrab", "story-second"], catalogue.Info.Stories.Select(story => story.Value));
        Assert.Equal("story-rostam-sohrab", first.Story.Id.Value);
        Assert.Equal(PilotSample.Load(SampleFiles.PilotPath).Events.Length, first.Steps.Length);
        Assert.Equal([1, 2], second.Steps.Select(step => step.Number));
        Assert.All(second.Steps, step => Assert.Equal("story-second", step.Story.Value));
        Assert.Equal(first.Steps.Length + 2, projection.AllSteps.Length);

        // A step is found whichever story it belongs to, and an entity appears in the steps of both.
        Assert.Equal(2, projection.Find(new EventId("second-02"))!.Number);
        Assert.Contains(projection.Entity(new EntityId("character-zal"))!.Appearances, appearance => appearance.Step.Story.Value == "story-second");
        Assert.Equal(2, projection.Lanes(new EntityId("story-second"))!.Steps.Length);
        Assert.Equal(["story-rostam-sohrab", "story-second"], projection.Overview().Stories.Select(story => story.Id.Value));
        Assert.Null(projection.Story(new EntityId("character-zal")));
    }

    [Fact]
    public void The_story_a_visitor_enters_first_is_the_one_the_collection_is_about()
    {
        var catalogue = SampleFiles.Read(sample =>
        {
            TwoStories(sample);
            sample["subjectId"] = "story-second";
        });

        Assert.Equal("story-second", catalogue.Info.Subject.Value);
        Assert.Equal("story-second", At(catalogue).Story()!.Story.Id.Value);
    }

    [Fact]
    public void A_step_names_a_story_the_collection_tells()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            TwoStories(sample);
            sample.Record("events", "second-01")["storyId"] = "character-zal";
        });

        Assert.Contains("second-01: storyId character-zal is no story of the collection.", problems);
    }

    [Theory]
    [InlineData("fa", "fa")]
    [InlineData("Persian", "fa")]
    [InlineData("de", "de")]
    [InlineData("en", "en")]
    public void A_file_is_written_in_any_language_and_says_which(string stated, string tag)
    {
        var sample = Subject();
        sample["language"] = stated;
        var catalogue = Read(sample);

        Assert.Equal(tag, catalogue.Info.Language);
        Assert.Equal([tag], catalogue.Find(catalogue.Info.Subject)!.Label.Languages);
    }

    [Fact]
    public void The_language_of_a_file_is_a_language_tag_said_once_and_no_translation_repeats_it()
    {
        var unreadable = Subject();
        unreadable["language"] = "the language of poets";
        var twice = SampleFiles.Pilot();
        twice["language"] = "en";
        var persian = Subject();
        persian["language"] = "fa";
        persian["entities"]![1]!["labelFa"] = "نامی";
        const string Translation = """{ "formatVersion": "dastan-translation-0.1", "datasetId": "a-company", "language": "fa", "review": "draft" }""";

        Assert.Contains("sample: language the language of poets is no language tag; write en, fa, ar or the like.", Problems(unreadable));
        Assert.Contains("sample: language is said by editionContext.summaryLanguage; it is said once.", Problems(twice));
        Assert.Contains("person-first: labelFa repeats the language the file is written in; label is the Persian name.", Problems(persian));
        persian["entities"]![1]!.AsObject().Remove("labelFa");
        Assert.Contains("translation fa: it translates into the language the sample is written in.", Problems(persian, Translation));
    }

    [Fact]
    public void A_story_can_be_written_in_Persian_and_translated_into_English()
    {
        var sample = SampleFiles.Pilot();
        sample["editionContext"]!["summaryLanguage"] = "Persian";
        foreach (var entity in sample["entities"]!.AsArray())
        {
            entity!.AsObject().Remove("labelFa");
        }
        const string English = """
            {
              "formatVersion": "dastan-translation-0.1",
              "datasetId": "dastan-shahnameh-rostam-sohrab-pilot-v0.5",
              "language": "en",
              "review": "reviewed",
              "entities": { "character-rostam": { "label": "Rostam, in English" } }
            }
            """;
        var catalogue = PilotSample.Read(sample.ToJsonString(), SampleFiles.Options(translations: English));
        var hero = catalogue.Find(new EntityId("character-rostam"))!;

        Assert.Equal("fa", catalogue.Info.Language);
        Assert.Equal("Rostam, in English", hero.Label.Resolve("en", "fa").Value);
        Assert.Equal(hero.Label.In("fa")!.Value.Value, hero.Label.Resolve("de", "fa").Value);
    }
}
