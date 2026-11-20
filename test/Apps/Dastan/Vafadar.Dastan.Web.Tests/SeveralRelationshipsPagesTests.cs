using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Relationships as records of their own, on the site (step 19.6, ADR 0018): two visitors show several side by
/// side, each with where and when it holds and – for a kind with a direction – who holds the role; kinds of
/// one group exclude each other; and a relationship that is to be called something else stays as it is until
/// the other side agrees. Every test has a site of its own.
/// </summary>
public sealed class SeveralRelationshipsPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string People = "/en/account/people";
    private const string Forms = "/preferences/people";
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
        visitor.Post($"{Forms}/{form}", People, [.. fields, ("returnUrl", People)]);

    private static async Task<string> Me(Browser visitor) =>
        Regex.Match((await visitor.Get(People)).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;

    // The request that waits for the answer of a visitor.
    private static async Task<string> Waiting(Browser visitor)
    {
        var html = (await visitor.Get(People)).Html;
        var waiting = html[html.IndexOf("id=\"waiting\"", StringComparison.Ordinal)..];
        return Regex.Match(waiting, "name=\"relationship\" value=\"([a-f0-9]{32})\"").Groups[1].Value;
    }

    // One asks, the other accepts; returns the identifier of the relationship.
    private static async Task<string> Shown(Browser from, Browser to, string toName, params (string, string)[] fields)
    {
        Assert.Equal($"{People}?said=asked", (await Send(from, "ask", [("name", toName), .. fields])).Response.Headers.Location?.OriginalString);
        var id = await Waiting(to);
        Assert.Equal($"{People}?said=accepted", (await Send(to, "answer", ("relationship", id), ("answer", "accept"))).Response.Headers.Location?.OriginalString);
        return id;
    }

    private static string Mine(Page page)
    {
        var from = page.Html.IndexOf("<section id=\"mine\"", StringComparison.Ordinal);
        return Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(page.Html[from..page.Html.IndexOf("</section>", from, StringComparison.Ordinal)], "<details.*?</details>|<form.*?</form>", string.Empty, RegexOptions.Singleline)), "(<[^>]+>|\\s)+", " ");
    }

    [Fact]
    public async Task A_friend_is_a_colleague_too_and_each_says_where_and_when()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        await Shown(mina, sara, "Sara", ("kind", "friend"));
        await Shown(sara, mina, "Mina", ("kind", "colleague"), ("place", "The Zabol Press"), ("since", "2010"), ("until", "2018"));
        await Shown(mina, sara, "Sara", ("kind", "classmate"), ("place", "مدرسهٔ کابل"), ("since", "1377"), ("calendar", "solar-hijri"));

        var mine = Regex.Replace(Mine(await mina.Get(People)), "\\s+", " ");

        Assert.Contains("Sara – friend", mine, StringComparison.Ordinal);
        Assert.Contains("Sara – colleague, The Zabol Press, 2010 to 2018", mine, StringComparison.Ordinal);
        Assert.Contains("Sara – classmate, مدرسهٔ کابل, since 1377 SH", mine, StringComparison.Ordinal);

        // The form to call one something else shows it as it is.
        var html = (await mina.Get(People)).Html;
        Assert.Matches("name=\"place\" [^>]*value=\"The Zabol Press\"", html);
        Assert.Matches("name=\"since\" [^>]*value=\"2010\"", html);
        Assert.Contains("<option value=\"colleague\" selected>colleague</option>", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"solar-hijri\" selected>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "guardian", "ward", "since 2010")]
    [InlineData("fa", "سرپرست", "تحت سرپرستی", "از ۲۰۱۰")]
    public async Task A_kind_with_a_direction_reads_from_each_side_as_what_the_other_is(string language, string role, string inverse, string since)
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");

        // Mina says: Sara is my guardian. The same form offers the other way round: "ward".
        var form = WebUtility.HtmlDecode((await mina.Get($"/{language}/account/people")).Html);
        Assert.Contains($"<option value=\"guardian\">{role}</option>", form, StringComparison.Ordinal);
        Assert.Contains($"<option value=\"guardian-mine\">{inverse}</option>", form, StringComparison.Ordinal);
        var id = await Shown(mina, sara, "Sara", ("kind", "guardian"), ("since", "2010"));
        await Send(mina, "show", ("relationship", id), ("scope", "public"));
        await Send(sara, "show", ("relationship", id), ("scope", "public"));

        Assert.Contains($"Sara – {role}", Mine(await mina.Get($"/{language}/account/people")), StringComparison.Ordinal);
        Assert.Contains($"Mina – {inverse}", Mine(await sara.Get($"/{language}/account/people")), StringComparison.Ordinal);

        // And so does everybody else, on the page about each of the two.
        var stranger = _site.Visitor();
        var aboutMina = Regex.Replace((await stranger.Get($"/{language}/people/{await Me(mina)}")).Text, "\\s+", " ");
        var aboutSara = Regex.Replace((await stranger.Get($"/{language}/people/{await Me(sara)}")).Text, "\\s+", " ");
        Assert.Contains($"Sara – {role}", aboutMina, StringComparison.Ordinal);
        Assert.Contains(since, aboutMina, StringComparison.Ordinal);
        Assert.Contains($"Mina – {inverse}", aboutSara, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("close-friend", "", "", "excluded", "one role in a family and one step of friendship")]
    [InlineData("friend", "", "", "exists", "You two have this relationship, or a request for it, already.")]
    [InlineData("colleague", "", "soon", "terms", "a year is a number from 1 to 9999")]
    [InlineData("colleague", "", "2020", "terms", "\"until\" is not before \"since\"")]
    public async Task What_cannot_stand_beside_what_the_two_show_says_why(string kind, string place, string since, string problem, string said)
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        await Shown(mina, sara, "Sara", ("kind", "friend"));

        var answer = await Send(sara, "ask", ("name", "Mina"), ("kind", kind), ("place", place), ("since", since), ("until", since == "2020" ? "2019" : string.Empty));

        Assert.Equal($"{People}?form=ask&problem={problem}#ask", answer.Response.Headers.Location?.OriginalString);
        Assert.Contains(said, (await sara.Get($"{People}?form=ask&problem={problem}")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_relationship_is_called_something_else_only_when_the_other_side_agrees()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var friend = await Shown(mina, sara, "Sara", ("kind", "friend"));
        await Send(mina, "show", ("relationship", friend), ("scope", "public"));
        await Send(sara, "show", ("relationship", friend), ("scope", "public"));
        var about = $"/en/people/{await Me(mina)}";

        // Nothing would change: said beside the list. Then Sara asks for "close friend".
        var same = await Send(sara, "change", ("relationship", friend), ("kind", "friend"));
        Assert.Equal($"{People}?form=mine&problem=same#mine", same.Response.Headers.Location?.OriginalString);
        Assert.Contains("That is what the relationship is called already.", (await sara.Get($"{People}?form=mine&problem=same")).Main, StringComparison.Ordinal);
        var asked = await Send(sara, "change", ("relationship", friend), ("kind", "close-friend"));
        Assert.Equal($"{People}?said=change-asked#mine", asked.Response.Headers.Location?.OriginalString);
        Assert.Contains("stays as it is until they agree", (await sara.Get($"{People}?said=change-asked")).Main, StringComparison.Ordinal);

        // Mina reads what is asked and what it would replace; everybody else still sees "friend".
        var waiting = Regex.Replace((await mina.Get(People)).Text, "\\s+", " ");
        Assert.Contains("Sara – close friend – instead of: friend", waiting, StringComparison.Ordinal);
        Assert.Contains("Sara – friend", Regex.Replace((await _site.Visitor().Get(about)).Text, "\\s+", " "), StringComparison.Ordinal);

        await Send(mina, "answer", ("relationship", await Waiting(mina)), ("answer", "accept"));

        // One relationship, called anew, still open to everybody as both had chosen.
        var seen = Regex.Replace((await _site.Visitor().Get(about)).Text, "\\s+", " ");
        Assert.Contains("Sara – close friend", seen, StringComparison.Ordinal);
        Assert.Single(Regex.Matches((await mina.Get(People)).Html, "action=\"/preferences/people/end\""));

        // Nobody asks to change a relationship that is not theirs.
        var dara = await Signed("Dara");
        var id = Regex.Match((await mina.Get(People)).Html, "action=\"/preferences/people/end\".*?name=\"relationship\" value=\"([a-f0-9]{32})\"", RegexOptions.Singleline).Groups[1].Value;
        Assert.Equal("/refused", (await Send(dara, "change", ("relationship", id), ("kind", "family"))).Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task A_blocked_account_finds_nothing_and_is_told_nothing_and_the_block_is_taken_back_on_the_page()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        await Shown(mina, sara, "Sara", ("kind", "friend"));
        var (minaId, saraId) = (await Me(mina), await Me(sara));

        // The page about Sara offers Mina to block her – and nobody their own page, or somebody not signed in.
        Assert.Contains("Block this account", (await mina.Get($"/en/people/{saraId}")).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Block this account", (await mina.Get($"/en/people/{minaId}")).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Block this account", (await _site.Visitor().Get($"/en/people/{saraId}")).Main, StringComparison.Ordinal);

        var blocked = await Send(mina, "block", ("account", saraId));
        Assert.Equal($"{People}?said=blocked#blocked", blocked.Response.Headers.Location?.OriginalString);
        var page = await mina.Get($"{People}?said=blocked");
        Assert.Contains("The account is blocked.", page.Main, StringComparison.Ordinal);
        Assert.Contains("Accounts you block", page.Main, StringComparison.Ordinal);
        Assert.Contains("You show no relationship with anybody yet.", page.Main, StringComparison.Ordinal);

        // To Sara the account is not there: its page is not found, and asking says what a wrong name says.
        Assert.Equal(HttpStatusCode.NotFound, (await sara.Get($"/en/people/{minaId}")).Status);
        Assert.Equal($"{People}?form=ask&problem=unknown#ask", (await Send(sara, "ask", ("name", "Mina"), ("kind", "friend"))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("block", (await sara.Get(People)).Main, StringComparison.OrdinalIgnoreCase);

        // Mina does not ask Sara either while she blocks her, and the page says what to do.
        Assert.Equal($"{People}?form=ask&problem=blocked#ask", (await Send(mina, "ask", ("name", "Sara"), ("kind", "friend"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("Take the block back before you ask.", (await mina.Get($"{People}?form=ask&problem=blocked")).Main, StringComparison.Ordinal);
        Assert.Contains("You block this account", (await mina.Get($"/en/people/{saraId}")).Main, StringComparison.Ordinal);

        Assert.Equal($"{People}?said=unblocked#ask", (await Send(mina, "unblock", ("account", saraId))).Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await sara.Get($"/en/people/{minaId}")).Status);
        Assert.Equal("/refused", (await Send(mina, "unblock", ("account", saraId))).Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Ending_one_relationship_leaves_the_others()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var friend = await Shown(mina, sara, "Sara", ("kind", "friend"));
        await Shown(mina, sara, "Sara", ("kind", "teammate"), ("place", "Zabol Riders"));

        Assert.Equal($"{People}?said=ended", (await Send(sara, "end", ("relationship", friend))).Response.Headers.Location?.OriginalString);

        var mine = Mine(await mina.Get(People));
        Assert.DoesNotContain("Sara – friend", mine, StringComparison.Ordinal);
        Assert.Contains("Sara – teammate, Zabol Riders", mine, StringComparison.Ordinal);
    }
}
