using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Original transaction-to-plan file/source authority, separate from the detached unsaved raw field handoff.</summary>
public sealed class EntryPlanOrigin(EntryEditorReadSnapshot source, Func<bool> current)
{
    /// <summary>Gets the parent form's exact source; raw handoff values never become a persisted transaction.</summary>
    public EntryEditorReadSnapshot Source { get; } = source;
    /// <summary>Gets whether the original parent query still owns this nested visit, including while it is covered.</summary>
    public bool IsOwned => current() && Source.IsAvailable && !Source.Accepted && !Source.IsPending;
    /// <summary>Rejects a replaced, missing or accepted original parent without following another profile.</summary>
    public void EnsureCurrent()
    {
        Source.EnsureCurrent();
        if (!current() || !Source.IsAvailable || Source.Accepted || Source.IsPending)
            throw new InvalidOperationException("The original transaction-to-plan visit retired.");
    }
}
