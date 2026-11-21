using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Studio.Site;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The symbol of Dastan in the tool of the editors (ADR 0026): before the name of the tool in the head of every
/// page, drawn from the one geometry of the brand, silent beside a name that is written in words; and the icon
/// of a tab as the brand made it.
/// </summary>
public sealed class StudioSymbolTests(Studio studio) : IClassFixture<Studio>
{
    private static readonly string Repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".."));
    private static readonly string Brand = Path.Combine(Repository, "branding", "dastan");

    [Theory]
    [InlineData("en", "Dastan Studio")]
    [InlineData("fa", "استودیوی داستان")]
    public async Task The_head_shows_the_symbol_before_the_name_of_the_tool_and_says_the_name_once(string language, string name)
    {
        var page = await studio.Get($"/{language}/");
        var link = Regex.Match(page.Html, "<a class=\"wordmark\"[^>]*>.*?</a>", RegexOptions.Singleline).Value;

        // The drawing is silent and has no words; the name stands in words, in the language of the editor.
        Assert.Contains($"<svg class=\"logo\" viewBox=\"0 0 {BrandGeometry.SymbolWidth} {BrandGeometry.Height}\" aria-hidden=\"true\" focusable=\"false\">", link, StringComparison.Ordinal);
        Assert.Equal(name, WebUtility.HtmlDecode(Regex.Replace(link, "<[^>]+>", string.Empty)).Trim());
        Assert.DoesNotContain("aria-label", link, StringComparison.Ordinal);

        // The symbol alone – the open D, the page, the dot – and not the wordmark; nothing fetched, nothing turned.
        Assert.Equal(2, Regex.Matches(link, "<path ").Count);
        Assert.Single(Regex.Matches(link, "<circle "));
        Assert.DoesNotContain(BrandGeometry.Wordmark, link, StringComparison.Ordinal);
        Assert.DoesNotMatch("<text|<image|href=\"http|transform", link);
    }

    [Fact]
    public void The_tool_draws_the_geometry_of_the_brand_and_no_other()
    {
        using var geometry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Brand, "source", "dastan-geometry.json")));
        var symbol = geometry.RootElement.GetProperty("symbol");

        Assert.Equal(BrandGeometry.Letter, symbol.GetProperty("letter").GetString());
        Assert.Equal(BrandGeometry.Page, symbol.GetProperty("page").GetString());
        Assert.Equal(BrandGeometry.DotRadius, symbol.GetProperty("dot").GetProperty("r").GetSingle());
        Assert.Equal(BrandGeometry.SymbolWidth, symbol.GetProperty("width").GetInt32());
        Assert.Equal(BrandGeometry.Wordmark, geometry.RootElement.GetProperty("wordmark").GetProperty("outline").GetString());
    }

    [Fact]
    public async Task The_icon_of_a_tab_is_the_one_the_brand_made_also_for_somebody_who_is_not_signed_in()
    {
        var page = await studio.Get("/en/");
        Assert.Contains("<link rel=\"icon\" href=\"favicon.ico\" sizes=\"48x48\"", page.Html, StringComparison.Ordinal);
        Assert.Matches("<link rel=\"icon\" href=\"favicon[^\"]*\\.svg\" type=\"image/svg(\\+|&#x2B;)xml\"", page.Html);

        // A browser asks for the icon before anybody signed in: the tool with accounts answers it all the same.
        using var closed = new Studio(accounts: true);
        foreach (var tool in new[] { studio, closed })
        {
            using var client = tool.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            foreach (var (address, made) in new[] { ("/en/favicon.ico", "exports/ico/favicon.ico"), ("/en/favicon.svg", "exports/svg/favicon.svg") })
            {
                using var answer = await client.GetAsync(new Uri(address, UriKind.Relative), TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
                Assert.Equal(File.ReadAllBytes(Path.Combine(Brand, made.Replace('/', Path.DirectorySeparatorChar))), await answer.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
            }
        }
    }
}
