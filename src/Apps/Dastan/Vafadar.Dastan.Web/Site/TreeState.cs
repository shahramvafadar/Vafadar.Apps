using System.Globalization;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// How a visitor looks at a family tree: how many generations above and below the one it is about and how far
/// beside its line, or the whole family, and whether generations run down the page or across it. Like the state of a chart it lives in the
/// address (ADR 0008): a link changes it and Back undoes it. It never decides what may be seen – the projection
/// answers for the reading position of the visitor, whatever the address asks for.
/// </summary>
public sealed record TreeState
{
    /// <summary>The number of generations a tree opens with, above and below.</summary>
    public const int DefaultReach = 2;

    /// <summary>The most generations an address may ask for in one direction.</summary>
    public const int FurthestReach = 12;

    /// <summary>Gets the number of generations of ancestors.</summary>
    public int Up { get; init; } = DefaultReach;

    /// <summary>Gets the number of generations of descendants.</summary>
    public int Down { get; init; } = DefaultReach;

    /// <summary>Gets how far the tree is opened beside the line: brothers and sisters, then their children, and so on.</summary>
    public int Beside { get; init; }

    /// <summary>Gets a value indicating whether everybody connected by family relations is shown instead of a line.</summary>
    public bool Whole { get; init; }

    /// <summary>Gets a value indicating whether generations run across the page instead of down it.</summary>
    public bool Across { get; init; }

    /// <summary>Reads the state from the values of a query. A value that is not one of the choices is not a choice.</summary>
    public static TreeState Read(string? up, string? down, string? beside, string? family, string? view) => new()
    {
        Up = Reach(up, DefaultReach),
        Down = Reach(down, DefaultReach),
        Beside = Reach(beside, 0),
        Whole = family == "whole",
        Across = view == "across",
    };

    /// <summary>Returns the query of the state, without what is the default.</summary>
    public string Query()
    {
        var parts = new List<string>(4);
        if (Whole)
        {
            parts.Add("family=whole");
        }
        else
        {
            if (Up != DefaultReach)
            {
                parts.Add("up=" + Up.ToString(CultureInfo.InvariantCulture));
            }

            if (Down != DefaultReach)
            {
                parts.Add("down=" + Down.ToString(CultureInfo.InvariantCulture));
            }

            if (Beside != 0)
            {
                parts.Add("beside=" + Beside.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (Across)
        {
            parts.Add("view=across");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    private static int Reach(string? value, int otherwise) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var reach) && reach <= FurthestReach ? reach : otherwise;
}
