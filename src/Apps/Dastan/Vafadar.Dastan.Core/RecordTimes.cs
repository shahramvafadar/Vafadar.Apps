using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>
/// A time the record of a name states about the name itself, among its facts: the stretch from when it was born,
/// founded or began to when it died, was dissolved or ended, or the one time something happened or was made.
/// </summary>
/// <param name="Key">A key of the time among everything of its collection: the name, and the facts it is made of.</param>
/// <param name="From">The fact the time is stated by – for a stretch, the one of its beginning.</param>
/// <param name="To">The fact of the end of a stretch; nothing for one time.</param>
/// <param name="Time">The time: a period for a stretch, otherwise what the fact states.</param>
/// <param name="Sources">The sources the facts rest on.</param>
public sealed record RecordTime(string Key, FactKind From, FactKind? To, TemporalExpression Time, ImmutableArray<SourceId> Sources)
{
    /// <summary>Gets a value indicating whether the time is the stretch between two facts.</summary>
    public bool IsSpan => To is not null;
}

/// <summary>A time of the record of a name that reaches, or may reach, a year that was asked for.</summary>
/// <param name="Entity">The name.</param>
/// <param name="Time">The time its record states.</param>
/// <param name="Holding">How sure it is that the time reaches the year; never <see cref="Holding.No"/>.</param>
public sealed record RecordThen(Entity Entity, RecordTime Time, Holding Holding);

/// <summary>
/// The times the records of names state among their facts (D-098). A collection says when somebody lived or when a
/// battle was fought as a fact of the person or of the battle; the views of time read these beside the dated
/// statements and the relations with a time, so that a name is found among the years by what its record says.
/// </summary>
public static class RecordTimes
{
    /// <summary>Gets the facts a stretch begins and ends with, pair by pair.</summary>
    public static ImmutableArray<(FactKind Begins, FactKind Ends)> Stretches { get; } =
    [
        (ProfileFacts.Born, ProfileFacts.Died),
        (ProfileFacts.Founded, ProfileFacts.Dissolved),
        (ProfileFacts.Began, ProfileFacts.Ended),
    ];

    /// <summary>Gets the facts that state one time of their own.</summary>
    public static ImmutableArray<FactKind> Single { get; } = [ProfileFacts.Happened, ProfileFacts.Created];

    /// <summary>
    /// Returns the times the record of a name states, as the visitor may be told them. A stretch is given only
    /// where both its ends are stated with a time that can be placed: somebody of whom only the birth is known is
    /// not said to have lived in every later year – the birth is given as one time then. A time that is unknown
    /// or does not apply gives nothing.
    /// </summary>
    /// <param name="entity">The name, as a projection gives it: only facts the visitor may be told.</param>
    public static ImmutableArray<RecordTime> Of(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Facts.IsDefaultOrEmpty)
        {
            return [];
        }

        var times = ImmutableArray.CreateBuilder<RecordTime>();
        foreach (var (begins, ends) in Stretches)
        {
            var (first, last) = (Placed(entity, begins), Placed(entity, ends));
            if (first is not null && last is not null)
            {
                var period = new Period(
                    first.Time is OccurrenceWindow began ? PeriodStart.Between(began.Earliest, began.Latest) : PeriodStart.At(((Occurrence)first.Time!).At),
                    last.Time is OccurrenceWindow ended ? PeriodEnd.Between(ended.Earliest, ended.Latest) : PeriodEnd.At(((Occurrence)last.Time!).At));
                times.Add(new($"{entity.Id.Value}~{begins.Key}~{ends.Key}", begins, ends, period, RestsOn(entity, [.. first.Sources.Concat(last.Sources).Distinct()])));
            }
            else if ((first ?? last) is { } one)
            {
                times.Add(new($"{entity.Id.Value}~{one.Kind.Key}", one.Kind, null, one.Time!, RestsOn(entity, one.Sources)));
            }
        }

        foreach (var kind in Single)
        {
            if (entity.Facts.FirstOrDefault(fact => fact.Kind.Key == kind.Key && YearCover.Reach(fact.Time) is not null) is { } fact)
            {
                times.Add(new($"{entity.Id.Value}~{kind.Key}", kind, null, fact.Time!, RestsOn(entity, fact.Sources)));
            }
        }

        return times.ToImmutable();
    }

    // The sources of a fact are optional (collection format): a fact that names none rests on the sources of its
    // record, which says it in the same breath – so the table of a chart names them and does not say "no source
    // recorded" beside a record that has one.
    private static ImmutableArray<SourceId> RestsOn(Entity entity, ImmutableArray<SourceId> sources) =>
        sources.IsDefaultOrEmpty ? entity.Sources : sources;

    /// <summary>Returns whether the record of a name states a time that can be placed among the years.</summary>
    public static bool Any(Entity entity) => !Of(entity).IsEmpty;

    // The first fact of a kind that states one time or a window of time: what a stretch can begin or end with.
    private static ProfileFact? Placed(Entity entity, FactKind kind) =>
        entity.Facts.FirstOrDefault(fact => fact.Kind.Key == kind.Key && fact.Time is Occurrence or OccurrenceWindow && YearCover.Reach(fact.Time) is not null);
}
