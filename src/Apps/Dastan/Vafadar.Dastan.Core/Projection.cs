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
    private readonly ILookup<EntityId, Relation> _relationsOf;

    private Projection(Catalogue catalogue, ReadingLevel boundary, Audience audience)
    {
        _info = catalogue.Info;
        Boundary = boundary;
        Entities = [.. catalogue.Entities.Where(entity => entity.Level.IsWithin(boundary))];
        Relations = [.. catalogue.Relations.Where(relation => relation.Level.IsWithin(boundary))];
        Media = [.. catalogue.Media.Where(item => item.Level.IsWithin(boundary) && MayBeShown(item, audience))];

        // The steps of the story: the visible events that have a place in the telling of this story, in that order.
        var story = _info.Story.Value;
        Steps =
        [
            .. catalogue.Events
                .Where(storyEvent => storyEvent.Level.IsWithin(boundary))
                .Select(storyEvent => (Event: storyEvent, Position: (storyEvent.Time as Occurrence)?.At as NarrativePosition))
                .Where(entry => entry.Position is { Order: NarrativeOrder.Presentation } position && position.Sequence == story)
                .OrderBy(entry => entry.Position!.Ordinal)
                .Select((entry, index) => new StoryStep(index + 1, entry.Event)),
        ];

        // Only the sources that support something visible are named: a source of a hidden record or of an item that
        // may not be shown would tell that the record exists.
        var cited = Entities.SelectMany(entity => entity.Sources)
            .Concat(Relations.SelectMany(relation => relation.Sources))
            .Concat(Steps.SelectMany(step => step.Event.Sources))
            .Concat(Media.SelectMany(item => item.Sources))
            .ToHashSet();
        Sources = [.. catalogue.Sources.Where(source => cited.Contains(source.Id))];

        _entities = Entities.ToFrozenDictionary(entity => entity.Id);
        _relations = Relations.ToFrozenDictionary(relation => relation.Id);
        _sources = Sources.ToFrozenDictionary(source => source.Id);
        _media = Media.ToFrozenDictionary(item => item.Id);
        _steps = Steps.ToFrozenDictionary(step => step.Event.Id);
        _relationsOf = Relations
            .SelectMany(relation => relation.From == relation.To
                ? [(Entity: relation.From, Relation: relation)]
                : new[] { (Entity: relation.From, Relation: relation), (Entity: relation.To, Relation: relation) })
            .ToLookup(entry => entry.Entity, entry => entry.Relation);
    }

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

    /// <summary>Gets the sources that support something visible, in the order of the collection.</summary>
    public ImmutableArray<Source> Sources { get; }

    /// <summary>Gets the media that may be shown, in the order of the collection.</summary>
    public ImmutableArray<MediaItem> Media { get; }

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
            _entities[_info.Story],
            _entities[_info.Edition.Work],
            _info.Edition,
            _info.ReadingStages,
            Boundary,
            StoryContinues,
            subjects,
            Steps.IsEmpty ? null : Steps[0]);
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
                .. Steps.SelectMany(step => step.Event.Associations
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
            [.. nodes.Where(node => !open.Contains(node.Id) && _relationsOf[node.Id].Any(relation => !Shown(relation)))]);
    }

    /// <summary>Returns the family relations of the collection.</summary>
    public FamilyView Family() => Family(null)!;

    /// <summary>
    /// Returns the family an entity belongs to – everybody connected to it by parentage or partnership – or
    /// <see langword="null"/>. An entity without such a relation has an empty family.
    /// </summary>
    public FamilyView? Family(EntityId around) => Find(around) is null ? null : Family((EntityId?)around);

    /// <summary>Returns the events of the story in the order of the telling.</summary>
    public StoryView Story() => new(_entities[_info.Story], Steps, StoryContinues);

    /// <summary>Returns the story as rows of entities over the steps of <see cref="Story"/>.</summary>
    public LanesView Lanes()
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

    /// <summary>Returns every media item that may be shown.</summary>
    public GalleryView Gallery() => new(GalleryRule.WholeCollection, null, null, GalleryOrder.Editorial, Media);

    /// <summary>Returns the media that depict an entity, or <see langword="null"/>.</summary>
    public GalleryView? Gallery(EntityId entity) => Find(entity) is not { } found
        ? null
        : new GalleryView(
            GalleryRule.DepictsEntity,
            found,
            null,
            GalleryOrder.Editorial,
            [.. Media.Where(item => item.DepictedEntities.Contains(entity))]);

    /// <summary>Returns the media that depict an event, or <see langword="null"/>.</summary>
    public GalleryView? Gallery(EventId storyEvent) => Find(storyEvent) is not { } step
        ? null
        : new GalleryView(
            GalleryRule.DepictsEvent,
            null,
            step,
            GalleryOrder.Editorial,
            [.. Media.Where(item => item.DepictedEvents.Contains(storyEvent))]);

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
        };

    /// <summary>Returns the sources of an event, or <see langword="null"/>.</summary>
    public EvidenceView? Evidence(EventId id) => Find(id) is not { } step
        ? null
        : Evidence(EvidenceSubject.Event, step.Event.Sources) with { Step = step, Review = step.Event.Review };

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
        };
    }

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
    public ImmutableArray<SearchHit> Search(string? text, int limit = 10)
    {
        var query = SearchText.Fold(text);
        if (query.Length == 0 || limit <= 0)
        {
            return [];
        }

        var hits = new List<(int Rank, SearchHit Hit)>();
        foreach (var entity in Entities)
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

            if (best is not null)
            {
                hits.Add(best.Value);
            }
        }

        return [.. hits.OrderBy(hit => hit.Rank).Take(limit).Select(hit => hit.Hit)];
    }

    // An item is shown when its eligibility says so. The model knows the eligibility for a demonstration only;
    // until it knows one for a public release, the public sees no media rather than media nobody cleared.
    private static bool MayBeShown(MediaItem item, Audience audience) =>
        audience == Audience.LocalDemonstration && item.DemoRenderAllowed;

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
        foreach (var step in Steps)
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

        // A child stands one row below its lowest parent, partners stand in one row. The number of rounds is
        // bounded, so relations that contradict each other (a child as the parent of its parent) end the loop too.
        var generation = parentage.Concat(partnerships)
            .SelectMany(relation => new[] { relation.From, relation.To })
            .Distinct()
            .ToDictionary(id => id, _ => 0);
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
            [.. partnerships.Select(ToEdge)]);
    }
}
