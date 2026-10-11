using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Profile-local explicit choices, never portable purchase, membership or security facts.</summary>
internal sealed class StoredResourceSelection
{
    /// <summary>Gets or sets the financial resource being chosen.</summary>
    public QuotaKind Kind { get; set; }
    /// <summary>Gets or sets the exact counting domain supplied by verified access.</summary>
    public QuotaScopeKind ScopeKind { get; set; }
    /// <summary>Gets or sets the exact profile or space identity.</summary>
    public Guid ScopeId { get; set; }
    /// <summary>Gets or sets canonical, sorted original identities; empty means an explicit empty choice.</summary>
    public string IdentitySet { get; set; } = string.Empty;
    /// <summary>Gets or sets a new revision for every changed choice, rejecting stale and ABA drafts.</summary>
    public Guid Revision { get; set; }

    /// <summary>Materializes a strictly bounded canonical value without granting any capability.</summary>
    public ResourceSelection ToSelection()
    {
        if (Revision == Guid.Empty || IdentitySet.Length > 8447)
            throw new InvalidOperationException("The stored resource choice is invalid.");
        var ids = IdentitySet.Length == 0 ? [] : IdentitySet.Split(';').Select(id => Guid.ParseExact(id, "N")).ToArray();
        var choice = new ResourceSelection(Kind, new(ScopeKind, ScopeId), ids);
        if (Encode(choice) != IdentitySet) throw new InvalidOperationException("The stored resource choice is not canonical.");
        return choice;
    }

    /// <summary>Checks stored input without turning a malformed choice into a barrier to retained corrections.</summary>
    internal bool HasValidValue()
    {
        try { _ = ToSelection(); return true; }
        catch (Exception exception) when (exception is FormatException or ArgumentException or InvalidOperationException) { return false; }
    }

    /// <summary>Malformed input grants no bounded new work; explicit reviewed Save can repair its exact row.</summary>
    internal ResourceSelection ForNewWork() => HasValidValue() ? ToSelection() : new(Kind, new(ScopeKind, ScopeId), []);

    /// <summary>Serializes the immutable canonical Core choice.</summary>
    internal static string Encode(ResourceSelection selection) => string.Join(';', selection.SelectedIds.Select(id => id.ToString("N")));

    /// <summary>Reads only the opened file's exact enabled financial scope; inactive builds read no choice rows.</summary>
    internal static Task<StoredResourceSelection?> ReadAsync(ZananceDbContext db, CommercialFileAccess access,
        QuotaKind kind, CancellationToken cancellationToken, bool tracking = false)
    {
        if (access.Context is not { } context) return Task.FromResult<StoredResourceSelection?>(null);
        var scope = Scope(context);
        var query = tracking ? db.ResourceSelections : db.ResourceSelections.AsNoTracking();
        return query.SingleOrDefaultAsync(row => row.Kind == kind && row.ScopeKind == scope.Kind && row.ScopeId == scope.Id, cancellationToken);
    }

    /// <summary>Uses verified financial identity only, never a guessed main-profile id or device identity.</summary>
    internal static QuotaScope Scope(CapabilityContext context) => new(context.Scope.Kind == EntitlementScopeKind.PersonalProfile
        ? QuotaScopeKind.PersonalProfile : QuotaScopeKind.SharedSpace, context.Scope.Id);
}
