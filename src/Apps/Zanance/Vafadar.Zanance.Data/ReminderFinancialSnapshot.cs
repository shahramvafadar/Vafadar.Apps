using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original financial inputs needed by one actual-file reminder preparation, independent of work selection.</summary>
public sealed class ReminderFinancialSnapshot
{
    /// <summary>Retains complete original calculation inputs and their value witness for one owned read.</summary>
    internal ReminderFinancialSnapshot(string path, DateOnly day, bool goalFinancials, bool quantityFinancials,
        List<Account> accounts, List<LedgerEntry> entries, List<AssetEvent> events, List<AssetType> types)
    {
        DatabasePath = path; Day = day; IncludesGoalFinancials = goalFinancials; IncludesQuantityFinancials = quantityFinancials;
        Accounts = accounts; Entries = entries; Events = events; Types = types;
        InputStamp = Capture(accounts, entries, events, types);
    }
    /// <summary>Gets every original account without selection or lifecycle filtering.</summary>
    public IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets original entries through the calculation date when goal progress is needed; no financial rule is duplicated.</summary>
    public IReadOnlyList<LedgerEntry> Entries { get; }
    /// <summary>Gets original quantity movements through the calculation date when quantity progress is needed.</summary>
    public IReadOnlyList<AssetEvent> Events { get; }
    /// <summary>Gets complete original holding-type metadata needed by quantity progress.</summary>
    public IReadOnlyList<AssetType> Types { get; }
    /// <summary>Gets the exact original calculation date; no system clock is changed.</summary>
    public DateOnly Day { get; }
    /// <summary>Gets the actual original file; it is neither portable nor an authorization token.</summary>
    internal string DatabasePath { get; }
    /// <summary>Gets whether the original contribution rules require dated ledger inputs.</summary>
    internal bool IncludesGoalFinancials { get; }
    /// <summary>Gets whether original quantity goals additionally require dated holding inputs.</summary>
    internal bool IncludesQuantityFinancials { get; }
    /// <summary>Gets the complete value witness, independent of audit timestamps or activity choices.</summary>
    internal string InputStamp { get; }
    /// <summary>Captures complete original financial values in stable identity order without changing them.</summary>
    internal static string Capture(IEnumerable<Account> accounts, IEnumerable<LedgerEntry> entries,
        IEnumerable<AssetEvent> events, IEnumerable<AssetType> types) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { Accounts = accounts.OrderBy(item => item.Id), Entries = entries.OrderBy(item => item.Id),
                Events = events.OrderBy(item => item.Id), Types = types.OrderBy(item => item.Id) }))));
}
