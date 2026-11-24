using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>How sure the records are that something held in a year.</summary>
public enum Holding
{
    /// <summary>What is stated does not reach the year.</summary>
    No = 0,

    /// <summary>
    /// It may have held: the year lies where a beginning or an end is known only between two times, or after a
    /// beginning whose end nobody states, or the time is said to be about.
    /// </summary>
    Possibly = 1,

    /// <summary>It held: the year lies inside what is stated.</summary>
    Yes = 2,
}

/// <summary>A relation of the world that held, or may have held, in a year.</summary>
/// <param name="Edge">The relation with its two entities.</param>
/// <param name="Holding">How sure that is; never <see cref="Holding.No"/>.</param>
public sealed record HeldThen(Edge Edge, Holding Holding);

/// <summary>What is stated of an entity for a year.</summary>
/// <param name="Entity">The entity.</param>
/// <param name="Statement">The statement.</param>
/// <param name="Holding">How sure it is that the statement is about that year; never <see cref="Holding.No"/>.</param>
public sealed record StatedThen(Entity Entity, DatedStatement Statement, Holding Holding);

/// <summary>A year of the axis of the common era.</summary>
/// <param name="Era">The common era or the time before it.</param>
/// <param name="Number">The year, counted from 1.</param>
public readonly record struct YearOfEra(Era Era, int Number)
{
    /// <summary>Gets where the year begins on the axis of years.</summary>
    public decimal Place => YearAxis.StartOf(Era, Number);

    /// <summary>Returns the year a place of the axis lies in.</summary>
    public static YearOfEra At(decimal place)
    {
        var (era, year) = YearAxis.YearAt(place);
        return new(era, year);
    }
}

/// <summary>The years a collection says something about: the first, the last, and the years in which something begins or ends.</summary>
/// <param name="First">The first year anything is stated for.</param>
/// <param name="Last">The last year anything is stated for.</param>
/// <param name="Turns">The years in which something begins, ends or happens, in order.</param>
/// <summary>What a collection says of a time that was asked for: a year, a stretch of years or a depth of time.</summary>
/// <param name="Asked">The time.</param>
/// <param name="Relations">The relations of the world that held or may have held in it.</param>
/// <param name="Stated">What is stated of the entities for it.</param>
public sealed record HeldInTime(AskedTime Asked, ImmutableArray<HeldThen> Relations, ImmutableArray<StatedThen> Stated)
{
    /// <summary>Gets what the records of the entities state for it: who lived, what existed, what happened.</summary>
    public ImmutableArray<RecordThen> Records { get; init; } = [];

    /// <summary>Gets a value indicating whether nothing is said of the time.</summary>
    public bool IsEmpty => Relations.IsEmpty && Stated.IsEmpty && Records.IsEmpty;
}

public sealed record YearsCovered(YearOfEra First, YearOfEra Last, ImmutableArray<YearOfEra> Turns);

/// <summary>What a collection says of one year, as far as the visitor may see it.</summary>
/// <param name="Year">The year.</param>
/// <param name="Relations">The relations of the world that held or may have held then, in the order of the collection.</param>
/// <param name="Stated">What is stated of entities for that year.</param>
/// <param name="Earlier">The nearest earlier year in which something begins, ends or happens.</param>
/// <param name="Later">The nearest later year in which something begins, ends or happens.</param>
public sealed record MomentView(YearOfEra Year, ImmutableArray<HeldThen> Relations, ImmutableArray<StatedThen> Stated, YearOfEra? Earlier, YearOfEra? Later)
{
    /// <summary>Gets what the records of the entities state for the year: who lived, what existed, what happened.</summary>
    public ImmutableArray<RecordThen> Records { get; init; } = [];

    /// <summary>Gets a value indicating whether nothing is stated for the year.</summary>
    public bool IsEmpty => Relations.IsEmpty && Stated.IsEmpty && Records.IsEmpty;
}

/// <summary>
/// Whether a time reaches a year. A year is asked for, not a day: what is stated to the year holds in that whole
/// year, and what ended in a year still held in it.
/// </summary>
public static class YearCover
{
    /// <summary>Returns how sure it is that a time reaches the year.</summary>
    public static Holding Of(TemporalExpression? time, YearOfEra year) => Of(time, new YearSpan(year.Place, year.Place + 1), present: null);

    /// <summary>
    /// Returns how sure it is that a time reaches a stretch of the axis of years – a year, several, or a depth
    /// of time counted back from a present (ADR 0021).
    /// </summary>
    /// <param name="time">The time.</param>
    /// <param name="asked">The stretch that is asked for.</param>
    /// <param name="present">The year a depth of time is counted from; without it a depth of time reaches no year.</param>
    public static Holding Of(TemporalExpression? time, YearSpan asked, int? present)
    {
        var (from, to) = (asked.From, asked.To);
        var held = time switch
        {
            Occurrence occurrence => TimePlaces.Of(occurrence.At, present) is { } at && at.From < to && at.To > from ? Holding.Yes : Holding.No,
            OccurrenceWindow window => TimePlaces.Of(window.Earliest, present) is { } earliest && TimePlaces.Of(window.Latest, present) is { } latest && earliest.From < to && latest.To > from
                ? earliest.From >= from && latest.To <= to ? Holding.Yes : Holding.Possibly
                : Holding.No,
            Period period => Of(period, from, to, present),
            AlternativeOccurrences alternatives => alternatives.Options.Any(option => Of(option, asked, present) != Holding.No) ? Holding.Possibly : Holding.No,
            _ => Holding.No,
        };
        return held == Holding.Yes && About(time!) ? Holding.Possibly : held;
    }

    /// <summary>Returns the stretch of the axis of years a time may reach, when it can be placed there. An end nobody states is the end of what is known.</summary>
    public static YearSpan? Reach(TemporalExpression? time) => Reach(time, present: null);

    /// <summary>Returns the stretch of the axis of years a time may reach, a depth of time among it, counted from a present.</summary>
    /// <param name="time">The time.</param>
    /// <param name="present">The year a depth of time is counted from; without it a depth of time has no place.</param>
    public static YearSpan? Reach(TemporalExpression? time, int? present)
    {
        switch (time)
        {
            case Occurrence occurrence:
                return TimePlaces.Of(occurrence.At, present);
            case OccurrenceWindow window:
                return TimePlaces.Of(window.Earliest, present) is { } earliest && TimePlaces.Of(window.Latest, present) is { } latest ? new YearSpan(earliest.From, latest.To) : null;
            case AlternativeOccurrences alternatives:
                var options = alternatives.Options.Select(option => Reach(option, present)).OfType<YearSpan>().ToList();
                return options.Count == 0 ? null : new YearSpan(options.Min(option => option.From), options.Max(option => option.To));
            case Period period:
                var start = period.Start.Earliest is { } first ? TimePlaces.Of(first, present) : null;
                var end = period.End.Latest is { } last ? TimePlaces.Of(last, present) : null;
                return (start, end) switch
                {
                    ({ } from, { } to) => new YearSpan(from.From, to.To),
                    ({ } from, null) => period.Start.Latest is { } latestStart && TimePlaces.Of(latestStart, present) is { } until ? new YearSpan(from.From, until.To) : from,
                    (null, { } to) => period.End.Earliest is { } earliestEnd && TimePlaces.Of(earliestEnd, present) is { } since ? new YearSpan(since.From, to.To) : to,
                    _ => null,
                };
            default:
                return null;
        }
    }

    private static Holding Of(Period period, decimal from, decimal to, int? present)
    {
        // Where the beginning may lie. One that nobody knows may lie anywhere before the end.
        YearSpan? earliestStart = null, latestStart = null;
        if (period.Start.State != PeriodStartState.Unknown)
        {
            earliestStart = period.Start.Earliest is { } earliest ? TimePlaces.Of(earliest, present) : null;
            latestStart = period.Start.Latest is { } latest ? TimePlaces.Of(latest, present) : null;
            if (earliestStart is null || latestStart is null)
            {
                return Holding.No;
            }
        }

        // Where the end may lie. One that is stated has both its sides; one that nobody states may lie anywhere after the beginning.
        var stated = period.End.Earliest is not null && period.End.Latest is not null;
        YearSpan? earliestEnd = null, latestEnd = null;
        if (stated)
        {
            earliestEnd = TimePlaces.Of(period.End.Earliest!, present);
            latestEnd = TimePlaces.Of(period.End.Latest!, present);
            if (earliestEnd is null || latestEnd is null)
            {
                return Holding.No;
            }
        }

        if ((earliestStart is { } began && began.From >= to) || (latestEnd is { } ended && ended.To <= from))
        {
            return Holding.No;
        }

        if (earliestStart is null && !stated)
        {
            // Neither end is known: nothing says which years it reaches.
            return Holding.No;
        }

        var begun = latestStart is { } surely && surely.From < to;
        var lasting = period.End.State == PeriodEndState.Ongoing || (earliestEnd is { } still && still.To > from);
        if (!begun || !lasting)
        {
            return Holding.Possibly;
        }

        // A beginning or an end that is said to be about leaves the years around it open.
        var aboutStart = (TimePlaces.IsLoose(period.Start.Earliest) || TimePlaces.IsLoose(period.Start.Latest)) && latestStart!.Value.To > from;
        var aboutEnd = (TimePlaces.IsLoose(period.End.Earliest) || TimePlaces.IsLoose(period.End.Latest)) && earliestEnd is { } end && end.From < to;
        return aboutStart || aboutEnd ? Holding.Possibly : Holding.Yes;
    }

    private static bool About(TemporalExpression time) => time switch
    {
        Occurrence occurrence => TimePlaces.IsLoose(occurrence.At),
        OccurrenceWindow window => TimePlaces.IsLoose(window.Earliest) || TimePlaces.IsLoose(window.Latest),
        _ => false,
    };
}
