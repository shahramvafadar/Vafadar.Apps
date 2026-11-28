using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>What kind of input a field of a record is filled in with.</summary>
public enum FieldKind
{
    /// <summary>A short text.</summary>
    Text = 0,

    /// <summary>A text of several lines.</summary>
    LongText = 1,

    /// <summary>A number.</summary>
    Number = 2,

    /// <summary>Yes or no.</summary>
    Flag = 3,

    /// <summary>One of a set of values: a value of the vocabulary, a reading stage, or a record of the collection.</summary>
    Choice = 4,

    /// <summary>Any number of records of the collection.</summary>
    References = 5,

    /// <summary>Texts, one in a line.</summary>
    Lines = 6,

    /// <summary>A part of the record with fields of its own.</summary>
    Group = 7,

    /// <summary>A list of parts that all have the same fields.</summary>
    Rows = 8,

    /// <summary>A part the form has no inputs for; it is written as it stands in the file.</summary>
    Json = 9,

    /// <summary>A time of the calendar: what happened once or between two times, or what lasted.</summary>
    Time = 10,

    /// <summary>The facts of the profile of an entity: one row for every fact its kind has (ADR 0017).</summary>
    Facts = 11,

    /// <summary>The fact a row of the profile states: its key, what it means, and whether the kind needs it.</summary>
    Fact = 12,
}

/// <summary>A value a field can be given, with what the editor is shown for it.</summary>
/// <param name="Value">The value as it is written into the record.</param>
/// <param name="Label">What the editor reads.</param>
public sealed record FieldOption(string Value, string Label);

/// <summary>One field of a record as a form shows it.</summary>
public sealed class FormField
{
    /// <summary>Gets the name of the input in the form.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the name of the field in the record.</summary>
    public required string Name { get; init; }

    /// <summary>Gets what the field is filled in with.</summary>
    public required FieldKind Kind { get; init; }

    /// <summary>Gets the field as the record has it.</summary>
    public JsonNode? Value { get; init; }

    /// <summary>Gets the values a choice or a list of references can take.</summary>
    public IReadOnlyList<FieldOption> Options { get; init; } = [];

    /// <summary>Gets the fields of a group.</summary>
    public IReadOnlyList<FormField> Parts { get; init; } = [];

    /// <summary>Gets the rows of a list, each with its fields; the last one is empty, for a part to add.</summary>
    public IReadOnlyList<IReadOnlyList<FormField>> Rows { get; init; } = [];

    /// <summary>Gets the language of the text, when the field is a text of the record in another language.</summary>
    public string? Language { get; init; }

    /// <summary>Gets what the field means, said beside it; nothing for a field that is named by the file alone.</summary>
    public string? Hint { get; init; }

    /// <summary>Gets a value indicating whether the field is one the record should have and does not.</summary>
    public bool Missing { get; init; }

    /// <summary>Gets the text an input starts with.</summary>
    public string Text => Value switch
    {
        null => Kind == FieldKind.Json ? "null" : string.Empty,
        JsonValue value when Kind is not FieldKind.Json && value.TryGetValue<string>(out var text) => text,
        JsonArray lines when Kind == FieldKind.Lines => string.Join('\n', lines.Select(line => line!.GetValue<string>())),
        _ when Kind == FieldKind.Json => Value.ToJsonString(RecordForm.Written),
        _ => Value.ToJsonString(),
    };

    /// <summary>Gets the values of a list of references that the record has.</summary>
    public IReadOnlySet<string> Chosen =>
        Value is JsonArray items ? items.Select(item => item!.GetValue<string>()).ToHashSet(StringComparer.Ordinal) : new HashSet<string>(StringComparer.Ordinal);
}

/// <summary>What a form of fields needs to know of the collection of its record.</summary>
/// <param name="Stages">The reading stages, by their level.</param>
/// <param name="Records">The records of each group a field can point to, by their identifier and name.</param>
public sealed record FormContext(IReadOnlyList<FieldOption> Stages, IReadOnlyDictionary<string, IReadOnlyList<FieldOption>> Records)
{
    /// <summary>
    /// Gets what a row of a list looks like, by the place of the list, for a list that has no row yet: without
    /// it an empty list has no inputs to add its first row with.
    /// </summary>
    public IReadOnlyDictionary<string, JsonObject> Shapes { get; init; } = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

    /// <summary>Reads what a form needs from the store.</summary>
    public static FormContext Of(EditorialStore store, string collection)
    {
        ArgumentNullException.ThrowIfNull(store);
        var records = new[] { "sources", "entities", "relations", "events", "media" }.ToDictionary(
            group => group,
            group => (IReadOnlyList<FieldOption>)[.. store.Records(collection, group).Select(record => new FieldOption(record.Id, $"{record.Label} ({record.Id})"))]);

        // The themes of the collection are entities an item is gathered under and never shows: a form offers
        // them for the themes of an item, and everything else for what an item shows.
        var themes = store.EntitiesOfType(collection, RecordForm.ThemeType);
        records[RecordForm.ThemeGroup] = [.. records["entities"].Where(entity => themes.Contains(entity.Value))];
        records[RecordForm.ShownGroup] = [.. records["entities"].Where(entity => !themes.Contains(entity.Value))];
        return new FormContext(
            [.. store.Stages(collection).Select(stage => new FieldOption(stage.Level.ToString(CultureInfo.InvariantCulture), $"{stage.Level} – {stage.Label}"))],
            records);
    }
}

/// <summary>
/// A record as a form (step 11.6): every field the record has gets the input that fits what it holds – a text,
/// a number, a choice of the vocabulary of the readers, a reading stage, records of the collection to point to,
/// rows for a list of parts. The form is made from the record as it is written and from what the readers list as
/// their vocabulary; it has no description of a record of its own. A part it has no inputs for is shown as it is
/// written. The form changes the fields a record has; a field is added or taken away in the record as text.
/// </summary>
public static class RecordForm
{
    /// <summary>How a part without inputs is written into its field.</summary>
    internal static readonly JsonSerializerOptions Written = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // The identifier of a record does not change, and the state of a review is what a review records.
    private static readonly FrozenSet<string> Kept = new[] { "id", "reviewStatus", "review" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The type of an entity that is a theme of a gallery, as a record writes it.</summary>
    internal const string ThemeType = "Theme";

    /// <summary>The records a form offers for the themes of an item: the entities that are themes.</summary>
    internal const string ThemeGroup = "themes";

    /// <summary>The records a form offers for what an item shows: the entities that are no themes.</summary>
    internal const string ShownGroup = "shown";

    // The records a field can point to. A context that does not tell themes from what can be shown offers every entity for what is shown.
    private static bool Targets(FormContext context, string group, out IReadOnlyList<FieldOption> records) =>
        context.Records.TryGetValue(group, out records!) || (group == ShownGroup && context.Records.TryGetValue("entities", out records!));

    // The themes a media item is gathered under (R-MED-02): offered for every item of a collection that has themes.
    private const string Themes = "themeIds";

    // The fields that name records of the collection, and the group of those records.
    private static readonly FrozenDictionary<string, string> Pointing = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["sourceIds"] = "sources",
        ["depictedEntityIds"] = ShownGroup,
        [Themes] = ThemeGroup,
        ["depictedEventIds"] = "events",
        ["relatedRelationIds"] = "relations",
        ["eventIds"] = "events",
        ["from"] = "entities",
        ["to"] = "entities",
        ["storyId"] = "entities",
        ["entityId"] = "entities",
        ["eventId"] = "events",
        ["mediaId"] = "media",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    // The lists of an entity a form offers also when the entity has none of them yet (step 13.6): its other
    // names, its identifiers in other catalogues, what is stated about it. Each with the row a list begins with.
    private static readonly FrozenDictionary<string, JsonObject> EntityLists = new Dictionary<string, JsonObject>(StringComparer.Ordinal)
    {
        ["names"] = new() { ["name"] = string.Empty, ["language"] = string.Empty, ["kind"] = "alternative" },
        ["identifiers"] = new() { ["issuer"] = string.Empty, ["value"] = string.Empty, ["url"] = string.Empty, ["match"] = "exact" },
        ["dated"] = new() { ["key"] = string.Empty, ["label"] = string.Empty, ["summary"] = string.Empty, ["time"] = null, ["sourceIds"] = new JsonArray(), ["spoilerLevel"] = 0, ["reviewStatus"] = "proposed" },
    }.ToFrozenDictionary(StringComparer.Ordinal);

    // The vocabulary of the readers, by the place of a field: "relations[].scope.kind". The fields of a
    // comparison stand under "comparison[]" except those of its lanes, which the readers name by their place.
    private static readonly FrozenDictionary<string, IReadOnlyList<FieldOption>> Vocabulary = ContentVocabulary.Collection
        .Concat(ContentVocabulary.Comparison.Where(choices => !choices.Field.StartsWith("translation", StringComparison.Ordinal))
            .Select(choices => choices with { Field = string.Join(", ", choices.Field.Split(", ").Select(field => field.StartsWith("lanes[]", StringComparison.Ordinal) ? field : "comparison[]." + field)) }))
        .SelectMany(choices => choices.Field.Split(", ").Select(field => (Field: field, Values: (IReadOnlyList<FieldOption>)[.. choices.Values.Select(value => new FieldOption(value.Name, value.Name))])))
        .ToFrozenDictionary(entry => entry.Field, entry => entry.Values, StringComparer.Ordinal);

    // The texts a translation may translate, by the group of the record – the list of the readers.
    private static readonly FrozenDictionary<string, ContentTexts> Translated = ContentVocabulary.Translated.ToFrozenDictionary(texts => texts.Group, StringComparer.Ordinal);

    // What takes part in a record: the list, who a row names, and the role the row gives – the places of the
    // file that a translation names as "roles", by who takes part.
    private const string Taking = "entityAssociations";
    private const string Who = "entityId";
    private const string Role = "role";

    // The stops of a thread: the list, what a stop names – one of three – and the reason the stop gives: the
    // places of the file that a translation names as "notes", by what the stop names.
    private const string Stops = "stops";
    private const string Note = "note";
    private static readonly string[] StopNames = ["entityId", "eventId", "mediaId"];

    // The places a media item stands in with words of their own (R-MED-07, ADR 0021): the list, what a place
    // names – an entity or an event, one of the two – and the caption the item has there; a translation names
    // the captions as "places", by what the place names. A row is shown with both names to choose from, so
    // that a place of the other kind can be added to a list that has places of one kind only.
    private const string Placements = "placements";
    private const string Caption = "caption";
    private static readonly string[] PlaceNames = ["entityId", "eventId"];

    private static JsonObject PlacementShape() => new() { ["entityId"] = string.Empty, ["eventId"] = string.Empty, [Caption] = string.Empty, ["sourceIds"] = new JsonArray() };

    // The placements of an item with every row in the same form: what it names first, then its caption and its
    // sources, then whatever else the row says. A list that is not a list of rows is left as it is written.
    private static JsonArray Whole(JsonArray places)
    {
        if (places.Any(place => place is not JsonObject))
        {
            return places;
        }

        return new JsonArray([.. places.Select(place =>
        {
            var row = PlacementShape();
            foreach (var (name, value) in place!.AsObject())
            {
                row[name] = value?.DeepClone();
            }

            return (JsonNode?)row;
        })]);
    }

    // The time of a relation: a relation of the world that states none has the inputs to state one.
    private const string Time = "time";

    // The statements with a date of an entity: the list, the key a row names itself by, and the texts of a row
    // a translation translates – the place "dated" of a translation document, by the key.
    // The facts of the profile of an entity: the list, the key a row states, and the three things a fact can
    // say – the place "facts" of a translation document holds the words, by the key of the fact.
    private const string Facts = "facts";
    private const string Fact = "fact";
    private const string Words = "text";

    private const string Dated = "dated";
    private const string Key = "key";
    private static readonly string[] DatedTexts = ["label", "summary"];

    /// <summary>Returns the fields of a record a form shows, in the order of the record.</summary>
    /// <param name="group">The group of the record.</param>
    /// <param name="record">The record.</param>
    /// <param name="context">What the form needs to know of the collection.</param>
    /// <param name="texts">
    /// The texts of the record in the other languages (step 11.8). Each stands below the field it translates: a
    /// text of the record, or the role of a row of what takes part.
    /// </param>
    /// <param name="dated">
    /// For an entity: the texts of its statements with a date in the other languages, by the key of the
    /// statement (step 12.5). Each stands below the text of its row.
    /// </param>
    public static IReadOnlyList<FormField> Of(string group, JsonObject record, FormContext context, IReadOnlyList<(string Language, JsonObject Texts)>? texts = null, IReadOnlyList<(string Language, JsonObject Texts)>? dated = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(context);
        var fields = record.Where(field => !Kept.Contains(field.Key))
            .Select(field => group == "entities" && field.Key == Facts && FactsOf(record, context) is { } profile ? profile
                : group == "media" && field.Key == Placements && field.Value is JsonArray places ? Field("media[]", "f", Placements, Whole(places), context, nested: false)
                : Field($"{group}[]", "f", field.Key, field.Value, context, nested: false))
            .ToList();
        if (StatesNoTime(group, record))
        {
            fields.Add(new FormField { Path = $"f.{Time}", Name = Time, Kind = FieldKind.Time, Parts = TimeForm.Parts($"f.{Time}", null) });
        }

        if (group == "entities")
        {
            // A list the entity does not have yet, with the one row that begins it.
            fields.AddRange(EntityLists.Keys.Order(StringComparer.Ordinal).Where(list => !record.ContainsKey(list))
                .Select(list => Field("entities[]", "f", list, new JsonArray(), context, nested: false)));

            // The facts its kind has, asked for also when the entity states none yet.
            if (!record.ContainsKey(Facts) && FactsOf(record, context) is { } profile)
            {
                fields.Add(profile);
            }
        }

        if (group == "media" && !record.ContainsKey(Themes) && context.Records.TryGetValue(ThemeGroup, out var themes) && themes.Count > 0)
        {
            // The themes of the collection, for an item that is under none yet.
            fields.Add(Field("media[]", "f", Themes, new JsonArray(), context, nested: false));
        }

        if (group == "media" && !record.ContainsKey(Placements))
        {
            // The places of an item that stands in none yet, with the one row that begins them.
            fields.Add(Field("media[]", "f", Placements, new JsonArray(), context, nested: false));
        }

        if (texts is not { Count: > 0 } || !Translated.TryGetValue(group, out var translated))
        {
            return fields;
        }

        var shown = new List<FormField>();
        foreach (var field in fields)
        {
            if (dated is { Count: > 0 } && group == "entities" && field is { Name: Dated, Kind: FieldKind.Rows })
            {
                // The texts of a statement with a date in another language go with its row.
                shown.Add(new FormField
                {
                    Path = field.Path, Name = field.Name, Kind = field.Kind, Value = field.Value,
                    Rows =
                    [
                        .. field.Rows.Select((row, index) => (IReadOnlyList<FormField>)
                        [
                            .. row.SelectMany(part => !DatedTexts.Contains(part.Name) || part.Kind is not (FieldKind.Text or FieldKind.LongText)
                                ? [part]
                                : dated.Select(text => Translation(
                                    $"t.{text.Language}.{Dated}[{index}].{part.Name}", part.Name, text.Language,
                                    TextOf((text.Texts[row.FirstOrDefault(named => named.Name == Key)?.Text ?? string.Empty] as JsonObject)?[part.Name]), part.Kind)).Prepend(part)),
                        ]),
                    ],
                });
                continue;
            }

            if (translated.Facts && field.Kind == FieldKind.Facts)
            {
                // The words of a fact in another language go with its row, by the key of the fact.
                shown.Add(new FormField
                {
                    Path = field.Path, Name = field.Name, Kind = field.Kind, Value = field.Value,
                    Rows =
                    [
                        .. field.Rows.Select((row, index) => (IReadOnlyList<FormField>)
                        [
                            .. row.SelectMany(part => part.Name != Words
                                ? [part]
                                : texts.Select(text => Translation($"t.{text.Language}.{Facts}[{index}].{Words}", Words, text.Language, TextOf((text.Texts[Facts] as JsonObject)?[row[0].Text]), part.Kind)).Prepend(part)),
                        ]),
                    ],
                });
                continue;
            }

            if (translated.Roles && field is { Name: Taking, Kind: FieldKind.Rows })
            {
                // The role of a row in another language goes with its row – also into the row that is added.
                shown.Add(new FormField
                {
                    Path = field.Path, Name = field.Name, Kind = field.Kind, Value = field.Value,
                    Rows =
                    [
                        .. field.Rows.Select((row, index) => (IReadOnlyList<FormField>)
                        [
                            .. row.SelectMany(part => part.Name != Role
                                ? [part]
                                : texts.Select(text => Translation($"t.{text.Language}.{Taking}[{index}].{Role}", Role, text.Language, RoleOf(text.Texts, row), part.Kind)).Prepend(part)),
                        ]),
                    ],
                });
                continue;
            }

            if (translated.Notes && field is { Name: Stops, Kind: FieldKind.Rows })
            {
                // The reason of a stop in another language goes with its stop – also into the stop that is added.
                shown.Add(new FormField
                {
                    Path = field.Path, Name = field.Name, Kind = field.Kind, Value = field.Value,
                    Rows =
                    [
                        .. field.Rows.Select((row, index) => (IReadOnlyList<FormField>)
                        [
                            .. row.SelectMany(part => part.Name != Note
                                ? [part]
                                : texts.Select(text => Translation($"t.{text.Language}.{Stops}[{index}].{Note}", Note, text.Language, NoteOf(text.Texts, row), part.Kind)).Prepend(part)),
                        ]),
                    ],
                });
                continue;
            }

            if (translated.Places && field is { Name: Placements, Kind: FieldKind.Rows })
            {
                // The caption of a place in another language goes with its place – also into the place that is added.
                shown.Add(new FormField
                {
                    Path = field.Path, Name = field.Name, Kind = field.Kind, Value = field.Value,
                    Rows =
                    [
                        .. field.Rows.Select((row, index) => (IReadOnlyList<FormField>)
                        [
                            .. row.SelectMany(part => part.Name != Caption
                                ? [part]
                                : texts.Select(text => Translation($"t.{text.Language}.{Placements}[{index}].{Caption}", Caption, text.Language, PlaceOf(text.Texts, row), part.Kind)).Prepend(part)),
                        ]),
                    ],
                });
                continue;
            }

            shown.Add(field);
            if (field.Kind is FieldKind.Text or FieldKind.LongText && translated.Fields.Contains(field.Name))
            {
                shown.AddRange(texts.Select(text => Translation($"t.{text.Language}.{field.Name}", field.Name, text.Language, TextOf(text.Texts[field.Name]), field.Kind)));
            }
        }

        return shown;
    }

    /// <summary>
    /// Returns the texts of the record in the other languages as a form was filled in, by the language. A text
    /// left empty is a text the language does not have; the role of a row that was taken out goes with it.
    /// </summary>
    /// <param name="group">The group of the record.</param>
    /// <param name="record">The record as the form was made from it.</param>
    /// <param name="form">What was sent.</param>
    /// <param name="texts">The texts the form was made from.</param>
    public static Dictionary<string, JsonObject> ReadTexts(string group, JsonObject record, IFormCollection form, IReadOnlyList<(string Language, JsonObject Texts)> texts)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(texts);
        var read = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        if (!Translated.TryGetValue(group, out var translated))
        {
            return read;
        }

        foreach (var (language, before) in texts)
        {
            var now = new JsonObject();
            foreach (var name in translated.Fields)
            {
                if (form.TryGetValue($"t.{language}.{name}", out var sent))
                {
                    if (Sent(sent) is { Length: > 0 } text)
                    {
                        now[name] = text;
                    }
                }
                else if (before[name] is { } kept)
                {
                    // The form had no input for the text: it stays as it is, for the readers to judge.
                    now[name] = kept.DeepClone();
                }
            }

            if (translated.Facts)
            {
                if (form.ContainsKey($"f.{Facts}[0].{Fact}"))
                {
                    // The words of a fact in another language are kept under the key of the fact; the words of
                    // a fact that is stated no longer go with it.
                    var words = new JsonObject();
                    for (var index = 0; form.TryGetValue($"f.{Facts}[{index}].{Fact}", out var key); index++)
                    {
                        if (Sent(key) is { Length: > 0 } fact && Sent(form[$"f.{Facts}[{index}].{Words}"]).Length > 0 && Sent(form[$"t.{language}.{Facts}[{index}].{Words}"]) is { Length: > 0 } said)
                        {
                            words[fact] = said;
                        }
                    }

                    if (words.Count > 0)
                    {
                        now[Facts] = words;
                    }
                }
                else if (before[Facts] is { } kept)
                {
                    now[Facts] = kept.DeepClone();
                }
            }

            if (translated.Roles)
            {
                if (record[Taking] is JsonArray rows)
                {
                    var roles = new JsonObject();
                    for (var index = 0; index <= rows.Count; index++)
                    {
                        var who = Sent(form[$"f.{Taking}[{index}].{Who}"]);
                        if (who.Length > 0 && Sent(form[$"f.{Taking}[{index}].{Role}"]).Length > 0 && Sent(form[$"t.{language}.{Taking}[{index}].{Role}"]) is { Length: > 0 } role)
                        {
                            roles[who] = role;
                        }
                    }

                    if (roles.Count > 0)
                    {
                        now["roles"] = roles;
                    }
                }
                else if (before["roles"] is { } kept)
                {
                    now["roles"] = kept.DeepClone();
                }
            }

            if (translated.Notes)
            {
                if (record[Stops] is JsonArray stops)
                {
                    // A reason in another language is kept under what its stop names now; the reason of a stop
                    // that was taken out goes with it.
                    var notes = new JsonObject();
                    for (var index = 0; index <= stops.Count; index++)
                    {
                        var named = StopNames.Select(name => Sent(form[$"f.{Stops}[{index}].{name}"])).FirstOrDefault(text => text.Length > 0);
                        if (named is not null && Sent(form[$"f.{Stops}[{index}].{Note}"]).Length > 0 && Sent(form[$"t.{language}.{Stops}[{index}].{Note}"]) is { Length: > 0 } note)
                        {
                            notes[named] = note;
                        }
                    }

                    if (notes.Count > 0)
                    {
                        now["notes"] = notes;
                    }
                }
                else if (before["notes"] is { } kept)
                {
                    now["notes"] = kept.DeepClone();
                }
            }

            if (translated.Places)
            {
                if (form.ContainsKey($"f.{Placements}[0].{Caption}"))
                {
                    // A caption in another language is kept under what its place names now; the caption of a
                    // place that was taken out goes with it.
                    var places = new JsonObject();
                    for (var index = 0; form.ContainsKey($"f.{Placements}[{index}].{Caption}"); index++)
                    {
                        var named = PlaceNames.Select(name => Sent(form[$"f.{Placements}[{index}].{name}"])).FirstOrDefault(text => text.Length > 0);
                        if (named is not null && Sent(form[$"f.{Placements}[{index}].{Caption}"]).Length > 0 && Sent(form[$"t.{language}.{Placements}[{index}].{Caption}"]) is { Length: > 0 } caption)
                        {
                            places[named] = caption;
                        }
                    }

                    if (places.Count > 0)
                    {
                        now["places"] = places;
                    }
                }
                else if (before["places"] is { } kept)
                {
                    now["places"] = kept.DeepClone();
                }
            }

            read[language] = now;
        }

        return read;
    }

    private static string Sent(StringValues sent) =>
        sent.Count == 0 ? string.Empty : (sent[^1] ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private static string TextOf(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;

    /// <summary>
    /// Returns the texts of the statements with a date of an entity in the other languages as a form was filled
    /// in: by the language, the texts by the key of the statement. A text left empty is one the language does
    /// not have; the texts of a row that was taken out go with it.
    /// </summary>
    /// <param name="record">The entity as the form was made from it.</param>
    /// <param name="form">What was sent.</param>
    /// <param name="dated">The texts the form was made from.</param>
    public static Dictionary<string, JsonObject> ReadDated(JsonObject record, IFormCollection form, IReadOnlyList<(string Language, JsonObject Texts)> dated)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(dated);
        var read = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        if (record[Dated] is not JsonArray rows)
        {
            return read;
        }

        foreach (var (language, before) in dated)
        {
            var now = new JsonObject();
            for (var index = 0; index <= rows.Count; index++)
            {
                var key = Sent(form[$"f.{Dated}[{index}].{Key}"]);
                if (key.Length == 0)
                {
                    continue;
                }

                var texts = new JsonObject();

                // A text the form has no input for – the explanation of a hypothetical placement (ADR 0028) –
                // is kept as the translation has it.
                var others = (before[key] as JsonObject)?.Select(text => text.Key).Where(name => !DatedTexts.Contains(name)) ?? [];
                foreach (var name in DatedTexts.Concat(others).ToList())
                {
                    if (form.TryGetValue($"t.{language}.{Dated}[{index}].{name}", out var sent))
                    {
                        if (Sent(sent) is { Length: > 0 } text)
                        {
                            texts[name] = text;
                        }
                    }
                    else if ((before[key] as JsonObject)?[name] is { } kept)
                    {
                        // The form had no input for the text: it stays as it is.
                        texts[name] = kept.DeepClone();
                    }
                }

                if (texts.Count > 0)
                {
                    now[key] = texts;
                }
            }

            read[language] = now;
        }

        return read;
    }

    // The role a language gives the row of what takes part, by whom the row names.
    private static string RoleOf(JsonObject texts, IReadOnlyList<FormField> row) =>
        row.FirstOrDefault(part => part.Name == Who) is { } who && who.Text.Length > 0 ? TextOf((texts["roles"] as JsonObject)?[who.Text]) : string.Empty;

    // The caption a language gives a place of a media item, by what the place names.
    private static string PlaceOf(JsonObject texts, IReadOnlyList<FormField> row) =>
        row.FirstOrDefault(part => PlaceNames.Contains(part.Name) && part.Text.Length > 0) is { } named ? TextOf((texts["places"] as JsonObject)?[named.Text]) : string.Empty;

    // The reason a language gives a stop, by what the stop names.
    private static string NoteOf(JsonObject texts, IReadOnlyList<FormField> row) =>
        row.FirstOrDefault(part => StopNames.Contains(part.Name) && part.Text.Length > 0) is { } named ? TextOf((texts["notes"] as JsonObject)?[named.Text]) : string.Empty;

    private static FormField Translation(string path, string name, string language, string text, FieldKind kind) => new()
    {
        Path = path, Name = name, Language = language, Value = JsonValue.Create(text),
        Kind = kind == FieldKind.LongText || text.Length > 70 || text.Contains('\n', StringComparison.Ordinal) ? FieldKind.LongText : FieldKind.Text,
    };

    /// <summary>
    /// Returns the record with its fields as a form was filled in, or the reasons the form cannot be understood.
    /// Whether the record may say what it then says is not asked here – the store asks the readers.
    /// </summary>
    public static JsonObject Read(string group, JsonObject record, IFormCollection form, FormContext context, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(problems);
        var fields = Of(group, record, context).ToDictionary(field => field.Name, StringComparer.Ordinal);
        var filled = new JsonObject();
        foreach (var (name, value) in record)
        {
            if (!fields.TryGetValue(name, out var field))
            {
                filled[name] = value?.DeepClone();
            }
            else if (Filled(field, form, problems) is { } read)
            {
                filled[name] = read.Node;
            }
        }

        if (StatesNoTime(group, record) && fields.TryGetValue(Time, out var added) && Filled(added, form, problems) is { Node: not null } time)
        {
            filled[Time] = time.Node;
        }

        if (group == "entities")
        {
            foreach (var list in EntityLists.Keys.Append(Facts).Where(list => !record.ContainsKey(list)))
            {
                if (fields.TryGetValue(list, out var begun) && Filled(begun, form, problems) is { Node: JsonArray { Count: > 0 } rows })
                {
                    filled[list] = rows;
                }
            }
        }

        if (group == "media")
        {
            if (!record.ContainsKey(Themes) && fields.TryGetValue(Themes, out var offered) && Filled(offered, form, problems) is { Node: JsonArray { Count: > 0 } under })
            {
                filled[Themes] = under;
            }

            // An item under no theme has no list of themes.
            if (filled[Themes] is JsonArray { Count: 0 })
            {
                filled.Remove(Themes);
            }

            if (!record.ContainsKey(Placements) && fields.TryGetValue(Placements, out var begun) && Filled(begun, form, problems) is { Node: JsonArray { Count: > 0 } places })
            {
                filled[Placements] = places;
            }

            if (filled[Placements] is JsonArray stated && stated.All(place => place is JsonObject))
            {
                // A place that names no source has no list of sources, and an item that stands in no place has
                // no list of places: the form showed both only to be filled in.
                foreach (var place in stated.Select(place => place!.AsObject()).Where(place => place["sourceIds"] is JsonArray { Count: 0 }))
                {
                    place.Remove("sourceIds");
                }

                if (stated.Count == 0)
                {
                    filled.Remove(Placements);
                }
            }
        }

        return filled;
    }

    // The facts of the profile of an entity as rows (ADR 0017): first what the record states, in its order, then
    // a row for every fact its kind has and the record does not state – the essential ones marked as missing –
    // and one more for a fact that may be stated several times. A fact says one thing: the row has the input
    // for exactly that. Nothing when the form cannot show the facts this way – the type is not known, or a
    // fact is written in a way the rows have no inputs for; the field is then the text of the file.
    private static FormField? FactsOf(JsonObject record, FormContext context)
    {
        if (ContentVocabulary.EntityType((string?)record["type"]) is not { } type || record[Facts] is not (null or JsonArray))
        {
            return null;
        }

        var kinds = ProfileFacts.Of(type);
        var stated = record[Facts] as JsonArray ?? [];
        var rows = new List<IReadOnlyList<FormField>>();
        foreach (var fact in stated)
        {
            if (fact is not JsonObject written || (string?)written[Fact] is not { } key || kinds.Find(key) is not { } kind
                || written.Any(part => part.Key is not (Fact or Words or "entityId" or Time or "sourceIds" or "spoilerLevel"))
                || (kind.Value == FactValue.Time && TimeForm.Parse(written[Time]) is null))
            {
                return null;
            }

            rows.Add(FactRow(rows.Count, kind, kinds.Essential.Contains(kind), written, context));
        }

        foreach (var kind in kinds.All.Where(kind => kind.Many || !stated.Any(fact => (string?)fact![Fact] == kind.Key)))
        {
            rows.Add(FactRow(rows.Count, kind, kinds.Essential.Contains(kind), null, context));
        }

        return new FormField { Path = $"f.{Facts}", Name = Facts, Kind = FieldKind.Facts, Value = record[Facts], Rows = rows };
    }

    // One row of the facts: the fact, what it says, what it rests on, and from which stage on it may be told.
    private static List<FormField> FactRow(int index, FactKind kind, bool essential, JsonObject? written, FormContext context)
    {
        var path = $"f.{Facts}[{index}]";
        var says = kind.Value switch
        {
            FactValue.Words => new FormField { Path = $"{path}.{Words}", Name = Words, Kind = FieldKind.Text, Value = JsonValue.Create((string?)written?[Words] ?? string.Empty) },
            FactValue.Time => new FormField { Path = $"{path}.{Time}", Name = Time, Kind = FieldKind.Time, Value = written?[Time], Parts = TimeForm.Parts($"{path}.{Time}", TimeForm.Parse(written?[Time])) },
            _ => new FormField
            {
                Path = $"{path}.entityId", Name = "entityId", Kind = FieldKind.Choice, Value = JsonValue.Create((string?)written?["entityId"] ?? string.Empty),
                Options = With(context.Records.TryGetValue("entities", out var entities) ? entities : [], (string?)written?["entityId"] ?? string.Empty, unchosen: true),
            },
        };
        return
        [
            new FormField
            {
                Path = $"{path}.{Fact}", Name = Fact, Kind = FieldKind.Fact, Value = JsonValue.Create(kind.Key),
                Hint = $"{kind.Meaning}. {(essential ? "Essential" : "May be added")}{(kind.Many ? "; may be stated several times" : null)}.",
                Missing = essential && written is null,
            },
            says,
            new FormField
            {
                Path = $"{path}.sourceIds", Name = "sourceIds", Kind = FieldKind.References, Value = written?["sourceIds"],
                Options = context.Records.TryGetValue("sources", out var sources) ? sources : [],
            },
            new FormField
            {
                Path = $"{path}.spoilerLevel", Name = "spoilerLevel", Kind = FieldKind.Choice,
                Value = JsonValue.Create(written?["spoilerLevel"]?.ToJsonString() ?? string.Empty),
                Options = [new FieldOption(string.Empty, "– with the entity"), .. context.Stages],
            },
        ];
    }

    // The facts as the rows were filled in: a row that says nothing is a fact the record does not state. A fact
    // keeps the parts it had – its sources and its stage are written only where they are stated.
    private static (JsonNode? Node, bool Present)? FilledFacts(FormField field, IFormCollection form, List<string> problems)
    {
        var facts = new JsonArray();
        foreach (var row in field.Rows)
        {
            if (Filled(row[1], form, problems) is not { Node: { } says })
            {
                continue;
            }

            var fact = new JsonObject { [Fact] = row[0].Text, [row[1].Name] = says };
            if (Filled(row[2], form, problems) is { Node: JsonArray sources } && (sources.Count > 0 || row[2].Value is not null))
            {
                fact["sourceIds"] = sources;
            }

            if (Sent(form[row[3].Path]) is { Length: > 0 } level)
            {
                if (int.TryParse(level, NumberStyles.None, CultureInfo.InvariantCulture, out var stage))
                {
                    fact["spoilerLevel"] = stage;
                }
                else
                {
                    problems.Add($"{Facts}, {row[0].Text}: the reading stage is a number.");
                }
            }

            facts.Add(fact);
        }

        return facts.Count == 0 ? null : (facts, true);
    }

    // A relation of the world outside a story that does not say when it held.
    private static bool StatesNoTime(string group, JsonObject record) =>
        group == "relations" && !record.ContainsKey(Time) && (string?)record["scope"]?["kind"] == "outside_the_story";

    /// <summary>
    /// Returns a record as a form shows it again after it was refused: what was filled in, with the fields that
    /// were left empty still there to fill in.
    /// </summary>
    public static JsonObject Shown(JsonObject record, JsonObject filled)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(filled);
        var shown = new JsonObject();
        foreach (var (name, value) in record)
        {
            shown[name] = filled[name] switch
            {
                null when value is JsonValue => JsonValue.Create(string.Empty),
                null => value?.DeepClone(),
                JsonObject part when value is JsonObject before => Shown(before, part),
                var read => read.DeepClone(),
            };
        }

        // What the form added to the record – a list it had none of, a time it did not state – is shown again too.
        foreach (var (name, value) in filled.Where(field => !record.ContainsKey(field.Key)))
        {
            shown[name] = value?.DeepClone();
        }

        return shown;
    }

    private static FormField Field(string place, string path, string name, JsonNode? value, FormContext context, bool nested)
    {
        var here = $"{place}.{name}";
        var input = $"{path}.{name}";
        FormField Of(FieldKind kind, IReadOnlyList<FieldOption>? options = null) =>
            new() { Path = input, Name = name, Kind = kind, Value = value, Options = options ?? [] };

        switch (value)
        {
            case JsonValue scalar when scalar.TryGetValue<string>(out var text):
                if (Vocabulary.TryGetValue(here, out var choices))
                {
                    return Of(FieldKind.Choice, With(choices, text));
                }

                if (Pointing.TryGetValue(name, out var pointed) && Targets(context, pointed, out var records))
                {
                    return Of(FieldKind.Choice, With(records, text, unchosen: true));
                }

                return Of(text.Length > 70 || text.Contains('\n', StringComparison.Ordinal) ? FieldKind.LongText : FieldKind.Text);

            case JsonValue scalar when scalar.TryGetValue<bool>(out _):
                return Of(FieldKind.Flag);

            case JsonValue scalar when scalar.GetValueKind() == JsonValueKind.Number:
                return name == "spoilerLevel" ? Of(FieldKind.Choice, With(context.Stages, scalar.ToJsonString())) : Of(FieldKind.Number);

            case JsonArray items when Pointing.TryGetValue(name, out var group) && Targets(context, group, out var targets) && items.All(IsText):
                return Of(FieldKind.References, targets);

            case JsonArray { Count: > 0 } items when items.All(IsText):
                return Of(FieldKind.Lines);

            case JsonArray { Count: 0 } when !nested && (context.Shapes.TryGetValue(here, out var begins) || (place == "entities[]" && EntityLists.TryGetValue(name, out begins)) || (place == "media[]" && name == Placements && (begins = PlacementShape()) is not null)):
                // A list without a row: the one row that begins it. Its texts are empty – a row nothing is
                // typed into is no row – and its choices stand at what is usual for such a row.
                return new FormField
                {
                    Path = input, Name = name, Kind = FieldKind.Rows, Value = value,
                    Rows =
                    [
                        [
                            .. Row($"{here}[]", $"{input}[0]", Empty(begins), context).Select(part =>
                                part.Kind == FieldKind.Choice && begins[part.Name] is JsonValue usual && usual.TryGetValue<string>(out var chosen) && chosen.Length > 0
                                    ? new FormField { Path = part.Path, Name = part.Name, Kind = part.Kind, Value = JsonValue.Create(chosen), Options = part.Options }
                                    : part),
                        ],
                    ],
                };

            case JsonArray { Count: > 0 } items when !nested && items.All(item => item is JsonObject { Count: > 0 }):
                // The rows of the list, and one more that is empty: filled in, it is a part that is added. A part
                // of a row that has parts of its own – the time of a statement with a date – is written as it
                // stands in the file.
                var shape = items[0]!.AsObject();
                var rows = items.Select((item, index) => Row($"{here}[]", $"{input}[{index}]", item!.AsObject(), context)).ToList();
                rows.Add(Row($"{here}[]", $"{input}[{items.Count}]", Empty(shape), context));
                return new FormField { Path = input, Name = name, Kind = FieldKind.Rows, Value = value, Rows = rows };

            case JsonObject stated when name == Time && TimeForm.Parse(stated) is { } time:
                return new FormField { Path = input, Name = name, Kind = FieldKind.Time, Value = value, Parts = TimeForm.Parts(input, time) };

            case null when name == Time && nested:
                // The time of a row that states none yet.
                return new FormField { Path = input, Name = name, Kind = FieldKind.Time, Parts = TimeForm.Parts(input, null) };

            case JsonObject parts when !nested && parts.Count > 0:
                return new FormField
                {
                    Path = input, Name = name, Kind = FieldKind.Group, Value = value,
                    Parts = [.. parts.Select(part => Field(here, input, part.Key, part.Value, context, nested: true))],
                };

            default:
                return Of(FieldKind.Json);
        }
    }

    private static List<FormField> Row(string place, string path, JsonObject row, FormContext context) =>
        [.. row.Select(part => Field(place, path, part.Key, part.Value, context, nested: true))];

    // A row like the first one of its list, with nothing in it: no text, nothing ticked, no time and nothing
    // pointed to. A number – a reading stage – stays; a part with parts of its own that is no time is left out,
    // for a row that is added says such a thing itself or not at all (the record of a review is not copied).
    private static JsonObject Empty(JsonObject shape) =>
        new(shape
            .Where(part => part.Value is not JsonObject || part.Key == Time)
            .Select(part => KeyValuePair.Create(part.Key, part.Value?.GetValueKind() switch
            {
                null or JsonValueKind.Object => null,
                JsonValueKind.String => (JsonNode?)JsonValue.Create(string.Empty),
                JsonValueKind.True or JsonValueKind.False => JsonValue.Create(false),
                JsonValueKind.Array => new JsonArray(),
                _ => part.Value.DeepClone(),
            })));

    private static bool IsText(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out _);

    // The values to choose from, with the value the record has when it is none of them – it stays choosable, and
    // the readers say what is wrong with it.
    // A field that names another record and is empty stays empty until the editor chooses (unchosen): without an
    // empty choice the list would show its first record, and a form sent without a look at it would name that
    // record. A field with a few fixed values begins with the first of them, which is the usual one.
    private static IReadOnlyList<FieldOption> With(IReadOnlyList<FieldOption> options, string current, bool unchosen = false) =>
        (current.Length == 0 && !unchosen) || options.Any(option => option.Value == current) ? options : [new FieldOption(current, current.Length == 0 ? "–" : current), .. options];

    // What a field was filled in with: the node to write, or nothing for a text that was left empty – a field
    // without a text is a field the record does not have.
    private static (JsonNode? Node, bool Present)? Filled(FormField field, IFormCollection form, List<string> problems)
    {
        var posted = form.TryGetValue(field.Path, out var values) ? values : StringValues.Empty;
        var text = posted.Count == 0 ? string.Empty : (posted[^1] ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);
        switch (field.Kind)
        {
            case FieldKind.Text or FieldKind.LongText:
                return text.Trim().Length == 0 ? null : (JsonValue.Create(text.Trim()), true);

            case FieldKind.Choice when field.Value is JsonValue number && number.GetValueKind() == JsonValueKind.Number:
                return Number(field, text, problems);

            case FieldKind.Choice:
                return text.Length == 0 ? null : (JsonValue.Create(text), true);

            case FieldKind.Number:
                return Number(field, text, problems);

            case FieldKind.Flag:
                return (JsonValue.Create(posted.Contains("true")), true);

            case FieldKind.References:
                // The records the field had stay in the order the record names them in – that order is the
                // writer's; what was ticked in addition follows in the order of the collection.
                var chosen = posted.ToHashSet(StringComparer.Ordinal);
                var known = field.Options.Select(option => option.Value).Where(chosen.Contains).ToList();
                var kept = field.Chosen.Count == 0 ? [] : field.Value!.AsArray().Select(item => item!.GetValue<string>()).Where(known.Contains).ToList();
                return (new JsonArray([.. kept.Concat(known.Except(kept, StringComparer.Ordinal)).Select(value => (JsonNode?)JsonValue.Create(value))]), true);

            case FieldKind.Lines:
                return (new JsonArray([.. text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(line => (JsonNode?)JsonValue.Create(line))]), true);

            case FieldKind.Time:
                return TimeForm.Read(field, form, problems);

            case FieldKind.Group:
                var group = new JsonObject();
                foreach (var part in field.Parts)
                {
                    if (Filled(part, form, problems) is { } read)
                    {
                        group[part.Name] = read.Node;
                    }
                }

                return (group, true);

            case FieldKind.Facts:
                return FilledFacts(field, form, problems);

            case FieldKind.Rows:
                var rows = new JsonArray();
                foreach (var row in field.Rows)
                {
                    // A row in which no text was filled in is no part: the empty row at the end, or one to take out.
                    if (row.Where(part => part.Kind is FieldKind.Text or FieldKind.LongText).All(part => string.IsNullOrWhiteSpace(form[part.Path])))
                    {
                        continue;
                    }

                    var filled = new JsonObject();
                    foreach (var part in row)
                    {
                        if (Filled(part, form, problems) is { } read)
                        {
                            filled[part.Name] = read.Node;
                        }
                    }

                    rows.Add(filled);
                }

                return (rows, true);

            default:
                try
                {
                    return (JsonNode.Parse(text.Length == 0 ? "null" : text), true);
                }
                catch (JsonException refused)
                {
                    problems.Add($"{field.Name}: {refused.Message}");
                    return (field.Value?.DeepClone(), true);
                }
        }
    }

    private static (JsonNode? Node, bool Present)? Number(FormField field, string text, List<string> problems)
    {
        // An editor who works in Persian writes Persian digits; a record holds a number.
        var written = Vafadar.Core.Text.Digits.ToAscii(text.Trim());
        if (long.TryParse(written, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole))
        {
            return (JsonValue.Create(whole), true);
        }

        if (decimal.TryParse(written, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return (JsonValue.Create(number), true);
        }

        problems.Add($"{field.Name}: \"{text}\" is not a number.");
        return (field.Value?.DeepClone(), true);
    }
}
