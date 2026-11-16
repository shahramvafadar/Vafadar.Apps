using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vafadar.Temporal;

/// <summary>
/// Reads and writes temporal expressions as JSON without losing or adding anything: a year stays a year, "55.0"
/// keeps its zero, and what is not stated is absent from the text.
/// </summary>
/// <remarks>
/// <para>
/// The format is this library's own and is documented in its README. Every object names its kind (<c>kind</c> for
/// expressions, <c>frame</c> for positions, <c>state</c> for the ends of a period). Reading is strict: an unknown
/// kind, an unknown or missing property and a value outside its range are a <see cref="JsonException"/>, because a
/// time that is silently read differently is worse than one that is refused.
/// </para>
/// <para>Add <see cref="Converters"/> to the options of an app, or use <see cref="Serialize"/> and <see cref="Deserialize"/>.</para>
/// </remarks>
public static class TemporalJson
{
    /// <summary>Gets the converters for <see cref="TemporalExpression"/> and <see cref="TimePosition"/>.</summary>
    public static IReadOnlyList<JsonConverter> Converters { get; } = [new ExpressionConverter(), new PositionConverter()];

    // Declared after Converters: static fields are set in the order they are written, and the options need them.
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Returns <paramref name="expression"/> as JSON text.</summary>
    public static string Serialize(TemporalExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return JsonSerializer.Serialize(expression, Options);
    }

    /// <summary>Reads an expression from JSON text.</summary>
    /// <exception cref="JsonException">The text is not a temporal expression of this format.</exception>
    public static TemporalExpression Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<TemporalExpression>(json, Options) ?? throw new JsonException("A temporal expression is an object.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        foreach (var converter in Converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    private sealed class ExpressionConverter : JsonConverter<TemporalExpression>
    {
        public override TemporalExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return Guarded(() => ReadExpression(document.RootElement));
        }

        public override void Write(Utf8JsonWriter writer, TemporalExpression value, JsonSerializerOptions options) =>
            WriteExpression(writer, value);
    }

    private sealed class PositionConverter : JsonConverter<TimePosition>
    {
        public override TimePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return Guarded(() => ReadPosition(document.RootElement));
        }

        public override void Write(Utf8JsonWriter writer, TimePosition value, JsonSerializerOptions options) =>
            WritePosition(writer, value);
    }

    // The types refuse impossible values with argument exceptions; for a reader they are faults of the text.
    private static T Guarded<T>(Func<T> read)
    {
        try
        {
            return read();
        }
        catch (ArgumentException exception)
        {
            throw new JsonException(exception.Message, exception);
        }
    }

    private static void WriteExpression(Utf8JsonWriter writer, TemporalExpression expression)
    {
        writer.WriteStartObject();
        switch (expression)
        {
            case Occurrence occurrence:
                writer.WriteString("kind", "occurrence");
                WritePosition(writer, "at", occurrence.At);
                break;

            case OccurrenceWindow window:
                writer.WriteString("kind", "occurrenceWindow");
                WritePosition(writer, "earliest", window.Earliest);
                WritePosition(writer, "latest", window.Latest);
                break;

            case AlternativeOccurrences alternatives:
                writer.WriteString("kind", "alternatives");
                writer.WriteStartArray("options");
                foreach (var option in alternatives.Options)
                {
                    WriteExpression(writer, option);
                }

                writer.WriteEndArray();
                break;

            case Period period:
                writer.WriteString("kind", "period");
                WriteBound(writer, "start", Name(period.Start.State), period.Start.Earliest, period.Start.Latest);
                WriteBound(writer, "end", Name(period.End.State), period.End.Earliest, period.End.Latest);
                writer.WriteString("continuity", Name(period.Continuity));
                if (period.LastConfirmed is not null)
                {
                    WritePosition(writer, "lastConfirmed", period.LastConfirmed);
                }

                break;

            case ElapsedTime elapsed:
                writer.WriteString("kind", "elapsed");
                WriteNumber(writer, "value", elapsed.Value);
                writer.WriteString("unit", Name(elapsed.Unit));
                WriteApproximate(writer, elapsed.IsApproximate);
                break;

            case EventTime eventTime:
                writer.WriteString("kind", "eventTime");
                writer.WriteString("eventId", eventTime.EventId);
                break;

            case UnknownTime:
                writer.WriteString("kind", "unknown");
                break;

            case InapplicableTime:
                writer.WriteString("kind", "inapplicable");
                break;

            default:
                throw new JsonException($"The expression {expression.GetType().Name} has no JSON form.");
        }

        if (expression.SourceText is not null)
        {
            writer.WriteString("sourceText", expression.SourceText);
        }

        writer.WriteEndObject();
    }

    private static void WriteBound(Utf8JsonWriter writer, string name, string state, TimePosition? earliest, TimePosition? latest)
    {
        writer.WriteStartObject(name);
        writer.WriteString("state", state);
        if (state == "known")
        {
            WritePosition(writer, "at", earliest!);
        }
        else if (state == "between")
        {
            WritePosition(writer, "earliest", earliest!);
            WritePosition(writer, "latest", latest!);
        }

        writer.WriteEndObject();
    }

    private static void WritePosition(Utf8JsonWriter writer, string name, TimePosition position)
    {
        writer.WritePropertyName(name);
        WritePosition(writer, position);
    }

    private static void WritePosition(Utf8JsonWriter writer, TimePosition position)
    {
        writer.WriteStartObject();
        switch (position)
        {
            case CalendarPosition calendar:
                writer.WriteString("frame", "calendar");
                if (calendar.Calendar != CalendarSystem.Unspecified)
                {
                    writer.WriteString("calendar", Name(calendar.Calendar));
                }

                writer.WriteString("era", Name(calendar.Era));
                writer.WriteNumber("year", calendar.Year);
                if (calendar.Month is { } month)
                {
                    writer.WriteNumber("month", month);
                }

                if (calendar.Day is { } day)
                {
                    writer.WriteNumber("day", day);
                }

                WriteApproximate(writer, calendar.IsApproximate);
                break;

            case AgePosition age:
                writer.WriteString("frame", "age");
                WriteNumber(writer, "value", age.Value);
                writer.WriteString("unit", Name(age.Unit));
                if (age.Reference.CommonEraYear is { } reference)
                {
                    writer.WriteNumber("reference", reference);
                }

                if (age.Basis != AgeBasis.CalendarYears)
                {
                    writer.WriteString("basis", Name(age.Basis));
                }

                WriteApproximate(writer, age.IsApproximate);
                break;

            case NarrativePosition narrative:
                writer.WriteString("frame", "narrative");
                writer.WriteString("sequence", narrative.Sequence);
                writer.WriteString("order", Name(narrative.Order));
                writer.WriteNumber("ordinal", narrative.Ordinal);
                break;

            default:
                throw new JsonException($"The position {position.GetType().Name} has no JSON form.");
        }

        writer.WriteEndObject();
    }

    // A decimal is written with its own digits: 55 and 55.0 are the same number stated with different care.
    private static void WriteNumber(Utf8JsonWriter writer, string name, decimal value)
    {
        writer.WritePropertyName(name);
        writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture), skipInputValidation: false);
    }

    private static void WriteApproximate(Utf8JsonWriter writer, bool isApproximate)
    {
        if (isApproximate)
        {
            writer.WriteBoolean("approximate", true);
        }
    }

    private static TemporalExpression ReadExpression(JsonElement element)
    {
        var reader = new ObjectReader(element);
        var kind = reader.RequiredString("kind");
        TemporalExpression expression = kind switch
        {
            "occurrence" => new Occurrence(ReadPosition(reader.Required("at"))),
            "occurrenceWindow" => new OccurrenceWindow(ReadPosition(reader.Required("earliest")), ReadPosition(reader.Required("latest"))),
            "alternatives" => new AlternativeOccurrences(ReadOptions(reader.Required("options"))),
            "period" => ReadPeriod(reader),
            "elapsed" => new ElapsedTime(reader.RequiredDecimal("value"), Parse<ElapsedUnit>(reader.RequiredString("unit")), reader.OptionalFlag("approximate")),
            "eventTime" => new EventTime(reader.RequiredString("eventId")),
            "unknown" => new UnknownTime(),
            "inapplicable" => new InapplicableTime(),
            _ => throw new JsonException($"Unknown kind of temporal expression '{kind}'."),
        };

        var sourceText = reader.OptionalString("sourceText");
        reader.RequireNoOtherProperties();
        return sourceText is null ? expression : expression with { SourceText = sourceText };
    }

    private static List<TemporalExpression> ReadOptions(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("'options' is a list.");
        }

        return [.. element.EnumerateArray().Select(ReadExpression)];
    }

    private static Period ReadPeriod(ObjectReader reader)
    {
        var startReader = new ObjectReader(reader.Required("start"));
        var startState = startReader.RequiredString("state");
        var start = startState switch
        {
            "known" => PeriodStart.At(ReadPosition(startReader.Required("at"))),
            "between" => PeriodStart.Between(ReadPosition(startReader.Required("earliest")), ReadPosition(startReader.Required("latest"))),
            "unknown" => PeriodStart.Unknown,
            _ => throw new JsonException($"Unknown state of a period start '{startState}'."),
        };
        startReader.RequireNoOtherProperties();

        var endReader = new ObjectReader(reader.Required("end"));
        var endState = endReader.RequiredString("state");
        var end = endState switch
        {
            "known" => PeriodEnd.At(ReadPosition(endReader.Required("at"))),
            "between" => PeriodEnd.Between(ReadPosition(endReader.Required("earliest")), ReadPosition(endReader.Required("latest"))),
            "ongoing" => PeriodEnd.Ongoing,
            "endedAtUnknownTime" => PeriodEnd.EndedAtUnknownTime,
            "unknown" => PeriodEnd.Unknown,
            _ => throw new JsonException($"Unknown state of a period end '{endState}'."),
        };
        endReader.RequireNoOtherProperties();

        var continuity = Parse<PeriodContinuity>(reader.RequiredString("continuity"));
        var lastConfirmed = reader.Optional("lastConfirmed") is { } confirmed ? ReadPosition(confirmed) : null;
        return new Period(start, end, continuity, lastConfirmed);
    }

    private static TimePosition ReadPosition(JsonElement element)
    {
        var reader = new ObjectReader(element);
        var frame = reader.RequiredString("frame");
        TimePosition position = frame switch
        {
            "calendar" => ReadCalendarPosition(reader),
            "age" => ReadAgePosition(reader),
            "narrative" => NarrativePosition.Of(reader.RequiredString("sequence"), Parse<NarrativeOrder>(reader.RequiredString("order")), reader.RequiredInteger("ordinal")),
            _ => throw new JsonException($"Unknown frame of a position '{frame}'."),
        };
        reader.RequireNoOtherProperties();
        return position;
    }

    private static CalendarPosition ReadCalendarPosition(ObjectReader reader)
    {
        var calendar = reader.OptionalString("calendar") is { } name ? Parse<CalendarSystem>(name) : CalendarSystem.Unspecified;
        if (calendar == CalendarSystem.Unspecified && reader.Has("calendar"))
        {
            throw new JsonException("A calendar that is not specified is left out.");
        }

        var era = Parse<Era>(reader.RequiredString("era"));
        var year = reader.RequiredInteger("year");
        var month = reader.OptionalInteger("month");
        var day = reader.OptionalInteger("day");
        if (day is not null && month is null)
        {
            throw new JsonException("A day needs its month.");
        }

        var position = (month, day) switch
        {
            (null, _) => CalendarPosition.OfYear(calendar, era, year),
            (not null, null) => CalendarPosition.OfMonth(calendar, era, year, month.Value),
            _ => CalendarPosition.OfDay(calendar, era, year, month.Value, day!.Value),
        };
        return reader.OptionalFlag("approximate") ? position.Approximately() : position;
    }

    private static AgePosition ReadAgePosition(ObjectReader reader)
    {
        var value = reader.RequiredDecimal("value");
        var unit = Parse<AgeUnit>(reader.RequiredString("unit"));
        var reference = reader.OptionalInteger("reference") is { } year ? AgeReference.Year(year) : AgeReference.Unspecified;
        var basis = reader.OptionalString("basis") is { } name ? Parse<AgeBasis>(name) : AgeBasis.CalendarYears;
        var position = AgePosition.Of(value, unit, reference, basis);
        return reader.OptionalFlag("approximate") ? position.Approximately() : position;
    }

    // Enum names are written in camel case and read back exactly; numbers and other spellings are refused.
    private static string Name<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static TEnum Parse<TEnum>(string name)
        where TEnum : struct, Enum
    {
        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (Name(value) == name)
            {
                return value;
            }
        }

        throw new JsonException($"'{name}' is not a value of {typeof(TEnum).Name}.");
    }

    /// <summary>Reads the properties of one JSON object and reports those nobody asked for.</summary>
    private sealed class ObjectReader
    {
        private readonly JsonElement _element;
        private readonly HashSet<string> _read = new(StringComparer.Ordinal);

        public ObjectReader(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("An object was expected.");
            }

            _element = element;
        }

        public bool Has(string name) => _element.TryGetProperty(name, out _);

        public JsonElement? Optional(string name)
        {
            _read.Add(name);
            return _element.TryGetProperty(name, out var value) ? value : null;
        }

        public JsonElement Required(string name) => Optional(name) ?? throw new JsonException($"'{name}' is missing.");

        public string RequiredString(string name) => OptionalString(name) ?? throw new JsonException($"'{name}' is missing.");

        public string? OptionalString(string name)
        {
            if (Optional(name) is not { } value)
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String ? value.GetString() : throw new JsonException($"'{name}' is a text.");
        }

        public int RequiredInteger(string name) => OptionalInteger(name) ?? throw new JsonException($"'{name}' is missing.");

        public int? OptionalInteger(string name)
        {
            if (Optional(name) is not { } value)
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
                ? number
                : throw new JsonException($"'{name}' is a whole number.");
        }

        public decimal RequiredDecimal(string name)
        {
            var value = Required(name);

            // The raw text keeps the digits of the source; exponent notation is refused because it hides them.
            if (value.ValueKind != JsonValueKind.Number
                || !decimal.TryParse(value.GetRawText(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
            {
                throw new JsonException($"'{name}' is a decimal number.");
            }

            return number;
        }

        public bool OptionalFlag(string name)
        {
            if (Optional(name) is not { } value)
            {
                return false;
            }

            // A flag is present and true, or absent: "approximate": false would be a second way to say the same.
            return value.ValueKind == JsonValueKind.True ? true : throw new JsonException($"'{name}' is true or left out.");
        }

        public void RequireNoOtherProperties()
        {
            foreach (var property in _element.EnumerateObject())
            {
                if (!_read.Contains(property.Name))
                {
                    throw new JsonException($"Unknown property '{property.Name}'.");
                }
            }
        }
    }
}
