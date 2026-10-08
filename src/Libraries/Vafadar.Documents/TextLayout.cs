namespace Vafadar.Documents;

/// <summary>A recognised or extracted word with its box, in any unit that grows downwards and to the right.</summary>
/// <param name="Top">The upper edge.</param>
/// <param name="Bottom">The lower edge.</param>
/// <param name="Left">The left edge.</param>
/// <param name="Text">The text of the word (or of a whole line when the source has no words).</param>
public readonly record struct LayoutWord(double Top, double Bottom, double Left, string Text)
{
    /// <summary>Gets the right edge, when supplied by the source.</summary>
    public double? Right { get; init; }

    /// <summary>Gets the zero-based page, kept separate even when coordinates overlap.</summary>
    public int Page { get; init; }

    /// <summary>Gets the source line identity, scoped to its block and page.</summary>
    public int? LineId { get; init; }

    /// <summary>Gets the source block identity, when supplied by the engine.</summary>
    public int? BlockId { get; init; }

    /// <summary>Gets the baseline angle in degrees, positive when it slopes down towards the right.</summary>
    public double? Angle { get; init; }

    /// <summary>Gets the engine's original text rotation when its returned boxes were already rotated upright.</summary>
    public double? SourceAngle { get; init; }

    /// <summary>Gets the engine's character-recognition confidence, when available; never a semantic confidence.</summary>
    public double? Confidence { get; init; }

    /// <summary>Gets the vertical centre.</summary>
    public double Centre => (Top + Bottom) / 2;
}

/// <summary>Reconstructed text and whether legacy column geometry could have paired adjacent rows incorrectly.</summary>
public sealed record TextLayoutResult(string? Text, bool IsAmbiguous = false);

/// <summary>Rebuilds the text rows of a page from word boxes.</summary>
public static class TextLayout
{
    /// <summary>
    /// Builds the text rows of a page from words. OCR engines and PDF files return text in blocks or columns, so a price
    /// can end up far from its label; words whose vertical centres lie within half a line height form one row, read in
    /// reading order (by <see cref="LayoutWord.Left"/>, right to left for rows in Arabic script).
    /// </summary>
    /// <returns>The rows separated by line feeds, or <see langword="null"/> when there are no words.</returns>
    public static string? Rows(IEnumerable<LayoutWord> words) => Reconstruct(words).Text;

    /// <summary>
    /// Reconstructs source lines first, then aligns separate columns using the source angle and full boxes. Metadata
    /// is optional: legacy boxes remain supported, but suspicious staggered columns are reported as ambiguous.
    /// </summary>
    public static TextLayoutResult Reconstruct(IEnumerable<LayoutWord> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        var pages = words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).GroupBy(w => w.Page).OrderBy(g => g.Key);
        var text = new List<string>();
        var ambiguous = false;
        foreach (var page in pages)
        {
            var items = page.ToArray();
            var angles = items.Where(w => w.Angle is not null).Select(w => w.Angle!.Value).Order().ToArray();
            var slope = angles.Length > 0 ? Math.Tan(angles[angles.Length / 2] * Math.PI / 180) : 0;
            double Centre(LayoutWord word) => word.Centre - word.Left * slope;
            var fragments = items.Where(w => w.LineId is not null).GroupBy(w => (w.BlockId, w.LineId))
                .Select(g => g.ToList()).Concat(items.Where(w => w.LineId is null).Select(w => new List<LayoutWord> { w }))
                .OrderBy(g => g.Average(Centre)).ToArray();
            var rows = new List<List<LayoutWord>>();
            foreach (var fragment in fragments)
            {
                var centre = fragment.Average(Centre);
                var height = fragment.Max(w => w.Bottom - w.Top);
                // Compare against the existing row's height too: a comma's very short box still belongs to its digits.
                var row = rows.LastOrDefault(r => Math.Abs(centre - r.Average(Centre)) < Math.Max(height, r.Max(w => w.Bottom - w.Top)) / 2);
                if (row is null)
                {
                    rows.Add(fragment);
                }
                else
                {
                    row.AddRange(fragment);
                }
            }

            // With no angle/line relationships a shifted price column is indistinguishable from a neighbouring row.
            // Expose that uncertainty rather than allowing the app to call a tax/total pairing certain.
            if (angles.Length == 0 && items.All(w => w.LineId is null))
            {
                ambiguous |= HasStaggeredColumns(rows);
            }

            text.Add(string.Join('\n', rows.OrderBy(r => r.Average(Centre)).Select(Join)));
        }

        // Form feed is an explicit page boundary; a total label cannot borrow a number from another page.
        return new TextLayoutResult(text.Count == 0 ? null : string.Join('\f', text), ambiguous);
    }

    private static bool HasStaggeredColumns(List<List<LayoutWord>> rows)
    {
        var pairs = rows.Where(r => r.Any(w => w.Text.Any(char.IsLetter)) && r.Any(w => w.Text.Any(char.IsDigit))
            && r.Max(w => w.Left) - r.Min(w => w.Left) > 10 * r.Max(w => w.Bottom - w.Top)).ToArray();
        return pairs.Length >= 2 && rows.FirstOrDefault() is { } first && first.All(w => !w.Text.Any(char.IsDigit))
            && rows.LastOrDefault() is { } last && last.All(w => !w.Text.Any(char.IsLetter));
    }

    private static string Join(List<LayoutWord> row)
    {
        // Keep numbers internally left-to-right, including when the surrounding labels are Persian.
        var ordered = row.OrderBy(w => w.Left).ToArray();
        var parts = new List<string>();
        for (var i = 0; i < ordered.Length; i++)
        {
            var word = ordered[i];
            if (i > 0 && ordered[i - 1].Right is { } right)
            {
                var previous = ordered[i - 1];
                var gap = word.Left - right;
                var charWidth = Math.Max(1, (right - previous.Left) / Math.Max(1, previous.Text.Length));
                var punctuation = word.Text.Trim() is "," or "." or "٫" or "٬" or "،";
                var followsSeparator = previous.Text.TrimEnd().LastOrDefault() is ',' or '.' or '٫' or '٬' or '،';
                if (gap >= -charWidth && gap <= charWidth && (punctuation && previous.Text.All(char.IsDigit)
                    || followsSeparator && word.Text.All(char.IsDigit)))
                {
                    parts[^1] += word.Text.Trim();
                    continue;
                }
            }

            parts.Add(word.Text.Trim());
        }

        return string.Join(' ', IsRightToLeft(row) ? parts.AsEnumerable().Reverse() : parts);
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
