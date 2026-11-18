using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The relationships two visitors show together, on the site (step 16.3, PRIV-005 to PRIV-009, PRIV-012):
/// asked, accepted by the other side only, shown to who both allow, ended by either; and circles. Every test
/// has a site of its own.
/// </summary>
public sealed class PeoplePagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string People = "/en/account/people";
    private const string Forms = "/preferences/people";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    // A visitor with an account of that name, signed in.
    private async Task<Browser> Signed(string name)
    {
        var visitor = _site.Visitor();
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("said=welcome", made.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return visitor;
    }

    private static Task<Page> Send(Browser visitor, string form, params (string, string)[] fields) =>
        visitor.Post($"{Forms}/{form}", People, [.. fields, ("returnUrl", People)]);

    private static async Task<string> Me(Browser visitor) =>
        Regex.Match((await visitor.Get(People)).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;

    private static async Task<string> Relationship(Browser visitor) =>
        Regex.Match((await visitor.Get(People)).Html, "name=\"relationship\" value=\"([a-f0-9]{32})\"").Groups[1].Value;

    // Mina asks Sara, Sara accepts; returns the identifier of the relationship.
    private static async Task<string> Friends(Browser mina, Browser sara, string saraName = "Sara")
    {
        Assert.Equal($"{People}?said=asked", (await Send(mina, "ask", ("name", saraName), ("kind", "friend"))).Response.Headers.Location?.OriginalString);
        var id = await Relationship(sara);
        Assert.Equal($"{People}?said=accepted", (await Send(sara, "answer", ("relationship", id), ("answer", "accept"))).Response.Headers.Location?.OriginalString);
        return id;
    }

    [Fact]
    public async Task The_page_is_for_who_signed_in_and_a_site_without_accounts_has_none()
    {
        var sent = await _site.Visitor().Get(People);
        Assert.Equal(HttpStatusCode.Redirect, sent.Status);
        Assert.EndsWith(Account, sent.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        using var site = new DemoSite();
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get(People)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get("/en/people/0123456789abcdef0123456789abcdef")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Post($"{Forms}/ask", "/en/", ("name", "Sara"), ("kind", "friend"))).Status);
    }

    [Theory]
    [InlineData("en", "People", "Ask somebody", "nobody can accept for somebody else", "close friend")]
    [InlineData("fa", "آشنایان", "درخواست از کسی", "هیچ‌کس نمی‌تواند به جای دیگری بپذیرد", "دوست نزدیک")]
    public async Task The_page_says_in_the_language_of_the_page_what_a_relationship_is(string language, string title, string ask, string rule, string kind)
    {
        var mina = await Signed("Mina");

        var page = await mina.Get($"/{language}/account/people");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"<h1>{title}</h1>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);
        Assert.Contains(ask, page.Main, StringComparison.Ordinal);
        Assert.Contains(rule, page.Main, StringComparison.Ordinal);
        Assert.Contains(kind, page.Main, StringComparison.Ordinal);
        Assert.Contains($"name=\"returnUrl\" value=\"/{language}/account/people\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Who_is_asked_finds_the_request_in_the_header_and_answers_it()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");

        await Send(mina, "ask", ("name", "sara"), ("kind", "close-friend"));

        Assert.Contains("Your request is made.", (await mina.Get($"{People}?said=asked")).Main, StringComparison.Ordinal);
        Assert.Contains("You asked", (await mina.Get(People)).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("waiting</a>", (await mina.Get("/en/")).Html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"account/people#waiting\">1 waiting</a>", (await sara.Get("/en/")).Html, StringComparison.Ordinal);
        var asked = await sara.Get(People);
        Assert.Contains("Waiting for your answer", asked.Main, StringComparison.Ordinal);
        Assert.Matches("Mina</a>\\s*<span class=\"meta\">– close friend", asked.Main);

        // Until it is accepted nobody is shown anything, not even the two.
        Assert.Contains("Nothing is shown to you here.", (await sara.Get($"/en/people/{await Me(mina)}")).Main, StringComparison.Ordinal);

        await Send(sara, "answer", ("relationship", await Relationship(sara)), ("answer", "accept"));

        Assert.DoesNotContain("waiting</a>", (await sara.Get("/en/")).Html, StringComparison.Ordinal);
        var mine = await mina.Get(People);
        Assert.DoesNotContain("You asked", mine.Main, StringComparison.Ordinal);
        Assert.Matches("Sara</a>\\s*<span class=\"meta\">– close friend", mine.Main);
        Assert.Contains("Who else may see it", mine.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_relationship_is_shown_to_the_two_only_until_both_open_it()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        var id = await Friends(mina, sara);
        var page = $"/en/people/{await Me(mina)}";

        Assert.Matches("Sara</a>\\s*<span class=\"meta\">– friend", (await mina.Get(page)).Main);
        Assert.Matches("Sara</a>\\s*<span class=\"meta\">– friend", (await sara.Get(page)).Main);
        Assert.Contains("Nothing is shown to you here.", (await dara.Get(page)).Main, StringComparison.Ordinal);
        Assert.Contains("Nothing is shown to you here.", (await _site.Visitor().Get(page)).Main, StringComparison.Ordinal);

        // One side opens it: the restriction of the other still holds.
        Assert.Equal($"{People}?said=shown", (await Send(mina, "show", ("relationship", id), ("scope", "public"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("value=\"public\" selected", (await mina.Get(People)).Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", (await dara.Get(page)).Main, StringComparison.Ordinal);

        await Send(sara, "show", ("relationship", id), ("scope", "public"));
        Assert.Matches("Sara</a>\\s*<span class=\"meta\">– friend", (await dara.Get(page)).Main);
        var open = await _site.Visitor().Get(page);
        Assert.Matches("Sara</a>\\s*<span class=\"meta\">– friend", open.Main);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\"", open.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("This is your own page.", open.Main, StringComparison.Ordinal);
        Assert.Contains("This is your own page.", (await mina.Get(page)).Main, StringComparison.Ordinal);

        // Taken back by one side, it is hidden at once.
        await Send(sara, "show", ("relationship", id), ("scope", "private"));
        Assert.DoesNotContain("Sara", (await _site.Visitor().Get(page)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_circle_shows_a_relationship_to_the_people_in_it_only()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        var zal = await Signed("Zal");
        var id = await Friends(mina, sara);
        await Send(sara, "show", ("relationship", id), ("scope", "public"));
        await Send(mina, "ask", ("name", "Dara"), ("kind", "colleague"));
        await Send(dara, "answer", ("relationship", await Relationship(dara)), ("answer", "accept"));

        var made = await Send(mina, "circle-add", ("name", "Trusted"));
        Assert.Equal($"{People}?said=circle#circles", made.Response.Headers.Location?.OriginalString);
        var circle = Regex.Match((await mina.Get(People)).Html, "name=\"circle\" value=\"(\\d+)\"").Groups[1].Value;

        // Zal has no relationship with Mina and does not come into her circle.
        var set = await Send(mina, "circle-set", ("circle", circle), ("member", await Me(dara)), ("member", await Me(zal)));
        Assert.Equal($"{People}?said=circle-set#circles", set.Response.Headers.Location?.OriginalString);
        var listed = (await mina.Get(People)).Html;
        Assert.Matches($"value=\"{await Me(dara)}\" checked", listed);
        Assert.DoesNotContain(await Me(zal), listed, StringComparison.Ordinal);

        await Send(mina, "show", ("relationship", id), ("scope", $"circle-{circle}"));

        var page = $"/en/people/{await Me(mina)}";
        Assert.Contains("Circle: Trusted", (await mina.Get(People)).Main, StringComparison.Ordinal);
        Assert.Matches("Sara</a>\\s*<span class=\"meta\">– friend", (await dara.Get(page)).Main);
        Assert.DoesNotContain("Sara", (await zal.Get(page)).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", (await _site.Visitor().Get(page)).Main, StringComparison.Ordinal);

        // The name of a circle is its owner's alone.
        Assert.DoesNotContain("Trusted", (await dara.Get(People)).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Trusted", (await dara.Get(page)).Main, StringComparison.Ordinal);

        // Removed, what was shown to it is shown to the two only.
        Assert.Equal($"{People}?said=circle-removed#circles", (await Send(mina, "circle-remove", ("circle", circle))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Sara", (await dara.Get(page)).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Nobody", "friend", "unknown", "No account has this name.")]
    [InlineData("Mina", "friend", "self", "That is your own account.")]
    [InlineData("Sara", "enemy", "kind", "Please choose what you are to each other.")]
    public async Task What_cannot_be_asked_says_why(string name, string kind, string problem, string said)
    {
        var mina = await Signed("Mina");
        await Signed("Sara");

        var answer = await Send(mina, "ask", ("name", name), ("kind", kind));

        Assert.Equal($"{People}?form=ask&problem={problem}#ask", answer.Response.Headers.Location?.OriginalString);
        var page = await mina.Get($"{People}?form=ask&problem={problem}");
        Assert.Contains(said, page.Main, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("You show no relationship with anybody yet.", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nobody_acts_on_a_relationship_for_somebody_else()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        await Send(mina, "ask", ("name", "Sara"), ("kind", "friend"));
        var id = await Relationship(sara);

        // The one who asked does not accept; a third account does nothing at all.
        Assert.Equal("/refused", (await Send(mina, "answer", ("relationship", id), ("answer", "accept"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(dara, "answer", ("relationship", id), ("answer", "accept"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(dara, "cancel", ("relationship", id))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, "answer", ("relationship", id), ("answer", "perhaps"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Waiting for your answer", (await sara.Get(People)).Main, StringComparison.Ordinal);

        await Send(sara, "answer", ("relationship", id), ("answer", "accept"));
        await Send(sara, "circle-add", ("name", "Family"));
        var saras = Regex.Match((await sara.Get(People)).Html, "name=\"circle\" value=\"(\\d+)\"").Groups[1].Value;

        Assert.Equal("/refused", (await Send(dara, "show", ("relationship", id), ("scope", "public"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(dara, "end", ("relationship", id))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, "show", ("relationship", id), ("scope", $"circle-{saras}"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, "show", ("relationship", id), ("scope", "everybody"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, "circle-set", ("circle", saras), ("member", await Me(sara)))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, "circle-remove", ("circle", saras))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Nothing is shown to you here.", (await dara.Get($"/en/people/{await Me(mina)}")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_request_is_declined_or_taken_back_and_a_relationship_is_ended_by_either_side()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");

        // Taken back by who asked.
        await Send(mina, "ask", ("name", "Sara"), ("kind", "friend"));
        Assert.Equal($"{People}?said=cancelled", (await Send(mina, "cancel", ("relationship", await Relationship(mina)))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Waiting for your answer", (await sara.Get(People)).Main, StringComparison.Ordinal);

        // Accepted, then ended by the side that was asked.
        var id = await Friends(mina, sara);
        await Send(mina, "show", ("relationship", id), ("scope", "public"));
        await Send(sara, "show", ("relationship", id), ("scope", "public"));
        Assert.Equal($"{People}?said=ended", (await Send(sara, "end", ("relationship", id))).Response.Headers.Location?.OriginalString);
        Assert.Contains("It is shown to nobody any more.", (await sara.Get($"{People}?said=ended")).Main, StringComparison.Ordinal);
        Assert.Contains("You show no relationship with anybody yet.", (await mina.Get(People)).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", (await _site.Visitor().Get($"/en/people/{await Me(mina)}")).Main, StringComparison.Ordinal);

        // Declined: nothing is shown, and the same question is not asked again at once.
        await Send(mina, "ask", ("name", "Sara"), ("kind", "family"));
        Assert.Equal($"{People}?said=declined", (await Send(sara, "answer", ("relationship", await Relationship(sara)), ("answer", "decline"))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("You asked", (await mina.Get(People)).Main, StringComparison.Ordinal);
        var again = await Send(mina, "ask", ("name", "Sara"), ("kind", "family"));
        Assert.Equal($"{People}?form=ask&problem=declined-lately#ask", again.Response.Headers.Location?.OriginalString);
        Assert.Contains("You can ask again after 30 days.", (await mina.Get($"{People}?form=ask&problem=declined-lately")).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a name that is far too long to be the name of a circle")]
    public async Task A_circle_without_a_plain_name_is_not_made(string name)
    {
        var mina = await Signed("Mina");

        var answer = await Send(mina, "circle-add", ("name", name));

        Assert.Equal($"{People}?form=circles&problem=circle-name#circles", answer.Response.Headers.Location?.OriginalString);
        Assert.Contains("A circle needs a name", (await mina.Get($"{People}?form=circles&problem=circle-name")).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ask")]
    [InlineData("answer")]
    [InlineData("cancel")]
    [InlineData("end")]
    [InlineData("show")]
    [InlineData("circle-add")]
    [InlineData("circle-set")]
    [InlineData("circle-remove")]
    public async Task Every_form_is_refused_to_who_is_not_signed_in(string form)
    {
        var answer = await _site.Visitor().Post($"{Forms}/{form}", Account, ("name", "Sara"), ("kind", "friend"), ("relationship", "x"), ("answer", "accept"), ("scope", "public"), ("circle", "1"), ("returnUrl", People));

        Assert.Equal("/refused", answer.Response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("https://elsewhere.example/account/people")]
    [InlineData("/en/story-rostam-sohrab")]
    public async Task A_form_leads_back_to_the_page_of_the_relationships_and_nowhere_else(string returnUrl)
    {
        var mina = await Signed("Mina");

        var answer = await mina.Post($"{Forms}/ask", People, ("name", "Nobody"), ("kind", "friend"), ("returnUrl", returnUrl));

        Assert.Equal("/account/people?form=ask&problem=unknown#ask", answer.Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task The_page_about_an_account_that_does_not_exist_or_was_closed_is_not_found()
    {
        var mina = await Signed("Mina");
        var id = await Me(mina);
        Assert.Equal(HttpStatusCode.OK, (await _site.Visitor().Get($"/en/people/{id}")).Status);

        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get("/en/people/0123456789abcdef0123456789abcdef")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get($"/en/people/{new string('a', 60)}")).Status);

        await mina.Post("/preferences/account/close", Account, ("current", Password), ("returnUrl", Account));
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get($"/en/people/{id}")).Status);
    }

    [Fact]
    public async Task What_an_address_says_is_not_written_into_the_page()
    {
        var mina = await Signed("Mina");

        var page = await mina.Get($"{People}?said=<b>hello</b>&form=ask&problem=<i>nonsense</i>");

        Assert.DoesNotContain("hello", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("nonsense", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"alert\"", page.Html, StringComparison.Ordinal);
    }
}
