namespace Vafadar.Zanance.Core.DataFiles;

/// <summary>Technical complete-review budgets, independent of commercial quotas; exceeding one refuses the whole input.</summary>
public sealed record CsvReadLimits
{
    /// <summary>Gets the ordinary interactive-import budgets, including room for the reference 100,000-row export.</summary>
    public static CsvReadLimits Import { get; } = new();
    /// <summary>Gets the maximum original stream size; source Length is never trusted.</summary>
    public int MaximumBytes { get; init; } = 64 * 1024 * 1024;
    /// <summary>Gets the maximum decoded UTF-16 character count.</summary>
    public int MaximumCharacters { get; init; } = 64 * 1024 * 1024;
    /// <summary>Gets the maximum retained rows, including the header.</summary>
    public int MaximumRows { get; init; } = 250001;
    /// <summary>Gets the maximum columns in one row.</summary>
    public int MaximumColumns { get; init; } = 1024;
    /// <summary>Gets the maximum total cells, preventing delimiter-heavy inputs from exhausting memory.</summary>
    public int MaximumCells { get; init; } = 4000000;
    /// <summary>Gets the maximum characters in one original field, without truncation.</summary>
    public int MaximumFieldCharacters { get; init; } = 65536;

    /// <summary>Refuses an invalid resource policy before input allocation.</summary>
    internal void Validate()
    {
        if (MaximumBytes <= 0 || MaximumCharacters <= 0 || MaximumRows <= 0 || MaximumColumns <= 0
            || MaximumCells <= 0 || MaximumFieldCharacters <= 0)
            throw new ArgumentOutOfRangeException(nameof(CsvReadLimits), "CSV resource budgets must be positive.");
    }
}

/// <summary>The explicit resource bound that refused a complete CSV input.</summary>
public enum CsvReadLimit
{
    /// <summary>Original stream bytes.</summary>
    Bytes,
    /// <summary>Decoded characters.</summary>
    Characters,
    /// <summary>Retained rows including the header.</summary>
    Rows,
    /// <summary>Columns in one row.</summary>
    Columns,
    /// <summary>Total parsed cells, including blank-record work.</summary>
    Cells,
    /// <summary>Original characters in one field.</summary>
    FieldCharacters,
}

/// <summary>Refuses an entire input without exposing its contents or silently truncating a financial record.</summary>
public sealed class CsvReadLimitException(CsvReadLimit limit, int maximum)
    : FormatException("The CSV input exceeds a technical resource limit.")
{
    /// <summary>Gets the exceeded budget kind.</summary>
    public CsvReadLimit Limit { get; } = limit;
    /// <summary>Gets its configured upper bound.</summary>
    public int Maximum { get; } = maximum;
}

/// <summary>Refuses a structurally incomplete CSV rather than treating its unfinished tail as financial fields.</summary>
public sealed class CsvMalformedInputException() : FormatException("The CSV contains an unterminated quoted field.");
