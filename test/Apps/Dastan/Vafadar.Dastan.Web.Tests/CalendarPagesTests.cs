using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site of the dated records with dates of other calendars beside them: test data – a day of the Solar
/// Hijri calendar, a day of the Lunar Hijri calendar, a day of the Gregorian calendar, a year of the Solar Hijri
/// calendar, and a day whose calendar the record does not say.
/// </summary>
public sealed class CalendarsSite : DatedSite
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
        foreach (var (key, label, at) in new[]
        {
            ("solar-day", "A day of the sun", """{ "frame": "calendar", "calendar": "solarHijri", "era": "solarHijri", "year": 1357, "month": 11, "day": 22 }"""),
            ("lunar-day", "A day of the moon", """{ "frame": "calendar", "calendar": "lunarHijri", "era": "lunarHijri", "year": 1447, "month": 9, "day": 1 }"""),
            ("gregorian-day", "A day of the west", """{ "frame": "calendar", "calendar": "gregorian", "era": "commonEra", "year": 2026, "month": 3, "day": 21 }"""),
            ("solar-year", "A year of the sun", """{ "frame": "calendar", "calendar": "solarHijri", "era": "solarHijri", "year": 389 }"""),
            ("unsaid-day", "A day of no said calendar", """{ "frame": "calendar", "era": "commonEra", "year": 1010, "month": 3, "day": 8 }"""),
        })
        {
            dated.Add(new JsonObject
            {
                ["key"] = key,
                ["label"] = label,
                ["time"] = new JsonObject { ["kind"] = "occurrence", ["at"] = JsonNode.Parse(at) },
                ["sourceIds"] = new JsonArray("src-ganjoor-index"),
                ["spoilerLevel"] = 0,
                ["reviewStatus"] = "proposed",
            });
        }

        File.WriteAllText(path, sample.ToJsonString());
    }
}

/// <summary>
/// A date is shown as it was written and, beside it, in the calendar the reader of the page reads in (step 19.9,
/// ADR 0021, R-TIME-05): Solar Hijri on a Persian page, Gregorian on another – said to be converted, and only
/// where the date is exact enough to convert.
/// </summary>
public sealed class CalendarPagesTests(CalendarsSite site) : IClassFixture<CalendarsSite>
{
    private const string Page = "story-rostam-sohrab/entity/work-shahnameh";

    [Theory]
    [InlineData("en", "22 Bahman 1357 SH (converted: 11 February 1979 CE)")]
    [InlineData("en", "1 Ramadan 1447 AH (converted: 18 February 2026 CE)")]
    [InlineData("fa", "۲۱ مارس ۲۰۲۶ میلادی (تبدیل‌شده: ۱ فروردین ۱۴۰۵ هجری خورشیدی)")]
    [InlineData("fa", "۱ رمضان ۱۴۴۷ هجری قمری (تبدیل‌شده: ۲۹ بهمن ۱۴۰۴ هجری خورشیدی)")]
    public async Task A_day_of_another_calendar_is_shown_as_written_and_converted_beside_it(string language, string written)
    {
        var page = await site.Visitor().Get($"/{language}/{Page}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(written, page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "21 March 2026 CE")]
    [InlineData("fa", "۲۲ بهمن ۱۳۵۷ هجری خورشیدی")]
    public async Task A_day_of_the_calendar_of_the_reader_is_shown_as_it_is(string language, string written)
    {
        var page = await site.Visitor().Get($"/{language}/{Page}");

        Assert.Contains(written, page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(written + " (", page.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "389 SH")]
    [InlineData("fa", "۳۸۹ هجری خورشیدی")]
    [InlineData("en", "8 March 1010 CE")]
    [InlineData("fa", "۸ مارس ۱۰۱۰ میلادی")]
    public async Task What_is_not_exact_enough_or_names_no_calendar_is_not_converted(string language, string written)
    {
        var page = await site.Visitor().Get($"/{language}/{Page}");

        // A year of one calendar is no year of another; and a calendar nobody said is not given to a date here.
        Assert.Contains(written, page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain(written + " (", page.Main, StringComparison.Ordinal);
    }
}
