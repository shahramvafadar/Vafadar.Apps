using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The way into everything seen side by side (D-092): a main way of the head of every page, the comparisons the
/// editors made, and names of one collection that a visitor lays side by side in the chart of time.
/// </summary>
public sealed class CompareHubPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    [Theory]
    [InlineData("en", "Side by side")]
    [InlineData("fa", "کنار هم")]
    public async Task Side_by_side_is_a_main_way_of_every_page(string language, string name)
    {
        foreach (var address in new[] { "", "search", "compare/iran-china-rome" })
        {
            var page = await site.Visitor().Get($"/{language}/{address}");
            var nav = System.Text.RegularExpressions.Regex.Match(page.Html, "<nav class=\"site-nav\"[^>]*>(.*?)</nav>", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
            Assert.Contains("href=\"compare\"", nav, StringComparison.Ordinal);
            Assert.Contains(name, WebUtility.HtmlDecode(nav), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task The_page_lists_the_comparisons_with_their_lanes()
    {
        var page = await site.Visitor().Get("/en/compare");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<title>Side by side – Dastan</title>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains("compare/iran-china-rome", page.Links);
        Assert.Contains("class=\"compare-lanes\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"page\"", System.Text.RegularExpressions.Regex.Match(page.Html, "<a href=\"compare\"[^>]*>").Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_start_shows_the_comparisons_before_what_there_is_to_read()
    {
        var page = await site.Visitor().Get("/en/");

        var comparisons = page.Html.IndexOf("id=\"side-by-side\"", StringComparison.Ordinal);
        var collections = page.Html.IndexOf("id=\"first-story\"", StringComparison.Ordinal);
        Assert.True(comparisons > 0 && collections > comparisons);
        Assert.Contains("compare", page.Links);
    }
}

/// <summary>
/// What a visitor compares (D-093): names of any collections of the library, chosen on the page and drawn on the
/// same page as lanes over one axis of years.
/// </summary>
public sealed class CompareNamesTests(LibrarySite site) : IClassFixture<LibrarySite>
{
    private const string Iran = "subject-ancient-iran";
    private const string China = "subject-ancient-china";
    private const string Rome = "subject-ancient-rome";

    private static string Pick(string collection, string name) => "pick=" + Uri.EscapeDataString($"{collection}/{name}");

    [Fact]
    public async Task Names_of_several_collections_are_drawn_side_by_side_on_the_same_page()
    {
        Assert.SkipWhen(LibrarySite.Collections.Count == 0, "The library holds no collection.");
        var page = await site.Visitor().Get($"/en/compare?{Pick(Iran, "dynasty-achaemenid")}&{Pick(China, "dynasty-western-han")}&{Pick(Rome, "government-roman-republic")}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("id=\"yours\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<svg class=\"tl-plot\"", page.Html, StringComparison.Ordinal);
        // Each name is a lane, with the collection it is of, and can be taken out again.
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(page.Html, "<ul class=\"chips compare-chosen\"[\\s\\S]*?</ul>").Single().Value.Split("<li ").Length - 1);
        Assert.Contains($"compare?{Pick(China, "dynasty-western-han")}&{Pick(Rome, "government-roman-republic")}#yours", page.Links);
        // The choice is ticked in the list, and the tools of the chart keep it.
        Assert.Matches($"value=\"{Iran}/dynasty-achaemenid\" checked", page.Html);
        Assert.Contains(page.Links, link => link.StartsWith("compare?view=", StringComparison.Ordinal) && link.Contains(Pick(Rome, "government-roman-republic"), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Events_and_periods_are_laid_beside_each_other_by_when_they_happened_began_and_ended()
    {
        Assert.SkipWhen(LibrarySite.Collections.Count == 0, "The library holds no collection.");
        var page = await site.Visitor().Get($"/en/compare?{Pick(Iran, "event-fall-of-nineveh")}&{Pick(China, "period-western-zhou")}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("Some of what was asked for cannot be compared", page.Main, StringComparison.Ordinal);
        Assert.Contains("When it happened", page.Main, StringComparison.Ordinal);
        Assert.Contains("From beginning to end", page.Main, StringComparison.Ordinal);
        Assert.Matches($"value=\"{China}/period-western-zhou\" checked", page.Html);
    }

    [Fact]
    public async Task A_name_that_is_not_here_is_left_out_and_said_so()
    {
        Assert.SkipWhen(LibrarySite.Collections.Count == 0, "The library holds no collection.");
        var page = await site.Visitor().Get($"/en/compare?{Pick(Iran, "person-cyrus-ii")}&{Pick(Iran, "no-such-name")}&pick=nonsense");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Some of what was asked for cannot be compared", page.Main, StringComparison.Ordinal);
        Assert.Contains("id=\"yours\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_name_with_a_time_begins_a_comparison_and_a_collection_leads_to_its_names()
    {
        Assert.SkipWhen(LibrarySite.Collections.Count == 0, "The library holds no collection.");
        var entity = await site.Visitor().Get($"/en/{Iran}/entity/person-cyrus-ii");
        var collection = await site.Visitor().Get($"/en/{Iran}");

        Assert.Contains($"compare?{Pick(Iran, "person-cyrus-ii")}#yours", entity.Links);
        Assert.Contains($"compare?collection={Iran}#choose", collection.Links);
        Assert.Contains("compare/iran-china-rome", collection.Links);
    }
}
