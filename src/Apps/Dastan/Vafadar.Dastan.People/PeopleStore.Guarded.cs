using Vafadar.Dastan.Core.Personal;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <content>
/// The rules of the store that are "count, then add" or "look, then write" (ADR 0015): at most so many
/// photographs, trees, circles, albums, open invitations and open requests of an account; so many records in a
/// tree and tags on a photograph; one request or relationship between two accounts; one claim of an account in
/// a tree. Two requests that arrive at the same moment would each count before the other wrote. So each of
/// these acts runs in a transaction that first takes a lock of the database for what it counts – the account,
/// the tree, the photograph, the two accounts – and the second request waits until the first has written. The
/// locks are of the transaction and are let go when it ends; they are taken in one order, so that two acts
/// never wait for each other.
/// </content>
public sealed partial class PeopleStore
{
    internal T Guarded<T>(string[] keys, Func<T> act)
    {
        // An act that is already part of a larger one runs under the locks of that one.
        if (_context.Database.CurrentTransaction is not null)
        {
            return act();
        }

        using var step = _context.Database.BeginTransaction();
        foreach (var key in keys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            _context.Database.ExecuteSql($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");
        }

        var done = act();
        step.Commit();
        return done;
    }

    internal static string OfAccount(string id) => "account:" + id;

    private static string OfTree(string tree) => "tree:" + tree;

    private static string OfPhoto(string photo) => "photo:" + photo;

    // The two accounts of a relationship, whoever asks whom: the same key for both directions.
    private string[] OfPair(string id, string name)
    {
        var key = Key((name ?? string.Empty).Trim());
        var other = _context.Visitors.AsNoTracking().Where(visitor => visitor.NameKey == key).Select(visitor => visitor.Id).SingleOrDefault() ?? "nobody";
        var (first, second) = string.CompareOrdinal(id, other) <= 0 ? (id, other) : (other, id);
        return [OfAccount(id), $"pair:{first}:{second}"];
    }

    /// <summary>Makes an album. Returns its number, or nothing when the name is not a name, is taken, or the account has as many albums as it may.</summary>
    public long? AddAlbum(string id, string name, string note = "") => Guarded([OfAccount(id)], () => AddAlbumUnguarded(id, name, note));

    /// <summary>Makes an invitation and returns its code, or nothing when the account has as many open ones as it may.</summary>
    public string? Invite(string id) => Guarded([OfAccount(id)], () => InviteUnguarded(id));

    /// <summary>Tags an account on a photograph; the tag waits for the answer of who is tagged.</summary>
    public bool TagPhoto(string id, string photo, string account) => Guarded([OfPhoto(photo)], () => TagPhotoUnguarded(id, photo, account));

    /// <summary>Objects to a photograph that is shown to everybody, as somebody who is on it.</summary>
    public bool ObjectToPhoto(string id, string photo) => Guarded([OfPhoto(photo)], () => ObjectToPhotoUnguarded(id, photo));

    /// <summary>Asks another account for a relationship and says what came of it.</summary>
    public string Ask(string id, string name, string kind, RelationshipTerms? terms = null) => Guarded(OfPair(id, name), () => AskUnguarded(id, name, kind, terms ?? RelationshipTerms.None, replaces: null));

    /// <summary>
    /// Asks the other side of an accepted relationship to call it something else – another kind, another
    /// direction, context or time. The relationship holds as it is until the other side agrees; then the new
    /// one takes its place, with the audience each side had chosen. Returns <c>asked</c>, or why not – like
    /// <see cref="Ask"/>, and <c>refused</c> for a relationship that is not this account's or not accepted,
    /// <c>same</c> when nothing would change.
    /// </summary>
    public string AskChange(string id, string relationship, string kind, RelationshipTerms? terms = null)
    {
        var stored = _context.Relationships.AsNoTracking().SingleOrDefault(candidate => candidate.Id == relationship);
        if (stored is null || (stored.From != id && stored.To != id))
        {
            return "refused";
        }

        var other = _context.Visitors.AsNoTracking().Where(visitor => visitor.Id == (stored.From == id ? stored.To : stored.From)).Select(visitor => visitor.Name).SingleOrDefault();
        return other is null ? "refused" : Guarded(OfPair(id, other), () => AskUnguarded(id, other, kind, terms ?? RelationshipTerms.None, relationship));
    }

    /// <summary>Makes a circle. Returns its number, or nothing when the name is not a name, is taken, or the account has as many circles as it may.</summary>
    public long? AddCircle(string id, string name) => Guarded([OfAccount(id)], () => AddCircleUnguarded(id, name));

    /// <summary>Begins a family tree. Returns its identifier, or nothing when the name is not a name or the account has as many trees as it may.</summary>
    public string? AddTree(string id, string name) => Guarded([OfAccount(id)], () => AddTreeUnguarded(id, name));

    /// <summary>Adds the record of a person to a tree. Returns its identifier, or nothing when it cannot be added.</summary>
    public string? AddPerson(string id, string tree, string name, Vafadar.Temporal.TemporalExpression? born, Vafadar.Temporal.TemporalExpression? died, bool deceased) =>
        Guarded([OfTree(tree)], () => AddPersonUnguarded(id, tree, name, born, died, deceased));

    /// <summary>Says that a record of a tree is the account itself, and says what came of it.</summary>
    public string Claim(string id, string tree, string person) => Guarded([OfTree(tree)], () => ClaimUnguarded(id, tree, person));

    /// <summary>Keeps a photograph that was brought in. Returns whether it was kept.</summary>
    public bool AddPhoto(string id, string photo, PhotoFile file, string caption, DateOnly? taken, ScopeKind shown, long? circle = null) =>
        Guarded([OfAccount(id)], () => AddPhotoUnguarded(id, photo, file, caption, taken, shown, circle));

    /// <summary>Puts a page on the list of an account; a full list lets its oldest page go.</summary>
    public void Keep(string id, KeptPage page) => Guarded([OfAccount(id)], () =>
    {
        KeepUnguarded(id, page);
        return true;
    });
}
