using System.Net;
using System.Text.Json.Nodes;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The comparisons in the tool of the editors (step 12.6): the list, bringing one in, changing its file and its
/// translations in one act, taking it out – each kept with who and why, and refused with the form kept.
/// </summary>
public sealed class StudioComparisonsTests : IDisposable
{
    private const string Id = "iran-china-rome";
    private const string Page = $"/en/comparisons/{Id}";
    private static readonly (string, string?)[] WhoAndWhy = [("Changing.By", "a writer"), ("Changing.Reason", "Clearer.")];

    private static readonly string Folder = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan", "content", "comparisons"));

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioComparisonsTests() => _desk = new Desk(_studio);

    public void Dispose() => _studio.Dispose();

    private static string File => System.IO.File.ReadAllText(Path.Combine(Folder, $"{Id}.json"));

    private void BroughtIn()
    {
        using var store = _studio.Store();
        Assert.True(store.ImportComparison(Path.Combine(Folder, $"{Id}.json"), "an editor", "The first comparison.").Done);
    }

    [Theory]
    [InlineData("en", "Comparisons", "The store holds no comparison.")]
    [InlineData("fa", "مقایسه‌ها", "انبار هیچ مقایسه‌ای ندارد.")]
    public async Task The_tool_names_the_comparisons_and_says_when_there_is_none(string language, string title, string none)
    {
        var empty = await _desk.Get($"/{language}/comparisons");
        BroughtIn();
        var listed = await _desk.Get($"/{language}/comparisons");
        var home = await _desk.Get($"/{language}/");

        Assert.Equal(HttpStatusCode.OK, empty.Status);
        Assert.Contains(title, empty.Text, StringComparison.Ordinal);
        Assert.Contains(none, empty.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(none, listed.Text, StringComparison.Ordinal);
        Assert.Contains($"comparisons/{Id}", listed.Html, StringComparison.Ordinal);
        Assert.Contains("href=\"comparisons\"", home.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_comparison_is_brought_in_as_its_file_and_one_the_site_cannot_read_is_refused_with_the_form_kept()
    {
        var broken = JsonNode.Parse(File)!.AsObject();
        broken["frame"] = "moonYears";

        var refused = await _desk.Fill("/en/comparisons", "add", ("Adding.Json", broken.ToJsonString()), ("Adding.By", "a writer"), ("Adding.Reason", "A first try."));
        var added = await _desk.Fill("/en/comparisons", "add", ("Adding.Json", File), ("Adding.By", "a writer"), ("Adding.Reason", "The comparison."));
        var twice = await _desk.Fill("/en/comparisons", "add", ("Adding.Json", File), ("Adding.By", "a writer"), ("Adding.Reason", "Again."));

        Assert.Contains("moonYears", refused.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", refused.Arrived, StringComparison.Ordinal);
        Assert.EndsWith($"comparisons/{Id}?done=added", added.Arrived, StringComparison.Ordinal);
        Assert.Contains("already holds the comparison", twice.Text, StringComparison.Ordinal);
        using var store = _studio.Store();
        var change = Assert.Single(store.ComparisonHistory(Id));
        Assert.Equal(("a writer", "The comparison."), (change.By, change.Reason));
    }

    [Fact]
    public async Task The_file_and_its_translation_are_changed_in_one_act_and_sent_as_they_stand_change_nothing()
    {
        BroughtIn();
        var changed = JsonNode.Parse(File)!.AsObject();
        changed["title"] = "Three lands side by side";

        var same = await _desk.Fill(Page, "change", WhoAndWhy);
        var page = await _desk.Fill(Page, "change", [("json", changed.ToJsonString()), .. WhoAndWhy]);
        var emptied = await _desk.Fill(Page, "change", [("t.fa", " "), .. WhoAndWhy]);

        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("Three lands side by side", page.Text, StringComparison.Ordinal);
        Assert.EndsWith("?done=changed", emptied.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Empty(store.Comparisons().Single().Languages);
        Assert.Equal(RecordChangeKind.Removed, store.ComparisonHistory(Id)[^1].Change);
    }

    [Fact]
    public async Task A_change_the_site_cannot_read_and_a_file_of_another_comparison_are_refused_with_what_was_written_kept()
    {
        BroughtIn();
        var other = JsonNode.Parse(File)!.AsObject();
        other["id"] = "another";

        var notJson = await _desk.Fill(Page, "change", [("json", "{ not a document"), .. WhoAndWhy]);
        var renamed = await _desk.Fill(Page, "change", [("json", other.ToJsonString()), .. WhoAndWhy]);
        var nameless = await _desk.Fill(Page, "change", [("json", File), ("Changing.By", " "), ("Changing.Reason", " ")]);

        Assert.Contains("{ not a document", Desk.Standing(notJson.Html).First(input => input.Name == "json").Value, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", notJson.Arrived, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", renamed.Arrived, StringComparison.Ordinal);
        Assert.DoesNotContain("?done=", nameless.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal(2, store.ComparisonHistory(Id).Length);
    }

    [Fact]
    public async Task A_comparison_is_taken_out_and_its_page_keeps_what_was_changed_of_it()
    {
        BroughtIn();

        var removed = await _desk.Fill(Page, "remove", ("Removing.By", "an editor"), ("Removing.Reason", "Not ready."));
        var page = await _desk.Get(Page);
        var never = await _desk.Get("/en/comparisons/never-there");

        Assert.EndsWith("comparisons?done=removed", removed.Arrived, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        Assert.Contains("Not ready.", page.Text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, never.Status);
    }
}
