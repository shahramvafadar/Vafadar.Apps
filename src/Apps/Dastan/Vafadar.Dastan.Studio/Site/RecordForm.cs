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

    /// <summary>Returns the fields of a record a form shows, in the order of the record.</summary>
    public static IReadOnlyList<FormField> Of(string group, JsonObject record, FormContext context)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(context);
        return [.. record.Where(field => !Kept.Contains(field.Key)).Select(field => Field($"{group}[]", "f", field.Key, field.Value, context, nested: false))];
    }

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

        return filled;
    }

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

            case JsonArray { Count: > 0 } items when !nested && items.All(item => item is JsonObject row && row.All(part => part.Value is JsonValue)):
                // The rows of the list, and one more that is empty: filled in, it is a part that is added.
                var shape = items[0]!.AsObject();
                var rows = items.Select((item, index) => Row($"{here}[]", $"{input}[{index}]", item!.AsObject(), context)).ToList();
                rows.Add(Row($"{here}[]", $"{input}[{items.Count}]", Empty(shape), context));
                return new FormField { Path = input, Name = name, Kind = FieldKind.Rows, Value = value, Rows = rows };

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
