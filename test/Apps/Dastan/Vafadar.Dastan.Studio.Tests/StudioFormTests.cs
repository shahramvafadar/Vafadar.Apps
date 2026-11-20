using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Vafadar.Dastan.Store.Tests;
using Vafadar.Dastan.Studio.Site;

namespace Vafadar.Dastan.Studio.Tests;

public sealed partial class Desk
{
    /// <summary>
    /// Sends the form of that name as a browser sends it – every input with what it stands at – after the given
    /// inputs were filled in. An input given with nothing is left out, as a box that was unticked is.
    /// </summary>
    public async Task<StudioPage> Fill(string address, string form, params (string Name, string? Value)[] changes)
    {
        var page = await Get(address);
        var markup = Forms().Matches(page.Html).Select(match => match.Value).Single(candidate => candidate.Contains($"name=\"_handler\" value=\"{form}\"", StringComparison.Ordinal));
        var action = WebUtility.HtmlDecode(Action().Match(markup).Groups[1].Value);
        var changed = changes.Select(change => change.Name).ToHashSet(StringComparer.Ordinal);
        var sent = Standing(markup).Where(input => !changed.Contains(input.Name)).ToList();
        sent.AddRange(changes.Where(change => change.Value is not null).Select(change => (change.Name, change.Value!)));
        return await Post($"/en/{action}", sent);
    }

    /// <summary>Returns the inputs of a form with what a browser would send for each.</summary>
    public static IEnumerable<(string Name, string Value)> Standing(string markup)
    {
        foreach (Match input in Regex.Matches(markup, "<input\\b[^>]*>"))
        {
            var type = Attribute(input.Value, "type") ?? "text";
            if (Attribute(input.Value, "name") is { } name && (type is not ("checkbox" or "radio") || Regex.IsMatch(input.Value, "\\schecked\\b")))
            {
                yield return (name, Attribute(input.Value, "value") ?? string.Empty);
            }
        }

        foreach (Match area in Regex.Matches(markup, "<textarea\\b([^>]*)>(.*?)</textarea>", RegexOptions.Singleline))
        {
            yield return (Attribute(area.Groups[1].Value, "name")!, WebUtility.HtmlDecode(area.Groups[2].Value));
        }

        foreach (Match select in Regex.Matches(markup, "<select\\b([^>]*)>(.*?)</select>", RegexOptions.Singleline))
        {
            var options = Regex.Matches(select.Groups[2].Value, "<option\\b[^>]*>").Select(option => option.Value).ToList();
            var chosen = options.FirstOrDefault(option => Regex.IsMatch(option, "\\sselected\\b")) ?? options[0];
            yield return (Attribute(select.Groups[1].Value, "name")!, Attribute(chosen, "value") ?? string.Empty);
        }
    }

    private static string? Attribute(string tag, string name) =>
        Regex.Match(tag, $"\\s{name}=\"([^\"]*)\"") is { Success: true } found ? WebUtility.HtmlDecode(found.Groups[1].Value) : null;
}

/// <summary>
/// A record as a form (step 11.6): every field with the input that fits it, made from the record itself and the
/// vocabulary of the readers; what the form sends is the record again, with what was filled in.
/// </summary>
public sealed class StudioFormTests : IDisposable
{
    private const string Collection = Studio.Collection;
    private const string Rostam = $"/en/{Collection}/record/entities/character-rostam";
    private const string Relation = $"/en/{Collection}/record/relations/relation-02";
    private const string Event = $"/en/{Collection}/record/events/event-01";
    private static readonly (string, string?)[] WhoAndWhy = [("Filling.By", "a writer"), ("Filling.Reason", "Filled in.")];

    private readonly Studio _studio = new();
    private readonly Desk _desk;

    public StudioFormTests() => _desk = new Desk(_studio);

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

    // --- the form as it is made ---

    [Fact]
    public void Every_record_of_the_collection_comes_back_from_its_form_as_it_was()
    {
        using var copy = new WorkingCopy();
        using var store = copy.Filled(files: true);
        var context = FormContext.Of(store, Collection);
        var records = 0;
        foreach (var group in new[] { "sources", "entities", "relations", "events", "media" })
        {
            foreach (var info in store.Records(Collection, group))
            {
                var record = WorkingCopy.Stored(store, group, info.Id);
                var sent = new FormCollection(Sent(RecordForm.Of(group, record, context))
                    .GroupBy(input => input.Name, StringComparer.Ordinal)
                    .ToDictionary(input => input.Key, input => new StringValues([.. input.Select(value => value.Value)]), StringComparer.Ordinal));
                var problems = new List<string>();

                var read = RecordForm.Read(group, record, sent, context, problems);

                Assert.Empty(problems);
                var differing = record.Where(field => !JsonNode.DeepEquals(field.Value, read[field.Key])).Select(field => $"{field.Key}: {field.Value?.ToJsonString()} / {read[field.Key]?.ToJsonString()}");
                Assert.True(JsonNode.DeepEquals(record, read), $"{info.Id}: {string.Join("; ", differing)}");
                records++;
            }
        }

        Assert.True(records > 50);
    }

    // What a browser sends for the fields as they stand.
    private static IEnumerable<(string Name, string Value)> Sent(IEnumerable<FormField> fields)
    {
        foreach (var field in fields)
        {
            switch (field.Kind)
            {
                case FieldKind.Flag:
                    if (field.Text == "true")
                    {
                        yield return (field.Path, "true");
                    }

                    break;
                case FieldKind.References:
                    foreach (var chosen in field.Chosen)
                    {
                        yield return (field.Path, chosen);
                    }

                    break;
                case FieldKind.Group:
                    foreach (var part in Sent(field.Parts))
                    {
                        yield return part;
                    }

                    break;
                case FieldKind.Rows or FieldKind.Facts:
                    foreach (var part in field.Rows.SelectMany(Sent))
                    {
                        yield return part;
                    }

                    break;
                default:
                    yield return (field.Path, field.Text);
                    break;
            }
        }
    }

    [Fact]
    public void The_fields_of_an_event_get_the_inputs_that_fit_what_they_hold()
    {
        using var copy = new WorkingCopy();
        using var store = copy.Filled(files: true);

        var fields = RecordForm.Of("events", WorkingCopy.Stored(store, "events", "event-01"), FormContext.Of(store, Collection)).ToDictionary(field => field.Name);

        // The identifier and the state of the review are not for a form to change.
        Assert.DoesNotContain("id", fields.Keys);
        Assert.DoesNotContain("reviewStatus", fields.Keys);
        Assert.Equal(FieldKind.Text, fields["label"].Kind);
        Assert.Equal(FieldKind.LongText, fields["summary"].Kind);
        Assert.Equal(FieldKind.Number, fields["order"].Kind);
        Assert.Equal(FieldKind.Choice, fields["orderBasis"].Kind);
        Assert.Contains(fields["orderBasis"].Options, option => option.Value == "narrative");
        Assert.Equal(FieldKind.Choice, fields["spoilerLevel"].Kind);
        Assert.Equal("0 – Opening and orientation", fields["spoilerLevel"].Options[0].Label);
        Assert.Equal(FieldKind.References, fields["sourceIds"].Kind);
        Assert.Contains(fields["sourceIds"].Options, option => option.Value == "src-ganjoor-s02");
        Assert.Equal(FieldKind.Group, fields["temporalExpression"].Kind);
        Assert.Equal(FieldKind.Choice, fields["temporalExpression"].Parts.Single(part => part.Name == "kind").Kind);
        Assert.Equal(FieldKind.Json, fields["temporalExpression"].Parts.Single(part => part.Name == "duration").Kind);
        Assert.Equal(FieldKind.Rows, fields["entityAssociations"].Kind);
        Assert.Equal(6, fields["entityAssociations"].Rows.Count);
        Assert.Equal("f.entityAssociations[1].role", fields["entityAssociations"].Rows[1].Single(part => part.Name == "role").Path);
        Assert.Equal(FieldKind.Choice, fields["entityAssociations"].Rows[0].Single(part => part.Name == "entityId").Kind);
    }

    // --- the form in the tool ---

    [Fact]
    public async Task The_page_of_a_record_has_its_fields_as_a_form()
    {
        var page = await _desk.Get(Relation);
        var form = FieldsForm(page);

        Assert.Contains("Change the fields of the record", page.Text, StringComparison.Ordinal);
        Assert.Matches("<select id=\"field-f-type\" name=\"f.type\">.*<option value=\"parentOf\" selected", form.ReplaceLineEndings(" "));
        Assert.Matches("<input type=\"checkbox\" name=\"f.sourceIds\" value=\"src-iranica-zal\" checked", form);
        Assert.DoesNotMatch("value=\"src-ganjoor-index\" checked", form);
        Assert.Contains("name=\"f.scope.kind\"", form, StringComparison.Ordinal);
        Assert.Contains("<label for=\"field-f-label\"><code>label</code></label>", form, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"f.id\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"f.reviewStatus\"", form, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Rostam)]
    [InlineData(Relation)]
    [InlineData(Event)]
    [InlineData($"/en/{Collection}/record/sources/src-ganjoor-index")]
    public async Task The_form_sent_as_it_stands_changes_nothing(string address)
    {
        var page = await _desk.Fill(address, "fields", WhoAndWhy);

        Assert.EndsWith("?done=unchanged", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("Nothing had to be changed.", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_text_a_choice_and_the_sources_are_filled_in_and_kept_with_who_and_why()
    {
        var page = await _desk.Fill(Relation, "fields",
            [("f.label", "Father and first teacher of"), ("f.spoilerLevel", "1"), ("f.sourceIds", "src-ganjoor-lineage"), ("Filling.By", "a writer"), ("Filling.Reason", "Read again.")]);

        Assert.EndsWith("relation-02?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Contains("The change was kept.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Reason: Read again.", page.Text, StringComparison.Ordinal);
        var record = await Written(Relation);
        Assert.Equal("Father and first teacher of", (string)record["label"]!);
        Assert.Equal(1, (int)record["spoilerLevel"]!);
        Assert.Equal("[\"src-ganjoor-lineage\"]", record["sourceIds"]!.ToJsonString());

        // What a form does not show stays as it was.
        Assert.Equal("relation-02", (string)record["id"]!);
        Assert.Equal("source_backed_editorial_review_pending", (string)record["reviewStatus"]!);
    }

    [Fact]
    public async Task A_row_is_added_by_filling_in_the_last_one()
    {
        var before = (await Written(Event))["entityAssociations"]!.AsArray();

        var page = await _desk.Fill(Event, "fields",
            [("f.entityAssociations[5].entityId", "character-zal"), ("f.entityAssociations[5].role", "father named by the host"), ("f.entityAssociations[5].presence", "setting"), .. WhoAndWhy]);

        Assert.True(page.Arrived is not null, page.Text[Math.Max(0, page.Text.IndexOf("Not done", StringComparison.Ordinal))..]);
        var rows = (await Written(Event))["entityAssociations"]!.AsArray();
        Assert.Equal(before.Count + 1, rows.Count);
        Assert.Equal("{\"entityId\":\"character-zal\",\"role\":\"father named by the host\",\"presence\":\"setting\"}", rows[^1]!.ToJsonString());
        Assert.True(JsonNode.DeepEquals(before[0], rows[0]));
    }

    [Fact]
    public void A_row_whose_texts_are_emptied_is_taken_out()
    {
        using var copy = new WorkingCopy();
        using var store = copy.Filled(files: true);
        var context = FormContext.Of(store, Collection);
        var record = WorkingCopy.Stored(store, "events", "event-01");
        var sent = Sent(RecordForm.Of("events", record, context)).Where(input => input.Name != "f.entityAssociations[4].role")
            .GroupBy(input => input.Name, StringComparer.Ordinal).ToDictionary(input => input.Key, input => new StringValues([.. input.Select(value => value.Value)]), StringComparer.Ordinal);
        var problems = new List<string>();

        var rows = RecordForm.Read("events", record, new FormCollection(sent), context, problems)["entityAssociations"]!.AsArray();

        Assert.Empty(problems);
        Assert.Equal(4, rows.Count);
        Assert.DoesNotContain(rows, row => (string)row!["entityId"]! == "place-samangan");
    }

    // --- the texts of the other languages (step 11.8) ---

    private static string Standing(StudioPage page, string name) =>
        Desk.Standing(FieldsForm(page)).Single(input => input.Name == name).Value;

    [Fact]
    public async Task The_text_of_a_field_in_Persian_stands_below_the_field()
    {
        var page = await _desk.Get(Event);
        var form = FieldsForm(page);

        Assert.Equal("اسبی گم‌شده و پذیرایی", Standing(page, "t.fa.label"));
        Assert.Equal("جای پذیرایی", Standing(page, "t.fa.entityAssociations[4].role"));
        Assert.Equal(string.Empty, Standing(page, "t.fa.entityAssociations[5].role"));
        Assert.Matches("name=\"t\\.fa\\.label\"[^>]*lang=\"fa\"", form);
        Assert.Contains("<span class=\"language\"> – Persian (fa)</span>", form, StringComparison.Ordinal);
        Assert.True(form.IndexOf("name=\"f.label\"", StringComparison.Ordinal) < form.IndexOf("name=\"t.fa.label\"", StringComparison.Ordinal));
        Assert.True(form.IndexOf("name=\"t.fa.label\"", StringComparison.Ordinal) < form.IndexOf("name=\"f.summary\"", StringComparison.Ordinal));

        // A field no translation may translate has no input for one.
        Assert.DoesNotContain("name=\"t.fa.orderBasis\"", form, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"t.fa.title\"", FieldsForm(await _desk.Get($"/en/{Collection}/record/sources/src-ganjoor-index")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_Persian_text_is_changed_and_one_left_empty_is_taken_out()
    {
        var page = await _desk.Fill(Event, "fields", [("t.fa.label", "اسبی گم‌شده و مهمانی"), ("t.fa.summary", " "), .. WhoAndWhy]);

        Assert.EndsWith("event-01?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Equal("اسبی گم‌شده و مهمانی", Standing(page, "t.fa.label"));
        Assert.Equal(string.Empty, Standing(page, "t.fa.summary"));

        // The record itself says what it said: only the Persian texts were changed.
        Assert.Equal("A missing horse and a welcome", (string)(await Written(Event))["label"]!);
        Assert.DoesNotContain("version 3", page.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_row_is_taken_out_together_with_its_Persian_role()
    {
        var page = await _desk.Fill(Event, "fields", [("f.entityAssociations[4].role", string.Empty), .. WhoAndWhy]);

        Assert.EndsWith("event-01?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Equal(4, (await Written(Event))["entityAssociations"]!.AsArray().Count);
        Assert.DoesNotContain("جای پذیرایی", FieldsForm(page), StringComparison.Ordinal);
        Assert.Equal("سرزمین مرزی شکار آغازین", Standing(page, "t.fa.entityAssociations[3].role"));
    }

    [Fact]
    public async Task A_row_is_added_with_its_Persian_role()
    {
        var page = await _desk.Fill(Event, "fields",
            [("f.entityAssociations[5].entityId", "character-zal"), ("f.entityAssociations[5].role", "father named by the host"), ("f.entityAssociations[5].presence", "setting"),
             ("t.fa.entityAssociations[5].role", "پدری که میزبان از او نام می‌برد"), .. WhoAndWhy]);

        Assert.EndsWith("event-01?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.Equal("پدری که میزبان از او نام می‌برد", Standing(page, "t.fa.entityAssociations[5].role"));
        Assert.Equal("مسافری در جست‌وجوی رخش", Standing(page, "t.fa.entityAssociations[0].role"));
    }

    [Fact]
    public async Task A_refused_form_keeps_the_Persian_texts_as_they_were_filled_in()
    {
        var page = await _desk.Fill(Event, "fields", [("f.order", "first"), ("t.fa.label", "اسبی گم‌شده و مهمانی"), .. WhoAndWhy]);

        Assert.Null(page.Arrived);
        Assert.Equal("اسبی گم‌شده و مهمانی", Standing(page, "t.fa.label"));
        Assert.Equal("اسبی گم‌شده و پذیرایی", Standing(await _desk.Get(Event), "t.fa.label"));
    }

    [Fact]
    public async Task A_text_left_empty_takes_its_field_out_of_the_record()
    {
        var page = await _desk.Fill($"/en/{Collection}/record/sources/src-ganjoor-index", "fields", [("f.reuseNote", " "), .. WhoAndWhy]);

        Assert.EndsWith("?done=changed", page.Arrived, StringComparison.Ordinal);
        Assert.False((await Written($"/en/{Collection}/record/sources/src-ganjoor-index")).ContainsKey("reuseNote"));
    }

    [Fact]
    public async Task What_the_readers_refuse_is_not_kept_and_the_form_stays_as_it_was_filled_in()
    {
        var page = await _desk.Fill(Rostam, "fields", [("f.label", string.Empty), ("f.summary", "A third summary."), .. WhoAndWhy]);

        Assert.Null(page.Arrived);
        Assert.Contains("Not done. Nothing was changed.", page.Text, StringComparison.Ordinal);
        var form = FieldsForm(page);
        Assert.Contains("A third summary.", form, StringComparison.Ordinal);
        Assert.Contains("name=\"f.label\"", form, StringComparison.Ordinal);
        Assert.Contains("value=\"Filled in.\"", form, StringComparison.Ordinal);
        Assert.Equal("A corrected summary.", (string)(await Written(Rostam))["summary"]!);
    }

    [Fact]
    public async Task A_number_that_is_none_and_a_form_without_who_and_why_are_refused()
    {
        var page = await _desk.Fill(Event, "fields", ("f.order", "first"), ("Filling.By", " "), ("Filling.Reason", string.Empty));

        Assert.Null(page.Arrived);
        Assert.Contains("order: \"first\" is not a number.", page.Text, StringComparison.Ordinal);
        Assert.Contains("Say who you are.", page.Text, StringComparison.Ordinal);
        Assert.Equal(1, (int)(await Written(Event))["order"]!);
    }

    [Fact]
    public async Task The_form_is_there_in_Persian()
    {
        var page = await _studio.Get($"/fa/{Collection}/record/entities/character-rostam");

        Assert.Contains("تغییر فیلدهای رکورد", page.Text, StringComparison.Ordinal);
        Assert.Contains("تغییر رکورد به‌صورت متن", page.Text, StringComparison.Ordinal);
    }
}
