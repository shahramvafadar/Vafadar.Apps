using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <summary>An account as another account is shown it: the identifier and the name.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
public sealed record PersonRef(string Id, string Name);

/// <summary>A relationship as one of its two sides sees it.</summary>
/// <param name="Id">The identifier of the relationship.</param>
/// <param name="Other">The other side.</param>
/// <param name="Kind">What the relationship is called: one of <see cref="PeopleStore.RelationshipKinds"/>.</param>
/// <param name="State">Where it stands: asked or accepted.</param>
/// <param name="AskedByMe">Whether this side asked.</param>
/// <param name="Shown">Who this side lets see it.</param>
/// <param name="Circle">For a circle: which one.</param>
public sealed record RelationshipInfo(string Id, PersonRef Other, string Kind, RelationshipState State, bool AskedByMe, ScopeKind Shown, long? Circle);

/// <summary>A circle of an account: a name for some of the people it has a relationship with.</summary>
/// <param name="Number">The number of the circle in the store.</param>
/// <param name="Name">The name its owner gave it.</param>
/// <param name="Members">Who is in it.</param>
public sealed record CircleInfo(long Number, string Name, ImmutableArray<PersonRef> Members);

/// <summary>A relationship as a page about one of its sides shows it.</summary>
/// <param name="Other">The other side.</param>
/// <param name="Kind">What the relationship is called.</param>
public sealed record ShownRelationship(PersonRef Other, string Kind);

/// <summary>What a viewer may see of an account: its name and the relationships both sides let that viewer see.</summary>
/// <param name="Person">The account.</param>
/// <param name="Relationships">The relationships the viewer may see.</param>
public sealed record ProfileView(PersonRef Person, ImmutableArray<ShownRelationship> Relationships);

/// <content>
/// The relationships two accounts show together (step 16.3, PRIV-005 to PRIV-009) and the circles that name an
/// audience. What an act may do and who may see a relationship is decided by the rules of step 15.2
/// (<see cref="SocialRelationship"/>, <see cref="AccessScope"/>); the store keeps where things stand.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>What a relationship can be called.</summary>
    public static readonly ImmutableArray<string> RelationshipKinds = ["friend", "close-friend", "family", "partner", "colleague", "classmate", "other"];

    /// <summary>How many requests of an account can wait for an answer at the same time.</summary>
    public const int OpenRequests = 20;

    /// <summary>How many circles an account can have.</summary>
    public const int MostCircles = 20;

    /// <summary>The greatest length of the name of a circle.</summary>
    public const int LongestCircleName = 40;

    /// <summary>How long somebody who was declined waits before asking the same account again.</summary>
    public static readonly TimeSpan AskAgainAfter = TimeSpan.FromDays(30);

    /// <summary>
    /// Asks another account, by its name, to show a relationship together. Returns <c>asked</c>, or why not:
    /// <c>kind</c>, <c>unknown</c>, <c>self</c>, <c>exists</c> (the two have a request or a relationship already),
    /// <c>declined-lately</c>, <c>too-many</c>.
    /// </summary>
    private string AskUnguarded(string id, string name, string kind)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!IsOpen(id))
        {
            return "unknown";
        }

        if (!RelationshipKinds.Contains(kind))
        {
            return "kind";
        }

        var key = Key(name.Trim());
        var other = _context.Visitors.AsNoTracking().SingleOrDefault(visitor => visitor.NameKey == key && visitor.ClosedAt == null);
        if (other is null)
        {
            return "unknown";
        }

        if (other.Id == id)
        {
            return "self";
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var between = _context.Relationships.AsNoTracking().Where(stored => (stored.From == id && stored.To == other.Id) || (stored.From == other.Id && stored.To == id)).ToList();
        if (between.Any(stored => (RelationshipState)stored.State is RelationshipState.Requested or RelationshipState.Accepted))
        {
            return "exists";
        }

        if (between.Any(stored => stored.From == id && (RelationshipState)stored.State == RelationshipState.Rejected && stored.ChangedAt > now - AskAgainAfter))
        {
            return "declined-lately";
        }

        if (_context.Relationships.Count(stored => stored.From == id && stored.State == (int)RelationshipState.Requested) >= OpenRequests)
        {
            return "too-many";
        }

        var rule = new SocialRelationship(Guid.NewGuid().ToString("N"), new AccountId(id), new AccountId(other.Id), kind);
        rule.Request(rule.From);
        _context.Relationships.Add(new StoredRelationship
        {
            Id = rule.Id,
            From = id,
            To = other.Id,
            Kind = kind,
            State = (int)rule.State,
            CreatedAt = now,
            ChangedAt = now,
        });
        Save();
        Tell(other.Id, always: false, to => NoticeMail.RelationshipAsked(to.Language, _site, to.Email!, NameOf(id), $"relationship-asked:{rule.Id}"));
        return "asked";
    }

    /// <summary>
    /// Returns the requests and the relationships of an account with accounts that are open: first what waits
    /// for its answer, then what it asked, then what was accepted – each by the name of the other side.
    /// </summary>
    public IReadOnlyList<RelationshipInfo> Relationships(string id)
    {
        if (!IsOpen(id))
        {
            return [];
        }

        var stored = _context.Relationships.AsNoTracking()
            .Where(relationship => (relationship.From == id || relationship.To == id)
                && (relationship.State == (int)RelationshipState.Requested || relationship.State == (int)RelationshipState.Accepted))
            .ToList();
        var others = Names(stored.Select(relationship => relationship.From == id ? relationship.To : relationship.From));
        return [.. stored
            .Where(relationship => others.ContainsKey(relationship.From == id ? relationship.To : relationship.From))
            .Select(relationship =>
            {
                var mine = relationship.From == id;
                return new RelationshipInfo(
                    relationship.Id,
                    others[mine ? relationship.To : relationship.From],
                    relationship.Kind,
                    (RelationshipState)relationship.State,
                    mine,
                    (ScopeKind)(mine ? relationship.FromScope : relationship.ToScope),
                    mine ? relationship.FromCircle : relationship.ToCircle);
            })
            .OrderBy(relationship => relationship.State == RelationshipState.Accepted ? 2 : relationship.AskedByMe ? 1 : 0)
            .ThenBy(relationship => relationship.Other.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>Returns how many requests wait for the answer of an account.</summary>
    public int Waiting(string id) => Relationships(id).Count(relationship => relationship.State == RelationshipState.Requested && !relationship.AskedByMe);

    /// <summary>
    /// Answers a request. Only the side that was asked can – nobody accepts for another account (PRIV-005).
    /// A request that was declined is kept for <see cref="AskAgainAfter"/>, so that it is not asked again at once.
    /// </summary>
    public bool Answer(string id, string relationship, bool accept) => Act(id, relationship, (rule, me) => accept ? rule.Accept(me) : rule.Reject(me), remove: false);

    /// <summary>Takes a request back before it was answered. Only the side that asked can. Nothing of it is kept.</summary>
    public bool Cancel(string id, string relationship) => Act(id, relationship, (rule, me) => rule.Cancel(me), remove: true);

    /// <summary>
    /// Ends a relationship that was accepted. Either side can, without the other (PRIV-009). Nothing of it is
    /// kept, and each side leaves the circles of the other.
    /// </summary>
    public bool End(string id, string relationship) => Act(id, relationship, (rule, me) => rule.End(me), remove: true);

    /// <summary>
    /// Sets who one side lets see a relationship: itself and the other side only, one of its circles, or
    /// everybody. A side sets its own and no other; the relationship is shown to who both sides allow (PRIV-008).
    /// </summary>
    public bool Show(string id, string relationship, ScopeKind kind, long? circle = null)
    {
        var stored = _context.Relationships.SingleOrDefault(candidate => candidate.Id == relationship);
        if (stored is null || !IsOpen(id) || !Enum.IsDefined(kind)
            || (kind == ScopeKind.Circle && !_context.Circles.AsNoTracking().Any(candidate => candidate.Number == circle && candidate.Owner == id)))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        var kept = kind == ScopeKind.Circle ? circle : null;
        var rule = Rule(stored);
        if (rule.Show(new AccountId(id), Scope(id, kind, kept)) != ActOutcome.Done)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        if (stored.From == id)
        {
            (stored.FromScope, stored.FromCircle) = ((int)kind, kept);
        }
        else
        {
            (stored.ToScope, stored.ToCircle) = ((int)kind, kept);
        }

        Save();
        return true;
    }

    /// <summary>Returns the circles of an account, by their names, each with its members that are open accounts.</summary>
    public IReadOnlyList<CircleInfo> Circles(string id)
    {
        var circles = _context.Circles.AsNoTracking().Where(circle => circle.Owner == id).ToList();
        var numbers = circles.Select(circle => circle.Number).ToList();
        var members = _context.CircleMembers.AsNoTracking().Where(member => numbers.Contains(member.Circle)).ToList();
        var names = Names(members.Select(member => member.Member));
        return [.. circles
            .OrderBy(circle => circle.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(circle => new CircleInfo(
                circle.Number,
                circle.Name,
                [.. members.Where(member => member.Circle == circle.Number && names.ContainsKey(member.Member)).Select(member => names[member.Member]).OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)]))];
    }

    /// <summary>Makes a circle. Returns its number, or <see langword="null"/> when the name is none, is taken or the account has as many circles as it may.</summary>
    private long? AddCircleUnguarded(string id, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var written = name.Trim();
        if (!IsOpen(id) || written.Length is 0 or > LongestCircleName || written.Any(char.IsControl))
        {
            return null;
        }

        var has = _context.Circles.AsNoTracking().Where(circle => circle.Owner == id).Select(circle => circle.Name).ToList();
        if (has.Count >= MostCircles || has.Contains(written, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var circle = new StoredCircle { Owner = id, Name = written };
        _context.Circles.Add(circle);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return circle.Number;
    }

    /// <summary>
    /// Says who is in a circle: of the accounts named, those the owner has an accepted relationship with. A
    /// circle names people the owner knows; it is no way to find out whether an account exists.
    /// </summary>
    public bool SetCircle(string id, long circle, IEnumerable<string> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (!IsOpen(id) || !_context.Circles.AsNoTracking().Any(candidate => candidate.Number == circle && candidate.Owner == id))
        {
            return false;
        }

        var known = Relationships(id).Where(relationship => relationship.State == RelationshipState.Accepted).Select(relationship => relationship.Other.Id).ToHashSet(StringComparer.Ordinal);
        _context.CircleMembers.RemoveRange(_context.CircleMembers.Where(member => member.Circle == circle));
        foreach (var member in members.Where(known.Contains).Distinct(StringComparer.Ordinal))
        {
            _context.CircleMembers.Add(new StoredCircleMember { Circle = circle, Member = member });
        }

        Save();
        return true;
    }

    /// <summary>
    /// Removes a circle. What was shown to it is shown to the two sides only from then on: taking an audience
    /// away never widens one.
    /// </summary>
    public bool RemoveCircle(string id, long circle)
    {
        var stored = _context.Circles.SingleOrDefault(candidate => candidate.Number == circle && candidate.Owner == id);
        if (stored is null)
        {
            return false;
        }

        foreach (var relationship in _context.Relationships.Where(candidate => candidate.FromCircle == circle || candidate.ToCircle == circle))
        {
            if (relationship.FromCircle == circle)
            {
                (relationship.FromScope, relationship.FromCircle) = ((int)ScopeKind.Private, null);
            }

            if (relationship.ToCircle == circle)
            {
                (relationship.ToScope, relationship.ToCircle) = ((int)ScopeKind.Private, null);
            }
        }

        foreach (var photo in _context.Photos.Where(candidate => candidate.Circle == circle))
        {
            (photo.Scope, photo.Circle) = ((int)ScopeKind.Private, null);
        }

        _context.Circles.Remove(stored);
        Save();
        return true;
    }

    /// <summary>
    /// Returns what a viewer may see of an open account – its name and the accepted relationships that both
    /// sides let this viewer see – or <see langword="null"/> when there is no such account. A viewer without an
    /// account sees what both sides show to everybody.
    /// </summary>
    public ProfileView? Profile(string? viewer, string id)
    {
        var person = _context.Visitors.AsNoTracking().SingleOrDefault(visitor => visitor.Id == id && visitor.ClosedAt == null);
        if (person is null)
        {
            return null;
        }

        AccountId? account = viewer is not null && IsOpen(viewer) ? new AccountId(viewer) : null;
        var accepted = _context.Relationships.AsNoTracking()
            .Where(relationship => (relationship.From == id || relationship.To == id) && relationship.State == (int)RelationshipState.Accepted)
            .ToList();
        var others = Names(accepted.Select(relationship => relationship.From == id ? relationship.To : relationship.From));

        // The scope is asked before anything is produced from a relationship (PRIV-012): what a viewer may not
        // see is not counted and not hinted at.
        return new(
            new PersonRef(person.Id, person.Name),
            [.. accepted
                .Where(relationship => others.ContainsKey(relationship.From == id ? relationship.To : relationship.From) && Rule(relationship).IsVisibleTo(account))
                .Select(relationship => new ShownRelationship(others[relationship.From == id ? relationship.To : relationship.From], relationship.Kind))
                .OrderBy(shown => shown.Other.Name, StringComparer.CurrentCultureIgnoreCase)]);
    }

    // Requests that were declined are forgotten when the time to wait is over.
    private void ForgetDeclined()
    {
        var before = _time.GetUtcNow().UtcDateTime - AskAgainAfter;
        _context.Relationships.RemoveRange(_context.Relationships.Where(relationship => relationship.State == (int)RelationshipState.Rejected && relationship.ChangedAt < before));
    }

    private bool Act(string id, string relationship, Func<SocialRelationship, AccountId, ActOutcome> act, bool remove)
    {
        var stored = _context.Relationships.SingleOrDefault(candidate => candidate.Id == relationship);
        if (stored is null || !IsOpen(id))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        var rule = Rule(stored);
        if (act(rule, new AccountId(id)) != ActOutcome.Done)
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        if (remove)
        {
            // The two are no longer people each other knows: neither stays in a circle of the other.
            var circles = _context.Circles.Where(circle => circle.Owner == stored.From || circle.Owner == stored.To).ToList();
            foreach (var circle in circles)
            {
                var other = circle.Owner == stored.From ? stored.To : stored.From;
                _context.CircleMembers.RemoveRange(_context.CircleMembers.Where(member => member.Circle == circle.Number && member.Member == other));
            }

            LeaveTrees(stored.From, stored.To);
            _context.Relationships.Remove(stored);
        }
        else
        {
            (stored.State, stored.ChangedAt) = ((int)rule.State, _time.GetUtcNow().UtcDateTime);
        }

        Save();
        return true;
    }

    // The relationship as the rules know it, brought to where the store says it stands.
    private SocialRelationship Rule(StoredRelationship stored)
    {
        var rule = new SocialRelationship(stored.Id, new AccountId(stored.From), new AccountId(stored.To), stored.Kind);
        rule.Request(rule.From);
        switch ((RelationshipState)stored.State)
        {
            case RelationshipState.Accepted:
                rule.Accept(rule.To);
                break;
            case RelationshipState.Rejected:
                rule.Reject(rule.To);
                break;
        }

        rule.Show(rule.From, Scope(stored.From, (ScopeKind)stored.FromScope, stored.FromCircle));
        rule.Show(rule.To, Scope(stored.To, (ScopeKind)stored.ToScope, stored.ToCircle));
        return rule;
    }

    // A circle that is gone or is not its owner's shows to nobody else.
    private AccessScope Scope(string owner, ScopeKind kind, long? circle)
    {
        var account = new AccountId(owner);
        if (kind == ScopeKind.Public)
        {
            return AccessScope.Everybody(account);
        }

        if (kind != ScopeKind.Circle || circle is null || !_context.Circles.AsNoTracking().Any(candidate => candidate.Number == circle && candidate.Owner == owner))
        {
            return AccessScope.OnlyMe(account);
        }

        return AccessScope.Circle(account, [.. _context.CircleMembers.AsNoTracking().Where(member => member.Circle == circle).Select(member => member.Member).AsEnumerable().Select(member => new AccountId(member))]);
    }

    private Dictionary<string, PersonRef> Names(IEnumerable<string> ids)
    {
        var asked = ids.Distinct().ToList();
        return _context.Visitors.AsNoTracking()
            .Where(visitor => asked.Contains(visitor.Id) && visitor.ClosedAt == null)
            .ToDictionary(visitor => visitor.Id, visitor => new PersonRef(visitor.Id, visitor.Name), StringComparer.Ordinal);
    }
}
