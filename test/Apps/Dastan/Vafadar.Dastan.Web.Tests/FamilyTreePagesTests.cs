using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// Family trees on the site (step 16.4, D-054): a tree of its owner, the accounts the owner names to see or to
/// change it, the records and links, and every change with who made it. Every test has a site of its own.
/// </summary>
public sealed class FamilyTreePagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Trees = "/en/account/trees";
    private const string Forms = "/preferences/trees";
    private readonly AccountsSite _site = new();

    public void Dispose() => _site.Dispose();

    private async Task<Browser> Signed(string name)
    {
        var visitor = _site.Visitor();
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("said=welcome", made.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return visitor;
    }

    // Makes a tree and returns the address of its page.
    private static async Task<string> Tree(Browser owner, string name = "The Rad family")
    {
        var made = await owner.Post($"{Forms}/add", Trees, ("name", name), ("returnUrl", Trees));
        var address = made.Response.Headers.Location!.OriginalString;
        Assert.Matches("^/en/account/trees/[a-f0-9]{32}\\?said=made$", address);
        return address[..address.IndexOf('?', StringComparison.Ordinal)];
    }

    private static string Id(string page) => page[(page.LastIndexOf('/') + 1)..];

    private static Task<Page> Send(Browser visitor, string page, string form, params (string, string)[] fields) =>
        visitor.Post($"{Forms}/{form}", page, [.. fields, ("tree", Id(page)), ("returnUrl", page)]);

    // Adds a person and returns the identifier of the record.
    private static async Task<string> Person(Browser visitor, string page, string name, string born = "", string died = "")
    {
        var added = await Send(visitor, page, "person-add", ("name", name), ("born", born), ("died", died));
        Assert.Equal($"{page}?said=person-added#people", added.Response.Headers.Location?.OriginalString);
        var html = (await visitor.Get(page)).Html;
        return Regex.Matches(html, "<li id=\"person-([a-f0-9]{32})\"><p><strong dir=\"auto\">([^<]+)</strong>").First(match => WebUtility.HtmlDecode(match.Groups[2].Value) == name).Groups[1].Value;
    }

    // The two show a relationship; returns the identifier of the second account.
    private static async Task<string> Friends(Browser first, Browser second, string secondName)
    {
        const string people = "/en/account/people";
        await first.Post("/preferences/people/ask", people, ("name", secondName), ("kind", "family"), ("returnUrl", people));
        var relationship = Regex.Match((await second.Get(people)).Html, "name=\"relationship\" value=\"([a-f0-9]{32})\"").Groups[1].Value;
        await second.Post("/preferences/people/answer", people, ("relationship", relationship), ("answer", "accept"), ("returnUrl", people));
        return Regex.Match((await second.Get(people)).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;
    }

    [Fact]
    public async Task The_pages_are_for_who_signed_in_and_a_site_without_accounts_has_none()
    {
        var sent = await _site.Visitor().Get(Trees);
        Assert.Equal(HttpStatusCode.Redirect, sent.Status);
        Assert.EndsWith(Account, sent.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get($"{Trees}/0123456789abcdef0123456789abcdef")).Status);

        using var site = new DemoSite();
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get(Trees)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Post($"{Forms}/add", "/en/", ("name", "A tree"))).Status);
    }

    [Theory]
    [InlineData("en", "Family trees", "Begin a family tree", "Nobody else sees it.")]
    [InlineData("fa", "شجره‌نامه‌ها", "آغاز یک شجره‌نامه", "هیچ‌کس دیگری آن را نمی‌بیند.")]
    public async Task The_page_of_the_trees_says_whose_a_tree_is_in_the_language_of_the_page(string language, string title, string begin, string rule)
    {
        var mina = await Signed("Mina");

        var page = await mina.Get($"/{language}/account/trees");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(begin, page.Main, StringComparison.Ordinal);
        Assert.Contains(rule, page.Main, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.Contains("href=\"account/trees\"", (await mina.Get($"/{language}/account")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_owner_builds_a_tree_of_people_and_their_links()
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);
        Assert.Contains("The tree is made.", (await mina.Get($"{page}?said=made")).Main, StringComparison.Ordinal);

        var zal = await Person(mina, page, "Zal");
        var rudabeh = await Person(mina, page, "Rudabeh");
        var rostam = await Person(mina, page, "Rostam", "1300", "1380");
        Assert.Equal($"{page}?said=linked#links", (await Send(mina, page, "link", ("kind", "parent"), ("from", zal), ("to", rostam))).Response.Headers.Location?.OriginalString);
        await Send(mina, page, "link", ("kind", "parent"), ("from", rudabeh), ("to", rostam));
        await Send(mina, page, "link", ("kind", "partner"), ("from", zal), ("to", rudabeh));

        var tree = await mina.Get(page);
        Assert.Contains("This tree is yours.", tree.Main, StringComparison.Ordinal);
        Assert.Matches("Rostam</strong>\\s*<span class=\"meta\">1300–1380</span>", tree.Main);
        Assert.Contains("Parents: Zal, Rudabeh", tree.Main, StringComparison.Ordinal);
        Assert.Contains("Partner: Rudabeh", tree.Main, StringComparison.Ordinal);
        Assert.Contains("Children: Rostam", tree.Main, StringComparison.Ordinal);
        Assert.Contains("Zal is a parent of Rostam", tree.Main, StringComparison.Ordinal);
        Assert.Contains("Zal and Rudabeh are partners", tree.Main, StringComparison.Ordinal);
        Assert.Contains("people: 3", (await mina.Get(Trees)).Main, StringComparison.Ordinal);

        // Every change is there with who made it, the newest first.
        Assert.Matches("Partners linked</span>:\\s*<bdi>Zal – Rudabeh</bdi>\\s*<span class=\"meta\">– <bdi>Mina</bdi>, 20\\d\\d-\\d\\d-\\d\\d", tree.Main);
        Assert.True(tree.Main.IndexOf("Partners linked", StringComparison.Ordinal) < tree.Main.IndexOf("Tree begun", StringComparison.Ordinal));
        Assert.DoesNotContain("<script", tree.Own, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("Zal", "soon", "")]
    [InlineData("Zal", "1300", "1299")]
    [InlineData("Zal", "0", "")]
    public async Task A_record_that_says_something_impossible_is_not_kept_and_the_page_says_why(string name, string born, string died)
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);

        var answer = await Send(mina, page, "person-add", ("name", name), ("born", born), ("died", died));

        Assert.Equal($"{page}?form=person-add&problem=person#person-add", answer.Response.Headers.Location?.OriginalString);
        var said = await mina.Get($"{page}?form=person-add&problem=person");
        Assert.Contains("nobody dies before they are born", said.Main, StringComparison.Ordinal);
        Assert.Contains("Nobody is in this tree yet.", said.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_year_is_written_with_the_digits_of_the_page_and_read_in_either()
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);
        var fa = page.Replace("/en/", "/fa/", StringComparison.Ordinal);

        var added = await mina.Post($"{Forms}/person-add", fa, ("name", "رستم"), ("born", "۱۳۰۰"), ("died", ""), ("deceased", "yes"), ("tree", Id(page)), ("returnUrl", fa));

        Assert.Equal($"{fa}?said=person-added#people", added.Response.Headers.Location?.OriginalString);
        Assert.Contains("زادهٔ ۱۳۰۰، درگذشته", (await mina.Get(fa)).Main, StringComparison.Ordinal);
        Assert.Contains("born 1300, deceased", (await mina.Get(page)).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("parent", "same", "A link is between two different people.")]
    [InlineData("parent", "exists", "These two are linked like this already.")]
    [InlineData("parent", "loop", "That would make somebody their own ancestor.")]
    [InlineData("cousin", "kind", "Please choose what the two are to each other.")]
    public async Task A_link_that_cannot_be_says_why(string kind, string problem, string said)
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);
        var zal = await Person(mina, page, "Zal");
        var rostam = await Person(mina, page, "Rostam");
        await Send(mina, page, "link", ("kind", "parent"), ("from", zal), ("to", rostam));
        var sohrab = await Person(mina, page, "Sohrab");
        await Send(mina, page, "link", ("kind", "parent"), ("from", rostam), ("to", sohrab));
        var (from, to) = problem switch { "same" => (zal, zal), "loop" => (sohrab, zal), _ => (zal, rostam) };

        var answer = await Send(mina, page, "link", ("kind", kind), ("from", from), ("to", to));

        Assert.Equal($"{page}?form=links&problem=link-{problem}#links", answer.Response.Headers.Location?.OriginalString);
        Assert.Contains(said, (await mina.Get($"{page}?form=links&problem=link-{problem}")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_record_is_changed_and_removed_and_a_link_is_removed()
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);
        var zal = await Person(mina, page, "Zal");
        var rostam = await Person(mina, page, "Rostam");
        await Send(mina, page, "link", ("kind", "parent"), ("from", zal), ("to", rostam));

        var changed = await Send(mina, page, "person-change", ("person", rostam), ("name", "Rostam of Zabol"), ("born", "1300"), ("died", ""));
        Assert.Equal($"{page}?said=person-changed#people", changed.Response.Headers.Location?.OriginalString);
        Assert.Contains("Rostam of Zabol", (await mina.Get(page)).Main, StringComparison.Ordinal);

        var link = Regex.Match((await mina.Get(page)).Html, "name=\"link\" value=\"(\\d+)\"").Groups[1].Value;
        Assert.Equal($"{page}?said=unlinked#links", (await Send(mina, page, "unlink", ("link", link))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("Parents:", (await mina.Get(page)).Main, StringComparison.Ordinal);

        Assert.Equal($"{page}?said=person-removed#people", (await Send(mina, page, "person-remove", ("person", zal))).Response.Headers.Location?.OriginalString);
        var tree = await mina.Get(page);
        Assert.DoesNotContain($"person-{zal}", tree.Html, StringComparison.Ordinal);
        Assert.Contains("Record removed", tree.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_owner_names_who_may_see_and_who_may_change_and_nobody_else_finds_the_tree()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        var zal = await Signed("Zal");
        var saraId = await Friends(mina, sara, "Sara");
        var daraId = await Friends(mina, dara, "Dara");
        var page = await Tree(mina);
        var rostam = await Person(mina, page, "Rostam");

        // Not named: the tree does not exist, signed in or not.
        Assert.Equal(HttpStatusCode.NotFound, (await sara.Get(page)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get(page)).Status);
        // A form sent all the same adds nothing, and leads to a page that is not there for them.
        await Send(sara, page, "person-add", ("name", "Sohrab"));
        Assert.DoesNotContain("Sohrab", (await mina.Get(page)).Main, StringComparison.Ordinal);

        var named = await Send(mina, page, "member", ("account", saraId), ("role", "viewer"));
        Assert.Equal($"{page}?said=member-set#members", named.Response.Headers.Location?.OriginalString);
        await Send(mina, page, "member", ("account", daraId), ("role", "editor"));

        // Who may see, sees the people and the history and gets no form.
        var seen = await sara.Get(page);
        Assert.Equal(HttpStatusCode.OK, seen.Status);
        Assert.Contains("Mina lets you see this tree.", seen.Main, StringComparison.Ordinal);
        Assert.Contains("Rostam", seen.Main, StringComparison.Ordinal);
        Assert.Contains("Every change", seen.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<form method=\"post\" action=\"/preferences/trees", seen.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Who may see or change this tree", seen.Main, StringComparison.Ordinal);
        Assert.Contains("Mina lets you see this tree.", (await sara.Get(Trees)).Main, StringComparison.Ordinal);
        Assert.Equal("/refused", (await Send(sara, page, "person-remove", ("person", rostam))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, page, "member", ("account", daraId), ("role", "none"))).Response.Headers.Location?.OriginalString);

        // Who may change, changes people and links and nothing about the tree itself.
        var edited = await dara.Get(page);
        Assert.Contains("Mina lets you see and change this tree.", edited.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/person-add\"", edited.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/member\"", edited.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/remove\"", edited.Html, StringComparison.Ordinal);
        await Person(dara, page, "Sohrab");
        Assert.Equal("/refused", (await Send(dara, page, "remove")).Response.Headers.Location?.OriginalString);
        Assert.Contains("problem=tree-name", (await Send(dara, page, "rename", ("name", "Dara's"))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        Assert.Matches("Person added</span>:\\s*<bdi>Sohrab</bdi>\\s*<span class=\"meta\">– <bdi>Dara</bdi>", (await mina.Get(page)).Main);

        // Somebody the owner shows no relationship with is not named, and is not offered.
        var zalId = Regex.Match((await zal.Get("/en/account/people")).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;
        Assert.Equal("/refused", (await Send(mina, page, "member", ("account", zalId), ("role", "viewer"))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(zalId, (await mina.Get(page)).Html, StringComparison.Ordinal);

        // Taken back, the tree is gone for them at once.
        await Send(mina, page, "member", ("account", saraId), ("role", "none"));
        Assert.Equal(HttpStatusCode.NotFound, (await sara.Get(page)).Status);
        Assert.Contains("none is shared with you", (await sara.Get(Trees)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_owner_renames_and_removes_the_tree()
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);

        Assert.Equal($"{page}?said=renamed", (await Send(mina, page, "rename", ("name", "The family of Zal"))).Response.Headers.Location?.OriginalString);
        Assert.Contains("<h1 dir=\"auto\">The family of Zal</h1>", (await mina.Get(page)).Html, StringComparison.Ordinal);
        Assert.Contains("problem=tree-name", (await Send(mina, page, "rename", ("name", " "))).Response.Headers.Location?.OriginalString, StringComparison.Ordinal);

        Assert.Equal($"{Trees}?said=removed", (await Send(mina, page, "remove")).Response.Headers.Location?.OriginalString);
        Assert.Contains("The tree is removed.", (await mina.Get($"{Trees}?said=removed")).Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await mina.Get(page)).Status);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("rename")]
    [InlineData("remove")]
    [InlineData("person-add")]
    [InlineData("person-change")]
    [InlineData("person-remove")]
    [InlineData("link")]
    [InlineData("unlink")]
    [InlineData("member")]
    public async Task Every_form_is_refused_to_who_is_not_signed_in(string form)
    {
        var answer = await _site.Visitor().Post($"{Forms}/{form}", Account, ("name", "A tree"), ("tree", "x"), ("person", "x"), ("link", "1"), ("account", "x"), ("role", "viewer"), ("returnUrl", Trees));

        Assert.Equal("/refused", answer.Response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("https://elsewhere.example/account/trees")]
    [InlineData("/en/account/trees/../../story-rostam-sohrab")]
    [InlineData("/en/account/trees/not-a-tree")]
    public async Task A_form_leads_back_to_a_page_of_the_trees_and_nowhere_else(string returnUrl)
    {
        var mina = await Signed("Mina");

        var answer = await mina.Post($"{Forms}/add", Trees, ("name", ""), ("returnUrl", returnUrl));

        Assert.Equal("/account/trees?form=add&problem=tree-name#add", answer.Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task What_an_address_says_is_not_written_into_the_page()
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);

        var tree = await mina.Get($"{page}?said=<b>hello</b>&form=links&problem=<i>nonsense</i>");

        Assert.DoesNotContain("hello", tree.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("nonsense", tree.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"alert\"", tree.Html, StringComparison.Ordinal);
    }
}
