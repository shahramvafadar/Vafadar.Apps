using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>A period as a timeline draws it: a name and a stretch of the axis of years (<see cref="YearAxis"/>).</summary>
/// <param name="Key">What the period is, for the page that draws it; unique in the timeline.</param>
/// <param name="Label">The name written on the bar.</param>
/// <param name="From">Where the period begins on the axis.</param>
/// <param name="To">Where it ends.</param>
/// <param name="StartIsApproximate">Whether the beginning is stated as "about".</param>
/// <param name="EndIsApproximate">Whether the end is stated as "about".</param>
public sealed record TimelineBand(string Key, string Label, decimal From, decimal To, bool StartIsApproximate = false, bool EndIsApproximate = false)
{
    /// <summary>Gets whether the sources disagree about the statement; the chart says so.</summary>
    public bool IsDisputed { get; init; }
}

/// <summary>A single event as a timeline draws it: a name at one place of the axis of years.</summary>
/// <param name="Key">What the event is, for the page that draws it; unique in the timeline.</param>
/// <param name="Label">The words of its callout.</param>
/// <param name="At">Where it stands on the axis.</param>
/// <param name="IsApproximate">Whether its time is stated as "about".</param>
public sealed record TimelineMark(string Key, string Label, decimal At, bool IsApproximate = false)
{
    /// <summary>Gets whether the sources disagree about the statement; the chart says so.</summary>
    public bool IsDisputed { get; init; }
}

/// <summary>One lane of a timeline: a subject with its periods and its single events.</summary>
/// <param name="Key">What the lane is, for the page that draws it.</param>
/// <param name="Label">The name of the lane.</param>
/// <param name="Bands">The periods of the lane.</param>
/// <param name="Marks">The single events of the lane.</param>
public sealed record TimelineLane(string Key, string Label, ImmutableArray<TimelineBand> Bands, ImmutableArray<TimelineMark> Marks);

/// <summary>How the callouts of single events avoid each other.</summary>
public enum MarkStacking
{
    /// <summary>A callout that would lie on another one is put on a further level. For a timeline across the page.</summary>
    Levels = 0,

    /// <summary>A callout that would lie on another one is moved along the axis until it is free. For a timeline down the page.</summary>
    Push = 1,
}

/// <summary>The stretch of time a timeline shows, its scale and the measures of its texts.</summary>
public sealed record TimelineFrame
{
    /// <summary>Gets the place on the axis of years where the timeline begins.</summary>
    public required decimal From { get; init; }

    /// <summary>Gets the place where it ends.</summary>
    public required decimal To { get; init; }

    /// <summary>Gets the length of one year along the timeline, in the unit of the drawing (em).</summary>
    public required double UnitsPerYear { get; init; }

    /// <summary>Gets how the callouts of single events avoid each other.</summary>
    public MarkStacking Stacking { get; init; }

    /// <summary>Gets the length a character of a label takes along the timeline; zero when texts run across it.</summary>
    public double CharacterLength { get; init; } = 0.58;

    /// <summary>Gets the space a bar keeps free around its text, along the timeline.</summary>
    public double BandPadding { get; init; } = 1.4;

    /// <summary>
    /// Gets the length a callout takes along the timeline when its words run across it. When it is not set, a
    /// callout is as long as its words – or 3.2 when texts have no length along the timeline
    /// (<see cref="CharacterLength"/> is zero).
    /// </summary>
    public double? MarkLength { get; init; }

    /// <summary>Gets the space between two callouts along the timeline.</summary>
    public double MarkGap { get; init; } = 0.6;

    /// <summary>Gets the smallest distance between two marks of the axis.</summary>
    public double TickGap { get; init; } = 5.5;
}

/// <summary>What a bar has room for.</summary>
public enum BandText
{
    /// <summary>Its name with the years it begins and ends in.</summary>
    NameAndYears = 0,

    /// <summary>Its name.</summary>
    Name = 1,

    /// <summary>Nothing: the bar is too short. The name stands in the table of the timeline and in the title of the bar.</summary>
    Nothing = 2,
}

/// <summary>A mark of the axis: a year.</summary>
/// <param name="At">Its place along the timeline.</param>
/// <param name="Era">The era of the year.</param>
/// <param name="Year">The year, counted in its era.</param>
/// <param name="IsEraStart">Whether the common era begins here.</param>
public sealed record TimelineTick(double At, Era Era, int Year, bool IsEraStart);

/// <summary>A lane with the rows its periods need: periods that overlap stand in rows of their own.</summary>
/// <param name="Lane">The lane.</param>
/// <param name="FirstRow">The first of its rows, counted over the whole timeline.</param>
/// <param name="Rows">The number of its rows; at least one.</param>
public sealed record PlacedLane(TimelineLane Lane, int FirstRow, int Rows);

/// <summary>A period with its place.</summary>
/// <param name="Band">The period.</param>
/// <param name="Lane">The index of its lane.</param>
/// <param name="Row">Its row, counted over the whole timeline.</param>
/// <param name="Start">Where the bar begins along the timeline.</param>
/// <param name="End">Where it ends.</param>
/// <param name="BeginsBefore">Whether the period begins before the stretch that is shown.</param>
/// <param name="EndsAfter">Whether it ends after it.</param>
/// <param name="Text">What the bar has room for.</param>
public sealed record PlacedBand(TimelineBand Band, int Lane, int Row, double Start, double End, bool BeginsBefore, bool EndsAfter, BandText Text);

/// <summary>A single event with its place and the place of its callout.</summary>
/// <param name="Mark">The event.</param>
/// <param name="Lane">The index of its lane.</param>
/// <param name="At">Where the event stands along the timeline.</param>
/// <param name="Start">Where its callout begins along the timeline.</param>
/// <param name="Length">How long the callout is along the timeline.</param>
/// <param name="Level">The level of the callout; zero is the one next to the axis.</param>
public sealed record PlacedMark(TimelineMark Mark, int Lane, double At, double Start, double Length, int Level);

/// <summary>A timeline laid out: lengths along the time in the unit of the frame, rows and levels across it as counts.</summary>
/// <param name="Length">The length of the timeline.</param>
/// <param name="Ticks">The marks of the axis.</param>
/// <param name="Lanes">The lanes with their rows.</param>
/// <param name="Bands">The periods that lie in the stretch shown.</param>
/// <param name="Marks">The single events that lie in it.</param>
/// <param name="Rows">The number of rows of all lanes.</param>
/// <param name="Levels">The number of levels the callouts need.</param>
public sealed record TimelineDrawing(
    double Length,
    ImmutableArray<TimelineTick> Ticks,
    ImmutableArray<PlacedLane> Lanes,
    ImmutableArray<PlacedBand> Bands,
    ImmutableArray<PlacedMark> Marks,
    int Rows,
    int Levels);

/// <summary>
/// Lays out lanes of periods and single events over one axis of years. The layout knows nothing about a page: the
/// same result is drawn across the page or down it. It states nothing that was not stated – a period keeps its
/// ends, a bar that is cut by the stretch shown says so, and what does not fit is left out of the bar, never
/// shortened into something else.
/// </summary>
public static class TimelineLayout
{
    // Steps of years between two marks of the axis, from fine to coarse.
    private static readonly int[] Steps = [1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 5000, 10000, 100000, 1000000];

    // Two periods that meet in one year do not overlap: the year a reign ends is the year the next begins.
    private const decimal Touching = 0.001m;

    /// <summary>Lays out the lanes for a stretch of time and a scale.</summary>
    /// <param name="lanes">The lanes, in the order they are drawn.</param>
    /// <param name="frame">The stretch of time, the scale and the measures of the texts.</param>
    public static TimelineDrawing Of(IReadOnlyList<TimelineLane> lanes, TimelineFrame frame)
    {
        ArgumentNullException.ThrowIfNull(lanes);
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.To <= frame.From)
        {
            throw new ArgumentException("A timeline ends after it begins.", nameof(frame));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frame.UnitsPerYear, nameof(frame));

        var length = Place(frame.To, frame);
        var placedLanes = ImmutableArray.CreateBuilder<PlacedLane>(lanes.Count);
        var bands = ImmutableArray.CreateBuilder<PlacedBand>();
        var rows = 0;
        for (var index = 0; index < lanes.Count; index++)
        {
            var used = Pack(lanes[index], index, rows, frame, bands);
            placedLanes.Add(new PlacedLane(lanes[index], rows, used));
            rows += used;
        }

        var marks = Stack(lanes, frame, length, out var levels);
        return new TimelineDrawing(length, Ticks(frame), placedLanes.MoveToImmutable(), bands.ToImmutable(), marks, rows, levels);
    }

    /// <summary>Returns the step of years between two marks of the axis at a scale.</summary>
    /// <param name="unitsPerYear">The length of one year along the timeline.</param>
    /// <param name="gap">The smallest distance between two marks.</param>
    public static int Step(double unitsPerYear, double gap)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitsPerYear);
        var found = Array.Find(Steps, step => step * unitsPerYear >= gap);
        return found > 0 ? found : Steps[^1];
    }

    private static double Place(decimal year, TimelineFrame frame) => (double)(year - frame.From) * frame.UnitsPerYear;

    // The periods of a lane, each in the first row that is free where it begins.
    private static int Pack(TimelineLane lane, int laneIndex, int firstRow, TimelineFrame frame, ImmutableArray<PlacedBand>.Builder placed)
    {
        var ends = new List<decimal>();
        foreach (var band in lane.Bands
            .Where(band => band.To > frame.From && band.From < frame.To)
            .OrderBy(band => band.From)
            .ThenByDescending(band => band.To))
        {
            var row = ends.FindIndex(end => band.From >= end - Touching);
            if (row < 0)
            {
                row = ends.Count;
                ends.Add(band.To);
            }
            else
            {
                ends[row] = band.To;
            }

            var start = Place(Math.Max(band.From, frame.From), frame);
            var end = Place(Math.Min(band.To, frame.To), frame);
            placed.Add(new PlacedBand(band, laneIndex, firstRow + row, start, end, band.From < frame.From, band.To > frame.To, Room(band, end - start, frame)));
        }

        // A lane without a period in the stretch shown keeps its row: it is still one of the subjects compared.
        return Math.Max(1, ends.Count);
    }

    private static BandText Room(TimelineBand band, double length, TimelineFrame frame)
    {
        if (frame.CharacterLength <= 0)
        {
            // Texts run across the timeline: a bar has room for its name when it is as long as a line is high.
            return length >= 4.2 ? BandText.NameAndYears : length >= 1.6 ? BandText.Name : BandText.Nothing;
        }

        var name = (band.Label.Length * frame.CharacterLength) + frame.BandPadding;
        var years = 2 * ((5 * frame.CharacterLength) + 0.8);
        return length >= name + years ? BandText.NameAndYears : length >= name ? BandText.Name : BandText.Nothing;
    }

    // The callouts of the single events of all lanes, in the order of time.
    private static ImmutableArray<PlacedMark> Stack(IReadOnlyList<TimelineLane> lanes, TimelineFrame frame, double length, out int levels)
    {
        var ends = new List<double>();
        var placed = ImmutableArray.CreateBuilder<PlacedMark>();
        foreach (var (mark, lane) in lanes
            .SelectMany((lane, index) => lane.Marks.Select(mark => (Mark: mark, Lane: index)))
            .Where(entry => entry.Mark.At >= frame.From && entry.Mark.At <= frame.To)
            .OrderBy(entry => entry.Mark.At)
            .ThenBy(entry => entry.Lane))
        {
            var at = Place(mark.At, frame);
            var extent = frame.MarkLength ?? (frame.CharacterLength > 0 ? (mark.Label.Length * frame.CharacterLength) + frame.BandPadding : 3.2);

            // A callout begins at its event and never leaves the timeline.
            var start = Math.Max(0, Math.Min(at, length - extent));
            var level = 0;
            if (frame.Stacking == MarkStacking.Push)
            {
                if (ends.Count == 0)
                {
                    ends.Add(double.NegativeInfinity);
                }

                start = Math.Max(start, ends[0] + frame.MarkGap);
                ends[0] = start + extent;
            }
            else
            {
                level = ends.FindIndex(end => start >= end + frame.MarkGap);
                if (level < 0)
                {
                    level = ends.Count;
                    ends.Add(start + extent);
                }
                else
                {
                    ends[level] = start + extent;
                }
            }

            placed.Add(new PlacedMark(mark, lane, at, start, extent, level));
        }

        levels = frame.Stacking == MarkStacking.Push ? Math.Min(1, ends.Count) : ends.Count;
        return placed.ToImmutable();
    }

    // The marks of the axis: round years of each era, and the place where the common era begins. The years before
    // the era are counted backwards from it, so the marks on both sides of it are round years, not round places.
    private static ImmutableArray<TimelineTick> Ticks(TimelineFrame frame)
    {
        var step = Step(frame.UnitsPerYear, frame.TickGap);
        var eraStart = YearAxis.StartOf(Era.CommonEra, 1);
        var ticks = new List<TimelineTick>();

        for (var year = step; YearAxis.StartOf(Era.BeforeCommonEra, year) >= frame.From; year += step)
        {
            Add(YearAxis.StartOf(Era.BeforeCommonEra, year), Era.BeforeCommonEra, year);
        }

        if (eraStart >= frame.From && eraStart <= frame.To)
        {
            ticks.Add(new TimelineTick(Place(eraStart, frame), Era.CommonEra, 1, IsEraStart: true));
        }

        for (var year = step; year <= frame.To; year += step)
        {
            Add(YearAxis.StartOf(Era.CommonEra, year), Era.CommonEra, year);
        }

        return [.. ticks.OrderBy(tick => tick.At)];

        void Add(decimal place, Era era, int year)
        {
            // A round year next to the beginning of the era would stand on its mark.
            var distance = (double)Math.Abs(place - eraStart) * frame.UnitsPerYear;
            if (place >= frame.From && place <= frame.To && (distance >= frame.TickGap / 2 || eraStart < frame.From || eraStart > frame.To))
            {
                ticks.Add(new TimelineTick(Place(place, frame), era, year, IsEraStart: false));
            }
        }
    }
}
