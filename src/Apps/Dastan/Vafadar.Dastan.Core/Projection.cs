using System.Collections.Frozen;
using System.Collections.Immutable;
using Vafadar.Core.Text;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>Who a collection is shown to.</summary>
public enum Audience
{
    /// <summary>A demonstration on the machine of the owner or a reviewer.</summary>
    LocalDemonstration = 0,

    /// <summary>Everybody on the web.</summary>
    Public = 1,
}

/// <summary>
/// What one visitor may see of a collection: the records that the publication state allows for the audience and
/// that lie within the visitor's reading boundary. Every view – overview, connections, family, story, lanes,
/// gallery, evidence, search – is an answer of this class and is built from these records only, so a view cannot
/// name, count or link to something the visitor has not read yet. A record that is hidden and a record that does
/// not exist give the same answer: none (ADR 0002).
/// </summary>
public sealed class Projection
{
    private readonly CatalogueInfo _info;
    private readonly FrozenDictionary<EntityId, Entity> _entities;
    private readonly FrozenDictionary<RelationId, Relation> _relations;
    private readonly FrozenDictionary<SourceId, Source> _sources;
    private readonly FrozenDictionary<MediaId, MediaItem> _media;
    private readonly FrozenDictionary<EventId, StoryStep> _steps;
    private readonly FrozenDictionary<EntityId, ImmutableArray<StoryStep>> _stepsOf;
    private readonly ILookup<EntityId, Relation> _relationsOf;

    private Projection(Catalogue catalogue, ReadingLevel boundary, Audience audience)
    {
        _info = catalogue.Info;
        Boundary = boundary;
        // An entity is shown with what is stated about it up to the boundary; a later statement is not part of
        // the entity a visitor gets.
        Entities =
        [
            .. catalogue.Entities
                .Where(entity => entity.Level.IsWithin(boundary))
                .Select(entity => entity.Dated.All(dated => dated.Level.IsWithin(boundary)) && entity.Names.All(name => name.Level.IsWithin(boundary))
                        && entity.Facts.All(fact => fact.Level.IsWithin(boundary))
                    ? entity
                    : new Entity
                    {
                        Id = entity.Id,
                        Type = entity.Type,
                        Label = entity.Label,
                        Summary = entity.Summary,
                        Sources = entity.Sources,
                        Level = entity.Level,
                        Dated = [.. entity.Dated.Where(dated => dated.Level.IsWithin(boundary))],
                        Names = [.. entity.Names.Where(name => name.Level.IsWithin(boundary))],
                        Identifiers = entity.Identifiers,
                        Position = entity.Position,
                        Facts = [.. entity.Facts.Where(fact => fact.Level.IsWithin(boundary))],
                        Picture = entity.Picture,
                    }),
        ];
        Relations = [.. catalogue.Relations.Where(relation => relation.Level.IsWithin(boundary))];
        Media = [.. catalogue.Media.Where(item => item.Level.IsWithin(boundary) && MayBeShown(item, audience))];

        // An item that may not be shown and has a public page at the institution that holds it is a reference:
        // its catalogue record and the way to that page, by the same reading boundary. It is no media item of
        // the visitor – it has no page, no file and no place in a chart – and nothing of the image is delivered.
        References =
        [
            .. catalogue.Media
                .Where(item => item.Level.IsWithin(boundary) && !MayBeShown(item, audience) && item.CataloguePage is not null)
                .Select(item => new MediaReference(
                    item.Id, item.Kind, item.Label, item.Caption, item.Representation, item.Created, item.Creator, item.Rights.Status,
                    item.CataloguePage!, item.DepictedEntities, item.DepictedEvents)
                {
                    CatalogueTitle = item.CatalogueTitle,
                    AccessionNumber = item.AccessionNumber,
                    CreditLine = item.CreditLine,
                }),
        ];

        // The steps of every story: the visible events that have a place in its telling, in that order. Each
        // story counts its own steps from 1.
        var shown = Entities.Select(entity => entity.Id).ToHashSet();
        Stories = [.. _info.Stories.Where(shown.Contains)];
        _stepsOf = Stories.ToFrozenDictionary(
            story => story,
            story => (ImmutableArray<StoryStep>)
            [
                .. catalogue.Events
                    .Where(storyEvent => storyEvent.Level.IsWithin(boundary))
                    .Select(storyEvent => (Event: storyEvent, Position: (storyEvent.Time as Occurrence)?.At as NarrativePosition))
                    .Where(entry => entry.Position is { Order: NarrativeOrder.Presentation } position && position.Sequence == story.Value)
                    .OrderBy(entry => entry.Position!.Ordinal)
                    .Select((entry, index) => new StoryStep(index + 1, entry.Event)),
            ]);
        Steps = Stories.IsEmpty ? [] : _stepsOf[Stories[0]];
        AllSteps = [.. Stories.SelectMany(story => _stepsOf[story])];

        // Only the sources that support something visible are named: a source of a hidden record or of an item that
        // may not be shown would tell that the record exists.
        var cited = Entities.SelectMany(entity => entity.Sources.Concat(entity.Dated.SelectMany(dated => dated.Sources)).Concat(entity.Names.SelectMany(name => name.Sources)).Concat(entity.Facts.SelectMany(fact => fact.Sources)))
            .Concat(Relations.SelectMany(relation => relation.Sources))
            .Concat(AllSteps.SelectMany(step => step.Event.Sources))
            .Concat(Media.SelectMany(item => item.Sources))
            .ToHashSet();
        Sources = [.. catalogue.Sources.Where(source => cited.Contains(source.Id))];

        _entities = Entities.ToFrozenDictionary(entity => entity.Id);
        _relations = Relations.ToFrozenDictionary(relation => relation.Id);
        _sources = Sources.ToFrozenDictionary(source => source.Id);
        _media = Media.ToFrozenDictionary(item => item.Id);
        _steps = AllSteps.ToFrozenDictionary(step => step.Event.Id);
        _relationsOf = Relations
            .SelectMany(relation => relation.From == relation.To
                ? [(Entity: relation.From, Relation: relation)]
                : new[] { (Entity: relation.From, Relation: relation), (Entity: relation.To, Relation: relation) })
            .ToLookup(entry => entry.Entity, entry => entry.Relation);

        // A thread is followed as far as the visitor may see its stops: it ends before the first stop that is not
        // reached, or that is an item the audience is not shown. A thread whose first stop cannot be seen is none.
        Threads =
        [
            .. catalogue.Threads
                .Where(thread => thread.Level.IsWithin(boundary))
                .Select(thread =>
                {
                    var steps = thread.Stops
                        .Select((stop, index) => new ThreadStep(
                            index + 1,
                            stop,
                            stop.Entity is { } entity ? _entities.GetValueOrDefault(entity) : null,
                            stop.Event is { } storyEvent ? _steps.GetValueOrDefault(storyEvent) : null,
                            stop.Media is { } item ? _media.GetValueOrDefault(item) : null))
                        .TakeWhile(step => step.Entity is not null || step.Step is not null || step.Media is not null)
                        .ToImmutableArray();
                    // The thread a view carries has the stops that are shown and no other: what is not
                    // reached is not handed out, not even unread (the rule of this class).
                    var shown = new DiscoveryThread
                    {
                        Id = thread.Id,
                        Label = thread.Label,
                        Summary = thread.Summary,
                        Level = thread.Level,
                        Stops = [.. thread.Stops.Take(steps.Length)],
                    };
                    return new ThreadView(shown, steps, steps.Length < thread.Stops.Length);
                })
                .Where(view => !view.Steps.IsEmpty),
        ];
    }

    /// <summary>Gets the threads the editors laid, each as far as the visitor may follow it.</summary>
    public ImmutableArray<ThreadView> Threads { get; }

    /// <summary>Returns a thread as far as the visitor may follow it, or <see langword="null"/>.</summary>
    public ThreadView? Thread(string id) => Threads.FirstOrDefault(view => view.Thread.Id == id);

    /// <summary>
    /// Returns where the threads lead on from an entity: each thread that has a stop at it, with the stop that
    /// follows when the visitor may see it.
    /// </summary>
    public ImmutableArray<ThreadOnward> Onward(EntityId entity) =>
    [
        .. Threads.SelectMany(view => view.Steps
            .Where(step => step.Stop.Entity == entity)
            .Select(step => new ThreadOnward(view.Thread, step, step.Number < view.Steps.Length ? view.Steps[step.Number] : null))),
    ];

    /// <summary>Gets the stage the visitor has read to.</summary>
    public ReadingLevel Boundary { get; }

    /// <summary>Gets a value indicating whether a later reading stage exists. It says nothing about what follows.</summary>
    public bool StoryContinues => Boundary.CompareTo(_info.ReadingStages[^1].Level) < 0;

    /// <summary>Gets the visible entities, in the order of the collection.</summary>
    public ImmutableArray<Entity> Entities { get; }

    /// <summary>Gets the visible relations, in the order of the collection.</summary>
    public ImmutableArray<Relation> Relations { get; }

    /// <summary>Gets the visible events of the story, in the order of the telling.</summary>
    public ImmutableArray<StoryStep> Steps { get; }

    /// <summary>Gets the visible stories the collection tells, the one a visitor enters first at the beginning.</summary>
    public ImmutableArray<EntityId> Stories { get; }

    /// <summary>Gets the visible events of all stories, story by story, each in the order of its telling.</summary>
    public ImmutableArray<StoryStep> AllSteps { get; }

    /// <summary>Gets the sources that support something visible, in the order of the collection.</summary>
    public ImmutableArray<Source> Sources { get; }

    /// <summary>Gets the media that may be shown, in the order of the collection.</summary>
    public ImmutableArray<MediaItem> Media { get; }

    /// <summary>
    /// Gets the items the visitor may know of and may not be shown: references to the page of the institution
    /// that holds them. They are found by a gallery only – never by <see cref="Find(MediaId)"/>.
    /// </summary>
    public ImmutableArray<MediaReference> References { get; }

    /// <summary>
    /// Returns what a visitor of the audience may see at a reading boundary, or <see langword="null"/> when the
    /// collection is not released to that audience at all. A boundary beyond the last stage is the last stage.
    /// </summary>
    /// <param name="catalogue">The collection.</param>
    /// <param name="boundary">The stage the visitor has read to.</param>
    /// <param name="audience">Who the collection is shown to.</param>
    /// <returns>The projection, or <see langword="null"/>.</returns>
    public static Projection? Of(Catalogue catalogue, ReadingLevel boundary, Audience audience)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        var publication = catalogue.Info.Publication;
        var released = audience switch
        {
            Audience.LocalDemonstration => publication.LocalDemoAllowed,
            Audience.Public => publication.PublicReleaseApproved,
            _ => false,
        };
        if (!released)
        {
            return null;
        }

        var last = catalogue.Info.ReadingStages[^1].Level;
        return new Projection(catalogue, boundary.CompareTo(last) > 0 ? last : boundary, audience);
    }

    /// <summary>Returns the visible entity, or <see langword="null"/>.</summary>
    public Entity? Find(EntityId id) => _entities.GetValueOrDefault(id);

    /// <summary>Returns the visible relation, or <see langword="null"/>.</summary>
    public Relation? Find(RelationId id) => _relations.GetValueOrDefault(id);

    /// <summary>Returns the visible event of the story, or <see langword="null"/>.</summary>
    public StoryStep? Find(EventId id) => _steps.GetValueOrDefault(id);

    /// <summary>Returns the media item that may be shown, or <see langword="null"/>.</summary>
    public MediaItem? Find(MediaId id) => _media.GetValueOrDefault(id);

    /// <summary>Returns the entry of the collection.</summary>
    public CollectionOverview Overview()
    {
        var first = FirstAppearances();
        ImmutableArray<Entity> subjects =
        [
            .. Entities
                .Where(entity => entity.Type.Nature is EntityNature.Being or EntityNature.Group)
                .OrderBy(entity => first.GetValueOrDefault(entity.Id, int.MaxValue)),
        ];
        return new CollectionOverview(
            _entities[_info.Subject],
            _info.Edition is { } edition ? _entities[edition.Work] : null,
            _info.Edition,
            _info.ReadingStages,
            Boundary,
            StoryContinues,
            subjects,
            Steps.IsEmpty ? null : Steps[0])
        {
            Stories = [.. Stories.Select(story => _entities[story])],
        };
    }

    /// <summary>
    /// Returns the profile of a node as this visitor may see it (ADR 0017), or <see langword="null"/> for a
    /// node the visitor may not see. A fact is told when its stage is reached and – where it names another
    /// node – that node may be seen; an essential fact that is not told is said to be unknown, whether nobody
    /// stated it or the story has not come to it, so that a profile tells nothing of what lies ahead. The
    /// picture is there only when the visitor may see that media item.
    /// </summary>
    public NodeProfile? Profile(EntityId id)
    {
        if (Find(id) is not { } entity)
        {
            return null;
        }

        var facts = ProfileFacts.Of(entity.Type);
        var told = entity.Facts.Where(fact => fact.Node is not { } node || _entities.ContainsKey(node)).ToLookup(fact => fact.Kind.Key, StringComparer.Ordinal);
        return new NodeProfile(
            entity,
            entity.Picture is { } picture ? Find(picture) : null,
            [.. facts.Essential.Select(kind => new ProfileLine(kind, [.. told[kind.Key]]))],
            [.. facts.Optional.Where(kind => told.Contains(kind.Key)).Select(kind => new ProfileLine(kind, [.. told[kind.Key]]))]);
    }

    /// <summary>Returns an entity with what a visitor may follow from it, or <see langword="null"/>.</summary>
    public EntityOverview? Entity(EntityId id)
    {
        if (Find(id) is not { } entity)
        {
            return null;
        }

        return new EntityOverview(
            entity,
            [.. _relationsOf[id].Select(relation => Connect(relation, id))],
            [
                .. AllSteps.SelectMany(step => step.Event.Associations
                    .Where(association => association.Entity == id)
                    .Select(association => new Appearance(step, association))),
            ],
            [.. Media.Where(item => item.DepictedEntities.Contains(id))]);
    }

    /// <summary>
    /// Returns the neighbourhood of an entity, or <see langword="null"/>. Expanding a node adds its relations; a
    /// node that is not connected to what is already open is ignored, so an address cannot open an unrelated island.
    /// </summary>
    /// <param name="focus">The entity in the middle.</param>
    /// <param name="expanded">The nodes whose relations are shown as well.</param>
    public ConnectionsView? Connections(EntityId focus, IEnumerable<EntityId>? expanded = null)
    {
        if (Find(focus) is not { } entity)
        {
            return null;
        }

        var open = new HashSet<EntityId> { focus };
        var requested = (expanded ?? []).Where(_entities.ContainsKey).Distinct().ToList();
        for (var grown = true; grown;)
        {
            grown = false;
            foreach (var id in requested)
            {
                if (!open.Contains(id) && _relationsOf[id].Any(relation => open.Contains(relation.From) || open.Contains(relation.To)))
                {
                    open.Add(id);
                    grown = true;
                }
            }
        }

        bool Shown(Relation relation) => open.Contains(relation.From) || open.Contains(relation.To);

        ImmutableArray<Edge> edges = [.. Relations.Where(Shown).Select(ToEdge)];
        var reached = edges.SelectMany(edge => new[] { edge.From.Id, edge.To.Id }).ToHashSet();
        ImmutableArray<Entity> nodes = [entity, .. Entities.Where(node => node.Id != focus && reached.Contains(node.Id))];
        return new ConnectionsView(
            entity,
            nodes,
            edges,
            [.. nodes.Where(node => node.Id != focus && open.Contains(node.Id))],
            [.. nodes.Where(node => !open.Contains(node.Id) && _relationsOf[node.Id].Any(relation => !Shown(relation)))]);
    }

    /// <summary>Returns the family relations of the collection.</summary>
    public FamilyView Family() => Family(null)!;

    /// <summary>
    /// Returns the family an entity belongs to – everybody connected to it by parentage or partnership – or
    /// <see langword="null"/>. An entity without such a relation has an empty family.
    /// </summary>
    public FamilyView? Family(EntityId around) => Find(around) is null ? null : Family((EntityId?)around);

    /// <summary>
    /// Returns the family tree around an entity, or <see langword="null"/>: its ancestors up to a number of
    /// generations, its descendants down to a number of generations, and the partners of all of them. Whether the
    /// tree goes on beyond what was asked for is part of the answer, so that a page can offer to open it further.
    /// </summary>
    /// <param name="around">The entity the tree is about.</param>
    /// <param name="up">How many generations of ancestors; zero for none.</param>
    /// <param name="down">How many generations of descendants; zero for none.</param>
    /// <param name="beside">
    /// How far the tree is opened beside the line: 1 adds the other children of the ancestors shown (brothers and
    /// sisters, uncles and aunts), 2 their children as well, and so on; zero for the line alone.
    /// </param>
    public FamilyView? Family(EntityId around, int up, int down, int beside = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(up);
        ArgumentOutOfRangeException.ThrowIfNegative(down);
        ArgumentOutOfRangeException.ThrowIfNegative(beside);
        if (Find(around) is null)
        {
            return null;
        }

        var parentage = Relations.Where(relation => relation.Type.Kinship == Kinship.Parentage).ToList();
        var partnerships = Relations.Where(relation => relation.Type.Kinship == Kinship.Partnership).ToList();

        // The line of the entity: how many generations above it (negative) or below it somebody stands. Whoever
        // is reached twice keeps the first place, so relations that run in a circle end the walk.
        var line = new Dictionary<EntityId, int> { [around] = 0 };
        Walk(up, -1, relation => (relation.To, relation.From));
        Walk(down, 1, relation => (relation.From, relation.To));

        var moreAbove = parentage.Any(relation => !line.ContainsKey(relation.From) && line.TryGetValue(relation.To, out var place) && place == -up);
        var moreBelow = parentage.Any(relation => !line.ContainsKey(relation.To) && line.TryGetValue(relation.From, out var place) && place == down);

        // Beside the line: the other children of the ancestors shown, step by step. How many steps somebody is
        // away from the line is kept, so that the tree can say whether it goes on there.
        var away = new Dictionary<EntityId, int>();
        var reached = line.Where(entry => entry.Value < 0).Select(entry => entry.Key).ToList();
        for (var step = 1; step <= beside && reached.Count > 0; step++)
        {
            var next = new List<EntityId>();
            foreach (var relation in parentage)
            {
                if (reached.Contains(relation.From) && !line.ContainsKey(relation.To) && away.TryAdd(relation.To, step))
                {
                    next.Add(relation.To);
                }
            }

            reached = next;
        }

        var moreBeside = parentage.Any(relation =>
            !line.ContainsKey(relation.To)
            && !away.ContainsKey(relation.To)
            && (beside == 0 ? line.TryGetValue(relation.From, out var place) && place < 0 : away.TryGetValue(relation.From, out var steps) && steps == beside));

        // Partners of everybody shown belong to the tree; their own parents and other children do not.
        var members = line.Keys.Concat(away.Keys).ToHashSet();
        foreach (var relation in partnerships)
        {
            if (line.ContainsKey(relation.From) || line.ContainsKey(relation.To) || away.ContainsKey(relation.From) || away.ContainsKey(relation.To))
            {
                members.Add(relation.From);
                members.Add(relation.To);
            }
        }

        parentage.RemoveAll(relation => !members.Contains(relation.From) || !members.Contains(relation.To));
        partnerships.RemoveAll(relation => !members.Contains(relation.From) || !members.Contains(relation.To));
        return Generations(parentage, partnerships, moreAbove, moreBelow, around, moreBeside);

        void Walk(int generations, int step, Func<Relation, (EntityId Near, EntityId Far)> ends)
        {
            var edge = new List<EntityId> { around };
            for (var generation = 1; generation <= generations && edge.Count > 0; generation++)
            {
                var next = new List<EntityId>();
                foreach (var relation in parentage)
                {
                    var (near, far) = ends(relation);
                    if (edge.Contains(near) && line.TryAdd(far, generation * step))
                    {
                        next.Add(far);
                    }
                }

                edge = next;
            }
        }
    }

    /// <summary>
    /// Returns the events of a story in the order of the telling – of the story a visitor enters first when none
    /// is named – or <see langword="null"/> when the collection tells no such story.
    /// </summary>
    public StoryView? Story(EntityId? story = null)
    {
        var id = story ?? (Stories.IsEmpty ? null : Stories[0]);
        return id is { } known && _stepsOf.TryGetValue(known, out var steps) ? new(_entities[known], steps, StoryContinues) : null;
    }

    /// <summary>Returns a story as rows of entities over the steps of <see cref="Story"/>, or <see langword="null"/>.</summary>
    public LanesView? Lanes(EntityId? story = null)
    {
        if (Story(story) is not { Steps: var steps })
        {
            return null;
        }

        return Lanes(steps);
    }

    private LanesView Lanes(ImmutableArray<StoryStep> Steps)
    {
        var order = new List<EntityId>();
        var seen = new HashSet<EntityId>();
        foreach (var association in Steps.SelectMany(step => step.Event.Associations))
        {
            if (seen.Add(association.Entity))
            {
                order.Add(association.Entity);
            }
        }

        return new LanesView(
            Steps,
            [
                .. order.Select(id => new Lane(
                    _entities[id],
                    [.. Steps.Select(step => step.Event.Associations.FirstOrDefault(association => association.Entity == id))])),
            ]);
    }

    /// <summary>
    /// Returns what the collection has with a time of the calendar: the entities something dated is stated about,
    /// and the media, each of which was made at some time. The story itself is not in it – it has an order, no dates.
    /// </summary>
    public TimelineView Timeline()
    {
        // A relation with a time stands in the lane of the entity it reads from: "played for" in the lane of the player.
        ImmutableArray<Edge> held = [.. Relations.Where(relation => relation.Time is not null).Select(ToEdge)];
        var holders = held.Select(edge => edge.From.Id).ToHashSet();
        return new([.. Entities.Where(entity => entity.Dated.Any(dated => dated.Time is not null) || holders.Contains(entity.Id))], Media, held);
    }

    /// <summary>
    /// Returns the years the collection says something about – through a relation that states when it held, or a
    /// statement about an entity with a time – or <see langword="null"/> when nothing the visitor may see can be
    /// placed among the years.
    /// </summary>
    public YearsCovered? Years()
    {
        var reaches = Relations.Select(relation => relation.Time)
            .Concat(Entities.SelectMany(entity => entity.Dated.Select(dated => dated.Time)))
            .Select(YearCover.Reach).OfType<YearSpan>().ToList();
        if (reaches.Count == 0)
        {
            return null;
        }

        // The year a stretch ends in is the one just before its end: the end is the beginning of what follows.
        const decimal Just = 0.0001m;
        var turns = reaches.SelectMany(reach => new[] { YearOfEra.At(reach.From), YearOfEra.At(reach.To - Just) }).Distinct().OrderBy(year => year.Place).ToImmutableArray();
        return new(turns[0], turns[^1], turns);
    }

    /// <summary>
    /// Returns what the collection says of one year: the relations of the world that held or may have held then,
    /// and what is stated of its entities for that year.
    /// </summary>
    public MomentView At(YearOfEra year)
    {
        var turns = Years()?.Turns ?? [];
        return new(
            year,
            [.. Relations.Select(relation => (Relation: relation, Holding: YearCover.Of(relation.Time, year)))
                .Where(held => held.Holding != Holding.No).Select(held => new HeldThen(ToEdge(held.Relation), held.Holding))],
            [.. Entities.SelectMany(entity => entity.Dated.Select(dated => new StatedThen(entity, dated, YearCover.Of(dated.Time, year))))
                .Where(stated => stated.Holding != Holding.No)],
            turns.Where(turn => turn.Place < year.Place).Select(turn => (YearOfEra?)turn).LastOrDefault(),
            turns.Where(turn => turn.Place > year.Place).Select(turn => (YearOfEra?)turn).FirstOrDefault());
    }

    /// <summary>
    /// Returns the real places of the collection: those that say where they are, each with what is stated to be
    /// there, and those that do not. A place of a story is not among them – it is no place on a map.
    /// </summary>
    public PlacesView Places()
    {
        var real = Entities.Where(entity => entity.Type.Nature == EntityNature.Place
            && entity.Type != EntityTypes.NarrativePlace && entity.Type != EntityTypes.NarrativeRegion).ToList();
        var here = Relations.ToLookup(relation => relation.To);
        return new(
            [.. real.Where(place => place.Position is not null).Select(place => new PlaceOnMap(place, place.Position!, [.. here[place.Id].Select(ToEdge)]))],
            [.. real.Where(place => place.Position is null)]);
    }

    /// <summary>
    /// Returns the published revisions of the collection, the newest first, each with the changes the visitor may
    /// know of: a change of a record of a later reading stage is not named, and not counted either. Empty for a
    /// collection that is no published revision.
    /// </summary>
    public ImmutableArray<RevisionEntry> Revisions() =>
    [
        .. _info.History.Reverse().Select(entry => entry with { Changes = [.. entry.Changes.Where(change => change.Level.IsWithin(Boundary))] }),
    ];

    /// <summary>Returns every media item that may be shown.</summary>
    public GalleryView Gallery() => new(GalleryRule.WholeCollection, null, null, GalleryOrder.Editorial, Media) { References = References };

    /// <summary>Returns the media that depict an entity, or <see langword="null"/>.</summary>
    public GalleryView? Gallery(EntityId entity) => Find(entity) is not { } found
        ? null
        : new GalleryView(
            GalleryRule.DepictsEntity,
            found,
            null,
            GalleryOrder.Editorial,
            [.. Media.Where(item => item.DepictedEntities.Contains(entity))])
        {
            References = [.. References.Where(item => item.DepictedEntities.Contains(entity))],
        };

    /// <summary>Returns the media that depict an event, or <see langword="null"/>.</summary>
    public GalleryView? Gallery(EventId storyEvent) => Find(storyEvent) is not { } step
        ? null
        : new GalleryView(
            GalleryRule.DepictsEvent,
            null,
            step,
            GalleryOrder.Editorial,
            [.. Media.Where(item => item.DepictedEvents.Contains(storyEvent))])
        {
            References = [.. References.Where(item => item.DepictedEvents.Contains(storyEvent))],
        };

    /// <summary>Returns a media item with what it shows, or <see langword="null"/>.</summary>
    public MediaView? MediaItem(MediaId id) => Find(id) is not { } item
        ? null
        : new MediaView(
            item,
            [.. item.DepictedEntities.Select(entity => _entities[entity])],
            [.. item.DepictedEvents.Select(storyEvent => _steps.GetValueOrDefault(storyEvent)).OfType<StoryStep>()],
            [.. item.RelatedRelations.Select(relation => ToEdge(_relations[relation]))]);

    /// <summary>Returns the sources of an entity, or <see langword="null"/>.</summary>
    public EvidenceView? Evidence(EntityId id) => Find(id) is not { } entity
        ? null
        : Evidence(EvidenceSubject.Entity, entity.Sources) with { Entity = entity };

    /// <summary>Returns the sources of a relation, or <see langword="null"/>.</summary>
    public EvidenceView? Evidence(RelationId id) => Find(id) is not { } relation
        ? null
        : Evidence(EvidenceSubject.Relation, relation.Sources) with
        {
            Relation = ToEdge(relation),
            PassageLocator = relation.PassageLocator,
            Review = relation.Review,
            Dispute = Dispute(relation.Review, relation.Reviewed),
        };

    /// <summary>Returns the sources of an event, or <see langword="null"/>.</summary>
    public EvidenceView? Evidence(EventId id) => Find(id) is not { } step
        ? null
        : Evidence(EvidenceSubject.Event, step.Event.Sources) with
        {
            Step = step,
            Review = step.Event.Review,
            Dispute = Dispute(step.Event.Review, step.Event.Reviewed),
        };

    /// <summary>Returns the sources for what an entity has to do with an event, or <see langword="null"/>.</summary>
    public EvidenceView? Evidence(EventId storyEvent, EntityId entity)
    {
        if (Find(storyEvent) is not { } step
            || step.Event.Associations.FirstOrDefault(association => association.Entity == entity) is not { } association)
        {
            return null;
        }

        return Evidence(EvidenceSubject.Association, step.Event.Sources) with
        {
            Step = step,
            Entity = _entities[entity],
            Association = association,
            Review = step.Event.Review,
            Dispute = Dispute(step.Event.Review, step.Event.Reviewed),
        };
    }

    // What a review says about a dispute is said to the visitor; who reviewed is the editors' record.
    private static string? Dispute(ReviewStatus review, ReviewRecord? record) =>
        review == ReviewStatus.Disputed ? record?.Note : null;

    /// <summary>Returns the sources of a media item, or <see langword="null"/>.</summary>
    public EvidenceView? Evidence(MediaId id) => Find(id) is not { } item
        ? null
        : Evidence(EvidenceSubject.Media, item.Sources) with { Media = item };

    /// <summary>
    /// Finds entities by a name in any of their languages. Case, accents, spelling variants of a letter and the
    /// shape of digits make no difference. A name that matches from its beginning comes first.
    /// </summary>
    /// <param name="text">What the visitor typed.</param>
    /// <param name="limit">The largest number of hits.</param>
    public ImmutableArray<SearchHit> Search(string? text, int limit = 10) => Search(text, new SearchFilter(), limit);

    /// <summary>
    /// Finds entities by a name and by what they are: of a kind, connected to a place, taking part in something
    /// that held in a year. Without a name, everything the filter lets through is found, in the order of the
    /// collection; without a name and without a filter, nothing is.
    /// </summary>
    /// <param name="text">What the visitor typed.</param>
    /// <param name="filter">What the hits have to be.</param>
    /// <param name="limit">The largest number of hits.</param>
    public ImmutableArray<SearchHit> Search(string? text, SearchFilter filter, int limit = 10)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var query = SearchText.Fold(text);
        if ((query.Length == 0 && filter.IsEmpty) || limit <= 0)
        {
            return [];
        }

        // Who is connected to the place, and who takes part in what held in the year – asked once, not per entity.
        var atPlace = filter.Place is { } place
            ? Relations.Where(relation => relation.From == place || relation.To == place).SelectMany(relation => new[] { relation.From, relation.To }).Append(place).ToHashSet()
            : null;
        HashSet<EntityId>? inYear = null;
        if (filter.Year is { } year)
        {
            var moment = At(year);
            inYear = moment.Relations.SelectMany(held => new[] { held.Edge.From.Id, held.Edge.To.Id }).Concat(moment.Stated.Select(stated => stated.Entity.Id)).ToHashSet();
        }

        bool Passes(Entity entity) =>
            (filter.Nature is not { } nature || entity.Type.Nature == nature)
            && (atPlace is null || atPlace.Contains(entity.Id))
            && (inYear is null || inYear.Contains(entity.Id));

        if (query.Length == 0)
        {
            // No name was typed: the hit carries no name that matched.
            return [.. Entities.Where(Passes).Take(limit).Select(entity => new SearchHit(entity, string.Empty, string.Empty))];
        }

        var hits = new List<(int Rank, SearchHit Hit)>();
        foreach (var entity in Entities.Where(Passes))
        {
            (int Rank, SearchHit Hit)? best = null;
            foreach (var language in entity.Label.Languages.Order(StringComparer.Ordinal))
            {
                var name = entity.Label.In(language)!.Value.Value;
                var rank = Rank(SearchText.Fold(name), query);
                if (rank >= 0 && (best is null || rank < best.Value.Rank))
                {
                    best = (rank, new SearchHit(entity, name, language));
                }
            }

            // The other names of the entity find it too: an earlier name, an abbreviation, the name in its own script.
            foreach (var other in entity.Names)
            {
                var rank = Rank(SearchText.Fold(other.Name), query);
                if (rank >= 0 && (best is null || rank < best.Value.Rank))
                {
                    best = (rank, new SearchHit(entity, other.Name, other.Language));
                }
            }

            if (best is not null)
            {
                hits.Add(best.Value);
            }
        }

        return [.. hits.OrderBy(hit => hit.Rank).Take(limit).Select(hit => hit.Hit)];
    }

    // An item is shown when its eligibility says so. The model knows the eligibility for a demonstration only;
    // until it knows one for a public release, the public sees no media rather than media nobody cleared.
    private static bool MayBeShown(MediaItem item, Audience audience) => audience switch
    {
        Audience.LocalDemonstration => item.DemoRenderAllowed,
        Audience.Public => item.PublicRenderAllowed,
        _ => false,
    };

    private static int Rank(string name, string query)
    {
        if (name == query)
        {
            return 0;
        }

        if (name.StartsWith(query, StringComparison.Ordinal))
        {
            return 1;
        }

        if (name.Contains(' ' + query, StringComparison.Ordinal))
        {
            return 2;
        }

        return name.Contains(query, StringComparison.Ordinal) ? 3 : -1;
    }

    private EvidenceView Evidence(EvidenceSubject subject, ImmutableArray<SourceId> sources) =>
        new(subject, [.. sources.Select(source => _sources[source])], _info.Edition, StoryContinues);

    private Edge ToEdge(Relation relation) => new(relation, _entities[relation.From], _entities[relation.To]);

    private Connection Connect(Relation relation, EntityId self)
    {
        var direction = relation.Type.Directionality == Directionality.Symmetric
            ? ConnectionDirection.Mutual
            : relation.From == self ? ConnectionDirection.Outgoing : ConnectionDirection.Incoming;
        return new Connection(relation, _entities[relation.From == self ? relation.To : relation.From], direction);
    }

    private Dictionary<EntityId, int> FirstAppearances()
    {
        var first = new Dictionary<EntityId, int>();
        foreach (var step in AllSteps)
        {
            foreach (var association in step.Event.Associations)
            {
                // The count so far is the place in the order of first appearances, within an event too.
                first.TryAdd(association.Entity, first.Count);
            }
        }

        return first;
    }

    private FamilyView? Family(EntityId? around)
    {
        var parentage = Relations.Where(relation => relation.Type.Kinship == Kinship.Parentage).ToList();
        var partnerships = Relations.Where(relation => relation.Type.Kinship == Kinship.Partnership).ToList();
        if (around is { } centre)
        {
            // Everybody reachable from the entity over family relations, in either direction.
            var family = new HashSet<EntityId> { centre };
            for (var grown = true; grown;)
            {
                grown = false;
                foreach (var relation in parentage.Concat(partnerships))
                {
                    if (family.Contains(relation.From) != family.Contains(relation.To))
                    {
                        family.Add(relation.From);
                        family.Add(relation.To);
                        grown = true;
                    }
                }
            }

            parentage.RemoveAll(relation => !family.Contains(relation.From));
            partnerships.RemoveAll(relation => !family.Contains(relation.From));
        }

        return Generations(parentage, partnerships);
    }

    private FamilyView Generations(List<Relation> parentage, List<Relation> partnerships, bool moreAbove = false, bool moreBelow = false, EntityId? alone = null, bool moreBeside = false)
    {
        // A child stands one row below its lowest parent, partners stand in one row. The number of rounds is
        // bounded, so relations that contradict each other (a child as the parent of its parent) end the loop too.
        var generation = parentage.Concat(partnerships)
            .SelectMany(relation => new[] { relation.From, relation.To })
            .Distinct()
            .ToDictionary(id => id, _ => 0);

        // Somebody whose family lies wholly outside what was asked for is still the one the tree is about.
        if (alone is { } only && (moreAbove || moreBelow))
        {
            generation.TryAdd(only, 0);
        }

        for (var round = 0; round <= generation.Count; round++)
        {
            var changed = false;
            foreach (var relation in parentage)
            {
                if (generation[relation.To] < generation[relation.From] + 1)
                {
                    generation[relation.To] = generation[relation.From] + 1;
                    changed = true;
                }
            }

            foreach (var relation in partnerships)
            {
                var row = Math.Max(generation[relation.From], generation[relation.To]);
                if (generation[relation.From] != row || generation[relation.To] != row)
                {
                    generation[relation.From] = row;
                    generation[relation.To] = row;
                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }

        return new FamilyView(
            [
                .. Entities
                    .Where(entity => generation.ContainsKey(entity.Id))
                    .Select(entity => new FamilyMember(entity, generation[entity.Id]))
                    .OrderBy(member => member.Generation),
            ],
            [.. parentage.Select(ToEdge)],
            [.. partnerships.Select(ToEdge)],
            moreAbove,
            moreBelow,
            moreBeside);
    }
}
