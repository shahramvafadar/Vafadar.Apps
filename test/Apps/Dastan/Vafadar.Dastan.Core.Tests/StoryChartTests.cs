namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The part of the lanes of a story that a chart shows. The small story has three steps (arrival, gift, lineage) and
/// four names (hero, town, keepsake, child); the third step and the child belong to the third reading stage.
/// </summary>
public sealed class StoryChartTests
{
    private static LanesView Lanes(int level = 2) =>
        Projection.Of(new SmallStory().Build(), new ReadingLevel(level), Audience.LocalDemonstration)!.Lanes();

    private static IEnumerable<string> Names(StoryChart chart) => chart.Lanes.Select(lane => lane.Entity.Id.Value);

    private static IEnumerable<string> Steps(StoryChart chart) => chart.Steps.Select(step => step.Event.Id.Value);

    [Fact]
    public void Without_a_choice_the_chart_shows_every_step_and_every_lane_of_the_view()
    {
        var view = Lanes();
        var chart = StoryCharts.Of(view);

        Assert.Equal(view.Steps, chart.Steps);
        Assert.Equal(["hero", "town", "keepsake", "child"], Names(chart));
        Assert.Equal((1, 3, 3, 4), (chart.First, chart.Last, chart.StepCount, chart.LaneCount));
        Assert.All(chart.Lanes, lane => Assert.Equal(3, lane.Cells.Length));
    }

    [Fact]
    public void A_run_of_steps_keeps_the_cells_of_those_steps()
    {
        var chart = StoryCharts.Of(Lanes(), first: 2, last: 3);

        Assert.Equal(["gift", "lineage"], Steps(chart));
        Assert.Equal((2, 3, 3), (chart.First, chart.Last, chart.StepCount));

        // The hero acts in the gift and is only mentioned in the lineage; the child appears in the lineage alone.
        Assert.Equal([Presence.Acts, Presence.MentionedOnly], chart.Lanes[0].Cells.Select(cell => cell!.Presence));
        Assert.Equal([null, Presence.Acts], chart.Lanes[3].Cells.Select(cell => cell?.Presence));
    }

    [Fact]
    public void A_lane_stays_when_its_name_has_no_part_in_the_steps_shown()
    {
        var chart = StoryCharts.Of(Lanes(), first: 1, last: 1);

        // The keepsake and the child have nothing to do with the arrival; their rows stay, empty.
        Assert.Equal(["hero", "town", "keepsake", "child"], Names(chart));
        Assert.Equal([null], chart.Lanes[2].Cells);
        Assert.Equal([null], chart.Lanes[3].Cells);
    }

    [Fact]
    public void The_lanes_are_closed_from_the_last_one_and_one_always_stays()
    {
        Assert.Equal(["hero", "town"], Names(StoryCharts.Of(Lanes(), lanes: 2)));
        Assert.Equal(["hero"], Names(StoryCharts.Of(Lanes(), lanes: 0)));
        Assert.Equal(["hero"], Names(StoryCharts.Of(Lanes(), lanes: -3)));
        Assert.Equal(4, StoryCharts.Of(Lanes(), lanes: 99).Lanes.Length);
        Assert.Equal(4, StoryCharts.Of(Lanes(), lanes: 2).LaneCount);
    }

    [Theory]
    [InlineData(0, 2, 1, 2)]
    [InlineData(-5, null, 1, 3)]
    [InlineData(2, 9, 2, 3)]
    [InlineData(3, 1, 3, 3)]
    [InlineData(7, 8, 3, 3)]
    public void What_is_asked_for_outside_the_view_ends_where_the_view_ends(int first, int? last, int shownFirst, int shownLast)
    {
        var chart = StoryCharts.Of(Lanes(), first, last);

        Assert.Equal((shownFirst, shownLast), (chart.First, chart.Last));
        Assert.Equal(shownLast - shownFirst + 1, chart.Steps.Length);
    }

    [Fact]
    public void A_chart_never_holds_more_than_the_reading_position()
    {
        // At the opening the story has one step and two names – whatever the chart is asked for.
        var chart = StoryCharts.Of(Lanes(level: 0), first: 1, last: 3, lanes: 4);

        Assert.Equal(["arrival"], Steps(chart));
        Assert.Equal(["hero", "town"], Names(chart));
        Assert.Equal((1, 1, 1, 2), (chart.First, chart.Last, chart.StepCount, chart.LaneCount));
    }

    [Fact]
    public void A_story_without_steps_has_an_empty_chart()
    {
        var chart = StoryCharts.Of(new LanesView([], []));

        Assert.Empty(chart.Steps);
        Assert.Empty(chart.Lanes);
        Assert.Equal((0, 0, 0, 0), (chart.First, chart.Last, chart.StepCount, chart.LaneCount));
    }
}
