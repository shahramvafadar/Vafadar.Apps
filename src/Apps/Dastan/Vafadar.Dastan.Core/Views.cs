using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

// The answers of a projection. Every record here was built from what one visitor may see, so a page can show all of
// it. A page never shows the ordinal of a story event: a gap between two ordinals would tell that something is
// hidden between them (ADR 0002).

/// <summary>An event of the story among the events a visitor may see.</summary>
/// <param name="Number">The place of the event among the visible events, from 1. This is what a page shows.</param>
/// <param name="Event">The event.</param>
public sealed record StoryStep(int Number, StoryEvent Event)
{
    /// <summary>Gets the story the event is a step of.</summary>
    public EntityId Story => new(((NarrativePosition)((Occurrence)Event.Time).At).Sequence);
}

/// <summary>The entry of a collection.</summary>
/// <param name="Subject">What the collection is about: the story it tells, or a company, a club, a person.</param>
/// <param name="Work">The work the story belongs to, when the collection follows a text.</param>
/// <param name="Edition">The text the editors worked from, when the collection follows one.</param>
/// <param name="Stages">The reading stages a visitor can choose from. Their labels are written for readers.</param>
/// <param name="Boundary">The stage the visitor has read to.</param>
/// <param name="StoryContinues">Whether a later stage exists. It says nothing about what or how much follows.</param>
/// <param name="Subjects">Who a visitor can start from – beings and groups – in the order they appear in the story.</param>
/// <param name="FirstStep">The first event of the story, if one is visible.</param>
public sealed record CollectionOverview(
    Entity Subject,
    Entity? Work,
    EditionContext? Edition,
    ImmutableArray<ReadingStage> Stages,
    ReadingLevel Boundary,
    bool StoryContinues,
    ImmutableArray<Entity> Subjects,
    StoryStep? FirstStep)
{
    /// <summary>Gets the stories the collection tells, the one a visitor enters first at the beginning.</summary>
    public ImmutableArray<Entity> Stories { get; init; } = [];
}

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
    ImmutableArray<Entity> Expandable)
{
    /// <summary>
    /// Gets the stretch of years the neighbourhood was asked for (D-084), or nothing. With one, a relation that
    /// states a time is among the edges only when it held in those years; a relation that states no time is
    /// among them always – it is told apart, not hidden.
    /// </summary>
    public Vafadar.Temporal.YearSpan? Years { get; init; }

    /// <summary>Gets the relations among the edges that only may have held in the years: their time is less than exact, or reaches out of them.</summary>
    public ImmutableHashSet<RelationId> Possibly { get; init; } = [];

    /// <summary>Gets how many relations of what is open state a time outside the years and are left out.</summary>
    public int LeftOut { get; init; }

    /// <summary>Gets the edges that held in the years that were asked for; without years, every edge.</summary>
    public IEnumerable<Edge> InYears => Years is null ? Edges : Edges.Where(edge => edge.Relation.Time is not null);

    /// <summary>Gets the edges that state no time, when years were asked for; without years, none.</summary>
    public IEnumerable<Edge> WithoutTime => Years is null ? [] : Edges.Where(edge => edge.Relation.Time is null);
}

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
    bool MoreBeside = false)
{
    /// <summary>Gets the stretch of years the tree was asked for in (D-084), or nothing for every year.</summary>
    public Vafadar.Temporal.YearSpan? Years { get; init; }

    /// <summary>
    /// Gets how many family relations are left out in the years: those that state another time, and those a
    /// relation of another time cuts off from the one the tree is about.
    /// </summary>
    public int LeftOut { get; init; }

    /// <summary>Gets how many of the relations that are drawn state no time: they are drawn at every time.</summary>
    public int WithoutTime { get; init; }

    /// <summary>Gets the relations of which it is not sure that they held in the years.</summary>
    public ImmutableHashSet<RelationId> Possibly { get; init; } = [];
}

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

    /// <summary>The items that show a time that was asked for: a year, a stretch of years or a depth of time (ADR 0021).</summary>
    ShowsTime = 3,

    /// <summary>The items the editors gathered under the theme (R-MED-02).</summary>
    OfTheme = 4,
}

/// <summary>A theme of a collection with how many items a visitor may see under it – shown or named as a reference.</summary>
/// <param name="Theme">The theme.</param>
/// <param name="Items">How many items stand under it for this visitor.</param>
public sealed record ThemeCount(Entity Theme, int Items);

/// <summary>How the items of a gallery are ordered.</summary>
public enum GalleryOrder
{
    /// <summary>The order the editors gave the items. No date is involved.</summary>
    Editorial = 0,

    /// <summary>By when each item was made, the oldest first. It is the time of the making – never the time of what an item shows.</summary>
    OldestFirst = 1,

    /// <summary>By when each item was made, the newest first.</summary>
    NewestFirst = 2,
}

/// <summary>
/// A work the collection has to do with and may not show: what the catalogue of the institution that holds it
/// says about it, and the way to its page there. The record holds nothing of the image – no address of a file, no
/// text alternative, no path – so that a page that shows all of it shows nothing it may not.
/// </summary>
/// <param name="Id">The identifier of the media item the reference stands for.</param>
/// <param name="Kind">Whether the work is an image or a video.</param>
/// <param name="Label">The title the editors give it.</param>
/// <param name="Caption">The editors' caption.</param>
/// <param name="Representation">How the work relates to what it shows.</param>
/// <param name="Created">When the work was made – never a time of the story.</param>
/// <param name="Creator">Who made it, as far as it is known, in the editors' words.</param>
/// <param name="Rights">What is established about the right to show it.</param>
/// <param name="CataloguePage">The public page of the institution that holds the work.</param>
/// <param name="DepictedEntities">The entities the work shows.</param>
/// <param name="DepictedEvents">The events the work shows.</param>
public sealed record MediaReference(
    MediaId Id,
    MediaKind Kind,
    LocalizedText Label,
    LocalizedText Caption,
    MediaRepresentation Representation,
    TemporalExpression Created,
    string Creator,
    RightsStatus Rights,
    Uri CataloguePage,
    ImmutableArray<EntityId> DepictedEntities,
    ImmutableArray<EventId> DepictedEvents)
{
    /// <summary>Gets the themes the work is gathered under.</summary>
    public ImmutableArray<EntityId> Themes { get; init; } = [];

    /// <summary>Gets the title the holding institution gives the work.</summary>
    /// <summary>Gets the time the item shows, where it shows a time of the world.</summary>
    public Vafadar.Temporal.TemporalExpression? Shows { get; init; }

    public string? CatalogueTitle { get; init; }

    /// <summary>Gets the number the holding institution gives the work.</summary>
    public string? AccessionNumber { get; init; }

    /// <summary>Gets the credit line the holding institution asks for.</summary>
    public string? CreditLine { get; init; }
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
    ImmutableArray<MediaItem> Items)
{
    /// <summary>
    /// Gets the items of the rule that may not be shown and are held by an institution with a public page: the
    /// gallery names them with their catalogue record and the way to that page, and shows nothing of them.
    /// </summary>
    public ImmutableArray<MediaReference> References { get; init; } = [];

    /// <summary>Gets the time that was asked for, for <see cref="GalleryRule.ShowsTime"/>.</summary>
    public AskedTime? Time { get; init; }

    /// <summary>
    /// Gets the stretch of years the gallery of a name was asked for (D-084), or nothing. With one,
    /// <see cref="Items"/> are the items that show those years, and <see cref="WithoutTime"/> those that state
    /// no time they show.
    /// </summary>
    public Vafadar.Temporal.YearSpan? Years { get; init; }

    /// <summary>Gets the items of the rule that state no time they show, when years were asked for: told apart, not hidden.</summary>
    public ImmutableArray<MediaItem> WithoutTime { get; init; } = [];

    /// <summary>Gets how many items of the rule – shown, or named as held elsewhere – show another time than the years and are left out.</summary>
    public int LeftOut { get; init; }

    /// <summary>
    /// Gets the themes a visitor can go on to from this gallery, in the order of the collection: every theme
    /// that has something to show at this reading position. A theme with nothing under it is not offered.
    /// </summary>
    public ImmutableArray<ThemeCount> Themes { get; init; } = [];

    /// <summary>
    /// Gets the items that were made in the time that was asked for and do not show it – or do not say what
    /// time they show. They are named apart: a picture made in a time is no picture of that time.
    /// </summary>
    public ImmutableArray<MediaItem> MadeThen { get; init; } = [];

    /// <summary>Gets the items that only may show the time: what they show is less than exact, or reaches out of it.</summary>
    public ImmutableHashSet<MediaId> Possibly { get; init; } = [];

    /// <summary>
    /// Returns the gallery with the items of one kind only, or in another order. An item whose time of making
    /// cannot be placed among the years stands after the others, in the order of the editors.
    /// </summary>
    public GalleryView Arranged(GalleryOrder order, MediaKind? kind)
    {
        var items = Items.Where(item => kind is null || item.Kind == kind);
        var undated = WithoutTime.Where(item => kind is null || item.Kind == kind);
        var references = References.Where(item => kind is null || item.Kind == kind);
        if (order != GalleryOrder.Editorial)
        {
            decimal? Made(Vafadar.Temporal.TemporalExpression time) => YearCover.Reach(time)?.Middle;
            IEnumerable<MediaItem> Ordered(IEnumerable<MediaItem> shown) => order == GalleryOrder.OldestFirst
                ? shown.OrderBy(item => Made(item.Created) is null).ThenBy(item => Made(item.Created))
                : shown.OrderBy(item => Made(item.Created) is null).ThenByDescending(item => Made(item.Created));
            items = Ordered(items);
            undated = Ordered(undated);
            references = order == GalleryOrder.OldestFirst
                ? references.OrderBy(item => Made(item.Created) is null).ThenBy(item => Made(item.Created))
                : references.OrderBy(item => Made(item.Created) is null).ThenByDescending(item => Made(item.Created));
        }

        return this with { Order = order, Items = [.. items], WithoutTime = [.. undated], References = [.. references], MadeThen = [.. MadeThen.Where(item => kind is null || item.Kind == kind)] };
    }

    /// <summary>Gets whether the gallery has nothing to show and nothing to refer to.</summary>
    public bool IsEmpty => Items.IsEmpty && WithoutTime.IsEmpty && References.IsEmpty && MadeThen.IsEmpty;
}

/// <summary>One media item with what it shows.</summary>
/// <param name="Item">The item.</param>
/// <param name="Depicted">The entities it depicts.</param>
/// <param name="Steps">The events it depicts.</param>
/// <param name="Relations">The relations it explains.</param>
public sealed record MediaView(
    MediaItem Item,
    ImmutableArray<Entity> Depicted,
    ImmutableArray<StoryStep> Steps,
    ImmutableArray<Edge> Relations)
{
    /// <summary>Gets the themes the item is gathered under, as far as the visitor has read: a theme of a later stage is not named.</summary>
    public ImmutableArray<Entity> Themes { get; init; } = [];
}

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
    EditionContext? Edition,
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

/// <summary>What the hits of a search have to be, beside having the name.</summary>
/// <param name="Nature">What kind of thing: a being, a group, a place, a work.</param>
/// <param name="Place">A place the hit is connected to by a relation, or is itself.</param>
/// <param name="Year">A year in which the hit takes part in a relation that held or may have held, or for which something is stated of it.</param>
public sealed record SearchFilter(EntityNature? Nature = null, EntityId? Place = null, YearOfEra? Year = null)
{
    /// <summary>
    /// Gets a time the hit takes part in something that held or may have held, or for which something is stated
    /// of it: a year, a stretch of years or a depth of time, as a visitor asked for it (ADR 0021).
    /// </summary>
    public AskedTime? Time { get; init; }

    /// <summary>Gets the year of the common era the visitor asks in; a depth of time is counted from it.</summary>
    public int Present { get; init; }

    /// <summary>
    /// Gets the keys of the kinds a hit has to be of (ADR 0029): one of them, or a kind below one of them. Empty,
    /// the kind does not matter.
    /// </summary>
    public ImmutableHashSet<string> Kinds { get; init; } = [];

    /// <summary>Gets a value indicating whether the filter lets everything through.</summary>
    public bool IsEmpty => Nature is null && Place is null && Year is null && Time is null && Kinds.IsEmpty;
}

/// <summary>What a collection has with a time of the calendar, as far as the visitor may see it.</summary>
/// <param name="Subjects">The entities something dated is stated about, in the order of the collection.</param>
/// <param name="Made">The media that may be shown; each has the time it was made.</param>
/// <param name="Held">The relations that say when they held, each with its two entities.</param>
public sealed record TimelineView(ImmutableArray<Entity> Subjects, ImmutableArray<MediaItem> Made, ImmutableArray<Edge> Held)
{
    /// <summary>Gets a value indicating whether there is nothing with a time to show.</summary>
    public bool IsEmpty => Subjects.IsEmpty && Made.IsEmpty;
}
