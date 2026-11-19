using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site, told a folder with legal texts of its own: terms in both languages, a privacy notice in English, an empty imprint.</summary>
public sealed class LegalSite : DemoSite
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-legal-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Legal", _folder);
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "terms.en.txt"), "# What you may do\nThe first line of a paragraph\nand its second line.\n\nA <b>second</b> paragraph.\n\n| A line\n| and the next\n\n- one\n- two\n## Smaller\n? Left out: {{operator.phone}}\nWrite to {{operator.mail}} or see https://example.org/page.\n# Ending\nThe last one.\n");
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

/// <summary>The demonstration site of an operator who said everything the legal texts ask for.</summary>
public sealed class NamedSite : DemoSite
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Dastan:Operator:Street", "1 Test Street");
        builder.UseSetting("Dastan:Operator:Town", "12345 Testtown");
        builder.UseSetting("Dastan:Operator:Country", "Germany");
        builder.UseSetting("Dastan:Operator:Host", "Test Hosting Ltd, Testtown");
        builder.UseSetting("Dastan:Operator:Network", "Test Network Ltd, Testtown");
        builder.UseSetting("Dastan:Operator:Authority", "The Test Authority, Testtown");
    }
}

/// <summary>
/// The legal pages of the site (row 18.8): terms, privacy notice, cookies and legal notice are part of the
/// site, in English, Persian and German, with what only the operator knows filled in from the settings; a
/// folder the site is told can put a text of its own in the place of one.
/// </summary>
public sealed class LegalPagesTests(LegalSite legal, DemoSite demo, NamedSite named) : IClassFixture<LegalSite>, IClassFixture<DemoSite>, IClassFixture<NamedSite>
{
    [Theory]
    [InlineData("terms")]
    [InlineData("privacy")]
    [InlineData("cookies")]
    [InlineData("imprint")]
    public async Task The_site_has_its_legal_pages_without_being_told_a_folder_in_three_languages(string page)
    {
        var visitor = demo.Visitor();

        foreach (var (address, language) in new[] { ($"/en/{page}", "en"), ($"/fa/{page}", "fa"), ($"/en/{page}?text=de", "de"), ($"/fa/{page}?text=en", "en") })
        {
            var shown = await visitor.Get(address);
            Assert.Equal(HttpStatusCode.OK, shown.Status);
            Assert.Contains($"<article class=\"legal\" lang=\"{language}\"", shown.Html, StringComparison.Ordinal);
            Assert.Contains($"href=\"{page}?text=de\"", shown.Html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task A_text_that_lacks_what_only_the_operator_knows_shows_the_gap_and_says_that_it_is_not_complete()
    {
        // The name, the site and the mail address are known; no address was given to this site.
        var imprint = await demo.Visitor().Get("/en/imprint");

        Assert.Contains("Shahram Vafadar", imprint.Main, StringComparison.Ordinal);
        Assert.Contains("info@dastan.online", imprint.Main, StringComparison.Ordinal);
        Assert.Contains("This text is not complete", imprint.Main, StringComparison.Ordinal);
        Assert.Contains("operator.street", imprint.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("Telephone", imprint.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task With_everything_said_no_text_has_a_gap_and_the_address_keeps_its_lines()
    {
        var texts = named.Services.GetRequiredService<LegalTexts>();
        Assert.Empty(texts.Missing());

        var imprint = await named.Visitor().Get("/en/imprint");
        Assert.DoesNotContain("This text is not complete", imprint.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", imprint.Html, StringComparison.Ordinal);
        Assert.Matches("<p class=\"lines\">\\s*Shahram Vafadar\\s*<br ?/?>\\s*1 Test Street\\s*<br ?/?>\\s*12345 Testtown\\s*<br ?/?>\\s*Germany\\s*</p>", imprint.Html);
        Assert.Contains("Test Hosting Ltd", (await named.Visitor().Get("/en/privacy")).Main, StringComparison.Ordinal);
        Assert.Contains("The Test Authority", (await named.Visitor().Get("/fa/privacy")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public void The_three_languages_of_a_text_say_the_same_in_the_same_order()
    {
        var texts = named.Services.GetRequiredService<LegalTexts>();
        foreach (var page in LegalTexts.Pages)
        {
            var english = texts.Of(page, "en")!;
            Assert.True(english.Blocks.Length > 10, page);
            foreach (var language in new[] { "fa", "de" })
            {
                var other = texts.Of(page, language)!;
                Assert.Equal(english.Blocks.Select(block => (block.Kind, block.Lines.Length)), other.Blocks.Select(block => (block.Kind, block.Lines.Length)));
            }
        }
    }

    [Theory]
    [InlineData("/en/", "Terms of use", "Privacy", "Cookies", "Report content")]
    [InlineData("/fa/story-rostam-sohrab", "شرایط استفاده", "حریم خصوصی", "کوکی", "گزارش محتوا")]
    public async Task Every_page_names_the_legal_pages_at_its_foot_in_its_language(string address, string terms, string privacy, string cookies, string notice)
    {
        var page = await demo.Visitor().Get(address);

        var foot = WebUtility.HtmlDecode(page.Html[page.Html.IndexOf("<footer", StringComparison.Ordinal)..]);
        Assert.Matches($"<a href=\"terms\">{terms}</a>.*<a href=\"privacy\">{privacy}.*<a href=\"cookies\">[^<]*{cookies}.*<a href=\"imprint\">.*<a href=\"notice\">{notice}</a>", foot.ReplaceLineEndings(" "));
    }

    [Fact]
    public async Task A_text_of_the_folder_is_shown_as_it_was_written_and_nothing_of_it_as_markup()
    {
        var page = await legal.Visitor().Get("/en/terms");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Terms of use</h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains("<h2>What you may do</h2>", page.Html, StringComparison.Ordinal);
        Assert.Contains("<p>The first line of a paragraph and its second line.</p>", page.Html, StringComparison.Ordinal);
        Assert.Contains("<p>A &lt;b&gt;second&lt;/b&gt; paragraph.</p>", page.Html, StringComparison.Ordinal);
        Assert.Matches("<p class=\"lines\">\\s*A line\\s*<br ?/?>\\s*and the next\\s*</p>", page.Html);
        Assert.Matches("<ul>\\s*<li>one</li>\\s*<li>two</li>\\s*</ul>", page.Html);
        Assert.Contains("<h3>Smaller</h3>", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("Left out", page.Html, StringComparison.Ordinal);
        Assert.Contains("href=\"mailto:info@dastan.online\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://example.org/page\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("<h2>Ending</h2>", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_text_of_the_folder_stands_in_the_place_of_the_one_of_the_site_for_its_page_and_language_only()
    {
        var visitor = legal.Visitor();

        Assert.Contains("آنچه می‌توانید بکنید", (await visitor.Get("/fa/terms")).Main, StringComparison.Ordinal);
        Assert.Contains("What the site keeps: test data.", (await visitor.Get("/en/privacy")).Main, StringComparison.Ordinal);

        // No Persian privacy notice in the folder: the one of the site is shown.
        var own = await visitor.Get("/fa/privacy");
        Assert.Equal(HttpStatusCode.OK, own.Status);
        Assert.DoesNotContain("What the site keeps", own.Main, StringComparison.Ordinal);
        Assert.Contains("GDPR", own.Main, StringComparison.Ordinal);

        // An imprint that is an empty file: the page is there and says that the text is not written.
        Assert.Contains("This text is not written yet", (await visitor.Get("/en/imprint")).Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_folder_gives_nothing_but_the_pages()
    {
        var visitor = legal.Visitor();

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/terms.en.txt")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/legal")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.Get("/en/terms/more")).Status);
    }
}
