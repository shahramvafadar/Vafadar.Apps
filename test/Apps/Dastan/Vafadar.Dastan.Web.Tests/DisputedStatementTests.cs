using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// A disputed statement is said to be disputed wherever it is shown, and a withdrawn one is told by the history
/// with its reason (step 10.2, GOV-004). The published site of the tests disputes who Rostam's father is
/// (relation-02) and the first step of the telling (event-01).
/// </summary>
public sealed class DisputedStatementTests(PublishedSite published, DatedSite dated) : IClassFixture<PublishedSite>, IClassFixture<DatedSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Mark = "<span class=\"disputed\">Disputed</span>";
    private const string Sign = "<span class=\"disputed-sign\" title=\"Disputed\">";

    [Theory]
    [InlineData("entity/character-rostam")]
    [InlineData("entity/character-rostam/connections")]
    [InlineData("entity/character-rostam/family")]
    public async Task A_disputed_relation_is_marked_in_every_list_it_stands_in(string address)
    {
        var page = await published.Visitor().Get($"/en/{Story}/{address}");

        // Of the relations of Rostam at the opening, one is disputed – and only that one is marked.
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal(1, Count(page.Main, Mark));
    }

    [Fact]
    public async Task The_drawing_of_the_connections_marks_the_disputed_relation_and_the_page_says_what_the_mark_means()
    {
        var page = await published.Visitor().Get($"/en/{Story}/entity/character-rostam/connections");

        Assert.Equal(1, Count(page.Main, Sign));
        Assert.Contains("the sources disagree about this statement", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_family_drawing_draws_a_disputed_descent_with_another_line()
    {
        var page = await published.Visitor().Get($"/en/{Story}/entity/character-rostam/family");

        Assert.Contains("graph-disputed", page.Main, StringComparison.Ordinal);
        Assert.Contains("the sources disagree about this statement", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_page_without_a_disputed_statement_has_no_mark_and_no_legend()
    {
        var page = await published.Visitor().Get($"/en/{Story}/entity/place-samangan/connections");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("disputed", page.Main, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_disputed_step_is_marked_in_the_telling_and_in_its_chart()
    {
        var page = await published.Visitor().Get($"/en/{Story}/story");

        Assert.Equal(1, Count(page.Main, Mark));
        Assert.Equal(1, Count(page.Main, Sign));
    }

    [Theory]
    [InlineData("relation/relation-02")]
    [InlineData("event/event-01")]
    public async Task The_evidence_of_a_disputed_statement_says_what_the_sources_disagree_about(string statement)
    {
        var page = await published.Visitor().Get($"/en/{Story}/evidence/{statement}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(Mark, page.Main, StringComparison.Ordinal);
        Assert.Contains("What the sources disagree about", page.Text, StringComparison.Ordinal);
        Assert.Contains("Two manuscripts tell this differently.", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("an editor", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_evidence_of_a_settled_statement_names_no_dispute()
    {
        var page = await published.Visitor().Get($"/en/{Story}/evidence/relation/relation-08");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("disputed", page.Main, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_mark_is_Persian_on_a_Persian_page()
    {
        var page = await published.Visitor().Get($"/fa/{Story}/entity/character-rostam");

        Assert.Contains("<span class=\"disputed\">مورد اختلاف</span>", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_withdrawal_is_told_by_the_history_with_its_reason_as_far_as_the_visitor_has_read()
    {
        var visitor = published.Visitor();

        var opening = await visitor.Get($"/en/{Story}/revisions");
        await visitor.ReadTo(4);
        var end = await visitor.Get($"/en/{Story}/revisions");

        Assert.Contains("Withdrawn", opening.Text, StringComparison.Ordinal);
        Assert.Contains("Brother of", opening.Text, StringComparison.Ordinal);
        Assert.Matches("Reason:\\s+The passage names another man\\.", opening.Text);
        Assert.DoesNotContain("A reason that tells the ending.", opening.Html, StringComparison.Ordinal);
        Assert.Contains("A reason that tells the ending.", end.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_disputed_date_is_marked_in_the_chart_and_the_table_and_a_withdrawn_one_is_not_shown()
    {
        var page = await dated.Visitor().Get($"/en/{Story}/timeline");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("tl-disputed", page.Main, StringComparison.Ordinal);
        Assert.Contains(Mark, page.Main, StringComparison.Ordinal);
        Assert.Contains("the sources disagree about this statement", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("A lost first version", page.Html, StringComparison.Ordinal);
    }

    private static int Count(string text, string part) => text.Split(part).Length - 1;
}
