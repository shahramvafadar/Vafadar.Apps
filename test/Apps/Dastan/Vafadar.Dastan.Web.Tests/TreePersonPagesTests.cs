using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Data.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The page of a person of a family tree (step 19.5, ADR 0017, PRIV-003): the profile – a picture or a
/// drawing, when and where somebody was born and died with what is not known said to be not known, a few
/// words, the family – and the form of who may say more. Every test has a site of its own.
/// </summary>
public sealed class TreePersonPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Trees = "/en/account/trees";
    private const string Photos = "/en/account/photos";
    private const string Forms = "/preferences/trees";
    private readonly PhotosSite _site = new();

    public void Dispose() => _site.Dispose();

    private async Task<Browser> Signed(string name)
    {
        var visitor = _site.Visitor();
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("said=welcome", made.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return visitor;
    }

    private static async Task<string> Tree(Browser owner)
    {
        var made = await owner.Post($"{Forms}/add", Trees, ("name", "The Rad family"), ("returnUrl", Trees));
        var address = made.Response.Headers.Location!.OriginalString;
        return address[..address.IndexOf('?', StringComparison.Ordinal)];
    }

    private static string Id(string page) => page[(page.LastIndexOf('/') + 1)..];

    private static Task<Page> Send(Browser visitor, string tree, string from, string form, params (string, string)[] fields) =>
        visitor.Post($"{Forms}/{form}", from, [.. fields, ("tree", Id(tree)), ("returnUrl", from)]);

    // Adds a person and returns the address of the page of the record.
    private static async Task<string> Person(Browser visitor, string tree, string name, params (string, string)[] fields)
    {
        var added = await Send(visitor, tree, tree, "person-add", [("name", name), .. fields]);
        Assert.Equal($"{tree}?said=person-added#people", added.Response.Headers.Location?.OriginalString);
        var html = (await visitor.Get(tree)).Html;
        var id = Regex.Matches(html, "<li id=\"person-([a-f0-9]{32})\"><p><strong><a href=\"[^\"]+\" dir=\"auto\">([^<]+)</a>").First(match => WebUtility.HtmlDecode(match.Groups[2].Value) == name).Groups[1].Value;
        return $"{tree}/person/{id}";
    }

    private static string Profile(Page page)
    {
        var from = page.Html.IndexOf("<section class=\"profile\"", StringComparison.Ordinal);
        Assert.True(from >= 0, "The page has no profile.");
        return WebUtility.HtmlDecode(page.Html[from..page.Html.IndexOf("</section>", from, StringComparison.Ordinal)]);
    }

    private static async Task<string> Bring(Browser owner, string scope)
    {
        var added = await owner.Upload("/preferences/photos/add", Photos, "IMG_0001.png", TestFiles.Png(), ("caption", "Zal in the garden"), ("taken", "1940-07-04"), ("scope", scope), ("confirmed", "on"), ("returnUrl", Photos));
        Assert.Equal($"{Photos}?said=added#mine", added.Response.Headers.Location?.OriginalString);
        return Regex.Match((await owner.Get(Photos)).Html, "<img src=\"/photo/([a-f0-9]{32})\"").Groups[1].Value;
    }

    [Fact]
    public async Task The_page_of_somebody_who_died_shows_the_times_as_they_were_stated_and_says_what_is_not_known()
    {
        var mina = await Signed("Mina");
        var tree = await Tree(mina);
        var zal = await Person(mina, tree, "Zal", ("born", "1250"), ("born-calendar", "solar-hijri"), ("born-about", "yes"), ("died", "1950"), ("died-month", "3"), ("died-day", "21"), ("died-calendar", "gregorian"));

        var page = await mina.Get(zal);
        var profile = Profile(page);

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1 dir=\"auto\">Zal</h1>", page.Html, StringComparison.Ordinal);
        Assert.Matches("<dt>Born</dt>\\s*<dd><bdi>about 1250 SH</bdi></dd>", profile);
        Assert.Matches("<dt>Died</dt>\\s*<dd><bdi>21 March 1950 CE</bdi></dd>", profile);
        Assert.Matches("<dt>Born in</dt>\\s*<dd class=\"unknown\">Not known</dd>", profile);
        Assert.Matches("<dt>Died in</dt>\\s*<dd class=\"unknown\">Not known</dd>", profile);

        // No picture: a drawing, which is decoration, and the words that say so.
        Assert.Contains("<svg class=\"kind-drawing\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"", profile, StringComparison.Ordinal);
        Assert.Contains("No picture", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
        Assert.Contains("noindex", page.Html, StringComparison.Ordinal);

        // The list of the tree writes the times short and leads to the page.
        var list = (await mina.Get(tree)).Main;
        Assert.Contains("about 1250 SH–1950", Regex.Replace(list, "\\s+", " "), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_form_of_a_record_shows_a_time_again_as_it_was_stated()
    {
        var mina = await Signed("Mina");
        var tree = await Tree(mina);
        await Person(mina, tree, "Sam", ("born", "1800"), ("born-latest", "1810"), ("deceased", "yes"), ("died-unknown", "yes"));

        var html = (await mina.Get(tree)).Html;

        Assert.Matches("name=\"born\" [^>]*value=\"1800\"", html);
        Assert.Matches("name=\"born-latest\" [^>]*value=\"1810\"", html);
        Assert.Matches("name=\"died-unknown\" value=\"yes\" checked", html);
        Assert.Matches("<details class=\"finer\" open", html);
        Assert.Contains("between 1800 CE and 1810 CE", Profile(await mina.Get((await PersonAddress(mina, tree, "Sam")))), StringComparison.Ordinal);
    }

    private static async Task<string> PersonAddress(Browser visitor, string tree, string name)
    {
        var html = (await visitor.Get(tree)).Html;
        return $"{tree}/person/{Regex.Matches(html, "<li id=\"person-([a-f0-9]{32})\"><p><strong><a href=\"[^\"]+\" dir=\"auto\">([^<]+)</a>").First(match => WebUtility.HtmlDecode(match.Groups[2].Value) == name).Groups[1].Value}";
    }

    [Theory]
    [InlineData("born-month", "13")]
    [InlineData("born-day", "5")]
    [InlineData("born-latest", "1700")]
    [InlineData("born-unknown", "yes")]
    public async Task Fields_of_a_time_that_contradict_each_other_are_refused(string field, string value)
    {
        var mina = await Signed("Mina");
        var tree = await Tree(mina);

        var answer = await Send(mina, tree, tree, "person-add", ("name", "Sam"), ("born", "1800"), (field, value), ("deceased", "yes"));

        Assert.Equal($"{tree}?form=person-add&problem=person#person-add", answer.Response.Headers.Location?.OriginalString);
        Assert.Contains("a month and a day are ones the calendar has", (await mina.Get(answer.Response.Headers.Location!.OriginalString)).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Of_somebody_who_lives_and_was_not_asked_the_page_says_why_there_is_nothing_else()
    {
        var mina = await Signed("Mina");
        var tree = await Tree(mina);
        var living = await Person(mina, tree, "Somebody living", ("born", "1980"));

        var page = await mina.Get(living);
        var profile = Profile(page);

        Assert.Contains("has not been asked", profile, StringComparison.Ordinal);
        Assert.Matches("<dt>Died</dt>\\s*<dd>Living</dd>", profile);
        Assert.DoesNotContain("Born in", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("person-describe", page.Html, StringComparison.Ordinal);

        // The form is not there, and sent anyway it is answered with the reason.
        var sent = await Send(mina, tree, living, "person-describe", ("person", Id(living)), ("bornAt", "Zabol"));
        Assert.Equal($"{living}?form=describe&problem=not-asked#describe", sent.Response.Headers.Location?.OriginalString);

        // The day of birth of somebody who lives is not the owner's to write.
        var day = await Send(mina, tree, tree, "person-change", ("person", Id(living)), ("name", "Somebody living"), ("born", "1980"), ("born-month", "5"), ("born-day", "1"));
        Assert.Equal($"{tree}?form=people&problem=person#people", day.Response.Headers.Location?.OriginalString);
        Assert.Contains("the day is theirs to say", (await mina.Get(day.Response.Headers.Location!.OriginalString)).Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "Say more about this person", "The profile is saved.", "Family")]
    [InlineData("fa", "دربارهٔ این فرد بیشتر بگویید", "نمایه ذخیره شد.", "خانواده")]
    public async Task Who_may_change_the_tree_writes_what_the_family_knows_about_somebody_who_died(string language, string title, string saved, string family)
    {
        var mina = await Signed("Mina");
        var tree = await Tree(mina);
        var zal = await Person(mina, tree, "Zal", ("born", "1870"), ("died", "1950"));
        var rostam = await Person(mina, tree, "Rostam");
        await Send(mina, tree, tree, "link", ("kind", "parent"), ("from", Id(zal)), ("to", Id(rostam)));
        var here = zal.Replace("/en/", $"/{language}/", StringComparison.Ordinal);

        Assert.Contains(title, (await mina.Get(here)).Main, StringComparison.Ordinal);
        var sent = await Send(mina, tree, here, "person-describe", ("person", Id(zal)), ("bornAt", "Zabol"), ("diedAt", ""), ("about", "He raised Rostam.\nHe had white hair."), ("picture", ""));

        Assert.Equal($"{here}?said=described#profile", sent.Response.Headers.Location?.OriginalString);
        var page = await mina.Get(sent.Response.Headers.Location!.OriginalString);
        Assert.Contains(saved, page.Main, StringComparison.Ordinal);
        Assert.Contains("<dd><bdi>Zabol</bdi></dd>", Profile(page), StringComparison.Ordinal);
        Assert.Contains("<p class=\"person-about\" dir=\"auto\">He raised Rostam.\nHe had white hair.</p>", WebUtility.HtmlDecode(page.Html), StringComparison.Ordinal);

        // The family leads to the pages of the others, and the form shows what is kept.
        Assert.Contains(family, page.Main, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"account/trees/{Id(tree)}/person/{Id(rostam)}\" dir=\"auto\">Rostam</a>", page.Html, StringComparison.Ordinal);
        Assert.Matches("name=\"bornAt\" [^>]*value=\"Zabol\"", page.Html);

        // A text that is too long is refused, and the page says how long it may be.
        var longer = await Send(mina, tree, here, "person-describe", ("person", Id(zal)), ("bornAt", new string('a', 201)));
        Assert.Equal($"{here}?form=describe&problem=long#describe", longer.Response.Headers.Location?.OriginalString);
        Assert.Contains("role=\"alert\"", (await mina.Get(longer.Response.Headers.Location!.OriginalString)).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_picture_is_a_photograph_of_who_chose_it_and_a_public_tree_shows_it_only_when_it_is_public()
    {
        var mina = await Signed("Mina");
        var tree = await Tree(mina);
        var zal = await Person(mina, tree, "Zal", ("born", "1870"), ("died", "1950"));
        var photo = await Bring(mina, "private");

        // The form offers the photographs of who looks, by their captions.
        Assert.Contains($"<option value=\"{photo}\">Zal in the garden</option>", (await mina.Get(zal)).Html, StringComparison.Ordinal);
        var sent = await Send(mina, tree, zal, "person-describe", ("person", Id(zal)), ("bornAt", "Zabol"), ("picture", photo));
        Assert.Equal($"{zal}?said=described#profile", sent.Response.Headers.Location?.OriginalString);

        var profile = Profile(await mina.Get(zal));
        Assert.Contains($"<img src=\"/photo/{photo}\"", profile, StringComparison.Ordinal);
        Assert.Contains("alt=\"Zal in the garden\"", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("kind-drawing", profile, StringComparison.Ordinal);

        // No photograph of somebody else, and none that is not there.
        var other = await Send(mina, tree, zal, "person-describe", ("person", Id(zal)), ("picture", new string('a', 32)));
        Assert.Equal($"{zal}?form=describe&problem=picture#describe", other.Response.Headers.Location?.OriginalString);

        // Made public, the tree has a page for everybody – with the drawing, while the photograph is private.
        await Send(mina, tree, tree, "publication", ("act", "ask"));
        await Send(mina, tree, tree, "publication", ("act", "publish"));
        var open = $"/en/trees/{Id(tree)}/person/{Id(zal)}";
        var stranger = _site.Visitor();
        var seen = await stranger.Get(open);
        Assert.Equal(HttpStatusCode.OK, seen.Status);
        Assert.Contains("kind-drawing", Profile(seen), StringComparison.Ordinal);
        Assert.Contains("<dd><bdi>Zabol</bdi></dd>", Profile(seen), StringComparison.Ordinal);
        Assert.DoesNotContain("person-describe", seen.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"trees/{Id(tree)}/person/{Id(zal)}\"", (await stranger.Get($"/en/trees/{Id(tree)}")).Html, StringComparison.Ordinal);

        await mina.Post("/preferences/photos/share", Photos, ("photo", photo), ("scope", "public"), ("confirmed", "on"), ("returnUrl", Photos));
        Assert.Contains($"<img src=\"/photo/{photo}\"", Profile(await stranger.Get(open)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_a_person_exists_for_nobody_who_may_not_see_the_tree()
    {
        var mina = await Signed("Mina");
        var tree = await Tree(mina);
        var zal = await Person(mina, tree, "Zal", ("born", "1870"), ("died", "1950"));
        var sara = await Signed("Sara");

        Assert.Equal(HttpStatusCode.NotFound, (await sara.Get(zal)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get(zal)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await mina.Get($"{tree}/person/{new string('a', 32)}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await _site.Visitor().Get($"/en/trees/{Id(tree)}/person/{Id(zal)}")).Status);
        Assert.Equal("/refused", (await Send(sara, tree, Trees, "person-describe", ("person", Id(zal)), ("bornAt", "Zabol"))).Response.Headers.Location?.OriginalString);
    }
}
