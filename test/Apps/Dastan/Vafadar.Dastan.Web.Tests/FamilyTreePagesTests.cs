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
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
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
        return Regex.Matches(html, "<li id=\"person-([a-f0-9]{32})\"><p><strong><a href=\"[^\"]+\" dir=\"auto\">([^<]+)</a></strong>").First(match => WebUtility.HtmlDecode(match.Groups[2].Value) == name).Groups[1].Value;
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
        Assert.Matches("Rostam</a></strong>\\s*<span class=\"meta\">1300–1380</span>", tree.Main);
        Assert.Contains("Parents: Zal, Rudabeh", tree.Main, StringComparison.Ordinal);
        Assert.Contains("Partner: Rudabeh", tree.Main, StringComparison.Ordinal);
        Assert.Contains("Children: Rostam", tree.Main, StringComparison.Ordinal);
        // Each name stands by itself inside the sentence (the marks U+2068 and U+2069), so that a name of
        // another script does not turn the sentence around; the sentence itself runs as the page does.
        Assert.Contains("⁨Zal⁩ is a parent of ⁨Rostam⁩", tree.Main, StringComparison.Ordinal);
        Assert.Contains("⁨Zal⁩ and ⁨Rudabeh⁩ are partners", tree.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<span dir=\"auto\">", tree.Html, StringComparison.Ordinal);
        Assert.Contains("people: 3", (await mina.Get(Trees)).Main, StringComparison.Ordinal);

        // Every change is there with who made it, the newest first.
        Assert.Matches("Partners linked</span>:\\s*<bdi>Zal – Rudabeh</bdi>\\s*<span class=\"meta\"> – <bdi>Mina</bdi>, 20\\d\\d-\\d\\d-\\d\\d", tree.Main);
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
    [InlineData("parent", "exists", "These two are linked already: two people hold one role in a family.")]
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
        // No form that changes the tree; what they may do is say that a record is them.
        Assert.DoesNotContain($"action=\"{Forms}/person-", seen.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/link\"", seen.Html, StringComparison.Ordinal);
        Assert.DoesNotContain($"action=\"{Forms}/unlink\"", seen.Html, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/claim\"", seen.Html, StringComparison.Ordinal);
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
        Assert.Matches("Person added</span>:\\s*<bdi>Sohrab</bdi>\\s*<span class=\"meta\"> – <bdi>Dara</bdi>", (await mina.Get(page)).Main);

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

    [Fact]
    public async Task Somebody_says_a_record_is_them_and_who_made_it_confirms()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        var saraId = await Friends(mina, sara, "Sara");
        var daraId = await Friends(mina, dara, "Dara");
        var page = await Tree(mina);
        var record = await Person(mina, page, "Sara Rad");
        await Send(mina, page, "member", ("account", saraId), ("role", "viewer"));
        await Send(mina, page, "member", ("account", daraId), ("role", "editor"));

        var claimed = await Send(sara, page, "claim", ("person", record));

        Assert.Equal($"{page}?said=claimed#people", claimed.Response.Headers.Location?.OriginalString);
        var waiting = await sara.Get(page);
        Assert.Contains("You said this is you.", waiting.Main, StringComparison.Ordinal);
        Assert.Contains("Take it back", waiting.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("This is me", waiting.Main, StringComparison.Ordinal);

        // Who made the record is asked; somebody else who may change the tree is not, and sees no claim.
        var asked = await mina.Get(page);
        Assert.Contains("Waiting for your answer", asked.Main, StringComparison.Ordinal);
        Assert.Contains("the same name is not proof", asked.Main, StringComparison.Ordinal);
        Assert.Matches("<bdi>Sara</bdi></a>\\s*<span class=\"meta\">– says they are <bdi>Sara Rad</bdi>", asked.Main);
        Assert.DoesNotContain("Waiting for your answer", (await dara.Get(page)).Main, StringComparison.Ordinal);
        var claim = Regex.Match(asked.Html, "name=\"claim\" value=\"(\\d+)\"").Groups[1].Value;
        Assert.Equal("/refused", (await Send(dara, page, "claim-review", ("claim", claim), ("answer", "confirm"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, page, "claim-review", ("claim", claim), ("answer", "confirm"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(mina, page, "claim-review", ("claim", claim), ("answer", "perhaps"))).Response.Headers.Location?.OriginalString);

        var confirmed = await Send(mina, page, "claim-review", ("claim", claim), ("answer", "confirm"));

        Assert.Equal($"{page}?said=claim-confirmed#claims", confirmed.Response.Headers.Location?.OriginalString);
        Assert.Matches($"This is the account <a href=\"people/{saraId}\"><bdi>Sara</bdi></a>", (await dara.Get(page)).Main);
        Assert.Contains("Confirmed \"this is me\"", (await mina.Get(page)).Main, StringComparison.Ordinal);

        // It names the account and widens nothing: Sara may still only see.
        Assert.DoesNotContain($"action=\"{Forms}/person-add\"", (await sara.Get(page)).Html, StringComparison.Ordinal);

        // Only she takes it back.
        Assert.Equal("/refused", (await Send(mina, page, "unclaim", ("person", record))).Response.Headers.Location?.OriginalString);
        Assert.Contains("This is not me after all", (await sara.Get(page)).Main, StringComparison.Ordinal);
        Assert.Equal($"{page}?said=unclaimed#people", (await Send(sara, page, "unclaim", ("person", record))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("This is the account", (await mina.Get(page)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_claim_that_is_refused_names_nobody_and_one_on_a_record_of_ones_own_has_nobody_to_confirm_it()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var saraId = await Friends(mina, sara, "Sara");
        var page = await Tree(mina);
        var record = await Person(mina, page, "Sara Rad");
        await Send(mina, page, "member", ("account", saraId), ("role", "viewer"));
        await Send(sara, page, "claim", ("person", record));
        var claim = Regex.Match((await mina.Get(page)).Html, "name=\"claim\" value=\"(\\d+)\"").Groups[1].Value;

        var refused = await Send(mina, page, "claim-review", ("claim", claim), ("answer", "refuse"));

        Assert.Equal($"{page}?said=claim-refused#claims", refused.Response.Headers.Location?.OriginalString);
        var after = await sara.Get(page);
        Assert.DoesNotContain("This is the account", after.Main, StringComparison.Ordinal);
        Assert.Contains("This is me", after.Main, StringComparison.Ordinal);

        // In her own tree, on a record she made herself, nobody else could say that it is her: it is not offered,
        // and a form sent all the same says why.
        Assert.DoesNotContain($"action=\"{Forms}/claim\"", (await mina.Get(page)).Html, StringComparison.Ordinal);
        var own = await Send(mina, page, "claim", ("person", record));
        Assert.Equal($"{page}?form=people&problem=claim-nobody#people", own.Response.Headers.Location?.OriginalString);
        Assert.Contains("Somebody other than you has to confirm", (await mina.Get($"{page}?form=people&problem=claim-nobody")).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("claim")]
    [InlineData("claim-review")]
    [InlineData("unclaim")]
    public async Task A_claim_is_refused_to_who_is_not_signed_in_and_to_who_may_not_see_the_tree(string form)
    {
        var mina = await Signed("Mina");
        var zal = await Signed("Zal");
        var page = await Tree(mina);
        var record = await Person(mina, page, "Zal Rad");

        Assert.Equal("/refused", (await _site.Visitor().Post($"{Forms}/{form}", Account, ("tree", Id(page)), ("person", record), ("claim", "1"), ("answer", "confirm"), ("returnUrl", page))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(zal, page, form, ("person", record), ("claim", "1"), ("answer", "confirm"))).Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task A_tree_is_drawn_with_parents_above_their_children_and_partners_side_by_side()
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina);
        Assert.DoesNotContain("<svg class=\"graph tree\"", (await mina.Get(page)).Html, StringComparison.Ordinal);
        var zal = await Person(mina, page, "Zal");
        var rudabeh = await Person(mina, page, "Rudabeh");
        var rostam = await Person(mina, page, "Rostam");
        await Send(mina, page, "link", ("kind", "parent"), ("from", zal), ("to", rostam));
        await Send(mina, page, "link", ("kind", "parent"), ("from", rudabeh), ("to", rostam));
        await Send(mina, page, "link", ("kind", "partner"), ("from", zal), ("to", rudabeh));

        var tree = await mina.Get(page);

        Assert.Contains("<svg class=\"graph tree\"", tree.Html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"The family tree “The Rad family” as a drawing\"", WebUtility.HtmlDecode(tree.Html), StringComparison.Ordinal);

        // A box per person, each leading to its record in the list; two lines with arrows and one dashed line.
        var boxes = Regex.Matches(tree.Html, "<foreignObject x=\"([\\d.]+)em\" y=\"([\\d.]+)em\"[^>]*>\\s*<div class=\"graph-node \">\\s*<span class=\"graph-entity\">\\s*<a class=\"graph-name\" href=\"account/trees/[a-f0-9]{32}#person-([a-f0-9]{32})\"")
            .ToDictionary(match => match.Groups[3].Value, match => (X: double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), Y: double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal(3, boxes.Count);
        Assert.Equal(boxes[zal].Y, boxes[rudabeh].Y);
        Assert.True(boxes[rostam].Y > boxes[zal].Y);
        Assert.NotEqual(boxes[zal].X, boxes[rudabeh].X);
        Assert.Equal(2, Regex.Matches(tree.Html, "marker-end=\"url\\(#graph-arrow\\)\"").Count);
        Assert.Single(Regex.Matches(tree.Html, "class=\"graph-line graph-partner\""));
        Assert.DoesNotContain("<script", tree.Own, StringComparison.Ordinal);

        // On a Persian page the tree begins at the right.
        var fa = await mina.Get(page.Replace("/en/", "/fa/", StringComparison.Ordinal));
        Assert.Contains("<svg class=\"graph tree\"", fa.Html, StringComparison.Ordinal);
        Assert.Contains("نمودار درخت", fa.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_tree_is_made_public_with_the_agreement_of_who_it_names_and_hidden_when_that_is_taken_back()
    {
        var mina = await Signed("Mina");
        var sara = await Signed("Sara");
        var dara = await Signed("Dara");
        var saraId = await Friends(mina, sara, "Sara");
        var daraId = await Friends(mina, dara, "Dara");
        var page = await Tree(mina);
        var open = $"/en/trees/{Id(page)}";
        await Send(mina, page, "person-add", ("name", "Zal"), ("born", "1870"), ("died", "1950"));
        var record = await Person(mina, page, "Sara Rad");
        await Send(mina, page, "member", ("account", saraId), ("role", "viewer"));
        await Send(mina, page, "member", ("account", daraId), ("role", "viewer"));

        // Not asked for: nobody sees it, and nobody is asked anything.
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get(open)).Status);
        Assert.Contains("It needs the agreement of everybody living it names.", (await mina.Get(page)).Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Making this tree public", (await sara.Get(page)).Main, StringComparison.Ordinal);
        Assert.Equal("/refused", (await Send(mina, page, "publication", ("act", "publish"))).Response.Headers.Location?.OriginalString);

        var asked = await Send(mina, page, "publication", ("act", "ask"));

        // Zal died and stands in nobody's way; Sara is living and is no account yet.
        Assert.Equal($"{page}?said=publication-asked#publication", asked.Response.Headers.Location?.OriginalString);
        var check = await mina.Get(page);
        Assert.Matches("<bdi>Sara Rad</bdi></a>\\s*<span class=\"meta\">– is living and has no account here", check.Main);
        Assert.DoesNotContain("<bdi>Zal</bdi></a>", check.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"publish\"", check.Html, StringComparison.Ordinal);
        Assert.Equal("/refused", (await Send(mina, page, "publication", ("act", "publish"))).Response.Headers.Location?.OriginalString);

        // She says the record is her; she is asked, and nobody else is.
        await Send(sara, page, "claim", ("person", record));
        var claim = Regex.Match((await mina.Get(page)).Html, "name=\"claim\" value=\"(\\d+)\"").Groups[1].Value;
        await Send(mina, page, "claim-review", ("claim", claim), ("answer", "confirm"));
        Assert.Matches("<bdi>Sara Rad</bdi></a>\\s*<span class=\"meta\">– has been asked and has not agreed", (await mina.Get(page)).Main);
        var question = await sara.Get(page);
        Assert.Contains("Do you agree to be named in it for everybody?", question.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("This tree is to be public", (await dara.Get(page)).Main, StringComparison.Ordinal);
        Assert.Equal("/refused", (await Send(dara, page, "consent", ("answer", "agree"))).Response.Headers.Location?.OriginalString);
        Assert.Equal("/refused", (await Send(sara, page, "publication", ("act", "publish"))).Response.Headers.Location?.OriginalString);

        Assert.Equal($"{page}?said=consent-given#consent", (await Send(sara, page, "consent", ("answer", "agree"))).Response.Headers.Location?.OriginalString);

        // Everybody agreed – and it is not public until the owner makes it so.
        var ready = await mina.Get(page);
        Assert.Contains("Nobody stands in the way.", ready.Main, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get(open)).Status);
        Assert.Equal($"{page}?said=published#publication", (await Send(mina, page, "publication", ("act", "publish"))).Response.Headers.Location?.OriginalString);

        // Everybody sees the people and how they belong together – and nothing about accounts or changes.
        var shown = await _site.Visitor().Get(open);
        Assert.Equal(HttpStatusCode.OK, shown.Status);
        Assert.Contains("Everybody living it names has agreed", shown.Main, StringComparison.Ordinal);
        Assert.Contains("<svg class=\"graph tree\"", shown.Html, StringComparison.Ordinal);
        Assert.Matches("Zal</a></strong>\\s*<span class=\"meta\">1870–1950</span>", shown.Main);
        Assert.Contains("Sara Rad", shown.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Mina", shown.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("This is the account", shown.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Every change", shown.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<form", shown.Main, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex, nofollow\"", shown.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", shown.Own, StringComparison.Ordinal);
        Assert.Contains($"href=\"trees/{Id(page)}\"", (await mina.Get(page)).Html, StringComparison.Ordinal);
        Assert.Contains("نام برده شده به این نام‌بردن رضایت داده است", (await _site.Visitor().Get(open.Replace("/en/", "/fa/", StringComparison.Ordinal))).Main, StringComparison.Ordinal);

        // She takes her agreement back: hidden at once, and the owner is told why.
        Assert.Equal($"{page}?said=consent-withdrawn#consent", (await Send(sara, page, "consent", ("answer", "take-back"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get(open)).Status);
        Assert.Contains("it is hidden now: somebody it names does not agree", (await mina.Get(page)).Main, StringComparison.Ordinal);

        // Agreed again it is shown again; taken back by the owner it is gone and what was agreed is forgotten.
        await Send(sara, page, "consent", ("answer", "agree"));
        Assert.Equal(HttpStatusCode.OK, (await _site.Visitor().Get(open)).Status);
        Assert.Equal($"{page}?said=unpublished#publication", (await Send(mina, page, "publication", ("act", "take-back"))).Response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get(open)).Status);
        Assert.DoesNotContain("This tree is to be public", (await sara.Get(page)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_tree_of_people_who_all_died_is_made_public_by_its_owner_alone()
    {
        var mina = await Signed("Mina");
        var page = await Tree(mina, "Long ago");
        await Send(mina, page, "person-add", ("name", "Sam"), ("born", "1800"), ("died", "1870"));
        await Send(mina, page, "person-add", ("name", "Simin"), ("born", ""), ("died", ""), ("deceased", "yes"));

        await Send(mina, page, "publication", ("act", "ask"));
        Assert.Contains("Nobody stands in the way.", (await mina.Get(page)).Main, StringComparison.Ordinal);
        await Send(mina, page, "publication", ("act", "publish"));

        var shown = await _site.Visitor().Get($"/en/trees/{Id(page)}");
        Assert.Equal(HttpStatusCode.OK, shown.Status);
        Assert.Contains("Sam", shown.Main, StringComparison.Ordinal);
        Assert.Matches("Simin</a></strong>\\s*<span class=\"meta\">deceased</span>", shown.Main);

        // Somebody living added afterwards hides it until that is settled.
        await Send(mina, page, "person-add", ("name", "Somebody living"), ("born", ""), ("died", ""));
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get($"/en/trees/{Id(page)}")).Status);
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef")]
    [InlineData("not-a-tree")]
    public async Task The_address_of_a_tree_that_is_not_public_or_does_not_exist_is_not_found(string id)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get($"/en/trees/{id}")).Status);

        using var site = new DemoSite();
        Assert.Equal(HttpStatusCode.NotFound, (await site.Visitor().Get($"/en/trees/{id}")).Status);
    }

    [Theory]
    [InlineData("publication")]
    [InlineData("consent")]
    public async Task Making_public_is_refused_to_who_is_not_signed_in(string form)
    {
        var answer = await _site.Visitor().Post($"{Forms}/{form}", Account, ("tree", "x"), ("act", "ask"), ("answer", "agree"), ("returnUrl", Trees));

        Assert.Equal("/refused", answer.Response.Headers.Location?.OriginalString);
    }
}
