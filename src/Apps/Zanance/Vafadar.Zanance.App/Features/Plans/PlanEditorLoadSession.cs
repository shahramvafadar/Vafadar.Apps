using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Owns one complete original plan form and read-only refresh of its retained raw draft.</summary>
public sealed class PlanEditorLoadSession(PlanStore plans, PlanEditorReadRequest request,
    Func<string, bool>? acceptsFile = null, Func<bool>? originalOwner = null)
{
    private long _revision;
    private CancellationTokenSource _lifetime = new();
    private string? _path;
    private PlanEditorReadSnapshot? _prepared;
    private PlanEditorReadSnapshot? _original;
    /// <summary>Gets the published original packet; a departed form grants no pending action authority.</summary>
    public PlanEditorReadSnapshot? Review { get; private set; }
    /// <summary>Gets whether this routed identity already owns a published raw draft.</summary>
    public bool HasPublished => _original is not null;
    /// <summary>Adopts only the exact original parent's known additive account/default commit.</summary>
    public void AcceptAccountReturn(PlanAccountHandoff visit)
    {
        visit.EnsureCurrent(); var next = visit.Accepted ?? throw new InvalidOperationException("The original account result is not committed.");
        if (ReferenceEquals(_original, next)) return;
        if (!ReferenceEquals(_original, visit.Source) || _original.IsPending || _original.Accepted
            || next.Request != request || next.DatabasePath != _path || !next.IsAvailable)
            throw new InvalidOperationException("Another plan form cannot receive this account result.");
        next.EnsureCurrent(); Retire(); _original = next;
    }
    /// <summary>Retires pending reads and callbacks without replacing original raw input or acceptance.</summary>
    public void Retire()
    { _revision++; Review = _prepared = null; _lifetime.Cancel(); _lifetime.Dispose(); _lifetime = new(); }
    private bool AcceptFile(string path)
    {
        if (originalOwner?.Invoke() == false || _path is not null && path != _path || acceptsFile?.Invoke(path) == false) return false;
        _path ??= path; return true;
    }
    /// <summary>Reads a fresh complete form or rechecks the original packet without overwriting its raw fields.</summary>
    public async Task<PlanEditorReadSnapshot> ReadAsync(bool reviewAccess = false)
    {
        Retire(); var revision = _revision; var token = _lifetime.Token;
        if (_original is { } original)
        {
            if (original.IsPending || original.Accepted) throw new InvalidOperationException("The original plan writer is pending or accepted.");
            var next = original;
            if (reviewAccess) next = await plans.RefreshEditorSnapshotAsync(original, token);
            else await plans.ValidateEditorSnapshotAsync(original, token);
            Current(revision, token); _prepared = next; return next;
        }
        var result = await plans.ReadEditorSnapshotAsync(request, AcceptFile, token); Current(revision, token);
        _prepared = result; return result;
    }
    /// <summary>Publishes only this preparation after complete native field/derived-value publication.</summary>
    public void Publish(PlanEditorReadSnapshot source)
    {
        if (!ReferenceEquals(_prepared, source) || source.DatabasePath != _path || !source.IsAvailable || originalOwner?.Invoke() == false)
            throw new InvalidOperationException("The original plan preparation retired or is unavailable.");
        source.EnsureCurrent(); _original = source; Review = source;
    }
    /// <summary>Revalidates a published original before a native decision or writer submission.</summary>
    public async Task ValidateAsync()
    {
        var revision = _revision; var token = _lifetime.Token;
        var source = Review ?? throw new InvalidOperationException("The original plan form is not published.");
        await plans.ValidateEditorSnapshotAsync(source, token); Current(revision, token);
        if (!ReferenceEquals(Review, source)) throw new InvalidOperationException("The original plan publication changed.");
    }
    private void Current(long revision, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (revision != _revision || originalOwner?.Invoke() == false)
            throw new OperationCanceledException("The original plan read retired.");
    }
}
