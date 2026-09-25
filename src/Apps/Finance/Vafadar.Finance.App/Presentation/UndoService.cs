using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Data;

namespace Vafadar.Finance.App.Presentation;

/// <summary>
/// Keeps the most recently deleted entries for a short time so that the list can offer "Undo" (TX-05).
/// </summary>
public sealed class UndoService(FinanceStore store, TimeProvider time)
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(8);
    private IReadOnlyList<LedgerEntry> _deleted = [];
    private DateTimeOffset _deletedAt;

    /// <summary>Raised when the undo offer appears or disappears.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets a value indicating whether an undo is currently offered.</summary>
    public bool CanUndo => _deleted.Count > 0 && time.GetUtcNow() - _deletedAt < Window;

    /// <summary>Gets how long the offer is still valid.</summary>
    public TimeSpan Remaining => CanUndo ? Window - (time.GetUtcNow() - _deletedAt) : TimeSpan.Zero;

    /// <summary>Offers to restore <paramref name="deleted"/>.</summary>
    public void Offer(IReadOnlyList<LedgerEntry> deleted)
    {
        _deleted = deleted;
        _deletedAt = time.GetUtcNow();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Withdraws the offer.</summary>
    public void Dismiss()
    {
        if (_deleted.Count > 0)
        {
            _deleted = [];
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Restores the deleted entries.</summary>
    public async Task UndoAsync()
    {
        var entries = _deleted;
        _deleted = [];
        Changed?.Invoke(this, EventArgs.Empty);
        if (entries.Count > 0)
        {
            await store.RestoreEntriesAsync(entries);
        }
    }
}
