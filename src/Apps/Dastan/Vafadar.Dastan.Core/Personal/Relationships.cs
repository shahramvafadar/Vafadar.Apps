using System.Collections.Immutable;

namespace Vafadar.Dastan.Core.Personal;

/// <summary>Where a social relationship between two accounts stands (PRIV-007).</summary>
public enum RelationshipState
{
    /// <summary>Written by one side and not sent.</summary>
    Draft = 0,

    /// <summary>Sent to the other side, who has not answered.</summary>
    Requested = 1,

    /// <summary>Accepted by the other side.</summary>
    Accepted = 2,

    /// <summary>Declined by the other side.</summary>
    Rejected = 3,

    /// <summary>Taken back by the side that asked, before an answer.</summary>
    Cancelled = 4,

    /// <summary>Ended by either side after it was accepted.</summary>
    Ended = 5,
}

/// <summary>What became of an act on a relationship or an invitation.</summary>
public enum ActOutcome
{
    /// <summary>The act was done.</summary>
    Done = 0,

    /// <summary>Nothing changed: the act had been done before. Nobody is told a second time.</summary>
    AlreadyDone = 1,

    /// <summary>The act is not this account's to do, or comes too late.</summary>
    Refused = 2,
}

/// <summary>
/// A social relationship that two accounts show together, such as "close friend". It is displayable only after
/// the other person accepted it (accepted foundation of document 06), and then only to who both sides allow
/// (PRIV-008): one side's wider audience does not override the other's restriction.
/// </summary>
public sealed class SocialRelationship
{
    /// <summary>Initializes a new instance of the <see cref="SocialRelationship"/> class as a draft of one account.</summary>
    public SocialRelationship(string id, AccountId from, AccountId to, string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        if (from == to)
        {
            throw new ArgumentException("A relationship is between two accounts.", nameof(to));
        }

        (Id, From, To, Kind) = (id, from, to, kind);
        FromScope = AccessScope.OnlyMe(from);
        ToScope = AccessScope.OnlyMe(to);
    }

    /// <summary>Gets the identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the account that asked.</summary>
    public AccountId From { get; }

    /// <summary>Gets the account that was asked.</summary>
    public AccountId To { get; }

    /// <summary>Gets what the relationship is called.</summary>
    public string Kind { get; }

    /// <summary>Gets where the relationship stands.</summary>
    public RelationshipState State { get; private set; }

    /// <summary>Gets who the side that asked lets see the relationship.</summary>
    public AccessScope FromScope { get; private set; }

    /// <summary>Gets who the side that was asked lets see the relationship.</summary>
    public AccessScope ToScope { get; private set; }

    /// <summary>Gets how many times somebody was told of something: a request, an answer. An act done twice tells once.</summary>
    public int Notifications { get; private set; }

    /// <summary>Sends the request. Only the side that wrote it can; a request that was sent is not sent again.</summary>
    public ActOutcome Request(AccountId by) => (by == From, State) switch
    {
        (false, _) => ActOutcome.Refused,
        (true, RelationshipState.Draft) => Move(RelationshipState.Requested),
        (true, RelationshipState.Requested) => ActOutcome.AlreadyDone,
        _ => ActOutcome.Refused,
    };

    /// <summary>
    /// Accepts the request. Only the side that was asked can: nobody – no curator, no manager of a family –
    /// accepts for an independent account (PRIV-005). An acceptance that comes after the request was cancelled
    /// creates nothing.
    /// </summary>
    public ActOutcome Accept(AccountId by) => (by == To, State) switch
    {
        (false, _) => ActOutcome.Refused,
        (true, RelationshipState.Requested) => Move(RelationshipState.Accepted),
        (true, RelationshipState.Accepted) => ActOutcome.AlreadyDone,
        _ => ActOutcome.Refused,
    };

    /// <summary>Declines the request. Only the side that was asked can.</summary>
    public ActOutcome Reject(AccountId by) => (by == To, State) switch
    {
        (false, _) => ActOutcome.Refused,
        (true, RelationshipState.Requested) => Move(RelationshipState.Rejected),
        (true, RelationshipState.Rejected) => ActOutcome.AlreadyDone,
        _ => ActOutcome.Refused,
    };

    /// <summary>Takes the request back before it was answered. Only the side that asked can (PRIV-009).</summary>
    public ActOutcome Cancel(AccountId by) => (by == From, State) switch
    {
        (false, _) => ActOutcome.Refused,
        (true, RelationshipState.Requested) => Move(RelationshipState.Cancelled),
        (true, RelationshipState.Cancelled) => ActOutcome.AlreadyDone,
        _ => ActOutcome.Refused,
    };

    /// <summary>Ends a relationship that was accepted. Either side can (PRIV-009); from then on it is shown to nobody else.</summary>
    public ActOutcome End(AccountId by) => (IsParty(by), State) switch
    {
        (false, _) => ActOutcome.Refused,
        (true, RelationshipState.Accepted) => Move(RelationshipState.Ended),
        (true, RelationshipState.Ended) => ActOutcome.AlreadyDone,
        _ => ActOutcome.Refused,
    };

    /// <summary>Sets who one side lets see the relationship. A side sets its own scope and no other.</summary>
    public ActOutcome Show(AccountId by, AccessScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.Owner != by || !IsParty(by))
        {
            return ActOutcome.Refused;
        }

        if (by == From)
        {
            FromScope = scope;
        }
        else
        {
            ToScope = scope;
        }

        return ActOutcome.Done;
    }

    /// <summary>
    /// Returns whether a viewer may see the relationship. The two sides see it in every state; anybody else
    /// only once it is accepted, and only when both sides allow that viewer.
    /// </summary>
    public bool IsVisibleTo(AccountId? viewer) =>
        (viewer is { } account && IsParty(account))
        || (State == RelationshipState.Accepted && FromScope.Allows(viewer) && ToScope.Allows(viewer));

    private bool IsParty(AccountId account) => account == From || account == To;

    private ActOutcome Move(RelationshipState state)
    {
        State = state;
        Notifications++;
        return ActOutcome.Done;
    }
}

/// <summary>What an invitation is for. An invitation serves one purpose and no second (PRIV-006).</summary>
public enum InvitationPurpose
{
    /// <summary>To join: to have an account.</summary>
    Join = 0,

    /// <summary>To claim the record of a person as one's own; the claim is reviewed separately.</summary>
    ClaimRecord = 1,
}

/// <summary>Where an invitation stands.</summary>
public enum InvitationState
{
    /// <summary>It can be used.</summary>
    Open = 0,

    /// <summary>It was used.</summary>
    Used = 1,

    /// <summary>The one who sent it took it back.</summary>
    Revoked = 2,
}

/// <summary>
/// An invitation for somebody without an account, by mail or by a link (accepted foundation). Opening it,
/// registering, claiming a record and accepting a relationship are separate acts (PRIV-006): an invitation that
/// was used made an account and nothing else. It can be taken back, runs out, and is used once.
/// </summary>
public sealed class Invitation
{
    /// <summary>Initializes a new instance of the <see cref="Invitation"/> class.</summary>
    public Invitation(string id, AccountId by, InvitationPurpose purpose, DateOnly lastDay, PersonId? record = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        (Id, By, Purpose, LastDay, Record) = (id, by, purpose, lastDay, record);
    }

    /// <summary>Gets the identifier. It says nothing about anybody: the address of an invitation carries no private information.</summary>
    public string Id { get; }

    /// <summary>Gets the account that sent it.</summary>
    public AccountId By { get; }

    /// <summary>Gets what it is for.</summary>
    public InvitationPurpose Purpose { get; }

    /// <summary>Gets the last day it can be used.</summary>
    public DateOnly LastDay { get; }

    /// <summary>Gets the record it is about, for an invitation to claim one.</summary>
    public PersonId? Record { get; }

    /// <summary>Gets where it stands.</summary>
    public InvitationState State { get; private set; }

    /// <summary>Gets the account that used it.</summary>
    public AccountId? UsedBy { get; private set; }

    /// <summary>Uses the invitation for an account. Used a second time by the same account nothing changes; by another it is refused.</summary>
    public ActOutcome Use(AccountId account, DateOnly today)
    {
        if (State == InvitationState.Used)
        {
            return UsedBy == account ? ActOutcome.AlreadyDone : ActOutcome.Refused;
        }

        if (State == InvitationState.Revoked || today > LastDay)
        {
            return ActOutcome.Refused;
        }

        (State, UsedBy) = (InvitationState.Used, account);
        return ActOutcome.Done;
    }

    /// <summary>Takes the invitation back. Only who sent it can, and only while it was not used.</summary>
    public ActOutcome Revoke(AccountId by) => (by == By, State) switch
    {
        (false, _) => ActOutcome.Refused,
        (true, InvitationState.Open) => Take(),
        (true, InvitationState.Revoked) => ActOutcome.AlreadyDone,
        _ => ActOutcome.Refused,
    };

    private ActOutcome Take()
    {
        State = InvitationState.Revoked;
        return ActOutcome.Done;
    }
}

/// <summary>Where the claim of an account on the record of a person stands (PRIV-004).</summary>
public enum ClaimState
{
    /// <summary>Asked for; somebody else has to look at it.</summary>
    Requested = 0,

    /// <summary>Confirmed by a reviewer.</summary>
    Confirmed = 1,

    /// <summary>Refused by a reviewer.</summary>
    Refused = 2,
}

/// <summary>
/// The claim that an account and the record of a person are the same person (PRIV-004). The same mail address,
/// name or photograph is a clue and proves nothing: a claim is confirmed by somebody who is not the claimant,
/// and confirming it merges no records and widens no audience.
/// </summary>
public sealed class IdentityClaim(PersonId record, AccountId account)
{
    /// <summary>Gets the record that is claimed.</summary>
    public PersonId Record { get; } = record;

    /// <summary>Gets the account that claims it.</summary>
    public AccountId Account { get; } = account;

    /// <summary>Gets where the claim stands.</summary>
    public ClaimState State { get; private set; }

    /// <summary>Gets who reviewed the claim.</summary>
    public AccountId? ReviewedBy { get; private set; }

    /// <summary>Records what a reviewer found. Nobody reviews their own claim, and a claim is reviewed once.</summary>
    public ActOutcome Review(AccountId reviewer, bool confirmed)
    {
        if (reviewer == Account || State != ClaimState.Requested)
        {
            return ActOutcome.Refused;
        }

        (State, ReviewedBy) = (confirmed ? ClaimState.Confirmed : ClaimState.Refused, reviewer);
        return ActOutcome.Done;
    }
}

/// <summary>The record of a person in a private space.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name, as its creator wrote it.</param>
/// <param name="Mail">A mail address, when one is known. It is a clue for a match and never a proof.</param>
/// <param name="Scope">Who may see that the record exists and what it says.</param>
public sealed record PersonRecord(PersonId Id, string Name, string? Mail, AccessScope Scope);

/// <summary>Two records that might describe the same person: a suggestion for a reviewer, never a merge (PRIV-004).</summary>
/// <param name="One">One record.</param>
/// <param name="Other">The other record.</param>
/// <param name="Clue">What the two have in common.</param>
public sealed record MatchSuggestion(PersonId One, PersonId Other, string Clue);

/// <summary>Finds records that might describe the same person.</summary>
public static class Matching
{
    /// <summary>
    /// Returns the pairs of records with the same mail address, for a reviewer to look at. Nothing is merged and
    /// no scope is changed: the records are returned as they are.
    /// </summary>
    public static ImmutableArray<MatchSuggestion> Suggest(IEnumerable<PersonRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        return
        [
            .. records.Where(record => !string.IsNullOrWhiteSpace(record.Mail))
                .GroupBy(record => record.Mail!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .SelectMany(group => group.SelectMany((one, index) => group.Skip(index + 1).Select(other => new MatchSuggestion(one.Id, other.Id, "the same mail address")))),
        ];
    }
}
