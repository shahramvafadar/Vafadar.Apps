using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site, told a folder for its legal texts: terms in both languages, a privacy notice in English, no imprint.</summary>
public sealed class LegalSite : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-legal-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Legal", _folder);
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "terms.en.txt"), "# What you may do\nThe first line of a paragraph\nand its second line.\n\nA <b>second</b> paragraph.\n# Ending\nThe last one.\n");
        File.WriteAllText(Path.Combine(_folder, "terms.fa.txt"), "# آنچه می‌توانید بکنید\nمتن آزمون.\n");
        File.WriteAllText(Path.Combine(_folder, "privacy.en.txt"), "What the site keeps: test data.\n");
        File.WriteAllText(Path.Combine(_folder, "imprint.en.txt"), "\n\n");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}

/// <summary>
/// The legal pages of the site (step 18.3.4): the site has the places and names them at the foot of every
/// page; the words come from a folder it is told, and a text that is not written is said to be missing.
/// </summary>
public sealed class LegalPagesTests(LegalSite legal, DemoSite demo) : IClassFixture<LegalSite>, IClassFixture<DemoSite>
{
    [Fact]
    public async Task A_site_without_a_folder_for_them_has_no_legal_pages_and_names_none()
    {
        var visitor = demo.Visitor();

        Assert.DoesNotContain("legal-links", (await visitor.Get("/en/")).Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/terms")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/privacy")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/imprint")).Status);
    }

    [Theory]
    [InlineData("/en/", "Terms of use", "Privacy", "Imprint")]
    [InlineData("/fa/story-rostam-sohrab", "شرایط استفاده", "حریم خصوصی", "مشخصات گرداننده")]
    public async Task Every_page_names_the_legal_pages_at_its_foot_in_its_language(string address, string terms, string privacy, string imprint)
    {
        var page = await legal.Visitor().Get(address);

        var foot = WebUtility.HtmlDecode(page.Html[page.Html.IndexOf("<footer", StringComparison.Ordinal)..]);
        Assert.Matches($"<a href=\"terms\">{terms}</a>.*<a href=\"privacy\">{privacy}</a>.*<a href=\"imprint\">{imprint}</a>", foot.ReplaceLineEndings(" "));
    }

    [Fact]
    public async Task A_text_is_shown_as_its_owner_wrote_it_with_headings_and_paragraphs_and_nothing_of_it_as_markup()
    {
        var page = await legal.Visitor().Get("/en/terms");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Terms of use</h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains("<h2>What you may do</h2>", page.Html, StringComparison.Ordinal);
        Assert.Contains("<p>The first line of a paragraph and its second line.</p>", page.Html, StringComparison.Ordinal);
        Assert.Contains("<p>A &lt;b&gt;second&lt;/b&gt; paragraph.</p>", page.Html, StringComparison.Ordinal);
        Assert.Contains("<h2>Ending</h2>", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("not written yet", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_text_is_in_the_language_of_the_page_and_one_that_is_not_written_is_said_to_be_missing()
    {
        var visitor = legal.Visitor();

        Assert.Contains("آنچه می‌توانید بکنید", (await visitor.Get("/fa/terms")).Main, StringComparison.Ordinal);
        Assert.Contains("What the site keeps: test data.", (await visitor.Get("/en/privacy")).Main, StringComparison.Ordinal);

        // No Persian privacy notice, and an imprint that is an empty file: the page is there and says so.
        var missing = await visitor.Get("/fa/privacy");
        Assert.Equal(HttpStatusCode.OK, missing.Status);
        Assert.Contains("این متن هنوز نوشته نشده است", missing.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("What the site keeps", missing.Main, StringComparison.Ordinal);
        Assert.Contains("This text is not written yet", (await visitor.Get("/en/imprint")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_folder_gives_nothing_but_the_three_pages()
    {
        var visitor = legal.Visitor();

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/terms.en.txt")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/legal")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/terms/more")).Status);
    }
}
