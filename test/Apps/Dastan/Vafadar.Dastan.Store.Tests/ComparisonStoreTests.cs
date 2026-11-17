using System.Text.Json.Nodes;
using Vafadar.Dastan.Content;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The comparisons in the store (step 12.6): brought in with their translations, read by the readers of the
/// site before they are kept, every change with who, when and why, written out as they went in.
/// </summary>
public sealed class ComparisonStoreTests : IDisposable
{
    private const string Id = "iran-china-rome";
    private static readonly DateOnly Today = new(2026, 11, 17);

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private string File => Path.Combine(_copy.Samples, "..", "comparisons", $"{Id}.json");

    private string Persian => Path.Combine(_copy.Samples, "..", "comparisons", $"{Id}.fa.json");

    private (int Exit, string Output) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        return (ContentCommands.Run(arguments, output, Today, _copy.Clock), output.ToString());
    }

    [Fact]
    public void A_comparison_comes_in_with_its_translation_and_goes_out_as_it_went_in()
    {
        using var store = _copy.Open();

        var result = store.ImportComparison(File, "an editor", "The first comparison.");
        var summary = Assert.Single(store.Comparisons());
        var exported = store.Comparison(Id)!;

        Assert.True(result.Done, string.Join('\n', result.Problems));
        Assert.Equal(2, result.Changed);
        Assert.Equal((Id, 1, "an editor"), (summary.Id, summary.Version, summary.UpdatedBy));
        Assert.Equal(["fa"], summary.Languages);
        Assert.Equal((string?)JsonNode.Parse(System.IO.File.ReadAllText(File))!["title"], summary.Title);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(System.IO.File.ReadAllText(File)), JsonNode.Parse(exported.Json)));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(System.IO.File.ReadAllText(Persian)), JsonNode.Parse(exported.Translations["fa"])));

        // What comes out is what the site reads.
        var read = ComparisonSample.Read(exported.Json, [.. exported.Translations.Values]);
        Assert.Equal(Id, read.Id.Value);
    }

    [Fact]
    public void Bringing_the_same_file_in_again_changes_nothing_and_a_changed_one_is_a_change_with_who_and_why()
    {
        using var store = _copy.Open();
        store.ImportComparison(File, "an editor");

        var same = store.ImportComparison(File, "somebody else", "Again.");
        var changed = JsonNode.Parse(System.IO.File.ReadAllText(File))!.AsObject();
        changed["title"] = "Three lands side by side";
        System.IO.File.WriteAllText(File, changed.ToJsonString());
        _copy.Clock.Advance(TimeSpan.FromMinutes(5));
        var again = store.ImportComparison(File, "a writer", "A plainer title.");

        Assert.Equal(0, same.Changed);
        Assert.Equal(1, again.Changed);
        Assert.Equal("Three lands side by side", store.Comparisons().Single().Title);
        var history = store.ComparisonHistory(Id);
        Assert.Equal([RecordChangeKind.Added, RecordChangeKind.Added, RecordChangeKind.Changed], history.Select(change => change.Change));
        var last = history[^1];
        Assert.Equal((string.Empty, 2, "a writer", "A plainer title."), (last.Language, last.Version, last.By, last.Reason));
        Assert.Contains("Three lands side by side", last.After, StringComparison.Ordinal);
        Assert.DoesNotContain("Three lands side by side", last.Before, StringComparison.Ordinal);
    }

    [Fact]
    public void What_the_site_could_not_read_is_refused_and_changes_nothing()
    {
        using var store = _copy.Open();
        store.ImportComparison(File, "an editor");
        var broken = JsonNode.Parse(System.IO.File.ReadAllText(File))!.AsObject();
        broken["frame"] = "moonYears";

        var refused = store.PutComparison(broken.ToJsonString(), new Dictionary<string, string>(), "a writer", "Trying.");
        var nameless = store.PutComparison("""{ "formatVersion": "dastan-comparison-0.1" }""", new Dictionary<string, string>(), "a writer", "Trying.");
        var noJson = store.PutComparison("not a document", new Dictionary<string, string>(), "a writer", "Trying.");
        var wrongLanguage = store.PutComparison(System.IO.File.ReadAllText(File), new Dictionary<string, string> { ["de"] = System.IO.File.ReadAllText(Persian) }, "a writer", "Trying.");

        Assert.False(refused.Done);
        Assert.NotEmpty(refused.Problems);
        Assert.Equal(["a comparison names itself: it has an id."], nameless.Problems);
        Assert.StartsWith("the comparison cannot be read", Assert.Single(noJson.Problems), StringComparison.Ordinal);
        Assert.Equal(["de: the translation says it is in fa."], wrongLanguage.Problems);
        Assert.Equal(2, store.ComparisonHistory(Id).Length);
        Assert.Equal(["fa"], store.Comparisons().Single().Languages);
    }

    [Fact]
    public void A_translation_that_is_not_given_is_taken_out_and_a_comparison_is_taken_out_with_its_history_kept()
    {
        using var store = _copy.Open();
        store.ImportComparison(File, "an editor");

        var without = store.PutComparison(System.IO.File.ReadAllText(File), new Dictionary<string, string>(), "a translator", "The Persian text is being rewritten.");
        var languages = store.Comparisons().Single().Languages;
        var removed = store.RemoveComparison(Id, "an editor", "Not ready.");
        var unknown = store.RemoveComparison("nothing", "an editor", "x");

        Assert.Equal(1, without.Changed);
        Assert.Empty(languages);
        Assert.True(removed.Done);
        Assert.Empty(store.Comparisons());
        Assert.Null(store.Comparison(Id));
        Assert.Equal(RecordChangeKind.Removed, store.ComparisonHistory(Id)[^1].Change);
        Assert.Equal(["nothing: the store holds no comparison of that name."], unknown.Problems);
    }

    [Fact]
    public void A_collection_and_a_comparison_live_in_one_store_without_touching_each_other()
    {
        using var store = _copy.Filled();
        var records = store.Summary(WorkingCopy.Collection)!.Records.Sum(group => group.Count);

        store.ImportComparison(File, "an editor");

        Assert.Equal(records, store.Summary(WorkingCopy.Collection)!.Records.Sum(group => group.Count));
        Assert.Single(store.Comparisons());
        Assert.Equal([WorkingCopy.Collection], store.Collections);
    }

    [Fact]
    public void The_commands_bring_a_comparison_in_list_it_write_it_out_and_take_it_out()
    {
        var target = Path.Combine(_copy.Folder, "out", $"{Id}.json");

        var (imported, saidImport) = Run("store", "comparison", "import", File, "--store", _copy.Database, "--by", "an editor");
        var (listed, list) = Run("store", "comparison", "list", "--store", _copy.Database);
        var (exported, _) = Run("store", "comparison", "export", Id, "--store", _copy.Database, "--out", target);
        var (history, changes) = Run("store", "comparison", "history", Id, "--store", _copy.Database);
        var (again, _) = Run("store", "comparison", "export", Id, "--store", _copy.Database, "--out", target);
        var (removed, _) = Run("store", "comparison", "remove", Id, "--store", _copy.Database, "--by", "an editor", "--reason", "Not ready.");
        var (gone, saidGone) = Run("store", "comparison", "export", Id, "--store", _copy.Database, "--out", Path.Combine(_copy.Folder, "out", "other.json"));
        var (unknown, _) = Run("store", "comparison", "polish", Id, "--store", _copy.Database);

        Assert.Equal(ContentCommands.Done, imported);
        Assert.Contains("brought in", saidImport, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, listed);
        Assert.Contains($"{Id}  version 1  fa", list, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, exported);
        Assert.True(System.IO.File.Exists(target));
        Assert.True(System.IO.File.Exists(Path.Combine(_copy.Folder, "out", $"{Id}.fa.json")));
        Assert.Equal(ContentCommands.Done, history);
        Assert.Contains("2 changes", changes, StringComparison.Ordinal);
        Assert.NotEqual(ContentCommands.Done, again);
        Assert.Equal(ContentCommands.Done, removed);
        Assert.Equal(ContentCommands.NotUsable, gone);
        Assert.Contains("holds no comparison", saidGone, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUnderstood, unknown);
    }
}
