using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The story as a chart on the page of the story: names in rows, the steps of the telling in columns, in a stage
/// that is closed and opened at its sides (ADR 0008, D-040).
/// </summary>
public sealed class StoryChartPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Address = $"{Story}/story";

    private Projection At(int level) => Projection.Of(site.Catalogue, new ReadingLevel(level), Audience.LocalDemonstration)!;

    private async Task<Page> Open(int level, string query = "", string language = "en")
    {
        var visitor = site.Visitor();
        if (level > 0)
        {
            await visitor.ReadTo(level, language);
        }

        return await visitor.Get($"/{language}/{Address}{query}");
    }

    private static string Chart(Page page) => Regex.Match(page.Html, "<div class=\"sc\".*?</div>", RegexOptions.Singleline).Value;

    // The numbers of the steps in the heads of the columns, in the order they are written.
    private static List<int> Columns(Page page) =>
        [.. Regex.Matches(Chart(page), "<a class=\"sc-head\" href=\"[^\"]*#step-(\\d+)\"").Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))];

    private static List<string> Rows(Page page) =>
        [.. Regex.Matches(Chart(page), "<a class=\"sc-name[^\"]*\" href=\"[^\"]*/entity/([^\"]+)\"").Select(match => match.Groups[1].Value)];

    private static int Marks(Page page) => Regex.Matches(Chart(page), "<a class=\"sc-mark").Count;

    private static double PlotWidth(Page page) =>
        double.Parse(Regex.Match(page.Html, "<svg class=\"sc-plot\" width=\"([0-9.]+)em\"").Groups[1].Value, CultureInfo.InvariantCulture);

    private static string Side(Page page, string name) =>
        Regex.Match(page.Html, $"<span class=\"stage-label\">{Regex.Escape(name)}</span>(.*?)</div>", RegexOptions.Singleline).Groups[1].Value;

    [Theory]
    [InlineData(0, "en")]
    [InlineData(2, "fa")]
    [InlineData(4, "en")]
    [InlineData(4, "fa")]
    public async Task The_chart_has_the_steps_the_names_and_the_marks_of_the_projection(int level, string language)
    {
        var page = await Open(level, language: language);
        var lanes = At(level).Lanes();

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal(lanes.Steps.Select(step => step.Number), Columns(page));
        Assert.Equal(lanes.Lanes.Select(lane => lane.Entity.Id.Value), Rows(page));
        Assert.Equal(lanes.Lanes.Sum(lane => lane.Cells.Count(cell => cell is not null)), Marks(page));

        // The table below still says the same, for every step and every name.
        Assert.Equal(lanes.Lanes.Length, Regex.Matches(page.Html, "<th scope=\"row\">").Count);
    }

    [Fact]
    public async Task A_mark_says_who_has_which_part_in_which_step_and_leads_to_the_step()
    {
        var page = await Open(4);
        var chart = WebUtility.HtmlDecode(Chart(page));

        Assert.Contains("title=\"Rostam: takes part – step 1, A missing horse and a welcome\"", chart, StringComparison.Ordinal);
        Assert.Contains("title=\"Rostam: is only mentioned, not present – step 3, Sohrab learns his lineage\"", chart, StringComparison.Ordinal);

        // Somebody who is only spoken of has a hollow mark and is told apart by more than its colour.
        Assert.Matches("<a class=\"sc-mark sc-absent\" href=\"story-rostam-sohrab/story#step-3\" title=\"Rostam: is only mentioned[^\"]*\">\\s*<span aria-hidden=\"true\">○</span>", chart);
        Assert.Matches("<a class=\"sc-mark \" href=\"story-rostam-sohrab/story#step-1\" title=\"Rostam: takes part[^\"]*\">\\s*<span aria-hidden=\"true\">●</span><span class=\"visually-hidden\">Rostam: takes part – step 1, A missing horse and a welcome</span>", chart);
        Assert.Contains("What the marks mean", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_chart_stands_in_a_stage_with_the_steps_at_its_sides_and_says_what_it_shows()
    {
        var page = await Open(4);

        Assert.Contains("<div class=\"stage \" id=\"chart\" dir=\"ltr\">", page.Html, StringComparison.Ordinal);
        Assert.Contains("<div class=\"stage-frame\" tabindex=\"0\" role=\"region\" aria-label=\"The story as a chart: Rostam and Sohrab\" dir=\"ltr\">", page.Html, StringComparison.Ordinal);
        Assert.Matches("<div class=\"stage-edge stage-left\" dir=\"ltr\">\\s*<span class=\"stage-label\">Earlier steps</span>", page.Html);
        Assert.Matches("<div class=\"stage-edge stage-right\" dir=\"ltr\">\\s*<span class=\"stage-label\">Later steps</span>", page.Html);
        Assert.Matches("<div class=\"stage-edge stage-top\" dir=\"ltr\">\\s*<span class=\"stage-label\">Names of the steps</span>", page.Html);
        Assert.Matches("<div class=\"stage-edge stage-bottom\" dir=\"ltr\">\\s*<span class=\"stage-label\">Names</span>", page.Html);
        Assert.Contains("Shown: steps 1 to 8 of 8, and 12 of 12 names.", page.Main, StringComparison.Ordinal);
        Assert.Contains("an order, not a calendar", page.Main, StringComparison.Ordinal);

        // No script and no style of its own.
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_whole_story_is_open_at_first_and_is_closed_a_step_at_a_time_from_either_side()
    {
        var whole = await Open(4);
        var part = await Open(4, "?from=2&to=3");

        // Everything is open: there is nothing to open further, and a step to close at each side. A step leads
        // back to the stage, so that the chart stays on the screen.
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Earlier steps: open a step further (not possible here)\">", Side(whole, "Earlier steps"), StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"story-rostam-sohrab/story?from=2#chart\" title=\"Earlier steps: close a step\">", Side(whole, "Earlier steps"), StringComparison.Ordinal);
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Later steps: open a step further (not possible here)\">", Side(whole, "Later steps"), StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"story-rostam-sohrab/story?to=7#chart\" title=\"Later steps: close a step\">", Side(whole, "Later steps"), StringComparison.Ordinal);

        Assert.Equal([2, 3], Columns(part));
        Assert.Equal(12, Rows(part).Count);
        Assert.Contains("Shown: steps 2 to 3 of 8, and 12 of 12 names.", part.Main, StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story?to=3#chart\" title=\"Earlier steps: open a step further\"", Side(part, "Earlier steps"), StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story?from=3&amp;to=3#chart\" title=\"Earlier steps: close a step\"", Side(part, "Earlier steps"), StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story?from=2&amp;to=4#chart\" title=\"Later steps: open a step further\"", Side(part, "Later steps"), StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story?from=2&amp;to=2#chart\" title=\"Later steps: close a step\"", Side(part, "Later steps"), StringComparison.Ordinal);

        // The last step that is left cannot be closed.
        var one = await Open(4, "?from=5&to=5");
        Assert.Equal([5], Columns(one));
        Assert.Contains("title=\"Earlier steps: close a step (not possible here)\"", Side(one, "Earlier steps"), StringComparison.Ordinal);
        Assert.Contains("title=\"Later steps: close a step (not possible here)\"", Side(one, "Later steps"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_names_of_the_steps_and_the_rows_are_closed_and_opened_at_their_sides()
    {
        var whole = await Open(4);
        var numbers = await Open(4, "?heads=numbers");
        var few = await Open(4, "?lanes=3");

        // With their names the columns are wide; with their numbers alone the whole story fits a narrow screen.
        Assert.Equal(8 * 8.5, PlotWidth(whole), precision: 3);
        Assert.Equal(8, Regex.Matches(Chart(whole), "<bdi class=\"sc-title\">").Count);
        Assert.Contains("href=\"story-rostam-sohrab/story?heads=numbers#chart\" title=\"Names of the steps: close a step\"", Side(whole, "Names of the steps"), StringComparison.Ordinal);

        Assert.Equal(8 * 3.2, PlotWidth(numbers), precision: 3);
        Assert.Empty(Regex.Matches(Chart(numbers), "<bdi class=\"sc-title\">"));
        Assert.Contains("title=\"Sohrab learns his lineage\"", Chart(numbers), StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story#chart\" title=\"Names of the steps: open a step further\"", Side(numbers, "Names of the steps"), StringComparison.Ordinal);
        Assert.Equal(Marks(whole), Marks(numbers));

        Assert.Equal(["character-rostam", "character-rakhsh", "character-king-samangan"], Rows(few));
        Assert.Contains("Shown: steps 1 to 8 of 8, and 3 of 12 names.", few.Main, StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story?lanes=4#chart\" title=\"Names: open a step further\"", Side(few, "Names"), StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story?lanes=2#chart\" title=\"Names: close a step\"", Side(few, "Names"), StringComparison.Ordinal);
        Assert.Contains("href=\"story-rostam-sohrab/story?lanes=11#chart\" title=\"Names: close a step\"", Side(whole, "Names"), StringComparison.Ordinal);

        // The table keeps every name.
        Assert.Equal(12, Regex.Matches(few.Html, "<th scope=\"row\">").Count);
    }

    [Theory]
    [InlineData("?to=8")]
    [InlineData("?to=99&lanes=99")]
    [InlineData("?from=0")]
    [InlineData("?from=-2&to=two")]
    [InlineData("?lanes=0&heads=large")]
    public async Task What_an_address_asks_for_beyond_the_story_or_without_meaning_is_the_whole_chart(string query)
    {
        var page = await Open(4, query);
        var plain = await Open(4);

        Assert.Equal(Chart(plain), Chart(page));
        Assert.Contains("Shown: steps 1 to 8 of 8, and 12 of 12 names.", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task At_the_opening_the_chart_has_one_step_and_no_side_leads_further()
    {
        // The reading position decides, whatever the address asks for.
        foreach (var query in new[] { string.Empty, "?to=8", "?from=3&to=8&lanes=12" })
        {
            var page = await Open(0, query);

            Assert.Equal([1], Columns(page));
            Assert.Equal(At(0).Lanes().Lanes.Length, Rows(page).Count);
            Assert.DoesNotContain("Sohrab", Chart(page), StringComparison.Ordinal);
            Assert.DoesNotContain("href=", Side(page, "Later steps"), StringComparison.Ordinal);
            Assert.DoesNotContain("href=", Side(page, "Earlier steps"), StringComparison.Ordinal);
            Assert.Contains("Shown: steps 1 to 1 of 1,", page.Main, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task On_a_Persian_page_the_chart_begins_at_the_right_and_counts_in_Persian_digits()
    {
        var english = await Open(4);
        var persian = await Open(4, language: "fa");

        // The first step stands where reading begins; the sides of the stage follow.
        Assert.Matches("<foreignObject x=\"0em\" y=\"0\" width=\"8.5em\"[^>]*>\\s*<a class=\"sc-head\" href=\"[^\"]*#step-1\"", english.Html);
        Assert.Matches("<foreignObject x=\"59.5em\" y=\"0\" width=\"8.5em\"[^>]*>\\s*<a class=\"sc-head\" href=\"[^\"]*#step-1\"", persian.Html);
        Assert.Contains("<div class=\"sc\" dir=\"rtl\">", persian.Html, StringComparison.Ordinal);

        var decoded = WebUtility.HtmlDecode(persian.Html);
        Assert.Matches("<div class=\"stage-edge stage-right\" dir=\"rtl\">\\s*<span class=\"stage-label\">گام‌های پیش‌تر</span>", decoded);
        Assert.Matches("<div class=\"stage-edge stage-left\" dir=\"rtl\">\\s*<span class=\"stage-label\">گام‌های پس‌تر</span>", decoded);
        Assert.Contains("نمایش: گام ۱ تا ۸ از ۸، و ۱۲ نام از ۱۲.", persian.Main, StringComparison.Ordinal);
        Assert.Contains("<bdi class=\"sc-title\">سهراب تبار خود را می‌شناسد</bdi>", decoded, StringComparison.Ordinal);
        Assert.DoesNotMatch("(x|y|x1|x2|y1|y2|width|height)=\"[^\"]*[۰-۹٫]", decoded);
        Assert.Equal(PlotWidth(english), PlotWidth(persian));
    }
}
