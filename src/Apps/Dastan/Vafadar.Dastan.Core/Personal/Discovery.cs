using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Personal;

/// <summary>
/// That a person was at a university, a company, an organisation for a time, and what of it may be found
/// (PRIV-010). The name, the affiliation and its dates are allowed separately: a name that may be seen is no
/// consent to be found through the place.
/// </summary>
/// <param name="Person">The record of the person.</param>
/// <param name="Account">The account of the person, who alone gives these permissions.</param>
/// <param name="Organisation">The entity of the university or organisation.</param>
/// <param name="Attended">The time of the attendance, as it is stated.</param>
/// <param name="Graduated">The time of a graduation, when one is recorded. It is a statement of its own, not the end of the attendance.</param>
/// <param name="FindableHere">Who may find the person through this organisation.</param>
/// <param name="DatesShown">Who may see when – and so find the person by a year.</param>
public sealed record Affiliation(
    PersonId Person,
    AccountId Account,
    EntityId Organisation,
    TemporalExpression Attended,
    TemporalExpression? Graduated,
    AccessScope FindableHere,
    AccessScope DatesShown);

/// <summary>What a year is asked of an attendance (PRIV-011). Each question has its own meaning.</summary>
public enum AttendanceQuestion
{
    /// <summary>The attendance reaches the year.</summary>
    AttendedDuring = 0,

    /// <summary>The attendance began in the year.</summary>
    EnteredIn = 1,

    /// <summary>A graduation that is recorded happened in the year.</summary>
    GraduatedIn = 2,
}

/// <summary>Somebody found through an organisation and a year.</summary>
/// <param name="Person">The record of the person.</param>
/// <param name="Holding">Whether the records say so, or only leave it open (an attendance known between two times).</param>
public sealed record FoundPerson(PersonId Person, Holding Holding);

/// <summary>The answer to a search through an organisation: who was found, and how many – both for this viewer only.</summary>
/// <param name="People">The people the viewer may find.</param>
public sealed record AffiliationResult(ImmutableArray<FoundPerson> People)
{
    /// <summary>Gets the number of people found. It counts what the viewer may find and nothing else (PRIV-012).</summary>
    public int Count => People.Length;
}

/// <summary>Finding people through a university or an organisation and a time.</summary>
public static class AffiliationSearch
{
    /// <summary>
    /// Returns who a viewer may find at an organisation in a year. Authorization comes first: an affiliation the
    /// viewer may not find is not asked the question at all, so neither the list nor its count – nor the absence
    /// of a result – says anything about it. People who are found together are not thereby classmates,
    /// acquaintances or friends: the answer is a list of records, never a relationship.
    /// </summary>
    public static AffiliationResult Find(IEnumerable<Affiliation> affiliations, EntityId organisation, YearOfEra year, AttendanceQuestion question, AccountId? viewer)
    {
        ArgumentNullException.ThrowIfNull(affiliations);
        return new(
        [
            .. affiliations
                .Where(affiliation => affiliation.Organisation == organisation)
                .Where(affiliation => affiliation.FindableHere.Allows(viewer) && affiliation.DatesShown.Allows(viewer))
                .Select(affiliation => new FoundPerson(affiliation.Person, Answer(affiliation, year, question)))
                .Where(found => found.Holding != Holding.No),
        ]);
    }

    /// <summary>
    /// Returns who a viewer may find at an organisation at any time: who allows being found there, whether or
    /// not the dates may be seen.
    /// </summary>
    public static AffiliationResult Find(IEnumerable<Affiliation> affiliations, EntityId organisation, AccountId? viewer)
    {
        ArgumentNullException.ThrowIfNull(affiliations);
        return new(
        [
            .. affiliations
                .Where(affiliation => affiliation.Organisation == organisation && affiliation.FindableHere.Allows(viewer))
                .Select(affiliation => new FoundPerson(affiliation.Person, Holding.Yes)),
        ]);
    }

    private static Holding Answer(Affiliation affiliation, YearOfEra year, AttendanceQuestion question) => question switch
    {
        AttendanceQuestion.AttendedDuring => YearCover.Of(affiliation.Attended, year),
        AttendanceQuestion.GraduatedIn => YearCover.Of(affiliation.Graduated, year),

        // The year an attendance began: the year of the beginning, when one is stated. A beginning known only
        // between two years leaves each of them open; an unknown one answers no year.
        _ => affiliation.Attended switch
        {
            Period { Start: { Earliest: { } earliest, Latest: { } latest } } => YearCover.Of(
                ReferenceEquals(earliest, latest) || earliest == latest ? new Occurrence(earliest) : new OccurrenceWindow(earliest, latest), year),
            Occurrence or OccurrenceWindow => YearCover.Of(affiliation.Attended, year),
            _ => Holding.No,
        },
    };
}

/// <summary>A part of an image or a video that can be asked for by itself.</summary>
public enum MediaPart
{
    /// <summary>The file.</summary>
    File = 0,

    /// <summary>A small picture of it.</summary>
    Thumbnail = 1,

    /// <summary>The still picture of a video.</summary>
    Poster = 2,

    /// <summary>The captions of a video.</summary>
    Captions = 3,

    /// <summary>The transcript of a video.</summary>
    Transcript = 4,

    /// <summary>A stretch of a video.</summary>
    Segment = 5,
}

/// <summary>Where a tag that connects an image or a video with a person stands.</summary>
public enum TagState
{
    /// <summary>Proposed by somebody; the person has not answered.</summary>
    Proposed = 0,

    /// <summary>Approved by the person.</summary>
    Approved = 1,

    /// <summary>Taken away by the person.</summary>
    Removed = 2,
}

/// <summary>
/// An image or a video somebody shares in a private space: who it belongs to, who may see it, when it was taken
/// and when it was shared. The two times are different facts: a photograph of long ago that was shared today is
/// new in a list of recent shares and old in its caption.
/// </summary>
public sealed class SharedMedia
{
    private readonly Dictionary<PersonId, (AccountId Account, TagState State)> _tags = [];

    /// <summary>Initializes a new instance of the <see cref="SharedMedia"/> class.</summary>
    public SharedMedia(string id, AccessScope scope, DateOnly taken, DateOnly shared)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(scope);
        (Id, Scope, Taken, Shared) = (id, scope, taken, shared);
    }

    /// <summary>Gets the identifier.</summary>
    public string Id { get; }

    /// <summary>Gets who may see the item. Every way to it – an album, a page of a person, a gallery of a time, a list of recent shares, an address of a moment – asks this scope.</summary>
    public AccessScope Scope { get; private set; }

    /// <summary>Gets the day the picture was taken.</summary>
    public DateOnly Taken { get; }

    /// <summary>Gets the day it was shared.</summary>
    public DateOnly Shared { get; }

    /// <summary>Gets a value indicating whether its owner withdrew the item.</summary>
    public bool IsWithdrawn { get; private set; }

    /// <summary>Proposes that the item shows a person. The person, with their account, answers.</summary>
    public void Tag(PersonId person, AccountId account) => _tags.TryAdd(person, (account, TagState.Proposed));

    /// <summary>Approves a tag. Only the tagged person can. The approval connects the item with the person; it lets nobody see the item who could not before (PRIV-013).</summary>
    public ActOutcome Approve(PersonId person, AccountId by) => Answer(person, by, TagState.Approved);

    /// <summary>Takes a tag away. Only the tagged person can.</summary>
    public ActOutcome RemoveTag(PersonId person, AccountId by) => Answer(person, by, TagState.Removed);

    /// <summary>Returns where the tag of a person stands, or <see langword="null"/> when there is none.</summary>
    public TagState? TagOf(PersonId person) => _tags.TryGetValue(person, out var tag) ? tag.State : null;

    /// <summary>Changes who may see the item. Only its owner can; the change holds for every way to the item from then on.</summary>
    public ActOutcome Share(AccountId by, AccessScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (by != Scope.Owner || scope.Owner != by)
        {
            return ActOutcome.Refused;
        }

        Scope = scope;
        return ActOutcome.Done;
    }

    /// <summary>Withdraws the item. Only its owner can; nothing of it is delivered afterwards.</summary>
    public ActOutcome Withdraw(AccountId by)
    {
        if (by != Scope.Owner)
        {
            return ActOutcome.Refused;
        }

        IsWithdrawn = true;
        return ActOutcome.Done;
    }

    /// <summary>
    /// Returns whether a viewer may be given a part of the item. Every part has the same answer: a poster, a
    /// caption or a transcript tells what the file shows, so none is given to who may not see the file.
    /// </summary>
    public bool MayOpen(AccountId? viewer, MediaPart part = MediaPart.File) => Enum.IsDefined(part) && !IsWithdrawn && Scope.Allows(viewer);

    /// <summary>
    /// Returns whether the item stands in the life of a person for a viewer: the person approved the tag, the
    /// viewer may see the item, and the viewer may see the person. Being a friend of the tagged person is not
    /// enough – the audience is the one the owner of the item chose.
    /// </summary>
    public bool IsInLifeOf(PersonRecord person, AccountId? viewer)
    {
        ArgumentNullException.ThrowIfNull(person);
        return TagOf(person.Id) == TagState.Approved && MayOpen(viewer) && person.Scope.Allows(viewer);
    }

    private ActOutcome Answer(PersonId person, AccountId by, TagState state)
    {
        if (!_tags.TryGetValue(person, out var tag) || tag.Account != by)
        {
            return ActOutcome.Refused;
        }

        if (tag.State == state)
        {
            return ActOutcome.AlreadyDone;
        }

        _tags[person] = (tag.Account, state);
        return ActOutcome.Done;
    }
}

/// <summary>Something that would be made public with a story, and whether it may be.</summary>
/// <param name="What">What it is, in the words of the check.</param>
/// <param name="MayBePublished">Whether it is public already.</param>
public sealed record PublicationFinding(string What, bool MayBePublished);

/// <summary>
/// The check before something private is made public (PRIV-014): making public is an act of its own, and what
/// depends on the story – the people in it, their relationships, its images – is looked at again. The check
/// changes nothing and publishes nothing; it says what stands in the way.
/// </summary>
public static class PublicationCheck
{
    /// <summary>Returns what a story would make public, each with whether it may be: a relationship both sides show to everybody, an item that is public, a person whose record is.</summary>
    public static ImmutableArray<PublicationFinding> Of(IEnumerable<PersonRecord> people, IEnumerable<SocialRelationship> relationships, IEnumerable<SharedMedia> media)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(media);
        return
        [
            .. people.Select(person => new PublicationFinding($"person {person.Id}", person.Scope.Allows(null))),
            .. relationships.Select(relationship => new PublicationFinding($"relationship {relationship.Id}", relationship.IsVisibleTo(null))),
            .. media.Select(item => new PublicationFinding($"media {item.Id}", item.MayOpen(null))),
        ];
    }

    /// <summary>Returns what stands in the way of making a story public.</summary>
    public static ImmutableArray<PublicationFinding> Blockers(IEnumerable<PersonRecord> people, IEnumerable<SocialRelationship> relationships, IEnumerable<SharedMedia> media) =>
        [.. Of(people, relationships, media).Where(finding => !finding.MayBePublished)];
}

/// <summary>What a viewer sees of a set of shared items.</summary>
public static class SharedGallery
{
    /// <summary>
    /// Returns the items shared since a day that a viewer may see, the one shared last first. An old photograph
    /// that was shared lately is among them; a friendship that was accepted lately adds nothing – only the scope
    /// its owner chose for an item counts.
    /// </summary>
    public static ImmutableArray<SharedMedia> RecentShares(IEnumerable<SharedMedia> media, DateOnly since, AccountId? viewer)
    {
        ArgumentNullException.ThrowIfNull(media);
        return [.. media.Where(item => item.Shared >= since && item.MayOpen(viewer)).OrderByDescending(item => item.Shared)];
    }
}
