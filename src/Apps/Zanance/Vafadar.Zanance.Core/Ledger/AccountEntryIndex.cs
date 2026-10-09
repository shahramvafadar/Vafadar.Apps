namespace Vafadar.Zanance.Core.Ledger;

/// <summary>
/// Routes one unchanged ledger snapshot to its account slices, keeping original order and entry identity.
/// Balances still use <see cref="LedgerCalculator.Balance"/>; no financial formula is copied here (D-75).
/// </summary>
public sealed class AccountEntryIndex
{
    private readonly ILookup<Guid, LedgerEntry> _entries;

    /// <summary>Enumerates the snapshot once; transfers belong to both endpoints without becoming two entries.</summary>
    public AccountEntryIndex(IEnumerable<LedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = Touches(entries).ToLookup(pair => pair.Account, pair => pair.Entry);
    }

    /// <summary>Returns the relevant original entries in original order, or an empty slice for an unused account.</summary>
    public IEnumerable<LedgerEntry> For(Guid accountId) => _entries[accountId];

    private static IEnumerable<(Guid Account, LedgerEntry Entry)> Touches(IEnumerable<LedgerEntry> entries)
    {
        foreach (var entry in entries)
        {
            yield return (entry.AccountId, entry);
            // EffectOn already handles every financial kind and a legacy self-transfer; never double that effect.
            if (entry.ToAccountId is { } destination && destination != entry.AccountId)
            {
                yield return (destination, entry);
            }
        }
    }
}
