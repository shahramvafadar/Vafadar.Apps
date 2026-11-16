namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The chart of a neighbourhood: the focus in the middle, what leads to it above, what it leads to below, what is
/// the same from both sides beside it – and every relation of the view exactly once. In the small story the hero
/// visits the town and is the parent of the child.
/// </summary>
public sealed class ConnectionsLayoutTests
{
    private static readonly EntityId Hero = new("hero");
    private static readonly EntityId Town = new("town");
    private static readonly EntityId Child = new("child");
    private static readonly EntityId Keepsake = new("keepsake");

    private static ConnectionsChart Draw(EntityId focus, SmallStory? story = null, int level = 2, params EntityId[] expanded)
    {
        var projection = Projection.Of((story ?? new SmallStory()).Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;
        return ConnectionsLayout.Of(projection.Connections(focus, expanded)!, text => text.Resolve("en").Value);
    }

    private static IEnumerable<(string Name, ChartWing Wing, int Depth)> Places(ConnectionsChart chart) =>
        chart.Boxes.Select(box => (box.Entity.Id.Value, box.Wing, box.Depth));

    [Fact]
    public void What_the_focus_leads_to_stands_below_it_one_box_under_the_other()
    {
        var chart = Draw(Hero);

        Assert.Equal("hero", chart.Focus.Id.Value);
        Assert.Equal([("town", ChartWing.Below, 1), ("child", ChartWing.Below, 1)], Places(chart));
        Assert.True(chart.Boxes[0].Top >= chart.FocusTop + chart.FocusHeight);
        Assert.True(chart.Boxes[1].Top >= chart.Boxes[0].Top + chart.Boxes[0].Height);
        Assert.All(chart.Boxes, box => Assert.True(box.Left > chart.FocusLeft));
        Assert.True(chart.Height >= chart.Boxes[1].Top + chart.Boxes[1].Height);
    }

    [Fact]
    public void What_leads_to_the_focus_stands_above_it()
    {
        // The hero is the parent of the child and visits the town: seen from either, the hero stands above.
        foreach (var focus in new[] { Child, Town })
        {
            var chart = Draw(focus);
            var hero = Assert.Single(chart.Boxes);

            Assert.Equal(("hero", ChartWing.Above), (hero.Entity.Id.Value, hero.Wing));
            Assert.True(hero.Top + hero.Height <= chart.FocusTop);
            Assert.True(hero.Top >= 0);
        }
    }

    [Fact]
    public void Names_whose_relation_is_the_same_from_both_sides_stand_beside_the_focus_at_one_side_and_the_other()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("rivals", "town", "keepsake", "opponentOf", level: 1, source: "passage-1"));
        story.Relations.Add(SmallStory.Relation("foes", "keepsake", "hero", "opponentOf", level: 1, source: "passage-1"));

        var chart = Draw(Keepsake, story);

        Assert.Equal([("hero", ChartWing.BesideStart, 1), ("town", ChartWing.BesideEnd, 1)], Places(chart));
        var (start, end) = (chart.Boxes[0], chart.Boxes[1]);
        Assert.True(start.Left + start.Width <= chart.FocusLeft);
        Assert.True(end.Left >= chart.FocusLeft + chart.FocusWidth);
        Assert.Equal(chart.FocusTop, start.Top);
        Assert.Equal(chart.FocusTop, end.Top);
        Assert.All(chart.Boxes, box => Assert.Equal((false, false), (box.TowardBox, box.TowardParent)));
        Assert.DoesNotContain(chart.Lines, line => line.ArrowAtStart || line.ArrowAtEnd);
    }

    [Fact]
    public void An_entity_without_relations_is_drawn_alone()
    {
        var chart = Draw(Keepsake);

        Assert.Empty(chart.Boxes);
        Assert.Empty(chart.Lines);
        Assert.True(chart.FocusTop > 0);
        Assert.True(chart.Width >= chart.FocusLeft + chart.FocusWidth);
        Assert.True(chart.Height >= chart.FocusTop + chart.FocusHeight);
    }

    [Fact]
    public void An_arrow_points_from_the_first_entity_of_a_relation_to_the_second()
    {
        var down = Draw(Hero);
        var up = Draw(Child);
        var toChild = down.Boxes.Single(box => box.Entity.Id == Child);
        var fromHero = up.Boxes.Single();

        Assert.Equal((true, false), (toChild.TowardBox, toChild.TowardParent));
        Assert.Equal((false, true), (fromHero.TowardBox, fromHero.TowardParent));

        // The line that meets a box carries the arrow: at the box when the relation leads to it, at the other
        // end when it leads from it.
        var meets = down.Lines.Single(line => Math.Abs(line.Y1 - (toChild.Top + ConnectionsChart.Anchor)) < 0.001 && line.Y1 == line.Y2);
        Assert.Equal((false, true), (meets.ArrowAtStart, meets.ArrowAtEnd));
        Assert.True(meets.X2 < toChild.Left && meets.X2 > toChild.Left - 1);
        var leaves = up.Lines.Single(line => line.Y1 == line.Y2);
        Assert.Equal((true, false), (leaves.ArrowAtStart, leaves.ArrowAtEnd));
    }

    [Fact]
    public void Two_relations_between_the_same_entities_share_a_box_and_make_it_taller()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("rivals", "hero", "child", "opponentOf", level: 2, source: "passage-2"));

        var box = Draw(Hero, story).Boxes.Single(candidate => candidate.Entity.Id == Child);
        var single = Draw(Hero).Boxes.Single(candidate => candidate.Entity.Id == Child);

        // A relation with a direction decides the side; the one without stays in the same box.
        Assert.Equal(["parent", "rivals"], box.Relations.Select(relation => relation.Id.Value));
        Assert.Equal(ChartWing.Below, box.Wing);
        Assert.True(box.Height > single.Height);
        Assert.True(box.TowardBox);
    }

    [Fact]
    public void A_longer_name_and_a_taller_script_need_a_taller_box()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("a town with a very long name that does not fit on one line", EntityTypes.NarrativePlace));
        story.Relations.Add(SmallStory.Relation("long", "hero", "a town with a very long name that does not fit on one line", "visits"));
        var view = Projection.Of(story.Build(), new ReadingLevel(2), Audience.LocalDemonstration)!.Connections(Hero)!;

        var latin = ConnectionsLayout.Of(view, text => text.Resolve("en").Value);
        var tall = ConnectionsLayout.Of(view, text => text.Resolve("en").Value, lineHeight: 1.9);

        Assert.True(latin.Boxes[^1].Height > latin.Boxes[0].Height);
        Assert.True(tall.Boxes[^1].Height > latin.Boxes[^1].Height);
        Assert.True(tall.Height > latin.Height);
        Assert.All(latin.Boxes, box => Assert.Equal(ConnectionsChart.BoxWidth, box.Width));
    }

    [Fact]
    public void The_neighbours_of_an_opened_box_hang_on_it_one_step_further_out_at_the_same_side()
    {
        var chart = Draw(Town, expanded: Hero);

        // The hero stands above the town; what the hero leads to is drawn above the hero – a step further from
        // the middle – although the relation points down: the arrow says so, not the place.
        var (child, hero) = (chart.Boxes[0], chart.Boxes[1]);
        Assert.Equal([("child", ChartWing.Above, 2), ("hero", ChartWing.Above, 1)], Places(chart));
        Assert.True(hero.IsExpanded);
        Assert.False(hero.CanExpand);
        Assert.Equal(["parent"], child.Relations.Select(relation => relation.Id.Value));
        Assert.True(child.Top + child.Height <= hero.Top);
        Assert.True(child.Left > hero.Left);
        Assert.True(child.TowardBox);

        // The line of the child starts at the box of the hero and ends level with the name of the child.
        Assert.Contains(chart.Lines, line => line.X1 == line.X2 && line.Y1 == hero.Top && Math.Abs(line.Y2 - (child.Top + ConnectionsChart.Anchor)) < 0.001);
    }

    [Fact]
    public void A_step_further_at_a_side_opens_what_can_be_opened_there_and_a_step_back_closes_it()
    {
        var closed = Draw(Town);
        var open = Draw(Town, expanded: Hero);

        var hero = Assert.Single(closed.Boxes);
        Assert.True(hero.CanExpand);
        Assert.False(hero.IsExpanded);
        Assert.Equal([Hero], closed.Further(ChartWing.Above));
        Assert.Empty(closed.Nearer(ChartWing.Above));
        Assert.Empty(closed.Further(ChartWing.Below, ChartWing.BesideStart, ChartWing.BesideEnd));

        // Opened, the hero can be closed again; the child has nothing further to show.
        Assert.Equal([Hero], open.Nearer(ChartWing.Above));
        Assert.Empty(open.Further(ChartWing.Above));
        Assert.Empty(open.Nearer(ChartWing.Below, ChartWing.BesideStart, ChartWing.BesideEnd));
    }

    [Fact]
    public void A_step_opens_the_nearest_boxes_first_and_closes_the_farthest_first()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("keeps", "child", "keepsake", "carries", level: 2, source: "passage-2"));

        // The hero leads to the town and the child; the child leads on to the keepsake.
        var first = Draw(Hero, story);
        Assert.Equal([Child], first.Further(ChartWing.Below));

        var second = Draw(Hero, story, expanded: Child);
        Assert.Equal([("town", ChartWing.Below, 1), ("child", ChartWing.Below, 1), ("keepsake", ChartWing.Below, 2)], Places(second));
        Assert.Equal([Child], second.Nearer(ChartWing.Below));
        Assert.Empty(second.Further(ChartWing.Below));
    }

    [Fact]
    public void An_entity_that_is_already_drawn_is_referred_to_and_not_drawn_twice()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("also-visits", "child", "town", "visits", level: 2, source: "passage-2"));

        var chart = Draw(Town, story, expanded: Hero);

        // Hero and child both lead to the town. The relation between the two hangs on the hero and refers to
        // the child, which has a box of its own. Above the focus the boxes are named from the top down: the
        // nearest to the focus comes last.
        Assert.Equal(
            [("child", 1, false), ("child", 2, true), ("hero", 1, false)],
            chart.Boxes.Select(box => (box.Entity.Id.Value, box.Depth, box.IsReference)));
        var reference = chart.Boxes.Single(box => box.IsReference);
        Assert.False(reference.CanExpand);
        Assert.False(reference.IsExpanded);
    }

    [Fact]
    public void A_relation_back_to_the_focus_from_a_deeper_box_refers_to_the_focus()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("also-visits", "child", "town", "visits", level: 2, source: "passage-2"));

        var chart = Draw(Hero, story, expanded: Child);

        var back = chart.Boxes.Single(box => box.Depth == 2);
        Assert.Equal(("town", true, ChartWing.Below), (back.Entity.Id.Value, back.IsReference, back.Wing));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Every_relation_of_the_view_is_drawn_exactly_once_whatever_is_opened(int level)
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("also-visits", "child", "town", "visits", level: 2, source: "passage-2"));
        story.Relations.Add(SmallStory.Relation("rivals", "hero", "child", "opponentOf", level: 2, source: "passage-2"));
        story.Relations.Add(SmallStory.Relation("foes", "keepsake", "hero", "opponentOf", level: 1, source: "passage-1"));
        story.Relations.Add(SmallStory.Relation("guards", "town", "keepsake", "opponentOf", level: 1, source: "passage-1"));
        var projection = Projection.Of(story.Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;
        var everybody = projection.Entities.Select(entity => entity.Id).ToArray();

        foreach (var focus in projection.Entities)
        {
            foreach (var expanded in new[] { [], everybody, everybody.Reverse().ToArray(), new[] { Hero }, new[] { Child, Town } })
            {
                var view = projection.Connections(focus.Id, expanded)!;
                var chart = ConnectionsLayout.Of(view, text => text.Resolve("en").Value);

                Assert.Equal(
                    view.Edges.Select(edge => edge.Relation.Id.Value).Order(),
                    chart.Boxes.SelectMany(box => box.Relations).Select(relation => relation.Id.Value).Order());

                // No two boxes lie on each other – the focus is one of them – and all of them lie inside the chart.
                List<(double Left, double Top, double Right, double Bottom)> frames =
                [
                    .. chart.Boxes.Select(box => (box.Left, box.Top, box.Left + box.Width, box.Top + box.Height)),
                    (chart.FocusLeft, chart.FocusTop, chart.FocusLeft + chart.FocusWidth, chart.FocusTop + chart.FocusHeight),
                ];
                for (var first = 0; first < frames.Count; first++)
                {
                    var a = frames[first];
                    Assert.True(a.Left >= 0 && a.Top >= 0 && a.Right <= chart.Width && a.Bottom <= chart.Height);
                    for (var second = first + 1; second < frames.Count; second++)
                    {
                        var b = frames[second];
                        Assert.False(a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom, $"Two boxes of {focus.Id} lie on each other.");
                    }
                }

                // A line runs across or down, never at a slant, and stays inside the chart.
                Assert.All(chart.Lines, line =>
                {
                    Assert.True(line.X1 == line.X2 || line.Y1 == line.Y2);
                    Assert.InRange(Math.Min(line.X1, line.X2), 0, chart.Width);
                    Assert.InRange(Math.Max(line.X1, line.X2), 0, chart.Width);
                    Assert.InRange(Math.Min(line.Y1, line.Y2), 0, chart.Height);
                    Assert.InRange(Math.Max(line.Y1, line.Y2), 0, chart.Height);
                });

                // Nobody is drawn twice, and what hangs on a box stays at its side of the chart.
                var drawn = chart.Boxes.Where(box => !box.IsReference).Select(box => box.Entity.Id).ToList();
                Assert.Equal(drawn.Count, drawn.Distinct().Count());
                Assert.DoesNotContain(focus.Id, drawn);
                Assert.All(chart.Boxes.Where(box => box.Wing == ChartWing.Above), box => Assert.True(box.Top + box.Height <= chart.FocusTop));
                Assert.All(chart.Boxes.Where(box => box.Wing == ChartWing.Below), box => Assert.True(box.Top >= chart.FocusTop + chart.FocusHeight));
                Assert.All(chart.Boxes.Where(box => box.Wing == ChartWing.BesideStart), box => Assert.True(box.Left + box.Width <= chart.FocusLeft));
                Assert.All(chart.Boxes.Where(box => box.Wing == ChartWing.BesideEnd), box => Assert.True(box.Left >= chart.FocusLeft + chart.FocusWidth));

                // A step at a side only names boxes of that side, and what it opens was closed.
                foreach (var wing in Enum.GetValues<ChartWing>())
                {
                    Assert.All(chart.Further(wing), id => Assert.Contains(chart.Boxes, box => box.Entity.Id == id && box.Wing == wing && box.CanExpand));
                    Assert.All(chart.Nearer(wing), id => Assert.Contains(chart.Boxes, box => box.Entity.Id == id && box.Wing == wing && box.IsExpanded));
                }
            }
        }
    }
}
