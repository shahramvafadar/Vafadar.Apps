using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>A member of a family tree with its place.</summary>
/// <param name="Entity">The member.</param>
/// <param name="Layer">The generation, counted from the oldest one of the tree: 0, 1, 2, …</param>
/// <param name="Place">
/// Where the member stands inside its generation, counted in boxes: two members of a generation are at least 1
/// apart. The page turns it into a length.
/// </param>
public sealed record TreeNode(Entity Entity, int Layer, double Place);

/// <summary>
/// How the line from a parent to a child runs, so that the drawing says whose child somebody is. Children of the
/// same parents share one line across the room between two generations; children of other parents – a half-brother,
/// a cousin – get a line of their own there, and a parent of two such families lets a line out for each.
/// </summary>
/// <param name="Relation">The relation from the parent to the child.</param>
/// <param name="Port">Where the line leaves the box of the parent, as a share of the side of the box (0 to 1).</param>
/// <param name="Track">Where the line crosses over to the child, as a share of the room between two generations, counted from the child (0 to 1).</param>
public sealed record TreeRoute(Edge Relation, double Port, double Track);

/// <summary>
/// A family laid out as a tree. Places are counts, not lengths: the layout says who stands in which generation and
/// where in it, the page decides how large a box is and whether generations run down the page or across it.
/// </summary>
/// <param name="Nodes">The members, generation by generation and inside one from its beginning.</param>
/// <param name="Parentage">The relations from a parent to a child, each with the way its line runs.</param>
/// <param name="Partnerships">The relations between partners.</param>
/// <param name="Layers">The number of generations.</param>
/// <param name="Breadth">The room the widest generation needs, in boxes.</param>
/// <param name="Lines">The number of lines the longest name needs in a box.</param>
/// <param name="Crossings">How many pairs of lines from parents to children cross.</param>
public sealed record FamilyTree(
    ImmutableArray<TreeNode> Nodes,
    ImmutableArray<TreeRoute> Parentage,
    ImmutableArray<Edge> Partnerships,
    int Layers,
    double Breadth,
    int Lines,
    int Crossings)
{
    /// <summary>Returns the place of a member.</summary>
    public TreeNode Node(EntityId id) => Nodes.First(node => node.Entity.Id == id);
}

/// <summary>
/// Lays out a <see cref="FamilyView"/> as a tree: generations as layers, partners beside each other, and inside a
/// generation an order that lets as few lines cross as it finds. It is a layout of what the view holds and adds
/// nothing: no member, no relation and no order of birth. The layout itself is <see cref="TreeShapeLayout"/>,
/// which knows nothing of entities and also draws the trees of visitors.
/// </summary>
public static class FamilyTreeLayout
{
    /// <summary>Returns the tree of a family.</summary>
    /// <param name="view">The family.</param>
    /// <param name="text">Returns a text in the language of the page; the longest name decides how many lines a box needs.</param>
    /// <param name="charactersPerLine">How many characters of a name fit on a line of a box.</param>
    public static FamilyTree Of(FamilyView view, Func<LocalizedText, string> text, int charactersPerLine = 11)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(charactersPerLine, 1);

        var placement = TreeShapeLayout.Of(Shape(view, text), charactersPerLine);
        var entities = view.Members.ToDictionary(member => member.Entity.Id.Value, member => member.Entity, StringComparer.Ordinal);
        return new FamilyTree(
            [.. placement.Nodes.Select(node => new TreeNode(entities[node.Id], node.Layer, node.Place))],
            [.. placement.Parentage.Select((route, index) => new TreeRoute(view.Parentage[index], route.Port, route.Track))],
            view.Partnerships,
            placement.Layers,
            placement.Breadth,
            placement.Lines,
            placement.Crossings);
    }

    /// <summary>Returns a family as the layout sees it: who, in which generation, with which name, and the lines between them.</summary>
    public static TreeShape Shape(FamilyView view, Func<LocalizedText, string> text)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);
        return new TreeShape(
            [.. view.Members.Select(member => new TreeShapeMember(member.Entity.Id.Value, member.Generation, text(member.Entity.Label)))],
            [.. view.Parentage.Select(edge => new TreeShapeLink(edge.From.Id.Value, edge.To.Id.Value))],
            [.. view.Partnerships.Select(edge => new TreeShapeLink(edge.From.Id.Value, edge.To.Id.Value))]);
    }
}
