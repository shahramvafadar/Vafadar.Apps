using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>
/// How much room a text needs in a box of a drawing. The server cannot measure text, so the estimate is made for the
/// narrowest screen (360 px): on a wider one a box has room to spare, never too little.
/// </summary>
internal static class TextEstimate
{
    /// <summary>Padding and border of a box, in em.</summary>
    public const double Padding = 1.15;

    /// <summary>The least height of a name that is a link: what a finger needs (44 px at the base size), in em.</summary>
    public const double Target = 2.75;

    // The width of a drawing on the narrowest screen: 360 px without the margins of the page.
    private const double Narrowest = 328;

    /// <summary>
    /// Returns how many characters fit on a line of a box that takes a share of the width, for an average character
    /// width in px. Words do not break where a line ends, so a part of the line is assumed to stay empty.
    /// </summary>
    public static int PerLine(double widthPercent, double characterWidth, double taken = 0) =>
        Math.Max(4, (int)((((Narrowest * widthPercent / 100) - 26 - taken) / characterWidth) * 0.8));

    /// <summary>Returns the number of lines a text needs.</summary>
    public static int Lines(string text, int perLine) => Math.Max(1, (int)Math.Ceiling(text.Length / (double)perLine));
}

/// <summary>The box of a member in a family drawing.</summary>
/// <param name="Entity">The member.</param>
/// <param name="Generation">The row of the member: 0 for those without a visible parent.</param>
/// <param name="X">The distance from the side where reading starts, in percent.</param>
/// <param name="Width">The width, in percent.</param>
/// <param name="Top">The distance from the top, in em.</param>
/// <param name="Height">The estimated height, in em.</param>
public sealed record FamilyBox(Entity Entity, int Generation, double X, double Width, double Top, double Height);

/// <summary>A line of a family drawing: from a parent down to a child, or between two partners.</summary>
/// <param name="Relation">The relation the line stands for.</param>
/// <param name="IsParentage">Whether the line leads from a parent to a child; otherwise it joins partners.</param>
/// <param name="X1">Where the line starts, in percent.</param>
/// <param name="Y1">Where the line starts, in em.</param>
/// <param name="X2">Where the line ends, in percent.</param>
/// <param name="Y2">Where the line ends, in em.</param>
public sealed record FamilyLink(Edge Relation, bool IsParentage, double X1, double Y1, double X2, double Y2);

/// <summary>A family laid out in rows of generations. Vertical measures are in em, horizontal ones in percent.</summary>
/// <param name="Height">The height of the drawing, in em.</param>
/// <param name="Boxes">The members, generation by generation.</param>
/// <param name="Links">The lines between them.</param>
public sealed record FamilyDrawing(double Height, ImmutableArray<FamilyBox> Boxes, ImmutableArray<FamilyLink> Links);

/// <summary>Lays out a <see cref="FamilyView"/> for drawing.</summary>
public static class FamilyLayout
{
    // More than three boxes do not fit beside each other on a phone; a larger generation continues on the next line.
    private const int BesideEachOther = 3;
    private const double Between = 4;
    private const double LineGap = 0.75;
    private const double GenerationGap = 2.5;

    /// <summary>
    /// Returns the drawing of a family: parents above their children, the members of a generation beside each
    /// other. The order inside a generation is the order of the collection; nothing is rearranged to make lines
    /// shorter, so a line may cross another in a large family.
    /// </summary>
    /// <param name="view">The family.</param>
    /// <param name="text">Returns a text in the language of the page; its length decides how many lines a box needs.</param>
    /// <param name="lineHeight">The height of a line of text in em (taller for Arabic script).</param>
    public static FamilyDrawing Of(FamilyView view, Func<LocalizedText, string> text, double lineHeight = 1.6)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);

        var boxes = ImmutableArray.CreateBuilder<FamilyBox>();
        var y = 0.0;
        foreach (var generation in view.Members.GroupBy(member => member.Generation).OrderBy(group => group.Key))
        {
            foreach (var line in generation.Chunk(BesideEachOther))
            {
                var slot = 100.0 / line.Length;
                var width = slot - Between;
                var height = line.Max(member => TextEstimate.Padding
                    + Math.Max(TextEstimate.Target, TextEstimate.Lines(text(member.Entity.Label), TextEstimate.PerLine(width, 9.6)) * lineHeight));
                for (var index = 0; index < line.Length; index++)
                {
                    boxes.Add(new FamilyBox(line[index].Entity, generation.Key, (slot * index) + (Between / 2), width, y, height));
                }

                y += height + LineGap;
            }

            y += GenerationGap - LineGap;
        }

        var placed = boxes.ToDictionary(box => box.Entity.Id);
        var links = ImmutableArray.CreateBuilder<FamilyLink>();
        foreach (var edge in view.Parentage)
        {
            // From the middle of the parent's lower edge to just above the middle of the child's upper edge.
            var (parent, child) = (placed[edge.From.Id], placed[edge.To.Id]);
            links.Add(new FamilyLink(edge, true, Middle(parent), parent.Top + parent.Height, Middle(child), child.Top - 0.3));
        }

        foreach (var edge in view.Partnerships)
        {
            var (first, second) = (placed[edge.From.Id], placed[edge.To.Id]);
            if (first.X > second.X)
            {
                (first, second) = (second, first);
            }

            // Partners in one line are joined side to side; otherwise from the lower edge of one to the upper edge of the other.
            links.Add(first.Top == second.Top
                ? new FamilyLink(edge, false, first.X + first.Width, first.Top + (first.Height / 2), second.X, second.Top + (second.Height / 2))
                : new FamilyLink(edge, false, Middle(first), Math.Min(first.Top + first.Height, second.Top + second.Height), Middle(second), Math.Max(first.Top, second.Top)));
        }

        return new FamilyDrawing(Math.Max(0, y - GenerationGap), boxes.ToImmutable(), links.ToImmutable());

        static double Middle(FamilyBox box) => box.X + (box.Width / 2);
    }
}
