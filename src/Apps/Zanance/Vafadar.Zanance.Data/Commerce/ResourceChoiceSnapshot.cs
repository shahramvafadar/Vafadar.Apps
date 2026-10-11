using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>One original retained identity shown for explicit choice; financial history stays in its original stores.</summary>
public sealed record ResourceChoiceItem(Guid Id, string Name, QuotaItemState State);

/// <summary>Immutable reviewed choice bound to the opened file, original items, current rights and stored revision.</summary>
public sealed class ResourceChoiceSnapshot
{
    internal ResourceChoiceSnapshot(QuotaKind kind, IReadOnlyList<ResourceChoiceItem> items, ResourceAvailability? availability,
        string path, CommercialFileAccess access, StoredResourceSelection? stored)
    {
        Kind = kind;
        Items = Array.AsReadOnly(items.ToArray());
        Maximum = availability?.Maximum;
        RequiresSelection = availability?.RequiresSelection == true
            || (availability?.Maximum is > 0 && stored is not null && !stored.HasValidValue());
        SelectedIds = Array.AsReadOnly(items.Where(item => availability?.IsSelected(item.Id)
            ?? item.State is QuotaItemState.Active or QuotaItemState.Paused).Select(item => item.Id).Order().ToArray());
        DatabasePath = path;
        Access = access;
        Revision = stored?.Revision;
    }

    /// <summary>Gets the reviewed resource kind.</summary>
    public QuotaKind Kind { get; }
    /// <summary>Gets complete original identities and states, including retained archived and ended history.</summary>
    public IReadOnlyList<ResourceChoiceItem> Items { get; }
    /// <summary>Gets the current maximum; null means every eligible item remains available.</summary>
    public int? Maximum { get; }
    /// <summary>Gets whether the user can save a choice in the current enabled, bounded financial scope.</summary>
    public bool CanChoose => Maximum is > 0;
    /// <summary>Gets whether an absent, stale or excessive choice needs explicit review.</summary>
    public bool RequiresSelection { get; }
    /// <summary>Gets currently usable identities, never a guessed subset above capacity.</summary>
    public IReadOnlyList<Guid> SelectedIds { get; }
    /// <summary>Gets the captured actual-file identity, private to the data boundary.</summary>
    internal string DatabasePath { get; }
    /// <summary>Gets the synchronous verified-rights guard.</summary>
    internal CommercialFileAccess Access { get; }
    /// <summary>Gets the captured row revision, including the distinct absent-row state.</summary>
    internal Guid? Revision { get; }
}
