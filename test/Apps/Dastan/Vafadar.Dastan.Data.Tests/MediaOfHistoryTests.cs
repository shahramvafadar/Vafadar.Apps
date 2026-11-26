using System.Text.Json.Nodes;
using System.Xml.Linq;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// Pictures of a history (backlog E3) and the times of the collection a picture has (E7, MED-006): the kinds of
/// what a picture is beyond an artwork of a story and a diagram, and the days an item was brought in and last
/// changed. The records are test data.
/// </summary>
public sealed class MediaOfHistoryTests
{
    private static JsonObject FirstMedia(JsonObject sample) => (JsonObject)sample["media"]!.AsArray()[0]!;

    [Theory]
    [InlineData("photograph_of_historical_object", MediaRepresentation.PhotographOfHistoricalObject)]
    [InlineData("work_of_its_time", MediaRepresentation.WorkOfItsTime)]
    [InlineData("later_depiction_of_history", MediaRepresentation.LaterDepictionOfHistory)]
    [InlineData("present_day_reconstruction", MediaRepresentation.PresentDayReconstruction)]
    [InlineData("present_day_map", MediaRepresentation.PresentDayMap)]
    public void A_picture_says_what_kind_of_picture_of_a_history_it_is(string kind, MediaRepresentation expected)
    {
        string? id = null;
        var catalogue = SampleFiles.Read(sample =>
        {
            var item = FirstMedia(sample);
            id = (string)item["id"]!;
            item["depictedContext"]!["kind"] = kind;
        });

        Assert.Equal(expected, catalogue.Media.Single(item => item.Id.Value == id).Representation);
    }

    [Fact]
    public void A_kind_nobody_knows_is_refused()
    {
        var problems = SampleFiles.Problems(sample => FirstMedia(sample)["depictedContext"]!["kind"] = "a_nice_picture");

        Assert.Contains("a_nice_picture", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Fact]
    public void An_item_says_when_it_was_brought_in_and_when_what_is_said_of_it_changed()
    {
        string? id = null;
        var catalogue = SampleFiles.Read(sample =>
        {
            var item = FirstMedia(sample);
            id = (string)item["id"]!;
            item["addedAt"] = "2026-11-20";
            item["changedAt"] = "2026-11-26";
        });
        var read = catalogue.Media.Single(item => item.Id.Value == id);

        Assert.Equal(new DateOnly(2026, 11, 20), read.BroughtIn);
        Assert.Equal(new DateOnly(2026, 11, 26), read.Changed);
    }

    [Fact]
    public void An_item_without_them_has_neither_and_a_change_before_the_item_is_refused()
    {
        var plain = SampleFiles.Read(_ => { }).Media[0];
        Assert.Null(plain.BroughtIn);
        Assert.Null(plain.Changed);

        var problems = SampleFiles.Problems(sample =>
        {
            var item = FirstMedia(sample);
            item["addedAt"] = "2026-11-26";
            item["changedAt"] = "2026-11-20";
        });
        Assert.EndsWith("changedAt is before addedAt: what is said of an item cannot change before the item is there.", Assert.Single(problems), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Ui.resx")]
    [InlineData("Ui.fa.resx")]
    public void Every_kind_of_picture_is_said_in_words_in_every_language_of_the_pages(string file)
    {
        var texts = XDocument.Load(Path.Combine(SampleFiles.App, "Vafadar.Dastan.Web", "Resources", file)).Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string)data.Element("value")!);

        foreach (var kind in Enum.GetNames<MediaRepresentation>())
        {
            Assert.False(string.IsNullOrWhiteSpace(texts.GetValueOrDefault($"Media_Representation_{kind}")), $"{file} does not name {kind}.");
            Assert.False(string.IsNullOrWhiteSpace(texts.GetValueOrDefault($"Media_RepresentationNote_{kind}")), $"{file} does not say what {kind} is.");
        }
    }
}
