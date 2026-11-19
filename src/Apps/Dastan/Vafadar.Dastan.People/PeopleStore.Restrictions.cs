using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <summary>What of an account a restriction is about.</summary>
public enum RestrictionKind
{
    /// <summary>A photograph: nobody but its owner sees it.</summary>
    Photo = 0,

    /// <summary>A family tree: it is not public and cannot be made public.</summary>
    Tree = 1,

    /// <summary>The account: it is closed by who runs the site.</summary>
    Account = 2,
}

/// <summary>
/// A restriction as the person it concerns reads it: the statement of reasons Article 17 of the Digital
/// Services Act asks for – what was restricted, why, on which ground, and how to contest it.
/// </summary>
/// <param name="Number">The number of the restriction, which the person names when contesting it.</param>
/// <param name="Kind">What it is about.</param>
/// <param name="Target">The photograph or the tree, by its identifier; nothing for the account.</param>
/// <param name="Reason">The facts and circumstances the decision rests on, as who runs the site wrote them.</param>
/// <param name="Ground">The law or the rule of the terms the decision rests on.</param>
/// <param name="Noticed">Whether the decision followed a notice of somebody, and not the site's own look.</param>
/// <param name="At">When it was imposed.</param>
public sealed record RestrictionInfo(long Number, RestrictionKind Kind, string? Target, string Reason, string Ground, bool Noticed, DateTime At);

/// <content>
/// What who runs the site does about content that may not stay (the terms of the site, and Articles 16 and 17 of
/// Regulation (EU) 2022/2065): a photograph is taken out of sight, a tree is taken back from the public, an
/// account is closed – each with a statement of reasons the person concerned reads on the site, since the site
/// knows no mail address to send it to. Nothing here is done by the site itself: every restriction is an act of
/// its operator, made with a command, and can be lifted the same way.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>The longest text of a reason or a ground, in characters.</summary>
    public const int LongestReason = 2000;

    /// <summary>
    /// Restricts a photograph or a tree, or closes an account. Returns the number of the restriction, or
    /// nothing when there is no such photograph, tree or open account, or the words are missing.
    /// </summary>
    /// <param name="kind">What is restricted.</param>
    /// <param name="target">The identifier of the photograph or the tree, or the name of the account.</param>
    /// <param name="reason">The facts the decision rests on.</param>
    /// <param name="ground">The law or the rule of the terms it rests on.</param>
    /// <param name="noticed">Whether it followed a notice.</param>
    public long? Restrict(RestrictionKind kind, string target, string reason, string ground, bool noticed)
    {
        ArgumentNullException.ThrowIfNull(target);
        var (why, rule) = (reason?.Trim() ?? string.Empty, ground?.Trim() ?? string.Empty);
        if (why.Length is 0 or > LongestReason || rule.Length is 0 or > LongestReason)
        {
            return null;
        }

        string? account;
        string? about = target;
        switch (kind)
        {
            case RestrictionKind.Photo:
                account = _context.Photos.AsNoTracking().Where(photo => photo.Id == target).Select(photo => photo.Owner).SingleOrDefault();
                break;
            case RestrictionKind.Tree:
                account = _context.Trees.AsNoTracking().Where(tree => tree.Id == target).Select(tree => tree.Owner).SingleOrDefault();
                break;
            default:
                var key = Key(target.Trim());
                var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.NameKey == key && candidate.ClosedAt == null);
                account = visitor?.Id;
                about = null;
                if (visitor is not null)
                {
                    // Closed like an account its visitor closed – shown nowhere, signed out everywhere – and kept
                    // for the same time, in which the visitor can contest it. Only the operator opens it again.
                    (visitor.ClosedAt, visitor.ClosedByOperator, visitor.Stamp) = (Now, true, NewStamp());
                }

                break;
        }

        if (account is null)
        {
            _context.ChangeTracker.Clear();
            return null;
        }

        var stored = new StoredRestriction { Account = account, Kind = (int)kind, Target = about, Reason = why, Ground = rule, Noticed = noticed, ImposedAt = Now };
        _context.Restrictions.Add(stored);
        if (kind == RestrictionKind.Tree)
        {
            Record(target, null, "operator-restricted", string.Empty);
        }

        Save();
        _restricted = null;
        return stored.Number;
    }

    /// <summary>Lifts a restriction. An account that was closed by it is opened again. Returns whether there was one to lift.</summary>
    public bool Lift(long number)
    {
        var stored = _context.Restrictions.SingleOrDefault(candidate => candidate.Number == number && candidate.LiftedAt == null);
        if (stored is null)
        {
            return false;
        }

        stored.LiftedAt = Now;
        if ((RestrictionKind)stored.Kind == RestrictionKind.Tree && stored.Target is { } tree && _context.Trees.AsNoTracking().Any(candidate => candidate.Id == tree))
        {
            Record(tree, null, "operator-lifted", string.Empty);
        }

        if ((RestrictionKind)stored.Kind == RestrictionKind.Account
            && _context.Visitors.SingleOrDefault(candidate => candidate.Id == stored.Account && candidate.ClosedByOperator) is { } visitor)
        {
            (visitor.ClosedAt, visitor.ClosedByOperator, visitor.Stamp) = (null, false, NewStamp());
        }

        Save();
        _restricted = null;
        return true;
    }

    /// <summary>Returns the restrictions that stand on what belongs to an open account: what its visitor reads on the page of the account.</summary>
    public IReadOnlyList<RestrictionInfo> Restrictions(string id) =>
        [.. _context.Restrictions.AsNoTracking().Where(candidate => candidate.Account == id && candidate.LiftedAt == null).OrderBy(candidate => candidate.Number).AsEnumerable().Select(Info)];

    /// <summary>Returns every restriction of the store that stands, for who runs the site.</summary>
    public IReadOnlyList<(RestrictionInfo Restriction, string Account)> AllRestrictions()
    {
        var standing = _context.Restrictions.AsNoTracking().Where(candidate => candidate.LiftedAt == null).OrderBy(candidate => candidate.Number).ToList();
        var names = _context.Visitors.AsNoTracking().ToDictionary(visitor => visitor.Id, visitor => visitor.Name, StringComparer.Ordinal);
        return [.. standing.Select(stored => (Info(stored), names.GetValueOrDefault(stored.Account) ?? stored.Account))];
    }

    /// <summary>
    /// Returns why an account was closed by who runs the site, to somebody who proves to be its visitor with the
    /// name or the address and the password. It is also sent to the address of the account; this is how it is
    /// read on the site. Nothing for any other account, and nothing for a wrong password, which is counted.
    /// </summary>
    public RestrictionInfo? ClosedByOperator(string nameOrMail, string password)
    {
        ArgumentNullException.ThrowIfNull(nameOrMail);
        ArgumentNullException.ThrowIfNull(password);
        return Accounts.Proves(nameOrMail, password) is { } id ? ClosedByOperator(id) : null;
    }

    /// <summary>
    /// Returns why an account was closed by who runs the site, for who has just proved to be its visitor – by
    /// the password, or through the provider of an account that has no password. Nothing for an account that
    /// the operator did not close.
    /// </summary>
    public RestrictionInfo? ClosedByOperator(string id)
    {
        if (!_context.Visitors.AsNoTracking().Any(candidate => candidate.Id == id && candidate.ClosedByOperator))
        {
            return null;
        }

        return _context.Restrictions.AsNoTracking()
            .Where(candidate => candidate.Account == id && candidate.Kind == (int)RestrictionKind.Account && candidate.LiftedAt == null)
            .OrderByDescending(candidate => candidate.Number)
            .AsEnumerable()
            .Select(Info)
            .FirstOrDefault();
    }

    /// <summary>Returns a restriction that stands, by its number, or nothing.</summary>
    public RestrictionInfo? Restriction(long number) =>
        _context.Restrictions.AsNoTracking().Where(candidate => candidate.Number == number && candidate.LiftedAt == null).AsEnumerable().Select(Info).FirstOrDefault();

    /// <summary>Returns the records of the persons in a tree with their identifiers, for who runs the site.</summary>
    public IReadOnlyList<(string Id, string Name)> PersonsOfTree(string tree) =>
        [.. _context.Persons.AsNoTracking().Where(person => person.Tree == tree).OrderBy(person => person.Name).Select(person => new { person.Id, person.Name }).AsEnumerable().Select(person => (person.Id, person.Name))];

    /// <summary>
    /// Takes the record of a person out of a tree, with its links: what who runs the site does for a person
    /// who objects to being in the tree (Articles 17 and 21 of Regulation (EU) 2016/679) when the owner of the
    /// tree did not. The history of the tree says that it was done, and not the name that was taken out.
    /// </summary>
    public bool RemovePersonByOperator(string tree, string person)
    {
        var stored = _context.Persons.SingleOrDefault(candidate => candidate.Id == person && candidate.Tree == tree);
        if (stored is null)
        {
            return false;
        }

        _context.Kinships.RemoveRange(_context.Kinships.Where(link => link.Tree == tree && (link.From == person || link.To == person)));
        if (stored.Account is { } account)
        {
            ForgetConsent(tree, account);
        }

        _context.Persons.Remove(stored);
        Record(tree, null, "operator-person-removed", string.Empty);
        Save();
        return true;
    }

    private HashSet<string>? _restricted;

    // Whether a photograph or a tree is restricted. Asked for every photograph a page shows, so read once.
    private bool IsRestricted(RestrictionKind kind, string target)
    {
        _restricted ??= [.. _context.Restrictions.AsNoTracking().Where(candidate => candidate.LiftedAt == null && candidate.Target != null).Select(candidate => candidate.Kind + ":" + candidate.Target)];
        return _restricted.Contains($"{(int)kind}:{target}");
    }

    private static RestrictionInfo Info(StoredRestriction stored) =>
        new(stored.Number, (RestrictionKind)stored.Kind, stored.Target, stored.Reason, stored.Ground, stored.Noticed, stored.ImposedAt);
}
