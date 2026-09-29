namespace Vafadar.Documents;

/// <summary>A recognised or extracted word with its box, in any unit that grows downwards and to the right.</summary>
/// <param name="Top">The upper edge.</param>
/// <param name="Bottom">The lower edge.</param>
/// <param name="Left">The left edge.</param>
/// <param name="Text">The text of the word (or of a whole line when the source has no words).</param>
public readonly record struct LayoutWord(double Top, double Bottom, double Left, string Text)
{
    /// <summary>Gets the vertical centre.</summary>
    public double Centre => (Top + Bottom) / 2;
}

/// <summary>Rebuilds the text rows of a page from word boxes.</summary>
public static class TextLayout
{
    /// <summary>
    /// Builds the text rows of a page from words. OCR engines and PDF files return text in blocks or columns, so a price
    /// can end up far from its label; words whose vertical centres lie within half a line height form one row, read in
    /// reading order (by <see cref="LayoutWord.Left"/>, right to left for rows in Arabic script).
    /// </summary>
    /// <returns>The rows separated by line feeds, or <see langword="null"/> when there are no words.</returns>
    public static string? Rows(IEnumerable<LayoutWord> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        var rows = new List<List<LayoutWord>>();
        foreach (var word in words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.Centre))
        {
            var height = Math.Max(word.Bottom - word.Top, double.Epsilon);
            var row = rows.LastOrDefault();
            if (row is not null && Math.Abs(word.Centre - row.Average(w => w.Centre)) < height / 2)
            {
                row.Add(word);
            }
            else
            {
                rows.Add([word]);
            }
        }

        return rows.Count == 0 ? null : string.Join('\n', rows.Select(r => string.Join(' ', (IsRightToLeft(r) ? r.OrderByDescending(w => w.Left) : r.OrderBy(w => w.Left)).Select(w => w.Text.Trim()))));
    }

    /// <summary>
    /// Chooses the pages of a long document that are read: the first pages and always the last one, where invoices and
    /// statements put their totals.
    /// </summary>
    /// <param name="pageCount">The number of pages in the document.</param>
    /// <param name="maxPages">The most pages to read (at least 1).</param>
    /// <returns>Zero-based page indexes in document order.</returns>
    public static IReadOnlyList<int> PagesToRead(int pageCount, int maxPages)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPages, 1);
        if (pageCount <= 0)
        {
            return [];
        }

        return pageCount <= maxPages
            ? [.. Enumerable.Range(0, pageCount)]
            : [.. Enumerable.Range(0, maxPages - 1), pageCount - 1];
    }

    /// <summary>Gets a value indicating whether a text holds enough letters or digits to be worth reading.</summary>
    public static bool HasContent(string? text) => text is not null && text.Count(char.IsLetterOrDigit) >= 12;

    // A row written mostly in Arabic script (Persian) is read from right to left.
    private static bool IsRightToLeft(List<LayoutWord> row)
    {
        var text = string.Concat(row.Select(w => w.Text));
        return text.Count(c => c is >= '؀' and <= 'ۿ') > text.Count(char.IsAsciiLetter);
    }
}
