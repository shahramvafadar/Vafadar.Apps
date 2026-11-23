using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The shapes the work order for content prescribes (docs/content-work-order.md), as made-up files: a history
/// – a region, a dynasty, its office, rulers with their reigns, a capital, a period, an event – and a work that
/// tells no story but names its stories. The site must be able to read what the work order tells a writer to
/// write; a change of the reader that refuses one of them fails here and not in the line of work of a writer.
/// </summary>
public sealed class WorkOrderShapesTests
{
    private static string Shapes => Path.Combine(Path.GetDirectoryName(SampleFiles.FixturesPath)!, "work-order-shapes");

    [Theory]
    [InlineData("a-history.json", "Region 1, Dynasty 1, Office 1, Person 2, Place 1, Period 1, Event 1")]
    [InlineData("a-work-without-a-story.json", "Work 2, Person 2, Period 1, NarrativeArc 2, Character 3, Object 1, Place 1, Organisation 1, Dynasty 1, Family 1, Office 1")]
    [InlineData("places-people-were-at.json", "Concept 1, University 1, Company 1, Organisation 1, Place 1")]
    public void A_shape_of_the_work_order_is_one_the_site_can_use(string file, string kinds)
    {
        var report = ContentCheck.File(Path.Combine(Shapes, file));

        Assert.True(report.IsUsable, string.Join('\n', report.Problems));
        Assert.Empty(report.Notices);
        Assert.Contains(report.Summary, line => line.Name == "entities" && line.Value.Contains(kinds, StringComparison.Ordinal));
    }

    [Fact]
    public void A_translation_beside_a_collection_is_read_with_it()
    {
        var report = ContentCheck.File(Path.Combine(Shapes, "a-history.json"));

        Assert.Contains(report.Summary, line => line.Name == "translations" && line.Value == "fa");

        // And by itself: a translation is checked with the file it translates.
        Assert.True(ContentCheck.File(Path.Combine(Shapes, "a-history.fa.json")).IsUsable);
    }

    [Fact]
    public void A_work_that_names_stories_and_has_no_edition_is_refused_with_the_reason_the_work_order_gives()
    {
        var folder = Path.Combine(Path.GetTempPath(), "dastan-shape-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var file = Path.Combine(folder, "a-work.json");
            var written = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(Shapes, "a-work-without-a-story.json")))!.AsObject();
            written.Remove("editionContext");
            File.WriteAllText(file, written.ToJsonString());

            var report = ContentCheck.File(file);

            Assert.Contains(report.Problems, problem => problem.Contains("editionContext is missing", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
