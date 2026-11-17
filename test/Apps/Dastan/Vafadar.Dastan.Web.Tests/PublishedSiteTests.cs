using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The site as it runs for the public, with a copy of the collection made into a published revision: approved,
/// numbered, every relation and event reviewed, and one diagram allowed for the public. The Persian text stays
/// the draft it is. The revision is test data – the collection of the repository is an editorial sample.
/// </summary>
public sealed class PublishedSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-published-" + Guid.NewGuid().ToString("N"), "content");

    /// <summary>The diagram of the copy that is allowed for the public.</summary>
    public const string PublicDiagram = "media-dastan-family-illustration";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Dastan:Content", _content);
        if (Directory.Exists(_content))
        {
            return;
        }

        // The collection and, beside it, the documents of the editors it points to for reviewers.
        var app = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan"));
        var copies = Path.GetDirectoryName(_content)!;
        foreach (var folder in new[] { "content", Path.Combine("docs", "product") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(app, folder), "*", SearchOption.AllDirectories))
            {
                var copy = Path.Combine(copies, Path.GetRelativePath(app, file));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy, overwrite: true);
            }
        }

        var path = Path.Combine(_content, "samples", "shahnameh-pilot.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var publication = sample["publication"]!.AsObject();
        publication["state"] = "published_revision";
        publication["publicReleaseApproved"] = true;
        publication["revision"] = new JsonObject { ["number"] = 3, ["publishedAt"] = "2026-11-17" };

        // What the three revisions changed: something of the opening, and something of a later part.
        publication["history"] = JsonNode.Parse(
            """
            [
              { "number": 1, "publishedAt": "2026-11-03", "changes": [] },
              { "number": 2, "publishedAt": "2026-11-10", "note": "A correction of the later part.", "changes": [
                { "kind": "changed", "record": "character-sohrab", "recordKind": "entity", "label": "Sohrab", "spoilerLevel": 2 } ] },
              { "number": 3, "publishedAt": "2026-11-17", "note": "The summary of Rostam was corrected.", "changes": [
                { "kind": "changed", "record": "character-rostam", "recordKind": "entity", "label": "Rostam", "spoilerLevel": 0 },
                { "kind": "added", "record": "character-sohrab-new-detail", "recordKind": "relation", "label": "Father of", "spoilerLevel": 2 },
                { "kind": "removed", "record": "character-earlier", "recordKind": "entity", "label": "An earlier name", "spoilerLevel": 0 } ] }
            ]
            """);
        foreach (var group in new[] { "relations", "events" })
        {
            foreach (var record in sample[group]!.AsArray())
            {
                record!["reviewStatus"] = "reviewed";
                record["review"] = new JsonObject { ["by"] = "an editor", ["on"] = "2026-11-16" };
            }
        }

        var diagram = sample["media"]!.AsArray().Single(item => (string?)item!["id"] == PublicDiagram)!;
        diagram["publicRenderEligibility"] = new JsonObject { ["allowed"] = true };
        File.WriteAllText(path, sample.ToJsonString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_content))
        {
            Directory.Delete(Path.GetDirectoryName(_content)!, recursive: true);
        }
    }
}

/// <summary>
/// What everybody may see (D-044): a published revision – with the reading position still in force, without the
/// media and the texts that are not for the public – and nothing of an editorial sample.
/// </summary>
public sealed class PublishedSiteTests(PublishedSite published, PublicSite unpublished) : IClassFixture<PublishedSite>, IClassFixture<PublicSite>
{
    private const string Story = "story-rostam-sohrab";

    [Fact]
    public async Task An_editorial_sample_is_not_shown_to_the_public_at_all()
    {
        var page = await unpublished.Visitor().Get($"/en/{Story}");

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
        Assert.DoesNotContain("Rostam", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_published_revision_is_shown_to_the_public_and_says_which_revision_it_is()
    {
        var page = await published.Visitor().Get($"/en/{Story}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1><bdi>Rostam and Sohrab</bdi></h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains("Published revision 3 of 2026-11-17. A correction is published as a new revision.", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Local demonstration", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_reading_position_holds_for_the_public_as_for_a_demonstration()
    {
        var visitor = published.Visitor();

        var early = await visitor.Get($"/en/{Story}/entity/character-sohrab");
        await visitor.ReadTo(2);
        var later = await visitor.Get($"/en/{Story}/entity/character-sohrab");

        Assert.Equal(HttpStatusCode.NotFound, early.Status);
        Assert.Equal(HttpStatusCode.OK, later.Status);
    }

    [Fact]
    public async Task The_public_sees_the_media_allowed_for_the_public_and_no_other()
    {
        var visitor = published.Visitor();
        await visitor.ReadTo(4);

        var gallery = await visitor.Get($"/en/{Story}/gallery");
        var allowed = await visitor.Get($"/en/{Story}/media/{PublishedSite.PublicDiagram}");
        var demonstrationOnly = await visitor.Get($"/en/{Story}/media/media-dastan-parentage-video");
        var file = await visitor.Get($"/media/{Story}/media-dastan-parentage-video/file");

        Assert.Contains($"{Story}/media/{PublishedSite.PublicDiagram}", gallery.Links);
        Assert.DoesNotContain($"{Story}/media/media-dastan-parentage-video", gallery.Links);
        Assert.Equal(HttpStatusCode.OK, allowed.Status);
        Assert.Equal(HttpStatusCode.NotFound, demonstrationOnly.Status);
        Assert.Equal(HttpStatusCode.NotFound, file.Status);
    }

    [Fact]
    public async Task A_draft_translation_does_not_reach_the_public()
    {
        var page = await published.Visitor().Get($"/fa/{Story}");
        var text = WebUtility.HtmlDecode(page.Html);

        // The page is Persian; the texts of the collection are the reviewed ones – the English ones – and no
        // page tells the public about a draft.
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Equal("fa", page.Root("lang"));
        Assert.Contains("Rostam and Sohrab", text, StringComparison.Ordinal);
        Assert.DoesNotContain("رستم و سهراب", text, StringComparison.Ordinal);
        Assert.DoesNotContain("پیش‌نویس", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_draft_comparison_is_not_shown_to_the_public()
    {
        var visitor = published.Visitor();

        var start = await visitor.Get("/en/");
        var comparison = await visitor.Get("/en/compare/iran-china-rome");

        Assert.DoesNotContain("compare/iran-china-rome", start.Links);
        Assert.Equal(HttpStatusCode.NotFound, comparison.Status);
    }
}
