using System.Net;
using System.Text.Json.Nodes;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Tests;

public sealed partial class Desk
{
    /// <summary>
    /// Sends a form of a page that was opened earlier, with what it stood at then – as an editor does who filled
    /// a form in while somebody else worked.
    /// </summary>
    public async Task<StudioPage> Send(StudioPage opened, string form, params (string Name, string? Value)[] changes)
    {
        var markup = Forms().Matches(opened.Html).Select(match => match.Value).Single(candidate => candidate.Contains($"name=\"_handler\" value=\"{form}\"", StringComparison.Ordinal));
        var action = WebUtility.HtmlDecode(Action().Match(markup).Groups[1].Value);
        var changed = changes.Select(change => change.Name).ToHashSet(StringComparer.Ordinal);
        var sent = Standing(markup).Where(input => !changed.Contains(input.Name)).ToList();
        sent.AddRange(changes.Where(change => change.Value is not null).Select(change => (change.Name, change.Value!)));
        return await Post($"/en/{action}", sent);
    }
}

/// <summary>
/// Two editors at one thing (step 13.2): a form that was opened before somebody else changed what it shows is
/// not kept over that change – the editor is told, keeps the inputs, and may send them again knowingly.
/// </summary>
public sealed class StudioStaleFormTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Rostam = $"/en/{Collection}/record/entities/character-rostam";
    private const string Stale = "changed by somebody else after you opened the form";

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioStaleFormTests() => _desk = new Desk(_studio);

    public void Dispose() => _studio.Dispose();

    private void SomebodyElse(Action<JsonObject> change)
    {
        using var store = _studio.Store();
        var record = store.Record(Collection, "entities", "character-rostam")!.Body;
        change(record);
        Assert.True(store.Put(Collection, "entities", record, "somebody else", "Meanwhile.").Done);
    }

    private string Summary()
    {
        using var store = _studio.Store();
        return (string)store.Record(Collection, "entities", "character-rostam")!.Body["summary"]!;
    }

    [Theory]
    [InlineData("fields", "f.label", "Filling")]
    [InlineData("change", null, "Changing")]
    [InlineData("remove", null, "Removing")]
    public async Task A_form_opened_before_somebody_else_changed_the_record_is_not_kept(string form, string? input, string model)
    {
        var opened = await _desk.Get(Rostam);
        SomebodyElse(record => record["summary"] = "What somebody else wrote meanwhile.");
        (string, string?)[] changes = input is null
            ? [($"{model}.By", "a writer"), ($"{model}.Reason", "Mine.")]
            : [(input, "Rostam of the tests"), ($"{model}.By", "a writer"), ($"{model}.Reason", "Mine.")];

        var page = await _desk.Send(opened, form, changes);

        Assert.DoesNotContain("?done=", page.Arrived, StringComparison.Ordinal);
        Assert.Contains(Stale, page.Text, StringComparison.Ordinal);
        Assert.Equal("What somebody else wrote meanwhile.", Summary());
        using var store = _studio.Store();
        Assert.Equal("somebody else", store.History(Collection, "entities", "character-rostam")[^1].By);
    }

    [Fact]
    public async Task The_form_that_comes_back_has_the_inputs_of_the_editor_and_sent_again_it_is_kept()
    {
        var opened = await _desk.Get(Rostam);
        SomebodyElse(record => record["summary"] = "What somebody else wrote meanwhile.");

        var refused = await _desk.Send(opened, "fields", ("f.label", "Rostam of the tests"), ("Filling.By", "a writer"), ("Filling.Reason", "Mine."));
        var inputs = Desk.Standing(refused.Html).GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => input.First().Value, StringComparer.Ordinal);
        var again = await _desk.Send(refused, "fields", ("Filling.By", "a writer"), ("Filling.Reason", "Mine, knowingly."));

        Assert.Equal("Rostam of the tests", inputs["f.label"]);
        Assert.EndsWith("?done=changed", again.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal("Rostam of the tests", (string?)store.Record(Collection, "entities", "character-rostam")!.Body["label"]);
    }

    [Fact]
    public async Task A_change_of_the_Persian_text_of_the_record_by_somebody_else_makes_the_form_stale_too()
    {
        var opened = await _desk.Get(Rostam);
        using (var store = _studio.Store())
        {
            var texts = store.Texts(Collection, "entities", "character-rostam").Single(text => text.Language == "fa").Texts;
            texts["summary"] = "متنی که دیگری در این میان نوشت.";
            Assert.True(store.Put(Collection, "entities", store.Record(Collection, "entities", "character-rostam")!.Body, "a translator", "Meanwhile.", new Dictionary<string, JsonObject> { ["fa"] = texts }).Done);
        }

        var page = await _desk.Send(opened, "fields", ("Filling.By", "a writer"), ("Filling.Reason", "Mine."));

        Assert.Contains(Stale, page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_change_of_another_record_does_not_make_a_form_stale()
    {
        var opened = await _desk.Get(Rostam);
        using (var store = _studio.Store())
        {
            var zal = store.Record(Collection, "entities", "character-zal")!.Body;
            zal["summary"] = "Another record, changed meanwhile.";
            Assert.True(store.Put(Collection, "entities", zal, "somebody else", "Meanwhile.").Done);
        }

        var page = await _desk.Send(opened, "fields", ("f.label", "Rostam of the tests"), ("Filling.By", "a writer"), ("Filling.Reason", "Mine."));

        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_stages_and_a_comparison_are_guarded_in_the_same_way()
    {
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan", "content", "comparisons"));
        using (var store = _studio.Store())
        {
            Assert.True(store.ImportComparison(Path.Combine(folder, "iran-china-rome.json"), "an editor").Done);
        }

        var stages = await _desk.Get($"/en/{Collection}/stages");
        var comparison = await _desk.Get("/en/comparisons/iran-china-rome");
        using (var store = _studio.Store())
        {
            var labels = store.Stages(Collection).Select(stage => stage.Label).ToList();
            labels[0] = "A new name, meanwhile";
            Assert.True(store.PutStages(Collection, labels, new Dictionary<string, IReadOnlyList<string?>>(), "somebody else", "Meanwhile.").Done);
            var file = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "iran-china-rome.json")))!.AsObject();
            file["title"] = "A new title, meanwhile";
            Assert.True(store.PutComparison(file.ToJsonString(), new Dictionary<string, string>(), "somebody else", "Meanwhile.").Done);
        }

        var staleStages = await _desk.Send(stages, "stages", ("Keeping.By", "an editor"), ("Keeping.Reason", "Mine."));
        var staleComparison = await _desk.Send(comparison, "change", ("Changing.By", "an editor"), ("Changing.Reason", "Mine."));
        var staleRemoval = await _desk.Send(comparison, "remove", ("Removing.By", "an editor"), ("Removing.Reason", "Mine."));

        Assert.Contains(Stale, staleStages.Text, StringComparison.Ordinal);
        Assert.Contains(Stale, staleComparison.Text, StringComparison.Ordinal);
        Assert.Contains(Stale, staleRemoval.Text, StringComparison.Ordinal);
        using var after = _studio.Store();
        Assert.Equal("A new name, meanwhile", after.Stages(Collection)[0].Label);
        Assert.Equal("A new title, meanwhile", after.Comparisons().Single().Title);
    }
}
