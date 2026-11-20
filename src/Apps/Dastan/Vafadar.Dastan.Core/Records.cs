using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>
/// How far into a story a visitor has to have read before a record may be shown to them. Level 0 is the opening;
/// what each higher level stands for is told by the collection (<see cref="ReadingStage"/>).
/// </summary>
public readonly record struct ReadingLevel : IComparable<ReadingLevel>
{
    /// <summary>Creates a level.</summary>
    public ReadingLevel(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    /// <summary>Gets the number of the level, 0 or greater.</summary>
    public int Value { get; }

    /// <summary>The opening of a story: what may be shown to someone who has read nothing yet.</summary>
    public static ReadingLevel Opening => default;

    /// <inheritdoc />
    public int CompareTo(ReadingLevel other) => Value.CompareTo(other.Value);

    /// <summary>Returns whether a visitor at <paramref name="boundary"/> may see a record of this level.</summary>
    public bool IsWithin(ReadingLevel boundary) => Value <= boundary.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>One stage of a story a visitor can choose as their reading boundary.</summary>
/// <param name="Level">The level.</param>
/// <param name="Label">What the stage is called, without giving away what happens in it.</param>
public sealed record ReadingStage(ReadingLevel Level, LocalizedText Label);

/// <summary>A subject of a collection: a character, a place, a work, an object – or a company, a team, a person.</summary>
public sealed class Entity
{
    /// <summary>Gets the identifier. It does not change when the entity is renamed.</summary>
    public required EntityId Id { get; init; }

    /// <summary>Gets the type.</summary>
    public required EntityType Type { get; init; }

    /// <summary>Gets the name.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets the short description shown first.</summary>
    public required LocalizedText Summary { get; init; }

    /// <summary>Gets the sources that support the description.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>Gets the reading level from which the entity may be shown at all.</summary>
    public required ReadingLevel Level { get; init; }

    /// <summary>
    /// Gets what is stated about the entity with a time of the calendar: a period it lasted, a day or a year
    /// something happened to it. A story has an order and no dates; this is for what really has one – when a
    /// work was written, when a manuscript was made.
    /// </summary>
    public ImmutableArray<DatedStatement> Dated { get; init; } = [];

    /// <summary>
    /// Gets the other names of the entity: names it is also known by, names it had before, abbreviations, titles,
    /// its name in the script it was first written in. The search finds the entity by each of them. A name
    /// that is alike does not make two entities one (DOM-003).
    /// </summary>
    public ImmutableArray<EntityName> Names { get; init; } = [];

    /// <summary>Gets what the entity is called in the catalogues of others, each with who issued it.</summary>
    public ImmutableArray<ExternalIdentifier> Identifiers { get; init; } = [];

    /// <summary>
    /// Gets the facts of the profile of the entity (step 19.5, ADR 0017): what its kind needs and what was
    /// added, each of a kind the table of <see cref="ProfileFacts"/> has for its type.
    /// </summary>
    public ImmutableArray<ProfileFact> Facts { get; init; } = [];

    /// <summary>
    /// Gets the media item that is the picture of the profile, or nothing. It is shown to exactly those who
    /// may see that item; everybody else sees a drawing of the kind.
    /// </summary>
    public MediaId? Picture { get; init; }

    /// <summary>
    /// Gets where on the earth a real place is, when the editors state it. A place of a story has none: it is
    /// not a place on a map.
    /// </summary>
    public GeoPosition? Position { get; init; }
}

/// <summary>How exactly a position says where a place is.</summary>
public enum PositionPrecision
{
    /// <summary>The place is there: a building, a monument, a small town.</summary>
    Exact = 0,

    /// <summary>The place is about there: its site is known roughly.</summary>
    Approximate = 1,

    /// <summary>The position is the middle of an area – a region, a country – and says nothing of its borders.</summary>
    CentreOfArea = 2,
}

/// <summary>Where a real place is: degrees north and east, and how exactly that is meant.</summary>
/// <param name="Latitude">Degrees north of the equator; negative to the south. From −90 to 90.</param>
/// <param name="Longitude">Degrees east of Greenwich; negative to the west. From −180 to 180.</param>
/// <param name="Precision">How exactly the position is meant.</param>
public sealed record GeoPosition(decimal Latitude, decimal Longitude, PositionPrecision Precision);

/// <summary>What kind of other name a name is.</summary>
public enum NameKind
{
    /// <summary>A name the entity is also known by.</summary>
    Alternative = 0,

    /// <summary>A name the entity had before; its time says when, where that is stated.</summary>
    Former = 1,

    /// <summary>A short form.</summary>
    Abbreviation = 2,

    /// <summary>A title or an epithet the entity is called by.</summary>
    Title = 3,

    /// <summary>The name in the script it was first written in.</summary>
    OriginalScript = 4,
}

/// <summary>Another name of an entity.</summary>
/// <param name="Name">The name, as it is written.</param>
/// <param name="Language">The language of the name, as a language tag: "en", "fa", "ar".</param>
/// <param name="Kind">What kind of name it is.</param>
/// <param name="Time">When the name was used, where that is stated; a time of the calendar.</param>
/// <param name="Sources">The sources that support the name, when the editors name some.</param>
/// <param name="Level">The reading level from which the name may be shown; never before its entity.</param>
public sealed record EntityName(string Name, string Language, NameKind Kind, TemporalExpression? Time, ImmutableArray<SourceId> Sources, ReadingLevel Level);

/// <summary>How sure the editors are that an identifier of another catalogue names this entity.</summary>
public enum IdentifierMatch
{
    /// <summary>The record of the catalogue is about this entity.</summary>
    Exact = 0,

    /// <summary>The record is about nearly the same: a wider or a narrower subject.</summary>
    Close = 1,

    /// <summary>It is not established that the record is about this entity.</summary>
    Uncertain = 2,
}

/// <summary>
/// What an entity is called in the catalogue of somebody else. Naming the identifier does not endorse what that
/// catalogue states (DOM-003).
/// </summary>
/// <param name="Issuer">Who issued the identifier: the name of the catalogue.</param>
/// <param name="Value">The identifier.</param>
/// <param name="Url">The address of the record in that catalogue, when there is one.</param>
/// <param name="Match">How sure the editors are that the record is about this entity.</param>
public sealed record ExternalIdentifier(string Issuer, string Value, Uri? Url, IdentifierMatch Match);

/// <summary>The units a quantity can be stated in. A unit has one meaning; a new one is a new entry here.</summary>
public static class QuantityUnits
{
    /// <summary>The unit of an amount of money, which names its currency beside it.</summary>
    public const string Money = "money";

    /// <summary>Gets every unit with what it measures.</summary>
    public static ImmutableArray<(string Key, string Meaning)> All { get; } =
    [
        ("count", "A bare number: how many, where the label says of what"),
        ("people", "Persons: a population, an army, an audience"),
        ("members", "Members of a group"),
        ("employees", "People a company employs"),
        ("verses", "Verses or couplets of a poem"),
        ("pages", "Pages or folios"),
        ("volumes", "Volumes of a work"),
        ("copies", "Copies printed or sold"),
        ("manuscripts", "Manuscripts that are known"),
        ("goals", "Goals scored"),
        ("matches", "Matches played"),
        ("points", "Points in a competition"),
        ("titles", "Titles won"),
        ("seats", "Seats of a hall or of a parliament"),
        ("years", "A length of time in years – not a date"),
        ("days", "A length of time in days"),
        ("metres", "A length or a height"),
        ("kilometres", "A distance"),
        ("square_kilometres", "An area"),
        ("kilograms", "A weight"),
        ("percent", "A share of a hundred"),
        ("money", "An amount of money; currency names it with three letters (ISO 4217)"),
    ];

    /// <summary>Returns whether the list has the unit.</summary>
    public static bool Has(string key) => All.Any(unit => string.Equals(unit.Key, key, StringComparison.Ordinal));
}

/// <summary>A quantity as it is stated: a number with its unit, and whether the source says "about".</summary>
/// <param name="Value">The number.</param>
/// <param name="Unit">The unit, from <see cref="QuantityUnits"/>.</param>
/// <param name="IsApproximate">Whether the source states the number as approximate. A page then says "about".</param>
/// <param name="Currency">For an amount of money: the currency, as three letters (ISO 4217).</param>
public sealed record Quantity(decimal Value, string Unit, bool IsApproximate = false, string? Currency = null);

/// <summary>
/// A statement about one entity: something that lasted or happened at a time of the calendar, a quantity –
/// a population, a number of verses, a price – or both: a quantity at a time.
/// </summary>
public sealed class DatedStatement
{
    /// <summary>Gets the identifier, unique in the collection; it names the row of the statement on a page.</summary>
    public required string Key { get; init; }

    /// <summary>Gets what lasted or happened, in a few words.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets a sentence about it, when the editors wrote one.</summary>
    public LocalizedText? Summary { get; init; }

    /// <summary>
    /// Gets the time, as it is stated: a period for what lasted, anything else for what happened. A statement
    /// of a quantity may have none: how many verses a poem has is not said for a year.
    /// </summary>
    public Vafadar.Temporal.TemporalExpression? Time { get; init; }

    /// <summary>Gets the quantity the statement states, when it states one.</summary>
    public Quantity? Quantity { get; init; }

    /// <summary>Gets the sources that support the statement and its time.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>Gets how far the editors have come with the statement.</summary>
    public required ReviewStatus Review { get; init; }

    /// <summary>Gets who reviewed the statement and when, once the editors have; a published revision needs it.</summary>
    public ReviewRecord? Reviewed { get; init; }

    /// <summary>Gets the reading level from which the statement may be shown; never before its entity.</summary>
    public required ReadingLevel Level { get; init; }
}

/// <summary>What a <see cref="Relation"/> is a statement about, and where in the story it holds.</summary>
public sealed class RelationScope
{
    /// <summary>Gets the kind of statement.</summary>
    public required RelationScopeKind Kind { get; init; }

    /// <summary>Gets the story the statement belongs to; a statement about the world outside a story may name none.</summary>
    public EntityId? Story { get; init; }

    /// <summary>Gets the events the statement is tied to; empty for a statement about the whole story.</summary>
    public required ImmutableArray<EventId> Events { get; init; }

    /// <summary>Gets the editors' note on how the statement is to be read, when there is one.</summary>
    public string? Interpretation { get; init; }
}

/// <summary>
/// A sourced statement that connects two entities: its type and direction, where in the story it holds, and the
/// passage that supports it. A relation is stated by the editors; nothing derives one from another, from a shared
/// event or from two things standing next to each other in a picture.
/// </summary>
public sealed class Relation
{
    /// <summary>Gets the identifier.</summary>
    public required RelationId Id { get; init; }

    /// <summary>Gets the entity the relation reads from.</summary>
    public required EntityId From { get; init; }

    /// <summary>Gets the entity the relation reads to.</summary>
    public required EntityId To { get; init; }

    /// <summary>Gets the type, which also says whether the relation has a direction.</summary>
    public required RelationType Type { get; init; }

    /// <summary>Gets the wording of the relation, read from <see cref="From"/> to <see cref="To"/>.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets what the statement is about and where it holds.</summary>
    public required RelationScope Scope { get; init; }

    /// <summary>Gets the sources that support the statement.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>Gets where in the sources the statement is found.</summary>
    public string? PassageLocator { get; init; }

    /// <summary>
    /// Gets when the relation held, as it is stated: a period for what lasted – a membership, a reign, a season –
    /// or a time for what happened once. Only a statement about the world outside a story has one; what a story
    /// tells has an order and no dates. Somebody who came back is two relations, each with its time: nothing
    /// joins two tenures into one.
    /// </summary>
    public TemporalExpression? Time { get; init; }

    /// <summary>Gets how far the editors have come with the statement.</summary>
    public required ReviewStatus Review { get; init; }

    /// <summary>Gets who reviewed the statement and when, once the editors have; a published revision needs it.</summary>
    public ReviewRecord? Reviewed { get; init; }

    /// <summary>Gets the reading level from which the relation may be shown.</summary>
    public required ReadingLevel Level { get; init; }
}

/// <summary>What one entity has to do with one event.</summary>
/// <param name="Entity">The entity.</param>
/// <param name="Role">What the entity does or is in the event, in the editors' words.</param>
/// <param name="Presence">What the association asserts about the entity being there.</param>
public sealed record EventAssociation(EntityId Entity, LocalizedText Role, Presence Presence);

/// <summary>
/// One event of a story, shared by everything that has to do with it. Its associations say what each entity does in
/// it; they are not a list of who was present. Its time is a place in the story order, a period, a date – whatever
/// the source states – and is never completed into something more exact.
/// </summary>
public sealed class StoryEvent
{
    /// <summary>Gets the identifier.</summary>
    public required EventId Id { get; init; }

    /// <summary>Gets the short title.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets the short description.</summary>
    public required LocalizedText Summary { get; init; }

    /// <summary>Gets when the event takes place, as stated.</summary>
    public required TemporalExpression Time { get; init; }

    /// <summary>Gets what each entity has to do with the event.</summary>
    public required ImmutableArray<EventAssociation> Associations { get; init; }

    /// <summary>
    /// Gets whether the editors condensed several scenes into this event. Then even two acting characters are not
    /// thereby in the same place at the same time.
    /// </summary>
    public bool CondensesSeveralScenes { get; init; }

    /// <summary>Gets the sources that support the event.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>Gets how far the editors have come with the event.</summary>
    public required ReviewStatus Review { get; init; }

    /// <summary>Gets who reviewed the statement and when, once the editors have; a published revision needs it.</summary>
    public ReviewRecord? Reviewed { get; init; }

    /// <summary>Gets the reading level from which the event may be shown.</summary>
    public required ReadingLevel Level { get; init; }
}

/// <summary>A source that supports statements: a passage of the text, a reference work, a museum record.</summary>
public sealed class Source
{
    /// <summary>Gets the identifier.</summary>
    public required SourceId Id { get; init; }

    /// <summary>Gets the title as it is cited.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the address of the source. Opening it leaves Dastan and may show more of the story.</summary>
    public required Uri Url { get; init; }

    /// <summary>Gets what kind of evidence the source is.</summary>
    public required SourceKind Kind { get; init; }

    /// <summary>Gets the day the editors looked the source up.</summary>
    public required DateOnly AccessedOn { get; init; }

    /// <summary>Gets where in the source the cited passage is, in words.</summary>
    public string? PassageLocator { get; init; }

    /// <summary>Gets the editors' note on how the source may be used.</summary>
    public string? ReuseNote { get; init; }

    /// <summary>Gets the authors, when the source names them.</summary>
    public ImmutableArray<string> Authors { get; init; } = [];

    /// <summary>Gets the day the source was published, when it states one.</summary>
    public DateOnly? PublishedOn { get; init; }

    /// <summary>Gets the day the source was last revised, when it states one.</summary>
    public DateOnly? UpdatedOn { get; init; }

    /// <summary>
    /// Gets the reading level from which the source may be named: its title and locator can give the story away.
    /// </summary>
    public required ReadingLevel Level { get; init; }
}

/// <summary>The record of a review: who of the editors looked at a statement, and on which day.</summary>
/// <param name="By">Who reviewed, as the editors name themselves. It is kept with the content and not shown on a page.</param>
/// <param name="On">The day of the review.</param>
/// <param name="Note">What the reviewer noted, if anything.</param>
public sealed record ReviewRecord(string By, DateOnly On, string? Note = null);
