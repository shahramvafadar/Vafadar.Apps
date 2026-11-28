using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>Identifies a <see cref="Comparison"/>.</summary>
public readonly record struct ComparisonId
{
    /// <summary>Creates the identifier.</summary>
    public ComparisonId(string value) => Value = Identifier.Checked(value);

    /// <summary>Gets the stable text of the identifier.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// The kind of time the lanes of a comparison share. Lanes stand side by side only when they are counted in the same
/// way (document 04, TIME-006): the same number in two different counts is not the same time.
/// </summary>
public enum ComparisonFrame
{
    /// <summary>Years of the common era and of the time before it (<see cref="YearAxis"/>).</summary>
    CommonEraYears = 0,
}

/// <summary>A period of a lane: something that lasted, with the time it is stated to have lasted.</summary>
public sealed class ComparisonPeriod
{
    /// <summary>Gets what the period is called in addresses and by the page; unique in its comparison.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the name.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets a sentence about the period, when the editors wrote one.</summary>
    public LocalizedText? Summary { get; init; }

    /// <summary>Gets when it began and ended, as it is stated.</summary>
    public required Period Time { get; init; }

    /// <summary>Gets the sources that support the period and its time.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>Gets how far the editors have come with the statement.</summary>
    public required ReviewStatus Review { get; init; }

    /// <summary>Gets the kinds of the item (ADR 0029): a war, a drought, a reign. Empty while the comparison is not classified.</summary>
    public ImmutableArray<Kind> Kinds { get; init; } = [];
}

/// <summary>A single event of a lane, with the time it is stated to have happened.</summary>
public sealed class ComparisonEvent
{
    /// <summary>Gets what the event is called in addresses and by the page; unique in its comparison.</summary>
    public required string Key { get; init; }

    /// <summary>Gets what happened, in a few words.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets a sentence about the event, when the editors wrote one.</summary>
    public LocalizedText? Summary { get; init; }

    /// <summary>Gets when it happened, as it is stated.</summary>
    public required TemporalExpression Time { get; init; }

    /// <summary>Gets the sources that support the event and its time.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>Gets how far the editors have come with the statement.</summary>
    public required ReviewStatus Review { get; init; }

    /// <summary>Gets the kinds of the item (ADR 0029): a war, a drought, a reign. Empty while the comparison is not classified.</summary>
    public ImmutableArray<Kind> Kinds { get; init; } = [];
}

/// <summary>One of the subjects that are compared, with its periods and its single events.</summary>
public sealed class ComparisonLane
{
    /// <summary>Gets what the lane is called by the page; unique in its comparison.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the name of the subject.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets the periods, in the order of the editors.</summary>
    public required ImmutableArray<ComparisonPeriod> Periods { get; init; }

    /// <summary>Gets the single events, in the order of the editors.</summary>
    public required ImmutableArray<ComparisonEvent> Events { get; init; }
}

/// <summary>
/// Several subjects side by side over one kind of time: what lasted and what happened in each, so that one can see
/// what was at the same time. It states no connection between the lanes – standing side by side is not a relation.
/// </summary>
public sealed class Comparison
{
    private Comparison()
    {
    }

    /// <summary>Gets the identifier.</summary>
    public required ComparisonId Id { get; init; }

    /// <summary>Gets the title.</summary>
    public required LocalizedText Title { get; init; }

    /// <summary>Gets what the comparison is about, in a sentence or two.</summary>
    public required LocalizedText Summary { get; init; }

    /// <summary>Gets the kind of time the lanes share.</summary>
    public required ComparisonFrame Frame { get; init; }

    /// <summary>Gets the first position of the stretch of time the comparison is about.</summary>
    public required TimePosition From { get; init; }

    /// <summary>Gets the last position of that stretch.</summary>
    public required TimePosition To { get; init; }

    /// <summary>Gets the lanes, in the order they are shown.</summary>
    public required ImmutableArray<ComparisonLane> Lanes { get; init; }

    /// <summary>Gets the sources the statements of the comparison cite.</summary>
    public required ImmutableArray<Source> Sources { get; init; }

    /// <summary>Gets what the editors allow to be done with the comparison.</summary>
    public required PublicationState Publication { get; init; }

    /// <summary>Gets the kinds the periods and events of the comparison have, and the wider kinds above them, in the order of their list (ADR 0029).</summary>
    public ImmutableArray<Kind> Kinds => KindFilter.Offered(Lanes.SelectMany(lane => lane.Periods.Select(period => period.Kinds).Concat(lane.Events.Select(single => single.Kinds))));

    /// <summary>
    /// Returns the comparison with only the periods and events of the chosen kinds (ADR 0029); a lane left with
    /// nothing is left out. Nothing chosen, it is the comparison itself; nothing left, it is nothing.
    /// </summary>
    /// <param name="chosen">The keys of the kinds chosen; a wider kind takes in those below it.</param>
    public Comparison? Narrowed(IReadOnlySet<string> chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        if (chosen.Count == 0)
        {
            return this;
        }

        ImmutableArray<ComparisonLane> lanes =
        [
            .. Lanes
                .Select(lane => new ComparisonLane
                {
                    Key = lane.Key,
                    Label = lane.Label,
                    Periods = [.. lane.Periods.Where(period => KindFilter.Passes(period.Kinds, chosen))],
                    Events = [.. lane.Events.Where(single => KindFilter.Passes(single.Kinds, chosen))],
                })
                .Where(lane => !lane.Periods.IsEmpty || !lane.Events.IsEmpty),
        ];
        return lanes.IsEmpty ? null : new Comparison
        {
            Id = Id,
            Title = Title,
            Summary = Summary,
            Frame = Frame,
            From = From,
            To = To,
            Lanes = lanes,
            Sources = Sources,
            Publication = Publication,
        };
    }

    /// <summary>Returns whether the comparison may be shown to an audience.</summary>
    public bool MayBeShownTo(Audience audience) =>
        audience == Audience.LocalDemonstration ? Publication.LocalDemoAllowed : Publication.PublicReleaseApproved;

    /// <summary>
    /// Creates a comparison after checking that it holds together: lanes exist, every key is used once, every cited
    /// source is one of the comparison, and the stretch of time it is about can be placed in its frame.
    /// </summary>
    /// <exception cref="ArgumentException">The parts do not hold together; the message names the first problem.</exception>
    public static Comparison Create(
        ComparisonId id,
        LocalizedText title,
        LocalizedText summary,
        ComparisonFrame frame,
        TimePosition from,
        TimePosition to,
        ImmutableArray<ComparisonLane> lanes,
        ImmutableArray<Source> sources,
        PublicationState publication)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(publication);

        if (lanes.IsDefaultOrEmpty)
        {
            throw new ArgumentException("A comparison has at least one lane.", nameof(lanes));
        }

        if (YearAxis.Of(from) is not { } first || YearAxis.Of(to) is not { } last || last.From <= first.From)
        {
            throw new ArgumentException("The stretch of time of a comparison is stated in years of its frame and ends after it begins.", nameof(to));
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        var known = sources.IsDefault ? [] : sources.Select(source => source.Id).ToHashSet();
        if (!sources.IsDefault && known.Count != sources.Length)
        {
            throw new ArgumentException("A source is listed twice.", nameof(sources));
        }

        foreach (var lane in lanes)
        {
            Unique(Identifier.Checked(lane.Key), "lane");
            foreach (var period in lane.Periods)
            {
                Unique(Identifier.Checked(period.Key), "period");
                Cited(period.Sources, period.Key);
            }

            foreach (var single in lane.Events)
            {
                Unique(Identifier.Checked(single.Key), "event");
                Cited(single.Sources, single.Key);
            }
        }

        return new Comparison
        {
            Id = id,
            Title = title,
            Summary = summary,
            Frame = frame,
            From = from,
            To = to,
            Lanes = lanes,
            Sources = sources.IsDefault ? [] : sources,
            Publication = publication,
        };

        void Unique(string key, string kind)
        {
            if (!keys.Add(key))
            {
                throw new ArgumentException($"The key \"{key}\" of a {kind} is used twice.", nameof(lanes));
            }
        }

        void Cited(ImmutableArray<SourceId> cited, string key)
        {
            if (!cited.IsDefault && cited.FirstOrDefault(source => !known.Contains(source)) is { Value: not null } missing)
            {
                throw new ArgumentException($"\"{key}\" cites the source \"{missing}\", which the comparison does not have.", nameof(lanes));
            }
        }
    }
}

/// <summary>A period or an event of a comparison that has no place on its axis, with the reason in the record itself.</summary>
/// <param name="Lane">The lane.</param>
/// <param name="Key">The key of the period or the event.</param>
/// <param name="Label">Its name.</param>
public sealed record UnplacedItem(ComparisonLane Lane, string Key, LocalizedText Label);

/// <summary>A comparison as lanes for a timeline, with what could not be placed.</summary>
/// <param name="Lanes">The lanes with their periods and events as stretches and places on the axis of years.</param>
/// <param name="Unplaced">What has no place on the axis: an unknown beginning or end, a time of another kind.</param>
/// <param name="From">Where the stretch the comparison is about begins on the axis.</param>
/// <param name="To">Where it ends.</param>
public sealed record ComparisonChart(ImmutableArray<TimelineLane> Lanes, ImmutableArray<UnplacedItem> Unplaced, decimal From, decimal To);

/// <summary>Turns a comparison into what a timeline draws, without adding to what is stated.</summary>
public static class ComparisonCharts
{
    /// <summary>
    /// Places the periods and events of a comparison on the axis of years. A time that is stated to the year stands
    /// in the middle of that year; a time that is only known to lie between two positions stands in the middle
    /// between them and is marked as approximate. What has no known beginning or end, or is not counted in the
    /// years of the axis, is not drawn – it is returned as unplaced, so that the page can say so.
    /// </summary>
    /// <param name="comparison">The comparison.</param>
    /// <param name="text">Chooses the wording of a text for the page.</param>
    /// <param name="callout">
    /// Writes the words of the callout of an event – its year and its name, say. The layout measures them, so they
    /// are chosen here and not by the page that draws. Without it a callout carries the name of its event.
    /// </param>
    public static ComparisonChart Of(Comparison comparison, Func<LocalizedText, string> text, Func<ComparisonEvent, string>? callout = null)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(text);

        var unplaced = ImmutableArray.CreateBuilder<UnplacedItem>();
        var lanes = ImmutableArray.CreateBuilder<TimelineLane>(comparison.Lanes.Length);
        foreach (var lane in comparison.Lanes)
        {
            var bands = ImmutableArray.CreateBuilder<TimelineBand>();
            foreach (var period in lane.Periods)
            {
                if (Start(period.Time) is { } start && End(period.Time) is { } end && end.At > start.At)
                {
                    bands.Add(new TimelineBand(period.Key, text(period.Label), start.At, end.At, start.Approximate, end.Approximate) { IsDisputed = period.Review == ReviewStatus.Disputed });
                }
                else
                {
                    unplaced.Add(new UnplacedItem(lane, period.Key, period.Label));
                }
            }

            var marks = ImmutableArray.CreateBuilder<TimelineMark>();
            foreach (var single in lane.Events)
            {
                if (Moment(single.Time) is { } moment)
                {
                    // Of a window only the window is stated: its two ends go with the mark, and the chart draws them.
                    var window = single.Time is OccurrenceWindow { } between && between.Earliest != between.Latest
                        && YearAxis.Of(between.Earliest) is { } opens && YearAxis.Of(between.Latest) is { } closes
                        ? (From: opens.From, To: closes.To)
                        : ((decimal From, decimal To)?)null;
                    marks.Add(new TimelineMark(single.Key, callout?.Invoke(single) ?? text(single.Label), moment.At, moment.Approximate)
                    {
                        IsDisputed = single.Review == ReviewStatus.Disputed,
                        WindowFrom = window?.From,
                        WindowTo = window?.To,
                    });
                }
                else
                {
                    unplaced.Add(new UnplacedItem(lane, single.Key, single.Label));
                }
            }

            lanes.Add(new TimelineLane(lane.Key, text(lane.Label), bands.ToImmutable(), marks.ToImmutable()));
        }

        // The stretch runs from the beginning of its first year to the end of its last.
        return new ComparisonChart(lanes.MoveToImmutable(), unplaced.ToImmutable(), YearAxis.Of(comparison.From)!.Value.From, YearAxis.Of(comparison.To)!.Value.To);
    }

    private static (decimal At, bool Approximate)? Start(Period period) => period.Start.State switch
    {
        PeriodStartState.Unknown => null,
        _ => Between(period.Start.Earliest, period.Start.Latest),
    };

    private static (decimal At, bool Approximate)? End(Period period) => period.End.Earliest is null || period.End.Latest is null
        ? null
        : Between(period.End.Earliest, period.End.Latest);

    private static (decimal At, bool Approximate)? Moment(TemporalExpression time) => time switch
    {
        Occurrence occurrence => Between(occurrence.At, occurrence.At),
        OccurrenceWindow window => Between(window.Earliest, window.Latest),
        _ => null,
    };

    private static (decimal At, bool Approximate)? Between(TimePosition? earliest, TimePosition? latest)
    {
        if (earliest is null || latest is null || YearAxis.Of(earliest) is not { } first || YearAxis.Of(latest) is not { } last)
        {
            return null;
        }

        var approximate = !ReferenceEquals(earliest, latest) && earliest != latest
            || earliest is CalendarPosition { IsApproximate: true }
            || latest is CalendarPosition { IsApproximate: true };
        return ((first.From + last.To) / 2m, approximate);
    }
}
