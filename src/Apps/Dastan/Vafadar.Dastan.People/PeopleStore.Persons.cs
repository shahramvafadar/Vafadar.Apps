using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <content>
/// The profile of a person of a private tree (step 19.5, ADR 0017): where they were born and died, a few words
/// about them, and a picture – one of the photographs of who describes them, shown to exactly those who may
/// see that photograph. What may be kept depends on who the person is (PRIV-003): of somebody who died, what
/// the people of the tree know; of somebody who lives, only what they say themselves once the record is their
/// account; of a child, what an account that is their parent in the tree decides. Of a living person nobody
/// asked, a record holds a name, the years and how they are related – and nothing else.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>The longest name of a place in the profile of a person, in characters.</summary>
    public const int LongestPlace = 200;

    /// <summary>The longest text about a person, in characters.</summary>
    public const int LongestAbout = 1000;

    /// <summary>Below this age a person is a child, and what is kept about them is a parent's to decide.</summary>
    public const int AdultAt = 18;

    /// <summary>
    /// Says where a person was born and died, a few words about them and which photograph is their picture.
    /// Returns <c>done</c>, or why not: <c>refused</c> (no such record, or the account may not see the tree, or
    /// the record is somebody else's account, or the account may not change the tree), <c>not-asked</c> (a
    /// living person who has no account here: nothing but the name, the years and the relations is kept),
    /// <c>long</c> (a text is too long or has more than one line where it has one), <c>picture</c> (no
    /// photograph of the account).
    /// </summary>
    /// <param name="id">The account that describes.</param>
    /// <param name="tree">The tree.</param>
    /// <param name="person">The record.</param>
    /// <param name="birthPlace">Where they were born, in words; nothing for not stated.</param>
    /// <param name="deathPlace">Where they died, in words; nothing for not stated.</param>
    /// <param name="about">A few words about them; nothing for none.</param>
    /// <param name="picture">The photograph that is their picture – one of the account's own; nothing for none.</param>
    public string DescribePerson(string id, string tree, string person, string? birthPlace, string? deathPlace, string? about, string? picture) =>
        Guarded([OfTree(tree)], () =>
        {
            var stored = _context.Persons.SingleOrDefault(candidate => candidate.Id == person && candidate.Tree == tree);
            if (stored is null || Role(id, tree) == TreeRole.None)
            {
                _context.ChangeTracker.Clear();
                return "refused";
            }

            if (!MayDescribe(stored, id))
            {
                _context.ChangeTracker.Clear();
                return MayBeDescribed(stored) || stored.Account is not null ? "refused" : "not-asked";
            }

            var (born, died, words) = (Line(birthPlace, LongestPlace), Line(deathPlace, LongestPlace), Text(about, LongestAbout));
            if (born.Refused || died.Refused || words.Refused)
            {
                _context.ChangeTracker.Clear();
                return "long";
            }

            var photo = string.IsNullOrWhiteSpace(picture) ? null : picture.Trim();
            if (photo is not null && photo != stored.Picture && !_context.Photos.AsNoTracking().Any(candidate => candidate.Id == photo && candidate.Owner == id))
            {
                _context.ChangeTracker.Clear();
                return "picture";
            }

            (stored.BirthPlace, stored.DeathPlace, stored.About, stored.Picture) = (born.Value, died.Value, words.Value, photo);

            // Somebody agreed to be named as the record was: changed, the record is agreed to again.
            if (stored.Account is { } account)
            {
                ForgetConsent(tree, account);
            }

            Record(tree, id, "person-described", stored.Name);
            Save();
            return "done";
        });

    // Whether anything beyond the name, the years and the relations may be kept of a record at all: somebody
    // who died, somebody whose account the record is, or a child.
    private bool MayBeDescribed(StoredPerson record) =>
        record.Deceased || (record.Account is { } account && IsOpen(account)) || IsChild(record);

    // Who may say it: the person themself where the record is an account; who may change the tree for somebody
    // who died; an account that is a parent of the child in this tree for a child.
    private bool MayDescribe(StoredPerson record, string id)
    {
        if (record.Account is { } account && IsOpen(account))
        {
            return account == id;
        }

        if (record.Deceased)
        {
            return Role(id, record.Tree) >= TreeRole.Editor;
        }

        return IsChild(record) && _context.Kinships.AsNoTracking()
            .Where(link => link.Tree == record.Tree && (link.Kind == "parent" || link.Kind == "adoptive-parent" || link.Kind == "guardian") && link.To == record.Id)
            .Join(_context.Persons.AsNoTracking(), link => link.From, parent => parent.Id, (_, parent) => parent.Account)
            .Any(parent => parent == id);
    }

    // A child: somebody who lives and whose record says a birth less than eighteen years ago.
    private bool IsChild(StoredPerson record) =>
        !record.Deceased && PersonTime.Years(PersonTime.Read(record.Born)) is var (_, latest) && Today.Year - latest < AdultAt;

    // What a record held beyond the name, the years and the relations is not kept when it may not be any more.
    private void ForgetProfileIfNotKept(StoredPerson record)
    {
        if (!MayBeDescribed(record))
        {
            (record.BirthPlace, record.DeathPlace, record.About, record.Picture) = (null, null, null, null);
        }
    }

    private static (string? Value, bool Refused) Line(string? written, int longest)
    {
        var text = written?.Trim() ?? string.Empty;
        return text.Length == 0 ? (null, false) : (text, text.Length > longest || text.Any(char.IsControl));
    }

    private static (string? Value, bool Refused) Text(string? written, int longest)
    {
        var text = (written ?? string.Empty).ReplaceLineEndings("\n").Trim();
        return text.Length == 0 ? (null, false) : (text, text.Length > longest || text.Any(letter => char.IsControl(letter) && letter != '\n'));
    }
}
