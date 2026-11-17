using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

// The answers of a projection. Every record here was built from what one visitor may see, so a page can show all of
// it. A page never shows the ordinal of a story event: a gap between two ordinals would tell that something is
// hidden between them (ADR 0002).

/// <summary>An event of the story among the events a visitor may see.</summary>
/// <param name="Number">The place of the event among the visible events, from 1. This is what a page shows.</param>
/// <param name="Event">The event.</param>
public sealed record StoryStep(int Number, StoryEvent Event);

/// <summary>The entry of a collection.</summary>
/// <param name="Story">The story the collection tells.</param>
/// <param name="Work">The work the story belongs to.</param>
/// <param name="Edition">The text the editors worked from.</param>
/// <param name="Stages">The reading stages a visitor can choose from. Their labels are written for readers.</param>
/// <param name="Boundary">The stage the visitor has read to.</param>
/// <param name="StoryContinues">Whether a later stage exists. It says nothing about what or how much follows.</param>
/// <param name="Subjects">Who a visitor can start from – beings and groups – in the order they appear in the story.</param>
/// <param name="FirstStep">The first event of the story, if one is visible.</param>
public sealed record CollectionOverview(
    Entity Story,
    Entity Work,
    EditionContext Edition,
    ImmutableArray<ReadingStage> Stages,
    ReadingLevel Boundary,
    bool StoryContinues,
    ImmutableArray<Entity> Subjects,
    StoryStep? FirstStep);

/// <summary>How a relation reads from the side of one of its entities.</summary>
public enum ConnectionDirection
{
    /// <summary>The entity is the first of the relation: the label reads from it.</summary>
    Outgoing = 0,

    /// <summary>The entity is the second of the relation: the label reads towards it.</summary>
    Incoming = 1,

    /// <summary>The relation is the same from both sides.</summary>
    Mutual = 2,
}

/// <summary>A relation, seen from one of its entities.</summary>
/// <param name="Relation">The relation.</param>
/// <param name="Other">The entity at the other end.</param>
/// <param name="Direction">How the label reads from this side.</param>
public sealed record Connection(Relation Relation, Entity Other, ConnectionDirection Direction);

/// <summary>What an entity has to do with one event.</summary>
/// <param name="Step">The event.</param>
/// <param name="Association">The role and the presence of the entity.</param>
public sealed record Appearance(StoryStep Step, EventAssociation Association);

/// <summary>One entity with everything a visitor may follow from it.</summary>
/// <param name="Entity">The entity.</param>
/// <param name="Connections">Its relations, in the order of the collection.</param>
/// <param name="Appearances">The events it has to do with, in story order.</param>
/// <param name="Media">The media that depict it.</param>
public sealed record EntityOverview(
    Entity Entity,
    ImmutableArray<Connection> Connections,
    ImmutableArray<Appearance> Appearances,
    ImmutableArray<MediaItem> Media);

/// <summary>A relation with its two entities, in the direction the relation states.</summary>
/// <param name="Relation">The relation.</param>
/// <param name="From">The first entity.</param>
/// <param name="To">The second entity.</param>
public sealed record Edge(Relation Relation, Entity From, Entity To);

/// <summary>
/// The neighbourhood of an entity: the same nodes and edges for the graph and for the list next to it, so the two
/// cannot disagree.
/// </summary>
/// <param name="Focus">The entity in the middle.</param>
/// <param name="Nodes">The focus first, then every entity an edge leads to, in the order of the collection.</param>
/// <param name="Edges">The relations of the focus and of the expanded entities.</param>
/// <param name="Expanded">The nodes other than the focus whose relations are shown.</param>
/// <param name="Expandable">The nodes that have relations which are not among the edges yet.</param>
public sealed record ConnectionsView(
    Entity Focus,
    ImmutableArray<Entity> Nodes,
    ImmutableArray<Edge> Edges,
    ImmutableArray<Entity> Expanded,
    ImmutableArray<Entity> Expandable);

/// <summary>A member of a family view.</summary>
/// <param name="Entity">The entity.</param>
/// <param name="Generation">The row of the member: 0 for those without a visible parent, children one below their parents.</param>
public sealed record FamilyMember(Entity Entity, int Generation);

/// <summary>Parents, children and partners, as far as the relations state them.</summary>
/// <param name="Members">The members, by generation and then in the order of the collection.</param>
/// <param name="Parentage">The relations from a parent to a child.</param>
/// <param name="Partnerships">The relations between partners.</param>
/// <param name="MoreAbove">Whether the view ends above at somebody whose parents the visitor may see but the view leaves out.</param>
/// <param name="MoreBelow">Whether the view ends below at somebody whose children the visitor may see but the view leaves out.</param>
/// <param name="MoreBeside">Whether the view leaves out relatives beside the line – brothers and sisters, their children – whom the visitor may see.</param>
public sealed record FamilyView(
    ImmutableArray<FamilyMember> Members,
    ImmutableArray<Edge> Parentage,
    ImmutableArray<Edge> Partnerships,
    bool MoreAbove = false,
    bool MoreBelow = false,
    bool MoreBeside = false);

/// <summary>The events of the story in the order of the telling.</summary>
/// <param name="Story">The story.</param>
/// <param name="Steps">The visible events.</param>
/// <param name="Continues">Whether a later reading stage exists. It says nothing about what or how much follows.</param>
public sealed record StoryView(Entity Story, ImmutableArray<StoryStep> Steps, bool Continues);

/// <summary>The row of one entity across the events of the story.</summary>
/// <param name="Entity">The entity; its nature says whether it is a being, a place or an object.</param>
/// <param name="Cells">One cell per step of the view, empty where the entity has nothing to do with the event.</param>
public sealed record Lane(Entity Entity, ImmutableArray<EventAssociation?> Cells);

/// <summary>
/// The story as rows of entities over the same steps as <see cref="StoryView"/>. A cell keeps the presence, so
/// somebody who is only mentioned is never drawn as somebody who is there.
/// </summary>
/// <param name="Steps">The visible events, the same as in the story view.</param>
/// <param name="Lanes">The entities, in the order they first appear.</param>
public sealed record LanesView(ImmutableArray<StoryStep> Steps, ImmutableArray<Lane> Lanes);

/// <summary>Why the items of a gallery are in it.</summary>
public enum GalleryRule
{
    /// <summary>Every item of the collection that may be shown.</summary>
    WholeCollection = 0,

    /// <summary>The items that depict the entity.</summary>
    DepictsEntity = 1,

    /// <summary>The items that depict the event.</summary>
    DepictsEvent = 2,
}

/// <summary>How the items of a gallery are ordered.</summary>
public enum GalleryOrder
{
    /// <summary>The order the editors gave the items. No date is involved.</summary>
    Editorial = 0,
}

/// <summary>Media that may be shown, with the rule that selected them.</summary>
/// <param name="Rule">Why the items are in the gallery.</param>
/// <param name="Entity">The entity of the rule, if it has one.</param>
/// <param name="Step">The event of the rule, if it has one.</param>
/// <param name="Order">How the items are ordered.</param>
/// <param name="Items">The items; empty when nothing may be shown at this reading position.</param>
public sealed record GalleryView(
    GalleryRule Rule,
    Entity? Entity,
    StoryStep? Step,
    GalleryOrder Order,
    ImmutableArray<MediaItem> Items);

/// <summary>One media item with what it shows.</summary>
/// <param name="Item">The item.</param>
/// <param name="Depicted">The entities it depicts.</param>
/// <param name="Steps">The events it depicts.</param>
/// <param name="Relations">The relations it explains.</param>
public sealed record MediaView(
    MediaItem Item,
    ImmutableArray<Entity> Depicted,
    ImmutableArray<StoryStep> Steps,
    ImmutableArray<Edge> Relations);

/// <summary>The kind of statement an evidence view supports.</summary>
public enum EvidenceSubject
{
    /// <summary>That an entity exists and is what its summary says.</summary>
    Entity = 0,

    /// <summary>A relation between two entities.</summary>
    Relation = 1,

    /// <summary>An event.</summary>
    Event = 2,

    /// <summary>What one entity has to do with one event.</summary>
    Association = 3,

    /// <summary>A media item: what it is and who made it.</summary>
    Media = 4,
}

/// <summary>The sources of one statement – of that statement, not of everything around it.</summary>
/// <param name="Subject">The kind of the statement.</param>
/// <param name="Sources">The sources.</param>
/// <param name="Edition">The text the editors worked from.</param>
/// <param name="SourcesMayRevealMore">
/// Whether a page should warn before opening a source: the pages of a source are outside the app and can tell more
/// of the story than the visitor has read.
/// </param>
public sealed record EvidenceView(
    EvidenceSubject Subject,
    ImmutableArray<Source> Sources,
    EditionContext Edition,
    bool SourcesMayRevealMore)
{
    /// <summary>Gets the entity of the statement, for an entity and for an association.</summary>
    public Entity? Entity { get; init; }

    /// <summary>Gets the relation with its entities, for a relation.</summary>
    public Edge? Relation { get; init; }

    /// <summary>Gets the event, for an event and for an association.</summary>
    public StoryStep? Step { get; init; }

    /// <summary>Gets the role and the presence, for an association.</summary>
    public EventAssociation? Association { get; init; }

    /// <summary>Gets the media item, for a media item.</summary>
    public MediaItem? Media { get; init; }

    /// <summary>Gets where in the sources to look, when the statement has a locator of its own.</summary>
    public string? PassageLocator { get; init; }

    /// <summary>Gets how far the editors' review of the statement has come, when the statement carries one.</summary>
    public ReviewStatus? Review { get; init; }

    /// <summary>Gets what the sources disagree about, for a disputed statement whose review says so.</summary>
    public string? Dispute { get; init; }
}

/// <summary>An entity found by its name.</summary>
/// <param name="Entity">The entity.</param>
/// <param name="Name">The name that matched.</param>
/// <param name="Language">The language of that name.</param>
public sealed record SearchHit(Entity Entity, string Name, string Language);

/// <summary>What a collection has with a time of the calendar, as far as the visitor may see it.</summary>
/// <param name="Subjects">The entities something dated is stated about, in the order of the collection.</param>
/// <param name="Made">The media that may be shown; each has the time it was made.</param>
public sealed record TimelineView(ImmutableArray<Entity> Subjects, ImmutableArray<MediaItem> Made)
{
    /// <summary>Gets a value indicating whether there is nothing with a time to show.</summary>
    public bool IsEmpty => Subjects.IsEmpty && Made.IsEmpty;
}
