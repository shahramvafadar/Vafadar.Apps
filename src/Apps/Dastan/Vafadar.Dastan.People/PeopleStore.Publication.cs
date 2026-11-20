using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>Somebody a tree names who stands in the way of making it public.</summary>
/// <param name="Person">The record.</param>
/// <param name="Name">The name of the record.</param>
/// <param name="Why"><c>no-account</c>: the person is living and is no account, so nobody can agree for them; <c>no-consent</c>: the account the record is has not agreed.</param>
public sealed record PublicationBlocker(string Person, string Name, string Why);

/// <summary>Where the making public of a tree stands, as the account that asks may know it.</summary>
/// <param name="Asked">Whether the owner asked to make the tree public.</param>
/// <param name="Published">Whether the owner made it public.</param>
/// <param name="IsPublic">Whether everybody sees it now: the owner made it public and everybody it names agrees.</param>
/// <param name="Blockers">Who stands in the way; for the owner only.</param>
/// <param name="AskedOfMe">Whether the account that asks is a record of the tree whose answer is wanted.</param>
/// <param name="Consented">Whether the account that asks agreed to be named.</param>
public sealed record PublicationState(bool Asked, bool Published, bool IsPublic, ImmutableArray<PublicationBlocker> Blockers, bool AskedOfMe, bool Consented)
{
    /// <summary>Gets what holds for a tree nobody asked to make public.</summary>
    public static PublicationState None { get; } = new(false, false, false, [], false, false);
}

/// <content>
/// Making a family tree public (step 16.6, D-053, PRIV-014): an act of its own, of the owner, that needs the
/// consent of every living person the tree names. The check before it is the rule of step 15.2
/// (<see cref="PublicationCheck"/>). Somebody who is no account cannot agree, so a tree that names a living
/// person without an account cannot be public. Consent that is taken back hides the tree at once (D-055).
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>
    /// Asks to make a tree public. Only the owner can. From then on the accounts the tree names are asked, and
    /// the owner sees who stands in the way.
    /// </summary>
    public bool AskPublication(string id, string tree) => Guarded([OfTree(tree)], () => AskPublicationUnguarded(id, tree));

    private bool AskPublicationUnguarded(string id, string tree)
    {
        var stored = _context.Trees.SingleOrDefault(candidate => candidate.Id == tree && candidate.Owner == id);
        if (stored is null || !IsOpen(id) || stored.PublicationAskedAt is not null)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        stored.PublicationAskedAt = Now;
        Record(tree, id, "publication-asked", stored.Name);
        Save();

        // The accounts the tree names are asked – and told that they are.
        foreach (var named in _context.Persons.AsNoTracking().Where(person => person.Tree == tree && person.Account != null && person.Account != id).Select(person => person.Account!).Distinct().ToList())
        {
            Tell(named, always: false, to => NoticeMail.PublicationAsked(to.Language, _site, to.Email!, NameOf(id), $"tree-publication-asked:{tree}:{named}:{Now:yyyyMMdd}"));
        }

        return true;
    }

    /// <summary>
    /// Agrees to be named in a tree that is to be public, or takes that back. Only an account that is a record
    /// of the tree can, for itself, and only once the owner asked. Taken back, the tree is hidden at once.
    /// </summary>
    public bool Consent(string id, string tree, bool agree)
    {
        var asked = _context.Trees.AsNoTracking().Any(candidate => candidate.Id == tree && candidate.PublicationAskedAt != null);
        var record = _context.Persons.AsNoTracking().FirstOrDefault(person => person.Tree == tree && person.Account == id);
        var stored = _context.PublicationConsents.SingleOrDefault(consent => consent.Tree == tree && consent.Account == id);
        if (!asked || record is null || !IsOpen(id) || Role(id, tree) == TreeRole.None || agree == (stored is not null))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        if (agree)
        {
            _context.PublicationConsents.Add(new StoredPublicationConsent { Tree = tree, Account = id, At = Now });
        }
        else
        {
            _context.PublicationConsents.Remove(stored!);
        }

        Record(tree, id, agree ? "consent-given" : "consent-withdrawn", record.Name);
        Save();
        return true;
    }

    /// <summary>Makes a tree public. Only the owner can, once asked, and only when nobody stands in the way.</summary>
    public bool Publish(string id, string tree)
    {
        var stored = _context.Trees.SingleOrDefault(candidate => candidate.Id == tree && candidate.Owner == id);
        if (stored is null || !IsOpen(id) || stored.PublicationAskedAt is null || stored.PublishedAt is not null || IsRestricted(RestrictionKind.Tree, tree) || !Blockers(tree).IsEmpty)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        stored.PublishedAt = Now;
        Record(tree, id, "published", stored.Name);
        Save();
        return true;
    }

    /// <summary>
    /// Takes the making public of a tree back, whether it was only asked or done. Only the owner can. Nobody
    /// but those it is shared with sees the tree afterwards, and what was agreed is forgotten.
    /// </summary>
    public bool Unpublish(string id, string tree)
    {
        var stored = _context.Trees.SingleOrDefault(candidate => candidate.Id == tree && candidate.Owner == id);
        if (stored is null || !IsOpen(id) || stored.PublicationAskedAt is null)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        (stored.PublicationAskedAt, stored.PublishedAt) = (null, null);
        _context.PublicationConsents.RemoveRange(_context.PublicationConsents.Where(consent => consent.Tree == tree));
        Record(tree, id, "unpublished", stored.Name);
        Save();
        return true;
    }

    /// <summary>
    /// Returns a tree as everybody may see it, or <see langword="null"/> when it is not public: its name, its
    /// people with their years and its links – not who it is shared with, not its history, not which account a
    /// record is. A tree its owner made public is shown only while everybody it names agrees.
    /// </summary>
    public TreeView? PublicTree(string tree)
    {
        var stored = _context.Trees.AsNoTracking().SingleOrDefault(candidate => candidate.Id == tree && candidate.PublishedAt != null);
        if (stored is null || !IsOpen(stored.Owner) || IsRestricted(RestrictionKind.Tree, tree) || !Blockers(tree).IsEmpty || Tree(stored.Owner, tree) is not { } whole)
        {
            return null;
        }

        return new(
            whole.Tree with { Role = TreeRole.None },
            // The picture of a person is public only where that photograph is.
            [.. whole.People.Select(person => person with { Account = null, Claimable = false, MayDescribe = false, Picture = person.Picture is { } picture ? Photo(null, picture.Id) : null })],
            whole.Links,
            [],
            []);
    }

    // Where the making public of a tree stands for a viewer who may see the tree.
    private PublicationState Publication(StoredTree tree, string viewer, TreeRole role)
    {
        if (tree.PublicationAskedAt is null)
        {
            return PublicationState.None;
        }

        var blockers = Blockers(tree.Id);
        var named = _context.Persons.AsNoTracking().Any(person => person.Tree == tree.Id && person.Account == viewer);
        var consented = _context.PublicationConsents.AsNoTracking().Any(consent => consent.Tree == tree.Id && consent.Account == viewer);
        return new(
            true,
            tree.PublishedAt is not null,
            tree.PublishedAt is not null && blockers.IsEmpty,
            role == TreeRole.Owner ? blockers : [],
            named,
            named && consented);
    }

    // Who stands in the way of making a tree public: asked of the rule, which is told for every record whether
    // it may be public – somebody who died, or somebody whose account agreed.
    private ImmutableArray<PublicationBlocker> Blockers(string tree)
    {
        var people = _context.Persons.AsNoTracking().Where(person => person.Tree == tree).ToList();
        var agreed = _context.PublicationConsents.AsNoTracking().Where(consent => consent.Tree == tree).Select(consent => consent.Account).ToHashSet(StringComparer.Ordinal);
        var open = Names(people.Select(person => person.Account).OfType<string>());
        var owner = new AccountId(_context.Trees.AsNoTracking().Where(candidate => candidate.Id == tree).Select(candidate => candidate.Owner).Single());

        // An account that is closed is, for this, no account: it cannot be asked and its agreement does not hold.
        string? AccountOf(StoredPerson person) => person.Account is { } account && open.ContainsKey(account) ? account : null;
        var records = people.Select(person => new PersonRecord(
            new PersonId(person.Id),
            person.Name,
            null,
            Agreed(person) ? AccessScope.Everybody(owner) : AccessScope.OnlyMe(owner)));

        // Somebody who died is named on the owner's word (D-053) – as long as the record is not an open
        // account: an account is alive, whatever the record says, and is asked itself.
        bool Agreed(StoredPerson person) => AccountOf(person) is { } account ? agreed.Contains(account) : person.Deceased;
        var blocked = PublicationCheck.Blockers(records, [], []).Select(finding => finding.What).ToHashSet(StringComparer.Ordinal);
        return [.. people
            .Where(person => blocked.Contains($"person {person.Id}"))
            .OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(person => new PublicationBlocker(person.Id, person.Name, AccountOf(person) is null ? "no-account" : "no-consent"))];
    }

    // What an account agreed to in a tree is forgotten when it is no record of the tree any more.
    private void ForgetConsent(string tree, string account) =>
        _context.PublicationConsents.RemoveRange(_context.PublicationConsents.Where(consent => consent.Tree == tree && consent.Account == account));
}
