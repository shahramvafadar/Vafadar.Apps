using System.Text;

namespace Vafadar.Finance.Core.DataFiles;

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
    public static IReadOnlyList<IReadOnlyList<string>> Read(string text, char separator)
    {
        ArgumentNullException.ThrowIfNull(text);
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        var start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (c == separator)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                cell.Clear();
                if (row.Count > 1 || row[0].Length > 0)
                {
                    rows.Add(row);
                }

                row = [];
            }
            else
            {
                cell.Append(c);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>Removes the protective apostrophe added by <see cref="Text"/>.</summary>
    public static string Unprotect(string value) =>
        value.Length > 1 && value[0] == '\'' && value[1] is '=' or '+' or '-' or '@' or '\t' or '\r' ? value[1..] : value;
}
