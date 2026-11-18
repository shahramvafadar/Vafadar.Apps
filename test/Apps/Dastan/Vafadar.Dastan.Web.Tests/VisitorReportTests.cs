using System.Net;
using Microsoft.AspNetCore.Hosting;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>The demonstration site, told a folder for the reports of its visitors.</summary>
public sealed class ReportingSite : DemoSite
{
    /// <summary>Gets the folder the reports are kept in.</summary>
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "dastan-site-reports-" + Guid.NewGuid().ToString("N"));

    /// <summary>Gets the reports that wait.</summary>
    public IReadOnlyList<VisitorReport> Waiting => new ReportBox(Folder).Waiting();

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseSetting("Dastan:Reports", Folder);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(Folder))
        {
            Directory.Delete(Folder, recursive: true);
        }
    }
}

/// <summary>
/// A visitor reports an error (step 14.8, R-GOV-03): the form on the page of the sources of a statement, what
/// is kept, and what is refused. Every test has a site of its own, so that one test's reports are not another's.
/// </summary>
public sealed class VisitorReportTests : IDisposable
{
    private const string Story = "story-rostam-sohrab";
    private const string Rostam = $"/en/{Story}/evidence/entity/character-rostam";
    private readonly ReportingSite _site = new();

    public void Dispose() => _site.Dispose();

    private static (string, string)[] Report(string text, string statement = "evidence/entity/character-rostam", string website = "") =>
        [("collection", Story), ("statement", statement), ("text", text), ("website", website), ("returnUrl", Rostam)];

    [Theory]
    [InlineData("en", "Something is wrong here?", "no name, no address")]
    [InlineData("fa", "چیزی این‌جا نادرست است؟", "بدون نام و بدون نشانی")]
    public async Task The_page_of_the_sources_of_a_statement_offers_the_form_in_the_language_of_the_page(string language, string title, string privacy)
    {
        var page = await _site.Visitor().Get($"/{language}/{Story}/evidence/entity/character-rostam");

        Assert.Contains(title, page.Main, StringComparison.Ordinal);
        Assert.Contains(privacy, page.Main, StringComparison.Ordinal);
        Assert.Contains("<form method=\"post\" action=\"/report\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"statement\" value=\"evidence/entity/character-rostam\"", page.Html, StringComparison.Ordinal);
        Assert.Contains($"maxlength=\"{ReportBox.LongestText}\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", page.Own, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_that_was_told_no_folder_offers_no_form_and_takes_no_report()
    {
        using var plain = new DemoSite();
        var visitor = plain.Visitor();
        var page = await visitor.Get(Rostam);
        var sent = await visitor.Post("/report", Rostam, Report("Words."));

        Assert.DoesNotContain("action=\"/report\"", page.Html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, sent.Status);
    }

    [Fact]
    public async Task A_report_is_kept_with_its_statement_and_the_visitor_is_thanked()
    {
        var visitor = _site.Visitor();
        var sent = await visitor.Post("/report", Rostam, Report("  The summary names the wrong father.  "));

        Assert.Equal(HttpStatusCode.Redirect, sent.Status);
        Assert.Equal($"{Rostam}?reported=thanks#report", sent.Response.Headers.Location!.OriginalString);

        var report = Assert.Single(_site.Waiting);
        Assert.Equal((Story, "en", "evidence/entity/character-rostam", "The summary names the wrong father."), (report.Collection, report.Language, report.Statement, report.Text));
        Assert.Null(report.Revision);

        // Nothing of the visitor is in the file: no address of the machine, no browser.
        var file = File.ReadAllText(Assert.Single(Directory.GetFiles(_site.Folder)));
        Assert.DoesNotContain("127.0.0.1", file, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost", file, StringComparison.Ordinal);

        var thanked = await visitor.Get($"{Rostam}?reported=thanks");
        Assert.Contains("Thank you. The editors will read it.", thanked.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("action=\"/report\"", thanked.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_report_from_a_Persian_page_about_a_relation_names_both()
    {
        var relation = _site.Catalogue.Relations.First(candidate => candidate.Level.CompareTo(Core.ReadingLevel.Opening) == 0).Id.Value;
        var from = $"/fa/{Story}/evidence/relation/{relation}";
        var sent = await _site.Visitor().Post("/report", from, ("collection", Story), ("statement", $"evidence/relation/{relation}"), ("text", "این رابطه درست نیست."), ("website", ""), ("returnUrl", from + "?from=x#top"));

        Assert.Equal($"{from}?from=x&reported=thanks#report", sent.Response.Headers.Location!.OriginalString);
        var report = Assert.Single(_site.Waiting);
        Assert.Equal(("fa", $"evidence/relation/{relation}", "این رابطه درست نیست."), (report.Language, report.Statement, report.Text));
    }

    [Fact]
    public async Task A_statement_the_visitor_has_not_read_to_cannot_be_reported()
    {
        var later = _site.Catalogue.Entities.First(entity => entity.Level.CompareTo(Core.ReadingLevel.Opening) > 0).Id.Value;
        var sent = await _site.Visitor().Post("/report", Rostam, Report("I know what comes.", $"evidence/entity/{later}"));

        // Refused like any form that is no form of the site, and nothing is kept.
        Assert.Equal(HttpStatusCode.SeeOther, sent.Status);
        Assert.Empty(_site.Waiting);
    }

    [Theory]
    [InlineData("", "evidence/entity/character-rostam", Story)]
    [InlineData("   ", "evidence/entity/character-rostam", Story)]
    [InlineData("Words.", "evidence/entity/nobody", Story)]
    [InlineData("Words.", "../../etc/passwd", Story)]
    [InlineData("Words.", "evidence/entity/character-rostam", "another-story")]
    public async Task What_is_no_report_is_refused_and_nothing_is_kept(string text, string statement, string collection)
    {
        var sent = await _site.Visitor().Post("/report", Rostam, ("collection", collection), ("statement", statement), ("text", text), ("website", ""), ("returnUrl", Rostam));

        Assert.Equal(HttpStatusCode.SeeOther, sent.Status);
        Assert.Empty(_site.Waiting);
    }

    [Fact]
    public async Task Words_longer_than_the_form_allows_and_a_form_without_its_token_are_refused()
    {
        var visitor = _site.Visitor();
        var tooLong = await visitor.Post("/report", Rostam, Report(new string('a', ReportBox.LongestText + 1)));
        using var client = _site.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var content = new FormUrlEncodedContent(Report("Words.").Select(field => KeyValuePair.Create(field.Item1, field.Item2)));
        using var bare = await client.PostAsync(new Uri("/report", UriKind.Relative), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.SeeOther, tooLong.Status);
        Assert.Equal(HttpStatusCode.SeeOther, bare.StatusCode);
        Assert.Empty(_site.Waiting);
    }

    [Fact]
    public async Task A_program_that_fills_in_the_field_nobody_sees_is_thanked_and_nothing_is_kept()
    {
        var sent = await _site.Visitor().Post("/report", Rostam, Report("Buy something.", website: "https://spam.example"));

        Assert.Equal($"{Rostam}?reported=thanks#report", sent.Response.Headers.Location!.OriginalString);
        Assert.Empty(_site.Waiting);
    }

    [Fact]
    public async Task More_reports_in_an_hour_than_a_person_writes_are_asked_to_wait()
    {
        var visitor = _site.Visitor();
        for (var number = 0; number < ReportDesk.PerHour; number++)
        {
            var sent = await visitor.Post("/report", Rostam, Report($"Report {number}."));
            Assert.EndsWith("reported=thanks#report", sent.Response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        }

        var more = await visitor.Post("/report", Rostam, Report("One more."));

        Assert.Equal($"{Rostam}?reported=later#report", more.Response.Headers.Location!.OriginalString);
        Assert.Equal(ReportDesk.PerHour, _site.Waiting.Count);
        var page = await visitor.Get($"{Rostam}?reported=later");
        Assert.Contains("Please try again later; nothing was kept this time.", page.Main, StringComparison.Ordinal);
        Assert.Contains("action=\"/report\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_address_that_says_something_else_about_a_report_says_nothing()
    {
        var page = await _site.Visitor().Get($"{Rostam}?reported=%3Cb%3E");

        Assert.DoesNotContain("Thank you", page.Main, StringComparison.Ordinal);
        Assert.Contains("action=\"/report\"", page.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_return_address_of_a_report_is_an_address_of_the_site()
    {
        var sent = await _site.Visitor().Post("/report", Rostam, ("collection", Story), ("statement", "evidence/entity/character-rostam"), ("text", "Words."), ("website", ""), ("returnUrl", "https://elsewhere.example/"));

        Assert.Equal("/?reported=thanks#report", sent.Response.Headers.Location!.OriginalString);
    }
}
