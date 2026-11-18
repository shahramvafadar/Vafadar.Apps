using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site with a diagram that names a smaller size of itself. Test data.</summary>
public sealed class SizedSite : DemoSite
{
    private readonly string _content = Path.Combine(Path.GetTempPath(), "dastan-sized-" + Guid.NewGuid().ToString("N"), "content");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

        // A smaller size of the diagram: here the poster of the video stands in for it – what matters is that the
        // record names a file of its own for a width.
        var path = Path.Combine(_content, "samples", "shahnameh-pilot.json");
        var sample = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var diagram = sample["media"]!.AsArray().Select(node => node!.AsObject()).First(item => (string?)item["id"] == "media-dastan-family-illustration");
        diagram["dimensions"] = new JsonObject { ["width"] = 1600, ["height"] = 900 };
        diagram["sizes"] = JsonNode.Parse("""[{ "width": 480, "url": "../media/dastan-parentage-poster.png" }]""");
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
/// The smaller sizes of the image of a media item on the pages (step 17.8, D-056): the page names them with
/// their widths, the browser takes the one that fits, and a size is given by the same rule as the image.
/// </summary>
public sealed class ImageSizesPagesTests(SizedSite sized, DemoSite demo) : IClassFixture<SizedSite>, IClassFixture<DemoSite>
{
    private const string Story = "story-rostam-sohrab";
    private const string Item = "media-dastan-family-illustration";

    [Fact]
    public async Task The_page_of_an_image_and_the_gallery_name_its_sizes_with_their_widths()
    {
        var visitor = sized.Visitor();
        await visitor.ReadTo(4);

        var page = await visitor.Get($"/en/{Story}/media/{Item}");
        var gallery = await visitor.Get($"/en/{Story}/gallery");

        var set = $"srcset=\"/media/{Story}/{Item}/size-0 480w, /media/{Story}/{Item}/file 1600w\"";
        Assert.Contains($"<img src=\"/media/{Story}/{Item}/file\" {set} sizes=\"(min-width: 64rem) 60rem, 100vw\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"{set} sizes=\"(min-width: 40rem) 20rem, 100vw\"", gallery.Html, StringComparison.Ordinal);

        // The size is a file of its own, and another format is still offered as before.
        var size = await visitor.Get($"/media/{Story}/{Item}/size-0");
        Assert.Equal(HttpStatusCode.OK, size.Status);
        Assert.Equal("image/png", size.Response.Content.Headers.ContentType?.MediaType);
        Assert.NotEqual((await (await visitor.Get($"/media/{Story}/{Item}/file")).Bytes()).Length, (await size.Bytes()).Length);
        Assert.Contains($"/media/{Story}/{Item}/rendition-0", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_size_is_given_to_who_may_see_the_image_and_a_size_that_is_not_named_is_nothing()
    {
        var visitor = sized.Visitor();

        // Before the image is reached in the story, nothing of it is given – no size either.
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/media/{Story}/{Item}/size-0")).Status);

        await visitor.ReadTo(4);
        Assert.Equal(HttpStatusCode.OK, (await visitor.Get($"/media/{Story}/{Item}/size-0")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/media/{Story}/{Item}/size-1")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/media/{Story}/{Item}/size-x")).Status);
    }

    [Fact]
    public async Task An_image_without_smaller_sizes_is_shown_as_before()
    {
        var visitor = demo.Visitor();
        await visitor.ReadTo(4);

        var page = await visitor.Get($"/en/{Story}/media/{Item}");

        Assert.Contains($"<img src=\"/media/{Story}/{Item}/file\" alt=", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("srcset", page.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get($"/media/{Story}/{Item}/size-0")).Status);
    }
}
