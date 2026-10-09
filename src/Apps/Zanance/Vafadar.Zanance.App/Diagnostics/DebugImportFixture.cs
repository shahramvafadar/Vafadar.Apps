#if DEBUG
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Fictitious September import rows, shared by the explicitly requested development checks only.</summary>
internal static class DebugImportFixture
{
    /// <summary>Builds unsaved detail rows for the 412/395/17 acceptance scenario, preserving optional stable ids.</summary>
    internal static List<LedgerEntry> Details(LedgerEntry aggregate, IReadOnlyList<Guid>? ids = null) =>
        new long[] { 6000, 7000, 6500, 8000, 5500, 6500 }.Select((amount, i) => new LedgerEntry(ids?[i] ?? Guid.CreateVersion7())
        {
            AccountId = aggregate.AccountId, CategoryId = aggregate.CategoryId, Kind = EntryKind.Expense, Amount = amount,
            Date = new DateOnly(2026, 9, 3 + (i * 4)), Title = $"Fictitious detail {i + 1}", Source = EntrySource.Import,
        }).ToList();
}
#endif
