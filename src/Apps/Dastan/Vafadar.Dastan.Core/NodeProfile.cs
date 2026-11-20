using System.Collections.Frozen;
using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>What a fact of a profile says: a time, a place, another node, or words.</summary>
public enum FactValue
{
    /// <summary>A time: an expression of <see cref="TemporalExpression"/>, never a bare year.</summary>
    Time = 0,

    /// <summary>A place: a node whose nature is a place.</summary>
    Place = 1,

    /// <summary>Another node of the collection, of any kind.</summary>
    Node = 2,

    /// <summary>Words, in the languages of the collection.</summary>
    Words = 3,
}

/// <summary>
/// One kind of fact a profile can state (step 19.5, ADR 0017). A key means the same and takes the same kind of
/// value on every kind of node that has it.
/// </summary>
/// <param name="Key">The key, as it is written in a record.</param>
/// <param name="Value">What the fact says.</param>
/// <param name="Meaning">What it means, for who writes a record.</param>
/// <param name="Many">Whether a node may state it more than once – the people who took part in an event.</param>
public sealed record FactKind(string Key, FactValue Value, string Meaning, bool Many = false);

/// <summary>The facts the profile of one kind of node has.</summary>
/// <param name="Essential">The facts the kind needs: one that is not stated is said to be unknown, not left out.</param>
/// <param name="Optional">The facts that may be added.</param>
public sealed record KindFacts(ImmutableArray<FactKind> Essential, ImmutableArray<FactKind> Optional)
{
    /// <summary>Gets every fact of the kind, the essential ones first.</summary>
    public ImmutableArray<FactKind> All { get; } = [.. Essential, .. Optional];

    /// <summary>Returns the fact of a key, or nothing when the kind has no such fact.</summary>
    public FactKind? Find(string key) => All.FirstOrDefault(fact => string.Equals(fact.Key, key, StringComparison.Ordinal));
}

/// <summary>
/// The table of the facts of a profile: for each kind of the ontology which facts are essential and which may
/// be added (ADR 0017). The same table stands in the product documents
/// (<c>docs/product/03-domain-model-and-ontology.md</c>, DOM-018); a test holds the two together.
/// </summary>
public static class ProfileFacts
{
    /// <summary>When a person or an animal was born.</summary>
    public static FactKind Born { get; } = new("born", FactValue.Time, "When they were born");

    /// <summary>When a person or an animal died. Somebody who lives states a time that does not apply.</summary>
    public static FactKind Died { get; } = new("died", FactValue.Time, "When they died; for somebody who lives, a time that does not apply");

    /// <summary>Where somebody was born.</summary>
    public static FactKind BirthPlace { get; } = new("birthPlace", FactValue.Place, "Where they were born");

    /// <summary>Where somebody died.</summary>
    public static FactKind DeathPlace { get; } = new("deathPlace", FactValue.Place, "Where they died");

    /// <summary>What a real person was or is.</summary>
    public static FactKind KnownFor { get; } = new("knownFor", FactValue.Words, "What they were or are: a poet, a ruler, a physician");

    /// <summary>What a character is in the story.</summary>
    public static FactKind Role { get; } = new("role", FactValue.Words, "What it is in the story, in words that give nothing of the story away");

    /// <summary>Where a character or a people comes from.</summary>
    public static FactKind Origin { get; } = new("origin", FactValue.Place, "Where it comes from");

    /// <summary>Whose companion an animal of a story is.</summary>
    public static FactKind CompanionOf { get; } = new("companionOf", FactValue.Node, "Whose companion it is");

    /// <summary>When a group, a place or a custom began.</summary>
    public static FactKind Founded { get; } = new("founded", FactValue.Time, "When it was founded, first held or first given");

    /// <summary>When a group or a place ended.</summary>
    public static FactKind Dissolved { get; } = new("dissolved", FactValue.Time, "When it was closed, dissolved or ended; for what still exists, a time that does not apply");

    /// <summary>What a group or a subject is about.</summary>
    public static FactKind Field { get; } = new("field", FactValue.Words, "Its field: what it does or is about");

    /// <summary>Where a group has its seat.</summary>
    public static FactKind Seat { get; } = new("seat", FactValue.Place, "Its seat, its capital or its head office");

    /// <summary>Who founded a group.</summary>
    public static FactKind Founder { get; } = new("founder", FactValue.Node, "Who founded it", Many: true);

    /// <summary>Where a club, a team or a people is at home.</summary>
    public static FactKind Home { get; } = new("home", FactValue.Place, "Where it is at home: the ground of a club, the land of a people");

    /// <summary>The colours of a club or a team.</summary>
    public static FactKind Colours { get; } = new("colours", FactValue.Words, "Its colours");

    /// <summary>The language of a work or of a people.</summary>
    public static FactKind Language { get; } = new("language", FactValue.Words, "Its language");

    /// <summary>What a node is a part of.</summary>
    public static FactKind PartOf { get; } = new("partOf", FactValue.Node, "What it is a part of: the region of a place, the work of a story, the club of a team");

    /// <summary>When a work or a thing was made.</summary>
    public static FactKind Created { get; } = new("created", FactValue.Time, "When it was made, written or first described");

    /// <summary>Who made a work or a thing.</summary>
    public static FactKind Creator { get; } = new("creator", FactValue.Node, "Who made it", Many: true);

    /// <summary>Who holds an object of a story.</summary>
    public static FactKind HeldBy { get; } = new("heldBy", FactValue.Node, "Who holds it in the story");

    /// <summary>Where a real thing is kept.</summary>
    public static FactKind KeptAt { get; } = new("keptAt", FactValue.Place, "Where it is kept");

    /// <summary>Who owns a brand or a thing.</summary>
    public static FactKind Owner { get; } = new("owner", FactValue.Node, "Who owns it");

    /// <summary>When something happened.</summary>
    public static FactKind Happened { get; } = new("happened", FactValue.Time, "When it happened or was held");

    /// <summary>Where something happened.</summary>
    public static FactKind Where { get; } = new("where", FactValue.Place, "Where it happened, or where it is found");

    /// <summary>Who took part in something that happened.</summary>
    public static FactKind Participant { get; } = new("participant", FactValue.Node, "Who took part", Many: true);

    /// <summary>When a period began.</summary>
    public static FactKind Began { get; } = new("began", FactValue.Time, "When it began");

    /// <summary>When a period ended.</summary>
    public static FactKind Ended { get; } = new("ended", FactValue.Time, "When it ended");

    /// <summary>Who holds a competition or gives an honour.</summary>
    public static FactKind Organiser { get; } = new("organiser", FactValue.Node, "Who holds it, or who gives it");

    /// <summary>Who won a season of a competition.</summary>
    public static FactKind Winner { get; } = new("winner", FactValue.Node, "Who won it");

    private static readonly FrozenDictionary<string, KindFacts> Table = new Dictionary<string, KindFacts>(StringComparer.Ordinal)
    {
        // What a story tells.
        [EntityTypes.Work.Key] = Facts([Creator, Created], [Language, PartOf]),
        [EntityTypes.StoryArc.Key] = Facts([PartOf], [Created]),
        [EntityTypes.Character.Key] = Facts([Role], [Origin]),
        [EntityTypes.AnimalCharacter.Key] = Facts([Role], [CompanionOf]),
        [EntityTypes.NarrativePlace.Key] = Facts([PartOf], []),
        [EntityTypes.NarrativeRegion.Key] = Facts([PartOf], []),
        [EntityTypes.NarrativeObject.Key] = Facts([HeldBy], [Creator]),

        // People, and people who act under one name.
        [EntityTypes.Person.Key] = Facts([Born, Died, KnownFor], [BirthPlace, DeathPlace]),
        [EntityTypes.Organisation.Key] = Facts([Founded, Field], [Dissolved, Seat, Founder]),
        [EntityTypes.Company.Key] = Facts([Founded, Field, Seat], [Dissolved, Founder]),
        [EntityTypes.University.Key] = Facts([Founded, Seat], [Dissolved, Founder, Field]),
        [EntityTypes.Government.Key] = Facts([Founded, Seat], [Dissolved]),
        [EntityTypes.SportsClub.Key] = Facts([Founded, Home], [Colours, Dissolved]),
        [EntityTypes.SportsTeam.Key] = Facts([Founded, Home], [Colours, PartOf, Dissolved]),
        [EntityTypes.Dynasty.Key] = Facts([Founded, Seat], [Dissolved, Founder]),
        [EntityTypes.Family.Key] = Facts([Founded, Seat], [Founder]),
        [EntityTypes.Community.Key] = Facts([Home], [Language]),

        // Places.
        [EntityTypes.Place.Key] = Facts([PartOf], [Founded, Dissolved]),
        [EntityTypes.Region.Key] = Facts([PartOf], [Founded, Dissolved]),
        [EntityTypes.Country.Key] = Facts([Seat], [Founded, Dissolved, PartOf]),

        // Things.
        [EntityTypes.RealObject.Key] = Facts([Created, KeptAt], [Creator, Owner]),
        [EntityTypes.Brand.Key] = Facts([Founded, Owner], [Dissolved]),
        [EntityTypes.Product.Key] = Facts([Created, Creator], [Dissolved, PartOf]),

        // What happened, and spans of time.
        [EntityTypes.Event.Key] = Facts([Happened, Where], [Participant]),
        [EntityTypes.Period.Key] = Facts([Began, Ended], [Where]),
        [EntityTypes.Competition.Key] = Facts([Founded, Field], [Organiser, Where]),
        [EntityTypes.CompetitionSeason.Key] = Facts([PartOf, Happened], [Winner, Where]),

        // Roles, and subjects.
        [EntityTypes.Office.Key] = Facts([PartOf], [Founded, Dissolved]),
        [EntityTypes.Honour.Key] = Facts([Organiser], [Founded, Field]),
        [EntityTypes.NaturalSubject.Key] = Facts([Field], [Where]),
        [EntityTypes.Concept.Key] = Facts([Field], [Created, Creator]),
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Gets every kind of fact the table knows, by its key.</summary>
    public static ImmutableArray<FactKind> All { get; } =
        [.. Table.Values.SelectMany(facts => facts.All).Distinct().OrderBy(fact => fact.Key, StringComparer.Ordinal)];

    /// <summary>Returns the facts of a kind of node. Every kind of the ontology has some.</summary>
    public static KindFacts Of(EntityType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Table.TryGetValue(type.Key, out var facts) ? facts : throw new ArgumentException($"The kind {type.Key} has no facts in the table of profiles.", nameof(type));
    }

    /// <summary>Returns the kind of fact of a key, or nothing when no kind of node has it.</summary>
    public static FactKind? Find(string key) => All.FirstOrDefault(fact => string.Equals(fact.Key, key, StringComparison.Ordinal));

    private static KindFacts Facts(FactKind[] essential, FactKind[] optional) => new([.. essential], [.. optional]);
}

/// <summary>
/// One fact a record states about its node: a time, a place, another node or words – exactly the one its kind
/// of fact takes – with the sources it rests on and from where in the story on it may be told. Like another
/// name of a node it belongs to the record of the node and has no review of its own.
/// </summary>
public sealed class ProfileFact
{
    /// <summary>Gets the kind of fact.</summary>
    public required FactKind Kind { get; init; }

    /// <summary>Gets the time, for a fact that says when. A time nobody knows is stated as unknown.</summary>
    public TemporalExpression? Time { get; init; }

    /// <summary>Gets the node, for a fact that names a place or another node.</summary>
    public EntityId? Node { get; init; }

    /// <summary>Gets the words, for a fact that is said in words.</summary>
    public LocalizedText? Words { get; init; }

    /// <summary>Gets the sources the fact rests on.</summary>
    public required ImmutableArray<SourceId> Sources { get; init; }

    /// <summary>Gets the reading stage from which the fact may be told.</summary>
    public required ReadingLevel Level { get; init; }
}

/// <summary>One line of a profile: a kind of fact, and what is stated of it. Nothing stated: it is not known.</summary>
/// <param name="Kind">The kind of fact.</param>
/// <param name="Stated">What the node states, in the order of its record; empty when nothing is known to the visitor.</param>
public sealed record ProfileLine(FactKind Kind, ImmutableArray<ProfileFact> Stated)
{
    /// <summary>Gets a value indicating whether the visitor is told nothing of this fact.</summary>
    public bool IsUnknown => Stated.IsEmpty;
}

/// <summary>
/// The profile of a node as one visitor may see it (ADR 0017): who or what it is, its picture when the visitor
/// may see that picture, the facts its kind needs – each stated or said to be unknown – and the facts that
/// were added. A page shows all of it; a card shows <see cref="Card"/>.
/// </summary>
/// <param name="Entity">The node.</param>
/// <param name="Picture">The picture of the profile, or nothing: none was chosen, or the visitor may not see it.</param>
/// <param name="Essential">The facts the kind needs, in the order of the table – every one, stated or not.</param>
/// <param name="More">The facts that were added, in the order of the table – only those that are stated.</param>
public sealed record NodeProfile(Entity Entity, MediaItem? Picture, ImmutableArray<ProfileLine> Essential, ImmutableArray<ProfileLine> More)
{
    /// <summary>How many facts a card shows at most.</summary>
    public const int CardFacts = 3;

    /// <summary>Gets the facts a card shows: the first stated ones, the essential before the added.</summary>
    public ImmutableArray<ProfileLine> Card { get; } = [.. Essential.Concat(More).Where(line => !line.IsUnknown).Take(CardFacts)];
}
