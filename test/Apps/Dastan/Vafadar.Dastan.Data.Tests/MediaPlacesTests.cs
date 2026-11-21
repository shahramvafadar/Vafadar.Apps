using System.Text.Json.Nodes;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// One picture in several places, with words for each (step 20.10, R-MED-07): a place refers to the item and
/// names somebody or a step the item says it shows; the item is not copied.
/// </summary>
public sealed class MediaPlacesTests
{
    private const string Item = "media-met-453265";

    private static JsonNode Place(string field, string id, string caption) => new JsonObject { [field] = id, ["caption"] = caption };

    [Fact]
    public void An_item_without_places_has_its_own_caption_everywhere()
    {
        var item = SampleFiles.Read().Media.Single(item => item.Id.Value == Item);

        Assert.Empty(item.Places);
        Assert.Same(item.Caption, item.CaptionAt(new EntityId("character-rostam")));
        Assert.Same(item.Caption, item.CaptionAt(new EventId("event-06")));
    }

    [Fact]
    public void A_place_has_words_and_sources_of_its_own_and_leaves_the_item_one()
    {
        var before = SampleFiles.Read().Media.Single(item => item.Id.Value == Item);
        var catalogue = SampleFiles.Read(sample =>
        {
            var beside = Place("entityId", "character-sohrab", "Sohrab, at the left.");
            beside["sourceIds"] = new JsonArray("src-ganjoor-s19");
            sample.Record("media", Item)["placements"] = new JsonArray(beside, Place("eventId", "event-06", "The moment of this step."));
        });

        var item = catalogue.Media.Single(item => item.Id.Value == Item);
        Assert.Equal(2, item.Places.Length);
        Assert.Equal("Sohrab, at the left.", item.CaptionAt(new EntityId("character-sohrab")).In("en")!.Value.Value);
        Assert.Equal([new SourceId("src-ganjoor-s19")], item.PlaceAt(new EntityId("character-sohrab"))!.Sources);
        Assert.Equal("The moment of this step.", item.CaptionAt(new EventId("event-06")).In("en")!.Value.Value);

        // Somebody it shows without a place has the caption of the item; the item itself is what it was.
        Assert.Equal(before.Caption.In("en")!.Value.Value, item.CaptionAt(new EntityId("character-rostam")).In("en")!.Value.Value);
        Assert.Equal((before.Caption.In("en")!.Value.Value, before.Level, before.Rights.Status), (item.Caption.In("en")!.Value.Value, item.Level, item.Rights.Status));
        Assert.Equal(before.Sources, item.Sources);
    }

    [Fact]
    public void A_place_names_one_thing_the_item_shows_and_each_thing_once()
    {
        Assert.Equal(
            [$"{Item}: a placement names character-zal, which the item does not say it shows."],
            SampleFiles.Problems(sample => sample.Record("media", Item)["placements"] = new JsonArray(Place("entityId", "character-zal", "Zal."))));

        Assert.Equal(
            [$"{Item}: two placements name character-rostam."],
            SampleFiles.Problems(sample => sample.Record("media", Item)["placements"] = new JsonArray(Place("entityId", "character-rostam", "One."), Place("entityId", "character-rostam", "Two."))));

        var both = Place("entityId", "character-rostam", "Both.");
        both["eventId"] = "event-06";
        Assert.Equal(
            [$"{Item}: a placement names one entity or one event."],
            SampleFiles.Problems(sample => sample.Record("media", Item)["placements"] = new JsonArray(both)));

        var neither = new JsonObject { ["caption"] = "Nowhere." };
        Assert.Equal(
            [$"{Item}: a placement names one entity or one event."],
            SampleFiles.Problems(sample => sample.Record("media", Item)["placements"] = new JsonArray(neither)));
    }

    [Fact]
    public void A_source_of_a_place_that_does_not_exist_is_a_problem()
    {
        var problems = SampleFiles.Problems(sample =>
        {
            var beside = Place("entityId", "character-rostam", "Rostam.");
            beside["sourceIds"] = new JsonArray("src-nowhere");
            sample.Record("media", Item)["placements"] = new JsonArray(beside);
        });

        Assert.Contains("src-nowhere", Assert.Single(problems), StringComparison.Ordinal);
    }
}
