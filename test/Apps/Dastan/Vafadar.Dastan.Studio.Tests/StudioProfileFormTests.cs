using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Studio.Tests;

/// <summary>
/// The profile of an entity in the tool of the editors (step 19.5, ADR 0017): the form asks for every fact the
/// kind of the entity has, says which essential one is missing, and writes a fact with exactly what it says –
/// words with their Persian, another entity, or a time.
/// </summary>
public sealed class StudioProfileFormTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Rostam = $"/en/{Collection}/record/entities/character-rostam";
    private const string Zal = $"/en/{Collection}/record/entities/character-zal";
    private static readonly (string, string?)[] WhoAndWhy = [("Filling.By", "a writer"), ("Filling.Reason", "Filled in.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioProfileFormTests() => _desk = new Desk(_studio);

    public void Dispose()
    {
        _desk.Dispose();
        _studio.Dispose();
    }

    private async Task<JsonObject> Written(string address)
    {
        var page = await _desk.Get(address);
        return JsonNode.Parse(WebUtility.HtmlDecode(Regex.Match(page.Main, "<textarea id=\"change-json\"[^>]*>(.*?)</textarea>", RegexOptions.Singleline).Groups[1].Value))!.AsObject();
    }

    private static string FieldsForm(StudioPage page) =>
        Regex.Matches(page.Html, "<form\\b.*?</form>", RegexOptions.Singleline).Select(match => match.Value).Single(form => form.Contains("name=\"_handler\" value=\"fields\"", StringComparison.Ordinal));

    private static string Standing(StudioPage page, string name) =>
        Desk.Standing(FieldsForm(page)).Single(input => input.Name == name).Value;

    [Fact]
    public async Task The_form_asks_for_every_fact_of_the_kind_and_says_which_essential_one_is_missing()
    {
        var stated = FieldsForm(await _desk.Get(Rostam));

        // What the record states stands first, with its Persian words; what its kind may add follows.
        Assert.Contains("id=\"profile-facts\"", stated, StringComparison.Ordinal);
        Assert.Equal(["role", "origin"], Regex.Matches(stated, "name=\"f\\.facts\\[\\d\\]\\.fact\" value=\"(\\w+)\"").Select(match => match.Groups[1].Value));
        Assert.Contains("name=\"f.facts[0].text\" value=\"A warrior\"", stated, StringComparison.Ordinal);
        Assert.Contains("name=\"t.fa.facts[0].text\" value=\"پهلوان\"", WebUtility.HtmlDecode(stated), StringComparison.Ordinal);
        Assert.Matches("name=\"f\\.facts\\[0\\]\\.sourceIds\" value=\"src-ganjoor-s02\" checked", stated);
        Assert.Contains("name=\"f.facts[1].entityId\"", stated, StringComparison.Ordinal);
        Assert.DoesNotContain("essential, not stated", stated, StringComparison.Ordinal);
        Assert.Contains("What it is in the story, in words that give nothing of the story away. Essential.", stated, StringComparison.Ordinal);

        // An entity that states none of them: every fact of its kind is asked for, and the essential one is marked.
        var bare = FieldsForm(await _desk.Get(Zal));
        Assert.Equal(["role", "origin"], Regex.Matches(bare, "name=\"f\\.facts\\[\\d\\]\\.fact\" value=\"(\\w+)\"").Select(match => match.Groups[1].Value));
        Assert.Single(Regex.Matches(bare, "essential, not stated"));
        Assert.Contains("class=\"field part row missing\"", bare, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_fact_is_stated_with_its_words_in_both_languages_and_another_names_an_entity()
    {
        var page = await _desk.Fill(Zal, "fields",
            [("f.facts[0].text", "Rostam's father"), ("t.fa.facts[0].text", "پدر رستم"), ("f.facts[0].sourceIds", "src-iranica-zal"),
             ("f.facts[1].entityId", "place-turan"), ("f.facts[1].spoilerLevel", "1"), .. WhoAndWhy]);

        Assert.EndsWith("character-zal?done=changed", page.Arrived, StringComparison.Ordinal);
        var facts = (await Written(Zal))["facts"]!.AsArray();
        Assert.Equal(
            """[{"fact":"role","text":"Rostam's father","sourceIds":["src-iranica-zal"]},{"fact":"origin","entityId":"place-turan","spoilerLevel":1}]""",
            facts.ToJsonString(new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        Assert.Equal("پدر رستم", Standing(page, "t.fa.facts[0].text"));
        Assert.DoesNotContain("essential, not stated", FieldsForm(page), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_fact_whose_words_are_emptied_is_taken_out_with_its_Persian_words()
    {
        var page = await _desk.Fill(Rostam, "fields", [("f.facts[0].text", " "), .. WhoAndWhy]);

        Assert.EndsWith("character-rostam?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Null((await Written(Rostam))["facts"]);
        Assert.Equal(string.Empty, Standing(page, "t.fa.facts[0].text"));
        Assert.Single(Regex.Matches(FieldsForm(page), "essential, not stated"));
    }

    [Fact]
    public async Task Who_publishes_reads_what_the_profiles_lack_as_notes_that_stop_nothing()
    {
        var page = await _desk.Get($"/en/{Collection}/revisions");

        Assert.Contains("id=\"profile-notes\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("entities have a profile that lacks something", page.Text, StringComparison.Ordinal);
        Assert.Contains("These are notes, not faults", page.Text, StringComparison.Ordinal);
        Assert.Contains($"href=\"{Collection}/record/entities/character-zal#profile-facts\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("essential facts not stated: role", page.Text, StringComparison.Ordinal);

        // The form that publishes is there all the same.
        Assert.Contains("name=\"_handler\" value=\"publish\"", page.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_fact_that_does_not_fit_is_refused_by_the_readers_and_the_form_keeps_what_was_filled_in()
    {
        // The origin of a character is a place; a character is none.
        var page = await _desk.Fill(Zal, "fields", [("f.facts[1].entityId", "character-rostam"), .. WhoAndWhy]);

        Assert.Contains("character-zal, the fact origin: it names character-rostam, which is no place.", page.Text, StringComparison.Ordinal);
        Assert.Null((await Written(Zal))["facts"]);
    }
}
