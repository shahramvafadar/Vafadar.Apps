using Vafadar.Dastan.Core.Personal;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <content>
/// The rules of the store that are "count, then add" or "look, then write" (ADR 0015, ADR 0022): at most so
/// many photographs, trees, circles, albums, open invitations and open requests of an account; so many records
/// in a tree and tags on a photograph; one request or relationship between two accounts; one claim of an
/// account in a tree; nobody in a circle or a tree of somebody they show no accepted relationship with; nobody
/// their own ancestor. Two requests that arrive at the same moment would each look before the other wrote. So
/// each of these acts runs in a transaction that first takes a lock of the database for what it looks at – the
/// account, the two accounts, the photograph, the tree – reads what it decides on only then, and the second
/// request waits until the first has written. The locks are of the transaction and are let go when it ends.
/// <para>
/// <b>The order.</b> The locks have names, and a transaction takes them in the order of their names
/// (<c>account:</c>, <c>pair:</c>, <c>photo:</c>, <c>restrictions</c>, <c>tree:</c>): it may take another one
/// later, but only one that comes after all it holds. Two acts therefore never wait for each other. An act
/// inside another one asks for its locks like any other: what is held already is held, what comes later in the
/// order is taken, and what would come earlier is refused – an open transaction is no proof that the right
/// locks are held (ADR 0022 has the table of which act takes what).
/// </para>
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>
    /// Called with the name of a place in an act, after the act has decided and before it writes. Tests use it
    /// to hold two acts at the same place; nothing else sets it.
    /// </summary>
    internal Action<string>? Reached { get; set; }

    private void Reach(string place) => Reached?.Invoke(place);

    // The locks the transaction under way holds, and the transaction they belong to.
    private readonly SortedSet<string> _held = new(StringComparer.Ordinal);
    private object? _heldIn;

    internal T Guarded<T>(string[] keys, Func<T> act)
    {
        // An act that is part of a larger one runs in the transaction of that one, under its locks and the ones
        // it still may take.
        if (_context.Database.CurrentTransaction is not null)
        {
            Hold(keys);
            return act();
        }

        using var step = _context.Database.BeginTransaction();
        try
        {
            Hold(keys);
            var done = act();
            step.Commit();
            return done;
        }
        catch
        {
            // Nothing of an act that failed is written with the next one.
            _context.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            _held.Clear();
            _heldIn = null;
        }
    }

    /// <summary>
    /// Takes locks in the transaction under way, in the order of their names. A lock that is held is held; one
    /// that comes before a lock that is held cannot be taken any more without risking that two acts wait for
    /// each other, and is refused.
    /// </summary>
    private void Hold(IEnumerable<string> keys)
    {
        var step = _context.Database.CurrentTransaction ?? throw new InvalidOperationException("A lock is taken in a transaction.");
        if (!ReferenceEquals(_heldIn, step))
        {
            // A transaction that was not begun here holds nothing yet.
            _held.Clear();
            _heldIn = step;
        }

        foreach (var key in keys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            if (_held.Contains(key))
            {
                continue;
            }

            if (_held.Count > 0 && StringComparer.Ordinal.Compare(key, _held.Max) < 0)
            {
                throw new InvalidOperationException($"The lock {Kind(key)} is asked for after {Kind(_held.Max!)}: the act that begins has to take both, in their order.");
            }

            _context.Database.ExecuteSql($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");
            _held.Add(key);
        }
    }

    // The kind of a lock without what it names: an identifier is nothing for a message.
    private static string Kind(string key) => key.Split(':')[0];

    internal static string OfAccount(string id) => "account:" + id;

    // The two accounts, whichever is named first: the same key for both directions.
    private static string OfTwo(string one, string other) =>
        string.CompareOrdinal(one, other) <= 0 ? $"pair:{one}:{other}" : $"pair:{other}:{one}";

    private static string OfTree(string tree) => "tree:" + tree;

    private static string OfPhoto(string photo) => "photo:" + photo;

    // The two accounts of a relationship, whoever asks whom: the same key for both directions.
    private string[] OfPair(string id, string name)
    {
        var key = Key((name ?? string.Empty).Trim());
        var other = _context.Visitors.AsNoTracking().Where(visitor => visitor.NameKey == key).Select(visitor => visitor.Id).SingleOrDefault() ?? "nobody";
        return [OfAccount(id), OfTwo(id, other)];
    }

    /// <summary>Makes an album. Returns its number, or nothing when the name is not a name, is taken, or the account has as many albums as it may.</summary>
    public long? AddAlbum(string id, string name, string note = "") => Guarded([OfAccount(id)], () => AddAlbumUnguarded(id, name, note));

    /// <summary>Makes an invitation and returns its code, or nothing when the account has as many open ones as it may.</summary>
    public string? Invite(string id) => Guarded([OfAccount(id)], () => InviteUnguarded(id));

    /// <summary>Tags an account on a photograph; the tag waits for the answer of who is tagged.</summary>
    public bool TagPhoto(string id, string photo, string account) => Guarded([OfTwo(id, account), OfPhoto(photo)], () => TagPhotoUnguarded(id, photo, account));

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
