using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Affiliations on the site (step 19.6, ADR 0018, PRIV-010 to PRIV-012): somebody says where they were and
/// chooses who sees that and may find them through the place; a search shows who lets this visitor find
/// them, and says the same when there is nobody and when there is nobody this visitor may find. Every test
/// has a site of its own.
/// </summary>
public sealed class AffiliationPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Page = "/en/account/affiliations";
    private const string Forms = "/preferences/affiliations";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    private async Task<Browser> Signed(string name)
    {
        var visitor = _site.Visitor();
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("said=welcome", made.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return visitor;
    }

    private static Task<Page> Send(Browser visitor, string form, params (string, string)[] fields) =>
        visitor.Post($"{Forms}/{form}", Page, [.. fields, ("returnUrl", Page)]);

    private static async Task<string> Me(Browser visitor) =>
        Regex.Match((await visitor.Get("/en/account/people")).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;

    // States a place and says who sees it; returns the number of the affiliation.
    private static async Task<string> At(Browser visitor, string place, string findable, string dates, params (string, string)[] fields)
    {
        var known = Numbers(await visitor.Get(Page));
        Assert.Equal($"{Page}?said=added#mine", (await Send(visitor, "add", [("place", place), .. fields])).Response.Headers.Location?.OriginalString);
        var number = Numbers(await visitor.Get(Page)).Except(known).Single();
        Assert.Equal($"{Page}?said=shown#mine", (await Send(visitor, "show", ("affiliation", number), ("findable", findable), ("dates", dates))).Response.Headers.Location?.OriginalString);
        return number;
    }

    private static string[] Numbers(Page page) => [.. Regex.Matches(page.Html, "<li id=\"affiliation-(\\d+)\"").Select(match => match.Groups[1].Value)];

    private static string Words(Page page) => Regex.Replace(page.Text, "\\s+", " ");

    private static string Found(Page page)
    {
        var from = page.Html.IndexOf("<h3 id=\"found\"", StringComparison.Ordinal);
        return from < 0 ? string.Empty : Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(page.Html[from..page.Html.IndexOf("</section>", from, StringComparison.Ordinal)], "<[^>]+>", " ")), "\\s+", " ").Trim();
    }

    [Theory]
    [InlineData("en", "Where you were", "You have stated no place yet.", "Find people through a place")]
    [InlineData("fa", "جاهایی که بوده‌اید", "هنوز جایی ثبت نکرده‌اید.", "یافتن افراد از راه یک جا")]
    public async Task The_page_is_for_who_signed_in_and_says_what_it_is_for(string language, string title, string none, string find)
    {
        var page = await (await Signed("Mina")).Get($"/{language}/account/affiliations");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"<h1>{title}</h1>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains(none, page.Main, StringComparison.Ordinal);
        Assert.Contains(find, page.Main, StringComparison.Ordinal);
        Assert.Contains("noindex", page.Html, StringComparison.Ordinal);

        // The search asks this page again: its address is written from the language of the page on, once.
        Assert.Contains("<form method=\"get\" action=\"account/affiliations\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"<base href=\"/{language}/\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);

        // Who is not signed in is shown where to sign in; a site without accounts has no such page.
        Assert.EndsWith($"/{language}/account", (await _site.Visitor().Get($"/{language}/account/affiliations")).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        using var plain = new DemoSite();
        Assert.Equal(HttpStatusCode.NotFound, (await plain.Visitor().Get($"/{language}/account/affiliations")).Status);
    }

    [Fact]
    public async Task A_place_is_shown_to_nobody_until_its_account_says_so_and_then_with_or_without_its_dates()
    {
        var mina = await Signed("Mina");
        var about = $"/en/people/{await Me(mina)}";
        await Send(mina, "add", ("place", "School of Kabul"), ("role", "student of history"), ("since", "1998"), ("until", "2002"), ("graduated", "2002"));
        var number = Numbers(await mina.Get(Page)).Single();

        Assert.Contains("School of Kabul – student of history", Words(await mina.Get(Page)), StringComparison.Ordinal);
        Assert.Contains("1998 to 2002, graduated 2002", Words(await mina.Get(Page)), StringComparison.Ordinal);
        Assert.DoesNotContain("School of Kabul", (await _site.Visitor().Get(about)).Main, StringComparison.Ordinal);
        Assert.Contains("School of Kabul", (await mina.Get(about)).Main, StringComparison.Ordinal);

        // Shown to everybody, the dates to nobody: the page about Mina names the place and no year.
        await Send(mina, "show", ("affiliation", number), ("findable", "public"), ("dates", "private"));
        var seen = Words(await _site.Visitor().Get(about));
        Assert.Contains("Where they were School of Kabul – student of history", seen, StringComparison.Ordinal);
        Assert.DoesNotContain("1998", seen, StringComparison.Ordinal);

        await Send(mina, "show", ("affiliation", number), ("findable", "public"), ("dates", "public"));
        Assert.Contains("School of Kabul – student of history – 1998 to 2002, graduated 2002", Words(await _site.Visitor().Get(about)), StringComparison.Ordinal);

        // The form shows the choice that was made.
        Assert.Matches("<select id=\"dates-\\d+\" name=\"dates\">.*?<option value=\"public\" selected>", (await mina.Get(Page)).Html.ReplaceLineEndings(" "));
    }

    [Fact]
    public async Task A_search_finds_who_chose_to_be_found_and_says_the_same_about_nobody_and_about_somebody_hidden()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        await At(mina, "School of Kabul", "public", "public", ("since", "1998"), ("until", "2002"), ("role", "student"));
        await At(sara, "school of kabul", "public", "private", ("since", "1999"), ("until", "2003"));
        await Send(dara, "add", ("place", "School of Kabul"), ("since", "1998"), ("until", "2002"));

        // By the place: who lets Dara find them there – Mina with what she shows, Sara without her dates.
        var found = Found(await dara.Get($"{Page}?place=School+of+Kabul"));
        Assert.Contains("People at: School of Kabul", found, StringComparison.Ordinal);
        Assert.Contains("Mina – student – 1998 to 2002", found, StringComparison.Ordinal);
        Assert.Contains("Sara", found, StringComparison.Ordinal);
        Assert.DoesNotContain("1999", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Dara", found, StringComparison.Ordinal);

        // By a year: only who also shows the dates is asked at all.
        var inYear = Found(await dara.Get($"{Page}?place=School+of+Kabul&year=2000&question=during&calendar=gregorian"));
        Assert.Contains("Mina", inYear, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", inYear, StringComparison.Ordinal);
        Assert.Contains("Mina", Found(await dara.Get($"{Page}?place=School+of+Kabul&year=1998&question=entered")), StringComparison.Ordinal);

        // Nobody there, nobody that may be found, and a place nobody states: the same words.
        const string none = "People at: {0} Nobody you may find.";
        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, none, "School of Kabul"), Found(await dara.Get($"{Page}?place=School+of+Kabul&year=2010")));
        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, none, "School of Kabul"), Found(await dara.Get($"{Page}?place=School+of+Kabul&year=2003")));
        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, none, "Nowhere"), Found(await dara.Get($"{Page}?place=Nowhere")));
    }

    [Fact]
    public async Task A_year_of_the_solar_calendar_finds_who_was_there_in_it()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        await At(mina, "مدرسهٔ کابل", "public", "public", ("since", "1377"), ("until", "1381"), ("calendar", "solar-hijri"));

        var page = await sara.Get($"/fa/account/affiliations?place={Uri.EscapeDataString("مدرسهٔ کابل")}&year={Uri.EscapeDataString("۱۳۷۹")}&calendar=solar-hijri");
        var found = Found(page);
        Assert.True(found.Length > 0, page.Main);

        Assert.Contains("Mina", found, StringComparison.Ordinal);
        Assert.Contains("۱۳۷۷", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Mina", Found(await sara.Get($"/fa/account/affiliations?place={Uri.EscapeDataString("مدرسهٔ کابل")}&year=۱۳۹۰&calendar=solar-hijri")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "1998", "2002", "")]
    [InlineData("School of Kabul", "2002", "1998", "")]
    [InlineData("School of Kabul", "soon", "", "")]
    [InlineData("School of Kabul", "1998", "2002", "1990")]
    public async Task A_place_that_says_something_impossible_is_not_kept_and_the_page_says_why(string place, string since, string until, string graduated)
    {
        var mina = await Signed("Mina");

        var answer = await Send(mina, "add", ("place", place), ("since", since), ("until", until), ("graduated", graduated));

        Assert.Equal($"{Page}?form=add&problem=affiliation#add", answer.Response.Headers.Location?.OriginalString);
        var page = await mina.Get($"{Page}?form=add&problem=affiliation");
        Assert.Contains("nobody graduates before they begin", page.Main, StringComparison.Ordinal);
        Assert.Contains("You have stated no place yet.", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_place_is_changed_and_removed_by_its_account_and_by_nobody_else()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var number = await At(mina, "School of Kabul", "public", "public", ("since", "1998"));

        Assert.Equal("/refused", (await Send(sara, "remove", ("affiliation", number))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "show", ("affiliation", number), ("findable", "public"), ("dates", "public"))).Response.Headers.Location?.OriginalString);
        Assert.Equal($"{Page}?form=mine&problem=affiliation#mine", (await Send(sara, "change", ("affiliation", number), ("place", "Somewhere"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await _site.Visitor().Post($"{Forms}/add", Page, ("place", "Somewhere"), ("returnUrl", Page))).Response.Headers.Location?.OriginalString);

        Assert.Equal($"{Page}?said=changed#mine", (await Send(mina, "change", ("affiliation", number), ("place", "Kabul Books"), ("role", "typesetter"), ("since", "2003"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Mina – typesetter – since 2003", Found(await sara.Get($"{Page}?place=kabul+books")), StringComparison.Ordinal);
        Assert.Matches("name=\"place\" [^>]*value=\"Kabul Books\"", (await mina.Get(Page)).Html);

        Assert.Equal($"{Page}?said=removed#mine", (await Send(mina, "remove", ("affiliation", number))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Nobody you may find.", Found(await sara.Get($"{Page}?place=kabul+books")), StringComparison.Ordinal);
    }
}
