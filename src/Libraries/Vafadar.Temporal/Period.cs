namespace Vafadar.Temporal;

/// <summary>What is known about the start of a <see cref="Period"/>.</summary>
public enum PeriodStartState
{
    /// <summary>The start is at a stated position.</summary>
    Known = 0,

    /// <summary>The start lies between two positions.</summary>
    Between = 1,

    /// <summary>The start is not known.</summary>
    Unknown = 2,
}

/// <summary>What is known about the end of a <see cref="Period"/>. The four open states are different facts.</summary>
public enum PeriodEndState
{
    /// <summary>The end is at a stated position.</summary>
    Known = 0,

    /// <summary>The end lies between two positions.</summary>
    Between = 1,

    /// <summary>According to the latest information the period is still going on.</summary>
    Ongoing = 2,

    /// <summary>The period has ended, but it is not known when.</summary>
    EndedAtUnknownTime = 3,

    /// <summary>It is not known whether the period has ended.</summary>
    Unknown = 4,
}

/// <summary>Whether a <see cref="Period"/> is asserted to have lasted without a break.</summary>
public enum PeriodContinuity
{
    /// <summary>Nothing is said. The period may have had gaps, so nothing is certain about a time inside it.</summary>
    NotAsserted = 0,

    /// <summary>The source asserts that the period lasted without a break between its start and its end.</summary>
    Continuous = 1,
}

/// <summary>The start of a <see cref="Period"/>.</summary>
public sealed record PeriodStart
{
    private PeriodStart(PeriodStartState state, TimePosition? earliest, TimePosition? latest)
    {
        State = state;
        Earliest = earliest;
        Latest = latest;
    }

    /// <summary>Gets what is known about the start.</summary>
    public PeriodStartState State { get; }

    /// <summary>Gets the stated or earliest possible position; <see langword="null"/> when the start is unknown.</summary>
    public TimePosition? Earliest { get; }

    /// <summary>Gets the stated or latest possible position; <see langword="null"/> when the start is unknown.</summary>
    public TimePosition? Latest { get; }

    /// <summary>The start is not known.</summary>
    public static PeriodStart Unknown { get; } = new(PeriodStartState.Unknown, null, null);

    /// <summary>The period started at <paramref name="position"/>.</summary>
    public static PeriodStart At(TimePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new PeriodStart(PeriodStartState.Known, position, position);
    }

    /// <summary>The period started somewhere between two positions.</summary>
    /// <exception cref="ArgumentException">The positions are reversed or cannot be put on one scale.</exception>
    public static PeriodStart Between(TimePosition earliest, TimePosition latest)
    {
        ArgumentNullException.ThrowIfNull(earliest);
        ArgumentNullException.ThrowIfNull(latest);
        Scale.RequireOrdered(earliest, latest, nameof(latest));
        return new PeriodStart(PeriodStartState.Between, earliest, latest);
    }
}

/// <summary>The end of a <see cref="Period"/>.</summary>
public sealed record PeriodEnd
{
    private PeriodEnd(PeriodEndState state, TimePosition? earliest, TimePosition? latest)
    {
        State = state;
        Earliest = earliest;
        Latest = latest;
    }

    /// <summary>Gets what is known about the end.</summary>
    public PeriodEndState State { get; }

    /// <summary>Gets the stated or earliest possible position; <see langword="null"/> for the open states.</summary>
    public TimePosition? Earliest { get; }

    /// <summary>Gets the stated or latest possible position; <see langword="null"/> for the open states.</summary>
    public TimePosition? Latest { get; }

    /// <summary>
    /// According to the latest information the period is still going on. Say when that information is from with
    /// <see cref="Period.LastConfirmed"/>: without a newer confirmation it proves nothing about today.
    /// </summary>
    public static PeriodEnd Ongoing { get; } = new(PeriodEndState.Ongoing, null, null);

    /// <summary>The period has ended, but it is not known when.</summary>
    public static PeriodEnd EndedAtUnknownTime { get; } = new(PeriodEndState.EndedAtUnknownTime, null, null);

    /// <summary>It is not known whether the period has ended.</summary>
    public static PeriodEnd Unknown { get; } = new(PeriodEndState.Unknown, null, null);

    /// <summary>The period ended at <paramref name="position"/>.</summary>
    public static PeriodEnd At(TimePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new PeriodEnd(PeriodEndState.Known, position, position);
    }

    /// <summary>The period ended somewhere between two positions.</summary>
    /// <exception cref="ArgumentException">The positions are reversed or cannot be put on one scale.</exception>
    public static PeriodEnd Between(TimePosition earliest, TimePosition latest)
    {
        ArgumentNullException.ThrowIfNull(earliest);
        ArgumentNullException.ThrowIfNull(latest);
        Scale.RequireOrdered(earliest, latest, nameof(latest));
        return new PeriodEnd(PeriodEndState.Between, earliest, latest);
    }
}

/// <summary>
/// Something lasted from a start to an end: a reign, a membership, the existence of a company. Start and end are
/// known, uncertain or open independently of each other. A period is different from an occurrence somewhere inside a
/// window (<see cref="OccurrenceWindow"/>), even when both name the same two positions.
/// </summary>
public sealed record Period : TemporalExpression
{
    /// <summary>Creates a period.</summary>
    /// <param name="start">What is known about the start.</param>
    /// <param name="end">What is known about the end.</param>
    /// <param name="continuity">Whether the source asserts that the period lasted without a break.</param>
    /// <param name="lastConfirmed">
    /// The latest position at which the period is confirmed to have been going on, when the end is open.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The positions cannot be put on one scale, or the end is before the start in every reading.
    /// </exception>
    public Period(PeriodStart start, PeriodEnd end, PeriodContinuity continuity = PeriodContinuity.NotAsserted, TimePosition? lastConfirmed = null)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        if (!Enum.IsDefined(continuity))
        {
            throw new ArgumentOutOfRangeException(nameof(continuity), continuity, "Unknown continuity.");
        }

        if (start.Earliest is { } startEarliest && end.Latest is { } endLatest)
        {
            Scale.RequireOrdered(startEarliest, endLatest, nameof(end));
        }

        if (lastConfirmed is not null)
        {
            if (start.Earliest is { } notBefore)
            {
                Scale.RequireOrdered(notBefore, lastConfirmed, nameof(lastConfirmed));
            }

            if (end.Latest is { } notAfter)
            {
                Scale.RequireOrdered(lastConfirmed, notAfter, nameof(lastConfirmed));
            }
        }

        Start = start;
        End = end;
        Continuity = continuity;
        LastConfirmed = lastConfirmed;
    }

    /// <summary>Gets what is known about the start.</summary>
    public PeriodStart Start { get; }

    /// <summary>Gets what is known about the end.</summary>
    public PeriodEnd End { get; }

    /// <summary>Gets whether the source asserts that the period lasted without a break.</summary>
    public PeriodContinuity Continuity { get; }

    /// <summary>
    /// Gets the latest position at which the period is confirmed to have been going on. It supports what was true
    /// then and is not carried forward to today.
    /// </summary>
    public TimePosition? LastConfirmed { get; }
}
