using Microsoft.AspNetCore.Components;
using Vafadar.Dastan.Core;
using EventId = Vafadar.Dastan.Core.EventId;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The addresses of the pages that say what supports a statement, and the way back from them. A page that links to
/// one says where it stands (<c>from</c>), so that closing the evidence returns the visitor to the same drawing,
/// gallery or step – with whatever was opened there.
/// </summary>
public static class EvidenceAddress
{
    /// <summary>Returns the address of the evidence of an entity.</summary>
    public static string Of(string collection, EntityId id, string? from = null) => $"{collection}/evidence/entity/{id}{From(from)}";

    /// <summary>Returns the address of the evidence of a relation.</summary>
    public static string Of(string collection, RelationId id, string? from = null) => $"{collection}/evidence/relation/{id}{From(from)}";

    /// <summary>Returns the address of the evidence of an event.</summary>
    public static string Of(string collection, EventId id, string? from = null) => $"{collection}/evidence/event/{id}{From(from)}";

    /// <summary>Returns the address of the evidence of what an entity has to do with an event.</summary>
    public static string Of(string collection, EventId storyEvent, EntityId entity, string? from = null) =>
        $"{collection}/evidence/event/{storyEvent}/entity/{entity}{From(from)}";

    /// <summary>Returns the address of the evidence of a media item.</summary>
    public static string Of(string collection, MediaId id, string? from = null) => $"{collection}/evidence/media/{id}{From(from)}";

    /// <summary>Returns the address of the page that stands before a source of another site.</summary>
    public static string Source(string collection, SourceId id, string? from = null) => $"{collection}/source/{id}{From(from)}";

    /// <summary>
    /// Returns the path of the current page as other pages of the same language write it, without what its address
    /// carried after the path: a page adds what it has checked itself.
    /// </summary>
    public static string Path(NavigationManager navigation)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        var address = navigation.ToBaseRelativePath(navigation.Uri);
        var end = address.IndexOfAny(['?', '#']);
        return end < 0 ? address : address[..end];
    }

    /// <summary>
    /// Returns the address a visitor says they came from when it is a page of this collection, otherwise
    /// <see langword="null"/>. What an address claims is never followed out of the site.
    /// </summary>
    public static string? Checked(string? from, string collection)
    {
        if (string.IsNullOrEmpty(from) || from.Length > 400 || !from.StartsWith(collection, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = from.AsSpan(collection.Length);
        var inside = rest.IsEmpty || rest[0] is '/' or '?' or '#';
        return inside
            && !from.Contains("//", StringComparison.Ordinal)
            && !from.Contains("..", StringComparison.Ordinal)
            && !from.Any(character => character is '\\' or ':' or '<' or '>' or '"' or '\'' || char.IsControl(character) || char.IsWhiteSpace(character))
                ? from
                : null;
    }

    private static string From(string? from) => string.IsNullOrEmpty(from) ? string.Empty : $"?from={Uri.EscapeDataString(from)}";
}
