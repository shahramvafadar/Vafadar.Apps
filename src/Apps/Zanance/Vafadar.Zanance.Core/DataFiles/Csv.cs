using System.Text;

namespace Vafadar.Zanance.Core.DataFiles;

/// <summary>
/// Minimal RFC 4180 CSV reading and writing: quotes, embedded separators, line breaks and Unicode are preserved
/// (IO-06). Text cells that a spreadsheet would run as a formula are neutralised on export (AT-55).
/// </summary>
public static class Csv
{
    /// <summary>Returns a cell for export; text starting with a formula character gets a leading apostrophe.</summary>
    public static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
    }

    /// <summary>Encodes format-three text reversibly, including an original leading apostrophe.</summary>
    public static string LiteralText(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Doubling an original apostrophe distinguishes it from the spreadsheet protection marker.
        return value[0] == '\'' || NeedsLiteralProtection(value.AsSpan()) ? "'" + value : value;
    }

    /// <summary>Decodes only format-three text; older files retain their original protection contract.</summary>
    public static string UnprotectLiteralText(string value) =>
        value.Length > 1 && value[0] == '\'' && (value[1] == '\'' || NeedsLiteralProtection(value.AsSpan(1)))
            ? value[1..] : value;

    /// <summary>Refuses unknown own-file text contracts instead of guessing the account or metadata meaning.</summary>
    public static bool UsesLiteralText(string version) => version.Trim() switch
    {
        "3" => true,
        "" or "1" or "2" => false,
        _ => throw new FormatException("Unsupported CSV text format version."),
    };

    /// <summary>Finds the unique version marker even when a later producer appends optional columns.</summary>
    internal static int TextVersionColumn(IReadOnlyList<string> header)
    {
        var found = -1;
        for (var index = 0; index < header.Count; index++)
        {
            if (!string.Equals(header[index].Trim(), "format_version", StringComparison.OrdinalIgnoreCase)) continue;
            if (found >= 0) throw new FormatException("Ambiguous CSV text format version columns.");
            found = index;
        }
        return found;
    }

    private static bool NeedsLiteralProtection(ReadOnlySpan<char> value)
    {
        if (!value.IsEmpty && value[0] is '\t' or '\r' or '\n') return true;
        var visible = value.TrimStart();
        return !visible.IsEmpty && visible[0] is '=' or '+' or '-' or '@';
    }

    /// <summary>Writes rows with <paramref name="separator"/>, quoting cells when needed, CRLF line ends.</summary>
    public static string Write(IEnumerable<IReadOnlyList<string>> rows, char separator = ',')
    {
        ArgumentNullException.ThrowIfNull(rows);
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(separator);
                }

                var cell = row[i] ?? string.Empty;
                if (cell.IndexOfAny([separator, '"', '\r', '\n']) >= 0)
                {
                    builder.Append('"').Append(cell.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
                }
                else
                {
                    builder.Append(cell);
                }
            }

            builder.Append("\r\n");
        }

        return builder.ToString();
    }

    /// <summary>Guesses the separator from the first line: comma, semicolon or tab – the most frequent outside quotes.</summary>
    public static char DetectSeparator(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int comma = 0, semicolon = 0, tab = 0;
        var quoted = false;
        foreach (var c in text)
        {
            if (c == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted)
            {
                if (c is '\r' or '\n')
                {
                    break;
                }

                comma += c == ',' ? 1 : 0;
                semicolon += c == ';' ? 1 : 0;
                tab += c == '\t' ? 1 : 0;
            }
        }

        return tab > comma && tab > semicolon ? '\t' : semicolon > comma ? ';' : ',';
    }

    /// <summary>Reads all rows; quoted cells may contain separators, quotes ("") and line breaks. Empty lines are skipped.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> Read(string text, char separator, CsvReadLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var budget = limits ?? CsvReadLimits.Import; budget.Validate();
        if (text.Length > budget.MaximumCharacters) throw new CsvReadLimitException(CsvReadLimit.Characters, budget.MaximumCharacters);
        var cells = 0;
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        var start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;
        void Append(char value)
        {
            if (cell.Length >= budget.MaximumFieldCharacters) throw new CsvReadLimitException(CsvReadLimit.FieldCharacters, budget.MaximumFieldCharacters);
            cell.Append(value);
        }
        void EndCell()
        {
            if (row.Count >= budget.MaximumColumns) throw new CsvReadLimitException(CsvReadLimit.Columns, budget.MaximumColumns);
            if (cells >= budget.MaximumCells) throw new CsvReadLimitException(CsvReadLimit.Cells, budget.MaximumCells);
            cells++; row.Add(cell.ToString()); cell.Clear();
        }
        void EndRow()
        {
            if (rows.Count >= budget.MaximumRows) throw new CsvReadLimitException(CsvReadLimit.Rows, budget.MaximumRows);
            rows.Add(row);
        }


        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                        // IO-08 / D-387: joining a quoted amount to trailing digits silently changes its value.
                        if (i + 1 < text.Length && text[i + 1] != separator && text[i + 1] is not '\r' and not '\n')
                            throw new CsvMalformedInputException();
                    }
                }
                else
                {
                    Append(c);
                }
            }
            else if (c == '"')
            {
                if (cell.Length != 0) throw new CsvMalformedInputException();
                quoted = true;
            }
            else if (c == separator)
            {
                EndCell();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                EndCell();
                if (row.Count > 1 || row[0].Length > 0)
                {
                    EndRow();
                }

                row = [];
            }
            else
            {
                Append(c);
            }
        }

        if (quoted) throw new CsvMalformedInputException();
        if (cell.Length > 0 || row.Count > 0)
        {
            EndCell();
            EndRow();
        }

        return rows;
    }

    /// <summary>Checks the decoded size before detecting the separator and parsing a complete selected file.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> ReadDetected(string text, CsvReadLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var budget = limits ?? CsvReadLimits.Import; budget.Validate();
        if (text.Length > budget.MaximumCharacters) throw new CsvReadLimitException(CsvReadLimit.Characters, budget.MaximumCharacters);
        return Read(text, DetectSeparator(text), budget);
    }

    /// <summary>Removes the protective apostrophe added by <see cref="Text"/>.</summary>
    public static string Unprotect(string value) =>
        value.Length > 1 && value[0] == '\'' && value[1] is '=' or '+' or '-' or '@' or '\t' or '\r' ? value[1..] : value;
}
