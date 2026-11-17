using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Vafadar.Dastan.Store;
using Vafadar.Dastan.Store.Tests;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The tool of the editors, started in memory over a store that holds the collection of the repository after
/// some work: every statement reviewed and revision 1 published; then the summary of Rostam corrected, the
/// relation of Zal and Rostam changed and waiting for a review again, and one relation taken out.
/// </summary>
public sealed class Studio : WebApplicationFactory<Program>
{
    private readonly WorkingCopy _copy = new();

    public const string Collection = WorkingCopy.Collection;

    public Studio()
    {
        var day = new DateOnly(2026, 11, 17);
        using var store = _copy.Filled(files: true);
        foreach (var statement in store.Statements(Collection))
        {
            Assert.True(store.Review(Collection, statement.Statement, ReviewAct.Reviewed, "a reviewer", day, "Read against the text.").Done);
        }

        Assert.NotNull(store.Publish(Collection, day, "an editor", "The first publication.").Revision);
        _copy.Clock.Advance(TimeSpan.FromHours(2));

        var rostam = WorkingCopy.Stored(store, "entities", "character-rostam");
        rostam["summary"] = "A corrected summary.";
        Assert.True(store.Put(Collection, "entities", rostam, "a writer", "The summary said too much.").Done);

        var relation = WorkingCopy.Stored(store, "relations", "relation-02");
        relation["label"] = "Father and teacher of";
        relation["reviewStatus"] = "source_backed_editorial_review_pending";
        relation.Remove("review");
        Assert.True(store.Put(Collection, "relations", relation, "a writer", "The passage says more.").Done);

        Assert.True(store.Remove(Collection, "relation-18", "an editor", "Not told in the selected part.").Done);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Studio:Store", _copy.Database);
        builder.UseSetting("Studio:Files", _copy.Samples);
    }

    /// <summary>Asks the tool for a page.</summary>
    public async Task<StudioPage> Get(string address)
    {
        using var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(new Uri(address, UriKind.Relative));
        return new StudioPage(response.StatusCode, await response.Content.ReadAsStringAsync(), response.Headers.ToDictionary(header => header.Key, header => string.Join(", ", header.Value), StringComparer.OrdinalIgnoreCase));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _copy.Dispose();
        }
    }
}

/// <summary>A page of the tool as it was answered.</summary>
public sealed partial record StudioPage(HttpStatusCode Status, string Html, IReadOnlyDictionary<string, string> Headers)
{
    /// <summary>Gets the address an act led back to, when the page was reached that way.</summary>
    public string? Arrived { get; init; }

    /// <summary>Gets the text of the page: the markup of its main part without the tags, with single spaces.</summary>
    public string Text => Spaces().Replace(WebUtility.HtmlDecode(Tags().Replace(Main, " ")), " ").Trim();

    /// <summary>Gets the markup of the main part of the page.</summary>
    public string Main => MainPart().Match(Html).Value;

    /// <summary>Gets the addresses the page links to.</summary>
    public IEnumerable<string> Links => Anchors().Matches(Html).Select(match => WebUtility.HtmlDecode(match.Groups[1].Value));

    [GeneratedRegex("<main\\b.*?</main>", RegexOptions.Singleline)]
    private static partial Regex MainPart();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex("\\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex("<a\\s[^>]*href=\"([^\"]*)\"")]
    private static partial Regex Anchors();
}

public sealed class StudioTests(Studio studio) : IClassFixture<Studio>
{
    private const string Collection = Studio.Collection;

    // --- what the store holds ---

    [Fact]
    public async Task The_start_lists_the_collections_of_the_store_with_what_waits_and_what_was_published()
    {
        var page = await studio.Get("/en/");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Collections</h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains(Collection, page.Links);
        Assert.Contains("1 of 27", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_collection_shows_its_groups_with_their_counts_and_the_work_that_is_open()
    {
        var page = await studio.Get($"/en/{Collection}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains($"{Collection}/records/relations", page.Links);
        Assert.Matches("Relations\\s*19", page.Text);
        Assert.Matches("Steps of the story\\s*8", page.Text);
        Assert.Contains("1 of 27 statements wait for a review", page.Text, StringComparison.Ordinal);
        Assert.Contains("Published revisions: 1", page.Text, StringComparison.Ordinal);
        Assert.Contains("Translated into: fa", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_records_of_a_group_are_listed_in_the_editors_order_with_the_state_of_their_review()
    {
        var page = await studio.Get($"/en/{Collection}/records/relations");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1>Relations: 19</h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains($"{Collection}/record/relations/relation-02", page.Links);
        Assert.DoesNotContain($"{Collection}/record/relations/relation-18", page.Links);
        Assert.Equal(1, Count(page.Main, "class=\"review review-waiting\""));
        Assert.Equal(18, Count(page.Main, "class=\"review review-reviewed\""));
        Assert.True(page.Main.IndexOf("relation-01", StringComparison.Ordinal) < page.Main.IndexOf("relation-02", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_record_shows_what_it_says_and_every_change_with_who_when_why_before_and_after()
    {
        var page = await studio.Get($"/en/{Collection}/record/entities/character-rostam");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<h1><bdi>Rostam</bdi></h1>", page.Html, StringComparison.Ordinal);
        Assert.Contains("Version 2, last changed by a writer on 2026-11-17 11:00 UTC", page.Text, StringComparison.Ordinal);
        Assert.Contains("A corrected summary.", page.Text, StringComparison.Ordinal);

        // The newest change first, with its reason and the field that differs.
        Assert.True(page.Text.IndexOf("Changed version 2", StringComparison.Ordinal) < page.Text.IndexOf("Came in version 1", StringComparison.Ordinal));
        Assert.Contains("Reason: The summary said too much.", page.Text, StringComparison.Ordinal);
        var diff = Regex.Match(page.Main, "<table class=\"list diff\">.*?</table>", RegexOptions.Singleline).Value;
        Assert.Equal(1, Count(diff, "<tr>") - 1);
        Assert.Contains("summary", diff, StringComparison.Ordinal);
        Assert.Contains("A corrected summary.", diff, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_field_that_was_taken_away_and_one_that_was_changed_are_both_named()
    {
        var page = await studio.Get($"/en/{Collection}/record/relations/relation-02");
        var diff = Regex.Match(page.Main, "<table class=\"list diff\">.*?</table>", RegexOptions.Singleline).Value;

        Assert.Contains("Father and teacher of", diff, StringComparison.Ordinal);
        Assert.Contains("(not there)", diff, StringComparison.Ordinal);
        Assert.Contains("Waits for a review", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_record_that_was_taken_out_keeps_the_page_of_its_history()
    {
        var gone = await studio.Get($"/en/{Collection}/record/relations/relation-18");
        var never = await studio.Get($"/en/{Collection}/record/relations/relation-99");

        Assert.Equal(HttpStatusCode.OK, gone.Status);
        Assert.Contains("This record was taken out of the collection.", gone.Text, StringComparison.Ordinal);
        Assert.Contains("Taken out version 3", gone.Text, StringComparison.Ordinal);
        Assert.Contains("Reason: Not told in the selected part.", gone.Text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, never.Status);
    }

    [Fact]
    public async Task What_waits_for_a_review_is_listed_and_leads_to_its_record()
    {
        var page = await studio.Get($"/en/{Collection}/waiting");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("1 of 27 statements wait for a review", page.Text, StringComparison.Ordinal);
        Assert.Contains("Father and teacher of", page.Text, StringComparison.Ordinal);
        Assert.Contains($"{Collection}/record/relations/relation-02", page.Links);
        Assert.Equal(1, Count(page.Main, "<tr>") - 1);
    }

    [Fact]
    public async Task The_history_of_a_collection_names_the_record_of_every_change_the_newest_first()
    {
        var page = await studio.Get($"/en/{Collection}/history");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        var shown = Regex.Match(page.Text, "The latest (\\d+) of (\\d+) changes, the newest first\\.");
        Assert.True(shown.Success, page.Text[..Math.Min(300, page.Text.Length)]);
        var (latest, all) = (int.Parse(shown.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), int.Parse(shown.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(Math.Min(100, all), latest);
        Assert.Equal(latest, Count(page.Main, "<article class=\"change "));
        Assert.Contains($"{Collection}/record/relations/relation-18", page.Links);
        Assert.True(page.Text.IndexOf("Not told in the selected part.", StringComparison.Ordinal) < page.Text.IndexOf("The summary said too much.", StringComparison.Ordinal));
        Assert.Contains("Translation: fa", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_revisions_the_store_published_are_listed()
    {
        var page = await studio.Get($"/en/{Collection}/revisions");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("2026-11-17", page.Text, StringComparison.Ordinal);
        Assert.Contains("an editor", page.Text, StringComparison.Ordinal);
        Assert.Contains("The first publication.", page.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/en/another-collection")]
    [InlineData("/en/another-collection/waiting")]
    [InlineData("/en/another-collection/history")]
    [InlineData("/en/another-collection/revisions")]
    [InlineData("/en/another-collection/records/relations")]
    [InlineData($"/en/{Collection}/records/people")]
    [InlineData($"/en/{Collection}/nothing")]
    public async Task What_the_store_does_not_hold_is_not_found(string address)
    {
        var page = await studio.Get(address);

        Assert.Equal(HttpStatusCode.NotFound, page.Status);
        Assert.Contains("The store holds nothing at this address.", page.Text, StringComparison.Ordinal);
    }

    // --- the tool itself ---

    [Fact]
    public async Task The_tool_is_Persian_from_right_to_left_with_Persian_digits()
    {
        var page = await studio.Get($"/fa/{Collection}");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("<html lang=\"fa\" dir=\"rtl\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("رابطه‌ها", page.Text, StringComparison.Ordinal);
        Assert.Contains("۱ گزاره از ۲۷ در انتظار بازبینی است", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Relations", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_page_is_kept_or_indexed_and_none_has_a_script()
    {
        var page = await studio.Get($"/en/{Collection}/record/entities/character-rostam");

        Assert.Contains("no-store", page.Headers["Cache-Control"], StringComparison.Ordinal);
        Assert.Equal("noindex, nofollow", page.Headers["X-Robots-Tag"]);
        Assert.Contains("default-src 'self'", page.Headers["Content-Security-Policy"], StringComparison.Ordinal);
        Assert.Equal("DENY", page.Headers["X-Frame-Options"]);
        Assert.DoesNotContain("<script", page.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" style=", page.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/en/", new string[0])]
    [InlineData($"/en/{Collection}", new string[0])]
    [InlineData($"/en/{Collection}/waiting", new string[0])]
    [InlineData($"/en/{Collection}/history", new string[0])]
    [InlineData($"/en/{Collection}/records/relations", new[] { "add" })]
    [InlineData($"/en/{Collection}/record/relations/relation-02", new[] { "review", "change", "remove" })]
    [InlineData($"/en/{Collection}/record/entities/character-rostam", new[] { "change", "remove" })]
    [InlineData($"/en/{Collection}/record/relations/relation-18", new string[0])]
    [InlineData($"/en/{Collection}/revisions", new[] { "publish" })]
    public async Task A_page_has_the_forms_of_what_can_be_done_there_and_no_other(string address, string[] forms)
    {
        var page = await studio.Get(address);

        // Every form that changes the store names what it does and carries the token of the tool.
        Assert.Equal(forms, Regex.Matches(page.Html, "name=\"_handler\" value=\"([^\"]*)\"").Select(match => match.Groups[1].Value));
        Assert.Equal(forms.Length + 1, Regex.Matches(page.Html, "<form ").Count);
        Assert.Equal(forms.Length + 1, Regex.Matches(page.Html, "name=\"__RequestVerificationToken\"").Count);
    }

    [Fact]
    public async Task The_tool_answers_the_machine_it_runs_on_and_nobody_else()
    {
        var other = await studio.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
            context.Request.Method = "GET";
            context.Request.Path = "/en/";
        }, TestContext.Current.CancellationToken);
        var here = await studio.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            context.Request.Method = "GET";
            context.Request.Path = "/en/";
        }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, other.Response.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, here.Response.StatusCode);
    }

    [Fact]
    public void The_tool_does_not_start_without_the_file_of_its_store()
    {
        using var without = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("Studio:Store", string.Empty));

        var refused = Assert.ThrowsAny<Exception>(() => without.CreateClient());

        Assert.Contains("Studio:Store", refused.ToString(), StringComparison.Ordinal);
    }

    private static int Count(string text, string part) => text.Split(part).Length - 1;
}
