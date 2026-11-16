using System.Collections.Immutable;

namespace Vafadar.Temporal;

/// <summary>
/// What is said about the time of something. The kinds are kept apart because they mean different things:
/// something happened at a position (<see cref="Occurrence"/>), happened somewhere inside a window
/// (<see cref="OccurrenceWindow"/>), happened at one of several possible times (<see cref="AlternativeOccurrences"/>),
/// lasted from a start to an end (<see cref="Period"/>), took a certain time without a known position
/// (<see cref="ElapsedTime"/>), is the time of another event (<see cref="EventTime"/>), is not known
/// (<see cref="UnknownTime"/>) or does not apply (<see cref="InapplicableTime"/>).
/// </summary>
/// <remarks>
/// An expression never holds more than its source says: no month or day is added to a year, no middle of a window is
/// taken as the date, and a missing end is not read as "still going on".
/// </remarks>
public abstract record TemporalExpression
{
    private protected TemporalExpression()
    {
    }

    /// <summary>
    /// Gets the wording of the source, when it is kept ("circa 1450", "in the fifth year of the reign"). It is shown
    /// with the expression and never parsed.
    /// </summary>
    public string? SourceText { get; init; }
}

/// <summary>
/// Something happened at a position. A position known only to the year says the event lies in that year – not that
/// it lasted the year, and not that the year is in doubt.
/// </summary>
public sealed record Occurrence : TemporalExpression
{
    /// <summary>Creates an occurrence at <paramref name="at"/>.</summary>
    public Occurrence(TimePosition at)
    {
        ArgumentNullException.ThrowIfNull(at);
        At = at;
    }

    /// <summary>Gets the position.</summary>
    public TimePosition At { get; }
}

/// <summary>
/// Something happened once, somewhere between two positions: "sometime between 20,000 and 10,000 years ago". The
/// window is what is not known – it is not a duration, and nothing is asserted to have lasted that long.
/// </summary>
public sealed record OccurrenceWindow : TemporalExpression
{
    /// <summary>Creates a window from <paramref name="earliest"/> to <paramref name="latest"/>.</summary>
    /// <exception cref="ArgumentException">
    /// The positions cannot be put on one scale, or <paramref name="latest"/> is before <paramref name="earliest"/>.
    /// Reversed bounds are an error of the input and are never swapped silently.
    /// </exception>
    public OccurrenceWindow(TimePosition earliest, TimePosition latest)
    {
        ArgumentNullException.ThrowIfNull(earliest);
        ArgumentNullException.ThrowIfNull(latest);
        Scale.RequireOrdered(earliest, latest, nameof(latest));
        Earliest = earliest;
        Latest = latest;
    }

    /// <summary>Gets the earliest possible position.</summary>
    public TimePosition Earliest { get; }

    /// <summary>Gets the latest possible position.</summary>
    public TimePosition Latest { get; }
}

/// <summary>
/// Something happened at one of several possible times: "in 1980 or in 1990". The possibilities stay separate – they
/// are neither averaged nor joined into a range.
/// </summary>
public sealed record AlternativeOccurrences : TemporalExpression
{
    /// <summary>Creates the alternatives.</summary>
    /// <param name="options">Two or more <see cref="Occurrence"/> or <see cref="OccurrenceWindow"/> expressions.</param>
    public AlternativeOccurrences(IEnumerable<TemporalExpression> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = [.. options];
        if (Options.Length < 2 || Options.Any(option => option is not (Occurrence or OccurrenceWindow)))
        {
            throw new ArgumentException("Alternatives are two or more occurrences or occurrence windows.", nameof(options));
        }
    }

    /// <summary>Gets the possible times, in the order of the source.</summary>
    public ImmutableArray<TemporalExpression> Options { get; }

    /// <inheritdoc />
    public bool Equals(AlternativeOccurrences? other) =>
        other is not null && SourceText == other.SourceText && Options.SequenceEqual(other.Options);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SourceText);
        foreach (var option in Options)
        {
            hash.Add(option);
        }

        return hash.ToHashCode();
    }
}

/// <summary>The unit of an <see cref="ElapsedTime"/>.</summary>
public enum ElapsedUnit
{
    /// <summary>Days.</summary>
    Days = 0,

    /// <summary>Months.</summary>
    Months = 1,

    /// <summary>Years.</summary>
    Years = 2,
}

/// <summary>
/// How long something took, without saying when: "it lasted about thirty years". It has no place on any scale and is
/// therefore never compared with a date or a period.
/// </summary>
public sealed record ElapsedTime : TemporalExpression
{
    /// <summary>Creates an elapsed time.</summary>
    public ElapsedTime(decimal value, ElapsedUnit unit, bool isApproximate = false)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "An elapsed time is not negative.");
        }

        if (!Enum.IsDefined(unit))
        {
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit.");
        }

        Value = value;
        Unit = unit;
        IsApproximate = isApproximate;
    }

    /// <summary>Gets the amount, with the digits of the source.</summary>
    public decimal Value { get; }

    /// <summary>Gets the unit of <see cref="Value"/>.</summary>
    public ElapsedUnit Unit { get; }

    /// <summary>Gets whether the source gives the amount as approximate.</summary>
    public bool IsApproximate { get; }
}

/// <summary>
/// The time of another event, named by its identifier: a photograph "of the school festival" has the festival's time,
/// whatever that turns out to be. The library does not look the event up; until the app does, the time is not known.
/// </summary>
public sealed record EventTime : TemporalExpression
{
    /// <summary>Creates a reference to the time of <paramref name="eventId"/>.</summary>
    public EventTime(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        EventId = eventId;
    }

    /// <summary>Gets the identifier of the event, as the app names it.</summary>
    public string EventId { get; }
}

/// <summary>The time is not known. Different from <see cref="InapplicableTime"/> and from any guess.</summary>
public sealed record UnknownTime : TemporalExpression;

/// <summary>A time of this kind does not exist for the thing described – a file that was never imported has no import time.</summary>
public sealed record InapplicableTime : TemporalExpression;
