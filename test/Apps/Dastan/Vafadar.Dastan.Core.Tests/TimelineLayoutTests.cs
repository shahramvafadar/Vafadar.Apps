using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The layout of lanes of periods and single events over one axis of years. The lanes are those of the example the
/// chart was asked for – China, Iran and Rome from 600 BCE to 700 CE; the years are round enough for a test of a
/// layout and are not a statement about history.
/// </summary>
public sealed class TimelineLayoutTests
{
    // A year as a chart places it: in the middle of the stretch the year covers.
    private static decimal Bce(int year) => YearAxis.StartOf(Era.BeforeCommonEra, year) + 0.5m;

    private static decimal Ce(int year) => YearAxis.StartOf(Era.CommonEra, year) + 0.5m;

    private static TimelineLane China { get; } = new(
        "china",
        "China",
        [
            new TimelineBand("zhou", "Zhou", Bce(770), Bce(221)),
            new TimelineBand("qin", "Qin", Bce(221), Bce(206)),
            new TimelineBand("han", "Han empire", Bce(206), Ce(220)),
            new TimelineBand("division", "Division", Ce(220), Ce(581)),
            new TimelineBand("tang", "Tang empire", Ce(581), Ce(907)),
        ],
        [new TimelineMark("gan-ying", "97 – Gan Ying reaches the Persian Gulf", Ce(97))]);

    private static TimelineLane Iran { get; } = new(
        "iran",
        "Iran",
        [
            new TimelineBand("medes", "Medes", Bce(678), Bce(550)),
            new TimelineBand("achaemenid", "Achaemenid empire", Bce(550), Bce(330)),
            new TimelineBand("seleucid", "Seleucids", Bce(330), Bce(247)),
            new TimelineBand("parthian", "Parthian empire", Bce(247), Ce(224)),
            new TimelineBand("sasanian", "Sasanian empire", Ce(224), Ce(651)),
            new TimelineBand("rashidun", "Rashidun", Ce(651), Ce(661)),
        ],
        []);

    private static TimelineLane Rome { get; } = new(
        "rome",
        "Rome",
        [
            new TimelineBand("kingdom", "Roman kingdom", Bce(753), Bce(509)),
            new TimelineBand("republic", "Roman republic", Bce(509), Bce(27)),
            new TimelineBand("empire", "Roman empire", Bce(27), Ce(395)),
            new TimelineBand("east", "Eastern Roman empire", Ce(395), Ce(1453)),
            new TimelineBand("west", "Western Rome", Ce(395), Ce(476), EndIsApproximate: true),
        ],
        [new TimelineMark("trajan", "116 – Trajan reaches the Persian Gulf", Ce(116))]);

    private static TimelineFrame Frame(double unitsPerYear = 0.06, MarkStacking stacking = MarkStacking.Levels) => new()
    {
        From = YearAxis.StartOf(Era.BeforeCommonEra, 600),
        To = YearAxis.StartOf(Era.CommonEra, 700),
        UnitsPerYear = unitsPerYear,
        Stacking = stacking,
    };

    private static TimelineDrawing Drawing(double unitsPerYear = 0.06, MarkStacking stacking = MarkStacking.Levels) =>
        TimelineLayout.Of([China, Iran, Rome], Frame(unitsPerYear, stacking));

    private static PlacedBand Bar(TimelineDrawing drawing, string key) => drawing.Bands.Single(band => band.Band.Key == key);

    [Fact]
    public void The_timeline_is_as_long_as_its_years_at_its_scale()
    {
        // From the beginning of 600 BCE to the beginning of 700 CE are 1299 years: there is no year zero.
        Assert.Equal(1299 * 0.06, Drawing().Length, precision: 6);
        Assert.Equal(1299 * 0.5, Drawing(0.5).Length, precision: 6);
    }

    [Fact]
    public void Periods_that_follow_each_other_share_a_row_and_periods_at_the_same_time_get_rows_of_their_own()
    {
        var drawing = Drawing();

        // Iran: one period ends in the year the next begins – one row. Rome: east and west at the same time – two.
        Assert.Equal([(0, 1), (1, 1), (2, 2)], drawing.Lanes.Select(lane => (lane.FirstRow, lane.Rows)));
        Assert.Equal(4, drawing.Rows);
        Assert.All(drawing.Bands.Where(band => band.Lane == 1), band => Assert.Equal(1, band.Row));
        Assert.Equal(2, Bar(drawing, "empire").Row);
        Assert.Equal(2, Bar(drawing, "east").Row);
        Assert.Equal(3, Bar(drawing, "west").Row);
    }

    [Fact]
    public void No_two_bars_of_a_row_lie_on_each_other()
    {
        foreach (var scale in new[] { 0.02, 0.06, 0.5 })
        {
            foreach (var row in Drawing(scale).Bands.GroupBy(band => band.Row))
            {
                var bars = row.OrderBy(band => band.Start).ToList();
                for (var index = 1; index < bars.Count; index++)
                {
                    Assert.True(bars[index].Start >= bars[index - 1].End - 1e-9, $"{bars[index].Band.Key} lies on {bars[index - 1].Band.Key} at the scale {scale}.");
                }
            }
        }
    }

    [Fact]
    public void A_bar_stands_where_its_years_stand()
    {
        var drawing = Drawing();
        var achaemenid = Bar(drawing, "achaemenid");

        // 550 BCE is 50 years after the beginning of 600 BCE, and the bar begins in the middle of that year.
        Assert.Equal(50.5 * 0.06, achaemenid.Start, precision: 6);
        Assert.Equal(270.5 * 0.06, achaemenid.End, precision: 6);
        Assert.Equal(Bar(drawing, "seleucid").Start, achaemenid.End, precision: 6);
        Assert.False(achaemenid.BeginsBefore || achaemenid.EndsAfter);
    }

    [Fact]
    public void A_period_that_reaches_out_of_the_stretch_shown_is_cut_and_says_so()
    {
        var drawing = Drawing();
        var zhou = Bar(drawing, "zhou");
        var east = Bar(drawing, "east");

        Assert.Equal(0, zhou.Start);
        Assert.True(zhou.BeginsBefore);
        Assert.False(zhou.EndsAfter);
        Assert.Equal(drawing.Length, east.End, precision: 6);
        Assert.True(east.EndsAfter);

        // What ends before the stretch or begins after it is not drawn; its lane stays.
        var later = TimelineLayout.Of([China, Iran, Rome], Frame() with { From = Ce(1000), To = Ce(1200) });
        Assert.Equal(["east"], later.Bands.Select(band => band.Band.Key));
        Assert.Equal([(0, 1), (1, 1), (2, 1)], later.Lanes.Select(lane => (lane.FirstRow, lane.Rows)));
        Assert.Empty(later.Marks);
    }

    [Fact]
    public void A_bar_carries_what_it_has_room_for()
    {
        var drawing = Drawing();

        // Fifteen years at this scale are less than two letters wide; nearly five centuries hold a name and two
        // years; two centuries hold a long name without its years.
        Assert.Equal(BandText.Nothing, Bar(drawing, "qin").Text);
        Assert.Equal(BandText.NameAndYears, Bar(drawing, "parthian").Text);
        Assert.Equal(BandText.Name, Bar(drawing, "achaemenid").Text);

        // Opened further, the short one has room as well.
        Assert.Equal(BandText.NameAndYears, Bar(Drawing(0.8), "qin").Text);
    }

    [Fact]
    public void Callouts_that_would_lie_on_each_other_stand_on_levels_of_their_own()
    {
        var drawing = Drawing();
        var first = drawing.Marks.Single(mark => mark.Mark.Key == "gan-ying");
        var second = drawing.Marks.Single(mark => mark.Mark.Key == "trajan");

        Assert.Equal(2, drawing.Marks.Length);
        Assert.Equal((0, 2), (first.Lane, second.Lane));
        Assert.Equal(696.5 * 0.06, first.At, precision: 6);
        Assert.Equal(first.At, first.Start, precision: 6);
        Assert.Equal(0, first.Level);
        Assert.Equal(1, second.Level);
        Assert.Equal(2, drawing.Levels);

        // Far enough apart, both stand next to the axis.
        var open = Drawing(2.5);
        Assert.All(open.Marks, mark => Assert.Equal(0, mark.Level));
        Assert.Equal(1, open.Levels);
    }

    [Fact]
    public void Down_the_page_a_callout_moves_along_the_time_instead_of_onto_a_further_level()
    {
        var drawing = TimelineLayout.Of([China, Iran, Rome], Frame(0.06, MarkStacking.Push) with { CharacterLength = 0 });
        var first = drawing.Marks[0];
        var second = drawing.Marks[1];

        Assert.All(drawing.Marks, mark => Assert.Equal(0, mark.Level));
        Assert.Equal(1, drawing.Levels);
        Assert.Equal(first.At, first.Start, precision: 6);
        Assert.True(second.Start >= first.Start + first.Length + 0.6 - 1e-9);
        Assert.True(second.Start > second.At);
    }

    [Fact]
    public void A_callout_at_the_end_of_the_timeline_stays_inside_it()
    {
        var late = new TimelineLane("late", "Late", [], [new TimelineMark("end", "An event in the last year shown", Ce(699))]);

        var drawing = TimelineLayout.Of([late], Frame());
        var mark = Assert.Single(drawing.Marks);

        Assert.True(mark.Start + mark.Length <= drawing.Length + 1e-9);
        Assert.True(mark.Start < mark.At);
        Assert.Equal(1, drawing.Rows);
        Assert.Empty(drawing.Bands);
    }

    [Fact]
    public void The_axis_is_marked_at_round_years_of_each_era_and_where_the_common_era_begins()
    {
        var ticks = Drawing().Ticks;

        Assert.Equal(
            [
                (Era.BeforeCommonEra, 600), (Era.BeforeCommonEra, 500), (Era.BeforeCommonEra, 400), (Era.BeforeCommonEra, 300),
                (Era.BeforeCommonEra, 200), (Era.BeforeCommonEra, 100), (Era.CommonEra, 1), (Era.CommonEra, 100),
                (Era.CommonEra, 200), (Era.CommonEra, 300), (Era.CommonEra, 400), (Era.CommonEra, 500),
                (Era.CommonEra, 600), (Era.CommonEra, 700),
            ],
            ticks.Select(tick => (tick.Era, tick.Year)));
        Assert.Equal(0, ticks[0].At, precision: 6);
        Assert.Single(ticks, tick => tick.IsEraStart);

        // A century before the era and a century of it are equally long; across the era one year is missing.
        Assert.Equal(100 * 0.06, ticks[1].At - ticks[0].At, precision: 6);
        Assert.Equal(100 * 0.06, ticks[8].At - ticks[7].At, precision: 6);
        Assert.Equal(199 * 0.06, ticks[7].At - ticks[5].At, precision: 6);
    }

    [Theory]
    [InlineData(0.06, 100)]
    [InlineData(0.012, 500)]
    [InlineData(0.6, 10)]
    [InlineData(6.0, 1)]
    [InlineData(0.000001, 1000000)]
    public void The_marks_of_the_axis_are_as_far_apart_as_a_year_number_is_wide(double unitsPerYear, int step)
    {
        Assert.Equal(step, TimelineLayout.Step(unitsPerYear, 5.5));
    }

    [Fact]
    public void A_timeline_needs_a_stretch_of_time_and_a_scale()
    {
        Assert.Throws<ArgumentException>(() => TimelineLayout.Of([China], Frame() with { To = Bce(700) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => TimelineLayout.Of([China], Frame(0)));
    }
}
