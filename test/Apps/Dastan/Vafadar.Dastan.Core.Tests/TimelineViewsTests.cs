using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// A timeline as views (step 19.9, ADR 0021): one view for each axis something is counted on – the calendar, a
/// depth of time, the order of a telling – with a window as a window, something that lasted as a stretch with
/// what is known of its ends, and nothing put at a middle nobody stated. The times are those of a test.
/// </summary>
public sealed class TimelineViewsTests
{
    private const int Present = 2026;

    private static CalendarPosition Bce(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, year);

    private static CalendarPosition Ce(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, year);

    private static AgePosition Ma(decimal value) => AgePosition.Of(value, AgeUnit.MillionYears, AgeReference.Year(Present));

    private static AgePosition Ka(decimal value, AgeReference? reference = null) => AgePosition.Of(value, AgeUnit.ThousandYears, reference ?? AgeReference.Year(Present));

    private static NarrativePosition Step(int ordinal, string telling = "story") => NarrativePosition.Of(telling, NarrativeOrder.Presentation, ordinal);

    private static ComparisonPeriod Lasting(string key, PeriodStart start, PeriodEnd end, ReviewStatus review = ReviewStatus.Proposed) => new()
    {
        Key = key, Label = LocalizedText.Of("en", key), Time = new Period(start, end), Sources = [], Review = review,
    };

    private static ComparisonEvent Happening(string key, TemporalExpression time) => new()
    {
        Key = key, Label = LocalizedText.Of("en", key), Time = time, Sources = [], Review = ReviewStatus.Proposed,
    };

    private static Timeline Of(params ComparisonLane[] lanes) => TimelineViews.Of(
        Comparison.Create(
            new ComparisonId("test"), LocalizedText.Of("en", "Test"), LocalizedText.Of("en", "A test."), ComparisonFrame.CommonEraYears,
            Bce(600), Ce(700), [.. lanes], [], new PublicationState(LocalDemoAllowed: true, PublicReleaseApproved: false)),
        Present);

    private static ComparisonLane Lane(string key, ImmutableArray<ComparisonPeriod> periods, ImmutableArray<ComparisonEvent> events = default) => new()
    {
        Key = key, Label = LocalizedText.Of("en", key), Periods = periods, Events = events.IsDefault ? [] : events,
    };

    private static TimeMark Only(Timeline timeline, TimeAxis axis) => Assert.Single(Assert.Single(Assert.Single(timeline.Views, view => view.Axis == axis).Lanes).Marks);

    [Fact]
    public void A_window_in_which_something_happened_is_a_window_and_not_a_point_at_its_middle()
    {
        var mark = Only(Of(Lane("east", [], [Happening("battle", new OccurrenceWindow(Bce(247), Bce(141)))])), TimeAxis.Calendar);

        Assert.Equal(TimeShape.Window, mark.Shape);
        Assert.Equal((YearAxis.StartOf(Era.BeforeCommonEra, 247), YearAxis.StartOf(Era.BeforeCommonEra, 141) + 1), (mark.From, mark.To));
        Assert.False(mark.IsLoose);

        // What happened at a stated time is a moment, as wide as the time is exact: a year, or a day of it.
        var year = Only(Of(Lane("east", [], [Happening("battle", new Occurrence(Ce(224)))])), TimeAxis.Calendar);
        Assert.Equal((TimeShape.Moment, 1m), (year.Shape, year.To - year.From));
        var day = Only(Of(Lane("east", [], [Happening("battle", new Occurrence(CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, 224, 4, 28)))])), TimeAxis.Calendar);
        Assert.True(day.To - day.From < 0.01m);
        Assert.True(Only(Of(Lane("east", [], [Happening("battle", new Occurrence(Ce(224).Approximately()))])), TimeAxis.Calendar).IsLoose);
    }

    [Fact]
    public void What_lasted_says_what_is_known_of_each_of_its_ends()
    {
        var known = Only(Of(Lane("east", [Lasting("reign", PeriodStart.At(Bce(550)), PeriodEnd.At(Bce(330)))])), TimeAxis.Calendar);
        Assert.Equal((TimeShape.Lasting, TimeEdge.Known, TimeEdge.Known), (known.Shape, known.Start, known.End));
        Assert.Equal((YearAxis.StartOf(Era.BeforeCommonEra, 550), YearAxis.StartOf(Era.BeforeCommonEra, 330) + 1), (known.From, known.To));
        Assert.Null(known.StartedBy);
        Assert.Null(known.EndsFrom);

        // A beginning between two times: the mark begins where it may have begun and says by when it had.
        var between = Only(Of(Lane("east", [Lasting("reign", PeriodStart.Between(Bce(247), Bce(238)), PeriodEnd.Between(Ce(224), Ce(226)))])), TimeAxis.Calendar);
        Assert.Equal((TimeEdge.Between, TimeEdge.Between), (between.Start, between.End));
        Assert.Equal(YearAxis.StartOf(Era.BeforeCommonEra, 247), between.From);
        Assert.Equal(YearAxis.StartOf(Era.BeforeCommonEra, 238) + 1, between.StartedBy);
        Assert.Equal(YearAxis.StartOf(Era.CommonEra, 224), between.EndsFrom);
        Assert.Equal(YearAxis.StartOf(Era.CommonEra, 226) + 1, between.To);
    }

    [Theory]
    [InlineData(TimeEdge.Ongoing)]
    [InlineData(TimeEdge.EndedAtUnknownTime)]
    [InlineData(TimeEdge.Unknown)]
    public void An_end_that_is_open_or_not_stated_is_said_to_be_so_and_is_not_drawn_as_an_end(TimeEdge end)
    {
        var state = end switch { TimeEdge.Ongoing => PeriodEnd.Ongoing, TimeEdge.EndedAtUnknownTime => PeriodEnd.EndedAtUnknownTime, _ => PeriodEnd.Unknown };
        var mark = Only(Of(Lane("east", [Lasting("reign", PeriodStart.At(Ce(600)), state)])), TimeAxis.Calendar);

        // The mark reaches as far as what is known – the year it began in – and says how its end stands.
        Assert.Equal((TimeEdge.Known, end), (mark.Start, mark.End));
        Assert.Equal((YearAxis.StartOf(Era.CommonEra, 600), YearAxis.StartOf(Era.CommonEra, 600) + 1), (mark.From, mark.To));
    }

    [Fact]
    public void A_beginning_nobody_states_is_open_at_that_side_and_a_period_nobody_states_anything_of_has_no_place()
    {
        var mark = Only(Of(Lane("east", [Lasting("reign", PeriodStart.Unknown, PeriodEnd.At(Ce(651)))])), TimeAxis.Calendar);
        Assert.Equal((TimeEdge.Unknown, TimeEdge.Known), (mark.Start, mark.End));
        Assert.Equal((YearAxis.StartOf(Era.CommonEra, 651), YearAxis.StartOf(Era.CommonEra, 651) + 1), (mark.From, mark.To));

        var nothing = Of(Lane("east", [Lasting("reign", PeriodStart.Unknown, PeriodEnd.Unknown)]));
        Assert.Empty(nothing.Views);
        Assert.Equal("reign", Assert.Single(nothing.Unplaced).Key);
    }

    [Fact]
    public void A_depth_of_time_has_a_view_of_its_own_counted_in_years_before_the_present()
    {
        var timeline = Of(
            Lane("earth", [Lasting("ice", PeriodStart.At(Ma(2.6m)), PeriodEnd.At(Ka(11.7m)))], [
                Happening("rock", new Occurrence(Ma(55))),
                Happening("tools", new OccurrenceWindow(Ka(20), Ka(10))),
                Happening("fire", new Occurrence(Ka(12, AgeReference.BeforePresent).Approximately())),
                Happening("sea", new Occurrence(AgePosition.Of(5.3m, AgeUnit.MillionYears, AgeReference.Unspecified))),
            ]),
            Lane("east", [Lasting("reign", PeriodStart.At(Bce(550)), PeriodEnd.At(Bce(330)))]));

        Assert.Equal([TimeAxis.Calendar, TimeAxis.Depth], timeline.Views.Select(view => view.Axis));
        Assert.Empty(timeline.Unplaced);
        var depth = timeline.Views[1];
        Assert.Equal(Present, depth.Present);
        Assert.Equal((55_000_000m, 10_000m), (depth.From, depth.To));

        // The earliest first: 55 million, 5.3 million, the ice, the tools, the fire.
        var marks = Assert.Single(depth.Lanes).Marks;
        Assert.Equal(["rock", "sea", "ice", "tools", "fire"], marks.Select(mark => mark.Key));
        Assert.Equal((TimeShape.Moment, 55_000_000m, 55_000_000m), (marks[0].Shape, marks[0].From, marks[0].To));
        Assert.Equal((TimeShape.Lasting, 2_600_000m, 11_700m), (marks[2].Shape, marks[2].From, marks[2].To));
        Assert.Equal((TimeShape.Window, 20_000m, 10_000m), (marks[3].Shape, marks[3].From, marks[3].To));

        // An age counted from 1950 is so many years more before the present; "about" and "ago" without a year are less than exact.
        Assert.Equal(12_000m + (Present - 1950), marks[4].From);
        Assert.True(marks[4].IsLoose);
        Assert.True(marks[1].IsLoose);
        Assert.False(marks[0].IsLoose);

        // The lane of the reign is on the calendar and not in the view of the depth.
        Assert.Equal("east", Assert.Single(timeline.Views[0].Lanes).Key);
    }

    [Fact]
    public void The_order_of_a_telling_has_a_view_for_each_telling_and_no_years()
    {
        var timeline = Of(Lane("hero", [], [
            Happening("born", new Occurrence(Step(1))),
            Happening("fights", new Occurrence(Step(7))),
            Happening("elsewhere", new Occurrence(Step(2, "other-story"))),
        ]));

        Assert.Equal([(TimeAxis.Order, "other-story"), (TimeAxis.Order, "story")], timeline.Views.Select(view => (view.Axis, view.Sequence!)));
        var story = timeline.Views[1];
        Assert.Equal((1m, 7m), (story.From, story.To));
        Assert.Equal(["born", "fights"], Assert.Single(story.Lanes).Marks.Select(mark => mark.Key));
        Assert.Null(story.Present);
    }

    [Fact]
    public void A_time_that_is_counted_on_two_axes_or_on_none_is_named_and_not_placed()
    {
        // A window or a period over two kinds of time cannot be said at all: the library of time refuses it.
        Assert.Throws<ArgumentException>(() => new OccurrenceWindow(Ka(12), Ce(100)));
        Assert.Throws<ArgumentException>(() => new Period(PeriodStart.At(Ma(1)), PeriodEnd.At(Ce(100))));

        var timeline = Of(Lane("east", [], [
            Happening("unknown", new UnknownTime()),
            Happening("other-era", new Occurrence(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 100))),
            Happening("placed", new Occurrence(Ce(100))),
        ]));

        Assert.Equal(["unknown", "other-era"], timeline.Unplaced.Select(item => item.Key));
        Assert.Equal("placed", Only(timeline, TimeAxis.Calendar).Key);
    }

    [Fact]
    public void The_calendar_of_a_comparison_is_as_long_as_it_says_and_longer_where_something_reaches_out_of_it()
    {
        var inside = Of(Lane("east", [Lasting("reign", PeriodStart.At(Bce(550)), PeriodEnd.At(Bce(330)))])).Views[0];
        Assert.Equal((YearAxis.StartOf(Era.BeforeCommonEra, 600), YearAxis.StartOf(Era.CommonEra, 700) + 1), (inside.From, inside.To));

        var outside = Of(Lane("east", [Lasting("reign", PeriodStart.At(Bce(12000)), PeriodEnd.At(Ce(1500)))])).Views[0];
        Assert.Equal((YearAxis.StartOf(Era.BeforeCommonEra, 12000), YearAxis.StartOf(Era.CommonEra, 1500) + 1), (outside.From, outside.To));
    }

    [Fact]
    public void A_disputed_statement_stays_disputed_on_its_mark()
    {
        var mark = Only(Of(Lane("east", [Lasting("reign", PeriodStart.At(Bce(550)), PeriodEnd.At(Bce(330)), ReviewStatus.Disputed)])), TimeAxis.Calendar);
        Assert.True(mark.IsDisputed);
    }
}
