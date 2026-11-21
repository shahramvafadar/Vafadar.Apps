using System.Buffers.Binary;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The logo on the site (ADR 0026): drawn in the head of every page from the one geometry of the brand, named
/// once, never mirrored; the icons of the tab and of the installed app as the assets of the brand made them.
/// </summary>
public sealed class LogoTests(DemoSite site) : IClassFixture<DemoSite>
{
    private static readonly string Repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".."));
    private static readonly string Brand = Path.Combine(Repository, "branding", "dastan");
    private static readonly SKColor Indigo = new(0x3b, 0x3a, 0x8f);

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    public async Task The_head_of_a_page_shows_the_logo_and_names_it_once(string language)
    {
        var page = await site.Visitor().Get($"/{language}/");
        var link = Regex.Match(page.Html, "<a class=\"wordmark\"[^>]*>.*?</a>", RegexOptions.Singleline).Value;

        // The link is named; the drawing in it is silent, so the name is said once and not twice.
        Assert.StartsWith("<a class=\"wordmark\" href=\"./\" aria-label=\"Dastan\">", link, StringComparison.Ordinal);
        Assert.Contains($"<svg class=\"logo\" viewBox=\"0 0 {BrandGeometry.LockupWidth} {BrandGeometry.Height}\" aria-hidden=\"true\" focusable=\"false\">", link, StringComparison.Ordinal);
        Assert.Equal(string.Empty, Regex.Replace(link, "<[^>]+>", string.Empty).Trim());

        // The symbol – the open D, the page, the dot, apart from each other – and the wordmark as outlines.
        Assert.Equal(2, Regex.Matches(Regex.Match(link, "<g class=\"logo-symbol\">.*?</g>", RegexOptions.Singleline).Value, "<path ").Count);
        Assert.Single(Regex.Matches(link, "<circle "));
        Assert.Contains("<path class=\"logo-word\" fill-rule=\"evenodd\"", link, StringComparison.Ordinal);

        // A drawing: no text that a font would have to be there for, no picture of pixels, nothing fetched, and
        // nothing that turns it around on a page written from right to left.
        Assert.DoesNotMatch("<text|<image|href=\"http|transform", link);
    }

    [Fact]
    public void The_site_draws_the_geometry_of_the_brand_and_no_other()
    {
        using var geometry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Brand, "source", "dastan-geometry.json")));
        var symbol = geometry.RootElement.GetProperty("symbol");

        Assert.Equal(BrandGeometry.Letter, symbol.GetProperty("letter").GetString());
        Assert.Equal(BrandGeometry.Page, symbol.GetProperty("page").GetString());
        Assert.Equal(BrandGeometry.DotRadius, symbol.GetProperty("dot").GetProperty("r").GetSingle());
        Assert.Equal(BrandGeometry.Wordmark, geometry.RootElement.GetProperty("wordmark").GetProperty("outline").GetString());
        Assert.Equal(BrandGeometry.LockupWidth, geometry.RootElement.GetProperty("lockup").GetProperty("width").GetInt32());
        Assert.Equal("#3B3A8F", BrandGeometry.Indigo);
        Assert.Equal("#1C1B2E", BrandGeometry.Ink);

        // The masters say the same: they are written from the geometry, and nobody changed one by hand.
        foreach (var master in new[] { "dastan-symbol.svg", "dastan-logo.svg" })
        {
            var text = File.ReadAllText(Path.Combine(Brand, "source", master));
            Assert.Contains(BrandGeometry.Letter, text, StringComparison.Ordinal);
            Assert.Equal(master == "dastan-logo.svg", text.Contains(BrandGeometry.Wordmark, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void No_asset_of_the_logo_holds_pixels_a_script_a_font_or_anything_fetched()
    {
        var drawings = Directory.EnumerateFiles(Brand, "*.svg", SearchOption.AllDirectories).ToList();
        Assert.True(drawings.Count >= 12, $"Only {drawings.Count} drawings.");
        foreach (var drawing in drawings)
        {
            var text = File.ReadAllText(drawing);
            Assert.Matches("^<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 \\d+ \\d+\"", text);
            Assert.DoesNotMatch("<image|<script|<text|<style|font-family|href=|url\\(|<foreignObject", text);
            Assert.Single(Regex.Matches(text, "<circle "));
        }
    }

    [Fact]
    public async Task The_icons_of_the_site_are_those_the_brand_made()
    {
        var visitor = site.Visitor();
        foreach (var (address, made) in new[]
        {
            ("/favicon.svg", "exports/svg/favicon.svg"), ("/favicon.ico", "exports/ico/favicon.ico"),
            ("/icons/icon-192.png", "exports/icons/icon-192.png"), ("/icons/icon-512.png", "exports/icons/icon-512.png"),
            ("/icons/icon-maskable-192.png", "exports/icons/icon-maskable-192.png"), ("/icons/icon-maskable-512.png", "exports/icons/icon-maskable-512.png"),
            ("/icons/apple-touch-icon.png", "exports/icons/apple-touch-icon.png"),
        })
        {
            var answer = await visitor.Get(address);
            Assert.Equal(HttpStatusCode.OK, answer.Status);
            Assert.Equal(File.ReadAllBytes(Path.Combine(Brand, made.Replace('/', Path.DirectorySeparatorChar))), await answer.Bytes());
        }

        // The page names the icon as pixels, the icon as a drawing and the icon of a home screen; the earlier icon is gone.
        var page = await visitor.Get("/en/");
        Assert.Contains("<link rel=\"icon\" href=\"/favicon.ico\" sizes=\"48x48\"", page.Html, StringComparison.Ordinal);
        Assert.Matches("<link rel=\"icon\" href=\"[^\"]*favicon[^\"]*\\.svg\" type=\"image/svg(\\+|&#x2B;)xml\"", page.Html);
        Assert.Contains("<link rel=\"apple-touch-icon\" href=\"/icons/apple-touch-icon.png\"", page.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/icon.svg")).Status);
    }

    [Fact]
    public async Task The_icon_of_a_tab_holds_three_sizes()
    {
        var icon = await (await site.Visitor().Get("/favicon.ico")).Bytes();

        Assert.Equal((0, 1, 3), (BinaryPrimitives.ReadUInt16LittleEndian(icon), BinaryPrimitives.ReadUInt16LittleEndian(icon.AsSpan(2)), BinaryPrimitives.ReadUInt16LittleEndian(icon.AsSpan(4))));
        var sizes = new List<int>();
        for (var entry = 0; entry < 3; entry++)
        {
            var at = 6 + (entry * 16);
            var (length, offset) = (BinaryPrimitives.ReadInt32LittleEndian(icon.AsSpan(at + 8)), BinaryPrimitives.ReadInt32LittleEndian(icon.AsSpan(at + 12)));
            using var picture = SKBitmap.Decode(icon.AsSpan(offset, length));
            Assert.Equal((icon[at], icon[at + 1]), ((byte)picture.Width, (byte)picture.Height));
            sizes.Add(picture.Width);
        }

        Assert.Equal([16, 32, 48], sizes);
    }

    [Theory]
    [InlineData("/icons/icon-192.png", 192, false)]
    [InlineData("/icons/icon-512.png", 512, false)]
    [InlineData("/icons/icon-maskable-192.png", 192, true)]
    [InlineData("/icons/icon-maskable-512.png", 512, true)]
    [InlineData("/icons/apple-touch-icon.png", 180, true)]
    public async Task An_icon_has_its_size_its_ground_and_the_whole_symbol_where_a_platform_leaves_it(string address, int size, bool whole)
    {
        using var icon = SKBitmap.Decode(await (await site.Visitor().Get(address)).Bytes());
        Assert.Equal((size, size), (icon.Width, icon.Height));

        // An icon a platform cuts fills its square with the indigo of the brand, corners and all; the other has
        // corners of its own and nothing outside them.
        var corner = icon.GetPixel(0, 0);
        Assert.Equal(whole ? (byte)255 : (byte)0, corner.Alpha);
        if (whole)
        {
            Assert.Equal(Indigo, corner);
        }

        Assert.Equal(Indigo, icon.GetPixel(size / 2, size / 20));

        // The symbol is white and whole: a D that reaches high and low, and nothing of it near the edge.
        var (least, most, furthest) = (size, 0, 0d);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pixel = icon.GetPixel(x, y);
                if (pixel.Alpha == 255 && pixel.Red > 200 && pixel.Green > 200)
                {
                    (least, most) = (Math.Min(least, y), Math.Max(most, y));
                    furthest = Math.Max(furthest, Math.Sqrt(Math.Pow(x + 0.5 - (size / 2d), 2) + Math.Pow(y + 0.5 - (size / 2d), 2)));
                }
            }
        }

        Assert.InRange((most - least) / (double)size, 0.45, 0.62);
        if (address.Contains("maskable", StringComparison.Ordinal))
        {
            // Inside the circle a platform never cuts: 40 % of the square from its middle.
            Assert.True(furthest <= size * 0.4, $"The symbol reaches {furthest / size:P1} from the middle.");
        }
    }
}
