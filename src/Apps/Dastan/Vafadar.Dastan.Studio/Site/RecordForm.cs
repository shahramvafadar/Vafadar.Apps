using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;
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
    /// <summary>Reads what a form needs from the store.</summary>
    public static FormContext Of(EditorialStore store, string collection)
    {
        ArgumentNullException.ThrowIfNull(store);
        return new FormContext(
            [.. store.Stages(collection).Select(stage => new FieldOption(stage.Level.ToString(CultureInfo.InvariantCulture), $"{stage.Level} – {stage.Label}"))],
            new[] { "sources", "entities", "relations", "events" }.ToDictionary(
                group => group,
                group => (IReadOnlyList<FieldOption>)[.. store.Records(collection, group).Select(record => new FieldOption(record.Id, $"{record.Label} ({record.Id})"))]));
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

    // The fields that name records of the collection, and the group of those records.
    private static readonly FrozenDictionary<string, string> Pointing = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["sourceIds"] = "sources",
        ["depictedEntityIds"] = "entities",
        ["depictedEventIds"] = "events",
        ["relatedRelationIds"] = "relations",
        ["eventIds"] = "events",
        ["from"] = "entities",
        ["to"] = "entities",
        ["storyId"] = "entities",
        ["entityId"] = "entities",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    // The vocabulary of the readers, by the place of a field: "relations[].scope.kind".
    private static readonly FrozenDictionary<string, IReadOnlyList<FieldOption>> Vocabulary = ContentVocabulary.Collection
        .SelectMany(choices => choices.Field.Split(", ").Select(field => (Field: field, Values: (IReadOnlyList<FieldOption>)[.. choices.Values.Select(value => new FieldOption(value.Name, value.Name))])))
        .ToFrozenDictionary(entry => entry.Field, entry => entry.Values, StringComparer.Ordinal);

    // The texts a translation may translate, by the group of the record – the list of the readers.
    private static readonly FrozenDictionary<string, ContentTexts> Translated = ContentVocabulary.Translated.ToFrozenDictionary(texts => texts.Group, StringComparer.Ordinal);

    // What takes part in a record: the list, who a row names, and the role the row gives – the places of the
    // file that a translation names as "roles", by who takes part.
    private const string Taking = "entityAssociations";
    private const string Who = "entityId";
    private const string Role = "role";

    // The time of a relation: a relation of the world that states none has the inputs to state one.
    private const string Time = "time";

    // The statements with a date of an entity: the list, the key a row names itself by, and the texts of a row
    // a translation translates – the place "dated" of a translation document, by the key.
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
        var fields = record.Where(field => !Kept.Contains(field.Key)).Select(field => Field($"{group}[]", "f", field.Key, field.Value, context, nested: false)).ToList();
        if (StatesNoTime(group, record))
        {
            fields.Add(new FormField { Path = $"f.{Time}", Name = Time, Kind = FieldKind.Time, Parts = TimeForm.Parts($"f.{Time}", null) });
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
                foreach (var name in DatedTexts)
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

        return filled;
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

                if (Pointing.TryGetValue(name, out var pointed) && context.Records.TryGetValue(pointed, out var records))
                {
                    return Of(FieldKind.Choice, With(records, text));
                }

                return Of(text.Length > 70 || text.Contains('\n', StringComparison.Ordinal) ? FieldKind.LongText : FieldKind.Text);

            case JsonValue scalar when scalar.TryGetValue<bool>(out _):
                return Of(FieldKind.Flag);

            case JsonValue scalar when scalar.GetValueKind() == JsonValueKind.Number:
                return name == "spoilerLevel" ? Of(FieldKind.Choice, With(context.Stages, scalar.ToJsonString())) : Of(FieldKind.Number);

            case JsonArray items when Pointing.TryGetValue(name, out var group) && context.Records.TryGetValue(group, out var targets) && items.All(IsText):
                return Of(FieldKind.References, targets);

            case JsonArray { Count: > 0 } items when items.All(IsText):
                return Of(FieldKind.Lines);

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

    // A row like the first one of its list, with nothing in it.
    private static JsonObject Empty(JsonObject shape) =>
        new(shape.Select(part => KeyValuePair.Create(part.Key, part.Value!.GetValueKind() switch
        {
            JsonValueKind.String => (JsonNode?)JsonValue.Create(string.Empty),
            JsonValueKind.True or JsonValueKind.False => JsonValue.Create(false),
            _ => part.Value.DeepClone(),
        })));

    private static bool IsText(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out _);

    // The values to choose from, with the value the record has when it is none of them – it stays choosable, and
    // the readers say what is wrong with it.
    private static IReadOnlyList<FieldOption> With(IReadOnlyList<FieldOption> options, string current) =>
        current.Length == 0 || options.Any(option => option.Value == current) ? options : [new FieldOption(current, current), .. options];

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
        if (long.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole))
        {
            return (JsonValue.Create(whole), true);
        }

        if (decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return (JsonValue.Create(number), true);
        }

        problems.Add($"{field.Name}: \"{text}\" is not a number.");
        return (field.Value?.DeepClone(), true);
    }
}
