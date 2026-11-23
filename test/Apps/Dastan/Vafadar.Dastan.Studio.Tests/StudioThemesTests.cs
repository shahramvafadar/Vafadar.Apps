using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The themes of a gallery in the tool of the editors (R-MED-02): a theme is added as an entity of the type
/// <c>Theme</c> – without a source, where it rests on none – and the form of an image or a video then offers
/// the themes of the collection to tick, apart from who and what the item shows.
/// </summary>
public sealed class StudioThemesTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Item = $"/en/{Collection}/record/media/media-met-453265";
    private static readonly (string, string?)[] Adding = [("Adding.By", "a writer"), ("Adding.Reason", "A theme of the gallery.")];
    private static readonly (string, string?)[] Filling = [("Filling.By", "a writer"), ("Filling.Reason", "Gathered.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioThemesTests() => _desk = new Desk(_studio);

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
    }

    private async Task<JsonObject> Written()
    {
        var page = await _desk.Get(Item);
        return JsonNode.Parse(WebUtility.HtmlDecode(Regex.Match(page.Main, "<textarea id=\"change-json\"[^>]*>(.*?)</textarea>", RegexOptions.Singleline).Groups[1].Value))!.AsObject();
    }

    private static string FieldsForm(StudioPage page) =>
        Regex.Matches(page.Html, "<form\\b.*?</form>", RegexOptions.Singleline).Select(match => match.Value).Single(form => form.Contains("name=\"_handler\" value=\"fields\"", StringComparison.Ordinal));

    private Task<StudioPage> NewTheme(string id, string label) => _desk.Fill($"/en/{Collection}/records/entities/new", "new",
        [("id", id), ("f.type", "Theme"), ("f.label", label), ("f.summary", "What the editors gathered here."), .. Adding]);

    [Fact]
    public async Task A_collection_without_themes_offers_none_and_a_theme_is_added_without_a_source()
    {
        Assert.DoesNotContain("themeIds", FieldsForm(await _desk.Get(Item)), StringComparison.Ordinal);

        var added = await NewTheme("theme-combat", "Combat");

        Assert.EndsWith("record/entities/theme-combat?done=added", added.Arrived, StringComparison.Ordinal);
        using var store = _studio.Store();
        Assert.Equal("Theme", (string?)store.Record(Collection, "entities", "theme-combat")!.Body["type"]);
        Assert.Equal(["theme-combat"], store.EntitiesOfType(Collection, "Theme"));
    }

    [Fact]
    public async Task The_form_of_an_item_offers_the_themes_to_tick_and_keeps_them_apart_from_what_the_item_shows()
    {
        await NewTheme("theme-combat", "Combat");
        await NewTheme("theme-mourning", "Mourning");
        var form = FieldsForm(await _desk.Get(Item));

        // The themes, none ticked; and no theme among who and what the item shows.
        Assert.Contains("<legend><code>themeIds</code></legend>", form, StringComparison.Ordinal);
        Assert.Matches("<input type=\"checkbox\" name=\"f.themeIds\" value=\"theme-combat\"\\s*/?>", form);
        Assert.Matches("<input type=\"checkbox\" name=\"f.themeIds\" value=\"theme-mourning\"\\s*/?>", form);
        Assert.DoesNotContain("name=\"f.themeIds\" value=\"character-rostam\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"f.depictedEntityIds\" value=\"theme-combat\"", form, StringComparison.Ordinal);
        Assert.Contains("name=\"f.depictedEntityIds\" value=\"character-rostam\"", form, StringComparison.Ordinal);

        // Sent as it stands, the form adds no list of themes.
        var same = await _desk.Fill(Item, "fields", Filling);
        Assert.EndsWith("?done=unchanged", same.Arrived, StringComparison.Ordinal);
        Assert.False((await Written()).ContainsKey("themeIds"));

        // Ticked, the item is under the theme; ticked off again, it has no list of themes.
        var ticked = await _desk.Fill(Item, "fields", [("f.themeIds", "theme-mourning"), .. Filling]);
        Assert.EndsWith("?done=changed", ticked.Arrived, StringComparison.Ordinal);
        Assert.Equal("[\"theme-mourning\"]", (await Written())["themeIds"]!.ToJsonString());
        Assert.Matches("<input type=\"checkbox\" name=\"f.themeIds\" value=\"theme-mourning\" checked", FieldsForm(ticked));

        var off = await _desk.Fill(Item, "fields", [("f.themeIds", null), .. Filling]);
        Assert.EndsWith("?done=changed", off.Arrived, StringComparison.Ordinal);
        Assert.False((await Written()).ContainsKey("themeIds"));
    }
}
