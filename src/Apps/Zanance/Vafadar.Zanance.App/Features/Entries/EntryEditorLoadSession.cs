using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Owns one original editor read, complete publication and read-only raw-draft refresh.</summary>
public sealed class EntryEditorLoadSession(ZananceStore store, EntryEditorReadRequest request, Func<string, bool>? acceptsFile = null, EntryEditorOrigin? origin = null)
{
    private long _revision;
    private CancellationTokenSource _lifetime = new();
    private string? _path;
    private EntryEditorReadSnapshot? _prepared;
    private EntryEditorReadSnapshot? _original;
    /// <summary>Gets the complete original published packet; a departed owner grants no write authority.</summary>
    public EntryEditorReadSnapshot? Review { get; private set; }
    /// <summary>Gets whether the original raw draft has a complete published source.</summary>
    public bool HasPublished => _original is not null;
    /// <summary>Adopts only the known metadata commit of this exact original parent's explicit account child.</summary>
    public void AcceptAccountReturn(EntryAccountHandoff handoff)
    {
        handoff.EnsureCurrent();
        var next = handoff.Accepted ?? throw new InvalidOperationException("The original account child has no accepted result.");
        if (ReferenceEquals(_original, next)) return;
        if (!ReferenceEquals(_original, handoff.Source) || _original.Accepted || _original.IsPending
            || next.Request != request || next.DatabasePath != _path || !next.IsAvailable)
            throw new InvalidOperationException("The original account result cannot replace another editor source.");
        next.EnsureCurrent(); Retire(); _original = next;
    }
    /// <summary>Retires pending reads/callbacks while retaining the original packet for explicit read-only refresh.</summary>
    public void Retire()
    {
        _revision++; Review = _prepared = null; _lifetime.Cancel(); _lifetime.Dispose(); _lifetime = new();
    }
    private bool AcceptFile(string path)
    {
        if (_path is not null && _path != path || origin?.IsOriginalFile(path) == false || acceptsFile?.Invoke(path) == false) return false;
        _path ??= path; return true;
    }
    /// <summary>Reads a fresh uncommitted form or revalidates an already published original without replacing its raw inputs.</summary>
    public async Task<EntryEditorReadSnapshot> ReadAsync()
    {
        Retire(); var revision = _revision; var token = _lifetime.Token;
        if (_original is { } original)
        {
            if (original.IsPending || original.Accepted)
                throw new InvalidOperationException("The original editor writer is pending or already saved.");
            await store.ValidateEntryEditorSnapshotAsync(original, token); Current(revision, token);
            _prepared = original; return original;
        }
        if (origin is not null) { await origin.ValidateAsync(store, request, token); Current(revision, token); }
        for (var attempt = 0; ; attempt++)
        {
            var source = await store.ReadEntryEditorSnapshotAsync(request, AcceptFile, token); Current(revision, token);
            try { await store.ValidateEntryEditorSnapshotAsync(source, token); }
            catch (EntryEditorSourceChangedException) when (attempt < 2) { Current(revision, token); continue; }
            if (origin is not null) { await origin.ValidateAsync(store, request, token); Current(revision, token); }
            Current(revision, token); _prepared = source; return source;
        }
    }
    /// <summary>Publishes only the exact completed read owned by this original request.</summary>
    public void Publish(EntryEditorReadSnapshot source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.IsPending || source.Accepted || !ReferenceEquals(source, _prepared) || source.Request != request || source.DatabasePath != _path
            || _lifetime.IsCancellationRequested || !source.IsAvailable)
            throw new InvalidOperationException("The original editor packet cannot publish into this form.");
        source.EnsureCurrent(); _original ??= source; _prepared = null; Review = source;
    }
    /// <summary>Rechecks the exact original publication before a native handoff or starting its financial Save.</summary>
    public async Task ValidateAsync()
    {
        var source = Review ?? throw new InvalidOperationException("The original editor requires read-only refresh.");
        var revision = _revision; var token = _lifetime.Token;
        await store.ValidateEntryEditorSnapshotAsync(source, token); Current(revision, token);
        if (!ReferenceEquals(source, Review)) throw new OperationCanceledException("The original editor publication retired.", token);
    }
    private void Current(long revision, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (revision != _revision) throw new OperationCanceledException("The original editor read retired.", token);
    }
}
