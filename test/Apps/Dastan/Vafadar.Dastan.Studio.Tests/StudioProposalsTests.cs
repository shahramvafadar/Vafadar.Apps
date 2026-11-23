using System.Net;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The places visitors proposed, in the tool of the editors (D-085): what waits – the same place proposed
/// several times as one –, the way to make its name, and the decision of an editor with the entry the place
/// has. Test data.
/// </summary>
public sealed class StudioProposalsTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 11, 23);
    private readonly Studio _studio;
    private readonly Desk _desk;
    private readonly ProposalBox _box;

    public StudioProposalsTests()
    {
        _studio = new Studio(accounts: false) { Reports = true };
        _desk = new Desk(_studio);
        _box = new ProposalBox(_studio.Database);
    }

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
    }

    [Theory]
    [InlineData("en", "Places visitors proposed", "No proposal waits.")]
    [InlineData("fa", "مکان‌هایی که بازدیدکنندگان پیشنهاد داده‌اند", "پیشنهادی در انتظار نیست.")]
    public async Task The_tool_has_a_page_of_the_proposed_places_that_says_when_none_waits(string language, string title, string none)
    {
        var home = await _desk.Get($"/{language}/");
        var page = await _desk.Get($"/{language}/proposals");

        Assert.Contains("href=\"proposals\"", home.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Text, StringComparison.Ordinal);
        Assert.Contains(none, page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_tool_that_was_not_told_to_show_what_visitors_write_has_no_such_page()
    {
        using var plain = new Studio();
        using var desk = new Desk(plain);

        Assert.DoesNotContain("href=\"proposals\"", (await desk.Get("/en/")).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await desk.Get("/en/proposals")).Status);
    }

    [Fact]
    public async Task The_same_place_proposed_twice_stands_together_with_what_each_proposal_says()
    {
        _box.Take(new ProposedPlace("University of <b>Tests</b>", "university", "Tehran", "https://example.org/about", "I studied there."), "fa", Day);
        _box.Take(new ProposedPlace("university of <B>tests</B>.", "university"), "en", Day.AddDays(1));
        _box.Take(new ProposedPlace("A company", "company"), "en", Day.AddDays(1));

        var page = await _desk.Get("/en/proposals");

        Assert.Contains("Waiting: 3 – places: 2", page.Text, StringComparison.Ordinal);
        Assert.Contains("proposed 2 times", page.Text, StringComparison.Ordinal);
        Assert.Contains("University of <b>Tests</b>", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Tests</b>", page.Html, StringComparison.Ordinal);
        Assert.Contains("Tehran", page.Text, StringComparison.Ordinal);
        Assert.Contains("I studied there.", page.Text, StringComparison.Ordinal);

        // The address a visitor wrote is opened apart and vouched for by nobody.
        Assert.Contains("href=\"https://example.org/about\" rel=\"external nofollow noopener noreferrer\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("nobody has checked it", page.Text, StringComparison.Ordinal);

        // The way to make the name begins with what was proposed.
        Assert.Contains($"href=\"{Studio.Collection}/records/entities/new?type=University&amp;label=University%20of%20%3Cb%3ETests%3C%2Fb%3E\"", page.Html, StringComparison.Ordinal);
        var start = await _desk.Get($"/en/{Studio.Collection}/records/entities/new?type=University&label=A%20place%20of%20the%20tests");
        var inputs = Desk.Standing(start.Html).GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => input.First().Value, StringComparer.Ordinal);
        Assert.Equal(("University", "A place of the tests"), (inputs["f.type"], inputs["f.label"]));

        // A type that is none is not taken: the form begins as it always did.
        var other = await _desk.Get($"/en/{Studio.Collection}/records/entities/new?type=Nothing");
        Assert.Equal("Person", Desk.Standing(other.Html).First(input => input.Name == "f.type").Value);
    }

    [Fact]
    public async Task A_place_that_was_added_is_noted_with_its_entry_and_every_proposal_of_it_is_decided()
    {
        _box.Take(new ProposedPlace("The court of the tests", "organisation"), "en", Day);
        _box.Take(new ProposedPlace("the Court of the Tests!", "organisation"), "en", Day);
        _box.Take(new ProposedPlace("Another place", "company"), "en", Day);

        // The place is decided by any of its proposals; its entry is asked for under the first of them as they wait.
        var first = _box.Waiting().First(proposal => proposal.Kind == "organisation");
        var other = _box.Waiting().Last(proposal => proposal.Kind == "organisation");

        // An entry the store does not have is said so, and nothing is decided; what was typed stays in the form.
        var wrong = await _desk.Submit("/en/proposals", "decide", ("Deciding.Added", first.Id), ($"entry-{first.Id}", $"{Studio.Collection}/nobody"), ("Deciding.By", "a reviewer"));
        Assert.True(wrong.Arrived is null && wrong.Text.Contains("The store has no name", StringComparison.Ordinal), wrong.Arrived + " | " + wrong.Text[..Math.Min(900, wrong.Text.Length)]);
        Assert.Contains($"The store has no name “{Studio.Collection}/nobody”", wrong.Text, StringComparison.Ordinal);
        Assert.Contains($"value=\"{Studio.Collection}/nobody\"", wrong.Html, StringComparison.Ordinal);
        Assert.Equal(3, _box.Waiting().Count);

        var done = await _desk.Submit("/en/proposals", "decide", ("Deciding.Added", other.Id), ($"entry-{first.Id}", $"{Studio.Collection}/character-rostam"), ("Deciding.By", "a reviewer"));

        Assert.Equal("/en/proposals?done=added", done.Arrived);
        Assert.Contains("Noted: the place was added.", done.Text, StringComparison.Ordinal);
        Assert.Equal("Another place", Assert.Single(_box.Waiting()).Name);
        Assert.All(_box.Decided(), proposal => Assert.Equal((ProposalOutcome.Added, $"{Studio.Collection}/character-rostam", "a reviewer"), (proposal.Outcome, proposal.Entry, proposal.SettledBy)));
        Assert.Equal(2, _box.Decided().Count);
        Assert.Contains($"href=\"{Studio.Collection}/record/entities/character-rostam\"", done.Html, StringComparison.Ordinal);
        Assert.Contains("decided by a reviewer", done.Text, StringComparison.Ordinal);

        // The store of the editors was not touched: a proposal changes nothing by itself.
        using var store = _studio.Store();
        Assert.DoesNotContain(store.History(Studio.Collection), change => (change.Reason ?? string.Empty).Contains("propos", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task A_place_that_gets_no_entry_a_proposal_decided_meanwhile_and_an_act_without_a_name_are_said_so()
    {
        _box.Take(new ProposedPlace("A place", "company"), "en", Day);
        var proposal = Assert.Single(_box.Waiting());

        var nameless = await _desk.Submit("/en/proposals", "decide", ("Deciding.Declined", proposal.Id), ("Deciding.By", " "));
        Assert.Null(nameless.Arrived);
        Assert.Single(_box.Waiting());

        var opened = await _desk.Get("/en/proposals");
        Assert.True(_box.Settle(proposal.Id, "somebody else", Day, ProposalOutcome.Added));
        var late = await _desk.Send(opened, "decide", ("Deciding.Declined", proposal.Id), ("Deciding.By", "a reviewer"));
        Assert.Contains("That proposal waits no longer", late.Text, StringComparison.Ordinal);
        Assert.Equal((ProposalOutcome.Added, "somebody else"), (_box.Decided()[0].Outcome, _box.Decided()[0].SettledBy));

        _box.Take(new ProposedPlace("A second place", "company"), "en", Day);
        var second = Assert.Single(_box.Waiting());
        var declined = await _desk.Submit("/en/proposals", "decide", ("Deciding.Declined", second.Id), ("Deciding.By", "a reviewer"));
        Assert.Equal("/en/proposals?done=declined", declined.Arrived);
        Assert.Contains("Noted: the place gets no entry.", declined.Text, StringComparison.Ordinal);
        Assert.Equal(ProposalOutcome.Declined, _box.Decided().Single(decided => decided.Name == "A second place").Outcome);
    }
}
