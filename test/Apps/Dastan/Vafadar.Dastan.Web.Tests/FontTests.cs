using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The bundled Persian font (ADR 0004, D-036): it comes from the site itself, with its license.</summary>
public sealed class FontTests(DemoSite site) : IClassFixture<DemoSite>
{
    // The file of the release 33.003 of the font, as it was published.
    private const string Published = "cdef5578ea583313cbea060a14aa107955a99717a861971b860cb564dfe1bbf5";

    [Fact]
    public async Task The_font_is_served_by_the_site_unchanged_and_with_its_license()
    {
        var visitor = site.Visitor();

        var font = await visitor.Get("/fonts/vazirmatn-nl-wght.woff2");
        var license = await visitor.Get("/fonts/vazirmatn-OFL.txt");

        Assert.Equal(HttpStatusCode.OK, font.Status);
        Assert.Equal("font/woff2", font.Response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Published, Convert.ToHexStringLower(SHA256.HashData(await font.Bytes())));
        Assert.Equal(HttpStatusCode.OK, license.Status);
        Assert.Contains("SIL OPEN FONT LICENSE Version 1.1", license.Html, StringComparison.Ordinal);
        Assert.Contains("The Vazirmatn Project Authors", license.Html, StringComparison.Ordinal);
    }

    // The file InterVariable.woff2 of the release 4.1 of Inter, as it was published (D-097).
    private const string InterPublished = "693b77d4f32ee9b8bfc995589b5fad5e99adf2832738661f5402f9978429a8e3";

    [Fact]
    public async Task The_Latin_font_is_served_by_the_site_unchanged_and_with_its_license()
    {
        var visitor = site.Visitor();

        var font = await visitor.Get("/fonts/inter-wght.woff2");
        var license = await visitor.Get("/fonts/inter-OFL.txt");

        Assert.Equal(HttpStatusCode.OK, font.Status);
        Assert.Equal("font/woff2", font.Response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(InterPublished, Convert.ToHexStringLower(SHA256.HashData(await font.Bytes())));
        Assert.Equal(HttpStatusCode.OK, license.Status);
        Assert.Contains("SIL OPEN FONT LICENSE Version 1.1", license.Html, StringComparison.Ordinal);
        Assert.Contains("The Inter Project Authors", license.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task The_styles_of_a_page_name_the_font_of_the_site_and_ask_no_other_site_for_anything(string language)
    {
        var visitor = site.Visitor();
        var page = await visitor.Get($"/{language}/");
        var sheets = Regex.Matches(page.Html, "<link rel=\"stylesheet\" href=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToList();
        Assert.Equal(2, sheets.Count);

        var styles = string.Empty;
        foreach (var sheet in sheets)
        {
            // A style sheet is addressed from the root of the site: it is the same in every language.
            var answer = await visitor.Get(new Uri(new Uri("http://localhost/"), sheet).AbsolutePath);
            Assert.Equal(HttpStatusCode.OK, answer.Status);
            styles += answer.Html;
        }

        Assert.Matches("@font-face\\s*\\{\\s*font-family: \"Vazirmatn\";\\s*src: url\\(\"fonts/vazirmatn-nl-wght\\.woff2\"\\) format\\(\"woff2\"\\);", styles);
        Assert.Matches("@font-face\\s*\\{\\s*font-family: \"Inter\";\\s*src: url\\(\"fonts/inter-wght\\.woff2\"\\) format\\(\"woff2\"\\);", styles);
        Assert.DoesNotMatch("url\\(\\s*[\"']?(https?:)?//", styles);
        Assert.DoesNotContain("@import", styles, StringComparison.Ordinal);
        Assert.DoesNotMatch("(src|href)=\"(https?:)?//", Regex.Replace(page.Html, "<a\\s[^>]*>|<link rel=\"(canonical|alternate)\"[^>]*>|<meta property=\"og:url\"[^>]*>", string.Empty));
    }
}
