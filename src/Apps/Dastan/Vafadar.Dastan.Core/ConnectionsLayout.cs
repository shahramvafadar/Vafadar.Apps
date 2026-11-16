using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>The side of a connections chart a name stands at, seen from the name in its middle.</summary>
public enum ChartWing
{
    /// <summary>Above the middle: a relation leads from the name to the name in the middle, as a parent stands above a child.</summary>
    Above = 0,

    /// <summary>Below the middle: a relation leads from the name in the middle to the name.</summary>
    Below = 1,

    /// <summary>Beside the middle, where reading starts: the relations are the same from both sides.</summary>
    BesideStart = 2,

    /// <summary>Beside the middle, where reading ends: the relations are the same from both sides.</summary>
    BesideEnd = 3,
}

/// <summary>One box of a connections chart: an entity with the relations that connect it to the box it hangs on.</summary>
/// <param name="Entity">The entity of the box.</param>
/// <param name="Relations">The relations between the entity and the box it hangs on.</param>
/// <param name="Wing">The side of the chart the box stands at.</param>
/// <param name="Depth">1 for a neighbour of the middle, 2 for a neighbour of an opened box, and so on.</param>
/// <param name="Left">The distance of the box from the side of the chart where reading starts, in em.</param>
/// <param name="Top">The distance of the box from the top of the chart, in em.</param>
/// <param name="Width">The width of the box, in em.</param>
/// <param name="Height">The estimated height of the box, in em.</param>
/// <param name="TowardBox">Whether a relation points from the box it hangs on to this entity.</param>
/// <param name="TowardParent">Whether a relation points from this entity to the box it hangs on.</param>
/// <param name="IsReference">Whether the entity is already drawn elsewhere; the box then only refers to it.</param>
/// <param name="IsExpanded">Whether the relations of the entity are drawn as well.</param>
/// <param name="CanExpand">Whether the entity has relations that are not drawn yet.</param>
public sealed record ConnectionBox(
    Entity Entity,
    ImmutableArray<Relation> Relations,
    ChartWing Wing,
    int Depth,
    double Left,
    double Top,
    double Width,
    double Height,
    bool TowardBox,
    bool TowardParent,
    bool IsReference,
    bool IsExpanded,
    bool CanExpand);

/// <summary>A straight line of a connections chart, in em from the side where reading starts and from the top.</summary>
/// <param name="X1">Where the line starts across the chart.</param>
/// <param name="Y1">Where the line starts down the chart.</param>
/// <param name="X2">Where the line ends across the chart.</param>
/// <param name="Y2">Where the line ends down the chart.</param>
/// <param name="ArrowAtStart">Whether an arrow points to the start of the line.</param>
/// <param name="ArrowAtEnd">Whether an arrow points to the end of the line.</param>
public sealed record ConnectionLine(double X1, double Y1, double X2, double Y2, bool ArrowAtStart = false, bool ArrowAtEnd = false);

/// <summary>
/// A neighbourhood laid out as a chart with the focus in its middle (ADR 0008). What a relation leads from stands
/// above the focus and what it leads to below it – as parents and children do in a family tree; names whose
/// relations are the same from both sides stand beside it. The neighbours of an opened box hang on that box, a
/// step further out. The chart knows no pixels and no markup: its measures are in em.
/// </summary>
/// <param name="Focus">The entity in the middle.</param>
/// <param name="FocusLeft">The distance of the focus from the side where reading starts, in em.</param>
/// <param name="FocusTop">The distance of the focus from the top, in em.</param>
/// <param name="FocusWidth">The width of the focus, in em.</param>
/// <param name="FocusHeight">The estimated height of the focus, in em.</param>
/// <param name="Width">The width of the whole chart, in em.</param>
/// <param name="Height">The height of the whole chart, in em.</param>
/// <param name="Boxes">The boxes: those above the focus from the top down, then those below it, then those beside it.</param>
/// <param name="Lines">The lines the boxes hang on.</param>
public sealed record ConnectionsChart(
    Entity Focus,
    double FocusLeft,
    double FocusTop,
    double FocusWidth,
    double FocusHeight,
    double Width,
    double Height,
    ImmutableArray<ConnectionBox> Boxes,
    ImmutableArray<ConnectionLine> Lines)
{
    /// <summary>The distance from the top of a box to the line that meets it, in em: the middle of its first line.</summary>
    public const double Anchor = 1.25;

    /// <summary>The width of a box, in em.</summary>
    public const double BoxWidth = 12;

    /// <summary>
    /// Returns the entities a step further at some sides opens: the boxes nearest to the middle that can be
    /// opened. Empty when nothing there can be opened.
    /// </summary>
    public ImmutableArray<EntityId> Further(params ChartWing[] wings)
    {
        var closed = Boxes.Where(box => wings.Contains(box.Wing) && box.CanExpand).ToList();
        return closed.Count == 0 ? [] : [.. closed.Where(box => box.Depth == closed.Min(other => other.Depth)).Select(box => box.Entity.Id)];
    }

    /// <summary>
    /// Returns the entities a step back at some sides closes: the opened boxes farthest from the middle. Empty
    /// when nothing there is open.
    /// </summary>
    public ImmutableArray<EntityId> Nearer(params ChartWing[] wings)
    {
        var open = Boxes.Where(box => wings.Contains(box.Wing) && box.IsExpanded).ToList();
        return open.Count == 0 ? [] : [.. open.Where(box => box.Depth == open.Max(other => other.Depth)).Select(box => box.Entity.Id)];
    }
}

/// <summary>Lays out a <see cref="ConnectionsView"/> as a chart.</summary>
public static class ConnectionsLayout
{
    // Measures in em.
    private const double Pad = 0.6;
    private const double Gap = 0.75;
    private const double Indent = 1.5;
    private const double ColumnGap = 0.5;
    private const double ArrowGap = 0.3;

    // Boxes deeper than this stand under each other without moving further across.
    private const int DeepestIndent = 4;

    // Average widths of a character, in em: of a name, and of the smaller words of a relation.
    private const double NameCharacter = 0.6;
    private const double RelationCharacter = 0.47;

    // What the padding of a box and the control that opens it take of its width, in em.
    private const double BoxPadding = 1.6;
    private const double Toggle = 3.25;

    /// <summary>
    /// Returns the chart of a neighbourhood. Every relation of the view is drawn exactly once: where it leads to
    /// an entity that is already in the chart, the box refers to that entity instead of drawing it again.
    /// </summary>
    /// <param name="view">The neighbourhood.</param>
    /// <param name="text">Returns a text in the language of the page; its length decides how many lines a box needs.</param>
    /// <param name="lineHeight">The height of a line of text in em (taller for Arabic script).</param>
    /// <remarks>The server cannot measure text: the height of a box is estimated from the number of its characters.</remarks>
    public static ConnectionsChart Of(ConnectionsView view, Func<LocalizedText, string> text, double lineHeight = 1.6)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);

        var expanded = view.Expanded.Select(entity => entity.Id).ToHashSet();
        var expandable = view.Expandable.Select(entity => entity.Id).ToHashSet();
        var drawnRelations = new HashSet<RelationId>();
        var drawnEntities = new HashSet<EntityId> { view.Focus.Id };
        var wings = Enum.GetValues<ChartWing>().ToDictionary(wing => wing, _ => new List<Node>());
        var mutual = 0;

        Place(view.Focus, null, 1, null);

        // Inside a wing the boxes stand under each other in the order they were placed: a box, then what hangs on it.
        var heights = new Dictionary<ChartWing, double>();
        var widths = new Dictionary<ChartWing, double>();
        foreach (var (wing, nodes) in Enum.GetValues<ChartWing>().Select(wing => (wing, wings[wing])))
        {
            var y = 0.0;
            foreach (var node in nodes)
            {
                node.Top = y;
                node.Left = Indent * Math.Min(node.Depth, DeepestIndent);
                y += node.Height + Gap;
            }

            heights[wing] = Math.Max(0, y - Gap);
            widths[wing] = nodes.Count == 0 ? 0 : nodes.Max(node => node.Left) + ConnectionsChart.BoxWidth;
        }

        // The focus has a heavier border and heavier letters than a box.
        var focusHeight = TextEstimate.Padding + 0.25
            + Math.Max(TextEstimate.Target, TextEstimate.Lines(text(view.Focus.Label), PerLine(NameCharacter * 1.05, 0)) * lineHeight);
        var centreLeft = Pad + (widths[ChartWing.BesideStart] > 0 ? widths[ChartWing.BesideStart] + ColumnGap : 0);
        var centreWidth = Math.Max(ConnectionsChart.BoxWidth, Math.Max(widths[ChartWing.Above], widths[ChartWing.Below]));
        var focusTop = Pad + (heights[ChartWing.Above] > 0 ? heights[ChartWing.Above] + Gap : 0);
        var focusBottom = focusTop + focusHeight;

        // From the places inside a wing to the places in the chart. The wing above the focus is the wing below it
        // turned over, the wing where reading starts the one where it ends turned round.
        foreach (var node in wings[ChartWing.Above])
        {
            node.Top = Pad + heights[ChartWing.Above] - node.Top - node.Height;
            node.Left += centreLeft;
        }

        foreach (var node in wings[ChartWing.Below])
        {
            node.Top += focusBottom + Gap;
            node.Left += centreLeft;
        }

        foreach (var node in wings[ChartWing.BesideEnd])
        {
            node.Top += focusTop;
            node.Left += centreLeft + centreWidth + ColumnGap;
        }

        foreach (var node in wings[ChartWing.BesideStart])
        {
            node.Top += focusTop;
            node.Left = Pad + widths[ChartWing.BesideStart] - node.Left - ConnectionsChart.BoxWidth;
        }

        var lines = ImmutableArray.CreateBuilder<ConnectionLine>();
        foreach (var (wing, nodes) in Enum.GetValues<ChartWing>().Select(wing => (wing, wings[wing])))
        {
            // Towards the start of reading the boxes are met at their far side, and the lines step the other way.
            var turned = wing == ChartWing.BesideStart;
            foreach (var family in nodes.GroupBy(node => node.Parent))
            {
                var children = family.ToList();
                var meets = turned ? children[0].Left + ConnectionsChart.BoxWidth : children[0].Left;
                var trunk = meets + (turned ? Indent / 2 : -Indent / 2);
                var anchors = children.Select(child => child.Top + ConnectionsChart.Anchor).ToList();
                var parent = family.Key;

                // The line the boxes of one parent hang on: from the parent to the farthest of them.
                switch (wing)
                {
                    case ChartWing.Above:
                        lines.Add(new ConnectionLine(trunk, parent?.Top ?? focusTop, trunk, anchors.Min()));
                        break;
                    case ChartWing.Below:
                        lines.Add(new ConnectionLine(trunk, parent is null ? focusBottom : parent.Top + parent.Height, trunk, anchors.Max()));
                        break;
                    default:
                        if (parent is null)
                        {
                            // Beside the focus the line starts at its side, level with its name.
                            var level = focusTop + ConnectionsChart.Anchor;
                            lines.Add(new ConnectionLine(turned ? centreLeft : centreLeft + ConnectionsChart.BoxWidth, level, trunk, level));
                            lines.Add(new ConnectionLine(trunk, level, trunk, anchors.Max()));
                        }
                        else
                        {
                            lines.Add(new ConnectionLine(trunk, parent.Top + parent.Height, trunk, anchors.Max()));
                        }

                        break;
                }

                foreach (var (child, anchor) in children.Zip(anchors))
                {
                    lines.Add(new ConnectionLine(trunk, anchor, meets + (turned ? ArrowGap : -ArrowGap), anchor, child.TowardParent, child.TowardBox));
                }
            }
        }

        var below = Math.Max(
            focusBottom + (heights[ChartWing.Below] > 0 ? Gap + heights[ChartWing.Below] : 0),
            focusTop + Math.Max(heights[ChartWing.BesideStart], heights[ChartWing.BesideEnd]));
        return new ConnectionsChart(
            view.Focus,
            centreLeft,
            focusTop,
            ConnectionsChart.BoxWidth,
            focusHeight,
            centreLeft + centreWidth + (widths[ChartWing.BesideEnd] > 0 ? ColumnGap + widths[ChartWing.BesideEnd] : 0) + Pad,
            below + Pad,
            [
                .. wings[ChartWing.Above].OrderBy(node => node.Top).Select(Box),
                .. wings[ChartWing.Below].Select(Box),
                .. wings[ChartWing.BesideStart].Select(Box),
                .. wings[ChartWing.BesideEnd].Select(Box),
            ],
            lines.ToImmutable());

        void Place(Entity parent, Node? parentNode, int depth, ChartWing? inherited)
        {
            var groups = view.Edges
                .Where(edge => (edge.From.Id == parent.Id || edge.To.Id == parent.Id) && !drawnRelations.Contains(edge.Relation.Id))
                .GroupBy(edge => edge.From.Id == parent.Id ? edge.To : edge.From)
                .ToList();

            // The neighbours of this parent are drawn here, as its boxes, even when an opened box before them
            // leads to them as well – that box then refers to them.
            var fresh = groups.ToDictionary(group => group.Key.Id, group => drawnEntities.Add(group.Key.Id));
            foreach (var group in groups)
            {
                ImmutableArray<Relation> relations = [.. group.Select(edge => edge.Relation).Where(relation => drawnRelations.Add(relation.Id))];
                if (relations.IsEmpty)
                {
                    continue;
                }

                var other = group.Key;
                var towardBox = relations.Any(relation => relation.Type.Directionality == Directionality.Directed && relation.From == parent.Id && relation.To == other.Id && other.Id != parent.Id);
                var towardParent = relations.Any(relation => relation.Type.Directionality == Directionality.Directed && relation.From == other.Id && other.Id != parent.Id);

                // Where a neighbour of the focus stands follows from its relations; what hangs on it stays at its side.
                var wing = inherited
                    ?? (towardBox ? ChartWing.Below
                        : towardParent ? ChartWing.Above
                        : mutual++ % 2 == 0 ? ChartWing.BesideEnd : ChartWing.BesideStart);
                var isReference = !fresh[other.Id];
                var isExpanded = !isReference && expanded.Contains(other.Id);
                var canExpand = !isReference && expandable.Contains(other.Id);

                // The name shares its line with the control that opens or closes the box; a box that refers to
                // another one says so on a line of its own.
                var node = new Node(other, relations, wing, depth, parentNode, towardBox, towardParent, isReference, isExpanded, canExpand)
                {
                    Height = TextEstimate.Padding
                        + Math.Max(TextEstimate.Target, TextEstimate.Lines(text(other.Label), PerLine(NameCharacter, isExpanded || canExpand ? Toggle : 0)) * lineHeight)
                        + (relations.Sum(relation => TextEstimate.Lines(text(relation.Label), PerLine(RelationCharacter, 0))) * lineHeight * 0.875)
                        + (isReference ? (lineHeight * 0.875) + 0.5 : 0),
                };
                wings[wing].Add(node);
                if (isExpanded)
                {
                    Place(other, node, depth + 1, wing);
                }
            }
        }

        static ConnectionBox Box(Node node) => new(
            node.Entity,
            node.Relations,
            node.Wing,
            node.Depth,
            node.Left,
            node.Top,
            ConnectionsChart.BoxWidth,
            node.Height,
            node.TowardBox,
            node.TowardParent,
            node.IsReference,
            node.IsExpanded,
            node.CanExpand);
    }

    // How many characters fit on a line of a box. Words do not break where a line ends, so a part of the line is
    // assumed to stay empty.
    private static int PerLine(double character, double taken) =>
        Math.Max(4, (int)((ConnectionsChart.BoxWidth - BoxPadding - taken) / character * 0.8));

    private sealed class Node(
        Entity entity,
        ImmutableArray<Relation> relations,
        ChartWing wing,
        int depth,
        Node? parent,
        bool towardBox,
        bool towardParent,
        bool isReference,
        bool isExpanded,
        bool canExpand)
    {
        public Entity Entity { get; } = entity;

        public ImmutableArray<Relation> Relations { get; } = relations;

        public ChartWing Wing { get; } = wing;

        public int Depth { get; } = depth;

        public Node? Parent { get; } = parent;

        public bool TowardBox { get; } = towardBox;

        public bool TowardParent { get; } = towardParent;

        public bool IsReference { get; } = isReference;

        public bool IsExpanded { get; } = isExpanded;

        public bool CanExpand { get; } = canExpand;

        public double Height { get; init; }

        public double Top { get; set; }

        public double Left { get; set; }
    }
}
