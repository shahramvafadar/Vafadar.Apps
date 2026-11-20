using System.Collections.Immutable;

namespace Vafadar.Dastan.Core.Personal;

/// <summary>
/// A kind of relationship two accounts can show together (ADR 0018).
/// </summary>
/// <param name="Key">The name of the kind in a record: <c>friend</c>, <c>guardian</c>.</param>
/// <param name="Group">
/// The group of kinds of which two people hold one at a time – <c>family</c>, <c>friendship</c> – or nothing for
/// a kind that stands beside every other.
/// </param>
/// <param name="Directed">
/// Whether the two sides hold different roles: one is the parent, the other the child. A kind without a
/// direction reads the same from both sides.
/// </param>
/// <param name="Placed">Whether the kind is usually said with where it holds: the employer, the school, the team.</param>
public sealed record RelationshipKind(string Key, string? Group, bool Directed, bool Placed);

/// <summary>
/// The kinds of relationship and which exclude each other (ADR 0018, R-PER-02). Between two people only one
/// family role holds – nobody is the parent and the sibling of the same person – and a parent is one by birth,
/// by adoption or as a guardian: three kinds of the role, which a child can have each from different people.
/// "Friend" and "close friend" are steps of one ladder. Everything else is free: a friend can be a colleague
/// and a classmate too, and a colleague at two employers.
/// </summary>
public static class RelationshipCatalogue
{
    /// <summary>The group of the roles in a family.</summary>
    public const string Family = "family";

    /// <summary>The group of the steps of friendship.</summary>
    public const string Friendship = "friendship";

    /// <summary>Every kind, in the order a form offers them.</summary>
    public static readonly ImmutableArray<RelationshipKind> All =
    [
        new("friend", Friendship, Directed: false, Placed: false),
        new("close-friend", Friendship, Directed: false, Placed: false),
        new("family", Family, Directed: false, Placed: false),
        new("parent", Family, Directed: true, Placed: false),
        new("adoptive-parent", Family, Directed: true, Placed: false),
        new("guardian", Family, Directed: true, Placed: false),
        new("sibling", Family, Directed: false, Placed: false),
        new("partner", Family, Directed: false, Placed: false),
        new("colleague", null, Directed: false, Placed: true),
        new("classmate", null, Directed: false, Placed: true),
        new("teammate", null, Directed: false, Placed: true),
        new("neighbour", null, Directed: false, Placed: false),
        new("other", null, Directed: false, Placed: false),
    ];

    /// <summary>Returns the kind with a key, or nothing.</summary>
    public static RelationshipKind? Find(string? key) => All.FirstOrDefault(kind => kind.Key == key);

    /// <summary>
    /// Returns whether two relationships cannot stand side by side between the same two people: the same kind
    /// in the same context, or two kinds of one group.
    /// </summary>
    /// <param name="one">The kind of the first.</param>
    /// <param name="oneContext">The context of the first, as it is compared; empty for none.</param>
    /// <param name="other">The kind of the second.</param>
    /// <param name="otherContext">The context of the second.</param>
    public static bool Exclude(RelationshipKind one, string oneContext, RelationshipKind other, string otherContext)
    {
        ArgumentNullException.ThrowIfNull(one);
        ArgumentNullException.ThrowIfNull(other);
        return (one.Group is not null && one.Group == other.Group) || (one.Key == other.Key && string.Equals(oneContext, otherContext, StringComparison.Ordinal));
    }
}
