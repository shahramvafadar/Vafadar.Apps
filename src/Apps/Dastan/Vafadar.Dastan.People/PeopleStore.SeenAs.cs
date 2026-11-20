using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>The page about an account as somebody else would see it (PRIV-015).</summary>
/// <param name="Kind">As whom: <c>public</c>, <c>circle</c> or <c>account</c>.</param>
/// <param name="Name">The name of the circle or of the account; nothing for the public.</param>
/// <param name="Profile">The relationships and the places.</param>
/// <param name="Photos">The photographs.</param>
/// <param name="Albums">The albums, each with the photographs that are seen.</param>
public sealed record SeenAs(string Kind, string? Name, ProfileView Profile, IReadOnlyList<PhotoInfo> Photos, IReadOnlyList<AlbumInfo> Albums);

/// <summary>A tree as somebody else would see it (PRIV-015).</summary>
/// <param name="Kind">As whom: <c>public</c>, <c>circle</c> or <c>account</c>.</param>
/// <param name="Name">The name of the circle or of the account; nothing for the public.</param>
/// <param name="Tree">The tree as it is seen, or nothing when it is not seen at all.</param>
public sealed record TreeSeenAs(string Kind, string? Name, TreeView? Tree);

/// <content>
/// Seeing as somebody else (step 19.6, ADR 0018, PRIV-015): an account looks at its own page and at a tree of
/// its own as the public, as one of its circles or as an account it shows a relationship with would see
/// them. The answer is drawn by the rules that draw the real page – the same scopes, asked for that viewer –
/// and not by a second set of rules. It shows what is the account's own to know: of a relationship, what the
/// account itself lets that viewer see; whether the other side hides it from that viewer is the other
/// side's choice and is not told to anybody – so for a circle and a named account a relationship can be
/// listed that the other side keeps from them. For the public nothing is left open: what everybody sees,
/// the account sees too.
/// </content>
public sealed partial class PeopleStore
{
    // Stands for "somebody who is in this circle and is nobody else": an identifier no account has.
    private const string CircleViewer = "somebody-of-the-circle";

    // The circle whose members are joined by the viewer above while an account looks as that circle.
    private (string Owner, long Circle)? _lookedAs;

    /// <summary>
    /// Returns the page about an account as somebody else would see it, or nothing when that is nobody the
    /// account can look as: <c>public</c>, <c>circle-</c> and the number of one of its circles, or
    /// <c>account-</c> and the identifier of an open account it shows an accepted relationship with.
    /// </summary>
    public SeenAs? SeeAs(string id, string asWhom)
    {
        if (!IsOpen(id) || Look(id, asWhom) is not var (kind, name, viewer, circle))
        {
            return null;
        }

        if (viewer is null)
        {
            return new(kind, name, Profile(null, id)!, LifeOf(null, id), AlbumsOf(null, id));
        }

        _lookedAs = circle is { } number ? (id, number) : null;
        try
        {
            var account = new AccountId(viewer);
            var accepted = _context.Relationships.AsNoTracking()
                .Where(relationship => (relationship.From == id || relationship.To == id) && relationship.State == (int)RelationshipState.Accepted)
                .ToList();
            var others = Names(accepted.Select(relationship => relationship.From == id ? relationship.To : relationship.From));

            // What the account itself lets this viewer see; the other side of a relationship sees it anyway.
            var shown = accepted
                .Where(relationship => others.ContainsKey(relationship.From == id ? relationship.To : relationship.From))
                .Where(relationship => relationship.From == viewer || relationship.To == viewer
                    || (relationship.From == id ? Scope(id, (ScopeKind)relationship.FromScope, relationship.FromCircle) : Scope(id, (ScopeKind)relationship.ToScope, relationship.ToCircle)).Allows(account))
                .Select(relationship => new ShownRelationship(others[relationship.From == id ? relationship.To : relationship.From], relationship.Kind) { Terms = Terms(relationship, id) })
                .OrderBy(relationship => relationship.Other.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(relationship => RelationshipKinds.IndexOf(relationship.Kind));
            var person = Names([id])[id];

            // Of the photographs only the account's own: whether a photograph of somebody else reaches this
            // viewer is that owner's choice.
            var photos = Shown(viewer, _context.Photos.AsNoTracking().Where(photo => photo.Owner == id).ToList()).ToList();
            return new(kind, name, new ProfileView(person, [.. shown]) { Affiliations = AffiliationsShown(account, id) }, photos, AlbumsShown(viewer, id, empty: false));
        }
        finally
        {
            _lookedAs = null;
        }
    }

    /// <summary>
    /// Returns a tree of an account as somebody else would see it, or nothing when the tree is not the
    /// account's own or that is nobody the account can look as. A tree is shared with accounts, not with
    /// circles: a circle sees of it what the public sees.
    /// </summary>
    public TreeSeenAs? SeeTreeAs(string id, string tree, string asWhom)
    {
        if (Role(id, tree) != TreeRole.Owner || Look(id, asWhom) is not var (kind, name, viewer, circle))
        {
            return null;
        }

        var seen = viewer is null || circle is not null ? PublicTree(tree) : Tree(viewer, tree);
        return new(kind, name, seen is null ? null : seen with
        {
            // What is theirs to do is not shown as something to do here: this is a look, not their session.
            Claims = [],
            CanClaim = false,
            Members = [],
            History = [],
            People = [.. seen.People.Select(person => person with { Claimable = false, MayDescribe = false })],
        });
    }

    // Whom an account can look as: the public (no viewer), one of its circles (the viewer that stands for a
    // member), or an open account it shows an accepted relationship with.
    private (string Kind, string? Name, string? Viewer, long? Circle)? Look(string id, string? asWhom)
    {
        if (asWhom == "public")
        {
            return ("public", null, null, null);
        }

        if (asWhom is not null && asWhom.StartsWith("circle-", StringComparison.Ordinal)
            && long.TryParse(asWhom.AsSpan(7), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number)
            && _context.Circles.AsNoTracking().Where(circle => circle.Number == number && circle.Owner == id).Select(circle => circle.Name).SingleOrDefault() is { } circleName)
        {
            return ("circle", circleName, CircleViewer, number);
        }

        if (asWhom is not null && asWhom.StartsWith("account-", StringComparison.Ordinal) && asWhom[8..] is var other && Know(id, other) && Names([other]).TryGetValue(other, out var person))
        {
            return ("account", person.Name, other, null);
        }

        return null;
    }
}
