using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>What an account may do with a family tree (D-054). A greater role may do what a smaller one may.</summary>
public enum TreeRole
{
    /// <summary>Nothing: the tree does not exist for this account.</summary>
    None = 0,

    /// <summary>See the tree.</summary>
    Viewer = 1,

    /// <summary>See and change the tree.</summary>
    Editor = 2,

    /// <summary>The owner: change it, say who may see or change it, remove it.</summary>
    Owner = 3,
}

/// <summary>A family tree as a list shows it.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name its owner gave it.</param>
/// <param name="Owner">The owner.</param>
/// <param name="Role">What the account that asks may do with it.</param>
/// <param name="People">How many records of people it has.</param>
public sealed record TreeInfo(string Id, string Name, PersonRef Owner, TreeRole Role, int People);

/// <summary>The record of a person in a family tree. A record can be there without an account (PRIV-002).</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Born">The year of birth, when it is known.</param>
/// <param name="Died">The year of death, when it is known.</param>
/// <param name="Deceased">Whether the person has died.</param>
/// <param name="Parents">The records of the parents.</param>
/// <param name="Children">The records of the children.</param>
/// <param name="Partners">The records of the partners.</param>
/// <param name="Account">The account the record is, once a claim was confirmed (D-052).</param>
/// <param name="Claimable">Whether the account that asks can say that the record is it: the record is nobody's, the account is no record of the tree yet, and there is somebody else to confirm it.</param>
public sealed record TreePerson(string Id, string Name, int? Born, int? Died, bool Deceased, ImmutableArray<string> Parents, ImmutableArray<string> Children, ImmutableArray<string> Partners, PersonRef? Account = null, bool Claimable = false);

/// <summary>A link between two records of a tree.</summary>
/// <param name="Number">The number of the link.</param>
/// <param name="Kind"><c>parent</c>: the first is a parent of the second; <c>partner</c>: the two are partners.</param>
/// <param name="From">The first record.</param>
/// <param name="To">The second record.</param>
public sealed record TreeLink(long Number, string Kind, string From, string To);

/// <summary>An account that may see or change a tree.</summary>
/// <param name="Account">The account.</param>
/// <param name="Role">What it may do.</param>
public sealed record TreeMember(PersonRef Account, TreeRole Role);

/// <summary>A change of a tree, kept with who made it (D-054).</summary>
/// <param name="At">When.</param>
/// <param name="By">The name of who made it; nothing when that account is deleted.</param>
/// <param name="What">What was done: a short word the pages translate.</param>
/// <param name="Subject">What it was done to, as it was named then.</param>
public sealed record TreeChange(DateTime At, string? By, string What, string Subject);

/// <summary>A family tree as an account that may see it is shown it.</summary>
/// <param name="Tree">The tree.</param>
/// <param name="People">The records, by name.</param>
/// <param name="Links">The links.</param>
/// <param name="Members">Who may see or change it; for the owner only.</param>
/// <param name="History">The changes, the newest first.</param>
public sealed record TreeView(TreeInfo Tree, ImmutableArray<TreePerson> People, ImmutableArray<TreeLink> Links, ImmutableArray<TreeMember> Members, ImmutableArray<TreeChange> History)
{
    /// <summary>Gets the claims that wait and that the account that asks made or is to review.</summary>
    public ImmutableArray<ClaimInfo> Claims { get; init; } = [];

    /// <summary>Gets a value indicating whether the account that asks can claim a record: it is none and claims none yet.</summary>
    public bool CanClaim { get; init; }

    /// <summary>Gets where the making public of the tree stands (D-053).</summary>
    public PublicationState Publication { get; init; } = PublicationState.None;

    /// <summary>Returns the record with an identifier.</summary>
    public TreePerson Person(string id) => People.First(person => person.Id == id);
}

/// <content>
/// Family trees (step 16.4, D-054): a tree has an owner, who names the accounts that may change it and those
/// that may only see it; every change is kept with who made it. A tree is seen by nobody else.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>How many trees an account can own.</summary>
    public const int MostTrees = 20;

    /// <summary>How many records of people a tree can have.</summary>
    public const int MostPeople = 500;

    /// <summary>The greatest length of the name of a tree or of a person.</summary>
    public const int LongestTreeName = 80;

    /// <summary>How many changes of a tree are shown.</summary>
    public const int HistoryShown = 100;

    /// <summary>Makes a tree for an account and returns its identifier, or <see langword="null"/> when the name is none or the account owns as many trees as it may.</summary>
    public string? AddTree(string id, string name)
    {
        if (!IsOpen(id) || Named(name) is not { } written || _context.Trees.Count(tree => tree.Owner == id) >= MostTrees)
        {
            return null;
        }

        var tree = new StoredTree { Id = Guid.NewGuid().ToString("N"), Owner = id, Name = written, CreatedAt = Now };
        _context.Trees.Add(tree);
        Record(tree.Id, id, "tree-made", written);
        Save();
        return tree.Id;
    }

    /// <summary>Returns the trees an account may see: its own first, then those shared with it, each by name.</summary>
    public IReadOnlyList<TreeInfo> Trees(string id)
    {
        if (!IsOpen(id))
        {
            return [];
        }

        var shared = _context.TreeMembers.AsNoTracking().Where(member => member.Account == id).ToDictionary(member => member.Tree, member => (TreeRole)member.Role, StringComparer.Ordinal);
        var ids = shared.Keys.ToList();
        var trees = _context.Trees.AsNoTracking().Where(tree => tree.Owner == id || ids.Contains(tree.Id)).ToList();
        var owners = Names(trees.Select(tree => tree.Owner));
        var counts = _context.Persons.AsNoTracking().GroupBy(person => person.Tree).Select(group => new { Tree = group.Key, Count = group.Count() }).ToDictionary(group => group.Tree, group => group.Count, StringComparer.Ordinal);
        return [.. trees
            .Where(tree => owners.ContainsKey(tree.Owner))
            .Select(tree => new TreeInfo(tree.Id, tree.Name, owners[tree.Owner], tree.Owner == id ? TreeRole.Owner : shared[tree.Id], counts.GetValueOrDefault(tree.Id)))
            .OrderByDescending(tree => tree.Role == TreeRole.Owner)
            .ThenBy(tree => tree.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>
    /// Returns a tree as an account may see it, or <see langword="null"/> when the account may not see it – a
    /// tree that is not shared with somebody does not exist for them.
    /// </summary>
    public TreeView? Tree(string? viewer, string tree)
    {
        if (viewer is null || Trees(viewer).SingleOrDefault(candidate => candidate.Id == tree) is not { } info)
        {
            return null;
        }

        var links = _context.Kinships.AsNoTracking().Where(link => link.Tree == tree).OrderBy(link => link.Number).ToList();
        var people = _context.Persons.AsNoTracking().Where(person => person.Tree == tree).ToList();
        var members = info.Role != TreeRole.Owner ? [] : _context.TreeMembers.AsNoTracking().Where(member => member.Tree == tree).ToList();
        var changes = _context.TreeChanges.AsNoTracking().Where(change => change.Tree == tree).OrderByDescending(change => change.Number).Take(HistoryShown).ToList();
        var claims = _context.Claims.AsNoTracking().Where(claim => claim.Tree == tree).OrderBy(claim => claim.Number).ToList();
        var names = Names(members.Select(member => member.Account)
            .Concat(changes.Select(change => change.By).OfType<string>())
            .Concat(people.Select(person => person.Account).OfType<string>())
            .Concat(claims.Select(claim => claim.Account)));
        var canClaim = !people.Any(person => person.Account == viewer) && !claims.Any(claim => claim.Account == viewer);
        return new(
            info,
            [.. people.OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase).Select(person => new TreePerson(
                person.Id,
                person.Name,
                person.Born,
                person.Died,
                person.Deceased,
                [.. links.Where(link => link.Kind == "parent" && link.To == person.Id).Select(link => link.From)],
                [.. links.Where(link => link.Kind == "parent" && link.From == person.Id).Select(link => link.To)],
                [.. links.Where(link => link.Kind == "partner" && (link.From == person.Id || link.To == person.Id)).Select(link => link.From == person.Id ? link.To : link.From)],
                person.Account is { } account ? names.GetValueOrDefault(account) : null,
                canClaim && person.Account is null && Reviewer(person, viewer) is not null))],
            [.. links.Select(link => new TreeLink(link.Number, link.Kind, link.From, link.To))],
            [.. members.Where(member => names.ContainsKey(member.Account)).Select(member => new TreeMember(names[member.Account], (TreeRole)member.Role)).OrderBy(member => member.Account.Name, StringComparer.CurrentCultureIgnoreCase)],
            [.. changes.Select(change => new TreeChange(change.At, change.By is { } by && names.TryGetValue(by, out var account) ? account.Name : null, change.What, change.Subject))])
        {
            // A claim is shown to who made it and to who is to answer it, and to nobody else who sees the tree.
            Claims =
            [
                .. claims.Where(claim => names.ContainsKey(claim.Account))
                    .Select(claim => new ClaimInfo(claim.Number, claim.Person, names[claim.Account], claim.Account == viewer, Reviewer(people.First(person => person.Id == claim.Person), claim.Account) == viewer))
                    .Where(claim => claim.Mine || claim.CanReview),
            ],
            CanClaim = canClaim,
            Publication = Publication(_context.Trees.AsNoTracking().Single(candidate => candidate.Id == tree), viewer, info.Role),
        };
    }

    /// <summary>Gives a tree another name. Only its owner can.</summary>
    public bool RenameTree(string id, string tree, string name)
    {
        var stored = _context.Trees.SingleOrDefault(candidate => candidate.Id == tree && candidate.Owner == id);
        if (stored is null || !IsOpen(id) || Named(name) is not { } written)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        stored.Name = written;
        Record(tree, id, "tree-renamed", written);
        Save();
        return true;
    }

    /// <summary>Removes a tree with everything in it. Only its owner can.</summary>
    public bool RemoveTree(string id, string tree)
    {
        var stored = _context.Trees.SingleOrDefault(candidate => candidate.Id == tree && candidate.Owner == id);
        if (stored is null || !IsOpen(id))
        {
            return false;
        }

        _context.Trees.Remove(stored);
        Save();
        return true;
    }

    /// <summary>
    /// Says what an account may do with a tree: see it, change it, or nothing. Only the owner can, and only
    /// for an account the owner has an accepted relationship with – naming somebody is no way to find out
    /// whether an account exists.
    /// </summary>
    public bool SetTreeMember(string id, string tree, string account, TreeRole role)
    {
        if (!IsOpen(id) || role is not (TreeRole.None or TreeRole.Viewer or TreeRole.Editor)
            || !_context.Trees.AsNoTracking().Any(candidate => candidate.Id == tree && candidate.Owner == id))
        {
            return false;
        }

        var stored = _context.TreeMembers.SingleOrDefault(member => member.Tree == tree && member.Account == account);
        var known = Relationships(id).FirstOrDefault(relationship => relationship.State == RelationshipState.Accepted && relationship.Other.Id == account);
        if (role == TreeRole.None)
        {
            if (stored is null)
            {
                return false;
            }

            _context.TreeMembers.Remove(stored);
            LeaveRecords(tree, account);
        }
        else if (known is null)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
        else if (stored is null)
        {
            _context.TreeMembers.Add(new StoredTreeMember { Tree = tree, Account = account, Role = (int)role });
        }
        else
        {
            stored.Role = (int)role;
        }

        Record(tree, id, role switch { TreeRole.None => "member-removed", TreeRole.Editor => "member-editor", _ => "member-viewer" }, Names([account]).GetValueOrDefault(account)?.Name ?? string.Empty);
        Save();
        return true;
    }

    /// <summary>
    /// Adds the record of a person to a tree and returns its identifier, or <see langword="null"/> when the
    /// account may not change the tree, the record says something impossible or the tree is full. A record says
    /// little on purpose: a name and, when they are known, the years of birth and of death (PRIV-003).
    /// </summary>
    public string? AddPerson(string id, string tree, string name, int? born, int? died, bool deceased)
    {
        if (Role(id, tree) < TreeRole.Editor || Named(name) is not { } written || !Lived(born, died)
            || _context.Persons.Count(person => person.Tree == tree) >= MostPeople)
        {
            return null;
        }

        var person = new StoredPerson { Id = Guid.NewGuid().ToString("N"), Tree = tree, Name = written, Born = born, Died = died, Deceased = deceased || died is not null, CreatedBy = id, CreatedAt = Now };
        _context.Persons.Add(person);
        Record(tree, id, "person-added", written);
        Save();
        return person.Id;
    }

    /// <summary>Changes the record of a person. Who may change the tree can.</summary>
    public bool ChangePerson(string id, string tree, string person, string name, int? born, int? died, bool deceased)
    {
        var stored = _context.Persons.SingleOrDefault(candidate => candidate.Id == person && candidate.Tree == tree);
        if (stored is null || Role(id, tree) < TreeRole.Editor || Named(name) is not { } written || !Lived(born, died))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        (stored.Name, stored.Born, stored.Died, stored.Deceased) = (written, born, died, deceased || died is not null);

        // Somebody agreed to be named as the record was: changed, the record is agreed to again.
        if (stored.Account is { } account)
        {
            ForgetConsent(tree, account);
        }

        Record(tree, id, "person-changed", written);
        Save();
        return true;
    }

    /// <summary>Removes the record of a person, with its links. Who may change the tree can.</summary>
    public bool RemovePerson(string id, string tree, string person)
    {
        var stored = _context.Persons.SingleOrDefault(candidate => candidate.Id == person && candidate.Tree == tree);
        if (stored is null || Role(id, tree) < TreeRole.Editor)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        _context.Kinships.RemoveRange(_context.Kinships.Where(link => link.Tree == tree && (link.From == person || link.To == person)));
        if (stored.Account is { } account)
        {
            ForgetConsent(tree, account);
        }

        _context.Persons.Remove(stored);
        Record(tree, id, "person-removed", stored.Name);
        Save();
        return true;
    }

    /// <summary>
    /// Links two records of a tree: the first is a <c>parent</c> of the second, or the two are <c>partner</c>s.
    /// Returns <c>done</c>, or why not: <c>refused</c>, <c>kind</c>, <c>unknown</c>, <c>same</c>, <c>exists</c>,
    /// <c>loop</c> (nobody is their own ancestor).
    /// </summary>
    public string Link(string id, string tree, string kind, string from, string to)
    {
        if (Role(id, tree) < TreeRole.Editor)
        {
            return "refused";
        }

        if (kind is not ("parent" or "partner"))
        {
            return "kind";
        }

        var people = _context.Persons.AsNoTracking().Where(person => person.Tree == tree && (person.Id == from || person.Id == to)).ToDictionary(person => person.Id, person => person.Name, StringComparer.Ordinal);
        if (from == to)
        {
            return people.ContainsKey(from) ? "same" : "unknown";
        }

        if (people.Count != 2)
        {
            return "unknown";
        }

        var links = _context.Kinships.AsNoTracking().Where(link => link.Tree == tree).ToList();
        if (links.Any(link => link.Kind == kind && ((link.From == from && link.To == to) || (link.From == to && link.To == from))))
        {
            return "exists";
        }

        if (kind == "parent" && Descends(links, of: to, who: from))
        {
            return "loop";
        }

        _context.Kinships.Add(new StoredKinship { Tree = tree, Kind = kind, From = from, To = to });
        Record(tree, id, kind == "parent" ? "parent-added" : "partner-added", $"{people[from]} – {people[to]}");
        Save();
        return "done";
    }

    /// <summary>Removes a link. Who may change the tree can.</summary>
    public bool Unlink(string id, string tree, long link)
    {
        var stored = _context.Kinships.SingleOrDefault(candidate => candidate.Number == link && candidate.Tree == tree);
        if (stored is null || Role(id, tree) < TreeRole.Editor)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        var names = _context.Persons.AsNoTracking().Where(person => person.Id == stored.From || person.Id == stored.To).ToDictionary(person => person.Id, person => person.Name, StringComparer.Ordinal);
        _context.Kinships.Remove(stored);
        Record(tree, id, stored.Kind == "parent" ? "parent-removed" : "partner-removed", $"{names.GetValueOrDefault(stored.From)} – {names.GetValueOrDefault(stored.To)}");
        Save();
        return true;
    }

    private TreeRole Role(string id, string tree) => Trees(id).SingleOrDefault(candidate => candidate.Id == tree)?.Role ?? TreeRole.None;

    // Whether somebody is among the descendants of a record: then they cannot become its parent.
    private static bool Descends(List<StoredKinship> links, string of, string who)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { of };
        var open = new Queue<string>([of]);
        while (open.TryDequeue(out var person))
        {
            foreach (var child in links.Where(link => link.Kind == "parent" && link.From == person).Select(link => link.To))
            {
                if (child == who)
                {
                    return true;
                }

                if (seen.Add(child))
                {
                    open.Enqueue(child);
                }
            }
        }

        return false;
    }

    // Two accounts whose relationship ended name each other in no tree any more.
    private void LeaveTrees(string one, string other)
    {
        var trees = _context.Trees.AsNoTracking().Where(tree => tree.Owner == one || tree.Owner == other).Select(tree => new { tree.Id, tree.Owner }).ToList();
        foreach (var tree in trees)
        {
            var guest = tree.Owner == one ? other : one;
            _context.TreeMembers.RemoveRange(_context.TreeMembers.Where(member => member.Tree == tree.Id && member.Account == guest));
            LeaveRecords(tree.Id, guest);
        }
    }

    private void Record(string tree, string by, string what, string subject) =>
        _context.TreeChanges.Add(new StoredTreeChange { Tree = tree, By = by, At = Now, What = what, Subject = subject });

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private static string? Named(string? name)
    {
        var written = name?.Trim() ?? string.Empty;
        return written.Length is 0 or > LongestTreeName || written.Any(char.IsControl) ? null : written;
    }

    // A year is one of the calendar, and nobody dies before they are born.
    private static bool Lived(int? born, int? died) =>
        born is null or (>= 1 and <= 9999) && died is null or (>= 1 and <= 9999) && (born is null || died is null || born <= died);
}
