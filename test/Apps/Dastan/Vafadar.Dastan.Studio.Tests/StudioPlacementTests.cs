using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The places a media item stands in with words of their own (R-MED-07, ADR 0021) as the form of the item has
/// them: a row to begin the list with, an entity or an event to name in every row, the caption of a place in
/// the other language beside it, and nothing left in the record that a row did not say.
/// </summary>
public sealed class StudioPlacementTests : IDisposable
{
    private const string Collection = Studio.Collection;

    // The item shows Rostam and Sohrab and the event of their fight; it stands in no place of its own yet.
    private const string Item = $"/en/{Collection}/record/media/media-met-453265";
    private static readonly (string, string?)[] WhoAndWhy = [("Filling.By", "a writer"), ("Filling.Reason", "Placed.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioPlacementTests() => _desk = new Desk(_studio);

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

    private static string? Standing(StudioPage page, string name) =>
        Desk.Standing(FieldsForm(page)).Where(input => input.Name == name).Select(input => input.Value).SingleOrDefault();

    [Fact]
    public async Task An_item_that_stands_in_no_place_has_the_row_that_begins_them()
    {
        var page = await _desk.Get(Item);
        var form = FieldsForm(page);

        Assert.False((await Written()).ContainsKey("placements"));
        Assert.Contains("<legend><code>placements</code></legend>", form, StringComparison.Ordinal);

        // What the place names – an entity or an event, nothing chosen –, its caption, its sources, and the
        // caption in Persian beside the caption.
        Assert.Equal(string.Empty, Standing(page, "f.placements[0].entityId"));
        Assert.Equal(string.Empty, Standing(page, "f.placements[0].eventId"));
        Assert.Equal(string.Empty, Standing(page, "f.placements[0].caption"));
        Assert.Equal(string.Empty, Standing(page, "t.fa.placements[0].caption"));
        Assert.Contains("name=\"f.placements[0].sourceIds\"", form, StringComparison.Ordinal);
        Assert.True(form.IndexOf("name=\"f.placements[0].caption\"", StringComparison.Ordinal) < form.IndexOf("name=\"t.fa.placements[0].caption\"", StringComparison.Ordinal));

        // Sent as it stands, the form adds nothing: a row nothing is written into is no place.
        var sent = await _desk.Fill(Item, "fields", WhoAndWhy);
        Assert.EndsWith("?done=unchanged", sent.Arrived, StringComparison.Ordinal);
        Assert.False((await Written()).ContainsKey("placements"));
    }

    [Fact]
    public async Task Places_are_added_one_for_an_entity_and_one_for_an_event_each_with_its_Persian_caption()
    {
        var first = await _desk.Fill(Item, "fields",
            [("f.placements[0].entityId", "character-sohrab"), ("f.placements[0].caption", "Sohrab, as the painter of 1610 saw him."), ("f.placements[0].sourceIds", "src-ganjoor-lineage"),
             ("t.fa.placements[0].caption", "سهراب، چنان‌که نگارگر دید."), .. WhoAndWhy]);

        Assert.True(first.Arrived is not null, first.Text[Math.Max(0, first.Text.IndexOf("Not done", StringComparison.Ordinal))..]);
        Assert.Equal(
            "[{\"entityId\":\"character-sohrab\",\"caption\":\"Sohrab, as the painter of 1610 saw him.\",\"sourceIds\":[\"src-ganjoor-lineage\"]}]",
            (await Written())["placements"]!.ToJsonString(new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        Assert.Equal("سهراب، چنان‌که نگارگر دید.", Standing(first, "t.fa.placements[0].caption"));

        // The row that is added offers an event too, though the list has a place of an entity only.
        Assert.Equal(string.Empty, Standing(first, "f.placements[1].eventId"));
        var second = await _desk.Fill(Item, "fields",
            [("f.placements[1].eventId", "event-06"), ("f.placements[1].caption", "The fight, told by the picture."), ("t.fa.placements[1].caption", "نبرد، به روایت نگاره."), .. WhoAndWhy]);

        Assert.True(second.Arrived is not null, second.Text[Math.Max(0, second.Text.IndexOf("Not done", StringComparison.Ordinal))..]);
        var places = (await Written())["placements"]!.AsArray();
        Assert.Equal(2, places.Count);
        Assert.Equal("{\"eventId\":\"event-06\",\"caption\":\"The fight, told by the picture.\"}", places[1]!.ToJsonString());
        Assert.Equal("سهراب، چنان‌که نگارگر دید.", Standing(second, "t.fa.placements[0].caption"));
        Assert.Equal("نبرد، به روایت نگاره.", Standing(second, "t.fa.placements[1].caption"));

        // The caption of the item itself, and its Persian one, are what they were.
        Assert.Equal(Standing(await _desk.Get(Item), "t.fa.caption"), Standing(second, "t.fa.caption"));

        // A place whose caption is emptied is taken out, and its Persian caption with it; the last one taken
        // out leaves an item without a list of places.
        var less = await _desk.Fill(Item, "fields", [("f.placements[0].caption", string.Empty), .. WhoAndWhy]);
        Assert.EndsWith("?done=changed", less.Arrived, StringComparison.Ordinal);
        Assert.Equal("event-06", (string)(await Written())["placements"]!.AsArray().Single()!["eventId"]!);
        Assert.DoesNotContain("چنان‌که نگارگر دید", FieldsForm(less), StringComparison.Ordinal);
        Assert.Equal("نبرد، به روایت نگاره.", Standing(less, "t.fa.placements[0].caption"));

        var none = await _desk.Fill(Item, "fields", [("f.placements[0].caption", string.Empty), .. WhoAndWhy]);
        Assert.EndsWith("?done=changed", none.Arrived, StringComparison.Ordinal);
        Assert.False((await Written()).ContainsKey("placements"));
        Assert.DoesNotContain("به روایت نگاره", FieldsForm(none), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_place_the_item_does_not_show_is_refused_by_the_readers_and_the_form_stays_as_it_was_filled_in()
    {
        var page = await _desk.Fill(Item, "fields",
            [("f.placements[0].entityId", "character-zal"), ("f.placements[0].caption", "Zal, who is not in the picture."), ("t.fa.placements[0].caption", "زال"), .. WhoAndWhy]);

        Assert.Null(page.Arrived);
        Assert.Contains("a placement names character-zal, which the item does not say it shows.", page.Text, StringComparison.Ordinal);
        Assert.Equal("character-zal", Standing(page, "f.placements[0].entityId"));
        Assert.Equal("Zal, who is not in the picture.", Standing(page, "f.placements[0].caption"));
        Assert.Equal("زال", Standing(page, "t.fa.placements[0].caption"));
        Assert.False((await Written()).ContainsKey("placements"));
    }
}
