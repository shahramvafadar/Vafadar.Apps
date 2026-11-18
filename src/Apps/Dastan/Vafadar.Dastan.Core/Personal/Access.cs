using System.Collections.Immutable;

namespace Vafadar.Dastan.Core.Personal;

/// <summary>Identifies a sign-in account: an identity that can act and be given permissions (PRIV-002).</summary>
/// <param name="Value">The stable text of the identifier.</param>
public readonly record struct AccountId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Identifies the record of a person. A record can exist without an account – a historical person, a relative
/// who died, somebody who was never invited (PRIV-002).
/// </summary>
/// <param name="Value">The stable text of the identifier.</param>
public readonly record struct PersonId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>Who an item is meant for.</summary>
public enum ScopeKind
{
    /// <summary>Its owner alone.</summary>
    Private = 0,

    /// <summary>Its owner and the accounts the owner named.</summary>
    Circle = 1,

    /// <summary>Everybody, with or without an account.</summary>
    Public = 2,
}

/// <summary>
/// The explicit scope of one item or relationship (PRIV-001): who may see it. A scope is asked before anything
/// is produced from the item – a result, a count, a preview (PRIV-012). An account, a record of a person, a
/// circle and a filter of a view are different things: a scope names accounts, nothing else.
/// </summary>
/// <param name="Owner">The account the item belongs to. Its owner always sees it.</param>
/// <param name="Kind">Who else may see it.</param>
/// <param name="Members">For a circle: the accounts in it.</param>
public sealed record AccessScope(AccountId Owner, ScopeKind Kind, ImmutableHashSet<AccountId> Members)
{
    /// <summary>Returns a scope for the owner alone.</summary>
    public static AccessScope OnlyMe(AccountId owner) => new(owner, ScopeKind.Private, []);

    /// <summary>Returns a scope for the owner and the accounts named.</summary>
    public static AccessScope Circle(AccountId owner, params AccountId[] members) => new(owner, ScopeKind.Circle, [.. members]);

    /// <summary>Returns a scope for everybody.</summary>
    public static AccessScope Everybody(AccountId owner) => new(owner, ScopeKind.Public, []);

    /// <summary>Returns whether a viewer may see the item. A viewer without an account sees what is public only.</summary>
    public bool Allows(AccountId? viewer) => Kind switch
    {
        ScopeKind.Public => true,
        _ when viewer is null => false,
        _ when viewer == Owner => true,
        ScopeKind.Circle => Members.Contains(viewer.Value),
        _ => false,
    };

    /// <summary>Returns the scope without an account: taking somebody out of a circle is a change of the scope.</summary>
    public AccessScope Without(AccountId account) => this with { Members = Members.Remove(account) };
}
