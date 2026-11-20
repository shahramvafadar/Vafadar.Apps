using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Seeing as somebody else, on the site (step 19.6, ADR 0018, PRIV-015), and the kinds of the links of a tree
/// (R-PER-02): an account looks at its own page and its own tree as the public, a circle or a named account
/// sees them; nobody looks at somebody else's that way; and a tree tells a guardian from a parent. Every test
/// has a site of its own.
/// </summary>
public sealed class SeenAsPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string People = "/en/account/people";
    private const string Trees = "/en/account/trees";
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
        visitor.Post($"/preferences/people/{form}", People, [.. fields, ("returnUrl", People)]);

    private static async Task<string> Me(Browser visitor) =>
        Regex.Match((await visitor.Get(People)).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;

    // One asks, the other accepts; returns the identifier of the relationship.
    private static async Task<string> Shown(Browser from, Browser to, string toName, string kind)
    {
        await Send(from, "ask", ("name", toName), ("kind", kind));
        var html = (await to.Get(People)).Html;
        var id = Regex.Match(html[html.IndexOf("id=\"waiting\"", StringComparison.Ordinal)..], "name=\"relationship\" value=\"([a-f0-9]{32})\"").Groups[1].Value;
        await Send(to, "answer", ("relationship", id), ("answer", "accept"));
        return id;
    }

    private static string Words(Page page) => Regex.Replace(page.Text, "\\s+", " ");

    [Fact]
    public async Task Somebody_sees_their_own_page_as_everybody_a_circle_and_a_named_account_see_it()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        var friend = await Shown(mina, sara, "Sara", "friend");
        var colleague = await Shown(mina, dara, "Dara", "colleague");
        await Send(mina, "circle-add", ("name", "Close"));
        var circle = Regex.Match((await mina.Get(People)).Html, "name=\"circle\" value=\"(\\d+)\"").Groups[1].Value;
        await Send(mina, "circle-set", ("circle", circle), ("member", await Me(sara)));
        await Send(mina, "show", ("relationship", friend), ("scope", "public"));
        await Send(sara, "show", ("relationship", friend), ("scope", "public"));
        await Send(mina, "show", ("relationship", colleague), ("scope", $"circle-{circle}"));
        var mine = $"/en/people/{await Me(mina)}";

        // The own page offers the look: everybody, the circle, and the two accounts – each once.
        var own = await mina.Get(mine);
        Assert.Contains("See this page as somebody else sees it", own.Main, StringComparison.Ordinal);
        Assert.Contains($"<option value=\"circle-{circle}\">Circle: Close</option>", own.Html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(own.Html, "<option value=\"account-[a-f0-9]{32}\">Sara</option>"));

        var everybody = Words(await mina.Get($"{mine}?as=public"));
        Assert.Contains("You are looking at your page as everybody sees it.", everybody, StringComparison.Ordinal);
        Assert.Contains("Sara – friend", everybody, StringComparison.Ordinal);
        Assert.DoesNotContain("Dara – colleague", everybody, StringComparison.Ordinal);

        var close = Words(await mina.Get($"{mine}?as=circle-{circle}"));
        Assert.Contains("as the circle ⁨Close⁩ sees it", close, StringComparison.Ordinal);
        Assert.Contains("Dara – colleague", close, StringComparison.Ordinal);
        Assert.Contains("the other side of it can keep it from them", close, StringComparison.Ordinal);

        var asDara = await mina.Get($"{mine}?as=account-{await Me(dara)}");
        Assert.Contains("as ⁨Dara⁩ sees it", Words(asDara), StringComparison.Ordinal);
        Assert.Contains("Back to your own view", asDara.Main, StringComparison.Ordinal);
        Assert.Contains("<option value=\"account-" + await Me(dara) + "\" selected>Dara</option>", asDara.Html, StringComparison.Ordinal);

        // What the real viewers see is what the look showed for them.
        Assert.Contains("Sara – friend", Words(await _site.Visitor().Get(mine)), StringComparison.Ordinal);
        Assert.DoesNotContain("Dara – colleague", Words(await _site.Visitor().Get(mine)), StringComparison.Ordinal);

        // The look as the circle listed what Mina lets it see. Whether Sara really sees the relationship with
        // Dara is Dara's choice too: he keeps it to the two, so she does not – until he opens it.
        Assert.DoesNotContain("Dara – colleague", Words(await sara.Get(mine)), StringComparison.Ordinal);
        await Send(dara, "show", ("relationship", colleague), ("scope", "public"));
        Assert.Contains("Dara – colleague", Words(await sara.Get(mine)), StringComparison.Ordinal);
        Assert.DoesNotContain("Dara – colleague", Words(await _site.Visitor().Get(mine)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nobody_looks_at_the_page_of_somebody_else_as_somebody_else()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        await Shown(mina, sara, "Sara", "friend");
        var mine = $"/en/people/{await Me(mina)}";

        // Sara asks Mina's page with a look: she gets the page she always gets, and no offer to look.
        var plain = await sara.Get(mine);
        var asked = await sara.Get($"{mine}?as=public");
        Assert.Equal(Words(plain), Words(asked));
        Assert.DoesNotContain("See this page as somebody else sees it", asked.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("You are looking", asked.Main, StringComparison.Ordinal);

        // A look as somebody Mina cannot name is her own page, said as such.
        var own = await mina.Get($"{mine}?as=account-{new string('a', 32)}");
        Assert.DoesNotContain("You are looking", own.Main, StringComparison.Ordinal);
        Assert.Contains("This is your own page.", own.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_owner_sees_a_tree_as_a_member_and_as_everybody_and_the_tree_tells_a_guardian_from_a_parent()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        await Shown(mina, sara, "Sara", "family");
        await Shown(mina, dara, "Dara", "friend");
        var made = await mina.Post("/preferences/trees/add", Trees, ("name", "The Rad family"), ("returnUrl", Trees));
        var tree = made.Response.Headers.Location!.OriginalString.Split('?')[0];
        var id = tree[(tree.LastIndexOf('/') + 1)..];
        Task<Page> Tree(string form, params (string, string)[] fields) => mina.Post($"/preferences/trees/{form}", tree, [.. fields, ("tree", id), ("returnUrl", tree)]);
        await Tree("person-add", ("name", "Zal"), ("born", "1870"), ("died", "1950"));
        await Tree("person-add", ("name", "Simorgh"), ("deceased", "yes"));
        await Tree("person-add", ("name", "Sam"), ("deceased", "yes"));
        var people = Regex.Matches((await mina.Get(tree)).Html, "<li id=\"person-([a-f0-9]{32})\"><p><strong><a href=\"[^\"]+\" dir=\"auto\">([^<]+)</a>").ToDictionary(match => match.Groups[2].Value, match => match.Groups[1].Value);

        // A guardian with the years it holds for, a parent by birth – and no second link between the same two.
        Assert.Equal($"{tree}?said=linked#links", (await Tree("link", ("kind", "guardian"), ("from", people["Simorgh"]), ("to", people["Zal"]), ("since", "1870"), ("until", "1886"))).Response.Headers.Location?.OriginalString);
        Assert.Equal($"{tree}?said=linked#links", (await Tree("link", ("kind", "parent"), ("from", people["Sam"]), ("to", people["Zal"]))).Response.Headers.Location?.OriginalString);
        Assert.Equal($"{tree}?form=links&problem=link-exists#links", (await Tree("link", ("kind", "adoptive-parent"), ("from", people["Sam"]), ("to", people["Zal"]))).Response.Headers.Location?.OriginalString);
        Assert.Equal($"{tree}?form=links&problem=link-time#links", (await Tree("link", ("kind", "partner"), ("from", people["Sam"]), ("to", people["Simorgh"]), ("since", "1860"))).Response.Headers.Location?.OriginalString);
        var page = Words(await mina.Get(tree));
        Assert.Contains("⁨Simorgh⁩ is the guardian of ⁨Zal⁩, 1870 to 1886", page, StringComparison.Ordinal);
        Assert.Contains("⁨Sam⁩ is a parent of ⁨Zal⁩", page, StringComparison.Ordinal);
        Assert.Contains("Only a guardianship says from when until when", (await mina.Get($"{tree}?form=links&problem=link-time")).Main, StringComparison.Ordinal);

        await Tree("member", ("account", await Me(sara)), ("role", "viewer"));

        // As Sara: the people, and nothing to do. As Dara and as everybody: nothing, and the page says why.
        var asSara = await mina.Get($"{tree}?as=account-{await Me(sara)}");
        Assert.Contains("as ⁨Sara⁩ sees it", Words(asSara), StringComparison.Ordinal);
        Assert.Contains("Zal", asSara.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("person-add", asSara.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Every change", asSara.Main, StringComparison.Ordinal);
        Assert.Contains("the tree is not shared with it", (await mina.Get($"{tree}?as=account-{await Me(dara)}")).Main, StringComparison.Ordinal);
        Assert.Contains("the tree is not public", (await mina.Get($"{tree}?as=public")).Main, StringComparison.Ordinal);

        // Sara has no such look at a tree that is not hers: she gets the page she always gets.
        var hers = await sara.Get($"{tree}?as=public");
        Assert.Equal(HttpStatusCode.OK, hers.Status);
        Assert.DoesNotContain("You are looking", hers.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("See this tree as somebody else sees it", hers.Main, StringComparison.Ordinal);
    }
}
