using System.Collections.Immutable;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The lanes of a chart of time made of names (step 9.4, D-092): one lane for a name, with what is stated about it
/// with a time and what is said between it and another name with a time. The chart of time of a collection and the
/// comparison a visitor makes of names of several collections build their lanes the same way. Nothing is added to
/// what the projection gives, and nothing is dated that states no date.
/// </summary>
public static class TimelineLanes
{
    /// <summary>Returns the lane of a name.</summary>
    /// <param name="subject">The name, as the projection of the visitor gives it in its chart of time.</param>
    /// <param name="view">The chart of time of the projection the name belongs to.</param>
    /// <param name="visitor">Who asks: the language a relation is said in.</param>
    /// <param name="prefix">
    /// What is put before every key of the lane and of the sources it cites, so that lanes of several collections
    /// stand in one chart without two keys meeting; empty for the chart of one collection.
    /// </param>
    public static ComparisonLane Of(Entity subject, TimelineView view, Visitor visitor, string prefix = "")
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(visitor);

        // What is said between this entity and another with a time: every relation its own bar or mark – what it
        // says and of whom, "Played for" and the club.
        var held = view.Held.Where(edge => edge.From.Id == subject.Id).ToList();
        LocalizedText Said(Edge edge) =>
            LocalizedText.Of(visitor.Language.Tag, $"{visitor.Text(edge.Relation.Label).Value} {visitor.Text(edge.To.Label).Value}");
        ImmutableArray<SourceId> Cited(ImmutableArray<SourceId> sources) =>
            prefix.Length == 0 ? sources : [.. sources.Select(source => new SourceId(prefix + source.Value))];

        return new ComparisonLane
        {
            Key = prefix + subject.Id.Value,
            Label = subject.Label,
            Periods =
            [
                .. subject.Dated.Where(dated => dated.Time is Period).Select(dated => new ComparisonPeriod
                {
                    Key = prefix + dated.Key,
                    Label = dated.Label,
                    Summary = dated.Summary,
                    Time = (Period)dated.Time!,
                    Sources = Cited(dated.Sources),
                    Review = dated.Review,
                }),
                .. held.Where(edge => edge.Relation.Time is Period).Select(edge => new ComparisonPeriod
                {
                    Key = prefix + edge.Relation.Id.Value,
                    Label = Said(edge),
                    Time = (Period)edge.Relation.Time!,
                    Sources = Cited(edge.Relation.Sources),
                    Review = edge.Relation.Review,
                }),
            ],
            Events =
            [
                .. subject.Dated.Where(dated => dated.Time is not null and not Period).Select(dated => new ComparisonEvent
                {
                    Key = prefix + dated.Key,
                    Label = dated.Label,
                    Summary = dated.Summary,
                    Time = dated.Time!,
                    Sources = Cited(dated.Sources),
                    Review = dated.Review,
                }),
                .. held.Where(edge => edge.Relation.Time is not Period).Select(edge => new ComparisonEvent
                {
                    Key = prefix + edge.Relation.Id.Value,
                    Label = Said(edge),
                    Time = edge.Relation.Time!,
                    Sources = Cited(edge.Relation.Sources),
                    Review = edge.Relation.Review,
                }),
            ],
        };
    }

    /// <summary>
    /// Returns whether a name states when it began or ended – born or died, founded or dissolved – with a time that
    /// can be placed: such a name has a life to lay beside others even when nothing else of it is dated.
    /// </summary>
    public static bool HasLifespan(Entity subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return Begins(subject) is not null || Ends(subject) is not null;
    }

    /// <summary>
    /// Returns the life of a name as a period – from when it was born or founded to when it died or was dissolved,
    /// as its record states them, with the uncertainty they are stated with – or <see langword="null"/> when the
    /// record states neither. A side that is not stated stays unknown; nothing is guessed.
    /// </summary>
    /// <param name="subject">The name, as the projection of the visitor gives it: only facts the visitor may be told.</param>
    /// <param name="label">What the bar is called in the language of the page, "Life" or "From founding to end".</param>
    /// <param name="prefix">What is put before its key and its sources, as for <see cref="Of"/>.</param>
    public static ComparisonPeriod? Lifespan(Entity subject, LocalizedText label, string prefix = "")
    {
        ArgumentNullException.ThrowIfNull(subject);
        var (begins, ends) = (Begins(subject), Ends(subject));
        if (begins is null && ends is null)
        {
            return null;
        }

        var start = begins?.Time switch
        {
            Occurrence at => PeriodStart.At(at.At),
            OccurrenceWindow window => PeriodStart.Between(window.Earliest, window.Latest),
            _ => PeriodStart.Unknown,
        };
        var end = ends?.Time switch
        {
            Occurrence at => PeriodEnd.At(at.At),
            OccurrenceWindow window => PeriodEnd.Between(window.Earliest, window.Latest),
            _ => PeriodEnd.Unknown,
        };
        return new ComparisonPeriod
        {
            Key = $"{prefix}{subject.Id.Value}~life",
            Label = label,
            Time = new Period(start, end),
            Sources = [.. new[] { begins, ends }.OfType<ProfileFact>().SelectMany(fact => fact.Sources).Distinct().Select(source => new SourceId(prefix + source.Value))],
            Review = ReviewStatus.SourcedReviewPending,
        };
    }

    // The first stated beginning and end of a name that can be placed: a single time or a window of time.
    private static ProfileFact? Begins(Entity subject) => subject.Facts.FirstOrDefault(fact =>
        (fact.Kind.Key == ProfileFacts.Born.Key || fact.Kind.Key == ProfileFacts.Founded.Key) && fact.Time is Occurrence or OccurrenceWindow);

    private static ProfileFact? Ends(Entity subject) => subject.Facts.FirstOrDefault(fact =>
        (fact.Kind.Key == ProfileFacts.Died.Key || fact.Kind.Key == ProfileFacts.Dissolved.Key) && fact.Time is Occurrence or OccurrenceWindow);

    /// <summary>Returns a source under the key a lane with that prefix cites it by.</summary>
    public static Source Renamed(Source source, string prefix)
    {
        ArgumentNullException.ThrowIfNull(source);
        return prefix.Length == 0 ? source : new Source
        {
            Id = new SourceId(prefix + source.Id.Value),
            Title = source.Title,
            Url = source.Url,
            Kind = source.Kind,
            AccessedOn = source.AccessedOn,
            PassageLocator = source.PassageLocator,
            ReuseNote = source.ReuseNote,
            Authors = source.Authors,
            PublishedOn = source.PublishedOn,
            UpdatedOn = source.UpdatedOn,
            Level = source.Level,
        };
    }

    /// <summary>
    /// Returns the chart of the lanes over the stretch of time they need: from the first to the last of what can be
    /// placed among years, with a little room at both ends. It is found by laying the lanes out once over any
    /// stretch. Lanes of which nothing can be placed get a stretch of one year; the table still says what is stated.
    /// </summary>
    public static Comparison Fitted(ComparisonId id, LocalizedText title, LocalizedText summary, ImmutableArray<ComparisonLane> lanes, ImmutableArray<Source> sources, Visitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        Comparison Over(decimal from, decimal to) => Comparison.Create(
            id, title, summary, ComparisonFrame.CommonEraYears, YearOf(from), YearOf(to), lanes, sources, new PublicationState(true, false));

        var chart = ComparisonCharts.Of(Over(1, 2), text => visitor.Text(text).Value);
        var places = chart.Lanes.SelectMany(lane => lane.Bands.SelectMany(band => new[] { band.From, band.To }).Concat(lane.Marks.Select(mark => mark.At))).ToList();
        if (places.Count == 0)
        {
            return Over(1, 2);
        }

        var room = Math.Max(10, Math.Ceiling((places.Max() - places.Min()) / 20));
        return Over(Math.Floor(places.Min()) - room, Math.Ceiling(places.Max()) + room);
    }

    private static TimePosition YearOf(decimal place)
    {
        var (era, year) = YearAxis.YearAt(place);
        return CalendarPosition.OfYear(CalendarSystem.Unspecified, era, year);
    }
}
