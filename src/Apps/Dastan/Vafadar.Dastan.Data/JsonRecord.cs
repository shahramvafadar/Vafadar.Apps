using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace Vafadar.Dastan.Data;

/// <summary>
/// One JSON object of an editorial file, read field by field. It remembers which fields were asked for, so a field
/// the reader does not know is reported instead of being ignored: a misspelt or newly added field must not change
/// what a visitor sees without anybody noticing.
/// </summary>
internal sealed class JsonRecord
{
    private readonly JsonElement _element;
    private readonly List<string> _problems;
    private readonly HashSet<string> _read = new(StringComparer.Ordinal);

    public JsonRecord(JsonElement element, string name, List<string> problems)
    {
        _element = element;
        _problems = problems;
        Name = name;
    }

    /// <summary>Gets or sets the name problems are reported under: the place in the file until the identifier is known.</summary>
    public string Name { get; set; }

    /// <summary>Returns whether the field is there, even when its value is null.</summary>
    public bool IsStated(string field) => _element.TryGetProperty(field, out _);

    /// <summary>Reports a problem of this record.</summary>
    public void Problem(string message) => _problems.Add($"{Name}: {message}");

    /// <summary>Reads a text that is not blank.</summary>
    public string? Text(string field, bool required = true)
    {
        if (!Value(field, required, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            Problem($"{field} is not a text.");
            return null;
        }

        return value.GetString();
    }

    /// <summary>Reads an identifier: a text without blanks.</summary>
    public string? Identifier(string field, bool required = true)
    {
        var text = Text(field, required);
        if (text is not null && text.Any(char.IsWhiteSpace))
        {
            Problem($"{field} is an identifier and contains a blank.");
            return null;
        }

        return text;
    }

    /// <summary>Reads a whole number.</summary>
    public int? Integer(string field, bool required = true)
    {
        if (!Value(field, required, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            Problem($"{field} is not a whole number.");
            return null;
        }

        return number;
    }

    /// <summary>Reads a number.</summary>
    public decimal? Number(string field, bool required = true)
    {
        if (!Value(field, required, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
        {
            Problem($"{field} is not a number.");
            return null;
        }

        return number;
    }

    /// <summary>Reads true or false.</summary>
    public bool? Flag(string field, bool required = true)
    {
        if (!Value(field, required, out var value))
        {
            return null;
        }

        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            Problem($"{field} is neither true nor false.");
            return null;
        }

        return value.GetBoolean();
    }

    /// <summary>Reads a day written as year-month-day.</summary>
    public DateOnly? Day(string field, bool required = true)
    {
        var text = Text(field, required);
        if (text is null)
        {
            return null;
        }

        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            Problem($"{field} is not a day written as year-month-day.");
            return null;
        }

        return day;
    }

    /// <summary>Reads one of the values the reader knows.</summary>
    public bool Choice<T>(string field, IReadOnlyDictionary<string, T> choices, [MaybeNullWhen(false)] out T choice, bool required = true)
    {
        choice = default;
        var text = Text(field, required);
        if (text is null)
        {
            return false;
        }

        if (!choices.TryGetValue(text, out choice))
        {
            Problem($"{field} has the unknown value \"{text}\".");
            return false;
        }

        return true;
    }

    /// <summary>Reads a list of texts.</summary>
    public ImmutableArray<string> Texts(string field, bool required = true)
    {
        if (!Value(field, required, out var value))
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            Problem($"{field} is not a list.");
            return [];
        }

        var texts = ImmutableArray.CreateBuilder<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                Problem($"{field} contains something that is not a text.");
                continue;
            }

            texts.Add(item.GetString()!);
        }

        return texts.ToImmutable();
    }

    /// <summary>Reads an object.</summary>
    public JsonRecord? Child(string field, bool required = true)
    {
        if (!Value(field, required, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            Problem($"{field} is not an object.");
            return null;
        }

        return new JsonRecord(value, $"{Name}.{field}", _problems);
    }

    /// <summary>Returns the value of a field as it stands in the file, for a reader of its own; null when it is missing.</summary>
    public JsonElement? Raw(string field, bool required = true) => Value(field, required, out var value) ? value : null;

    /// <summary>Reads a list of objects.</summary>
    public IReadOnlyList<JsonRecord> Children(string field, bool required = true)
    {
        if (!Value(field, required, out var value))
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            Problem($"{field} is not a list.");
            return [];
        }

        var children = new List<JsonRecord>();
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var name = $"{Name}.{field}[{index++}]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                _problems.Add($"{name}: it is not an object.");
                continue;
            }

            children.Add(new JsonRecord(item, name, _problems));
        }

        return children;
    }

    /// <summary>Reads an object whose fields are names chosen by the editors, each with an object as its value.</summary>
    public IReadOnlyList<(string Key, JsonRecord Record)> Named(string field, bool required = true)
    {
        var entries = new List<(string, JsonRecord)>();
        if (!Value(field, required, out var value))
        {
            return entries;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            Problem($"{field} is not an object.");
            return entries;
        }

        foreach (var property in value.EnumerateObject())
        {
            var name = $"{Name}.{field}.{property.Name}";
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                _problems.Add($"{name}: it is not an object.");
                continue;
            }

            entries.Add((property.Name, new JsonRecord(property.Value, name, _problems)));
        }

        return entries;
    }

    /// <summary>Reads an object whose fields are names chosen by the editors, each with a text as its value.</summary>
    public IReadOnlyList<(string Key, string Text)> NamedTexts(string field, bool required = true)
    {
        var entries = new List<(string, string)>();
        if (!Value(field, required, out var value))
        {
            return entries;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            Problem($"{field} is not an object.");
            return entries;
        }

        foreach (var property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.Value.GetString()))
            {
                _problems.Add($"{Name}.{field}.{property.Name}: it is not a text.");
                continue;
            }

            entries.Add((property.Name, property.Value.GetString()!));
        }

        return entries;
    }

    /// <summary>Requires that the field is absent or null, because the reader would otherwise have to guess its meaning.</summary>
    public void Nothing(string field, string reason)
    {
        _read.Add(field);
        if (_element.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Null)
        {
            Problem(reason);
        }
    }

    /// <summary>Requires that the field is absent.</summary>
    public void Absent(string field, string reason)
    {
        _read.Add(field);
        if (_element.TryGetProperty(field, out _))
        {
            Problem(reason);
        }
    }

    /// <summary>Marks editors' notes that the reader knows and that are not part of the read model.</summary>
    public void Notes(params string[] fields) => _read.UnionWith(fields);

    /// <summary>Reports every field nobody asked for.</summary>
    public void Finish()
    {
        foreach (var property in _element.EnumerateObject())
        {
            if (!_read.Contains(property.Name))
            {
                Problem($"the field {property.Name} is not known to this reader.");
            }
        }
    }

    private bool Value(string field, bool required, out JsonElement value)
    {
        _read.Add(field);
        if (_element.TryGetProperty(field, out value) && value.ValueKind != JsonValueKind.Null)
        {
            return true;
        }

        if (required)
        {
            Problem($"{field} is missing.");
        }

        return false;
    }
}
