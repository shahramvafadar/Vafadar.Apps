using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Vafadar.Dastan.Store;
using Vafadar.Dastan.Store.Tests;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// An editor at work in the tool (step 11.5): one browser with its cookies, which opens a page, fills in one of
/// its forms and sends it as a browser does – with the token of the form.
/// </summary>
public sealed partial class Desk(Studio studio) : IDisposable
{
    private readonly HttpClient _client = studio.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public void Dispose() => _client.Dispose();

    /// <summary>Opens a page.</summary>
    public async Task<StudioPage> Get(string address)
    {
        using var response = await _client.GetAsync(new Uri(address, UriKind.Relative));
        return await Page(response);
    }

    /// <summary>
    /// Fills in the form of that name on the page and sends it. An act that was done answers with the way back
    /// to the page, which is followed; an act that was refused answers with the page itself.
    /// </summary>
    public async Task<StudioPage> Submit(string address, string form, params (string Name, string Value)[] fields)
    {
        var page = await Get(address);
        var markup = Forms().Matches(page.Html).Select(match => match.Value).Single(candidate => candidate.Contains($"name=\"_handler\" value=\"{form}\"", StringComparison.Ordinal));
        var token = WebUtility.HtmlDecode(Token().Match(markup).Groups[1].Value);
        var action = WebUtility.HtmlDecode(Action().Match(markup).Groups[1].Value);
        return await Post($"/en/{action}", [("_handler", form), ("__RequestVerificationToken", token), .. fields]);
    }

    /// <summary>Sends a form as it is given, and follows the way back when the act was done.</summary>
    public async Task<StudioPage> Post(string address, IEnumerable<(string Name, string Value)> fields)
    {
        using var content = new FormUrlEncodedContent(fields.Select(field => KeyValuePair.Create(field.Name, field.Value)));
        using var response = await _client.PostAsync(new Uri(address, UriKind.Relative), content);
        if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther && response.Headers.Location is { } back)
        {
            var target = back.IsAbsoluteUri ? back.PathAndQuery : new Uri(new Uri("http://localhost/en/"), back).PathAndQuery;
            return await Get(target) with { Arrived = target };
        }

        return await Page(response);
    }

    private static async Task<StudioPage> Page(HttpResponseMessage response) =>
        new(response.StatusCode, await response.Content.ReadAsStringAsync(), response.Headers.ToDictionary(header => header.Key, header => string.Join(", ", header.Value), StringComparer.OrdinalIgnoreCase));

    [GeneratedRegex("<form\\b.*?</form>", RegexOptions.Singleline)]
    private static partial Regex Forms();

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex Token();

    [GeneratedRegex("<form[^>]*action=\"([^\"]*)\"")]
    private static partial Regex Action();
}

/// <summary>
/// Acting in the tool of the editors: a review, a change, taking out, adding and publishing – each a form that
/// says what it will do, is tried on the whole collection, and says what it did or why it did nothing. Every test
/// has a store of its own.
/// </summary>
public sealed class StudioActsTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Relation = $"/en/{Collection}/record/relations/relation-02";
    private const string Rostam = $"/en/{Collection}/record/entities/character-rostam";

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioActsTests() => _desk = new Desk(_studio);

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
    }

    private static (string, string)[] Review(string found, string? note = null, string by = "a reviewer") =>
    [
        ("Reviewing.Statement", "relation-02"), ("Reviewing.As", found), ("Reviewing.On", "2026-11-17"), ("Reviewing.Note", note ?? string.Empty), ("Reviewing.By", by),
    ];

    // --- a review ---

    [Fact]
    public async Task A_review_is_recorded_and_the_page_says_so_and_shows_the_statement_as_it_is_now()
    {
        var page = await _desk.Submit(Relation, "review", Review("reviewed", "Read against the text."));

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.EndsWith("relation-02?done=reviewed", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("The review was recorded.", page.Text, StringComparison.Ordinal);
        Assert.Contains("class=\"review review-reviewed\"", page.Main, StringComparison.Ordinal);
        Assert.Contains("Reason: Read against the text.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Changed version 4 – by a reviewer", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_review_the_readers_refuse_changes_nothing_says_why_and_keeps_what_was_filled_in()
    {
        var page = await _desk.Submit(Relation, "review", Review("disputed"));

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Null(page.Arrived);
        Assert.Contains("Not done. Nothing was changed.", page.Text, StringComparison.Ordinal);
        Assert.Contains("relation-02: the review of a disputed statement says in its note what the sources disagree about.", page.Text, StringComparison.Ordinal);
        Assert.Matches("value=\"disputed\"[^>]*checked", page.Main);
        Assert.Contains("value=\"a reviewer\"", page.Main, StringComparison.Ordinal);
        Assert.Contains("class=\"review review-waiting\"", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("version 4", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_review_needs_somebody_a_day_and_a_finding()
    {
        var page = await _desk.Submit(Relation, "review", ("Reviewing.Statement", "relation-02"), ("Reviewing.As", "approved"), ("Reviewing.On", "17.11.2026"), ("Reviewing.By", " "));

        Assert.Contains("Say who you are.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Give the day as a date.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Choose what the review found.", page.Text, StringComparison.Ordinal);
        Assert.Contains("class=\"review review-waiting\"", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_name_of_the_last_act_starts_the_next_form()
    {
        await _desk.Submit(Relation, "review", Review("reviewed", by: "Mina the reviewer"));

        var next = await _desk.Get(Rostam);

        // The page of a name has two forms – to change it and to take it out – and both start with the name.
        Assert.Equal(2, Regex.Matches(next.Main, "value=\"Mina the reviewer\"").Count);
        Assert.Contains("id=\"change-by\" name=\"Changing.By\" value=\"Mina the reviewer\"", next.Main, StringComparison.Ordinal);
    }

    // --- a change ---

    private async Task<string> Written(string address)
    {
        var page = await _desk.Get(address);
        return WebUtility.HtmlDecode(Regex.Match(page.Main, "<textarea id=\"change-json\"[^>]*>(.*?)</textarea>", RegexOptions.Singleline).Groups[1].Value);
    }

    [Fact]
    public async Task A_record_is_changed_as_text_and_the_change_is_kept_with_who_and_why()
    {
        var record = JsonNode.Parse(await Written(Rostam))!.AsObject();
        record["summary"] = "A second correction.";

        var page = await _desk.Submit(Rostam, "change", ("Changing.Json", record.ToJsonString()), ("Changing.Reason", "Shorter."), ("Changing.By", "a writer"));

        Assert.EndsWith("character-rostam?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("The change was kept.", page.Text, StringComparison.Ordinal);
        Assert.Contains("A second correction.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Changed version 3 – by a writer", page.Text, StringComparison.Ordinal);
        Assert.Contains("Reason: Shorter.", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_record_as_it_is_sent_back_unchanged_changes_nothing()
    {
        var page = await _desk.Submit(Rostam, "change", ("Changing.Json", await Written(Rostam)), ("Changing.Reason", "Nothing."), ("Changing.By", "a writer"));

        Assert.EndsWith("?done=unchanged", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("Nothing had to be changed.", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("version 3", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_change_that_would_make_the_collection_unusable_is_refused_and_the_text_stays_in_the_form()
    {
        var record = JsonNode.Parse(await Written(Rostam))!.AsObject();
        record["type"] = "Hero";

        var page = await _desk.Submit(Rostam, "change", ("Changing.Json", record.ToJsonString()), ("Changing.Reason", "A try."), ("Changing.By", "a writer"));

        Assert.Null(page.Arrived);
        Assert.Contains("Not done. Nothing was changed.", page.Text, StringComparison.Ordinal);
        Assert.Contains("character-rostam: type has the unknown value", page.Text, StringComparison.Ordinal);
        Assert.Contains("Hero", WebUtility.HtmlDecode(Regex.Match(page.Main, "<textarea id=\"change-json\"[^>]*>(.*?)</textarea>", RegexOptions.Singleline).Groups[1].Value), StringComparison.Ordinal);
        Assert.Contains("\"type\": \"Character\"", await Written(Rostam), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ not a record", "The text is not a record:")]
    [InlineData("[1, 2]", "The text is not a record: it has to be one object in braces.")]
    [InlineData("{ \"id\": \"character-someone-else\" }", "A record keeps its id (character-rostam).")]
    public async Task A_text_that_is_not_this_record_is_not_kept(string text, string expected)
    {
        var page = await _desk.Submit(Rostam, "change", ("Changing.Json", text), ("Changing.Reason", "A try."), ("Changing.By", "a writer"));

        Assert.Contains(expected, page.Text, StringComparison.Ordinal);
        Assert.Contains("\"type\": \"Character\"", await Written(Rostam), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_change_needs_somebody_and_a_reason()
    {
        var page = await _desk.Submit(Rostam, "change", ("Changing.Json", await Written(Rostam)), ("Changing.Reason", " "), ("Changing.By", string.Empty));

        Assert.Contains("Say who you are.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Say why.", page.Text, StringComparison.Ordinal);
    }

    // --- taking out and adding ---

    [Fact]
    public async Task A_record_is_taken_out_and_its_page_keeps_its_history()
    {
        var page = await _desk.Submit($"/en/{Collection}/record/relations/relation-17", "remove", ("Removing.Reason", "Told elsewhere."), ("Removing.By", "an editor"));
        var list = await _desk.Get($"/en/{Collection}/records/relations");

        Assert.EndsWith("relation-17?done=removed", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("The record was taken out.", page.Text, StringComparison.Ordinal);
        Assert.Contains("This record was taken out of the collection.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Reason: Told elsewhere.", page.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("<form method=\"post\" action=\"" + Collection, page.Main, StringComparison.Ordinal);
        Assert.Contains("<h1>Relations: 18</h1>", list.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_record_something_points_to_is_not_taken_out()
    {
        var page = await _desk.Submit($"/en/{Collection}/record/entities/character-zal", "remove", ("Removing.Reason", "A try."), ("Removing.By", "an editor"));

        Assert.Null(page.Arrived);
        Assert.Contains("Not done. Nothing was changed.", page.Text, StringComparison.Ordinal);
        Assert.Contains("character-zal", page.Text, StringComparison.Ordinal);
        Assert.Contains("id=\"now\"", page.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_record_is_added_at_the_end_of_its_group_and_never_in_place_of_one_that_is_there()
    {
        const string Source = """
            { "id": "src-new", "title": "A new source", "url": "https://example.org/a-new-source", "kind": "primary_literary_text_transcription",
              "accessedAt": "2026-11-17", "spoilerLevel": 0 }
            """;
        var sources = $"/en/{Collection}/records/sources";

        var added = await _desk.Submit(sources, "add", ("Adding.Json", Source), ("Adding.Reason", "For a later statement."), ("Adding.By", "a writer"));
        var again = await _desk.Submit(sources, "add", ("Adding.Json", Source), ("Adding.Reason", "Once more."), ("Adding.By", "a writer"));
        var nameless = await _desk.Submit(sources, "add", ("Adding.Json", "{ \"title\": \"Nobody\" }"), ("Adding.Reason", "A try."), ("Adding.By", "a writer"));

        Assert.EndsWith("record/sources/src-new?done=added", added.Arrived, StringComparison.Ordinal);
        Assert.Contains("The record was added.", added.Text, StringComparison.Ordinal);
        Assert.Contains("Came in version 1 – by a writer", added.Text, StringComparison.Ordinal);
        Assert.Contains("A record src-new is there already; change it on its page.", again.Text, StringComparison.Ordinal);
        Assert.Contains("A record names itself: it has an id.", nameless.Text, StringComparison.Ordinal);
    }

    // --- publishing ---

    [Fact]
    public async Task A_revision_is_published_only_when_every_statement_is_reviewed_and_then_the_store_keeps_it()
    {
        var revisions = $"/en/{Collection}/revisions";
        (string, string)[] form = [("Publishing.On", "2026-11-18"), ("Publishing.Note", "A correction."), ("Publishing.By", "an editor")];

        var early = await _desk.Submit(revisions, "publish", form);
        await _desk.Submit(Relation, "review", Review("reviewed", "Read again."));
        var published = await _desk.Submit(revisions, "publish", form);

        Assert.Null(early.Arrived);
        Assert.Contains("Publish revision 2", early.Text, StringComparison.Ordinal);
        Assert.Contains("relation-02: a published revision holds only reviewed statements", early.Text, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(early.Main, "<tr>").Count - 1);

        Assert.EndsWith("revisions?done=published", published.Arrived, StringComparison.Ordinal);
        Assert.Contains("The revision was published and is kept in the store.", published.Text, StringComparison.Ordinal);
        Assert.Contains("A correction.", published.Text, StringComparison.Ordinal);
        Assert.Contains("Publish revision 3", published.Text, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(published.Main, "<tr>").Count - 1);
    }

    // --- what guards an act ---

    [Fact]
    public async Task A_form_that_does_not_come_from_a_page_of_the_tool_is_refused_and_changes_nothing()
    {
        var page = await _desk.Post(Relation, [("_handler", "review"), .. Review("reviewed", "Forged.")]);
        var after = await _desk.Get(Relation);

        Assert.Equal(HttpStatusCode.BadRequest, page.Status);
        Assert.Contains("class=\"review review-waiting\"", after.Main, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_a_page_was_told_was_done_is_said_only_for_what_can_be_done_there()
    {
        var page = await _desk.Get($"{Rostam}?done=<b>anything</b>");

        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.DoesNotContain("class=\"done\"", page.Main, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>anything</b>", page.Html, StringComparison.Ordinal);
    }
}
