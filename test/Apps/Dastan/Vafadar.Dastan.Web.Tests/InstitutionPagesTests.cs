using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// A site with accounts and two collections that hold places people can have been at. Everything about the
/// places is test data: no such university, company or club exists.
/// <list type="bullet">
/// <item>The story holds a university with a former name and an identifier of a made-up catalogue, a company,
/// and a university only a later stage of the story shows.</item>
/// <item>The club collection holds a university of the same name with the same identifier – the same place, as
/// editors state it –, a second university of the same name without one – another place –, and an entity whose
/// identifier is the one the company of the story has.</item>
/// </list>
/// </summary>
public sealed class InstitutionsSite : DemoSite
{
    private readonly string _folder;
    private readonly bool _owns;
    private readonly bool _places;

    /// <summary>The name of the story in addresses.</summary>
    public const string Story = "story-rostam-sohrab";

    /// <summary>The name of the second collection in addresses.</summary>
    public const string Club = "subject-club";

    /// <summary>What both universities of one place and the other university of that name are called.</summary>
    public const string University = "Made-up University of Tus";

    public InstitutionsSite()
        : this(Path.Combine(Path.GetTempPath(), "dastan-site-places-" + Guid.NewGuid().ToString("N")), places: true, owns: true)
    {
    }

    private InstitutionsSite(string folder, bool places, bool owns) => (_folder, _places, _owns) = (folder, places, owns);

    /// <summary>Returns a site on the same accounts whose collections hold none of the places: they were withdrawn.</summary>
    public InstitutionsSite Withdrawn() => new(_folder, places: false, owns: false);

    private string Content => Path.Combine(_folder, _places ? "with" : "without", "content");

    /// <summary>Gets a value indicating whether the site takes what visitors write to the editors: reports, and places they propose.</summary>
    public bool TakesProposals { get; init; }

    /// <summary>Gets the connection of the database of the site.</summary>
    public string Database => TestDatabase.For(Path.Combine(_folder, "people.db"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (TakesProposals)
        {
            builder.UseSetting("Dastan:Reports", "true");
        }

        builder.UseSetting("Dastan:Database", TestDatabase.For(Path.Combine(_folder, "people.db")));
        builder.UseSetting("Dastan:Content", Content);
        if (Directory.Exists(Content))
        {
            return;
        }

        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        var copies = Path.GetDirectoryName(Content)!;
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(copies, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy, overwrite: true);
            }
        }

        if (!_places)
        {
            return;
        }

        var storyFile = Path.Combine(Content, "samples", "shahnameh-pilot.json");
        var story = JsonNode.Parse(File.ReadAllText(storyFile))!.AsObject();
        var source = (string)story["sources"]![0]!["id"]!;
        var entities = story["entities"]!.AsArray();
        entities.Add(Place("place-university", "University", University, "Test data: a university that does not exist.", source, 0,
            """[ { "name": "Tus Academy of the Tests", "language": "en", "kind": "former" }, { "name": "دانشگاه ساختگی توس", "language": "fa", "kind": "alternative" } ]""",
            """[ { "issuer": "made-up-catalogue", "value": "U-1", "match": "exact" } ]"""));
        entities.Add(Place("place-company", "Company", "Made-up Works of Tus", "Test data: a company that does not exist.", source, 0, null,
            """[ { "issuer": "made-up-catalogue", "value": "C-1", "match": "close" } ]"""));
        entities.Add(Place("place-later", "University", "Made-up Late University", "Test data: a university a later stage shows.", source, 3, null, null));
        File.WriteAllText(storyFile, story.ToJsonString());

        var club = JsonNode.Parse(ContentTemplates.Subject("club", new DateOnly(2026, 11, 18)))!.AsObject();
        source = (string)club["sources"]![0]!["id"]!;
        var about = club["entities"]!.AsArray().First(entity => (string?)entity!["id"] == (string?)club["subjectId"])!;
        about["label"] = "A club of the tests";
        about["summary"] = "Test data: a club that does not exist.";
        entities = club["entities"]!.AsArray();
        entities.Add(Place("place-university", "University", University, "Test data: the same made-up university, in another collection.", source, 0, null,
            """[ { "issuer": "Made-Up-Catalogue", "value": "U-1", "match": "exact" } ]"""));
        entities.Add(Place("place-town", "Place", "Made-up Town", "Test data: a town that does not exist.", source, 0, null, null));
        var other = Place("place-other", "University", University, "Test data: another made-up university of the same name.", source, 0, null, null);
        other["facts"] = JsonNode.Parse("""[ { "fact": "seat", "entityId": "place-town" } ]""");
        entities.Add(other);
        entities.Add(Place("place-company", "Company", "Made-up Works of the Club", "Test data: a company that does not exist.", source, 0, null,
            """[ { "issuer": "made-up-catalogue", "value": "C-1", "match": "exact" } ]"""));
        File.WriteAllText(Path.Combine(Content, "samples", "a-club.json"), club.ToJsonString());
    }

    private static JsonObject Place(string id, string type, string label, string summary, string source, int level, string? names, string? identifiers)
    {
        var place = new JsonObject
        {
            ["id"] = id,
            ["type"] = type,
            ["label"] = label,
            ["summary"] = summary,
            ["sourceIds"] = new JsonArray(source),
            ["spoilerLevel"] = level,
        };
        if (names is not null)
        {
            place["names"] = JsonNode.Parse(names);
        }

        if (identifiers is not null)
        {
            place["identifiers"] = JsonNode.Parse(identifiers);
        }

        return place;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _owns && Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}

/// <summary>
/// A place as an entry of a collection (step 20.4, ADR 0023): a form finds entries by a name and the visitor
/// chooses one or none; a statement that points at an entry is found through the entry – under another
/// name, in another collection where the editors say it is the same place – and never through a name alone.
/// Every test has a site of its own.
/// </summary>
public sealed class InstitutionPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Page = "/en/account/affiliations";
    private const string Forms = "/preferences/affiliations";
    private const string Story = InstitutionsSite.Story;
    private const string Club = InstitutionsSite.Club;
    private readonly InstitutionsSite _site = new();

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

    // States a place that everybody may find the account through; returns the number of the affiliation.
    private static async Task<string> At(Browser visitor, string words, string? entry, params (string, string)[] fields)
    {
        var known = Numbers(await visitor.Get(Page));
        (string, string)[] place = entry is null ? [("place", words)] : [("place", words), ("institution", entry)];
        Assert.Equal($"{Page}?said=added#mine", (await Send(visitor, "add", [.. place, .. fields])).Response.Headers.Location?.OriginalString);
        var number = Numbers(await visitor.Get(Page)).Except(known).Single();
        Assert.Equal($"{Page}?said=shown#mine", (await Send(visitor, "show", ("affiliation", number), ("findable", "public"), ("dates", "public"))).Response.Headers.Location?.OriginalString);
        return number;
    }

    private static string[] Numbers(Page page) => [.. Regex.Matches(page.Html, "<li id=\"affiliation-(\\d+)\"").Select(match => match.Groups[1].Value)];

    private static string Words(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")), "\\s+", " ").Trim();

    private static string Section(Page page, string id)
    {
        var from = page.Html.IndexOf($"<section id=\"{id}\"", StringComparison.Ordinal);
        return from < 0 ? string.Empty : page.Html[from..page.Html.IndexOf("</section>", from, StringComparison.Ordinal)];
    }

    private static string Found(Page page)
    {
        var from = page.Html.IndexOf("<h3 id=\"found\"", StringComparison.Ordinal);
        return from < 0 ? string.Empty : Words(page.Html[from..page.Html.IndexOf("</section>", from, StringComparison.Ordinal)]);
    }

    private static string[] Choices(string html) => [.. Regex.Matches(html, "name=\"institution\" type=\"radio\" value=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];

    [Fact]
    public async Task Looking_for_a_place_offers_every_entry_of_that_name_with_what_tells_them_apart_and_chooses_none()
    {
        var mina = await Signed("Mina");

        var page = await mina.Get($"{Page}?at=made-up+university+of+tus");
        var add = Section(page, "add");

        // Three entries go by the name: one of the story, two of the club collection. And "none of these".
        Assert.Equal([$"{Story}/place-university", $"{Club}/place-university", $"{Club}/place-other", "none"], Choices(add));
        Assert.DoesNotContain("checked", Regex.Match(add, "<fieldset.*</fieldset>", RegexOptions.Singleline).Value, StringComparison.Ordinal);
        var words = Words(add);
        Assert.Contains("Is it one of these?", words, StringComparison.Ordinal);
        Assert.Contains("University – in the collection “Rostam and Sohrab”", words, StringComparison.Ordinal);
        Assert.Contains("in the collection “A club of the tests”", words, StringComparison.Ordinal);
        Assert.Contains("Other names: Tus Academy of the Tests , دانشگاه ساختگی توس", words.Replace(", ", " , ", StringComparison.Ordinal).Replace("  ", " ", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("None of these – keep only my words", words, StringComparison.Ordinal);

        // Where a place is tells two of one name apart, when the collection states it.
        Assert.Contains("in the collection “A club of the tests” – Made-up Town", words, StringComparison.Ordinal);

        // The words that were looked for are the words of the statement until the visitor writes others.
        Assert.Matches("id=\"add-place\"[^>]*value=\"made-up university of tus\"", add);

        // A former name and a name in another language find the entry too.
        Assert.Equal([$"{Story}/place-university", "none"], Choices(Section(await mina.Get($"{Page}?at=Tus+Academy+of+the+Tests"), "add")));
        Assert.Equal([$"{Story}/place-university", "none"], Choices(Section(await mina.Get($"{Page}?at={Uri.EscapeDataString("دانشگاه ساختگی توس")}"), "add")));

        // Written with the Arabic letters of another keyboard it is found as well – and still chosen by the visitor.
        Assert.Equal([$"{Story}/place-university", "none"], Choices(Section(await mina.Get($"{Page}?at={Uri.EscapeDataString("دانشگاه ساختگي توس")}"), "add")));

        // What only a later stage of the story shows is offered to nobody, and a character is no place.
        Assert.Empty(Choices(Section(await mina.Get($"{Page}?at=Made-up+Late+University"), "add")));
        var none = await mina.Get($"{Page}?at=Rostam");
        Assert.Empty(Choices(Section(none, "add")));
        Assert.Contains("No entry of the collections goes by “Rostam”. Your statement stands in your own words.", Words(Section(none, "add")), StringComparison.Ordinal);

        // Looking stated nothing.
        Assert.Empty(Numbers(await mina.Get(Page)));
    }

    [Fact]
    public async Task A_statement_points_at_the_entry_that_was_chosen_and_at_nothing_a_form_makes_up()
    {
        var mina = await Signed("Mina");

        await At(mina, "my old university", $"{Story}/place-university", ("role", "Student"));
        var mine = Section(await mina.Get(Page), "mine");

        // The name is what the entry is called today and leads to its page; the words of the visitor stand beside it.
        Assert.Contains($"<a href=\"{Story}/entity/place-university\"", mine, StringComparison.Ordinal);
        Assert.Contains($"{InstitutionsSite.University} (stated as my old university ) – Student", Words(mine), StringComparison.Ordinal);

        // No entry, an entry that is no place, one a later stage shows, and words that are no reference: refused.
        foreach (var made in new[] { $"{Story}/place-nowhere", $"{Story}/character-rostam", $"{Story}/place-later", "nowhere/place-university", "no reference", new string('a', 300) })
        {
            Assert.Equal($"{Page}?form=add&problem=affiliation#add", (await Send(mina, "add", ("place", "Somewhere"), ("institution", made))).Response.Headers.Location?.OriginalString);
        }

        Assert.Single(Numbers(await mina.Get(Page)));

        // Words alone are allowed, and stay words.
        await At(mina, InstitutionsSite.University, null);
        mine = Section(await mina.Get(Page), "mine");
        Assert.Single(Regex.Matches(mine, "/entity/place-university\"><strong>"));
        Assert.Equal(2, Numbers(await mina.Get(Page)).Length);
    }

    [Fact]
    public async Task Somebody_is_found_through_the_entry_they_chose_under_any_name_and_not_through_a_name_alone()
    {
        var (viewer, sara, dara, kian, omid) = (await Signed("Mina"), await Signed("Sara"), await Signed("Dara"), await Signed("Kian"), await Signed("Omid"));
        await At(sara, "Tus Academy", $"{Story}/place-university", ("since", "1998"), ("until", "2002"), ("calendar", "gregorian"));
        await At(dara, "the university of the club", $"{Club}/place-university");
        await At(kian, InstitutionsSite.University, $"{Club}/place-other");
        await At(omid, InstitutionsSite.University, null);

        // The entry of the story: who chose it, and who chose the entry the editors call the same place.
        var found = Found(await viewer.Get($"{Page}?entity={Story}/place-university"));
        Assert.StartsWith($"People at: {InstitutionsSite.University}", found, StringComparison.Ordinal);
        Assert.Contains("Sara – Tus Academy", found, StringComparison.Ordinal);
        Assert.Contains("Dara – the university of the club", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Kian", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Omid", found, StringComparison.Ordinal);

        // The same from the other side, and the year is asked of the times of each.
        found = Found(await viewer.Get($"{Page}?entity={Club}/place-university&year=2000&question=during&calendar=gregorian"));
        Assert.Contains("Sara", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Kian", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", Found(await viewer.Get($"{Page}?entity={Club}/place-university&year=2010&question=during&calendar=gregorian")), StringComparison.Ordinal);

        // The other university of the same name is another place.
        found = Found(await viewer.Get($"{Page}?entity={Club}/place-other"));
        Assert.Contains("Kian", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Dara", found, StringComparison.Ordinal);

        // An identifier that is only close joins nothing: the two companies are two places.
        await At(sara, "the works", $"{Story}/place-company");
        Assert.DoesNotContain("Sara", Found(await viewer.Get($"{Page}?entity={Club}/place-company")), StringComparison.Ordinal);

        // Words find statements of the same words, and the page offers the entries of that name beside them.
        var byWords = await viewer.Get($"{Page}?place={Uri.EscapeDataString(InstitutionsSite.University)}");
        found = Found(byWords);
        Assert.Contains("Omid", found, StringComparison.Ordinal);
        Assert.Contains("Kian", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", found, StringComparison.Ordinal);
        Assert.DoesNotContain("Dara", found, StringComparison.Ordinal);
        Assert.Contains("Entries of the collections go by that name too", found, StringComparison.Ordinal);
        foreach (var entry in new[] { $"{Story}/place-university", $"{Club}/place-university", $"{Club}/place-other" })
        {
            Assert.Contains($"account/affiliations?entity={Uri.EscapeDataString(entry)}#find", byWords.Links);
        }

        // An entry that is none asks nothing.
        Assert.Empty(Found(await viewer.Get($"{Page}?entity={Story}/place-later")));
        Assert.Empty(Found(await viewer.Get($"{Page}?entity={Story}/character-rostam")));
    }

    [Fact]
    public async Task Two_stretches_at_one_entry_are_two_statements_and_the_same_statement_twice_is_one()
    {
        var mina = await Signed("Mina");
        var entry = $"{Story}/place-university";

        await At(mina, "first time", entry, ("since", "1998"), ("until", "2002"), ("calendar", "gregorian"));
        await At(mina, "second time", entry, ("since", "2010"), ("until", "2012"), ("calendar", "gregorian"));
        Assert.Equal(2, Numbers(await mina.Get(Page)).Length);

        // The same entry, role and times once more – under other words – states nothing new.
        Assert.Equal($"{Page}?form=add&problem=affiliation#add", (await Send(mina, "add", ("place", "again"), ("institution", entry), ("since", "1998"), ("until", "2002"), ("calendar", "gregorian"))).Response.Headers.Location?.OriginalString);

        // The years between the two are no attendance.
        var viewer = await Signed("Sara");
        Assert.Contains("Mina", Found(await viewer.Get($"{Page}?entity={entry}&year=2011&question=during&calendar=gregorian")), StringComparison.Ordinal);
        Assert.DoesNotContain("Mina", Found(await viewer.Get($"{Page}?entity={entry}&year=2005&question=during&calendar=gregorian")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_statement_can_be_pointed_at_an_entry_later_and_be_words_again()
    {
        var mina = await Signed("Mina");
        var number = await At(mina, "Tus Academy of the Tests", null);

        // Looking for the place of one statement offers the entries in the form of that statement only.
        var looked = await mina.Get($"{Page}?at=Tus+Academy+of+the+Tests&for={number}");
        Assert.Equal([$"{Story}/place-university", "none"], Choices(Section(looked, "mine")));
        Assert.Empty(Choices(Section(looked, "add")));
        Assert.Matches($"<li id=\"affiliation-{number}\">.*<details open", Regex.Replace(Section(looked, "mine"), "\\s+", " "));

        Assert.Equal($"{Page}?said=changed#mine", (await Send(mina, "change", ("affiliation", number), ("place", "Tus Academy of the Tests"), ("institution", $"{Story}/place-university"))).Response.Headers.Location?.OriginalString);
        var mine = Section(await mina.Get(Page), "mine");
        Assert.Contains($"<a href=\"{Story}/entity/place-university\"", mine, StringComparison.Ordinal);

        // The form of a statement that points at an entry offers to keep it – chosen – or to keep words only.
        Assert.Equal([$"{Story}/place-university", "none"], Choices(mine));
        Assert.Matches($"value=\"{Story}/place-university\" checked", mine);

        Assert.Equal($"{Page}?said=changed#mine", (await Send(mina, "change", ("affiliation", number), ("place", "Tus Academy of the Tests"), ("institution", "none"))).Response.Headers.Location?.OriginalString);
        Assert.DoesNotContain("/entity/place-university", Section(await mina.Get(Page), "mine"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_entry_that_is_withdrawn_leaves_the_words_and_the_statement_keeps_pointing_at_it()
    {
        var mina = await Signed("Mina");
        var number = await At(mina, "my old university", $"{Story}/place-university");
        var me = Regex.Match((await mina.Get("/en/account/people")).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;

        using var withdrawn = _site.Withdrawn();
        var again = withdrawn.Visitor();
        await again.Post("/preferences/account/sign-in", Account, ("name", "Mina"), ("password", Password), ("returnUrl", Account));

        // The page of the account and the page others see say the words, and no link leads to a page that is not there.
        var mine = Section(await again.Get(Page), "mine");
        Assert.DoesNotContain("/entity/place-university", mine, StringComparison.Ordinal);
        Assert.Contains("my old university", Words(mine), StringComparison.Ordinal);
        Assert.Contains("The entry this points at is not published at the moment.", Words(mine), StringComparison.Ordinal);
        var person = await again.Get($"/en/people/{me}");
        Assert.Contains("my old university", person.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("/entity/place-university", person.Html, StringComparison.Ordinal);

        // The statement keeps what it points at through a change of something else …
        Assert.Equal([$"{Story}/place-university", "none"], Choices(mine));
        Assert.Contains("Keep the entry it points at (it is not published at the moment)", Words(mine), StringComparison.Ordinal);
        Assert.Equal($"{Page}?said=changed#mine", (await again.Post($"{Forms}/change", Page, ("affiliation", number), ("place", "my old university"), ("role", "Student"), ("institution", $"{Story}/place-university"), ("returnUrl", Page))).Response.Headers.Location?.OriginalString);

        // … and no other statement can be pointed at it meanwhile.
        Assert.Equal($"{Page}?form=add&problem=affiliation#add", (await again.Post($"{Forms}/add", Page, ("place", "Somewhere"), ("institution", $"{Story}/place-university"), ("returnUrl", Page))).Response.Headers.Location?.OriginalString);

        // Published again, the statement is what it was.
        var back = Section(await mina.Get(Page), "mine");
        Assert.Contains($"<a href=\"{Story}/entity/place-university\"", back, StringComparison.Ordinal);
        Assert.Contains("– Student", Words(back), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_of_a_place_leads_to_who_states_to_have_been_there_and_names_nobody()
    {
        var sara = await Signed("Sara");
        await At(sara, "Tus Academy", $"{Story}/place-university");

        var page = await _site.Visitor().Get($"/en/{Story}/entity/place-university");
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"account/affiliations?entity={Uri.EscapeDataString($"{Story}/place-university")}#find", page.Links);
        Assert.Contains("People who state they were here", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", page.Main, StringComparison.Ordinal);

        // Who is not signed in is sent to sign in; a character is no place.
        Assert.Equal(HttpStatusCode.Redirect, (await _site.Visitor().Get($"{Page}?entity={Story}/place-university")).Status);
        Assert.DoesNotContain("account/affiliations", (await _site.Visitor().Get($"/en/{Story}/entity/character-rostam")).Main, StringComparison.Ordinal);

        // The page of a person who shows the place to everybody leads to the entry.
        var me = Regex.Match((await sara.Get("/en/account/people")).Html, "href=\"people/([a-f0-9]{32})\"[^>]*>Your page").Groups[1].Value;
        Assert.Contains($"{Story}/entity/place-university", (await (await Signed("Mina")).Get($"/en/people/{me}")).Links);
    }

    [Fact]
    public async Task Colleagues_can_say_at_which_entry_and_two_employers_of_one_name_are_two()
    {
        const string People = "/en/account/people";
        var (mina, sara) = (await Signed("Mina"), await Signed("Sara"));

        // Looking for the place offers the entries in the form that asks, and chooses none.
        var looked = Section(await mina.Get($"{People}?at=made-up+university+of+tus"), "ask");
        Assert.Equal([$"{Story}/place-university", $"{Club}/place-university", $"{Club}/place-other", "none"], Choices(looked));
        Assert.Matches("id=\"ask-place\"[^>]*value=\"made-up university of tus\"", looked);

        async Task<string?> Ask(string entry) =>
            (await mina.Post("/preferences/people/ask", People, ("name", "Sara"), ("kind", "colleague"), ("place", "the university"), ("institution", entry), ("returnUrl", People))).Response.Headers.Location?.OriginalString;

        // An entry a form makes up is refused; one the page offers is asked of the other side.
        Assert.Equal($"{People}?form=ask&problem=terms#ask", await Ask($"{Story}/character-rostam"));
        Assert.Equal($"{People}?said=asked", await Ask($"{Story}/place-university"));

        // While it waits, the place leads to its page too – for who is asked, where "which university" is the
        // question, on the page of the people and on My World; and for who asked.
        var link = $"colleague, <a href=\"{Story}/entity/place-university\">";
        Assert.Contains(link, Section(await sara.Get(People), "waiting"), StringComparison.Ordinal);
        Assert.Contains(link, Section(await sara.Get("/en/account/world"), "waiting"), StringComparison.Ordinal);
        Assert.Contains(link, Section(await mina.Get(People), "asked"), StringComparison.Ordinal);
        var id = Regex.Match((await sara.Get(People)).Html, "name=\"relationship\" value=\"([a-f0-9]{32})\"").Groups[1].Value;
        await sara.Post("/preferences/people/answer", People, ("relationship", id), ("answer", "accept"), ("returnUrl", People));

        // The relationship is written with what the entry is called today, not with the words of who asked.
        var mine = Words(Section(await mina.Get(People), "mine"));
        Assert.Contains(InstitutionsSite.University, mine, StringComparison.Ordinal);
        Assert.DoesNotContain("the university,", mine, StringComparison.Ordinal);

        // And the name leads to the page of the entry, for both sides; the kind before it and nothing after it changed.
        foreach (var side in new[] { mina, sara })
        {
            var held = Section(await side.Get(People), "mine");
            Assert.Contains($"colleague, <a href=\"{Story}/entity/place-university\">", held, StringComparison.Ordinal);
            Assert.Contains($"colleague, {InstitutionsSite.University}", Words(held), StringComparison.Ordinal);
        }

        // Colleagues at the other university of the same name are another relationship; the same entry again is not.
        Assert.Equal($"{People}?said=asked", await Ask($"{Club}/place-other"));
        Assert.NotEqual($"{People}?said=asked", await Ask($"{Story}/place-university"));

        // Words in place of the entry are another statement: the other side is asked, as for every change.
        Assert.Equal($"{People}?said=change-asked#mine", (await mina.Post("/preferences/people/change", People, ("relationship", id), ("kind", "colleague"), ("place", "the university"), ("institution", "none"), ("returnUrl", People))).Response.Headers.Location?.OriginalString);
        Assert.Equal($"{People}?form=mine&problem=terms#mine", (await mina.Post("/preferences/people/change", People, ("relationship", id), ("kind", "colleague"), ("place", "x"), ("institution", $"{Story}/place-later"), ("returnUrl", People))).Response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task The_choice_is_written_in_persian_too()
    {
        var mina = await Signed("Mina");

        var add = Words(Section(await mina.Get($"/fa/account/affiliations?at={Uri.EscapeDataString("دانشگاه ساختگی توس")}"), "add"));

        Assert.Contains("یکی از این‌هاست؟", add, StringComparison.Ordinal);
        Assert.Contains("هیچ‌کدام؛ فقط همان نوشتهٔ خودم بماند", add, StringComparison.Ordinal);
        Assert.Contains("جست‌وجوی جا با نام آن", add, StringComparison.Ordinal);
    }
}
