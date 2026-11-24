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
    /// <param name="named">
    /// What a time of the record of the name is called in the language of the page (<see cref="Named"/>). Without
    /// it the lane holds the statements and the relations only – for a chart that lays the life of a name itself.
    /// </param>
    public static ComparisonLane Of(Entity subject, TimelineView view, Visitor visitor, string prefix = "", Func<Entity, RecordTime, LocalizedText>? named = null)
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

        // What the record of the name states of the name itself (D-098): the stretch it lived or lasted, or the
        // one time it was born, happened or was made.
        var record = named is null ? [] : RecordTimes.Of(subject);
        return new ComparisonLane
        {
            Key = prefix + subject.Id.Value,
            Label = subject.Label,
            Periods =
            [
                .. record.Where(time => time.Time is Period).Select(time => new ComparisonPeriod
                {
                    Key = prefix + time.Key,
                    Label = named!(subject, time),
                    Time = (Period)time.Time,
                    Sources = Cited(time.Sources),
                    Review = ReviewStatus.SourcedReviewPending,
                }),
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
                .. record.Where(time => time.Time is not Period).Select(time => new ComparisonEvent
                {
                    Key = prefix + time.Key,
                    Label = named!(subject, time),
                    Time = time.Time,
                    Sources = Cited(time.Sources),
                    Review = ReviewStatus.SourcedReviewPending,
                }),
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
    /// Returns what a time of the record of a name is called: "Life" for the years of a person, "From beginning to
    /// end" for those of anything else, and the name of the fact for one time – "Born", "When it happened".
    /// </summary>
    public static LocalizedText Named(Entity subject, RecordTime time, Visitor visitor, Microsoft.Extensions.Localization.IStringLocalizer<Vafadar.Dastan.Web.Resources.Ui> text)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        // When something happened is the whole of what such a fact says: its mark is named by the time itself,
        // not by "When" – the lane already says what happened.
        return LocalizedText.Of(visitor.Language.Tag, time.IsSpan
            ? text[subject.Type.Key == EntityTypes.Person.Key ? "Compare_Life_Person" : "Compare_Life_Other"].Value
            : time.From.Key == ProfileFacts.Happened.Key ? TimeText.Of(time.Time, visitor, text)
            : text[$"Fact_{time.From.Key}"].Value);
    }

    /// <summary>
    /// Returns where on the axis of years the earliest of what a lane of a name holds begins – its record, what
    /// is stated of it, what is said between it and another name – or nothing when none of it can be placed
    /// there. The chart of a collection lays its lanes in this order, so that it reads as the time went.
    /// </summary>
    public static decimal? Earliest(Entity subject, TimelineView view) => Reach(subject, view)?.From;

    /// <summary>
    /// Returns the stretch of the axis of years everything a lane of a name holds lies in, or nothing when none
    /// of it can be placed there.
    /// </summary>
    public static YearSpan? Reach(Entity subject, TimelineView view)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(view);
        var reaches = RecordTimes.Of(subject).Select(time => (TemporalExpression?)time.Time)
            .Concat(subject.Dated.Select(dated => dated.Time))
            .Concat(view.Held.Where(edge => edge.From.Id == subject.Id).Select(edge => edge.Relation.Time))
            .Select(time => YearCover.Reach(time)).OfType<YearSpan>().ToList();
        return reaches.Count == 0 ? null : new YearSpan(reaches.Min(reach => reach.From), reaches.Max(reach => reach.To));
    }

    /// <summary>
    /// Returns of the lane of a name what stands for the name where many names share a lane (D-099): the stretch
    /// it lived or lasted, where its record states one; otherwise the stretches that are stated of it; otherwise
    /// its single times. The rest is in the lane of the name itself, which the chart of every name and the page
    /// of the name draw.
    /// </summary>
    public static ComparisonLane Chief(Entity subject, ComparisonLane lane)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(lane);
        var own = subject.Id.Value + "~";
        var whole = lane.Periods.Where(period => period.Key.Contains(own, StringComparison.Ordinal)).ToImmutableArray();
        return new ComparisonLane
        {
            Key = lane.Key,
            Label = lane.Label,
            Periods = !whole.IsEmpty ? whole : lane.Periods,
            Events = whole.IsEmpty && lane.Periods.IsEmpty ? lane.Events : [],
        };
    }

    /// <summary>
    /// Returns one lane for several names (D-099): everything their own lanes hold, each bar and mark named by
    /// whose it is. The chart lays what does not meet in time into one row, so that a lane of forty people is a
    /// few rows and not forty lanes. The stretch a name lived or lasted and the time something happened are
    /// named by the name alone; everything else by the name and what is said.
    /// </summary>
    /// <param name="key">The key of the lane among the lanes of the chart.</param>
    /// <param name="label">What the lane is called: the kind of its names.</param>
    /// <param name="lanes">The names with the lane of each, in the order they are to stand.</param>
    /// <param name="visitor">Who asks: the language the names are said in.</param>
    public static ComparisonLane Shared(string key, LocalizedText label, IEnumerable<(Entity Subject, ComparisonLane Lane)> lanes, Visitor visitor)
    {
        ArgumentNullException.ThrowIfNull(lanes);
        ArgumentNullException.ThrowIfNull(visitor);
        var periods = ImmutableArray.CreateBuilder<ComparisonPeriod>();
        var events = ImmutableArray.CreateBuilder<ComparisonEvent>();
        foreach (var (subject, lane) in lanes)
        {
            var own = subject.Id.Value + "~";
            LocalizedText Of(string item, LocalizedText what, bool whole) => whole
                ? subject.Label
                : LocalizedText.Of(visitor.Language.Tag, $"{visitor.Text(subject.Label).Value} – {visitor.Text(what).Value}");
            periods.AddRange(lane.Periods.Select(period => new ComparisonPeriod
            {
                Key = period.Key,
                Label = Of(period.Key, period.Label, period.Key.Contains(own, StringComparison.Ordinal)),
                Summary = period.Summary,
                Time = period.Time,
                Sources = period.Sources,
                Review = period.Review,
            }));
            events.AddRange(lane.Events.Select(single => new ComparisonEvent
            {
                Key = single.Key,
                Label = Of(single.Key, single.Label, single.Key.EndsWith(own + ProfileFacts.Happened.Key, StringComparison.Ordinal)),
                Summary = single.Summary,
                Time = single.Time,
                Sources = single.Sources,
                Review = single.Review,
            }));
        }

        return new ComparisonLane { Key = key, Label = label, Periods = periods.ToImmutable(), Events = events.ToImmutable() };
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
