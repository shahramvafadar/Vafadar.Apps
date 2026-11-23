using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The state of exploring on the pages (step 20.6, ADR 0025): the same bar on every view of a name, each view
/// led to with everything the state says, what is chosen shown without leaving, and what was left out
/// elsewhere named where it is shown.
/// </summary>
public sealed class ExploringPagesTests(DemoSite site) : IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Rostam = $"{Story}/entity/character-rostam";

    private async Task<Browser> Reader(int stage = 4, string language = "en")
    {
        var visitor = site.Visitor();
        await visitor.ReadTo(stage, language);
        return visitor;
    }

    // The links of the views, each with whether it is the view that is shown.
    private static (string Address, bool Shown)[] Views(Page page) =>
        [.. Regex.Matches(page.Main, "<a href=\"([^\"]*)\"( aria-current=\"page\")? data-explore>").Select(match => (WebUtility.HtmlDecode(match.Groups[1].Value), match.Groups[2].Success))];

    private static string Words(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), "\\s+", " ").Trim();

    private static string Chosen(Page page) => Regex.Match(page.Main, "<div class=\"explore-chosen\".*?</div>", RegexOptions.Singleline).Value;

    [Theory]
    [InlineData("", 0)]
    [InlineData("/connections", 1)]
    [InlineData("/family", 2)]
    [InlineData("/gallery", 3)]
    public async Task Every_view_of_a_name_has_the_same_bar_and_marks_itself(string view, int shown)
    {
        var page = await (await Reader()).Get($"/en/{Rostam}{view}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal([(Rostam, shown == 0), ($"{Rostam}/connections", shown == 1), ($"{Rostam}/family", shown == 2), ($"{Rostam}/gallery", shown == 3)], Views(page));
        Assert.Contains("Overview Connections Family Pictures", Words(Regex.Match(page.Main, "<nav class=\"view-bar\".*?</nav>", RegexOptions.Singleline).Value), StringComparison.Ordinal);
        Assert.Empty(Chosen(page));
        Assert.DoesNotContain("explore-aside", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_view_a_name_does_not_have_is_not_offered_and_a_page_of_no_single_name_has_no_bar()
    {
        var visitor = await Reader();

        // The horse has no family and no pictures of its own.
        var horse = await visitor.Get($"/en/{Story}/entity/character-rakhsh");
        Assert.DoesNotContain(Views(horse), view => view.Address.EndsWith("/family", StringComparison.Ordinal));

        // The page of a view that has nothing still names itself in its bar: the visitor sees where they are.
        var noFamily = await visitor.Get($"/en/{Story}/entity/character-rakhsh/family");
        Assert.Contains(Views(noFamily), view => view.Shown && view.Address.EndsWith("/family", StringComparison.Ordinal));

        // The chart and the pictures of the whole collection explore no single name.
        Assert.Empty(Views(await visitor.Get($"/en/{Story}/timeline")));
        Assert.Empty(Views(await visitor.Get($"/en/{Story}/gallery")));
    }

    [Fact]
    public async Task Choosing_looks_closer_without_leaving_and_the_middle_is_changed_by_an_act_of_its_own()
    {
        var visitor = await Reader();
        var connections = await visitor.Get($"/en/{Rostam}/connections");

        // Every statement of the list offers to look closer at the other one.
        var choose = WebUtility.HtmlDecode(Regex.Match(connections.Main, "<a class=\"choose\" href=\"([^\"]*sel=character-zal[^\"]*)\"").Groups[1].Value);
        Assert.Equal($"{Rostam}/connections?sel=character-zal#chosen", choose);

        var chosen = await visitor.Get($"/en/{choose}");
        Assert.Equal("Chosen: Zal – Character Put in the middle – Zal The whole profile – Zal Choose nothing – Zal", Words(Chosen(chosen)));

        // The page is still the page of the first name, and every view is led to with what is chosen.
        Assert.Contains("<h1>Connections of Rostam</h1>", chosen.Main, StringComparison.Ordinal);
        Assert.All(Views(chosen), view => Assert.EndsWith("?sel=character-zal", view.Address, StringComparison.Ordinal));
        Assert.DoesNotContain("sel=character-zal#chosen\">Look closer", chosen.Main, StringComparison.Ordinal);

        // The three acts: the other name in the middle of the same view, its page, and nothing chosen.
        var acts = Regex.Matches(Chosen(chosen), "href=\"([^\"]*)\"").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value)).ToArray();
        Assert.Equal([$"{Story}/entity/character-zal/connections", $"{Story}/entity/character-zal", $"{Rostam}/connections"], acts);
        Assert.Contains("<h1>Connections of Zal</h1>", (await visitor.Get($"/en/{acts[0]}")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_is_chosen_is_shown_only_when_the_visitor_may_see_it()
    {
        // Sohrab is told of from a later stage: at the first, choosing him is choosing nobody – as for a name nobody has.
        var early = await Reader(stage: 0);
        var asked = await early.Get($"/en/{Rostam}/connections?sel=character-sohrab");
        var none = await early.Get($"/en/{Rostam}/connections?sel=character-nobody");

        Assert.Empty(Chosen(asked));
        Assert.DoesNotContain("Sohrab", asked.Main, StringComparison.Ordinal);
        Assert.Equal(Words(none.Main.Replace("character-nobody", "character-sohrab", StringComparison.Ordinal)), Words(asked.Main));

        // Who has read that far sees him chosen.
        Assert.Contains("Sohrab", Words(Chosen(await (await Reader()).Get($"/en/{Rostam}/connections?sel=character-sohrab"))), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Each_view_is_found_as_it_was_left_and_a_step_inside_a_view_loses_nothing()
    {
        var visitor = await Reader();

        // The family is opened further and turned; then the pictures are asked for, oldest first.
        var family = await visitor.Get($"/en/{Rostam}/family?up=3&view=across&sel=character-zal");
        var toPictures = Views(family).Single(view => view.Address.Contains("/gallery", StringComparison.Ordinal)).Address;
        Assert.Equal($"{Rostam}/gallery?sel=character-zal&up=3&tree=across", toPictures);

        var pictures = await visitor.Get($"/en/{toPictures}&order=oldest");
        Assert.Equal(HttpStatusCode.OK, pictures.Status);

        // A choice inside the pictures keeps what the family was told.
        Assert.Contains($"{Rostam}/gallery?order=newest&sel=character-zal&up=3&tree=across", pictures.Links);

        // Back in the family it is as it was left – and now carries the order of the pictures.
        var back = Views(pictures).Single(view => view.Address.Contains("/family", StringComparison.Ordinal)).Address;
        Assert.Equal($"{Rostam}/family?sel=character-zal&up=3&view=across&order=oldest", back);
        var again = await visitor.Get($"/en/{back}");
        Assert.Contains("class=\"graph tree", again.Main, StringComparison.Ordinal);

        // A step inside the family – one generation fewer, which is how a tree opens – loses neither what is chosen nor the order.
        Assert.Contains($"{Rostam}/family?view=across&sel=character-zal&order=oldest#chart", again.Links);
        Assert.Contains("Chosen:", Words(Chosen(again)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_was_left_out_elsewhere_is_named_where_it_is_shown()
    {
        var visitor = await Reader();

        // The connections keep to the years themselves (D-084) and name only the kind of picture.
        var connections = await visitor.Get($"/en/{Rostam}/connections?from=900&to=1000&kind=image");
        Assert.Contains("Not left out in this view: the kind of picture you chose. It is kept for the view it belongs to.", Words(connections.Main), StringComparison.Ordinal);

        // The pictures keep to both and set nothing aside.
        var pictures = await visitor.Get($"/en/{Rostam}/gallery?from=900&to=1000&kind=image");
        Assert.DoesNotContain("explore-aside", pictures.Main, StringComparison.Ordinal);
        Assert.Contains($"{Rostam}/connections?from=900&to=1000&kind=image", Views(pictures).Select(view => view.Address));

        // The family tree keeps to the years too (step 25.3): only the kind of picture is set aside there.
        var family = await visitor.Get($"/en/{Rostam}/family?from=900&to=1000&kind=image");
        Assert.Contains("Not left out in this view: the kind of picture you chose. It is kept for the view it belongs to.", Words(family.Main), StringComparison.Ordinal);

        // How far a drawing is opened leaves nothing out, and neither does what is no choice.
        Assert.DoesNotContain("explore-aside", (await visitor.Get($"/en/{Rostam}/gallery?up=3&open=character-zal&from=1000&to=900&kind=sound")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Only_what_is_open_in_the_drawing_is_carried_as_open()
    {
        var visitor = await Reader();

        var page = await visitor.Get($"/en/{Rostam}/connections?open=character-zal,character-nobody,place-nowhere");

        // The links of the views and the links that choose name what is open, and nothing an address only asked for.
        var links = Views(page).Select(view => view.Address).Concat(page.Links.Where(link => link.Contains("sel=", StringComparison.Ordinal))).ToList();
        Assert.NotEmpty(links);
        Assert.All(links, link => Assert.DoesNotContain("nobody", link, StringComparison.Ordinal));
        Assert.All(links, link => Assert.DoesNotContain("place-nowhere", link, StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_bar_is_written_in_persian_too()
    {
        var page = await (await Reader(language: "fa")).Get($"/fa/{Rostam}/family?sel=character-zal&from=900&to=1000");

        var words = Words(page.Main);
        Assert.Contains("نمای کلی پیوندها خانواده تصویرها", words, StringComparison.Ordinal);
        Assert.Contains("انتخاب‌شده:", words, StringComparison.Ordinal);
        Assert.Contains("در میانه بگذار", words, StringComparison.Ordinal);

        // The family tree keeps to the years (step 25.3): they are said, and nothing is set aside.
        Assert.DoesNotContain("در این نما کنار گذاشته نشده", words, StringComparison.Ordinal);
        Assert.Contains("نسب زمانی ندارد و همیشه رسم می‌شود", words, StringComparison.Ordinal);
    }
}
