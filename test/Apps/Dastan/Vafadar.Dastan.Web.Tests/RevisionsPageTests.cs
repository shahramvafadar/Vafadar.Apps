using System.Net;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The page "what changed" (step 10.1, GOV-005): the revisions of a collection, newest first, with what each
/// changed – as far as the visitor has read, and not a word further.
/// </summary>
public sealed class RevisionsPageTests(PublishedSite published, DemoSite demonstration) : IClassFixture<PublishedSite>, IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";

    [Fact]
    public async Task The_note_that_names_the_revision_leads_to_what_the_revisions_changed()
    {
        var page = await published.Visitor().Get($"/en/{Story}");

        Assert.Contains($"{Story}/revisions", page.Links);
        Assert.Contains("Revisions and what they changed", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_revisions_are_listed_newest_first_with_their_day_and_their_note()
    {
        var page = await published.Visitor().Get($"/en/{Story}/revisions");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Revisions of Rostam and Sohrab</h1>", page.Html, StringComparison.Ordinal);
        var third = page.Main.IndexOf("id=\"revision-3\"", StringComparison.Ordinal);
        var second = page.Main.IndexOf("id=\"revision-2\"", StringComparison.Ordinal);
        var first = page.Main.IndexOf("id=\"revision-1\"", StringComparison.Ordinal);
        Assert.True(third >= 0 && third < second && second < first);
        Assert.Contains("The summary of Rostam was corrected.", page.Text, StringComparison.Ordinal);
        Assert.Contains("The first publication.", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_reader_at_the_opening_is_told_of_no_change_of_a_later_part()
    {
        var page = await published.Visitor().Get($"/en/{Story}/revisions");

        // Rostam is of the opening and has a page; the removed name has none any more.
        Assert.Contains($"{Story}/entity/character-rostam", page.Links);
        Assert.Contains("An earlier name", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("character-earlier", page.Main, StringComparison.Ordinal);

        // Sohrab is of a later part: neither his name nor the relation, and revision 2 says only that nothing
        // that can be seen was changed.
        Assert.DoesNotContain("Sohrab", page.Main.Replace("Rostam and Sohrab", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain("Father of", page.Main, StringComparison.Ordinal);
        Assert.Contains("Nothing that can be seen at your reading position was changed.", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_reader_of_the_later_part_is_told_of_its_changes()
    {
        var visitor = published.Visitor();
        await visitor.ReadTo(2);

        var page = await visitor.Get($"/en/{Story}/revisions");

        Assert.Contains($"{Story}/entity/character-sohrab", page.Links);
        Assert.Contains("Father of", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Nothing that can be seen at your reading position was changed.", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_is_there_in_Persian_and_names_no_reviewer()
    {
        var page = await published.Visitor().Get($"/fa/{Story}/revisions");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal("rtl", page.Root("dir"));
        Assert.DoesNotContain("an editor", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Revisions of", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_collection_that_is_no_revision_says_so_and_another_collection_is_not_found()
    {
        var visitor = demonstration.Visitor();

        var page = await visitor.Get($"/en/{Story}/revisions");
        var other = await visitor.Get("/en/another-story/revisions");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("This collection is not published as a revision yet.", page.Text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, other.Status);
    }
}
