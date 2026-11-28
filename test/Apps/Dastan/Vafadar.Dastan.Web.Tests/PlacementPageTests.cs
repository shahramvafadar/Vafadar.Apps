using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site of <see cref="DatedSite"/> in which Rostam has a hypothetical placement in time (ADR 0028): a window of
/// 1500–1100 BCE, an editorial hypothesis of very low confidence. The placement is test data.
/// </summary>
public sealed class PlacementSite : DatedSite
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        var path = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var rostam = sample["entities"]!.AsArray().Single(entity => (string?)entity!["id"] == "character-rostam")!;
        if (rostam["dated"] is not null)
        {
            return;
        }

        rostam["dated"] = JsonNode.Parse(
            """
            [
              {
                "key": "placement-rostam-birth",
                "label": "Hypothetical birth",
                "time": {
                  "kind": "occurrenceWindow",
                  "earliest": { "frame": "calendar", "era": "beforeCommonEra", "year": 1500 },
                  "latest": { "frame": "calendar", "era": "beforeCommonEra", "year": 1100 }
                },
                "placement": { "basis": "editorialHypothesis", "confidence": "veryLow", "explanation": "Test data: about 1300 BCE." },
                "sourceIds": ["src-ganjoor-index"],
                "spoilerLevel": 0,
                "reviewStatus": "proposed"
              }
            ]
            """);
        File.WriteAllText(path, sample.ToJsonString());

        var persianPath = Path.Combine(Content, "samples", "shahnameh-pilot.fa.json");
        var persian = JsonNode.Parse(File.ReadAllText(persianPath))!.AsObject();
        persian["dated"]!["placement-rostam-birth"] = JsonNode.Parse("""{ "label": "تولد فرضی", "explanation": "دادهٔ آزمایشی." }""");
        File.WriteAllText(persianPath, persian.ToJsonString());
    }
}

/// <summary>
/// A hypothetical placement in time (ADR 0028) on the page of its name – apart, under a heading that says it is a
/// hypothesis, with the sentence that no date of history exists – and in no chart of time.
/// </summary>
public sealed class PlacementPageTests(PlacementSite site) : IClassFixture<PlacementSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "Hypothetical placement in time", "Hypothetical birth", "an editorial working hypothesis of Dastan, not a scholarly estimate", "Confidence: very low", "Test data: about 1300 BCE.", "No historically verified date exists.")]
    [InlineData("fa", "جایگاه زمانی فرضی", "تولد فرضی", "فرضیهٔ کاری ویراستاران داستان، نه برآوردی پژوهشی", "اطمینان: بسیار کم", "دادهٔ آزمایشی.", "هیچ تاریخ مستند تاریخی وجود ندارد.")]
    public async Task The_page_of_a_name_shows_its_placement_apart_as_a_hypothesis(string language, string heading, string label, string basis, string confidence, string explanation, string noDate)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/character-rostam");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h2 id=\"placements\">", page.Html, StringComparison.Ordinal);
        Assert.Contains(heading, page.Main, StringComparison.Ordinal);
        foreach (var text in new[] { label, basis, confidence, explanation, noDate })
        {
            Assert.Contains(text, page.Main, StringComparison.Ordinal);
        }

        // A placement is not among what is stated of the name with a date.
        Assert.Contains("id=\"placement-placement-rostam-birth\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"stated\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"stated-placement-rostam-birth\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_chart_of_time_draws_a_placement()
    {
        var visitor = site.Visitor();
        var collection = await visitor.Get($"/en/{Story}/timeline");
        var name = await visitor.Get($"/en/{Story}/entity/character-rostam/timeline");

        // The chart of the collection still holds what is dated of the work, and nothing of the placement.
        Assert.Contains("id=\"item-work-written\"", collection.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("placement-rostam-birth", collection.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Hypothetical birth", collection.Html, StringComparison.Ordinal);

        // The name has nothing dated: its chart of time says so, and its page offers no comparison in time.
        Assert.Equal(HttpStatusCode.OK, name.Status);
        Assert.DoesNotContain("dastan-timeline", name.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Hypothetical birth", name.Html, StringComparison.Ordinal);
        var page = await visitor.Get($"/en/{Story}/entity/character-rostam");
        Assert.DoesNotContain("compare?pick=", page.Html, StringComparison.Ordinal);
    }
}
