namespace Vafadar.Temporal;

/// <summary>
/// Answers whether times overlap and which comes first – as far as the records allow. It never fills a gap to get an
/// answer: different kinds of time are <see cref="TemporalOverlap.Incompatible"/>, missing facts are
/// <see cref="TemporalOverlap.InsufficientInformation"/>, and readings that differ are "possible" or "undetermined".
/// </summary>
/// <remarks>
/// How expressions are read (ADR 0012):
/// <list type="bullet">
/// <item>An occurrence is one moment somewhere in its position, window or alternatives. Two occurrences whose
/// possible times meet overlap possibly, never definitely: sharing a year does not make events simultaneous.</item>
/// <item>A period runs from a start to an end, each somewhere in its position or range; both ends belong to it. It
/// definitely covers a time only when it is asserted to be continuous.</item>
/// <item>A window is exactly the stretch it names.</item>
/// </list>
/// </remarks>
public static class TemporalComparer
{
    /// <summary>Returns whether the times of two expressions overlap.</summary>
    public static OverlapResult Overlap(TemporalExpression first, TemporalExpression second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return Overlap(PositionsOf(first), scale => ExtentOf(first, scale), PositionsOf(second), scale => ExtentOf(second, scale));
    }

    /// <summary>
    /// Returns whether the time of <paramref name="expression"/> falls in <paramref name="window"/>: definitely when
    /// every reading does, possibly when some do.
    /// </summary>
    public static OverlapResult Overlap(TemporalExpression expression, TemporalWindow window)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(window);
        return Overlap(PositionsOf(expression), scale => ExtentOf(expression, scale), [window.From, window.To], scale => ExtentOf(window, scale));
    }

    /// <summary>
    /// Returns whether <paramref name="first"/> is before <paramref name="second"/>: every possible time of the one
    /// before every possible time of the other. A period counts from its earliest possible start to its latest
    /// possible end.
    /// </summary>
    public static OrderResult Order(TemporalExpression first, TemporalExpression second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var prepared = Prepare(PositionsOf(first), scale => ExtentOf(first, scale), PositionsOf(second), scale => ExtentOf(second, scale));
        if (prepared.Failure is { } failure)
        {
            return new OrderResult(failure == TemporalOverlap.Incompatible ? TemporalOrder.Incompatible : TemporalOrder.InsufficientInformation, ComparisonBasis.None);
        }

        var (a, b, basis) = (prepared.First!.Hull, prepared.Second!.Hull, prepared.Basis);
        var order = a.Hi < b.Lo ? TemporalOrder.Before : b.Hi < a.Lo ? TemporalOrder.After : TemporalOrder.Undetermined;
        return new OrderResult(order, basis);
    }

    private static OverlapResult Overlap(
        IReadOnlyList<TimePosition> firstPositions,
        Func<Scale, Extent?> firstExtent,
        IReadOnlyList<TimePosition> secondPositions,
        Func<Scale, Extent?> secondExtent)
    {
        var prepared = Prepare(firstPositions, firstExtent, secondPositions, secondExtent);
        if (prepared.Failure is { } failure)
        {
            return new OverlapResult(failure, ComparisonBasis.None);
        }

        var (a, b, basis) = (prepared.First!, prepared.Second!, prepared.Basis);
        var (definite, possible) = (a.Moments, b.Moments) switch
        {
            (null, null) => IntervalAndInterval(a, b),
            (null, not null) => MomentsAndInterval(b.Moments!, a),
            (not null, null) => MomentsAndInterval(a.Moments!, b),
            // Two occurrences that may fall together are not shown to fall together.
            _ => (false, a.Moments!.Any(x => b.Moments!.Any(y => x.Lo <= y.Hi && y.Lo <= x.Hi))),
        };

        if (definite)
        {
            // A period is only known to cover a time inside it when it is asserted to have had no gap.
            if (a.IsAssertedPeriod || b.IsAssertedPeriod)
            {
                basis |= ComparisonBasis.ContinuityAssertion;
            }

            return new OverlapResult(TemporalOverlap.Definite, basis);
        }

        return new OverlapResult(possible ? TemporalOverlap.Possible : TemporalOverlap.None, basis);
    }

    private static (bool Definite, bool Possible) IntervalAndInterval(Extent a, Extent b) =>
        (a.Continuous && b.Continuous && a.Start.Hi <= b.End.Lo && b.Start.Hi <= a.End.Lo,
         a.Start.Lo <= b.End.Hi && b.Start.Lo <= a.End.Hi);

    private static (bool Definite, bool Possible) MomentsAndInterval(IReadOnlyList<Span> moments, Extent interval)
    {
        // Every alternative has to fall inside for "definite"; one that can is enough for "possible".
        var definite = interval.Continuous && moments.All(moment => interval.Start.Hi <= moment.Lo && moment.Hi <= interval.End.Lo);
        var possible = moments.Any(moment => interval.Start.Lo <= moment.Hi && moment.Lo <= interval.End.Hi);
        return (definite, possible);
    }

    private static Prepared Prepare(
        IReadOnlyList<TimePosition> firstPositions,
        Func<Scale, Extent?> firstExtent,
        IReadOnlyList<TimePosition> secondPositions,
        Func<Scale, Extent?> secondExtent)
    {
        if (firstPositions.Count == 0 || secondPositions.Count == 0)
        {
            return new Prepared(TemporalOverlap.InsufficientInformation, null, null, ComparisonBasis.None);
        }

        var scale = Scale.For([.. firstPositions, .. secondPositions], sameStatement: false);
        if (scale.Status != ScaleStatus.Comparable)
        {
            var failure = scale.Status == ScaleStatus.Incompatible ? TemporalOverlap.Incompatible : TemporalOverlap.InsufficientInformation;
            return new Prepared(failure, null, null, ComparisonBasis.None);
        }

        var first = firstExtent(scale);
        var second = secondExtent(scale);
        if (first is null || second is null)
        {
            return new Prepared(TemporalOverlap.InsufficientInformation, null, null, ComparisonBasis.None);
        }

        var basis = scale.Basis;
        if (first.HasOpenBound || second.HasOpenBound)
        {
            basis |= ComparisonBasis.OpenBound;
        }

        return new Prepared(null, first, second, basis);
    }

    private static IReadOnlyList<TimePosition> PositionsOf(TemporalExpression expression) => expression switch
    {
        Occurrence occurrence => [occurrence.At],
        OccurrenceWindow window => [window.Earliest, window.Latest],
        AlternativeOccurrences alternatives => [.. alternatives.Options.SelectMany(PositionsOf)],
        Period period => [.. new[] { period.Start.Earliest, period.Start.Latest, period.End.Earliest, period.End.Latest, period.LastConfirmed }.OfType<TimePosition>()],
        _ => [],
    };

    private static Extent? ExtentOf(TemporalExpression expression, Scale scale) => expression switch
    {
        Occurrence occurrence => Extent.OfMoments([scale.Project(occurrence.At)]),
        OccurrenceWindow window => Extent.OfMoments([new Span(scale.Project(window.Earliest).Lo, scale.Project(window.Latest).Hi)]),
        AlternativeOccurrences alternatives => Extent.OfMoments([.. alternatives.Options.SelectMany(option => ExtentOf(option, scale)!.Moments!)]),
        Period period => ExtentOf(period, scale),
        _ => null,
    };

    private static Extent ExtentOf(TemporalWindow window, Scale scale)
    {
        // The window is exactly its stretch; only the margin of a coarse scale makes its edges uncertain.
        var from = scale.Project(window.From).Lo;
        var to = scale.Project(window.To).Hi;
        var slack = 2 * scale.Margin;
        return Extent.OfInterval(new Span(from, from + slack), new Span(to - slack, to), continuous: true, isAssertedPeriod: false);
    }

    private static Extent? ExtentOf(Period period, Scale scale)
    {
        var confirmed = period.LastConfirmed is null ? (Span?)null : scale.Project(period.LastConfirmed);
        var start = period.Start.State == PeriodStartState.Unknown
            ? Span.Unbounded
            : new Span(scale.Project(period.Start.Earliest!).Lo, scale.Project(period.Start.Latest!).Hi);
        var end = period.End.State is PeriodEndState.Known or PeriodEndState.Between
            ? new Span(scale.Project(period.End.Earliest!).Lo, scale.Project(period.End.Latest!).Hi)
            : Span.Unbounded;

        // What is not stated is limited by what is: a start is not after the end or the last confirmation, an open
        // end is not before the start or the last confirmation.
        if (period.Start.State == PeriodStartState.Unknown)
        {
            start = new Span(Span.NegativeInfinity, Math.Min(end.Hi, confirmed?.Hi ?? Span.PositiveInfinity));
        }

        if (end.IsUnbounded)
        {
            end = new Span(Math.Max(start.Lo, confirmed?.Lo ?? Span.NegativeInfinity), Span.PositiveInfinity);
        }

        if (start.IsUnbounded && end.IsUnbounded)
        {
            return null;
        }

        return Extent.OfInterval(start, end, period.Continuity == PeriodContinuity.Continuous, isAssertedPeriod: true);
    }

    /// <summary>The operands of a comparison on their common scale, or the reason there is none.</summary>
    private sealed record Prepared(TemporalOverlap? Failure, Extent? First, Extent? Second, ComparisonBasis Basis);

    /// <summary>
    /// The coordinates of an expression: the possible times of an occurrence (<see cref="Moments"/>), or the possible
    /// starts and ends of a period or window.
    /// </summary>
    private sealed record Extent(IReadOnlyList<Span>? Moments, Span Start, Span End, bool Continuous, bool IsAssertedPeriod)
    {
        public bool HasOpenBound => Moments is null && (Start.Lo == Span.NegativeInfinity || End.Hi == Span.PositiveInfinity);

        public Span Hull => Moments is null
            ? new Span(Start.Lo, End.Hi)
            : new Span(Moments.Min(moment => moment.Lo), Moments.Max(moment => moment.Hi));

        public static Extent OfMoments(IReadOnlyList<Span> moments) => new(moments, default, default, Continuous: false, IsAssertedPeriod: false);

        public static Extent OfInterval(Span start, Span end, bool continuous, bool isAssertedPeriod) =>
            new(null, start, end, continuous, isAssertedPeriod);
    }
}
