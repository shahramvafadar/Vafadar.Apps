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

    /// <summary>A recurring or bounded occasion: a competition, a season, an event of history, a period.</summary>
    Occasion = 5,

    /// <summary>An office, a title or an honour: what somebody holds or receives.</summary>
    Role = 6,

    /// <summary>A subject of knowledge: a species, an idea, a field.</summary>
    Subject = 7,
}

/// <summary>A type of entity from the managed list <see cref="EntityTypes"/>.</summary>
/// <param name="Key">The stable name of the type.</param>
/// <param name="Nature">What kind of thing entities of the type are.</param>
/// <param name="Meaning">What an entity of the type is, in a sentence for who writes content.</param>
public sealed record EntityType(string Key, EntityNature Nature, string Meaning);

/// <summary>
/// The entity types Dastan knows. A new type is a new entry here; who may add entries is an open product question
/// (Q-15), so for now the list changes with the code.
/// </summary>
public static class EntityTypes
{
    /// <summary>A work, such as a poem.</summary>
    public static EntityType Work { get; } = new("work", EntityNature.Work, "A work, such as a poem");

    /// <summary>A story inside a work, selected as one collection.</summary>
    public static EntityType StoryArc { get; } = new("storyArc", EntityNature.Work, "A story inside a work, selected as one collection");

    /// <summary>A character of a story.</summary>
    public static EntityType Character { get; } = new("character", EntityNature.Being, "A character of a story");

    /// <summary>An animal that is a character of a story.</summary>
    public static EntityType AnimalCharacter { get; } = new("animalCharacter", EntityNature.Being, "An animal that is a character of a story");

    /// <summary>A place of a story. It is not a place on a modern map.</summary>
    public static EntityType NarrativePlace { get; } = new("narrativePlace", EntityNature.Place, "A place of a story. It is not a place on a modern map");

    /// <summary>A region of a story. It has no modern border.</summary>
    public static EntityType NarrativeRegion { get; } = new("narrativeRegion", EntityNature.Place, "A region of a story. It has no modern border");

    /// <summary>An object of a story.</summary>
    public static EntityType NarrativeObject { get; } = new("narrativeObject", EntityNature.Object, "An object of a story");

    /// <summary>A real person, living or historical: a poet, a ruler, a scholar. Not a character of a story.</summary>
    public static EntityType Person { get; } = new("person", EntityNature.Being, "A real person, living or historical: a poet, a ruler, a scholar. Not a character of a story");

    /// <summary>People who act under one name: a court, a dynasty, a school, an institution.</summary>
    public static EntityType Organisation { get; } = new("organisation", EntityNature.Group, "People who act under one name: a court, a dynasty, a school, an institution");

    /// <summary>A real place: a city, a village, a building. It is, or was, somewhere on a map.</summary>
    public static EntityType Place { get; } = new("place", EntityNature.Place, "A real place: a city, a village, a building. It is, or was, somewhere on a map");

    /// <summary>A real region: a country, a province, a historical land.</summary>
    public static EntityType Region { get; } = new("region", EntityNature.Place, "A real region: a country, a province, a historical land");

    /// <summary>A company: a legal body that does business; not its brands.</summary>
    public static EntityType Company { get; } = new("company", EntityNature.Group, "A company: a legal body that does business; not its brands");

    /// <summary>A sports club.</summary>
    public static EntityType SportsClub { get; } = new("sportsClub", EntityNature.Group, "A sports club");

    /// <summary>One team of a club: the first team, a youth team, the women's team.</summary>
    public static EntityType SportsTeam { get; } = new("sportsTeam", EntityNature.Group, "One team of a club: the first team, a youth team, the women's team");

    /// <summary>A competition: a league, a cup, a tournament.</summary>
    public static EntityType Competition { get; } = new("competition", EntityNature.Occasion, "A competition: a league, a cup, a tournament");

    /// <summary>One season or edition of a competition.</summary>
    public static EntityType CompetitionSeason { get; } = new("competitionSeason", EntityNature.Occasion, "One season or edition of a competition");

    /// <summary>A brand: a name under which things are sold; not the company that owns it.</summary>
    public static EntityType Brand { get; } = new("brand", EntityNature.Object, "A brand: a name under which things are sold; not the company that owns it");

    /// <summary>A product or a line of products.</summary>
    public static EntityType Product { get; } = new("product", EntityNature.Object, "A product or a line of products");

    /// <summary>A university or another institution of learning; not the place of its campus.</summary>
    public static EntityType University { get; } = new("university", EntityNature.Group, "A university or another institution of learning; not the place of its campus");

    /// <summary>A government or a state as an institution; not the land it rules.</summary>
    public static EntityType Government { get; } = new("government", EntityNature.Group, "A government or a state as an institution; not the land it rules");

    /// <summary>A dynasty: a ruling house over generations.</summary>
    public static EntityType Dynasty { get; } = new("dynasty", EntityNature.Group, "A dynasty: a ruling house over generations");

    /// <summary>A family or a house, as a group with a name.</summary>
    public static EntityType Family { get; } = new("family", EntityNature.Group, "A family or a house, as a group with a name");

    /// <summary>A people or a community; not the land it lives in.</summary>
    public static EntityType Community { get; } = new("community", EntityNature.Group, "A people or a community; not the land it lives in");

    /// <summary>A country as a land with a name; its borders depend on the time.</summary>
    public static EntityType Country { get; } = new("country", EntityNature.Place, "A country as a land with a name; its borders depend on the time");

    /// <summary>Something that happened in the world: a battle, a founding, a match – not a step of the story.</summary>
    public static EntityType Event { get; } = new("event", EntityNature.Occasion, "Something that happened in the world: a battle, a founding, a match – not a step of the story");

    /// <summary>A period of history: an era, a reign as a span of time.</summary>
    public static EntityType Period { get; } = new("period", EntityNature.Occasion, "A period of history: an era, a reign as a span of time");

    /// <summary>An office or a title somebody holds: king, vizier, president.</summary>
    public static EntityType Office { get; } = new("office", EntityNature.Role, "An office or a title somebody holds: king, vizier, president");

    /// <summary>An honour or a prize that is given.</summary>
    public static EntityType Honour { get; } = new("honour", EntityNature.Role, "An honour or a prize that is given");

    /// <summary>A real thing: a manuscript, a sword, a coin.</summary>
    public static EntityType RealObject { get; } = new("object", EntityNature.Object, "A real thing: a manuscript, a sword, a coin");

    /// <summary>A subject of nature: a species, a river system, an element.</summary>
    public static EntityType NaturalSubject { get; } = new("naturalSubject", EntityNature.Subject, "A subject of nature: a species, a river system, an element");

    /// <summary>An idea, a field of knowledge, a technique.</summary>
    public static EntityType Concept { get; } = new("concept", EntityNature.Subject, "An idea, a field of knowledge, a technique");

    /// <summary>
    /// A theme the images and videos of a collection are gathered under: a motif, a genre, a school of painting.
    /// It is the editors' grouping – a media item names its themes – and is not something an item depicts.
    /// </summary>
    public static EntityType Theme { get; } = new("theme", EntityNature.Subject, "A theme the images and videos of the collection are gathered under: a motif, a genre, a school of painting. A media item names it in themeIds; nothing depicts it");

    /// <summary>Gets every known type.</summary>
    public static ImmutableArray<EntityType> All { get; } =
    [
        Work, StoryArc, Character, AnimalCharacter, NarrativePlace, NarrativeRegion, NarrativeObject,
        Person, Organisation, Place, Region, Company, SportsClub, SportsTeam, Competition, CompetitionSeason,
        Brand, Product, University, Government, Dynasty, Family, Community, Country, Event, Period, Office, Honour, RealObject, NaturalSubject, Concept, Theme,
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
/// <param name="Meaning">What a relation of the type says, where the name alone could be read in more than one way.</param>
/// <param name="From">What kinds of thing the first entity may be; empty when the type does not say.</param>
/// <param name="To">What kinds of thing the second entity may be; empty when the type does not say.</param>
public sealed record RelationType(
    string Key,
    Directionality Directionality,
    Kinship Kinship = Kinship.None,
    string? Meaning = null,
    ImmutableArray<EntityNature> From = default,
    ImmutableArray<EntityNature> To = default)
{
    /// <summary>Returns whether an entity of that nature may stand at that end of a relation of the type.</summary>
    public bool Fits(EntityNature nature, bool first)
    {
        var allowed = first ? From : To;
        return allowed.IsDefaultOrEmpty || allowed.Contains(nature);
    }
}

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

        // Relations of the world outside a story (step 10.6). Each says what it means and between what it holds;
        // a relation between the wrong kinds of thing is refused.
        new("siblingOf", Directionality.Symmetric, Kinship.None,
            "brothers or sisters; a family tree is drawn from parents, not from this", Beings, Beings),
        new("teacherOf", Directionality.Directed, Kinship.None,
            "the first taught the second", Beings, Beings),
        new("authorOf", Directionality.Directed, Kinship.None,
            "the first wrote or composed the work", [EntityNature.Being, EntityNature.Group], [EntityNature.Work]),
        new("bornIn", Directionality.Directed, Kinship.None,
            "the place or region of birth", Beings, Places),
        new("diedIn", Directionality.Directed, Kinship.None,
            "the place or region of death", Beings, Places),
        new("memberOf", Directionality.Directed, Kinship.None,
            "the first belonged to the group", Beings, Groups),
        new("founderOf", Directionality.Directed, Kinship.None,
            "the first founded the group or the place", Beings, [EntityNature.Group, EntityNature.Place]),
        new("locatedIn", Directionality.Directed, Kinship.None,
            "the first lies in, or has its seat in, the second", [EntityNature.Place, EntityNature.Group], Places),

        // The rest of what the product documents name (step 12.1, D-047): persons and offices, works and their
        // versions, identity that is not established, companies and brands, governments, sport, events, subjects.
        new("adoptiveParentOf", Directionality.Directed, Kinship.None,
            "the first adopted or raised the second; a family tree is drawn from parents by birth", [EntityNature.Being], [EntityNature.Being]),
        new("patronOf", Directionality.Directed, Kinship.None,
            "the first supported the second with money, protection or a commission", [EntityNature.Being, EntityNature.Group], [EntityNature.Being, EntityNature.Group, EntityNature.Work]),
        new("successorOf", Directionality.Directed, Kinship.None,
            "the first followed the second, in an office or as an organisation", [EntityNature.Being, EntityNature.Group], [EntityNature.Being, EntityNature.Group]),
        new("holderOf", Directionality.Directed, Kinship.None,
            "the first held the office, bore the title or received the honour", [EntityNature.Being, EntityNature.Group], [EntityNature.Role]),
        new("employedBy", Directionality.Directed, Kinship.None,
            "the first worked for the group", [EntityNature.Being], [EntityNature.Group]),
        new("leaderOf", Directionality.Directed, Kinship.None,
            "the first led, managed or governed the second", [EntityNature.Being], [EntityNature.Group, EntityNature.Place]),
        new("ownerOf", Directionality.Directed, Kinship.None,
            "the first owned the second, wholly or in part", [EntityNature.Being, EntityNature.Group], [EntityNature.Group, EntityNature.Place, EntityNature.Object, EntityNature.Work]),
        new("studiedAt", Directionality.Directed, Kinship.None,
            "the first studied at the institution or in the place", [EntityNature.Being], [EntityNature.Group, EntityNature.Place]),
        new("alliedWith", Directionality.Symmetric, Kinship.None,
            "allies", [EntityNature.Being, EntityNature.Group], [EntityNature.Being, EntityNature.Group]),
        new("supporterOf", Directionality.Directed, Kinship.None,
            "the first is known as a supporter of the group", [EntityNature.Being], [EntityNature.Group]),
        new("livedIn", Directionality.Directed, Kinship.None,
            "the first lived or worked in the place", [EntityNature.Being, EntityNature.Group], [EntityNature.Place]),
        new("translatorOf", Directionality.Directed, Kinship.None,
            "the first translated the work", [EntityNature.Being, EntityNature.Group], [EntityNature.Work]),
        new("illustratorOf", Directionality.Directed, Kinship.None,
            "the first illustrated or painted the work", [EntityNature.Being, EntityNature.Group], [EntityNature.Work]),
        new("publisherOf", Directionality.Directed, Kinship.None,
            "the first published the work", [EntityNature.Being, EntityNature.Group], [EntityNature.Work]),
        new("commissionerOf", Directionality.Directed, Kinship.None,
            "the first ordered the work to be made", [EntityNature.Being, EntityNature.Group], [EntityNature.Work]),
        new("dedicatedTo", Directionality.Directed, Kinship.None,
            "the work is dedicated to the second", [EntityNature.Work], [EntityNature.Being, EntityNature.Group]),
        new("adaptationOf", Directionality.Directed, Kinship.None,
            "the first work retells or adapts the second", [EntityNature.Work], [EntityNature.Work]),
        new("translationOf", Directionality.Directed, Kinship.None,
            "the first work is a translation of the second", [EntityNature.Work], [EntityNature.Work]),
        new("editionOf", Directionality.Directed, Kinship.None,
            "the first is an edition, a manuscript or a copy of the second work", [EntityNature.Work, EntityNature.Object], [EntityNature.Work]),
        new("about", Directionality.Directed, Kinship.None,
            "the work is about the second", [EntityNature.Work], default),
        new("keptAt", Directionality.Directed, Kinship.None,
            "the first is kept in the place or by the group", [EntityNature.Work, EntityNature.Object], [EntityNature.Place, EntityNature.Group]),
        new("portrayalOf", Directionality.Directed, Kinship.None,
            "the first portrays the second – a character drawn from a person, a role in an adaptation; they stay two entities", [EntityNature.Being], [EntityNature.Being]),
        new("possiblySameAs", Directionality.Symmetric, Kinship.None,
            "possibly the same; it is not established, and the two stay two entities", default, default),
        new("confusedWith", Directionality.Symmetric, Kinship.None,
            "commonly taken for one another; they are not the same", default, default),
        new("acquired", Directionality.Directed, Kinship.None,
            "the first took over ownership or control of the second; the second need not cease to exist", [EntityNature.Being, EntityNature.Group], [EntityNature.Group, EntityNature.Object]),
        new("mergedInto", Directionality.Directed, Kinship.None,
            "the first merged into the second, which followed it", [EntityNature.Group], [EntityNature.Group]),
        new("spunOffFrom", Directionality.Directed, Kinship.None,
            "the first was split off from the second", [EntityNature.Group], [EntityNature.Group]),
        new("subsidiaryOf", Directionality.Directed, Kinship.None,
            "the first belongs to the second as a part with a name of its own", [EntityNature.Group], [EntityNature.Group]),
        new("brandOf", Directionality.Directed, Kinship.None,
            "the first is a brand of the second", [EntityNature.Object], [EntityNature.Group]),
        new("productOf", Directionality.Directed, Kinship.None,
            "the first is made or sold by the second", [EntityNature.Object], [EntityNature.Being, EntityNature.Group]),
        new("governs", Directionality.Directed, Kinship.None,
            "the government or dynasty held power over the land or the people", [EntityNature.Group], [EntityNature.Place, EntityNature.Group]),
        new("capitalOf", Directionality.Directed, Kinship.None,
            "the first is the seat of government of the second", [EntityNature.Place], [EntityNature.Place, EntityNature.Group]),
        new("teamOf", Directionality.Directed, Kinship.None,
            "the first is a team of the club", [EntityNature.Group], [EntityNature.Group]),
        new("playsFor", Directionality.Directed, Kinship.None,
            "the first played for the team or club", [EntityNature.Being], [EntityNature.Group]),
        new("coachOf", Directionality.Directed, Kinship.None,
            "the first coached the team or club", [EntityNature.Being], [EntityNature.Group]),
        new("captainOf", Directionality.Directed, Kinship.None,
            "the first was captain of the team", [EntityNature.Being], [EntityNature.Group]),
        new("loanedTo", Directionality.Directed, Kinship.None,
            "the first played for the group on loan", [EntityNature.Being], [EntityNature.Group]),
        new("transferredTo", Directionality.Directed, Kinship.None,
            "the first moved to the group", [EntityNature.Being], [EntityNature.Group]),
        new("competesIn", Directionality.Directed, Kinship.None,
            "the first took part in the competition or season", [EntityNature.Being, EntityNature.Group], [EntityNature.Occasion]),
        new("winnerOf", Directionality.Directed, Kinship.None,
            "the first won the competition, the season or the event", [EntityNature.Being, EntityNature.Group], [EntityNature.Occasion]),
        new("organiserOf", Directionality.Directed, Kinship.None,
            "the first organised the competition or the event", [EntityNature.Being, EntityNature.Group], [EntityNature.Occasion]),
        new("seasonOf", Directionality.Directed, Kinship.None,
            "the first is a season or an edition of the second", [EntityNature.Occasion], [EntityNature.Occasion]),
        new("participantIn", Directionality.Directed, Kinship.None,
            "the first took part in the event", [EntityNature.Being, EntityNature.Group], [EntityNature.Occasion]),
        new("tookPlaceIn", Directionality.Directed, Kinship.None,
            "the event took place there", [EntityNature.Occasion], [EntityNature.Place]),
        new("during", Directionality.Directed, Kinship.None,
            "the first happened in, or is part of, the period", [EntityNature.Occasion], [EntityNature.Occasion]),
        new("worksIn", Directionality.Directed, Kinship.None,
            "the first worked in the field or on the subject", [EntityNature.Being, EntityNature.Group], [EntityNature.Subject]),
        new("foundIn", Directionality.Directed, Kinship.None,
            "the subject is found in the place", [EntityNature.Subject], [EntityNature.Place]),
    ];

    private static ImmutableArray<EntityNature> Beings => [EntityNature.Being];

    private static ImmutableArray<EntityNature> Groups => [EntityNature.Group];

    private static ImmutableArray<EntityNature> Places => [EntityNature.Place];

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

    /// <summary>A statement about the world outside the story: who wrote the work, where a poet was born.</summary>
    OutsideTheStory = 6,
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

/// <summary>What follows from a <see cref="Presence"/> wherever one is shown.</summary>
public static class PresenceRule
{
    /// <summary>
    /// Returns whether the entity is there in the scenes of the event – acting, involved, the setting or an
    /// object – and not only spoken of: mentioned, reported about, or mourned. Every drawing draws the two
    /// differently, and none decides it for itself.
    /// </summary>
    /// <param name="presence">What the association asserts.</param>
    public static bool IsThere(this Presence presence) =>
        presence is Presence.Acts or Presence.Involved or Presence.Setting or Presence.Object;
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
