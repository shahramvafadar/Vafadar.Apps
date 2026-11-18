using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// What the editors state about the stages of another telling of the same world (step 14.11): read from the
/// file, checked, and asked for a stage. Test data.
/// </summary>
public sealed class StageCorrespondenceTests
{
    private static Action<JsonObject> With(string correspondences) => sample => sample["spoilerPolicy"]!["correspondences"] = JsonNode.Parse(correspondences);

    [Fact]
    public void Nothing_is_stated_in_the_collection_of_the_repository()
    {
        Assert.Empty(PilotSample.Load(SampleFiles.PilotPath).Info.Correspondences);
    }

    [Fact]
    public void A_correspondence_answers_for_the_nearest_stage_at_or_before_the_one_that_was_read()
    {
        var info = SampleFiles.Read(With("""[ { "collection": "story-retold", "levels": [ { "here": 1, "there": 0 }, { "here": 3, "there": 2 } ] } ]""")).Info;
        var stated = Assert.Single(info.Correspondences);

        Assert.Equal("story-retold", stated.Collection);
        Assert.Null(stated.For(new ReadingLevel(0)));
        Assert.Equal(new ReadingLevel(0), stated.For(new ReadingLevel(1)));
        Assert.Equal(new ReadingLevel(0), stated.For(new ReadingLevel(2)));
        Assert.Equal(new ReadingLevel(2), stated.For(new ReadingLevel(3)));
        Assert.Equal(new ReadingLevel(2), stated.For(new ReadingLevel(4)));
    }

    [Fact]
    public void What_is_no_correspondence_is_refused_with_what_is_wrong()
    {
        IReadOnlyList<string> Of(string stated) => SampleFiles.Problems(With(stated));

        Assert.Contains("collection, correspondence with story-retold: a later stage here does not correspond to an earlier stage there.",
            Of("""[ { "collection": "story-retold", "levels": [ { "here": 1, "there": 3 }, { "here": 2, "there": 1 } ] } ]"""));
        Assert.Contains("collection, correspondence with story-retold: it names stages of this collection and, for each, a stage of the other.",
            Of("""[ { "collection": "story-retold", "levels": [ { "here": 9, "there": 0 } ] } ]"""));
        Assert.Contains("collection, correspondence with story-retold: it names stages of this collection and, for each, a stage of the other.",
            Of("""[ { "collection": "story-retold", "levels": [] } ]"""));
        Assert.Contains("collection, correspondence with story-rostam-sohrab: a collection corresponds to another one, not to itself.",
            Of("""[ { "collection": "story-rostam-sohrab", "levels": [ { "here": 0, "there": 0 } ] } ]"""));
        Assert.Contains("collection: a correspondence with another collection is stated once.",
            Of("""[ { "collection": "story-retold", "levels": [ { "here": 0, "there": 0 } ] }, { "collection": "story-retold", "levels": [ { "here": 1, "there": 1 } ] } ]"""));
        Assert.Contains(Of("""[ { "collection": "story-retold", "levels": [ { "here": 0, "there": 0 }, { "here": 0, "there": 1 } ] } ]"""), problem => problem.Contains("the stage 0 is named twice.", StringComparison.Ordinal));
        Assert.Contains(Of("""[ { "collection": "story-retold", "levels": [ { "here": 0 } ] } ]"""), problem => problem.Contains("there", StringComparison.Ordinal));
    }
}
