namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Derived protected row location; original authenticated ciphertext remains the only financial body archive.</summary>
internal sealed class HouseholdLocalFinancialReference
{
    /// <summary>Exact original global financial version, independent of ciphertext publication count.</summary>
    public long Version { get; set; }
    /// <summary>Original schema table, never interpolated as SQL.</summary>
    public string Table { get; set; } = "";
    /// <summary>Original canonical financial identity.</summary>
    public string Key { get; set; } = "";
    /// <summary>Previously completely verified publication.</summary>
    public long Cursor { get; set; }
    /// <summary>Standalone event or first stream event containing this original row.</summary>
    public int EventIndex { get; set; }
    /// <summary>Absolute JSON body offset in the framed stream; negative one denotes a standalone record.</summary>
    public long Offset { get; set; }
    /// <summary>Exact framed JSON length; zero for a standalone record.</summary>
    public int Length { get; set; }
}
