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
