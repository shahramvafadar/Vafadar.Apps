using System.Net;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The picture a link to the site is shown with (step 17.4, D-056).</summary>
public sealed class PreviewPictureTests(DemoSite demo) : IClassFixture<DemoSite>
{
    [Fact]
    public async Task The_site_draws_the_picture_a_link_is_shown_with()
    {
        var picture = await demo.Visitor().Get("/preview.png");

        Assert.Equal(System.Net.HttpStatusCode.OK, picture.Status);
        Assert.Equal("image/png", picture.Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=86400", picture.Response.Headers.CacheControl?.ToString());
        using var read = SkiaSharp.SKBitmap.Decode(await picture.Bytes());
        Assert.Equal((1200, 630), (read.Width, read.Height));

        // The ground of the mark at a corner, and the mark – something lighter – in the middle of the picture.
        Assert.Equal(new SkiaSharp.SKColor(0x2f, 0x2e, 0x7a), read.GetPixel(5, 5));
        Assert.Contains(Enumerable.Range(400, 400).Select(x => read.GetPixel(x, 315)), colour => colour.Red > 0xc0);
    }

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fa/story-rostam-sohrab")]
    public async Task A_page_names_the_picture_with_its_whole_address_and_its_size(string address)
    {
        var page = await demo.Visitor().Get(address);

        Assert.Contains("<meta property=\"og:image\" content=\"http://localhost/preview.png\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<meta property=\"og:image:width\" content=\"1200\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<meta property=\"og:image:height\" content=\"630\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"twitter:card\" content=\"summary_large_image\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_page_without_an_address_of_its_own_names_no_picture()
    {
        var page = await demo.Visitor().Get("/en/no-such-page");

        Assert.DoesNotContain("og:image", page.Html, StringComparison.Ordinal);
    }
}

/// <summary>
/// Pages that can be found and passed on (step 14.4): every page says where it is and where it is in the other
/// language, the pages of content say what they are about, a public site lists its pages for search engines –
/// only what a visitor sees who has read nothing yet – and a demonstration sends every engine away.
/// </summary>
public sealed class FindablePagesTests(DemoSite demo, PublishedSite published) : IClassFixture<DemoSite>, IClassFixture<PublishedSite>
{
    private const string Story = "story-rostam-sohrab";

    [Theory]
    [InlineData("en", "")]
    [InlineData("fa", Story)]
    [InlineData("en", Story + "/entity/character-rostam")]
    [InlineData("fa", Story + "/story")]
    public async Task Every_page_says_where_it_is_in_each_language(string language, string address)
    {
        var page = await demo.Visitor().Get($"/{language}/{address}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"<link rel=\"canonical\" href=\"http://localhost/{language}/{address}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"<link rel=\"alternate\" hreflang=\"en\" href=\"http://localhost/en/{address}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"<link rel=\"alternate\" hreflang=\"fa\" href=\"http://localhost/fa/{address}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"<meta property=\"og:url\" content=\"http://localhost/{language}/{address}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"<meta property=\"og:locale\" content=\"{language}\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_page_of_content_says_what_it_is_about_in_the_words_the_page_shows()
    {
        var visitor = demo.Visitor();
        var rostam = await visitor.Get($"/en/{Story}/entity/character-rostam");
        var start = await visitor.Get("/en/");
        var collection = await visitor.Get($"/en/{Story}");

        var description = Regex.Match(rostam.Html, "<meta name=\"description\" content=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(description);
        Assert.Contains(WebUtility.HtmlDecode(description).TrimEnd('…'), rostam.Text, StringComparison.Ordinal);
        Assert.Contains("<meta property=\"og:title\" content=\"Rostam\"", rostam.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"description\"", start.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"description\"", collection.Html, StringComparison.Ordinal);
        Assert.True(description.Length <= 201);
    }

    [Fact]
    public async Task A_page_that_is_not_there_has_no_address_and_a_search_is_not_kept()
    {
        var visitor = demo.Visitor();
        var missing = await visitor.Get($"/en/{Story}/entity/nobody");
        var search = await visitor.Get($"/en/{Story}/search?q=rostam");

        Assert.Equal(HttpStatusCode.NotFound, missing.Status);
        Assert.DoesNotContain("rel=\"canonical\"", missing.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("og:url", missing.Html, StringComparison.Ordinal);
        Assert.Contains("content=\"noindex, follow\"", search.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_demonstration_sends_every_engine_away_and_lists_nothing()
    {
        var visitor = demo.Visitor();
        var robots = await visitor.Get("/robots.txt");
        var sitemap = await visitor.Get("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, robots.Status);
        Assert.Equal("User-agent: *\nDisallow: /\n", robots.Html);
        Assert.DoesNotContain("Sitemap", robots.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, sitemap.Status);
    }

    [Fact]
    public async Task A_public_site_lets_engines_in_and_lists_its_pages_in_both_languages()
    {
        var visitor = published.Visitor();
        var robots = await visitor.Get("/robots.txt");
        var sitemap = await visitor.Get("/sitemap.xml");

        Assert.Contains("Allow: /\n", robots.Html, StringComparison.Ordinal);
        Assert.Contains("Disallow: /preferences/\n", robots.Html, StringComparison.Ordinal);
        Assert.Matches("Sitemap: https?://localhost/sitemap\\.xml\n", robots.Html);

        Assert.Equal(HttpStatusCode.OK, sitemap.Status);
        Assert.Equal("application/xml", sitemap.Response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", sitemap.Html, StringComparison.Ordinal);
        foreach (var address in new[] { "", Story, $"{Story}/story", $"{Story}/gallery", $"{Story}/revisions", $"{Story}/entity/character-rostam" })
        {
            Assert.Matches($"<loc>https?://localhost/en/{Regex.Escape(address)}</loc>", sitemap.Html);
            Assert.Matches($"<loc>https?://localhost/fa/{Regex.Escape(address)}</loc>", sitemap.Html);
        }

        Assert.Contains("hreflang=\"fa\"", sitemap.Html, StringComparison.Ordinal);

        // No address of a search, a form or a reading position.
        Assert.DoesNotContain("search", sitemap.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("preferences", sitemap.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_list_names_nothing_of_a_later_reading_stage()
    {
        var sitemap = (await published.Visitor().Get("/sitemap.xml")).Html;
        var catalogue = published.Catalogue;

        var later = catalogue.Entities.Where(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).ToList();
        Assert.NotEmpty(later);
        Assert.All(later, entity => Assert.DoesNotContain($"/entity/{entity.Id}<", sitemap, StringComparison.Ordinal));
        Assert.All(catalogue.Entities.Except(later), entity => Assert.Contains($"/entity/{entity.Id}<", sitemap, StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_style_of_the_site_has_rules_for_paper()
    {
        var page = await demo.Visitor().Get("/en/");
        var sheet = Regex.Match(page.Html, "href=\"(app[^\"]*\\.css)\"").Groups[1].Value;
        var css = (await demo.Visitor().Get("/" + sheet)).Html;

        Assert.Contains("@media print", css, StringComparison.Ordinal);
        Assert.Contains(".stage-edge", css[css.IndexOf("@media print", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }
}
