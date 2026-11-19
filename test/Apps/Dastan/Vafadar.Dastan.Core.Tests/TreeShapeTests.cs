using System.Collections.Immutable;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// A family as a layout sees it (step 16.4): generations found from the links alone, and a layout that knows
/// nothing of what a member is. How a family of a collection is laid out is checked by <c>FamilyTreeTests</c>,
/// which run through the same layout.
/// </summary>
public sealed class TreeShapeTests
{
    private static TreeShapeLink Link(string from, string to) => new(from, to);

    private static ImmutableDictionary<string, int> Rows(string[] members, TreeShapeLink[] parentage, params TreeShapeLink[] partnerships) =>
        TreeShape.Generations(members, parentage, partnerships);

    [Fact]
    public void Somebody_without_a_parent_stands_in_the_first_row_and_a_child_below_its_parents()
    {
        var rows = Rows(["sam", "zal", "rostam", "sohrab", "stranger"], [Link("sam", "zal"), Link("zal", "rostam"), Link("rostam", "sohrab")]);

        Assert.Equal((0, 1, 2, 3, 0), (rows["sam"], rows["zal"], rows["rostam"], rows["sohrab"], rows["stranger"]));
    }

    [Fact]
    public void A_child_stands_below_the_lower_of_its_parents()
    {
        // Rudabeh's father is in the tree, Zal's is not: Rudabeh stands a row lower than Zal would alone.
        var rows = Rows(["mehrab", "rudabeh", "zal", "rostam"], [Link("mehrab", "rudabeh"), Link("zal", "rostam"), Link("rudabeh", "rostam")]);

        Assert.Equal((0, 1, 0, 2), (rows["mehrab"], rows["rudabeh"], rows["zal"], rows["rostam"]));
    }

    [Fact]
    public void Partners_stand_in_one_row_and_everybody_below_them_moves_along()
    {
        var rows = Rows(
            ["mehrab", "rudabeh", "zal", "rostam", "sohrab"],
            [Link("mehrab", "rudabeh"), Link("zal", "rostam"), Link("rostam", "sohrab")],
            Link("zal", "rudabeh"));

        Assert.Equal((0, 1, 1, 2, 3), (rows["mehrab"], rows["rudabeh"], rows["zal"], rows["rostam"], rows["sohrab"]));
    }

    [Fact]
    public void Links_to_somebody_who_is_not_named_and_to_oneself_are_passed_over()
    {
        var rows = Rows(["zal", "rostam"], [Link("zal", "rostam"), Link("nobody", "zal"), Link("rostam", "rostam")], Link("rostam", "nobody"));

        Assert.Equal((0, 1), (rows["zal"], rows["rostam"]));
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void A_family_in_which_somebody_is_their_own_ancestor_still_ends()
    {
        var rows = Rows(["zal", "rostam", "sohrab"], [Link("zal", "rostam"), Link("rostam", "sohrab"), Link("sohrab", "zal")]);

        Assert.Equal(3, rows.Count);
        Assert.All(rows.Values, row => Assert.InRange(row, 0, 4));
    }

    [Fact]
    public void A_link_to_somebody_who_is_not_named_and_a_member_named_twice_do_not_stop_the_drawing()
    {
        // What a store that lost a person leaves behind: a link whose one end is not there any more.
        var shape = new TreeShape(
            [new("zal", 0, "Zal"), new("rostam", 1, "Rostam"), new("rostam", 1, "Rostam once more")],
            [Link("zal", "rostam"), Link("nobody", "rostam"), Link("zal", "gone")],
            [Link("zal", "gone")]);

        var tree = TreeShapeLayout.Of(shape);

        Assert.Equal(2, tree.Layers);
        Assert.Equal((0, 1), (tree.Node("zal").Layer, tree.Node("rostam").Layer));
        Assert.Equal(("zal", "rostam"), (Assert.Single(tree.Parentage).From, tree.Parentage[0].To));
        Assert.Empty(tree.Partnerships);
    }

    [Fact]
    public void A_shape_is_laid_out_without_knowing_what_a_member_is()
    {
        var shape = new TreeShape(
            [new("zal", 0, "Zal"), new("rudabeh", 0, "Rudabeh"), new("rostam", 1, "Rostam the son of Zal"), new("stranger", 0, "A stranger")],
            [Link("zal", "rostam"), Link("rudabeh", "rostam")],
            [Link("zal", "rudabeh")]);

        var tree = TreeShapeLayout.Of(shape);

        Assert.Equal(2, tree.Layers);
        Assert.Equal((0, 0, 1), (tree.Node("zal").Layer, tree.Node("rudabeh").Layer, tree.Node("rostam").Layer));

        // Partners stand beside each other, their child below the middle of the two.
        Assert.Equal(1, Math.Abs(tree.Node("zal").Place - tree.Node("rudabeh").Place));
        Assert.Equal((tree.Node("zal").Place + tree.Node("rudabeh").Place) / 2, tree.Node("rostam").Place);
        Assert.Equal(["zal", "rudabeh"], tree.Parentage.Select(route => route.From));
        Assert.Equal(tree.Parentage[0].Track, tree.Parentage[1].Track);
        Assert.Equal(0, tree.Crossings);
        Assert.True(tree.Lines >= 2);
        Assert.Equal(3, tree.Breadth);
    }

    [Fact]
    public void A_shape_without_members_is_an_empty_tree()
    {
        var tree = TreeShapeLayout.Of(new TreeShape([], [], []));

        Assert.Empty(tree.Nodes);
        Assert.Equal((0, 0d), (tree.Layers, tree.Breadth));
    }
}
