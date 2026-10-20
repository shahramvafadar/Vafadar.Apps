using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Owns complete original detail preparation, native actions and known writes independently of later routes.</summary>
public sealed class EntryDetailSession(ZananceStore store, Guid entryId)
{
    private long _revision;
    private CancellationTokenSource _lifetime = new();
    private string? _path;
    private bool _busy;
    private EntryDetailSnapshot? _displayed;
    private EntryDetailSnapshot? _prepared;

    /// <summary>Gets the original complete published packet, never an in-progress read.</summary>
    public EntryDetailSnapshot? Displayed => _displayed;
    /// <summary>Gets known action acceptance even when notification or the following native effect failed.</summary>
    public bool Accepted { get; private set; }
    /// <summary>Gets the exact committed deletion receipt; it grants no renewed Undo deadline.</summary>
    public IReadOnlyList<LedgerEntry>? Deleted { get; private set; }
    /// <summary>Gets whether the published original entry can own another explicit action.</summary>
    public bool CanInteract => !_busy && !Accepted && _displayed?.Entry is not null;
    /// <summary>Gets whether a native or writer effect currently belongs to this session.</summary>
    public bool IsBusy => _busy;

    /// <summary>Retires all pending callbacks and reads without transferring known outcomes to a new route.</summary>
    public void Retire()
    {
        _revision++; _displayed = _prepared = null; _lifetime.Cancel(); _lifetime.Dispose(); _lifetime = new();
    }

    /// <summary>Prepares one complete same-file read, with bounded persistent supersession and no publication yet.</summary>
    public async Task<EntryDetailSnapshot> ReadAsync()
    {
        Retire(); var revision = _revision; var token = _lifetime.Token;
        for (var attempt = 0; ; attempt++)
        {
            token.ThrowIfCancellationRequested();
            var source = await store.ReadEntryDetailSnapshotAsync(entryId, token, _path);
            Current(revision, token);
            if (_path is not null && _path != source.DatabasePath)
                throw new InvalidOperationException("The original entry detail cannot follow another file.");
            _path ??= source.DatabasePath;
            try { await store.ValidateEntryDetailSnapshotAsync(source, token); }
            catch (EntryDetailSupersededException) when (attempt < 2) { Current(revision, token); continue; }
            Current(revision, token); _prepared = source; return source;
        }
    }

    /// <summary>Admits only a fully prepared original packet while its read owner is still current.</summary>
    public void Publish(EntryDetailSnapshot source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!ReferenceEquals(source, _prepared) || source.Accepted || source.EntryId != entryId
            || source.DatabasePath != _path || _lifetime.IsCancellationRequested)
            throw new InvalidOperationException("The prepared original entry detail no longer belongs to this view.");
        source.EnsureCurrent(); _prepared = null; _displayed = source; Accepted = false; Deleted = null;
    }

    /// <summary>Checks exact displayed source/file/rights before dispatching an original navigation target.</summary>
    public async Task NavigateAsync(Func<EntryDetailSnapshot, Task> navigate)
    {
        if (!CanInteract) return;
        var source = _displayed!; var revision = _revision; var token = _lifetime.Token; _busy = true;
        try
        {
            await store.ValidateEntryDetailSnapshotAsync(source, token); Current(source, revision, token);
            await navigate(source);
        }
        finally { _busy = false; }
    }

    /// <summary>Marks only the original current entry as reviewed; a known write cannot be sent again.</summary>
    public Task MarkReviewedAsync() => WriteAsync((source, current, token) => store.MarkEntryDetailReviewedAsync(source, current, token));

    /// <summary>Saves a rule derived only from the original published category and match.</summary>
    public Task SaveRuleAsync() => WriteAsync((source, current, token) => store.SaveEntryDetailRuleAsync(source, current, token));

    /// <summary>Confirms and deletes only the original packet, retaining its exact committed short-Undo receipt.</summary>
    public async Task DeleteAsync(Func<Task<bool>> confirm)
    {
        if (!CanInteract) return;
        var source = _displayed!; var revision = _revision; var token = _lifetime.Token; _busy = true;
        IReadOnlyList<LedgerEntry>? deleted = null;
        try
        {
            await store.ValidateEntryDetailSnapshotAsync(source, token); Current(source, revision, token);
            if (!await confirm()) return;
            Current(source, revision, token);
            try { deleted = await store.DeleteEntryDetailAsync(source, () => Owns(source, revision), token); }
            catch (CommittedWriteNotificationException<IReadOnlyList<LedgerEntry>> exception) when (source.Accepted)
            { deleted = exception.Result; throw; }
        }
        finally
        {
            if (source.Accepted && Owns(source, revision))
            { Accepted = true; Deleted = deleted; _displayed = null; }
            _busy = false;
        }
    }

    private async Task WriteAsync(Func<EntryDetailSnapshot, Func<bool>, CancellationToken, Task> write)
    {
        if (!CanInteract) return;
        var source = _displayed!; var revision = _revision; var token = _lifetime.Token; _busy = true;
        try { await write(source, () => Owns(source, revision), token); }
        finally
        {
            if (source.Accepted && Owns(source, revision)) { Accepted = true; _displayed = null; }
            _busy = false;
        }
    }

    private bool Owns(EntryDetailSnapshot source, long revision) => revision == _revision && ReferenceEquals(source, _displayed);
    private void Current(long revision, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (revision != _revision) throw new OperationCanceledException("The original entry detail retired.", token);
    }
    private void Current(EntryDetailSnapshot source, long revision, CancellationToken token)
    {
        Current(revision, token);
        if (!Owns(source, revision)) throw new OperationCanceledException("The original displayed entry detail retired.", token);
    }
}
