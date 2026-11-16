namespace Vafadar.Temporal;

/// <summary>
/// Whether two times overlap. "Definite" and "none" hold within the recorded positions and assertions – they are not
/// claims about what really happened.
/// </summary>
public enum TemporalOverlap
{
    /// <summary>The times overlap in every reading the records allow.</summary>
    Definite = 0,

    /// <summary>The times overlap in some readings and not in others.</summary>
    Possible = 1,

    /// <summary>The times overlap in no reading.</summary>
    None = 2,

    /// <summary>A fact needed for the answer is missing: an unknown time, an age without a reference, a period without any stated position.</summary>
    InsufficientInformation = 3,

    /// <summary>The times are of different kinds – a date and a place in a story, two stories, two eras – and have no common scale.</summary>
    Incompatible = 4,
}

/// <summary>Whether one time is before another.</summary>
public enum TemporalOrder
{
    /// <summary>The first is before the second in every reading.</summary>
    Before = 0,

    /// <summary>The first is after the second in every reading.</summary>
    After = 1,

    /// <summary>The readings overlap; the records do not decide the order.</summary>
    Undetermined = 2,

    /// <summary>A fact needed for the answer is missing.</summary>
    InsufficientInformation = 3,

    /// <summary>The times are of different kinds and have no common scale.</summary>
    Incompatible = 4,
}

/// <summary>What an answer rests on, so that it can be explained instead of being shown as bare fact.</summary>
[Flags]
public enum ComparisonBasis
{
    /// <summary>Only stated days, ages or story positions were compared.</summary>
    None = 0,

    /// <summary>A year or a month was read as "somewhere in that year or month".</summary>
    PrecisionRange = 1,

    /// <summary>A position marked as approximate was compared at its stated value.</summary>
    ApproximateValue = 2,

    /// <summary>The answer holds because a period is asserted to have lasted without a break.</summary>
    ContinuityAssertion = 4,

    /// <summary>
    /// The calendars differ or are not named, so only the count of years was compared, with a margin: positions less
    /// than three years apart are not ordered.
    /// </summary>
    YearCountOnly = 8,

    /// <summary>A period with an unknown start or an open end took part.</summary>
    OpenBound = 16,
}

/// <summary>The answer of <see cref="TemporalComparer.Overlap(TemporalExpression, TemporalExpression)"/>.</summary>
/// <param name="Overlap">Whether the times overlap.</param>
/// <param name="Basis">What the answer rests on.</param>
public readonly record struct OverlapResult(TemporalOverlap Overlap, ComparisonBasis Basis);

/// <summary>The answer of <see cref="TemporalComparer.Order"/>.</summary>
/// <param name="Order">Whether the first time is before the second.</param>
/// <param name="Basis">What the answer rests on.</param>
public readonly record struct OrderResult(TemporalOrder Order, ComparisonBasis Basis);

/// <summary>
/// A stretch of time to search in: "during 1383", "from 2000 to 2005". Unlike a <see cref="Period"/> it asserts
/// nothing – it is the whole stretch from the beginning of its first position to the end of its last.
/// </summary>
public sealed record TemporalWindow
{
    private TemporalWindow(TimePosition from, TimePosition to)
    {
        From = from;
        To = to;
    }

    /// <summary>Gets the position the window begins with.</summary>
    public TimePosition From { get; }

    /// <summary>Gets the position the window ends with.</summary>
    public TimePosition To { get; }

    /// <summary>The whole of <paramref name="position"/>: a year, a month, a day.</summary>
    public static TemporalWindow Of(TimePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new TemporalWindow(position, position);
    }

    /// <summary>From the beginning of <paramref name="from"/> to the end of <paramref name="to"/>.</summary>
    /// <exception cref="ArgumentException">The positions are reversed or cannot be put on one scale.</exception>
    public static TemporalWindow Between(TimePosition from, TimePosition to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        Scale.RequireOrdered(from, to, nameof(to));
        return new TemporalWindow(from, to);
    }
}
