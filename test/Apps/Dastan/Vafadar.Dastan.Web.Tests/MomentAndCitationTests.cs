using System.Net;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// A moment of a video has an address (step 14.6, MED-014), and a statement is cited with the revision it was
/// read in (step 14.7, R-PUB-07).
/// </summary>
public sealed class MomentAndCitationTests(DemoSite demo, PublishedSite published) : IClassFixture<DemoSite>, IClassFixture<PublishedSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Video = "media-dastan-parentage-video";
    private const string Diagram = "media-dastan-family-illustration";

    // The video belongs to a later part of the story: a visitor who has read to the end.
    private async Task<Browser> Reader()
    {
        var visitor = demo.Visitor();
        await visitor.ReadTo(4);
        return visitor;
    }

    [Theory]
    [InlineData("75", 75)]
    [InlineData("1:15", 75)]
    [InlineData("01:02:03", 3723)]
    [InlineData("0:05", 5)]
    [InlineData("86400", 86400)]
    public void A_moment_is_seconds_or_minutes_and_seconds(string written, int seconds)
    {
        Assert.Equal(seconds, VideoMoment.Read(written));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0:00")]
    [InlineData("-5")]
    [InlineData("1:75")]
    [InlineData("1:2:3:4")]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("86401")]
    [InlineData("999999999")]
    [InlineData("1::5")]
    [InlineData("١:١٥")]
    public void What_is_no_time_is_no_moment(string? written)
    {
        Assert.Null(VideoMoment.Read(written));
    }

    [Theory]
    [InlineData(5, "0:05")]
    [InlineData(75, "1:15")]
    [InlineData(3723, "1:02:03")]
    public void A_moment_is_written_as_a_person_reads_it(int seconds, string written)
    {
        Assert.Equal(written, VideoMoment.Write(seconds));
        Assert.Equal(seconds, VideoMoment.Read(written));
    }

    [Theory]
    [InlineData("en", "?t=1:15", "The video starts at 1:15.", "From the beginning")]
    [InlineData("en", "?t=75", "The video starts at 1:15.", "From the beginning")]
    [InlineData("fa", "?t=1:15", "ویدئو از ۱:۱۵ آغاز می‌شود.", "از آغاز")]
    public async Task The_page_of_a_video_starts_at_the_moment_its_address_names(string language, string query, string starts, string beginning)
    {
        var page = await (await Reader()).Get($"/{language}/{Story}/media/{Video}{query}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Matches("<source src=\"[^\"#]+#t=75\"", page.Html);
        Assert.Contains(starts, page.Main, StringComparison.Ordinal);
        Assert.Contains(beginning, page.Main, StringComparison.Ordinal);
        Assert.Contains("name=\"t\" value=\"1:15\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?t=abc")]
    [InlineData("?t=1:99")]
    [InlineData("?t=%22%3E%3Cscript%3E")]
    public async Task Without_a_moment_the_video_starts_at_its_beginning_and_the_form_asks_for_one(string query)
    {
        var page = await (await Reader()).Get($"/en/{Story}/media/{Video}{query}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("#t=", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("The video starts at", page.Main, StringComparison.Ordinal);
        Assert.Contains("The address of a moment", page.Main, StringComparison.Ordinal);
        Assert.Contains($"action=\"{Story}/media/{Video}\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("&lt;script", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_form_keeps_the_gallery_the_visitor_came_from_and_an_image_has_no_moment()
    {
        var reader = await Reader();
        var video = await reader.Get($"/en/{Story}/media/{Video}?entity=character-rostam&t=10");
        var image = await reader.Get($"/en/{Story}/media/{Diagram}?t=10");

        Assert.Contains("<input type=\"hidden\" name=\"entity\" value=\"character-rostam\"", video.Html, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/media/{Video}?entity=character-rostam\"", video.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, image.Status);
        Assert.DoesNotContain("name=\"t\"", image.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("#t=", image.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en", "To cite this", "“Rostam”. Dastan, revision 3 of 2026-11-17.")]
    [InlineData("fa", "برای استناد", "ویرایش ۳ به تاریخ ۲۰۲۶-۱۱-۱۷")]
    public async Task A_statement_of_a_published_site_is_cited_with_its_revision(string language, string title, string cited)
    {
        var page = await published.Visitor().Get($"/{language}/{Story}/evidence/entity/character-rostam");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(cited, page.Main, StringComparison.Ordinal);
        Assert.Matches($"https?://localhost/{language}/{Story}/evidence/entity/character-rostam\\?revision=3<", page.Html);
        Assert.DoesNotContain("This address was written for", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_address_written_for_an_earlier_revision_says_that_a_later_one_is_shown()
    {
        var visitor = published.Visitor();
        var earlier = await visitor.Get($"/en/{Story}/evidence/entity/character-rostam?revision=2");
        var same = await visitor.Get($"/en/{Story}/evidence/entity/character-rostam?revision=3");
        var none = await visitor.Get($"/en/{Story}/evidence/entity/character-rostam?revision=abc");

        Assert.Contains("This address was written for revision 2. The site now shows revision 3", earlier.Main, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Story}/revisions\"", earlier.Html, StringComparison.Ordinal);
        Assert.Contains("revision=3<", earlier.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("This address was written for", same.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("This address was written for", none.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("abc", none.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_relation_is_cited_as_the_sentence_it_is_and_a_demonstration_has_nothing_to_cite()
    {
        var relation = published.Catalogue.Relations.First(candidate => candidate.Level.CompareTo(Core.ReadingLevel.Opening) == 0);
        var cited = await published.Visitor().Get($"/en/{Story}/evidence/relation/{relation.Id}");
        var demonstration = await demo.Visitor().Get($"/en/{Story}/evidence/entity/character-rostam");

        Assert.Contains("To cite this", cited.Main, StringComparison.Ordinal);
        Assert.Contains($"/evidence/relation/{relation.Id}?revision=3", cited.Html, StringComparison.Ordinal);
        Assert.Contains(" – ", cited.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("To cite this", demonstration.Main, StringComparison.Ordinal);
    }
}
