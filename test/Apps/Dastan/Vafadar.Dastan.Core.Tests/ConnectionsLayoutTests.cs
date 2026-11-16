namespace Vafadar.Dastan.Core.Tests;

/// <summary>The drawing of a neighbourhood: a tidy tree in which every relation of the view stands exactly once.</summary>
public sealed class ConnectionsLayoutTests
{
    private static readonly EntityId Hero = new("hero");
    private static readonly EntityId Town = new("town");
    private static readonly EntityId Child = new("child");

    private static ConnectionsDrawing Draw(EntityId focus, SmallStory? story = null, int level = 2, params EntityId[] expanded)
    {
        var projection = Projection.Of((story ?? new SmallStory()).Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;
        return ConnectionsLayout.Of(projection.Connections(focus, expanded)!, text => text.Resolve("en").Value);
    }

    [Fact]
    public void The_neighbours_of_the_focus_are_rows_below_each_other()
    {
        var drawing = Draw(Hero);

        Assert.Equal("hero", drawing.Focus.Id.Value);
        Assert.Equal(["town", "child"], drawing.Rows.Select(row => row.Entity.Id.Value));
        Assert.All(drawing.Rows, row => Assert.Equal(1, row.Depth));
        Assert.Equal(0, drawing.Rows[0].Top);
        Assert.True(drawing.Rows[1].Top >= drawing.Rows[0].Top + drawing.Rows[0].Height);
        Assert.True(drawing.Height >= drawing.Rows[1].Top + drawing.Rows[1].Height);
    }

    [Fact]
    public void The_focus_stands_beside_its_first_neighbour_and_the_line_of_the_neighbours_spans_them()
    {
        var drawing = Draw(Hero);
        var trunk = Assert.Single(drawing.Trunks);

        Assert.Equal(drawing.Rows[0].Top, drawing.FocusTop);
        Assert.Equal(1, trunk.Depth);
        Assert.Equal(drawing.Rows[0].Top + ConnectionsDrawing.Anchor, trunk.Top);
        Assert.Equal(drawing.Rows[1].Top + ConnectionsDrawing.Anchor, trunk.Bottom);
        Assert.InRange(drawing.FocusTop + ConnectionsDrawing.Anchor, trunk.Top, trunk.Bottom);
    }

    [Fact]
    public void An_entity_without_relations_is_drawn_alone()
    {
        var drawing = Draw(new EntityId("keepsake"));

        Assert.Empty(drawing.Rows);
        Assert.Empty(drawing.Trunks);
        Assert.Equal(0, drawing.FocusTop);
        Assert.True(drawing.Height > 0);
    }

    [Fact]
    public void An_arrow_points_from_the_first_entity_of_a_relation_to_the_second()
    {
        var fromHero = Draw(Hero).Rows.Single(row => row.Entity.Id == Child);
        var fromChild = Draw(Child).Rows.Single(row => row.Entity.Id == Hero);

        Assert.Equal((true, false), (fromHero.TowardRow, fromHero.TowardParent));
        Assert.Equal((false, true), (fromChild.TowardRow, fromChild.TowardParent));
    }

    [Fact]
    public void A_relation_that_is_the_same_from_both_sides_has_no_arrow()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("rivals", "town", "keepsake", "opponentOf", level: 1, source: "passage-1"));

        var row = Draw(new EntityId("keepsake"), story).Rows.Single();

        Assert.Equal((false, false), (row.TowardRow, row.TowardParent));
    }

    [Fact]
    public void Two_relations_between_the_same_entities_share_a_row_and_make_it_taller()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("rivals", "hero", "child", "opponentOf", level: 2, source: "passage-2"));

        var row = Draw(Hero, story).Rows.Single(candidate => candidate.Entity.Id == Child);
        var single = Draw(Hero).Rows.Single(candidate => candidate.Entity.Id == Child);

        Assert.Equal(["parent", "rivals"], row.Relations.Select(relation => relation.Id.Value));
        Assert.True(row.Height > single.Height);
        Assert.True(row.TowardRow);
    }

    [Fact]
    public void A_longer_name_and_a_taller_script_need_a_taller_row()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("a town with a very long name that does not fit on one line", EntityTypes.NarrativePlace));
        story.Relations.Add(SmallStory.Relation("long", "hero", "a town with a very long name that does not fit on one line", "visits"));
        var view = Projection.Of(story.Build(), new ReadingLevel(2), Audience.LocalDemonstration)!.Connections(Hero)!;

        var latin = ConnectionsLayout.Of(view, text => text.Resolve("en").Value);
        var tall = ConnectionsLayout.Of(view, text => text.Resolve("en").Value, lineHeight: 1.9);

        Assert.True(latin.Rows[^1].Height > latin.Rows[0].Height);
        Assert.True(tall.Rows[0].Height > latin.Rows[0].Height);
        Assert.True(tall.Height > latin.Height);
    }

    [Fact]
    public void The_neighbours_of_an_expanded_row_hang_below_it_one_step_deeper()
    {
        var drawing = Draw(Town, expanded: Hero);

        Assert.Equal([("hero", 1), ("child", 2)], drawing.Rows.Select(row => (row.Entity.Id.Value, row.Depth)));
        Assert.True(drawing.Rows[0].IsExpanded);
        Assert.False(drawing.Rows[0].CanExpand);
        Assert.Equal(["parent"], drawing.Rows[1].Relations.Select(relation => relation.Id.Value));

        var deeper = drawing.Trunks.Single(trunk => trunk.Depth == 2);
        Assert.Equal(drawing.Rows[0].Top + drawing.Rows[0].Height, deeper.Top);
        Assert.Equal(drawing.Rows[1].Top + ConnectionsDrawing.Anchor, deeper.Bottom);
    }

    [Fact]
    public void A_row_that_can_be_expanded_says_so_and_one_that_is_closed_has_no_rows_below_it()
    {
        var drawing = Draw(Town);

        var hero = Assert.Single(drawing.Rows);
        Assert.True(hero.CanExpand);
        Assert.False(hero.IsExpanded);
    }

    [Fact]
    public void An_entity_that_is_already_drawn_is_referred_to_and_not_drawn_twice()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("also-visits", "child", "town", "visits", level: 2, source: "passage-2"));

        var drawing = Draw(Town, story, expanded: Hero);

        // Hero and child are both neighbours of the town. The relation between the two hangs below the hero and
        // refers to the child, which has its own row.
        Assert.Equal(
            [("hero", 1, false), ("child", 2, true), ("child", 1, false)],
            drawing.Rows.Select(row => (row.Entity.Id.Value, row.Depth, row.IsReference)));
        Assert.False(drawing.Rows[1].CanExpand);
        Assert.False(drawing.Rows[1].IsExpanded);
    }

    [Fact]
    public void A_relation_back_to_the_focus_from_a_deeper_row_refers_to_the_focus()
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("also-visits", "child", "town", "visits", level: 2, source: "passage-2"));

        var drawing = Draw(Hero, story, expanded: Child);

        var back = drawing.Rows.Single(row => row.Depth == 2);
        Assert.Equal(("town", true), (back.Entity.Id.Value, back.IsReference));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Every_relation_of_the_view_is_drawn_exactly_once_whatever_is_expanded(int level)
    {
        var story = new SmallStory();
        story.Relations.Add(SmallStory.Relation("also-visits", "child", "town", "visits", level: 2, source: "passage-2"));
        story.Relations.Add(SmallStory.Relation("rivals", "hero", "child", "opponentOf", level: 2, source: "passage-2"));
        var projection = Projection.Of(story.Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;
        var everybody = projection.Entities.Select(entity => entity.Id).ToArray();

        foreach (var focus in projection.Entities)
        {
            foreach (var expanded in new[] { [], everybody, everybody.Reverse().ToArray(), new[] { Hero }, new[] { Child, Town } })
            {
                var view = projection.Connections(focus.Id, expanded)!;
                var drawing = ConnectionsLayout.Of(view, text => text.Resolve("en").Value);

                Assert.Equal(
                    view.Edges.Select(edge => edge.Relation.Id.Value).Order(),
                    drawing.Rows.SelectMany(row => row.Relations).Select(relation => relation.Id.Value).Order());

                // Rows do not overlap, a deeper row comes after the row it hangs on, and nobody is drawn twice.
                for (var index = 1; index < drawing.Rows.Length; index++)
                {
                    Assert.True(drawing.Rows[index].Top >= drawing.Rows[index - 1].Top + drawing.Rows[index - 1].Height);
                    Assert.True(drawing.Rows[index].Depth <= drawing.Rows[index - 1].Depth + 1);
                }

                var drawn = drawing.Rows.Where(row => !row.IsReference).Select(row => row.Entity.Id).ToList();
                Assert.Equal(drawn.Count, drawn.Distinct().Count());
                Assert.DoesNotContain(focus.Id, drawn);
            }
        }
    }
}
