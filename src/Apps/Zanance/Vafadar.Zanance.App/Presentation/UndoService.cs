using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps the most recently deleted entries for a short time so that the list can offer "Undo" (TX-05).
/// </summary>
public sealed class UndoService(ZananceStore store, TimeProvider time)
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(8);
    private IReadOnlyList<LedgerEntry> _deleted = [];
    private Func<Task>? _action;
    private DateTimeOffset _deletedAt;
    private long _offerVersion;
    private bool _undoing;

    /// <summary>Raised when the undo offer appears or disappears.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets a value indicating whether an undo is currently offered.</summary>
    public bool CanUndo => !_undoing && (_deleted.Count > 0 || _action is not null)
        && time.GetUtcNow() - _deletedAt is var elapsed && elapsed >= TimeSpan.Zero && elapsed < Window;

    /// <summary>Gets how long the offer is still valid.</summary>
    public TimeSpan Remaining => CanUndo ? Window - (time.GetUtcNow() - _deletedAt) : TimeSpan.Zero;

    /// <summary>Offers to restore <paramref name="deleted"/>.</summary>
    public void Offer(IReadOnlyList<LedgerEntry> deleted)
    {
        _deleted = deleted;
        _action = null;
        _deletedAt = time.GetUtcNow();
        _offerVersion++;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Offers to undo a change that is not a deletion, e.g. an aggregated entry reduced by "Link and replace" (ZEX-S0611).</summary>
    public void Offer(Func<Task> undo)
    {
        _deleted = [];
        _action = undo;
        _deletedAt = time.GetUtcNow();
        _offerVersion++;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Withdraws the offer.</summary>
    public void Dismiss()
    {
        _offerVersion++;
        if (_deleted.Count > 0 || _action is not null)
        {
            _deleted = [];
            _action = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Restores the deleted entries.</summary>
    public async Task UndoAsync()
    {
        // D-127: one Undo at a time; failure preserves the original deadline, never a renewed offer.
        if (_undoing) return;
        if (!CanUndo) { Dismiss(); return; }

        var entries = _deleted;
        var action = _action;
        var version = _offerVersion;
        _undoing = true;
        try
        {
            Changed?.Invoke(this, EventArgs.Empty);
            if (entries.Count > 0) await store.RestoreEntriesAsync(entries);
            if (action is not null) await action();

            // Completion of an older operation must not dismiss a replacement offer or revive a dismissed one.
            if (version == _offerVersion)
            {
                _deleted = [];
                _action = null;
                _offerVersion++;
            }
        }
        finally
        {
            _undoing = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
