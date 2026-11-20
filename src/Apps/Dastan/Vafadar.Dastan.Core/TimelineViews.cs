using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>The axes of a timeline (ADR 0021). What is counted in one way shares a view; nothing is moved to another.</summary>
public enum TimeAxis
{
    /// <summary>Years of the common era and before it. A place is the place of <see cref="YearAxis"/>.</summary>
    Calendar = 0,

    /// <summary>A depth of time: so many years before a named present. A place is that number of years.</summary>
    Depth = 1,

    /// <summary>The order of a telling, which has no distances. A place is the number of a step.</summary>
    Order = 2,
}

/// <summary>What a mark of a timeline says about its time.</summary>
public enum TimeShape
{
    /// <summary>Something that happened at a time that is stated.</summary>
    Moment = 0,

    /// <summary>Something that happened once, somewhere in a window: the window is what is known, not how long it took.</summary>
    Window = 1,

    /// <summary>Something that lasted.</summary>
    Lasting = 2,
}

/// <summary>What is known of the beginning or the end of something that lasted.</summary>
public enum TimeEdge
{
    /// <summary>Stated.</summary>
    Known = 0,

    /// <summary>Known to lie between two times.</summary>
    Between = 1,

    /// <summary>Still going on: an end that has not come.</summary>
    Ongoing = 2,

    /// <summary>It ended, and nobody states when.</summary>
    EndedAtUnknownTime = 3,

    /// <summary>Nobody states it – which does not say that it is still going on.</summary>
    Unknown = 4,
}

/// <summary>A mark of a timeline: a moment, a window or something that lasted, at its places of an axis.</summary>
/// <param name="Key">The identifier of the statement.</param>
/// <param name="Label">What it is.</param>
/// <param name="Shape">What it says about its time.</param>
/// <param name="From">Where the mark begins on its axis: the earliest place the statement may reach.</param>
/// <param name="To">Where it ends: the latest place it may reach. On the axis of depth both are years before the present, the larger first.</param>
/// <param name="Time">The time as the statement says it.</param>
public sealed record TimeMark(string Key, LocalizedText Label, TimeShape Shape, decimal From, decimal To, TemporalExpression Time)
{
    /// <summary>Gets what is known of the beginning, for something that lasted.</summary>
    public TimeEdge Start { get; init; }

    /// <summary>Gets what is known of the end, for something that lasted.</summary>
    public TimeEdge End { get; init; }

    /// <summary>Gets where the beginning is surely over, when it is only known to lie between two times.</summary>
    public decimal? StartedBy { get; init; }

    /// <summary>Gets where the end may begin, when it is only known to lie between two times.</summary>
    public decimal? EndsFrom { get; init; }

    /// <summary>Gets a value indicating whether the time is less than exact: "about", radiocarbon years, or "ago" without a year it is counted from.</summary>
    public bool IsLoose { get; init; }

    /// <summary>Gets a value indicating whether the statement is disputed.</summary>
    public bool IsDisputed { get; init; }
}

/// <summary>A lane of a view: what one subject has on the axis of the view.</summary>
/// <param name="Key">The identifier of the lane.</param>
/// <param name="Label">The subject.</param>
/// <param name="Marks">Its marks, in the order of the axis.</param>
public sealed record TimeLane(string Key, LocalizedText Label, ImmutableArray<TimeMark> Marks);

/// <summary>One view of a timeline: an axis and what lies on it.</summary>
/// <param name="Axis">The axis.</param>
/// <param name="From">Where the view begins.</param>
/// <param name="To">Where it ends.</param>
/// <param name="Lanes">The lanes that have something on the axis, in the order of the timeline.</param>
public sealed record TimeView(TimeAxis Axis, decimal From, decimal To, ImmutableArray<TimeLane> Lanes)
{
    /// <summary>Gets the telling whose order the axis is, for <see cref="TimeAxis.Order"/>.</summary>
    public string? Sequence { get; init; }

    /// <summary>Gets the year of the common era a depth of time is counted from, for <see cref="TimeAxis.Depth"/>.</summary>
    public int? Present { get; init; }
}

/// <summary>A timeline as views (ADR 0021): one for each axis something lies on, and what lies on none.</summary>
/// <param name="Views">The views: the calendar, the depth of time, and the order of each telling.</param>
/// <param name="Unplaced">What has a time that has no place on any axis: named, not dropped.</param>
public sealed record Timeline(ImmutableArray<TimeView> Views, ImmutableArray<UnplacedItem> Unplaced);

/// <summary>Makes the views of a timeline from what a comparison – or the timeline of a collection – states.</summary>
public static class TimelineViews
{
    /// <summary>
    /// Returns the views of a comparison. Every statement goes to the axis its time is counted on: a date of the
    /// calendar to the calendar, a depth of time to the depth of time, a place in a telling to the order of that
    /// telling. A window in which something happened once is a window; something that lasted is a stretch with
    /// what is known of its two ends; nothing is drawn at a middle that nobody stated.
    /// </summary>
    /// <param name="comparison">The comparison.</param>
    /// <param name="present">The year of the common era a depth of time is counted from when it does not name one.</param>
    public static Timeline Of(Comparison comparison, int present)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        var unplaced = ImmutableArray.CreateBuilder<UnplacedItem>();
        var placed = new List<(TimeAxis Axis, string? Sequence, ComparisonLane Lane, TimeMark Mark)>();
        foreach (var lane in comparison.Lanes)
        {
            foreach (var period in lane.Periods)
            {
                Add(lane, period.Key, period.Label, period.Time, period.Review);
            }

            foreach (var single in lane.Events)
            {
                Add(lane, single.Key, single.Label, single.Time, single.Review);
            }
        }

        var views = ImmutableArray.CreateBuilder<TimeView>();
        foreach (var view in placed.GroupBy(item => (item.Axis, item.Sequence)).OrderBy(group => group.Key.Axis).ThenBy(group => group.Key.Sequence, StringComparer.Ordinal))
        {
            var older = view.Key.Axis == TimeAxis.Depth;
            var lanes = comparison.Lanes
                .Select(lane => new TimeLane(
                    lane.Key,
                    lane.Label,
                    [.. view.Where(item => ReferenceEquals(item.Lane, lane)).Select(item => item.Mark).OrderBy(mark => older ? -mark.From : mark.From).ThenBy(mark => mark.Key, StringComparer.Ordinal)]))
                .Where(lane => !lane.Marks.IsEmpty)
                .ToImmutableArray();

            // The calendar of a comparison is as long as the comparison says; every other view reaches as far as what lies on it.
            var all = lanes.SelectMany(lane => lane.Marks).ToList();
            var (from, to) = view.Key.Axis switch
            {
                TimeAxis.Calendar when YearAxis.Of(comparison.From) is { } first && YearAxis.Of(comparison.To) is { } last =>
                    (Math.Min(first.From, all.Min(mark => mark.From)), Math.Max(last.To, all.Max(mark => mark.To))),
                TimeAxis.Depth => (all.Max(mark => mark.From), all.Min(mark => mark.To)),
                _ => (all.Min(mark => mark.From), all.Max(mark => mark.To)),
            };
            views.Add(new TimeView(view.Key.Axis, from, to, lanes) { Sequence = view.Key.Sequence, Present = older ? present : null });
        }

        return new(views.ToImmutable(), unplaced.ToImmutable());

        void Add(ComparisonLane lane, string key, LocalizedText label, TemporalExpression time, ReviewStatus review)
        {
            if (Mark(key, label, time, present) is { } found)
            {
                placed.Add((found.Axis, found.Sequence, lane, found.Mark with { IsDisputed = review == ReviewStatus.Disputed }));
            }
            else
            {
                unplaced.Add(new UnplacedItem(lane, key, label));
            }
        }
    }

    // The mark of a statement and the axis it lies on, or nothing when its time has no place on one axis.
    private static (TimeAxis Axis, string? Sequence, TimeMark Mark)? Mark(string key, LocalizedText label, TemporalExpression time, int present)
    {
        switch (time)
        {
            case Occurrence occurrence when Place(occurrence.At, present) is { } at:
                return (at.Axis, at.Sequence, new TimeMark(key, label, TimeShape.Moment, at.From, at.To, time) { IsLoose = TimePlaces.IsLoose(occurrence.At) });

            case OccurrenceWindow window when Place(window.Earliest, present) is { } earliest && Place(window.Latest, present) is { } latest && Same(earliest, latest):
                // The same time twice is a moment; anything else is the window in which it happened.
                return (earliest.Axis, earliest.Sequence, new TimeMark(key, label, window.Earliest == window.Latest ? TimeShape.Moment : TimeShape.Window, earliest.From, latest.To, time)
                {
                    IsLoose = TimePlaces.IsLoose(window.Earliest) || TimePlaces.IsLoose(window.Latest),
                });

            case Period period:
                var startEarliest = period.Start.Earliest is { } s1 ? Place(s1, present) : null;
                var startLatest = period.Start.Latest is { } s2 ? Place(s2, present) : null;
                var endEarliest = period.End.Earliest is { } e1 ? Place(e1, present) : null;
                var endLatest = period.End.Latest is { } e2 ? Place(e2, present) : null;
                var known = new[] { startEarliest, startLatest, endEarliest, endLatest }.Where(place => place is not null).Select(place => place!.Value).ToList();

                // Something of it has to be stated, all of it on one axis.
                if (known.Count == 0 || known.Any(place => !Same(place, known[0])))
                {
                    return null;
                }

                // A side that states nothing stands where the other side does: the mark is then as short as what is known, and says which side is open.
                var from = startEarliest?.From ?? endEarliest!.Value.From;
                var to = endLatest?.To ?? startLatest!.Value.To;
                var start = period.Start.State switch
                {
                    PeriodStartState.Known => TimeEdge.Known,
                    PeriodStartState.Between => TimeEdge.Between,
                    _ => TimeEdge.Unknown,
                };
                var end = period.End.State switch
                {
                    PeriodEndState.Known => TimeEdge.Known,
                    PeriodEndState.Between => TimeEdge.Between,
                    PeriodEndState.Ongoing => TimeEdge.Ongoing,
                    PeriodEndState.EndedAtUnknownTime => TimeEdge.EndedAtUnknownTime,
                    _ => TimeEdge.Unknown,
                };
                return (known[0].Axis, known[0].Sequence, new TimeMark(key, label, TimeShape.Lasting, from, to, time)
                {
                    Start = start,
                    End = end,
                    StartedBy = start == TimeEdge.Between ? startLatest!.Value.To : null,
                    EndsFrom = end == TimeEdge.Between ? endEarliest!.Value.From : null,
                    IsLoose = new[] { period.Start.Earliest, period.Start.Latest, period.End.Earliest, period.End.Latest }.Any(TimePlaces.IsLoose),
                });

            default:
                return null;
        }
    }

    private static bool Same((TimeAxis Axis, string? Sequence, decimal From, decimal To) one, (TimeAxis Axis, string? Sequence, decimal From, decimal To) other) =>
        one.Axis == other.Axis && one.Sequence == other.Sequence;

    // Where a position lies, on the axis it is counted on. On the axis of depth a place is a number of years before the present, and the earlier end is the larger.
    private static (TimeAxis Axis, string? Sequence, decimal From, decimal To)? Place(TimePosition position, int present)
    {
        switch (position)
        {
            case CalendarPosition when YearAxis.Of(position) is { } span:
                return (TimeAxis.Calendar, null, span.From, span.To);
            case AgePosition age:
                var years = age.Years + (present - (age.Reference.CommonEraYear ?? present));
                return (TimeAxis.Depth, null, years, years);
            case NarrativePosition step:
                return (TimeAxis.Order, step.Order == NarrativeOrder.InWorld ? step.Sequence + " (in the world)" : step.Sequence, step.Ordinal, step.Ordinal);
            default:
                return null;
        }
    }
}
