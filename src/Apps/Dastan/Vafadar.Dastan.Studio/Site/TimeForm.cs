using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>
/// A time of the calendar as a form (step 13.1): what happened once, what happened between two times, or what
/// lasted – with "about", an unknown beginning, an open or unknown end, as the file of a collection writes them.
/// A time the form has no inputs for – a place in a story, a calendar other than the Gregorian one – is not
/// taken apart: it stays written as it stands.
/// </summary>
public static class TimeForm
{
    private const string None = "none";
    private const string Once = "occurrence";
    private const string Window = "between";
    private const string Lasting = "period";

    private static readonly FieldOption[] Kinds = [new(None, "–"), new(Once, Once), new(Window, Window), new(Lasting, Lasting)];
    private static readonly FieldOption[] Eras = [new("commonEra", "CE"), new("beforeCommonEra", "BCE")];
    private static readonly FieldOption[] Starts = [.. Enum.GetNames<PeriodStartState>().Select(Option)];
    private static readonly FieldOption[] Ends = [.. Enum.GetNames<PeriodEndState>().Select(Option)];

    /// <summary>Returns the time of a field, when the form has inputs for all of it; nothing otherwise.</summary>
    public static TemporalExpression? Parse(JsonNode? node)
    {
        if (node is not JsonObject)
        {
            return null;
        }

        try
        {
            return TemporalJson.Deserialize(node.ToJsonString()) switch
            {
                Occurrence { At: CalendarPosition at } once when Plain(at) => once,
                OccurrenceWindow { Earliest: CalendarPosition earliest, Latest: CalendarPosition latest } window when Plain(earliest) && Plain(latest) => window,
                Period { LastConfirmed: null } period when Plain(period.Start.Earliest) && Plain(period.Start.Latest) && Plain(period.End.Earliest) && Plain(period.End.Latest) => period,
                _ => null,
            };
        }
        catch (Exception refused) when (refused is JsonException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Returns the inputs of a time: its kind, up to four positions, and how a period begins and ends.</summary>
    /// <param name="path">The name of the field in the form.</param>
    /// <param name="time">The time the inputs start with; nothing for a time that is not stated.</param>
    public static IReadOnlyList<FormField> Parts(string path, TemporalExpression? time)
    {
        var (kind, from, latest, until, untilLatest) = time switch
        {
            Occurrence once => (Once, once.At, null, null, (TimePosition?)null),
            OccurrenceWindow window => (Window, window.Earliest, window.Latest, null, null),
            Period period => (
                Lasting,
                period.Start.Earliest,
                period.Start.State == PeriodStartState.Between ? period.Start.Latest : null,
                period.End.Earliest,
                period.End.State == PeriodEndState.Between ? period.End.Latest : null),
            _ => (None, null, null, null, null),
        };
        var lasting = time as Period;
        return
        [
            Choice(path, "kind", Kinds, kind),
            .. Position(path, "at", from as CalendarPosition),
            .. Position(path, "latest", latest as CalendarPosition),
            Choice(path, "start", Starts, Name(lasting?.Start.State ?? PeriodStartState.Known)),
            Choice(path, "end", Ends, Name(lasting?.End.State ?? PeriodEndState.Known)),
            .. Position(path, "until", until as CalendarPosition),
            .. Position(path, "untilLatest", untilLatest as CalendarPosition),
            new FormField { Path = $"{path}.continuous", Name = "continuous", Kind = FieldKind.Flag, Value = JsonValue.Create(lasting?.Continuity == PeriodContinuity.Continuous) },
        ];
    }

    /// <summary>
    /// Returns the time as the form was filled in: nothing for a time that is not stated, the field as it was
    /// when nothing of it was changed, else the time written anew. What cannot be a time is a problem.
    /// </summary>
    public static (JsonNode? Node, bool Present)? Read(FormField field, IFormCollection form, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(problems);
        var path = field.Path;
        var stood = Parse(field.Value);
        var before = problems.Count;
        TemporalExpression? built = null;
        try
        {
            built = Sent(form, $"{path}.kind") switch
            {
                Once => new Occurrence(Required(form, path, "at", stood, problems)!),
                Window => new OccurrenceWindow(Required(form, path, "at", stood, problems)!, Required(form, path, "latest", stood, problems)!),
                Lasting => new Period(
                    Sent(form, $"{path}.start") switch
                    {
                        "unknown" => PeriodStart.Unknown,
                        "between" => PeriodStart.Between(Required(form, path, "at", stood, problems)!, Required(form, path, "latest", stood, problems)!),
                        _ => PeriodStart.At(Required(form, path, "at", stood, problems)!),
                    },
                    Sent(form, $"{path}.end") switch
                    {
                        "ongoing" => PeriodEnd.Ongoing,
                        "endedAtUnknownTime" => PeriodEnd.EndedAtUnknownTime,
                        "unknown" => PeriodEnd.Unknown,
                        "between" => PeriodEnd.Between(Required(form, path, "until", stood, problems)!, Required(form, path, "untilLatest", stood, problems)!),
                        _ => PeriodEnd.At(Required(form, path, "until", stood, problems)!),
                    },
                    form[$"{path}.continuous"].Contains("true") ? PeriodContinuity.Continuous : PeriodContinuity.NotAsserted),
                _ => null,
            };
        }
        catch (Exception refused) when (refused is ArgumentException or NullReferenceException)
        {
            if (problems.Count == before)
            {
                problems.Add($"{field.Name}: {refused.Message}");
            }
        }

        if (problems.Count > before)
        {
            return (field.Value?.DeepClone(), true);
        }

        if (built is null)
        {
            return null;
        }

        // A time that reads as before is the field as it was: the file keeps its own way of writing it.
        return (built.Equals(stood) ? field.Value!.DeepClone() : JsonNode.Parse(TemporalJson.Serialize(built)), true);
    }

    // A position the inputs can say: a year of the common era or before it, in the Gregorian calendar or in a
    // calendar the file does not name (D-043). The calendar of a position stays what the file says.
    private static bool Plain(TimePosition? position) =>
        position is null or CalendarPosition { Calendar: CalendarSystem.Gregorian or CalendarSystem.Unspecified };

    // The calendar of the position that stood at a place of the time before, for the position written there now.
    private static CalendarSystem Calendar(TemporalExpression? before, string name) => (before, name) switch
    {
        (Occurrence { At: CalendarPosition at }, "at") => at.Calendar,
        (OccurrenceWindow { Earliest: CalendarPosition at }, "at") => at.Calendar,
        (OccurrenceWindow { Latest: CalendarPosition latest }, "latest") => latest.Calendar,
        (Period { Start.Earliest: CalendarPosition at }, "at") => at.Calendar,
        (Period { Start.Latest: CalendarPosition latest }, "latest") => latest.Calendar,
        (Period { End.Earliest: CalendarPosition until }, "until") => until.Calendar,
        (Period { End.Latest: CalendarPosition latest }, "untilLatest") => latest.Calendar,
        _ => CalendarSystem.Unspecified,
    };

    private static IEnumerable<FormField> Position(string path, string name, CalendarPosition? position) =>
    [
        Choice(path, $"{name}.era", Eras, position is { Era: Era.BeforeCommonEra } ? "beforeCommonEra" : "commonEra"),
        Text(path, $"{name}.year", position?.Year),
        Text(path, $"{name}.month", position?.Month),
        Text(path, $"{name}.day", position?.Day),
        new FormField { Path = $"{path}.{name}.about", Name = $"{name}.about", Kind = FieldKind.Flag, Value = JsonValue.Create(position?.IsApproximate == true) },
    ];

    private static FormField Choice(string path, string name, IReadOnlyList<FieldOption> options, string value) =>
        new() { Path = $"{path}.{name}", Name = name, Kind = FieldKind.Choice, Value = JsonValue.Create(value), Options = options };

    private static FormField Text(string path, string name, int? value) =>
        new() { Path = $"{path}.{name}", Name = name, Kind = FieldKind.Text, Value = JsonValue.Create(value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty) };

    private static FieldOption Option(string name) => new(Name(name), Name(name));

    private static string Name<T>(T state)
        where T : notnull => Name(state.ToString()!);

    private static string Name(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    private static string Sent(IFormCollection form, string name) => form[name].ToString().Trim();

    // A position the kind of time needs: its year at least, a month for a day.
    private static CalendarPosition? Required(IFormCollection form, string path, string name, TemporalExpression? stood, List<string> problems)
    {
        int? Whole(string part)
        {
            var text = Vafadar.Core.Text.Digits.ToAscii(Sent(form, $"{path}.{name}.{part}"));
            if (text.Length == 0)
            {
                return null;
            }

            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            problems.Add($"time: {name}.{part} is a whole number.");
            return null;
        }

        var before = problems.Count;
        var (year, month, day) = (Whole("year"), Whole("month"), Whole("day"));
        if (problems.Count > before)
        {
            return null;
        }

        if (year is null)
        {
            problems.Add($"time: {name}.year is missing.");
            return null;
        }

        if (day is not null && month is null)
        {
            problems.Add($"time: {name}.day needs a month.");
            return null;
        }

        var era = Sent(form, $"{path}.{name}.era") == "beforeCommonEra" ? Era.BeforeCommonEra : Era.CommonEra;
        var calendar = Calendar(stood, name);
        var position = (month, day) switch
        {
            ({ } m, { } d) => CalendarPosition.OfDay(calendar, era, year.Value, m, d),
            ({ } m, null) => CalendarPosition.OfMonth(calendar, era, year.Value, m),
            _ => CalendarPosition.OfYear(calendar, era, year.Value),
        };
        return form[$"{path}.{name}.about"].Contains("true") ? position.Approximately() : position;
    }
}
