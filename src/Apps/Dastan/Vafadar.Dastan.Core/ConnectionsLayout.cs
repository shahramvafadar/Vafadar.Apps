using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>One row of a connections drawing: an entity with the relations that connect it to the row it hangs on.</summary>
/// <param name="Entity">The entity of the row.</param>
/// <param name="Relations">The relations between the entity and its parent in the drawing.</param>
/// <param name="Depth">1 for a neighbour of the focus, 2 for a neighbour of an expanded row, and so on.</param>
/// <param name="Top">The distance of the row from the top of the drawing, in em.</param>
/// <param name="Height">The estimated height of the row, in em.</param>
/// <param name="TowardRow">Whether a relation points from the parent to this entity.</param>
/// <param name="TowardParent">Whether a relation points from this entity to the parent.</param>
/// <param name="IsReference">Whether the entity is already drawn elsewhere; the row then only refers to it.</param>
/// <param name="IsExpanded">Whether the relations of the entity are drawn below it.</param>
/// <param name="CanExpand">Whether the entity has relations that are not drawn yet.</param>
public sealed record GraphRow(
    Entity Entity,
    ImmutableArray<Relation> Relations,
    int Depth,
    double Top,
    double Height,
    bool TowardRow,
    bool TowardParent,
    bool IsReference,
    bool IsExpanded,
    bool CanExpand);

/// <summary>The vertical line that the rows of one parent hang on.</summary>
/// <param name="Depth">The depth of the rows on the line.</param>
/// <param name="Top">Where the line starts, in em.</param>
/// <param name="Bottom">Where the line ends, in em.</param>
public sealed record GraphTrunk(int Depth, double Top, double Bottom);

/// <summary>
/// A neighbourhood laid out as a tidy tree: the focus at the top of the starting side, one row per neighbour, the neighbours of
/// an expanded row indented below it. The drawing knows no pixels and no markup; vertical measures are in em, the
/// horizontal position follows from the depth.
/// </summary>
/// <param name="Focus">The entity the drawing starts from.</param>
/// <param name="FocusTop">The distance of the focus from the top, in em.</param>
/// <param name="FocusHeight">The estimated height of the focus, in em.</param>
/// <param name="Height">The height of the whole drawing, in em.</param>
/// <param name="Rows">The rows from top to bottom.</param>
/// <param name="Trunks">The lines the rows hang on.</param>
public sealed record ConnectionsDrawing(
    Entity Focus,
    double FocusTop,
    double FocusHeight,
    double Height,
    ImmutableArray<GraphRow> Rows,
    ImmutableArray<GraphTrunk> Trunks)
{
    /// <summary>The distance from the top of a box to the connector that meets it, in em: the middle of its first line.</summary>
    public const double Anchor = 1.25;

    /// <summary>The share of the width the focus takes, in percent.</summary>
    public const double FocusWidth = 30;

    /// <summary>Returns where the line of the rows of a depth stands, in percent from the side where reading starts.</summary>
    public static double TrunkAt(int depth) => 34 + (10 * (Math.Min(depth, 4) - 1));

    /// <summary>Returns where the rows of a depth start, in percent; they reach to the other side.</summary>
    public static double BoxAt(int depth) => TrunkAt(depth) + 4;
}

/// <summary>Lays out a <see cref="ConnectionsView"/> for drawing.</summary>
public static class ConnectionsLayout
{
    private const double Gap = 0.75;
    // Padding and border of a box, in em.
    private const double Padding = 1.15;

    // The width of the drawing on the narrowest screen: 360 px without the margins of the page.
    private const double Narrowest = 328;

    // A name is a link, and a link that stands alone is at least as high as a finger needs (44 px at the base size).
    private const double Target = 2.75;

    /// <summary>
    /// Returns the drawing of a neighbourhood. Every relation of the view is drawn exactly once: where it leads to
    /// an entity that is already in the drawing, the row refers to that entity instead of drawing it again.
    /// </summary>
    /// <param name="view">The neighbourhood.</param>
    /// <param name="text">Returns a text in the language of the page; its length decides how many lines a box needs.</param>
    /// <param name="lineHeight">The height of a line of text in em (taller for Arabic script).</param>
    /// <remarks>
    /// The server cannot measure text. Heights are estimated for the narrowest screen (360 px); on a wider one a
    /// box has room to spare, never too little.
    /// </remarks>
    public static ConnectionsDrawing Of(ConnectionsView view, Func<LocalizedText, string> text, double lineHeight = 1.6)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);

        var expanded = view.Expanded.Select(entity => entity.Id).ToHashSet();
        var expandable = view.Expandable.Select(entity => entity.Id).ToHashSet();
        var drawnRelations = new HashSet<RelationId>();
        var drawnEntities = new HashSet<EntityId> { view.Focus.Id };
        var rows = ImmutableArray.CreateBuilder<GraphRow>();
        var trunks = ImmutableArray.CreateBuilder<GraphTrunk>();
        var y = 0.0;

        Place(view.Focus, 1, 0);

        // The focus stands at the top, beside its first neighbour. In the middle of its neighbours it would be out
        // of sight as soon as a few rows are open and the drawing is longer than the screen.
        const double focusTop = 0;
        // The focus has a heavier border and heavier letters than a row.
        var focusHeight = Padding + 0.25 + (Lines(text(view.Focus.Label), PerLine(ConnectionsDrawing.FocusWidth, 10, 0)) * lineHeight);
        return new ConnectionsDrawing(
            view.Focus,
            focusTop,
            focusHeight,
            Math.Max(Math.Max(0, y - Gap), focusTop + focusHeight),
            rows.ToImmutable(),
            trunks.ToImmutable());

        void Place(Entity parent, int depth, double from)
        {
            var groups = view.Edges
                .Where(edge => (edge.From.Id == parent.Id || edge.To.Id == parent.Id) && !drawnRelations.Contains(edge.Relation.Id))
                .GroupBy(edge => edge.From.Id == parent.Id ? edge.To : edge.From)
                .ToList();

            // The neighbours of this parent are drawn here, as its rows, even when an expanded row above them
            // leads to them as well – that row then refers to them.
            var fresh = groups.ToDictionary(group => group.Key.Id, group => drawnEntities.Add(group.Key.Id));
            double? first = null;
            var last = 0.0;
            foreach (var group in groups)
            {
                ImmutableArray<Relation> relations = [.. group.Select(edge => edge.Relation).Where(relation => drawnRelations.Add(relation.Id))];
                if (relations.IsEmpty)
                {
                    continue;
                }

                var other = group.Key;
                var isReference = !fresh[other.Id];
                var isExpanded = !isReference && expanded.Contains(other.Id);
                var canExpand = !isReference && expandable.Contains(other.Id);
                var width = 100 - ConnectionsDrawing.BoxAt(depth);

                // The name shares its line with the control that opens or closes the row; a row that refers to
                // another one says so on a line of its own.
                var height = Padding
                    + Math.Max(Target, Lines(text(other.Label), PerLine(width, 9.6, isExpanded || canExpand ? 52 : 0)) * lineHeight)
                    + (relations.Sum(relation => Lines(text(relation.Label), PerLine(width, 7.5, 0))) * lineHeight * 0.875)
                    + (isReference ? (lineHeight * 0.875) + 0.5 : 0);
                var row = new GraphRow(
                    other,
                    relations,
                    depth,
                    y,
                    height,
                    relations.Any(relation => relation.Type.Directionality == Directionality.Directed && relation.From == parent.Id && relation.To == other.Id && other.Id != parent.Id),
                    relations.Any(relation => relation.Type.Directionality == Directionality.Directed && relation.From == other.Id && other.Id != parent.Id),
                    isReference,
                    isExpanded,
                    canExpand);
                rows.Add(row);
                first ??= y + ConnectionsDrawing.Anchor;
                last = y + ConnectionsDrawing.Anchor;
                y += height + Gap;
                if (row.IsExpanded)
                {
                    Place(other, depth + 1, row.Top + row.Height);
                }
            }

            if (first is not null)
            {
                trunks.Add(new GraphTrunk(depth, depth == 1 ? first.Value : from, last));
            }
        }
    }

    // How many characters fit on a line of a box that takes a share of the width, for an average character width
    // in px. Words do not break where a line ends, so a part of the line is assumed to stay empty.
    private static int PerLine(double widthPercent, double characterWidth, double taken) =>
        Math.Max(4, (int)((((Narrowest * widthPercent / 100) - 26 - taken) / characterWidth) * 0.8));

    private static int Lines(string text, int perLine) => Math.Max(1, (int)Math.Ceiling(text.Length / (double)perLine));
}
