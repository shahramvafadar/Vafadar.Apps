namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The family tree around somebody – ancestors and descendants opened generation by generation – and its layout:
/// generations as layers, partners beside each other, an order with as few crossing lines as it finds.
/// </summary>
public sealed class FamilyTreeTests
{
    // A house of four generations, written into the collection in an order that is not the order of a tree:
    //
    //   grandfather = grandmother      elder
    //        |            |              |
    //      father = mother (child of elder)       aunt (child of grandfather and grandmother)
    //        |        |                             |
    //       son = wife      daughter              cousin
    //        |     |
    //       grandchild
    private static SmallStory House()
    {
        var story = new SmallStory();
        foreach (var name in new[] { "cousin", "wife", "aunt", "grandchild", "daughter", "son", "mother", "father", "elder", "grandmother", "grandfather" })
        {
            story.Entities.Add(SmallStory.Entity(name, EntityTypes.Character));
        }

        Parent(story, "grandfather", "father");
        Parent(story, "grandmother", "father");
        Parent(story, "grandfather", "aunt");
        Parent(story, "grandmother", "aunt");
        Parent(story, "elder", "mother");
        Parent(story, "father", "son");
        Parent(story, "mother", "son");
        Parent(story, "father", "daughter");
        Parent(story, "mother", "daughter");
        Parent(story, "aunt", "cousin");
        Parent(story, "son", "grandchild");
        Parent(story, "wife", "grandchild");
        Partners(story, "grandfather", "grandmother");
        Partners(story, "father", "mother");
        Partners(story, "son", "wife");
        return story;
    }

    private static void Parent(SmallStory story, string parent, string child, int level = 0) =>
        story.Relations.Add(SmallStory.Relation($"{parent}-of-{child}", parent, child, "parentOf", level, $"passage-{level}"));

    private static void Partners(SmallStory story, string first, string second) =>
        story.Relations.Add(SmallStory.Relation($"{first}-and-{second}", first, second, "spouseOf"));

    private static Projection At(SmallStory story, int level = 0) =>
        Projection.Of(story.Build(), new ReadingLevel(level), Audience.LocalDemonstration)!;

    private static FamilyView Around(string id, int up, int down, SmallStory? story = null) =>
        At(story ?? House()).Family(new EntityId(id), up, down)!;

    private static IEnumerable<string> Names(FamilyView view) => view.Members.Select(member => member.Entity.Id.Value).Order(StringComparer.Ordinal);

    private static FamilyTree Tree(FamilyView view, int perLine = 11) => FamilyTreeLayout.Of(view, text => text.Resolve("en").Value, perLine);

    private static TreeNode Node(FamilyTree tree, string id) => tree.Node(new EntityId(id));

    // --- The view ---

    [Fact]
    public void Without_a_generation_above_or_below_the_tree_is_somebody_and_their_partners()
    {
        var view = Around("son", 0, 0);

        Assert.Equal(["son", "wife"], Names(view));
        Assert.Empty(view.Parentage);
        Assert.Equal(["son-and-wife"], view.Partnerships.Select(edge => edge.Relation.Id.Value));
        Assert.True(view.MoreAbove);
        Assert.True(view.MoreBelow);
    }

    [Fact]
    public void A_generation_above_adds_the_parents_and_a_generation_below_the_children()
    {
        var view = Around("son", 1, 1);

        Assert.Equal(["father", "grandchild", "mother", "son", "wife"], Names(view));
        Assert.Equal(
            ["father-of-son", "mother-of-son", "son-of-grandchild", "wife-of-grandchild"],
            view.Parentage.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(["father-and-mother", "son-and-wife"], view.Partnerships.Select(edge => edge.Relation.Id.Value));
        Assert.Equal(
            [("father", 0), ("mother", 0), ("son", 1), ("wife", 1), ("grandchild", 2)],
            view.Members.Select(member => (member.Entity.Id.Value, member.Generation)).OrderBy(member => member.Generation).ThenBy(member => member.Value, StringComparer.Ordinal));

        // The parents have parents of their own; the grandchild has no child.
        Assert.True(view.MoreAbove);
        Assert.False(view.MoreBelow);
    }

    [Fact]
    public void The_tree_follows_the_line_of_somebody_and_not_their_whole_family()
    {
        var view = Around("son", 2, 2);

        // Grandparents of both sides, but neither the sister nor the aunt and her child: they are in nobody's line.
        Assert.Equal(["elder", "father", "grandchild", "grandfather", "grandmother", "mother", "son", "wife"], Names(view));
        Assert.DoesNotContain(view.Parentage, edge => edge.To.Id.Value is "daughter" or "aunt" or "cousin");
        Assert.False(view.MoreAbove);
        Assert.False(view.MoreBelow);

        // The whole family is the other question, and it still has everybody.
        Assert.Equal(11, At(House()).Family(new EntityId("son"))!.Members.Length);
    }

    [Fact]
    public void A_partner_belongs_to_the_tree_and_the_family_of_the_partner_does_not()
    {
        var view = Around("father", 1, 1);

        // The mother is there as his partner and the wife as the partner of his son; the father of the mother is
        // not an ancestor of the father.
        Assert.Equal(["daughter", "father", "grandfather", "grandmother", "mother", "son", "wife"], Names(view));
        Assert.DoesNotContain(view.Members, member => member.Entity.Id.Value == "elder");

        // Nobody above the grandparents; below the children there is a grandchild.
        Assert.False(view.MoreAbove);
        Assert.True(view.MoreBelow);
    }

    [Fact]
    public void Asking_for_more_generations_than_there_are_changes_nothing()
    {
        Assert.Equal(Names(Around("son", 2, 2)), Names(Around("son", 9, 9)));
        Assert.False(Around("son", 9, 9).MoreAbove);
    }

    [Fact]
    public void Somebody_whose_family_lies_outside_what_was_asked_for_stands_alone_and_the_tree_says_that_it_goes_on()
    {
        var view = Around("cousin", 0, 0);

        Assert.Equal(["cousin"], Names(view));
        Assert.Empty(view.Parentage);
        Assert.True(view.MoreAbove);
        Assert.False(view.MoreBelow);
    }

    [Fact]
    public void Something_without_a_family_has_an_empty_tree_and_something_unknown_has_none()
    {
        var projection = At(House());

        var town = projection.Family(new EntityId("town"), 2, 2)!;
        Assert.Empty(town.Members);
        Assert.False(town.MoreAbove || town.MoreBelow);
        Assert.Null(projection.Family(new EntityId("nobody"), 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Family(new EntityId("son"), -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Family(new EntityId("son"), 0, -1));
    }

    [Fact]
    public void What_the_story_has_not_told_yet_is_neither_in_the_tree_nor_announced_by_it()
    {
        var story = House();
        story.Entities.Add(SmallStory.Entity("forefather", EntityTypes.Character, level: 1, source: "passage-1"));
        Parent(story, "forefather", "grandfather", level: 1);

        // At the opening nobody stands above the grandfather – and the tree does not say that somebody will.
        var before = At(story).Family(new EntityId("father"), 1, 0)!;
        Assert.False(before.MoreAbove);
        Assert.DoesNotContain("forefather", Names(At(story).Family(new EntityId("father"), 9, 0)!));

        var after = At(story, level: 1).Family(new EntityId("father"), 1, 0)!;
        Assert.True(after.MoreAbove);
        Assert.Contains("forefather", Names(At(story, level: 1).Family(new EntityId("father"), 2, 0)!));
    }

    [Fact]
    public void Relations_that_run_in_a_circle_end_the_walk()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("other", EntityTypes.Character));
        Parent(story, "hero", "other");
        Parent(story, "other", "hero");

        var view = At(story).Family(new EntityId("hero"), 9, 9)!;

        Assert.Equal(["hero", "other"], Names(view));
        Assert.Equal(2, view.Parentage.Length);
    }

    // --- The layout ---

    [Fact]
    public void Generations_are_layers_and_a_child_stands_below_its_parents()
    {
        var tree = Tree(At(House()).Family(new EntityId("son"))!);

        Assert.Equal(4, tree.Layers);
        Assert.Equal(11, tree.Nodes.Length);
        Assert.All(tree.Parentage, edge => Assert.True(Node(tree, edge.To.Id.Value).Layer > Node(tree, edge.From.Id.Value).Layer));
        Assert.Equal(0, Node(tree, "grandfather").Layer);
        Assert.Equal(3, Node(tree, "grandchild").Layer);

        // The members are given layer by layer and inside a layer from its beginning.
        Assert.Equal(tree.Nodes.OrderBy(node => node.Layer).ThenBy(node => node.Place), tree.Nodes);
    }

    [Fact]
    public void No_two_members_of_a_generation_share_a_place_and_the_tree_begins_at_its_edge()
    {
        var tree = Tree(At(House()).Family(new EntityId("son"))!);

        foreach (var layer in tree.Nodes.GroupBy(node => node.Layer))
        {
            var places = layer.Select(node => node.Place).ToList();
            for (var index = 1; index < places.Count; index++)
            {
                Assert.True(places[index] - places[index - 1] >= 1 - 1e-9, $"Two members of the generation {layer.Key} are closer than a box.");
            }
        }

        Assert.Equal(0, tree.Nodes.Min(node => node.Place));
        Assert.Equal(tree.Nodes.Max(node => node.Place) + 1, tree.Breadth);
    }

    [Fact]
    public void Partners_stand_beside_each_other()
    {
        var tree = Tree(At(House()).Family(new EntityId("son"))!);

        Assert.All(tree.Partnerships, edge =>
        {
            var (first, second) = (Node(tree, edge.From.Id.Value), Node(tree, edge.To.Id.Value));
            Assert.Equal(first.Layer, second.Layer);
            Assert.Equal(1, Math.Abs(first.Place - second.Place), precision: 6);
        });
    }

    [Fact]
    public void The_children_of_a_branch_stand_on_the_side_of_their_branch()
    {
        var tree = Tree(At(House()).Family(new EntityId("son"))!);

        // The collection names the cousin first and the grandfather last. Two crossings no order avoids: twice two
        // parents have two children each, and the lines of such a pair of pairs always cross once. No more than
        // these are left.
        Assert.Equal(2, tree.Crossings);

        // The children of the father and the mother stand together, the child of the aunt on the side of the aunt.
        var aunt = Node(tree, "aunt").Place < Node(tree, "father").Place;
        Assert.Equal(aunt, Node(tree, "cousin").Place < Node(tree, "son").Place);
        Assert.Equal(aunt, Node(tree, "cousin").Place < Node(tree, "daughter").Place);
    }

    [Fact]
    public void Two_lines_cross_in_one_order_and_not_in_the_other()
    {
        var story = new SmallStory();
        foreach (var name in new[] { "anna", "boris", "child-of-boris", "child-of-anna" })
        {
            story.Entities.Add(SmallStory.Entity(name, EntityTypes.Character));
        }

        Parent(story, "anna", "child-of-anna");
        Parent(story, "boris", "child-of-boris");

        var tree = Tree(At(story).Family());

        // In the order of the collection the child of Boris would stand before the child of Anna.
        Assert.Equal(0, tree.Crossings);
        Assert.True(Node(tree, "anna").Place < Node(tree, "boris").Place);
        Assert.True(Node(tree, "child-of-anna").Place < Node(tree, "child-of-boris").Place);
        Assert.Equal(Node(tree, "anna").Place, Node(tree, "child-of-anna").Place);
    }

    [Fact]
    public void A_crossing_that_no_order_avoids_is_counted_and_stays()
    {
        var story = new SmallStory();
        foreach (var name in new[] { "anna", "boris", "first", "second" })
        {
            story.Entities.Add(SmallStory.Entity(name, EntityTypes.Character));
        }

        // Two parents who are not recorded as partners, each a parent of both children.
        foreach (var parent in new[] { "anna", "boris" })
        {
            Parent(story, parent, "first");
            Parent(story, parent, "second");
        }

        Assert.Equal(1, Tree(At(story).Family()).Crossings);
    }

    [Fact]
    public void A_parent_stands_above_the_middle_of_the_children_and_a_child_below_the_middle_of_a_couple()
    {
        var story = new SmallStory();
        foreach (var name in new[] { "anna", "boris", "clara" })
        {
            story.Entities.Add(SmallStory.Entity(name, EntityTypes.Character));
            Parent(story, "hero", name);
        }

        var one = Tree(At(story).Family());
        Assert.Equal([0, 1, 2], one.Nodes.Where(node => node.Layer == 1).Select(node => node.Place));
        Assert.Equal(1, Node(one, "hero").Place);

        var couple = Tree(Around("son", 0, 1));
        Assert.Equal(0.5, Node(couple, "grandchild").Place);
        Assert.Equal([0, 1], couple.Nodes.Where(node => node.Layer == 0).Select(node => node.Place));
    }

    [Fact]
    public void Somebody_with_two_partners_stands_between_them()
    {
        var story = new SmallStory();
        story.Entities.Add(SmallStory.Entity("first-partner", EntityTypes.Character));
        story.Entities.Add(SmallStory.Entity("second-partner", EntityTypes.Character));
        Partners(story, "hero", "first-partner");
        Partners(story, "hero", "second-partner");

        var tree = Tree(At(story).Family());

        Assert.Equal(1, Node(tree, "hero").Place);
        Assert.Equal([0, 2], new[] { Node(tree, "first-partner").Place, Node(tree, "second-partner").Place }.Order());
    }

    [Fact]
    public void The_same_family_is_always_laid_out_the_same()
    {
        var view = At(House()).Family(new EntityId("son"))!;

        Assert.Equal(Tree(view).Nodes, Tree(view).Nodes);
        Assert.Equal(Tree(At(House()).Family(new EntityId("son"))!).Nodes.Select(node => (node.Entity.Id, node.Layer, node.Place)), Tree(view).Nodes.Select(node => (node.Entity.Id, node.Layer, node.Place)));
    }

    [Fact]
    public void A_long_name_needs_more_lines_and_an_empty_family_has_an_empty_tree()
    {
        var story = House();
        story.Entities.Add(SmallStory.Entity("a relative with a name far too long for one line of a box", EntityTypes.Character));
        Parent(story, "elder", "a relative with a name far too long for one line of a box");

        Assert.Equal(1, Tree(At(House()).Family()).Lines);
        Assert.Equal(6, Tree(At(story).Family()).Lines);
        Assert.Equal(3, Tree(At(story).Family(), perLine: 22).Lines);

        var none = Tree(At(new SmallStory()).Family());
        Assert.Empty(none.Nodes);
        Assert.Equal((0, 0.0, 0, 0), (none.Layers, none.Breadth, none.Lines, none.Crossings));
    }
}
