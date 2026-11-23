using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Data.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The page of an account with its head (D-086, ADR 0024): the picture where the viewer may see that
/// photograph, the words the account wrote about itself where they are for the viewer, and the first places
/// it lets the viewer see – and the form with which the account, and nobody else, says them. Every test has a
/// site of its own.
/// </summary>
public sealed class AccountProfilePagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Photos = "/en/account/photos";
    private const string People = "/en/account/people";
    private readonly PhotosSite _site = new();

    public void Dispose() => _site.Dispose();

    private async Task<Browser> Signed(string name)
    {
        var visitor = _site.Visitor();
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("said=welcome", made.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return visitor;
    }

    private static async Task<string> Me(Browser visitor) =>
        Regex.Match((await visitor.Get(People)).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;

    private static async Task<string> Bring(Browser owner, string scope, string caption)
    {
        static string[] Ids(Page page) => [.. Regex.Matches(page.Html, "<img src=\"/photo/([a-f0-9]{32})\"").Select(match => match.Groups[1].Value).Distinct()];
        var known = Ids(await owner.Get(Photos));
        var added = await owner.Upload("/preferences/photos/add", Photos, "IMG_0001.png", TestFiles.Png(), ("caption", caption), ("taken", "1998-07-04"), ("scope", scope), ("confirmed", "on"), ("returnUrl", Photos));
        Assert.Equal($"{Photos}?said=added#mine", added.Response.Headers.Location?.OriginalString);
        return Ids(await owner.Get(Photos)).Except(known).Single();
    }

    private static Task<Page> Describe(Browser visitor, string id, params (string, string)[] fields) =>
        visitor.Post("/preferences/people/describe", $"/en/people/{id}", [.. fields, ("returnUrl", $"/en/people/{id}")]);

    // The head of the page: from the opening of the header to its end.
    private static string Head(Page page)
    {
        var html = WebUtility.HtmlDecode(page.Html);
        var from = html.IndexOf("<header class=\"page-header identity is-account\"", StringComparison.Ordinal);
        return from < 0 ? string.Empty : html[from..(html.IndexOf("</header>", from, StringComparison.Ordinal) + 9)];
    }

    [Theory]
    [InlineData("en", "An account on Dastan", "What your page says", "Change what your page says")]
    [InlineData("fa", "یک حساب در داستان", "آنچه صفحهٔ شما می‌گوید", "تغییر آنچه صفحهٔ شما می‌گوید")]
    public async Task The_page_of_an_account_has_a_head_and_its_own_account_the_form_for_it(string language, string mark, string title, string link)
    {
        var mina = await Signed("Mina");
        var id = await Me(mina);
        var page = await mina.Get($"/{language}/people/{id}");
        var head = Head(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(mark, head, StringComparison.Ordinal);
        Assert.Contains("<h1 dir=\"auto\">Mina</h1>", head, StringComparison.Ordinal);
        Assert.Contains("class=\"kind-drawing\"", head, StringComparison.Ordinal);
        Assert.Contains(link, head, StringComparison.Ordinal);
        Assert.Contains($"<h2 id=\"describe-title\">{title}</h2>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains("name=\"about\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"picture\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);

        // Nobody else gets the form, and the head of an account that says nothing is the drawing and the name.
        var stranger = await _site.Visitor().Get($"/{language}/people/{id}");
        Assert.DoesNotContain("id=\"describe\"", stranger.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(link, WebUtility.HtmlDecode(stranger.Html), StringComparison.Ordinal);
        Assert.Contains("<h1 dir=\"auto\">Mina</h1>", Head(stranger), StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"lead person-about\"", Head(stranger), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_words_of_an_account_are_read_by_who_it_chose_and_keep_their_lines()
    {
        var mina = await Signed("Mina");
        var id = await Me(mina);

        var sent = await Describe(mina, id, ("about", "I collect old maps.\nAnd <b>stories</b>."), ("shown", "public"), ("picture", ""));
        Assert.Equal($"/en/people/{id}?said=described", sent.Response.Headers.Location?.OriginalString);

        var own = await mina.Get($"/en/people/{id}?said=described");
        Assert.Contains("Kept. Your page says it to who you chose.", own.Main, StringComparison.Ordinal);
        Assert.Contains("<p class=\"lead person-about\" dir=\"auto\">I collect old maps.\nAnd <b>stories</b>.</p>", Head(own), StringComparison.Ordinal);
        Assert.DoesNotContain("<b>stories</b>", own.Html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"public\" selected", own.Html, StringComparison.Ordinal);

        Assert.Contains("I collect old maps.", Head(await _site.Visitor().Get($"/en/people/{id}")), StringComparison.Ordinal);

        // For the account alone: a stranger's page is the page of an account that says nothing.
        await Describe(mina, id, ("about", "Only for me."), ("shown", "private"), ("picture", ""));
        Assert.Contains("Only for me.", Head(await mina.Get($"/en/people/{id}")), StringComparison.Ordinal);
        var stranger = await _site.Visitor().Get($"/en/people/{id}");
        Assert.DoesNotContain("Only for me.", stranger.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("person-about", stranger.Html, StringComparison.Ordinal);

        // Looking at one's own page as the public shows what the public is shown.
        Assert.DoesNotContain("Only for me.", Head(await mina.Get($"/en/people/{id}?as=public")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_picture_of_an_account_is_one_of_its_photographs_and_is_seen_by_who_may_see_it()
    {
        var mina = await Signed("Mina");
        var id = await Me(mina);
        var open = await Bring(mina, "public", "Mina at the sea");
        var hidden = await Bring(mina, "private", "Mina at home");

        Assert.Equal($"/en/people/{id}?said=described", (await Describe(mina, id, ("about", ""), ("shown", "private"), ("picture", open))).Response.Headers.Location?.OriginalString);
        Assert.Contains($"src=\"/photo/{open}\"", Head(await mina.Get($"/en/people/{id}")), StringComparison.Ordinal);
        Assert.Contains($"src=\"/photo/{open}\"", Head(await _site.Visitor().Get($"/en/people/{id}")), StringComparison.Ordinal);
        Assert.Contains($"<option value=\"{open}\" selected", (await mina.Get($"/en/people/{id}")).Html, StringComparison.Ordinal);

        // A photograph only the account sees is its picture for the account, and a drawing for everybody else.
        await Describe(mina, id, ("about", ""), ("shown", "private"), ("picture", hidden));
        Assert.Contains($"src=\"/photo/{hidden}\"", Head(await mina.Get($"/en/people/{id}")), StringComparison.Ordinal);
        var stranger = Head(await _site.Visitor().Get($"/en/people/{id}"));
        Assert.DoesNotContain("/photo/", stranger, StringComparison.Ordinal);
        Assert.Contains("class=\"kind-drawing\"", stranger, StringComparison.Ordinal);

        // A photograph of somebody else is no picture of this account.
        var sara = await Signed("Sara");
        var saras = await Bring(sara, "public", "Sara");
        Assert.Equal($"/en/people/{id}?form=describe&problem=picture#describe", (await Describe(mina, id, ("about", ""), ("shown", "private"), ("picture", saras))).Response.Headers.Location?.OriginalString);
        Assert.Contains("That photograph is not one of yours.", (await mina.Get($"/en/people/{id}?form=describe&problem=picture")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_is_no_description_is_refused_and_only_the_account_describes_itself()
    {
        var mina = await Signed("Mina");
        var id = await Me(mina);

        var tooLong = await Describe(mina, id, ("about", new string('x', 301)), ("shown", "public"), ("picture", ""));
        Assert.Equal($"/en/people/{id}?form=describe&problem=long#describe", tooLong.Response.Headers.Location?.OriginalString);
        Assert.Contains("The words are too long: at most 300 characters.", (await mina.Get($"/en/people/{id}?form=describe&problem=long")).Main, StringComparison.Ordinal);
        Assert.Equal("/refused", (await Describe(mina, id, ("about", "Words."), ("shown", "everybody-and-more"), ("picture", ""))).Response.Headers.Location?.OriginalString);

        // The form of somebody else's page is sent for the sender: it describes the sender, never the other account.
        var sara = await Signed("Sara");
        var saraId = await Me(sara);
        var sent = await sara.Post("/preferences/people/describe", $"/en/people/{saraId}", ("about", "Written by Sara."), ("shown", "public"), ("picture", ""), ("returnUrl", $"/en/people/{id}"));
        Assert.Equal($"/people/{saraId}?said=described", sent.Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Written by Sara.", (await _site.Visitor().Get($"/en/people/{id}")).Html, StringComparison.Ordinal);
        Assert.Contains("Written by Sara.", Head(await _site.Visitor().Get($"/en/people/{saraId}")), StringComparison.Ordinal);

        // Who is not signed in describes nobody.
        Assert.Equal("/refused", (await _site.Visitor().Post("/preferences/people/describe", "/en", ("about", "Words."), ("shown", "public"), ("returnUrl", $"/en/people/{id}"))).Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task The_head_names_the_first_places_the_viewer_may_see_and_leads_to_the_rest()
    {
        var mina = await Signed("Mina");
        var id = await Me(mina);
        foreach (var place in new[] { "Alpha School", "Beta College", "Gamma Works", "Delta Club" })
        {
            await mina.Post("/preferences/affiliations/add", "/en/account/affiliations", ("place", place), ("institution", "none"), ("since", "1998"), ("returnUrl", "/en/account/affiliations"));
        }

        var numbers = Regex.Matches((await mina.Get("/en/account/affiliations")).Html, "<li id=\"affiliation-(\\d+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(4, numbers.Count);
        foreach (var number in numbers)
        {
            await mina.Post("/preferences/affiliations/show", "/en/account/affiliations", ("affiliation", number), ("findable", "public"), ("dates", "private"), ("returnUrl", "/en/account/affiliations"));
        }

        var head = Head(await _site.Visitor().Get($"/en/people/{id}"));

        Assert.Contains("<dt>Was at</dt>", head, StringComparison.Ordinal);
        Assert.Equal(3, new[] { "Alpha School", "Beta College", "Delta Club", "Gamma Works" }.Count(place => head.Contains(place, StringComparison.Ordinal)));
        Assert.Contains($"href=\"people/{id}#person-affiliations\">More places below</a>", head, StringComparison.Ordinal);
        Assert.DoesNotContain("1998", head, StringComparison.Ordinal);

        // An account that shows no place has no such line.
        var sara = await Signed("Sara");
        Assert.DoesNotContain("Was at", Head(await _site.Visitor().Get($"/en/people/{await Me(sara)}")), StringComparison.Ordinal);
    }
}
