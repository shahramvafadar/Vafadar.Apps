using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>
/// What kind of thing an entity is, as far as views need to know: a lane of a story is for beings, a setting is a
/// place. Nothing in the model assumes that an entity is a person.
/// </summary>
public enum EntityNature
{
    /// <summary>Someone who can act: a person, a character, an animal character.</summary>
    Being = 0,

    /// <summary>Several beings as one: a company, a club, a team.</summary>
    Group = 1,

    /// <summary>A place or a region.</summary>
    Place = 2,

    /// <summary>A work or a part of one: a poem, a story inside it.</summary>
    Work = 3,

    /// <summary>A thing.</summary>
    Object = 4,

    /// <summary>A recurring or bounded occasion: a competition, a season.</summary>
    Occasion = 5,
}

/// <summary>A type of entity from the managed list <see cref="EntityTypes"/>.</summary>
/// <param name="Key">The stable name of the type.</param>
/// <param name="Nature">What kind of thing entities of the type are.</param>
public sealed record EntityType(string Key, EntityNature Nature);

/// <summary>
/// The entity types Dastan knows. A new type is a new entry here; who may add entries is an open product question
/// (Q-15), so for now the list changes with the code.
/// </summary>
public static class EntityTypes
{
    /// <summary>A work, such as a poem.</summary>
    public static EntityType Work { get; } = new("work", EntityNature.Work);

    /// <summary>A story inside a work, selected as one collection.</summary>
    public static EntityType StoryArc { get; } = new("storyArc", EntityNature.Work);

    /// <summary>A character of a story.</summary>
    public static EntityType Character { get; } = new("character", EntityNature.Being);

    /// <summary>An animal that is a character of a story.</summary>
    public static EntityType AnimalCharacter { get; } = new("animalCharacter", EntityNature.Being);

    /// <summary>A place of a story. It is not a place on a modern map.</summary>
    public static EntityType NarrativePlace { get; } = new("narrativePlace", EntityNature.Place);

    /// <summary>A region of a story. It has no modern border.</summary>
    public static EntityType NarrativeRegion { get; } = new("narrativeRegion", EntityNature.Place);

    /// <summary>An object of a story.</summary>
    public static EntityType NarrativeObject { get; } = new("narrativeObject", EntityNature.Object);

    /// <summary>A real person.</summary>
    public static EntityType Person { get; } = new("person", EntityNature.Being);

    /// <summary>A company.</summary>
    public static EntityType Company { get; } = new("company", EntityNature.Group);

    /// <summary>A sports club.</summary>
    public static EntityType SportsClub { get; } = new("sportsClub", EntityNature.Group);

    /// <summary>A team of a club.</summary>
    public static EntityType SportsTeam { get; } = new("sportsTeam", EntityNature.Group);

    /// <summary>A competition.</summary>
    public static EntityType Competition { get; } = new("competition", EntityNature.Occasion);

    /// <summary>One season of a competition.</summary>
    public static EntityType CompetitionSeason { get; } = new("competitionSeason", EntityNature.Occasion);

    /// <summary>Gets every known type.</summary>
    public static ImmutableArray<EntityType> All { get; } =
    [
        Work, StoryArc, Character, AnimalCharacter, NarrativePlace, NarrativeRegion, NarrativeObject,
        Person, Company, SportsClub, SportsTeam, Competition, CompetitionSeason,
    ];

    /// <summary>Returns the type named <paramref name="key"/>, or <see langword="null"/> when the list has none.</summary>
    public static EntityType? Find(string key) => All.FirstOrDefault(type => string.Equals(type.Key, key, StringComparison.Ordinal));
}

/// <summary>Whether a relation reads from one entity to the other or the same in both directions.</summary>
public enum Directionality
{
    /// <summary>The relation has a direction: "parent of" is not "child of".</summary>
    Directed = 0,

    /// <summary>The relation is the same from both sides: "married to", "opponent of".</summary>
    Symmetric = 1,
}

/// <summary>What a relation type means for a family view.</summary>
public enum Kinship
{
    /// <summary>The type says nothing about family.</summary>
    None = 0,

    /// <summary>The first entity is a parent of the second.</summary>
    Parentage = 1,

    /// <summary>The two entities are partners (marriage and the like).</summary>
    Partnership = 2,
}

/// <summary>A type of relation from the managed list <see cref="RelationTypes"/>.</summary>
/// <param name="Key">The stable name of the type.</param>
/// <param name="Directionality">Whether relations of the type have a direction.</param>
/// <param name="Kinship">What the type means for a family view; a view never guesses that from the name.</param>
public sealed record RelationType(string Key, Directionality Directionality, Kinship Kinship = Kinship.None);

/// <summary>The relation types Dastan knows. A relation type has one meaning; similar wordings are not merged.</summary>
public static class RelationTypes
{
    /// <summary>Gets every known type.</summary>
    public static ImmutableArray<RelationType> All { get; } =
    [
        new("partOf", Directionality.Directed),
        new("parentOf", Directionality.Directed, Kinship.Parentage),
        new("spouseOf", Directionality.Symmetric, Kinship.Partnership),
        new("rules", Directionality.Directed),
        new("rides", Directionality.Directed),
        new("visits", Directionality.Directed),
        new("givesKeepsake", Directionality.Directed),
        new("entrustedTo", Directionality.Directed),
        new("carries", Directionality.Directed),
        new("supportsExpeditionOf", Directionality.Directed),
        new("opponentOf", Directionality.Symmetric),
        new("summonedBy", Directionality.Directed),
        new("returnsTo", Directionality.Directed),
        new("mourns", Directionality.Directed),
        new("huntsNear", Directionality.Directed),
        new("appearsAt", Directionality.Directed),
        new("withholdsRemedyFor", Directionality.Directed),
    ];

    /// <summary>Returns the type named <paramref name="key"/>, or <see langword="null"/> when the list has none.</summary>
    public static RelationType? Find(string key) => All.FirstOrDefault(type => string.Equals(type.Key, key, StringComparison.Ordinal));
}

/// <summary>What a relation is a statement about. It keeps "is the father of" apart from "is named as father in this scene".</summary>
public enum RelationScopeKind
{
    /// <summary>The structure of a work: a story is part of a poem.</summary>
    WorkStructure = 0,

    /// <summary>A lineage that the story presupposes.</summary>
    BackgroundLineage = 1,

    /// <summary>A relationship the reader learns in the events of the scope.</summary>
    RevealedInEvent = 2,

    /// <summary>A state the story shows without telling how it began: a king rules a place.</summary>
    ObservedContext = 3,

    /// <summary>An action inside the events of the scope. It is not a lasting relationship.</summary>
    ActionInEvent = 4,

    /// <summary>Something a character reports inside the events of the scope.</summary>
    ReportedInEvent = 5,
}

/// <summary>
/// What an association of an entity with an event asserts. An association is never "was there" unless it says so.
/// </summary>
public enum Presence
{
    /// <summary>The entity acts in the scenes of the event.</summary>
    Acts = 0,

    /// <summary>The entity is involved in the scenes without being a main actor.</summary>
    Involved = 1,

    /// <summary>The entity is where the scenes take place.</summary>
    Setting = 2,

    /// <summary>The entity is an object in the scenes.</summary>
    Object = 3,

    /// <summary>The entity is only spoken of; it is not present.</summary>
    MentionedOnly = 4,

    /// <summary>Someone reports about the entity; it is not present.</summary>
    SubjectOfReport = 5,

    /// <summary>The scenes concern the entity after its death.</summary>
    DeceasedSubject = 6,
}

/// <summary>How far the editors have come with a statement. It says nothing about whether the statement may be shown.</summary>
public enum ReviewStatus
{
    /// <summary>Suggested, not checked.</summary>
    Proposed = 0,

    /// <summary>Supported by a source; the editorial review is still open.</summary>
    SourcedReviewPending = 1,

    /// <summary>Reviewed by the editors.</summary>
    Reviewed = 2,

    /// <summary>Sources disagree, and the disagreement is kept.</summary>
    Disputed = 3,

    /// <summary>Withdrawn.</summary>
    Withdrawn = 4,
}

/// <summary>What kind of evidence a source is. A museum's retelling is not the poem.</summary>
public enum SourceKind
{
    /// <summary>The text of the work itself, in a stated transcription or edition.</summary>
    PrimaryText = 0,

    /// <summary>A scholarly reference work.</summary>
    ScholarlyReference = 1,

    /// <summary>The record of an object in a museum's catalogue.</summary>
    MuseumRecord = 2,

    /// <summary>The statement of an institution about the reuse of its material.</summary>
    RightsPolicy = 3,
}

/// <summary>Whether a media item is an image or a video.</summary>
public enum MediaKind
{
    /// <summary>A still image.</summary>
    Image = 0,

    /// <summary>A video.</summary>
    Video = 1,
}

/// <summary>What is established about the right to show a media item.</summary>
public enum RightsStatus
{
    /// <summary>Made for this project; no third-party material.</summary>
    OriginalProjectContent = 0,

    /// <summary>The catalogue marks the item as open access; delivery and review are separate steps.</summary>
    OpenAccessVerified = 1,

    /// <summary>Permission has not been established. The item must not be shown.</summary>
    PermissionNotVerified = 2,
}
