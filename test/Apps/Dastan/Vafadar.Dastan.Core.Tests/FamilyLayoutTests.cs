namespace Vafadar.Dastan.Core.Tests;

/// <summary>The drawing of a family: generations as rows, parents above their children.</summary>
public sealed class FamilyLayoutTests
{
    private static SmallStory Family()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("mother", EntityTypes.Character, level: 1, source: "passage-1"));
        story.Entities.Add(SmallStory.Entity("elder", EntityTypes.Character));
        story.Relations.Add(SmallStory.Relation("married", "hero", "mother", "spouseOf", level: 1, source: "passage-1"));
        story.Relations.Add(SmallStory.Relation("mother-of", "mother", "child", "parentOf", level: 2, source: "passage-2"));
        story.Relations.Add(SmallStory.Relation("elder-of", "elder", "hero", "parentOf"));
        return story;
    }

    private static FamilyDrawing Draw(SmallStory story, int level = 2, double lineHeight = 1.6) =>
        FamilyLayout.Of(
            Projection.Of(story.Build(), new ReadingLevel(level), Audience.LocalDemonstration)!.Family(),
            text => text.Resolve("en").Value,
            lineHeight);

    private static FamilyBox Box(FamilyDrawing drawing, string id) => drawing.Boxes.Single(box => box.Entity.Id.Value == id);

    [Fact]
    public void Every_generation_is_a_row_and_a_child_stands_below_its_parents()
    {
        var drawing = Draw(Family());

        Assert.Equal(["elder", "hero", "mother", "child"], drawing.Boxes.Select(box => box.Entity.Id.Value));
        Assert.Equal([0, 1, 1, 2], drawing.Boxes.Select(box => box.Generation));
        Assert.Equal(Box(drawing, "hero").Top, Box(drawing, "mother").Top);
        Assert.True(Box(drawing, "hero").Top > Box(drawing, "elder").Top + Box(drawing, "elder").Height);
        Assert.True(Box(drawing, "child").Top > Box(drawing, "hero").Top + Box(drawing, "hero").Height);
        Assert.Equal(Box(drawing, "child").Top + Box(drawing, "child").Height, drawing.Height, precision: 6);
    }

    [Fact]
    public void The_members_of_a_generation_share_the_width_and_do_not_overlap()
    {
        var drawing = Draw(Family());
        var (hero, mother) = (Box(drawing, "hero"), Box(drawing, "mother"));

        Assert.True(hero.X + hero.Width < mother.X);
        Assert.Equal(hero.Width, mother.Width);
        Assert.Equal(hero.Height, mother.Height);
        Assert.All(drawing.Boxes, box => Assert.InRange(box.X + box.Width, 0, 100));
        Assert.Equal(96, Box(drawing, "elder").Width);
    }

    [Fact]
    public void A_line_leads_from_each_parent_down_to_the_child()
    {
        var drawing = Draw(Family());
        var lines = drawing.Links.Where(link => link.IsParentage).ToList();

        Assert.Equal(["parent", "mother-of", "elder-of"], lines.Select(link => link.Relation.Relation.Id.Value));
        foreach (var line in lines)
        {
            var (parent, child) = (Box(drawing, line.Relation.From.Id.Value), Box(drawing, line.Relation.To.Id.Value));
            Assert.Equal(parent.X + (parent.Width / 2), line.X1);
            Assert.Equal(parent.Top + parent.Height, line.Y1);
            Assert.Equal(child.X + (child.Width / 2), line.X2);
            Assert.True(line.Y2 < child.Top && line.Y2 > line.Y1);
        }
    }

    [Fact]
    public void Partners_are_joined_side_to_side()
    {
        var drawing = Draw(Family());
        var line = Assert.Single(drawing.Links, link => !link.IsParentage);
        var (hero, mother) = (Box(drawing, "hero"), Box(drawing, "mother"));

        Assert.Equal("married", line.Relation.Relation.Id.Value);
        Assert.Equal((hero.X + hero.Width, mother.X), (line.X1, line.X2));
        Assert.Equal(line.Y1, line.Y2);
        Assert.InRange(line.Y1, hero.Top, hero.Top + hero.Height);
    }

    [Fact]
    public void A_family_at_an_earlier_stage_is_smaller_and_one_without_members_is_empty()
    {
        Assert.Equal(["elder", "hero", "mother"], Draw(Family(), level: 1).Boxes.Select(box => box.Entity.Id.Value));
        Assert.Equal(["elder", "hero"], Draw(Family(), level: 0).Boxes.Select(box => box.Entity.Id.Value));

        var none = Draw(new SmallStory(), level: 0);
        Assert.Empty(none.Boxes);
        Assert.Empty(none.Links);
        Assert.Equal(0, none.Height);
    }

    [Fact]
    public void A_generation_of_more_than_three_continues_on_the_next_line()
    {
        var story = new SmallStory();
        foreach (var name in new[] { "anna", "boris", "clara", "david" })
        {
            story.Entities.Add(SmallStory.Entity(name, EntityTypes.Character));
            story.Relations.Add(SmallStory.Relation($"parent-of-{name}", "hero", name, "parentOf"));
        }

        var drawing = Draw(story);
        var children = drawing.Boxes.Where(box => box.Generation == 1).ToList();

        Assert.Equal(5, children.Count);
        Assert.Equal(2, children.Select(box => box.Top).Distinct().Count());
        Assert.Equal(3, children.Count(box => box.Top == children[0].Top));
        Assert.True(children[3].Top >= children[0].Top + children[0].Height);
        Assert.Equal(5, drawing.Links.Count(link => link.IsParentage));
    }

    [Fact]
    public void A_taller_script_needs_taller_boxes()
    {
        var story = Family();
        story.Entities.Add(SmallStory.Entity("a relative with a name far too long for one line of a box", EntityTypes.Character));
        story.Relations.Add(SmallStory.Relation("long", "elder", "a relative with a name far too long for one line of a box", "parentOf"));

        var latin = Draw(story);
        var tall = Draw(story, lineHeight: 1.9);

        Assert.True(tall.Height > latin.Height);
        Assert.True(latin.Boxes.Max(box => box.Height) > latin.Boxes.Min(box => box.Height));
    }
}
