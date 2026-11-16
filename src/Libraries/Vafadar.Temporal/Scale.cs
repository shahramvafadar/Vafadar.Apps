namespace Vafadar.Temporal;

/// <summary>A closed range of coordinates on a <see cref="Scale"/>; an open side is one of the infinities.</summary>
internal readonly record struct Span(decimal Lo, decimal Hi)
{
    /// <summary>Stands for "no lower limit". No coordinate of a position comes near it.</summary>
    public const decimal NegativeInfinity = decimal.MinValue;

    /// <summary>Stands for "no upper limit".</summary>
    public const decimal PositiveInfinity = decimal.MaxValue;

    /// <summary>Gets a span without limits.</summary>
    public static Span Unbounded => new(NegativeInfinity, PositiveInfinity);

    /// <summary>Gets whether neither side is limited.</summary>
    public bool IsUnbounded => Lo == NegativeInfinity && Hi == PositiveInfinity;
}

/// <summary>Whether a set of positions can be put on one scale.</summary>
internal enum ScaleStatus
{
    /// <summary>The positions share a scale.</summary>
    Comparable,

    /// <summary>The positions belong to different kinds of time, eras, bases or stories; no order exists between them.</summary>
    Incompatible,

    /// <summary>The positions could share a scale, but a fact needed for it is missing (the reference of an age).</summary>
    InsufficientInformation,
}

/// <summary>
/// The common scale of the positions of a comparison, and the coordinates of each position on it. Coordinates exist
/// for ordering and overlap only: inside a year they are a regular grid, not elapsed days.
/// </summary>
/// <remarks>
/// The scales (ADR 0012):
/// <list type="bullet">
/// <item>Calendar positions of one named calendar: a grid of days, 12 months of 31 places per year. It keeps the
/// order of real dates and puts a day inside its month and a month inside its year.</item>
/// <item>Calendar positions of one era whose calendars differ or are not named: whole years, each side widened by
/// half a year, because the years of two calendars do not start on the same day.</item>
/// <item>Ages with the same basis: years, counted forward to the reference.</item>
/// <item>Positions of one story sequence in one order: the ordinal.</item>
/// </list>
/// Everything else has no common scale.
/// </remarks>
internal sealed class Scale
{
    private const decimal PlacesPerMonth = 31m;
    private const decimal PlacesPerYear = 12m * PlacesPerMonth;

    private readonly Mode _mode;
    private readonly bool _agesShareAnUnstatedReference;

    private Scale(ScaleStatus status, Mode mode = Mode.CalendarDays, bool agesShareAnUnstatedReference = false)
    {
        Status = status;
        _mode = mode;
        _agesShareAnUnstatedReference = agesShareAnUnstatedReference;
        if (mode == Mode.CalendarYears)
        {
            Basis = ComparisonBasis.YearCountOnly;
            Margin = 0.5m;
        }
    }

    private enum Mode
    {
        CalendarDays,
        CalendarYears,
        Age,
        Narrative,
    }

    /// <summary>Gets whether the positions share this scale.</summary>
    public ScaleStatus Status { get; }

    /// <summary>
    /// Gets how far a coordinate may be off because the scale is coarser than the positions; every span of
    /// <see cref="Project"/> is already widened by it on both sides.
    /// </summary>
    public decimal Margin { get; }

    /// <summary>Gets what the coordinates given out so far rest on; grows with each <see cref="Project"/>.</summary>
    public ComparisonBasis Basis { get; private set; }

    /// <summary>Finds the common scale of <paramref name="positions"/>.</summary>
    /// <param name="positions">The positions of the comparison; at least one.</param>
    /// <param name="sameStatement">
    /// Whether the positions belong to one expression. Positions of one statement share what the statement leaves
    /// unsaid: two ages without a stated reference have the same reference, two dates without a named calendar the
    /// same calendar. Positions of different statements do not.
    /// </param>
    public static Scale For(IReadOnlyList<TimePosition> positions, bool sameStatement)
    {
        if (positions.Count == 0)
        {
            return new Scale(ScaleStatus.InsufficientInformation);
        }

        return positions[0] switch
        {
            CalendarPosition => ForCalendar(positions, sameStatement),
            AgePosition => ForAge(positions, sameStatement),
            NarrativePosition => ForNarrative(positions),
            _ => new Scale(ScaleStatus.Incompatible),
        };
    }

    /// <summary>
    /// Throws when <paramref name="later"/> is before <paramref name="earlier"/> in every reading, or when the two
    /// cannot be ordered at all. Used where one statement names two positions in a fixed order.
    /// </summary>
    public static void RequireOrdered(TimePosition earlier, TimePosition later, string parameterName)
    {
        var scale = For([earlier, later], sameStatement: true);
        if (scale.Status != ScaleStatus.Comparable)
        {
            throw new ArgumentException("The positions are of different kinds of time and cannot be ordered.", parameterName);
        }

        if (scale.Project(later).Hi < scale.Project(earlier).Lo)
        {
            throw new ArgumentException("The later position is before the earlier one.", parameterName);
        }
    }

    /// <summary>
    /// Returns the coordinates <paramref name="position"/> may have: one point for an age or a story position, the
    /// whole unit for a year or a month.
    /// </summary>
    public Span Project(TimePosition position)
    {
        switch (position)
        {
            case CalendarPosition calendar when _mode == Mode.CalendarDays:
                Note(calendar.IsApproximate, calendar.Precision != CalendarPrecision.Day);
                var first = (calendar.ProlepticYear * PlacesPerYear) + (((calendar.Month ?? 1) - 1) * PlacesPerMonth) + ((calendar.Day ?? 1) - 1);
                var last = (calendar.ProlepticYear * PlacesPerYear) + (((calendar.Month ?? 12) - 1) * PlacesPerMonth) + ((calendar.Day ?? PlacesPerMonth) - 1);
                return new Span(first, last);

            case CalendarPosition calendar:
                Note(calendar.IsApproximate, coarse: true);
                return new Span(calendar.ProlepticYear - Margin, calendar.ProlepticYear + 1 + Margin);

            case AgePosition age:
                Note(age.IsApproximate, coarse: false);
                var year = (_agesShareAnUnstatedReference ? 0 : age.Reference.CommonEraYear!.Value) - age.Years;
                return new Span(year, year);

            case NarrativePosition narrative:
                return new Span(narrative.Ordinal, narrative.Ordinal);

            default:
                throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown kind of position.");
        }
    }

    private static Scale ForCalendar(IReadOnlyList<TimePosition> positions, bool sameStatement)
    {
        if (positions.Any(position => position is not CalendarPosition))
        {
            return new Scale(ScaleStatus.Incompatible);
        }

        var first = (CalendarPosition)positions[0];
        var calendars = positions.Cast<CalendarPosition>().ToList();
        if (calendars.Any(position => CalendarRules.EpochOf(position.Era) != CalendarRules.EpochOf(first.Era)))
        {
            return new Scale(ScaleStatus.Incompatible);
        }

        var oneCalendar = calendars.All(position => position.Calendar == first.Calendar);
        var named = first.Calendar != CalendarSystem.Unspecified;
        return new Scale(ScaleStatus.Comparable, oneCalendar && (named || sameStatement) ? Mode.CalendarDays : Mode.CalendarYears);
    }

    private static Scale ForAge(IReadOnlyList<TimePosition> positions, bool sameStatement)
    {
        if (positions.Any(position => position is not AgePosition))
        {
            return new Scale(ScaleStatus.Incompatible);
        }

        var ages = positions.Cast<AgePosition>().ToList();
        if (ages.Any(age => age.Basis != ages[0].Basis))
        {
            return new Scale(ScaleStatus.Incompatible);
        }

        if (ages.All(age => age.Reference.IsSpecified))
        {
            return new Scale(ScaleStatus.Comparable, Mode.Age);
        }

        return sameStatement && ages.All(age => !age.Reference.IsSpecified)
            ? new Scale(ScaleStatus.Comparable, Mode.Age, agesShareAnUnstatedReference: true)
            : new Scale(ScaleStatus.InsufficientInformation);
    }

    private static Scale ForNarrative(IReadOnlyList<TimePosition> positions)
    {
        if (positions.Any(position => position is not NarrativePosition))
        {
            return new Scale(ScaleStatus.Incompatible);
        }

        var first = (NarrativePosition)positions[0];
        var sameSequence = positions.Cast<NarrativePosition>()
            .All(position => position.Order == first.Order && string.Equals(position.Sequence, first.Sequence, StringComparison.Ordinal));
        return sameSequence ? new Scale(ScaleStatus.Comparable, Mode.Narrative) : new Scale(ScaleStatus.Incompatible);
    }

    private void Note(bool approximate, bool coarse)
    {
        if (approximate)
        {
            Basis |= ComparisonBasis.ApproximateValue;
        }

        if (coarse && _mode == Mode.CalendarDays)
        {
            Basis |= ComparisonBasis.PrecisionRange;
        }
    }
}
