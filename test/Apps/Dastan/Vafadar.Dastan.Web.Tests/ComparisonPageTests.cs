using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The page of a comparison: lanes of periods and single events over one axis of years, drawn by the server
/// (ADR 0008), with the same lanes as a table. How the chart is looked at lives in the address.
/// </summary>
public sealed class ComparisonPageTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Address = "compare/iran-china-rome";

    private static readonly string[] Keys =
    [
        "eastern-zhou", "qin", "han", "division", "sui", "tang", "gan-ying", "daqin-envoys",
        "medes", "achaemenid", "seleucid", "parthian", "sasanian", "rashidun", "umayyad", "gaugamela", "carrhae", "valerian",
        "roman-kingdom", "roman-republic", "roman-empire", "western-rome", "eastern-rome", "trajan",
    ];

    private Task<Page> Open(string query = "", string language = "en") => site.Visitor().Get($"/{language}/{Address}{query}");

    // The length of the drawing across the page, in em: the width of its plot.
    private static double PlotWidth(Page page) =>
        double.Parse(Regex.Match(page.Html, "<svg class=\"tl-plot\" width=\"([0-9.]+)em\"").Groups[1].Value, CultureInfo.InvariantCulture);

    // The steps at a side of the stage: what stands after the name of the side, up to the end of the side.
    private static string Side(Page page, string name) =>
        Regex.Match(page.Html, $"<span class=\"stage-label\">{Regex.Escape(name)}</span>(.*?)</div>", RegexOptions.Singleline).Groups[1].Value;

    private static string Choice(Page page, string name) =>
        Regex.Match(page.Html, $"<(a|span) class=\"choice\"[^>]*>{Regex.Escape(name.Replace("'", "&#x27;", StringComparison.Ordinal))}</(a|span)>").Value;

    [Fact]
    public async Task The_start_page_leads_to_the_comparison()
    {
        var english = await site.Visitor().Get("/en/");
        var persian = await site.Visitor().Get("/fa/");

        Assert.Contains("Side by side", english.Main, StringComparison.Ordinal);
        Assert.Contains("Iran, China and Rome side by side", english.Main, StringComparison.Ordinal);
        Assert.Contains(Address, english.Links);
        Assert.Contains("ایران، چین و روم در کنار هم", persian.Main, StringComparison.Ordinal);
        Assert.Contains(Address, persian.Links);
    }

    [Fact]
    public async Task The_page_shows_the_comparison_and_says_that_it_is_a_draft()
    {
        var page = await Open();

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<title>Iran, China and Rome side by side – Dastan</title>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains("What ruled in Iran, in China and in Rome between 600 BCE and 700 CE", page.Main, StringComparison.Ordinal);
        Assert.Contains("Draft. The periods and years of this comparison are proposed", page.Main, StringComparison.Ordinal);
        Assert.Contains("Shown: 600 BCE to 700 CE", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_a_choice_both_directions_are_in_the_page_and_the_screen_decides()
    {
        var fit = await Open();
        var across = await Open("?view=across");
        var down = await Open("?view=down");

        Assert.Matches("<div class=\"stage only-wide\" dir=\"ltr\">.*?<div class=\"tl tl-across\" dir=\"ltr\">", fit.Html.ReplaceLineEndings(" "));
        Assert.Matches("<div class=\"stage only-narrow\" dir=\"ltr\">.*?<div class=\"tl tl-down\" dir=\"ltr\">", fit.Html.ReplaceLineEndings(" "));

        // A visitor who chose gets the one they chose, on every screen.
        Assert.Contains("<div class=\"stage \" dir=\"ltr\">", across.Html, StringComparison.Ordinal);
        Assert.Contains("<div class=\"tl tl-across\" dir=\"ltr\">", across.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("tl-down", across.Html, StringComparison.Ordinal);
        Assert.Contains("<div class=\"tl tl-down\" dir=\"ltr\">", down.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("tl-across", down.Html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(down.Html, "<div class=\"stage "));
        Assert.Contains("aria-current=\"true\"", Choice(down, "Down the page"), StringComparison.Ordinal);
        Assert.DoesNotContain("aria-current", Choice(down, "Across the page"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_chart_is_drawn_without_a_script_and_without_a_style_of_its_own()
    {
        var page = await Open();

        // The security policy of the site allows neither (ADR 0003): places and sizes are attributes of the SVG.
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<style", page.Html, StringComparison.Ordinal);
        Assert.Matches("<svg class=\"tl-plot\" width=\"[0-9.]+em\" height=\"[0-9.]+em\">", page.Html);
    }

    [Fact]
    public async Task The_time_scrolls_in_a_frame_that_the_keyboard_reaches_and_a_screen_reader_can_name()
    {
        var page = await Open("?view=across");

        Assert.Contains("<div class=\"stage-frame\" tabindex=\"0\" role=\"region\" aria-label=\"Timeline: Iran, China and Rome side by side\" dir=\"ltr\">", page.Html, StringComparison.Ordinal);

        // At the scale a chart starts at, 1300 years are longer than any screen is wide.
        Assert.Equal((1300 * 0.06) + 4.8, PlotWidth(page), precision: 3);
    }

    [Fact]
    public async Task Every_period_and_event_has_its_row_in_the_table_and_a_bar_leads_to_its_row()
    {
        var page = await Open("?view=across");
        var rows = Regex.Matches(page.Html, "<tr id=\"item-([a-z-]+)\"").Select(match => match.Groups[1].Value).ToList();
        var targets = page.Links.Where(link => link.Contains("#item-", StringComparison.Ordinal)).ToList();

        Assert.Equal(Keys, rows);
        Assert.NotEmpty(targets);
        Assert.All(targets, link =>
        {
            Assert.StartsWith($"{Address}?view=across#item-", link, StringComparison.Ordinal);
            Assert.Contains(link[(link.IndexOf('#', StringComparison.Ordinal) + 6)..], rows);
        });

        // Every event has a callout; the table says for each statement how far the editors are and what supports it.
        foreach (var key in new[] { "gan-ying", "daqin-envoys", "gaugamela", "carrhae", "valerian", "trajan" })
        {
            Assert.Contains($"{Address}?view=across#item-{key}", targets);
        }

        Assert.Equal(Keys.Length, Regex.Matches(page.Main, "Suggested by the editors, not checked yet").Count);
        Assert.Equal(Keys.Length, Regex.Matches(page.Main, "No source recorded").Count);
        Assert.Equal(["China", "Iran", "Rome"], Regex.Matches(page.Html, "<caption><bdi>([^<]+)</bdi></caption>").Select(match => match.Groups[1].Value));
    }

    [Fact]
    public async Task The_table_states_a_time_as_it_is_stated()
    {
        var page = await Open();

        Assert.Contains("550 BCE – 330 BCE", page.Main, StringComparison.Ordinal);
        Assert.Contains("about 678 BCE – 550 BCE", page.Main, StringComparison.Ordinal);
        Assert.Contains("330 BCE – between 247 BCE and 141 BCE", page.Main, StringComparison.Ordinal);
        Assert.Contains("206 BCE – 220 CE", page.Main, StringComparison.Ordinal);
        Assert.Contains("With the years of the Xin (9–23) between its two halves.", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_bar_carries_what_it_has_room_for_and_never_a_year_nobody_stated()
    {
        var page = await Open("?view=across");
        var open = await Open("?view=across&scale=6&from=-700&to=700");

        // Fifteen years are too short for a name at this scale: the bar keeps its title and is no link to follow.
        Assert.Contains("<span class=\"tl-bar  lane-1\" title=\"Qin: 221 BCE &#x2013; 206 BCE\"></span>", page.Html, StringComparison.Ordinal);
        Assert.Matches("<a class=\"tl-bar  lane-1\" href=\"compare/iran-china-rome\\?view=across&amp;scale=6&amp;from=-700&amp;to=700#item-qin\"[^>]*>\\s*<bdi class=\"tl-year\">221 BCE</bdi>\\s*<bdi class=\"tl-name\">Qin</bdi>\\s*<bdi class=\"tl-year\">206 BCE</bdi>", open.Html);

        // An end that is stated as "about" is written so and fades; an end between two years fades and has no year.
        Assert.Matches("<a class=\"tl-bar tl-soft-start lane-2\"[^>]*#item-medes\"[^>]*>\\s*<bdi class=\"tl-year\">c\\. 678 BCE</bdi>", open.Html);
        Assert.Matches("<a class=\"tl-bar tl-soft-end lane-2\"[^>]*#item-seleucid\"[^>]*>\\s*<bdi class=\"tl-year\">330 BCE</bdi>\\s*<bdi class=\"tl-name\">Alexander and the Seleucids</bdi>\\s*</a>", open.Html);

        // A period that reaches out of the time shown is cut to a point at that end.
        Assert.Matches("<a class=\"tl-bar tl-cut-start lane-1\"[^>]*#item-eastern-zhou\"", page.Html);
        Assert.Matches("<a class=\"tl-bar tl-cut-end lane-3\"[^>]*#item-eastern-rome\"", page.Html);
    }

    [Fact]
    public async Task A_callout_names_the_year_of_its_event()
    {
        var english = await Open("?view=across");
        var persian = await Open("?view=across", "fa");

        Assert.Contains(">97 CE – Gan Ying reaches the Persian Gulf</a>", WebUtility.HtmlDecode(english.Html), StringComparison.Ordinal);
        Assert.Contains(">331 BCE – Battle of Gaugamela</a>", WebUtility.HtmlDecode(english.Html), StringComparison.Ordinal);
        Assert.Contains(">۹۷ م – رسیدن گان‌ینگ به خلیج فارس</a>", WebUtility.HtmlDecode(persian.Html), StringComparison.Ordinal);
        Assert.Contains(">۳۳۱ پ.م – نبرد گوگمل</a>", WebUtility.HtmlDecode(persian.Html), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_axis_names_the_era_of_every_year_and_marks_where_the_common_era_begins()
    {
        var page = await Open("?view=across");
        var ticks = Regex.Matches(page.Html, "<tspan class=\"tl-tick\">([^<]+)</tspan>").Select(match => match.Groups[1].Value).ToList();

        Assert.Equal(
            ["600 BCE", "500 BCE", "400 BCE", "300 BCE", "200 BCE", "100 BCE", "1 CE", "100 CE", "200 CE", "300 CE", "400 CE", "500 CE", "600 CE", "700 CE"],
            ticks);
        Assert.Single(Regex.Matches(page.Html, "<line class=\"tl-grid tl-era\""));
    }

    [Fact]
    public async Task On_a_Persian_page_the_texts_are_Persian_and_the_time_still_runs_from_left_to_right()
    {
        var page = await Open(language: "fa");

        Assert.Equal("rtl", page.Root("dir"));
        Assert.Contains("<div class=\"tl tl-across\" dir=\"ltr\">", page.Html, StringComparison.Ordinal);
        Assert.Contains("<div class=\"tl tl-down\" dir=\"ltr\">", page.Html, StringComparison.Ordinal);

        // The stage keeps its sides where they are on the screen, and its frame begins at the left; what stands
        // at a side is Persian and written from the right.
        Assert.Equal(2, Regex.Matches(page.Html, "<div class=\"stage-frame\" [^>]*dir=\"ltr\">").Count);
        Assert.Contains("<div class=\"stage-edge stage-left\" dir=\"rtl\">", page.Html, StringComparison.Ordinal);
        Assert.Contains("title=\"سال‌های پیش‌تر: یک گام بازتر\"", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains("direction=\"rtl\"><tspan class=\"tl-tick\">۶۰۰ پ.م</tspan></text>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains("نمایش از ۶۰۰ پ.م تا ۷۰۰ م", page.Main, StringComparison.Ordinal);
        Assert.Contains("شاهنشاهی هخامنشی", page.Main, StringComparison.Ordinal);
        Assert.Contains("۵۵۰ پیش از میلاد تا ۳۳۰ پیش از میلاد", page.Main, StringComparison.Ordinal);
        Assert.Contains("میان ۲۴۷ پیش از میلاد و ۱۴۱ پیش از میلاد", page.Main, StringComparison.Ordinal);

        // The same years stand at the same places as on the English page.
        Assert.Equal(PlotWidth(await Open()), PlotWidth(page));
    }

    [Theory]
    [InlineData("?scale=3", 0.12)]
    [InlineData("?scale=0", 0.015)]
    [InlineData("?scale=6", 1.0)]
    [InlineData("?scale=7", 0.06)]
    [InlineData("?scale=-1", 0.06)]
    [InlineData("?scale=wide", 0.06)]
    public async Task The_scale_is_one_of_the_scales_of_a_chart(string query, double unitsPerYear)
    {
        var page = await Open(query + "&view=across");

        Assert.Equal((1300 * unitsPerYear) + 4.8, PlotWidth(page), precision: 3);
    }

    [Fact]
    public async Task Opening_further_and_drawing_closer_together_are_links_that_end_where_the_scales_end()
    {
        var start = await Open();
        var closest = await Open("?scale=0");
        var furthest = await Open("?scale=6&view=down");

        Assert.Contains("href=\"compare/iran-china-rome?scale=3\"", Choice(start, "Open further"), StringComparison.Ordinal);
        Assert.Contains("href=\"compare/iran-china-rome?scale=1\"", Choice(start, "Closer together"), StringComparison.Ordinal);
        Assert.Equal("<span class=\"choice\" aria-disabled=\"true\">Closer together</span>", Choice(closest, "Closer together"));
        Assert.Equal("<span class=\"choice\" aria-disabled=\"true\">Open further</span>", Choice(furthest, "Open further"));

        // A step keeps what else the visitor chose; the default scale is not written into an address.
        Assert.Contains("href=\"compare/iran-china-rome?view=down&amp;scale=5\"", Choice(furthest, "Closer together"), StringComparison.Ordinal);
        Assert.Contains("href=\"compare/iran-china-rome\"", Choice(await Open("?scale=3"), "Closer together"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?from=0&to=300")]
    [InlineData("?from=-300&to=0")]
    [InlineData("?from=300&to=-300")]
    [InlineData("?from=300&to=300")]
    [InlineData("?from=-300")]
    [InlineData("?from=+300&to=400")]
    [InlineData("?from=early&to=late")]
    [InlineData("?from=-10000&to=300")]
    public async Task A_stretch_of_time_that_cannot_be_read_is_not_a_choice(string query)
    {
        var page = await Open(query);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Shown: 600 BCE to 700 CE", page.Main, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"true\"", Choice(page, "The comparison's own time"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_time_is_opened_and_closed_at_the_two_sides_it_runs_between()
    {
        var start = await Open("?view=across");
        var opened = await Open("?view=across&from=-700&to=700");

        // Across the page the earlier years are at the left side of the stage and the later ones at the right.
        Assert.Matches("<div class=\"stage-edge stage-left\" dir=\"ltr\">\\s*<span class=\"stage-label\">Earlier years</span>", start.Html);
        Assert.Matches("<div class=\"stage-edge stage-right\" dir=\"ltr\">\\s*<span class=\"stage-label\">Later years</span>", start.Html);

        // A step is the distance between two years of the axis: a century at this scale.
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across&amp;from=-700&amp;to=700\" title=\"Earlier years: open a step further\">", Side(start, "Earlier years"), StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across&amp;from=-500&amp;to=700\" title=\"Earlier years: close a step\">", Side(start, "Earlier years"), StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across&amp;from=-600&amp;to=800\" title=\"Later years: open a step further\">", Side(start, "Later years"), StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across&amp;from=-600&amp;to=600\" title=\"Later years: close a step\">", Side(start, "Later years"), StringComparison.Ordinal);

        // From the beginning of 700 BCE to the end of 700 CE are 1400 years; the sign is what is seen, the words
        // are read out.
        Assert.Contains("Shown: 700 BCE to 700 CE", opened.Main, StringComparison.Ordinal);
        Assert.Equal((1400 * 0.06) + 4.8, PlotWidth(opened), precision: 3);
        Assert.Contains("<span aria-hidden=\"true\">&#x2B;</span><span class=\"visually-hidden\">Earlier years: open a step further</span>", Side(opened, "Earlier years"), StringComparison.Ordinal);
        Assert.DoesNotContain("aria-current", Choice(opened, "The comparison's own time"), StringComparison.Ordinal);

        // Closing the step again leads back to the address of the comparison's own time.
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across\" title=\"Earlier years: close a step\">", Side(opened, "Earlier years"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_side_that_cannot_be_opened_or_closed_further_says_so_and_is_no_link()
    {
        var narrow = await Open("?view=across&from=1&to=100");
        var early = await Open("?view=across&from=-9999&to=-9000&scale=0");

        // A century is one step: closing it would leave nothing.
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Earlier years: close a step (not possible here)\">", Side(narrow, "Earlier years"), StringComparison.Ordinal);
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Later years: close a step (not possible here)\">", Side(narrow, "Later years"), StringComparison.Ordinal);
        Assert.Contains("href=", Side(narrow, "Later years"), StringComparison.Ordinal);

        // Before the first year an address can name there is nothing to open.
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Earlier years: open a step further (not possible here)\">", Side(early, "Earlier years"), StringComparison.Ordinal);
        Assert.Contains("title=\"Later years: open a step further\"", Side(early, "Later years"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_single_events_are_closed_and_opened_at_the_side_where_their_callouts_stand()
    {
        var with = await Open("?view=across");
        var without = await Open("?view=across&events=off");

        Assert.Matches("<div class=\"stage-edge stage-top\" dir=\"ltr\">\\s*<span class=\"stage-label\">Events</span>", with.Html);
        Assert.Equal(6, Regex.Matches(with.Html, "<a class=\"tl-callout ").Count);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across&amp;events=off\" title=\"Events: close a step\">", Side(with, "Events"), StringComparison.Ordinal);
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Events: open a step further (not possible here)\">", Side(with, "Events"), StringComparison.Ordinal);

        // Without its events the chart has no callout and no line to one; the table keeps them.
        Assert.Empty(Regex.Matches(without.Html, "<a class=\"tl-callout "));
        Assert.Empty(Regex.Matches(without.Html, "class=\"tl-leader "));
        Assert.Contains("<tr id=\"item-gaugamela\"", without.Html, StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across\" title=\"Events: open a step further\">", Side(without, "Events"), StringComparison.Ordinal);
        Assert.True(double.Parse(Regex.Match(without.Html, "<svg class=\"tl-plot\" width=\"[0-9.]+em\" height=\"([0-9.]+)em\"").Groups[1].Value, CultureInfo.InvariantCulture)
            < double.Parse(Regex.Match(with.Html, "<svg class=\"tl-plot\" width=\"[0-9.]+em\" height=\"([0-9.]+)em\"").Groups[1].Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task The_lanes_are_closed_and_opened_from_the_last_one()
    {
        var all = await Open("?view=across");
        var two = await Open("?view=across&lanes=2");
        var one = await Open("?view=across&lanes=1");

        Assert.Matches("<div class=\"stage-edge stage-bottom\" dir=\"ltr\">\\s*<span class=\"stage-label\">Subjects</span>", all.Html);
        Assert.Equal(3, Regex.Matches(all.Html, "<div class=\"tl-lane-name ").Count);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across&amp;lanes=2\" title=\"Subjects: close a step\">", Side(all, "Subjects"), StringComparison.Ordinal);
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Subjects: open a step further (not possible here)\">", Side(all, "Subjects"), StringComparison.Ordinal);

        // China and Iran; Rome is closed, with its bars and its event. The table keeps all three.
        Assert.Equal(2, Regex.Matches(two.Html, "<div class=\"tl-lane-name ").Count);
        Assert.DoesNotContain("#item-roman-republic\"", two.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("#item-trajan\"", two.Html, StringComparison.Ordinal);
        Assert.Contains("<tr id=\"item-roman-republic\"", two.Html, StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across\" title=\"Subjects: open a step further\">", Side(two, "Subjects"), StringComparison.Ordinal);
        Assert.Contains("<a class=\"stage-step\" href=\"compare/iran-china-rome?view=across&amp;lanes=1\" title=\"Subjects: close a step\">", Side(two, "Subjects"), StringComparison.Ordinal);

        // The last lane stays: a comparison shows at least one subject.
        Assert.Single(Regex.Matches(one.Html, "<div class=\"tl-lane-name "));
        Assert.Contains("<span class=\"stage-step\" aria-disabled=\"true\" title=\"Subjects: close a step (not possible here)\">", Side(one, "Subjects"), StringComparison.Ordinal);
        Assert.Equal(PlotWidth(all), PlotWidth(one));
    }

    [Theory]
    [InlineData("?lanes=0")]
    [InlineData("?lanes=many")]
    [InlineData("?lanes=100")]
    [InlineData("?events=no")]
    public async Task A_number_of_lanes_or_a_word_for_the_events_that_is_no_choice_is_not_one(string query)
    {
        var page = await Open(query + "&view=across");

        Assert.Equal(3, Regex.Matches(page.Html, "<div class=\"tl-lane-name ").Count);
        Assert.Equal(6, Regex.Matches(page.Html, "<a class=\"tl-callout ").Count);
    }

    [Fact]
    public async Task Down_the_page_the_sides_of_the_stage_follow_the_chart()
    {
        var page = await Open("?view=down");

        // The time runs from top to bottom, the lanes stand beside each other from the left, the events at the right.
        Assert.Matches("<div class=\"stage-edge stage-top\" dir=\"ltr\">\\s*<span class=\"stage-label\">Earlier years</span>", page.Html);
        Assert.Matches("<div class=\"stage-edge stage-bottom\" dir=\"ltr\">\\s*<span class=\"stage-label\">Later years</span>", page.Html);
        Assert.Matches("<div class=\"stage-edge stage-left\" dir=\"ltr\">\\s*<span class=\"stage-label\">Subjects</span>", page.Html);
        Assert.Matches("<div class=\"stage-edge stage-right\" dir=\"ltr\">\\s*<span class=\"stage-label\">Events</span>", page.Html);
    }

    [Theory]
    [InlineData("/en/compare/nothing")]
    [InlineData("/en/compare/Iran-China-Rome")]
    [InlineData("/en/compare/iran-china-rome/more")]
    public async Task A_comparison_that_does_not_exist_is_not_found(string address)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get(address)).Status);
    }
}

/// <summary>The public does not get a comparison that is not approved for it.</summary>
public sealed class PublicComparisonTests(PublicSite site) : IClassFixture<PublicSite>
{
    [Fact]
    public async Task A_comparison_that_is_not_approved_for_the_public_does_not_exist_for_it()
    {
        var start = await site.Visitor().Get("/en/");
        var page = await site.Visitor().Get("/en/compare/iran-china-rome");

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
        Assert.DoesNotContain("Iran, China and Rome", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("compare/", start.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Side by side", start.Html, StringComparison.Ordinal);
    }
}
