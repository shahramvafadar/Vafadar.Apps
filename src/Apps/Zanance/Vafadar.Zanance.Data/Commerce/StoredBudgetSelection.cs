using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Canonical budget definitions in the existing scoped choice row; never reinterpret them as entity ids.</summary>
internal static class StoredBudgetSelection
{
    /// <summary>Checks the distinct definition encoding without blocking retained corrections on corrupt input.</summary>
    internal static bool IsValid(StoredResourceSelection row)
    {
        try { _ = Parse(row); return true; }
        catch (Exception exception) when (exception is FormatException or ArgumentException or InvalidOperationException or OverflowException)
        { return false; }
    }
    /// <summary>Reads a valid definition choice; malformed stored choices grant no bounded new work.</summary>
    internal static BudgetSelection ForNewWork(StoredResourceSelection row) => IsValid(row) ? Parse(row)
        : new(new(row.ScopeKind, row.ScopeId), []);

    private static BudgetSelection Parse(StoredResourceSelection row)
    {
        if (row.Kind != QuotaKind.BudgetDefinitions || row.Revision == Guid.Empty)
            throw new InvalidOperationException("The budget choice is invalid.");
        return BudgetSelection.Parse(new(row.ScopeKind, row.ScopeId), row.IdentitySet);
    }

    /// <summary>Changes the row only when the explicit canonical value changes, preserving no-op revisions.</summary>
    internal static void Replace(StoredResourceSelection row, BudgetSelection selection)
    {
        if (row.IdentitySet == selection.Value && IsValid(row)) return;
        row.IdentitySet = selection.Value;
        row.Revision = Guid.NewGuid();
    }
}
