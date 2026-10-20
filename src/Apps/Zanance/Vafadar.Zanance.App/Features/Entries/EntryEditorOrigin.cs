using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Retains the exact displayed detail/file/rights across navigation to its original editor.</summary>
public sealed class EntryEditorOrigin(EntryDetailSnapshot original)
{
    /// <summary>Rejects a different source identity or a changed original detail before destination publication.</summary>
    public async Task ValidateAsync(ZananceStore store, EntryEditorReadRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (original.Entry is null || request.SourceId != original.EntryId)
            throw new InvalidOperationException("The original editor navigation source changed.");
        original.EnsureCurrent();
        try { await store.ValidateEntryDetailSnapshotAsync(original, ct); }
        catch (EntryDetailSupersededException) { throw new EntryEditorSourceChangedException(); }
    }
    /// <summary>Refuses a different configured file and retired cached rights before any editor row query.</summary>
    public bool IsOriginalFile(string path)
    { original.EnsureAccessCurrent(); return string.Equals(path, original.DatabasePath, StringComparison.Ordinal); }
}
