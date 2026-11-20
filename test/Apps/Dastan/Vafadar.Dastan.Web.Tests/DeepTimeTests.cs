using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site of the dated records with records of deep time beside them: test data, said of the work of the
/// sample because it is there – a rock, tools, a fire, a sea and an age of ice.
/// </summary>
public sealed class DeepTimeSite : DatedSite
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var path = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var first = !File.Exists(path);
        base.ConfigureWebHost(builder);
        if (!first)
        {
            return;
        }

        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var dated = sample["entities"]!.AsArray().Single(entity => (string?)entity!["id"] == "work-shahnameh")!["dated"]!.AsArray();
        foreach (var record in JsonNode.Parse(
            """
            [
              {
                "key": "rock-formed", "label": "A rock is formed",
                "time": { "kind": "occurrence", "at": { "frame": "age", "value": 55, "unit": "millionYears", "reference": 1950 } }
              },
              {
                "key": "tools-made", "label": "Tools are made",
                "time": {
                  "kind": "occurrenceWindow",
                  "earliest": { "frame": "age", "value": 20, "unit": "thousandYears", "reference": 2000 },
                  "latest": { "frame": "age", "value": 10, "unit": "thousandYears", "reference": 2000 }
                }
              },
              {
                "key": "fire-used", "label": "A fire is used",
                "time": { "kind": "occurrence", "at": { "frame": "age", "value": 12, "unit": "thousandYears", "reference": 1950, "basis": "radiocarbonYears", "approximate": true } }
              },
              {
                "key": "sea-dried", "label": "A sea falls dry",
                "time": { "kind": "occurrence", "at": { "frame": "age", "value": 5.3, "unit": "millionYears" } }
              },
              {
                "key": "ice-lasted", "label": "An age of ice",
                "time": {
                  "kind": "period",
                  "start": { "state": "known", "at": { "frame": "age", "value": 2.6, "unit": "millionYears", "reference": 1950 } },
                  "end": { "state": "known", "at": { "frame": "age", "value": 11.7, "unit": "thousandYears", "reference": 1950 } },
                  "continuity": "notAsserted"
                }
              }
            ]
            """)!.AsArray())
        {
            var entry = record!.DeepClone().AsObject();
            entry["sourceIds"] = new JsonArray("src-ganjoor-index");
            entry["spoilerLevel"] = 0;
            entry["reviewStatus"] = "proposed";
            dated.Add(entry);
        }

        File.WriteAllText(path, sample.ToJsonString());
    }
}

/// <summary>
/// Very distant times keep their meaning (step 19.9, R-TIME-04, ADR 0021): a depth of time is written with its
/// number, its unit and what it is counted from, as the record says it; and it is asked for like a year – by
/// the search and by "what held at this time" – on the one axis of years it shares with the calendar.
/// </summary>
public sealed class DeepTimeTests(DeepTimeSite site) : IClassFixture<DeepTimeSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "55 million years before the present (counted from 1950)")]
    [InlineData("en", "between 20 thousand years before 2000 CE and 10 thousand years before 2000 CE")]
    [InlineData("en", "about 12 thousand years before the present (counted from 1950), in radiocarbon years")]
    [InlineData("en", "5.3 million years ago")]
    [InlineData("en", "2.6 million years before the present (counted from 1950) – 11.7 thousand years before the present (counted from 1950)")]
    [InlineData("fa", "۵۵ میلیون سال پیش از اکنون (شمرده از ۱۹۵۰)")]
    [InlineData("fa", "۵٫۳ میلیون سال پیش")]
    [InlineData("fa", "حدود ۱۲ هزار سال پیش از اکنون (شمرده از ۱۹۵۰)، به سال رادیوکربنی")]
    public async Task A_depth_of_time_is_written_as_the_record_says_it(string language, string written)
    {
        var page = await site.Visitor().Get($"/{language}/{Story}/entity/work-shahnameh");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(written, page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("50-60 Ma", "A rock is formed", false)]
    [InlineData("55 million years ago", "A rock is formed", false)]
    [InlineData("۵۰ تا ۶۰ میلیون سال پیش", "A rock is formed", false)]
    [InlineData("10 to 20.1 ka", "Tools are made", false)]
    [InlineData("15 to 30 ka", "Tools are made", true)]
    [InlineData("10 to 20 thousand years ago", "A fire is used", true)]
    [InlineData("5 to 6 Ma", "A sea falls dry", true)]
    [InlineData("1 Ma", "An age of ice", false)]
    [InlineData("100 to 200 ka", "An age of ice", false)]
    [InlineData("18001 BCE to 8000 BCE", "Tools are made", false)]
    [InlineData("18000 BCE to 8000 BCE", "Tools are made", true)]
    [InlineData("5.3 Ma", "A sea falls dry", true)]
    [InlineData("12 ka", "A fire is used", true)]
    public async Task What_held_at_a_depth_of_time_is_found_and_what_is_less_than_exact_only_may_have(string typed, string stated, bool possibly)
    {
        var page = await site.Visitor().Get($"/en/{Story}/at?time={Uri.EscapeDataString(typed)}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(stated, page.Main, StringComparison.Ordinal);
        var row = page.Main[page.Main.IndexOf(stated, StringComparison.Ordinal)..];
        row = row[..row.IndexOf("</dd>", StringComparison.Ordinal)];
        Assert.Equal(possibly, row.Contains("may have held", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_depth_of_time_reaches_nothing_that_lies_elsewhere()
    {
        var deep = await site.Visitor().Get($"/en/{Story}/at?time=50-60+Ma");
        foreach (var other in new[] { "Tools are made", "A fire is used", "A sea falls dry", "An age of ice", "The work is written" })
        {
            Assert.DoesNotContain(other, deep.Main, StringComparison.Ordinal);
        }

        // A year of the calendar asks the calendar: nothing of the deep time holds in the year 1000.
        var year = await site.Visitor().Get($"/en/{Story}/at?year=1000");
        Assert.Contains("The work is written", year.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("A rock is formed", year.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("An age of ice", year.Main, StringComparison.Ordinal);

        Assert.Contains("Nothing is stated for this time.", (await site.Visitor().Get($"/en/{Story}/at?time=3+Ga")).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("55 Ma", true)]
    [InlineData("50 to 60 million years ago", true)]
    [InlineData("1 Ma", true)]
    [InlineData("3 Ga", false)]
    [InlineData("600 to 500 BCE", false)]
    public async Task The_search_finds_by_a_depth_of_time(string typed, bool found)
    {
        var page = await site.Visitor().Get($"/en/{Story}/search?time={Uri.EscapeDataString(typed)}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal(found, page.Main.Contains("entity/work-shahnameh\"", StringComparison.Ordinal));
        Assert.Contains("name=\"time\"", page.Html, StringComparison.Ordinal);
    }
}
