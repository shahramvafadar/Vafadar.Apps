using System.Net;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// A visitor proposes a place to the editors (D-085, ADR 0023): the form on the page of the places somebody
/// was at, what is kept of a proposal – the place, never who proposed it –, what is refused, and the word a
/// statement in words gets when an entry of a collection goes by those words. Every test has a site of its own.
/// </summary>
public sealed class PlaceProposalPagesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Account = "/en/account";
    private const string Page = "/en/account/affiliations";
    private const string Forms = "/preferences/affiliations";
    private readonly InstitutionsSite _site = new() { TakesProposals = true };

    public void Dispose() => _site.Dispose();

    private IReadOnlyList<PlaceProposal> Waiting => new ProposalBox(_site.Database).Waiting();

    private static async Task<Browser> Signed(InstitutionsSite site, string name)
    {
        var visitor = site.Visitor();
        var made = await visitor.Post("/preferences/account/register", Account, ("name", name), ("age", "on"), ("terms", "on"), ("password", Password), ("again", Password), ("returnUrl", Account));
        Assert.Contains("said=welcome", made.Response.Headers.Location?.OriginalString, StringComparison.Ordinal);
        return visitor;
    }

    private static Task<Page> Propose(Browser visitor, params (string, string)[] fields) =>
        visitor.Post($"{Forms}/propose", Page, [.. fields, ("returnUrl", Page)]);

    private static string Words(Page page) => Regex.Replace(page.Text, "\\s+", " ");

    [Theory]
    [InlineData("en", "Propose a place to the editors", "Propose this place")]
    [InlineData("fa", "پیشنهاد یک مکان به ویراستاران", "همین مکان را پیشنهاد بده")]
    public async Task The_page_of_the_places_offers_to_propose_one_with_the_kinds_people_can_have_been_at(string language, string title, string send)
    {
        var page = await (await Signed(_site, "Mina")).Get($"/{language}/account/affiliations");
        var html = WebUtility.HtmlDecode(page.Html);

        Assert.Contains($"<h2 id=\"propose-title\">{title}</h2>", html, StringComparison.Ordinal);
        Assert.Contains(send, html, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Forms}/propose\"", html, StringComparison.Ordinal);
        Assert.Equal(
            ["university", "company", "organisation", "sportsClub", "sportsTeam", "government"],
            Regex.Matches(html[html.IndexOf("id=\"propose-kind\"", StringComparison.Ordinal)..html.IndexOf("id=\"propose-where\"", StringComparison.Ordinal)], "<option value=\"([^\"]+)\"").Select(option => option.Groups[1].Value));
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_that_takes_nothing_for_the_editors_has_no_form_and_no_address_for_it()
    {
        using var plain = new InstitutionsSite();
        var mina = await Signed(plain, "Mina");

        Assert.DoesNotContain("id=\"propose\"", (await mina.Get(Page)).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await Propose(mina, ("name", "A place"), ("kind", "company"))).Status);

        // Looking for a place no collection has says so, and offers nothing more.
        Assert.DoesNotContain("Propose it to the editors", (await mina.Get($"{Page}?at=Nowhere%20College#add")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Who_looks_for_a_place_no_collection_has_is_led_to_propose_it_with_the_name_filled_in()
    {
        var mina = await Signed(_site, "Mina");
        var looked = await mina.Get($"{Page}?at=Nowhere%20College#add");

        Assert.Contains("No entry of the collections goes by “Nowhere College”.", Words(looked), StringComparison.Ordinal);
        Assert.Contains("href=\"account/affiliations?at=Nowhere%20College#propose\"", looked.Html, StringComparison.Ordinal);
        Assert.Matches("<input id=\"propose-name\" name=\"name\" type=\"text\" required maxlength=\"160\" value=\"Nowhere College\"", looked.Html);
    }

    [Fact]
    public async Task A_proposal_is_kept_as_the_place_and_says_nothing_about_who_sent_it()
    {
        var mina = await Signed(_site, "Mina of the Tests");
        var sent = await Propose(mina, ("name", "  Nowhere   College "), ("kind", "university"), ("where", "Tus"), ("page", "https://example.org/college"), ("note", "I studied there from 1998."));

        Assert.Equal($"{Page}?said=proposed#propose", sent.Response.Headers.Location?.OriginalString);
        var proposal = Assert.Single(Waiting);
        Assert.Equal(("Nowhere College", "university", "Tus", "https://example.org/college", "I studied there from 1998.", "en"),
            (proposal.Name, proposal.Kind, proposal.Where, proposal.Page!.AbsoluteUri, proposal.Note, proposal.Language));
        Assert.DoesNotContain("Mina", string.Join('|', proposal.Id, proposal.Name, proposal.Where, proposal.Note, proposal.Page), StringComparison.Ordinal);

        var after = await mina.Get($"{Page}?said=proposed");
        Assert.Contains("Thank you. The editors have your proposal; it says nothing about who sent it.", Words(after), StringComparison.Ordinal);

        // Proposing states nothing: the visitor has no place by it.
        Assert.Contains("You have stated no place yet.", after.Main, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "university", "")]
    [InlineData("A place", "family", "")]
    [InlineData("A place", "university", "http://example.org")]
    [InlineData("A place", "university", "not an address")]
    public async Task What_is_no_proposal_is_refused_with_the_name_kept_and_nothing_is_kept_of_it(string name, string kind, string page)
    {
        var mina = await Signed(_site, "Mina");
        var sent = await Propose(mina, ("name", name), ("kind", kind), ("page", page));
        var back = sent.Response.Headers.Location?.OriginalString ?? string.Empty;

        Assert.StartsWith($"{Page}?form=propose&problem=proposal", back, StringComparison.Ordinal);
        Assert.EndsWith("#propose", back, StringComparison.Ordinal);
        Assert.Empty(Waiting);

        var again = await mina.Get(back[..back.IndexOf('#', StringComparison.Ordinal)]);
        Assert.Contains("That could not be sent: a proposal needs the name of the place", Words(again), StringComparison.Ordinal);
        if (name.Length > 0)
        {
            Assert.Contains($"name=\"name\" type=\"text\" required maxlength=\"160\" value=\"{name}\"", again.Html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Only_who_signed_in_proposes_and_only_so_often_in_an_hour()
    {
        var stranger = _site.Visitor();
        var refused = await stranger.Post($"{Forms}/propose", "/en", ("name", "A place"), ("kind", "company"), ("returnUrl", Page));
        Assert.Equal("/refused", refused.Response.Headers.Location?.OriginalString);
        Assert.Empty(Waiting);

        var mina = await Signed(_site, "Mina");
        for (var index = 0; index < 5; index++)
        {
            Assert.Equal($"{Page}?said=proposed#propose", (await Propose(mina, ("name", $"Place {index}"), ("kind", "company"))).Response.Headers.Location?.OriginalString);
        }

        var late = await Propose(mina, ("name", "One more"), ("kind", "company"));
        Assert.Equal($"{Page}?said=proposal-later#propose", late.Response.Headers.Location?.OriginalString);
        Assert.Equal(5, Waiting.Count);
        Assert.Contains("Please try again in an hour.", Words(await mina.Get($"{Page}?said=proposal-later")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_place_in_words_that_an_entry_goes_by_now_says_so_and_leads_to_choosing_it()
    {
        var mina = await Signed(_site, "Mina");
        await mina.Post($"{Forms}/add", Page, ("place", InstitutionsSite.University), ("institution", "none"), ("returnUrl", Page));
        await mina.Post($"{Forms}/add", Page, ("place", "Nowhere College"), ("institution", "none"), ("returnUrl", Page));
        var page = await mina.Get(Page);
        var numbers = Regex.Matches(page.Html, "<li id=\"affiliation-(\\d+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(2, numbers.Count);

        // The words of one are the name of an entry; of the other nothing goes by its words. Nothing is joined
        // by a name: the page says what it found and the visitor chooses.
        var hint = Regex.Matches(page.Html, "An entry of a collection goes by these words now\\.").Count;
        Assert.Equal(1, hint);
        var link = Regex.Match(WebUtility.HtmlDecode(page.Html), "href=\"(account/affiliations\\?for=(\\d+)&at=[^\"]+#affiliation-\\d+)\"[^>]*>See whether it is your place, and choose it");
        Assert.True(link.Success);

        var looked = await mina.Get("/en/" + link.Groups[1].Value);
        Assert.Contains($"{InstitutionsSite.Story}/place-university", looked.Html, StringComparison.Ordinal);

        // Once the entry is chosen, the word is no longer said.
        await mina.Post($"{Forms}/change", Page, ("affiliation", link.Groups[2].Value), ("place", InstitutionsSite.University), ("institution", $"{InstitutionsSite.Story}/place-university"), ("returnUrl", Page));
        Assert.DoesNotContain("An entry of a collection goes by these words now.", (await mina.Get(Page)).Html, StringComparison.Ordinal);
    }
}
