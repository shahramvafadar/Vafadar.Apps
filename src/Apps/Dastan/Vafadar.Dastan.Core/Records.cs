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
}

/// <summary>What a <see cref="Relation"/> is a statement about, and where in the story it holds.</summary>
public sealed class RelationScope
{
    /// <summary>Gets the kind of statement.</summary>
    public required RelationScopeKind Kind { get; init; }

    /// <summary>Gets the story the statement belongs to.</summary>
    public required EntityId Story { get; init; }

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

    /// <summary>Gets how far the editors have come with the statement.</summary>
    public required ReviewStatus Review { get; init; }

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

    /// <summary>
    /// Gets the reading level from which the source may be named: its title and locator can give the story away.
    /// </summary>
    public required ReadingLevel Level { get; init; }
}
