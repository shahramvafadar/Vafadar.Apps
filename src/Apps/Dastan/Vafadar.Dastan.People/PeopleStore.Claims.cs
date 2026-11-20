using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>The claim of an account that a record of a tree is the same person, as somebody who may see it is shown it.</summary>
/// <param name="Number">The number of the claim.</param>
/// <param name="Person">The record that is claimed.</param>
/// <param name="By">The account that claims it.</param>
/// <param name="Mine">Whether the account that asks made the claim.</param>
/// <param name="CanReview">Whether the account that asks is the one to confirm or refuse it.</param>
public sealed record ClaimInfo(long Number, string Person, PersonRef By, bool Mine, bool CanReview);

/// <content>
/// The claim of an account on the record of a person (step 16.4, D-052, PRIV-004): "this record is me". It is
/// confirmed or refused by the one who made the record – never by the claimant – and confirming it merges
/// nothing and widens no audience: the record then names the account, and that is all.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>
    /// Claims a record of a tree for an account. Returns <c>claimed</c>, or why not: <c>refused</c> (the account
    /// may not see the tree), <c>unknown</c>, <c>taken</c> (the record is somebody's already), <c>own</c> (the
    /// account has a record or a claim in this tree already), <c>nobody</c> (there is nobody else to confirm it).
    /// </summary>
    private string ClaimUnguarded(string id, string tree, string person)
    {
        if (Role(id, tree) == TreeRole.None)
        {
            return "refused";
        }

        var record = _context.Persons.AsNoTracking().SingleOrDefault(candidate => candidate.Id == person && candidate.Tree == tree);
        if (record is null)
        {
            return "unknown";
        }

        if (record.Account is not null)
        {
            return "taken";
        }

        if (_context.Persons.AsNoTracking().Any(candidate => candidate.Tree == tree && candidate.Account == id)
            || _context.Claims.AsNoTracking().Any(claim => claim.Tree == tree && claim.Account == id))
        {
            return "own";
        }

        if (Reviewer(record, id) is null)
        {
            return "nobody";
        }

        _context.Claims.Add(new StoredClaim { Tree = tree, Person = person, Account = id, At = Now });
        Record(tree, id, "claim-made", record.Name);
        Save();
        return "claimed";
    }

    /// <summary>
    /// Confirms or refuses a claim. Only the one who made the record can, or the owner of the tree when that
    /// account is gone – and never the claimant (PRIV-004). Confirmed, the record names the account; nothing is
    /// merged and nobody sees more than before.
    /// </summary>
    public bool ReviewClaim(string id, string tree, long claim, bool confirm) =>
        Guarded([OfTree(tree)], () => ReviewClaimUnguarded(id, tree, claim, confirm));

    private bool ReviewClaimUnguarded(string id, string tree, long claim, bool confirm)
    {
        var stored = _context.Claims.SingleOrDefault(candidate => candidate.Number == claim && candidate.Tree == tree);
        var record = stored is null ? null : _context.Persons.SingleOrDefault(candidate => candidate.Id == stored.Person && candidate.Tree == tree);
        if (stored is null || record is null || Reviewer(record, stored.Account) != id)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        // The rule decides: nobody reviews their own claim, and a claim is reviewed once.
        var rule = new IdentityClaim(new PersonId(record.Id), new AccountId(stored.Account));
        if (rule.Review(new AccountId(id), confirm) != ActOutcome.Done)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        // A claim on a record somebody was confirmed for meanwhile, or by somebody who may not see the tree any
        // more, confirms nothing.
        if (rule.State == ClaimState.Confirmed && record.Account is null && Role(stored.Account, tree) != TreeRole.None)
        {
            record.Account = stored.Account;
            Record(tree, id, "claim-confirmed", record.Name);
        }
        else
        {
            Record(tree, id, "claim-refused", record.Name);
        }

        _context.Claims.Remove(stored);
        Save();
        return true;
    }

    /// <summary>
    /// Takes back what an account said about a record of a tree: its claim that waits, or that the record is
    /// this account. From then on the record names nobody.
    /// </summary>
    public bool Unclaim(string id, string tree, string person) =>
        Guarded([OfTree(tree)], () => UnclaimUnguarded(id, tree, person));

    private bool UnclaimUnguarded(string id, string tree, string person)
    {
        var record = _context.Persons.SingleOrDefault(candidate => candidate.Id == person && candidate.Tree == tree);
        var claim = _context.Claims.SingleOrDefault(candidate => candidate.Tree == tree && candidate.Person == person && candidate.Account == id);
        if (record is null || (claim is null && record.Account != id))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        if (claim is not null)
        {
            _context.Claims.Remove(claim);
        }

        if (record.Account == id)
        {
            record.Account = null;
            ForgetConsent(tree, id);

            // What the account said about itself goes with it: of a living person nobody asked, nothing is kept.
            ForgetProfileIfNotKept(record);
        }

        Record(tree, id, "claim-withdrawn", record.Name);
        Save();
        return true;
    }

    // Who confirms a claim on a record: the account that made it, when it is open and still may see the tree;
    // else the owner of the tree. Never the claimant – then there is nobody.
    private string? Reviewer(StoredPerson record, string claimant)
    {
        var maker = record.CreatedBy is { } made && IsOpen(made) && Role(made, record.Tree) != TreeRole.None ? made : null;
        var reviewer = maker ?? _context.Trees.AsNoTracking().Where(tree => tree.Id == record.Tree).Select(tree => tree.Owner).SingleOrDefault();
        return reviewer is null || reviewer == claimant || !IsOpen(reviewer) ? null : reviewer;
    }

    // An account that may not see a tree any more is no record of it and claims none.
    private void LeaveRecords(string tree, string account)
    {
        _context.Claims.RemoveRange(_context.Claims.Where(claim => claim.Tree == tree && claim.Account == account));
        ForgetConsent(tree, account);
        foreach (var record in _context.Persons.Where(person => person.Tree == tree && person.Account == account))
        {
            record.Account = null;
        }
    }
}
