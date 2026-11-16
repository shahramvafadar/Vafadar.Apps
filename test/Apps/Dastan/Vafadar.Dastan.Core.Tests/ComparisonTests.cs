using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// A comparison – lanes of periods and single events over one kind of time – and what a timeline draws of it. The
/// years are those of a test, not a statement about history.
/// </summary>
public sealed class ComparisonTests
{
    private static readonly PublicationState Draft = new(LocalDemoAllowed: true, PublicReleaseApproved: false);

    private static CalendarPosition Bce(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.BeforeCommonEra, year);

    private static CalendarPosition Ce(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, year);

    private static ComparisonPeriod Lasting(string key, PeriodStart start, PeriodEnd end, params string[] sources) => new()
    {
        Key = key,
        Label = LocalizedText.Of("en", key),
        Time = new Period(start, end),
        Sources = [.. sources.Select(source => new SourceId(source))],
        Review = ReviewStatus.Proposed,
    };

    private static ComparisonEvent Happening(string key, TemporalExpression time) => new()
    {
        Key = key,
        Label = LocalizedText.Of("en", key),
        Time = time,
        Sources = [],
        Review = ReviewStatus.Proposed,
    };

    private static ComparisonLane Lane(string key, ImmutableArray<ComparisonPeriod> periods, ImmutableArray<ComparisonEvent> events = default) => new()
    {
        Key = key,
        Label = LocalizedText.Of("en", key),
        Periods = periods,
        Events = events.IsDefault ? [] : events,
    };

    private static Comparison Of(ImmutableArray<ComparisonLane> lanes, TimePosition? from = null, TimePosition? to = null, ImmutableArray<Source> sources = default) =>
        Comparison.Create(
            new ComparisonId("two-realms"),
            LocalizedText.Of("en", "Two realms"),
            LocalizedText.Of("en", "Two realms side by side."),
            ComparisonFrame.CommonEraYears,
            from ?? Bce(600),
            to ?? Ce(700),
            lanes,
            sources,
            Draft);

    private static ComparisonChart Chart(params ComparisonLane[] lanes) => ComparisonCharts.Of(Of([.. lanes]), text => text.Resolve("en", "en").Value);

    [Fact]
    public void A_comparison_is_shown_to_the_audience_its_editors_released_it_to()
    {
        var comparison = Of([Lane("east", [])]);

        Assert.True(comparison.MayBeShownTo(Audience.LocalDemonstration));
        Assert.False(comparison.MayBeShownTo(Audience.Public));
    }

    [Fact]
    public void A_comparison_has_lanes_and_a_stretch_of_time_that_can_be_placed()
    {
        Assert.Throws<ArgumentException>(() => Of([]));
        Assert.Throws<ArgumentException>(() => Of([Lane("east", [])], from: Ce(700), to: Bce(600)));
        Assert.Throws<ArgumentException>(() => Of([Lane("east", [])], from: Ce(700), to: Ce(700)));

        // A year of another count has no place among the years of the common era: nobody stated how it maps.
        Assert.Throws<ArgumentException>(() => Of([Lane("east", [])], from: CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 100)));
    }

    [Fact]
    public void Every_key_of_a_comparison_is_used_once()
    {
        var period = Lasting("first", PeriodStart.At(Bce(500)), PeriodEnd.At(Bce(400)));

        Assert.Contains("\"east\"", Assert.Throws<ArgumentException>(() => Of([Lane("east", []), Lane("east", [])])).Message, StringComparison.Ordinal);
        Assert.Contains("\"first\"", Assert.Throws<ArgumentException>(() => Of([Lane("east", [period]), Lane("west", [period])])).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Of([Lane("east", [period], [Happening("first", new Occurrence(Bce(450)))])]));
        Assert.Throws<ArgumentException>(() => Of([Lane("east", [period], [Happening("east", new Occurrence(Bce(450)))])]));
    }

    [Fact]
    public void A_cited_source_is_one_of_the_comparison()
    {
        var source = new Source
        {
            Id = new SourceId("source-chronicle"),
            Title = "A chronicle",
            Url = new Uri("https://example.org/chronicle"),
            Kind = SourceKind.ScholarlyReference,
            AccessedOn = new DateOnly(2026, 11, 16),
            Level = new ReadingLevel(0),
        };
        var cited = Lasting("first", PeriodStart.At(Bce(500)), PeriodEnd.At(Bce(400)), "source-chronicle");

        Assert.Single(Of([Lane("east", [cited])], sources: [source]).Sources);
        Assert.Contains("source-chronicle", Assert.Throws<ArgumentException>(() => Of([Lane("east", [cited])])).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Of([Lane("east", [cited])], sources: [source, source]));
    }

    [Fact]
    public void A_time_that_is_stated_to_the_year_stands_in_the_middle_of_that_year()
    {
        var chart = Chart(Lane(
            "east",
            [Lasting("first", PeriodStart.At(Bce(550)), PeriodEnd.At(Bce(330)))],
            [Happening("battle", new Occurrence(Ce(97)))]));

        var band = Assert.Single(chart.Lanes[0].Bands);
        var mark = Assert.Single(chart.Lanes[0].Marks);

        // 550 BCE begins at −549 on the axis of years: there is no year zero.
        Assert.Equal((-548.5m, -328.5m), (band.From, band.To));
        Assert.False(band.StartIsApproximate || band.EndIsApproximate);
        Assert.Equal(97.5m, mark.At);
        Assert.False(mark.IsApproximate);
        Assert.Equal(("first", "first", "battle"), (band.Key, band.Label, mark.Label));
        Assert.Empty(chart.Unplaced);
    }

    [Fact]
    public void The_chart_reaches_from_the_beginning_of_its_first_year_to_the_end_of_its_last()
    {
        var chart = Chart(Lane("east", []));

        Assert.Equal((YearAxis.StartOf(Era.BeforeCommonEra, 600), YearAxis.StartOf(Era.CommonEra, 701)), (chart.From, chart.To));
        Assert.Equal(1300m, chart.To - chart.From);
    }

    [Fact]
    public void What_is_stated_as_about_or_as_between_two_years_is_drawn_as_approximate()
    {
        var chart = Chart(Lane(
            "east",
            [
                Lasting("about", PeriodStart.At(Bce(678).Approximately()), PeriodEnd.At(Bce(550))),
                Lasting("between", PeriodStart.At(Bce(330)), PeriodEnd.Between(Bce(247), Bce(141))),
            ],
            [Happening("window", new OccurrenceWindow(Ce(100), Ce(109)))]));

        var about = chart.Lanes[0].Bands[0];
        var between = chart.Lanes[0].Bands[1];
        var window = Assert.Single(chart.Lanes[0].Marks);

        Assert.True(about.StartIsApproximate);
        Assert.False(about.EndIsApproximate);
        Assert.False(between.StartIsApproximate);
        Assert.True(between.EndIsApproximate);

        // Between the beginning of 247 BCE and the end of 141 BCE; the middle is where the bar is drawn to.
        Assert.Equal((-246m + -139m) / 2m, between.To);
        Assert.Equal(105m, window.At);
        Assert.True(window.IsApproximate);
    }

    [Fact]
    public void What_has_no_place_on_the_axis_is_not_drawn_and_is_named()
    {
        var chart = Chart(Lane(
            "east",
            [
                Lasting("no-start", PeriodStart.Unknown, PeriodEnd.At(Bce(400))),
                Lasting("goes-on", PeriodStart.At(Ce(600)), PeriodEnd.Ongoing),
                Lasting("ended-sometime", PeriodStart.At(Ce(600)), PeriodEnd.EndedAtUnknownTime),
                Lasting("other-count", PeriodStart.At(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 10)), PeriodEnd.At(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 20))),
                Lasting("drawn", PeriodStart.At(Bce(500)), PeriodEnd.At(Bce(400))),
            ],
            [Happening("undated", new UnknownTime()), Happening("in-the-story", new EventTime("event-battle"))]));

        Assert.Equal(["drawn"], chart.Lanes[0].Bands.Select(band => band.Key));
        Assert.Empty(chart.Lanes[0].Marks);
        Assert.Equal(
            ["no-start", "goes-on", "ended-sometime", "other-count", "undated", "in-the-story"],
            chart.Unplaced.Select(item => item.Key));
        Assert.All(chart.Unplaced, item => Assert.Equal("east", item.Lane.Key));
    }

    [Fact]
    public void A_period_that_ends_in_the_year_it_begins_in_has_no_length_to_draw()
    {
        var chart = Chart(Lane("east", [Lasting("a-year", PeriodStart.At(Ce(69)), PeriodEnd.At(Ce(69)))]));

        Assert.Empty(chart.Lanes[0].Bands);
        Assert.Equal(["a-year"], chart.Unplaced.Select(item => item.Key));
    }

    [Fact]
    public void The_page_chooses_the_words_of_a_callout_and_the_layout_measures_them()
    {
        var comparison = Of([Lane("east", [], [Happening("battle", new Occurrence(Ce(97)))])]);

        var plain = ComparisonCharts.Of(comparison, text => text.Resolve("en", "en").Value);
        var dated = ComparisonCharts.Of(comparison, text => text.Resolve("en", "en").Value, single => $"97 CE – {single.Key}");

        Assert.Equal("battle", plain.Lanes[0].Marks[0].Label);
        Assert.Equal("97 CE – battle", dated.Lanes[0].Marks[0].Label);

        var frame = new TimelineFrame { From = dated.From, To = dated.To, UnitsPerYear = 0.06 };
        Assert.True(TimelineLayout.Of(dated.Lanes, frame).Marks[0].Length > TimelineLayout.Of(plain.Lanes, frame).Marks[0].Length);
    }

    [Fact]
    public void A_callout_whose_words_run_across_the_time_has_the_length_the_page_gives_it()
    {
        var chart = Chart(Lane(
            "east",
            [Lasting("long-name-of-a-period", PeriodStart.At(Bce(500)), PeriodEnd.At(Ce(500)))],
            [Happening("first", new Occurrence(Ce(97))), Happening("second", new Occurrence(Ce(116)))]));

        var drawing = TimelineLayout.Of(chart.Lanes, new TimelineFrame
        {
            From = chart.From,
            To = chart.To,
            UnitsPerYear = 0.06,
            Stacking = MarkStacking.Push,
            MarkLength = 6.6,
        });

        // The names of the bars still run along the time and are measured; the callouts are all equally long.
        Assert.Equal(BandText.NameAndYears, Assert.Single(drawing.Bands).Text);
        Assert.All(drawing.Marks, mark => Assert.Equal(6.6, mark.Length));
        Assert.True(drawing.Marks[1].Start >= drawing.Marks[0].Start + 6.6 + 0.6 - 1e-9);
    }
}
