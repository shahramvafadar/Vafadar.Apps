using System.Collections.Frozen;
using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core;

/// <summary>The text witness the statements of a collection follow, and what is not established about it.</summary>
public sealed class EditionContext
{
    /// <summary>Gets the identifier of the edition context.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the work the edition is of.</summary>
    public required EntityId Work { get; init; }

    /// <summary>Gets what text was used, in the editors' words.</summary>
    public required string BaseText { get; init; }

    /// <summary>Gets the address of that text.</summary>
    public required Uri BaseUrl { get; init; }

    /// <summary>Gets what is known about the critical print edition behind the text – including that it is not known.</summary>
    public required string CriticalEditionNote { get; init; }

    /// <summary>Gets what part of the work the collection covers.</summary>
    public required string Scope { get; init; }

    /// <summary>Gets the language the editors wrote the summaries in, as a lower-case language tag.</summary>
    public required string SummaryLanguage { get; init; }
}

/// <summary>A published revision of a collection: which one it is and when it was published.</summary>
/// <param name="Number">The number of the revision, counted from 1.</param>
/// <param name="PublishedOn">The day the revision was published.</param>
public sealed record PublicationRevision(int Number, DateOnly PublishedOn);

/// <summary>What the editors allow to be done with a collection. Each permission is its own decision.</summary>
/// <param name="LocalDemoAllowed">Whether the collection may be shown in a local demonstration.</param>
/// <param name="PublicReleaseApproved">Whether the collection is approved for publication.</param>
/// <param name="Revision">
/// The published revision the collection is, or <see langword="null"/> for an editorial sample. Only a published
/// revision can be approved for publication, and it holds nothing but reviewed statements.
/// </param>
public sealed record PublicationState(bool LocalDemoAllowed, bool PublicReleaseApproved, PublicationRevision? Revision = null);

/// <summary>What a collection is as a whole.</summary>
public sealed class CatalogueInfo
{
    /// <summary>Gets the identifier of the collection.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the identifier of the story world the collection belongs to.</summary>
    public required string WorldId { get; init; }

    /// <summary>Gets the entity that stands for the collection itself – the story a visitor enters.</summary>
    public required EntityId Story { get; init; }

    /// <summary>Gets the text witness the statements follow.</summary>
    public required EditionContext Edition { get; init; }

    /// <summary>Gets the stages a visitor can choose as their reading boundary, from the opening on, without gaps.</summary>
    public required ImmutableArray<ReadingStage> ReadingStages { get; init; }

    /// <summary>Gets the boundary a visitor starts with.</summary>
    public required ReadingLevel DefaultLevel { get; init; }

    /// <summary>Gets what the editors allow to be done with the collection.</summary>
    public required PublicationState Publication { get; init; }
}

/// <summary>A collection that contradicts itself. <see cref="Problems"/> names every contradiction with its record.</summary>
public sealed class CatalogueException : Exception
{
    /// <summary>Creates the exception.</summary>
    public CatalogueException(IReadOnlyList<string> problems)
        : base($"The collection is not consistent ({problems.Count} problems). First: {problems[0]}")
    {
        Problems = problems;
    }

    /// <summary>Gets every problem that was found.</summary>
    public IReadOnlyList<string> Problems { get; }
}

/// <summary>
/// One collection as the editors hold it: complete, including everything a visitor may not see yet. It is built
/// once, checked as a whole and never changed. A visitor is never given the catalogue – only what the projection
/// makes of it for their reading boundary (ADR 0001).
/// </summary>
public sealed class Catalogue
{
    private readonly FrozenDictionary<EntityId, Entity> _entities;
    private readonly FrozenDictionary<RelationId, Relation> _relations;
    private readonly FrozenDictionary<EventId, StoryEvent> _events;
    private readonly FrozenDictionary<SourceId, Source> _sources;
    private readonly FrozenDictionary<MediaId, MediaItem> _media;

    /// <summary>Creates and checks a collection.</summary>
    /// <exception cref="CatalogueException">
    /// A record refers to something that does not exist, would give away a later record, or contradicts the meaning
    /// of its type. Nothing is skipped or repaired: a relation without its endpoint would silently change the graph.
    /// </exception>
    public Catalogue(
        CatalogueInfo info,
        IEnumerable<Entity> entities,
        IEnumerable<Relation> relations,
        IEnumerable<StoryEvent> events,
        IEnumerable<Source> sources,
        IEnumerable<MediaItem> media)
    {
        ArgumentNullException.ThrowIfNull(info);
        Info = info;
        Entities = [.. entities];
        Relations = [.. relations];
        Events = [.. events];
        Sources = [.. sources];
        Media = [.. media];

        var problems = new List<string>();
        _entities = Index(Entities, entity => entity.Id, entity => entity.Id.Value, problems);
        _relations = Index(Relations, relation => relation.Id, relation => relation.Id.Value, problems);
        _events = Index(Events, storyEvent => storyEvent.Id, storyEvent => storyEvent.Id.Value, problems);
        _sources = Index(Sources, source => source.Id, source => source.Id.Value, problems);
        _media = Index(Media, item => item.Id, item => item.Id.Value, problems);
        new Checker(this, problems).Run();
        if (problems.Count > 0)
        {
            throw new CatalogueException(problems);
        }
    }

    /// <summary>Gets what the collection is as a whole.</summary>
    public CatalogueInfo Info { get; }

    /// <summary>Gets the entities, in the order of the source.</summary>
    public ImmutableArray<Entity> Entities { get; }

    /// <summary>Gets the relations, in the order of the source.</summary>
    public ImmutableArray<Relation> Relations { get; }

    /// <summary>Gets the events, in the order of the source.</summary>
    public ImmutableArray<StoryEvent> Events { get; }

    /// <summary>Gets the sources, in the order of the source.</summary>
    public ImmutableArray<Source> Sources { get; }

    /// <summary>Gets the media items, in the order of the source.</summary>
    public ImmutableArray<MediaItem> Media { get; }

    /// <summary>Returns the entity, or <see langword="null"/> when the collection has none with this identifier.</summary>
    public Entity? Find(EntityId id) => _entities.GetValueOrDefault(id);

    /// <summary>Returns the relation, or <see langword="null"/> when the collection has none with this identifier.</summary>
    public Relation? Find(RelationId id) => _relations.GetValueOrDefault(id);

    /// <summary>Returns the event, or <see langword="null"/> when the collection has none with this identifier.</summary>
    public StoryEvent? Find(EventId id) => _events.GetValueOrDefault(id);

    /// <summary>Returns the source, or <see langword="null"/> when the collection has none with this identifier.</summary>
    public Source? Find(SourceId id) => _sources.GetValueOrDefault(id);

    /// <summary>Returns the media item, or <see langword="null"/> when the collection has none with this identifier.</summary>
    public MediaItem? Find(MediaId id) => _media.GetValueOrDefault(id);

    private static FrozenDictionary<TKey, TValue> Index<TKey, TValue>(
        ImmutableArray<TValue> records,
        Func<TValue, TKey> key,
        Func<TValue, string> name,
        List<string> problems)
        where TKey : notnull
    {
        var index = new Dictionary<TKey, TValue>();
        foreach (var record in records)
        {
            if (!index.TryAdd(key(record), record))
            {
                problems.Add($"{name(record)}: the identifier is used twice.");
            }
        }

        return index.ToFrozenDictionary();
    }

    /// <summary>The rules a collection has to keep. Each problem names its record first.</summary>
    private sealed class Checker(Catalogue catalogue, List<string> problems)
    {
        public void Run()
        {
            CheckIdentifiersAcrossKinds();
            CheckInfo();
            foreach (var entity in catalogue.Entities)
            {
                CheckSources(entity.Id.Value, entity.Sources, entity.Level);
                CheckStage(entity.Id.Value, entity.Level);
                foreach (var dated in entity.Dated)
                {
                    CheckSources(dated.Key, dated.Sources, dated.Level);
                    CheckStage(dated.Key, dated.Level);
                    if (!entity.Level.IsWithin(dated.Level))
                    {
                        problems.Add($"{dated.Key}: a statement about {entity.Id} cannot be shown before {entity.Id} itself.");
                    }
                }
            }

            foreach (var relation in catalogue.Relations)
            {
                Check(relation);
            }

            foreach (var storyEvent in catalogue.Events)
            {
                Check(storyEvent);
            }

            CheckStoryOrder();
            foreach (var source in catalogue.Sources)
            {
                CheckStage(source.Id.Value, source.Level);
            }

            foreach (var item in catalogue.Media)
            {
                Check(item);
            }
        }

        // The editorial source uses one space of identifiers; a text that names two records would be ambiguous in it.
        private void CheckIdentifiersAcrossKinds()
        {
            var all = catalogue.Entities.Select(entity => entity.Id.Value)
                .Concat(catalogue.Entities.SelectMany(entity => entity.Dated).Select(dated => dated.Key))
                .Concat(catalogue.Relations.Select(relation => relation.Id.Value))
                .Concat(catalogue.Events.Select(storyEvent => storyEvent.Id.Value))
                .Concat(catalogue.Sources.Select(source => source.Id.Value))
                .Concat(catalogue.Media.Select(item => item.Id.Value));
            foreach (var duplicate in all.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                if (!problems.Exists(problem => problem.StartsWith(duplicate.Key + ":", StringComparison.Ordinal)))
                {
                    problems.Add($"{duplicate.Key}: the identifier names records of two kinds.");
                }
            }
        }

        private void CheckInfo()
        {
            var info = catalogue.Info;
            var levels = info.ReadingStages.Select(stage => stage.Level.Value).ToList();
            if (levels.Count == 0 || !levels.SequenceEqual(Enumerable.Range(0, levels.Count)))
            {
                problems.Add("collection: the reading stages start at 0 and have no gaps.");
            }

            CheckStage("collection", info.DefaultLevel);
            CheckPublication(info.Publication);
            Known("collection", info.Story, ReadingLevel.Opening, "story");
            Known("collection", info.Edition.Work, ReadingLevel.Opening, "work of the edition");
        }

        // What everybody may see is a published revision (GOV-021): it says which revision it is, and every
        // statement in it has been reviewed – a disputed one is shown as disputed, anything else is not ready.
        private void CheckPublication(PublicationState publication)
        {
            if (publication.Revision is { Number: < 1 })
            {
                problems.Add("collection: the number of a published revision starts at 1.");
            }

            if (!publication.PublicReleaseApproved)
            {
                return;
            }

            if (publication.Revision is null)
            {
                problems.Add("collection: only a published revision is approved for the public; it states its number and the day it was published.");
            }

            var statements = catalogue.Relations.Select(relation => (Name: relation.Id.Value, relation.Review))
                .Concat(catalogue.Events.Select(storyEvent => (Name: storyEvent.Id.Value, storyEvent.Review)))
                .Concat(catalogue.Entities.SelectMany(entity => entity.Dated).Select(dated => (Name: dated.Key, dated.Review)));
            foreach (var (name, review) in statements)
            {
                if (review is not (ReviewStatus.Reviewed or ReviewStatus.Disputed))
                {
                    problems.Add($"{name}: a published revision holds only reviewed statements; this one is {review}.");
                }
            }
        }

        private void Check(Relation relation)
        {
            var name = relation.Id.Value;
            CheckSources(name, relation.Sources, relation.Level);
            CheckStage(name, relation.Level);
            Known(name, relation.From, relation.Level, "endpoint");
            Known(name, relation.To, relation.Level, "endpoint");
            Known(name, relation.Scope.Story, relation.Level, "story of the scope");
            foreach (var id in relation.Scope.Events)
            {
                Known(name, id, relation.Level, "event of the scope");
            }
        }

        private void Check(StoryEvent storyEvent)
        {
            var name = storyEvent.Id.Value;
            CheckSources(name, storyEvent.Sources, storyEvent.Level);
            CheckStage(name, storyEvent.Level);
            if (storyEvent.Associations.IsEmpty)
            {
                problems.Add($"{name}: an event says what at least one entity has to do with it.");
            }

            foreach (var repeated in storyEvent.Associations.GroupBy(association => association.Entity).Where(group => group.Count() > 1))
            {
                problems.Add($"{name}: {repeated.Key} is associated twice.");
            }

            foreach (var association in storyEvent.Associations)
            {
                // A place is where something happens and an object is a thing in it; neither acts or is mentioned
                // like a character, and showing one in a lane of participants would be wrong.
                if (Known(name, association.Entity, storyEvent.Level, "associated entity") is not { } entity)
                {
                    continue;
                }

                if (entity.Type.Nature == EntityNature.Place && association.Presence != Presence.Setting)
                {
                    problems.Add($"{name}: the place {entity.Id} is associated as {association.Presence}, not as the setting.");
                }

                if (entity.Type.Nature == EntityNature.Object && association.Presence != Presence.Object)
                {
                    problems.Add($"{name}: the object {entity.Id} is associated as {association.Presence}, not as an object.");
                }
            }
        }

        // Two events at the same place of the same story order would have no order between them.
        private void CheckStoryOrder()
        {
            var positions = catalogue.Events
                .Select(storyEvent => (storyEvent.Id, Position: (storyEvent.Time as Occurrence)?.At as NarrativePosition))
                .Where(entry => entry.Position is not null);
            foreach (var shared in positions.GroupBy(entry => entry.Position).Where(group => group.Count() > 1))
            {
                problems.Add($"{shared.First().Id}: {string.Join(", ", shared.Skip(1).Select(entry => entry.Id))} has the same place in the story order.");
            }
        }

        private void Check(MediaItem item)
        {
            var name = item.Id.Value;
            CheckSources(name, item.Sources, item.Level);
            CheckStage(name, item.Level);
            foreach (var id in item.DepictedEntities)
            {
                Known(name, id, item.Level, "depicted entity");
            }

            foreach (var id in item.DepictedEvents)
            {
                Known(name, id, item.Level, "depicted event");
            }

            foreach (var id in item.RelatedRelations)
            {
                Known(name, id, item.Level, "explained relation");
            }

            if (item.Rights.Status == RightsStatus.PermissionNotVerified && (item.DemoRenderAllowed || item.PublicRenderAllowed || item.File is not null || item.RemoteFile is not null))
            {
                problems.Add($"{name}: an item without established permission has no file and is not shown.");
            }

            // Showing an item to everybody is a decision of its own and asks for more than a demonstration does
            // of the rights, and the same of the item itself.
            if (item.PublicRenderAllowed && item.Rights.Status is not (RightsStatus.OriginalProjectContent or RightsStatus.OpenAccessVerified))
            {
                problems.Add($"{name}: only original project content and items with verified open access are shown to the public.");
            }

            if (item.PublicRenderAllowed && !item.DemoRenderAllowed && item.Rights.Status == RightsStatus.OriginalProjectContent)
            {
                problems.Add($"{name}: an item that is shown to the public is shown in a demonstration as well.");
            }

            if (!item.DemoRenderAllowed && !item.PublicRenderAllowed)
            {
                return;
            }

            if (item.DemoRenderAllowed && item.Rights.Status != RightsStatus.OriginalProjectContent)
            {
                problems.Add($"{name}: only original project content is shown in the demonstration.");
            }

            if (item.File is null)
            {
                problems.Add($"{name}: an item that is shown needs its file.");
            }

            if (item.Kind == MediaKind.Image && item.AltTextStatus != AltTextStatus.ProjectAuthored)
            {
                problems.Add($"{name}: an image that is shown needs a checked text alternative.");
            }

            if (item.Kind == MediaKind.Video && (item.DurationSeconds is not > 0 || item.Captions.IsEmpty || item.Transcript is null))
            {
                problems.Add($"{name}: a video that is shown needs its playing time, captions and a transcript.");
            }
        }

        private void CheckSources(string name, ImmutableArray<SourceId> sources, ReadingLevel level)
        {
            if (sources.IsDefaultOrEmpty)
            {
                problems.Add($"{name}: no source supports the record.");
                return;
            }

            foreach (var id in sources)
            {
                if (catalogue.Find(id) is not { } source)
                {
                    problems.Add($"{name}: the source {id} does not exist.");
                }
                else if (!source.Level.IsWithin(level))
                {
                    problems.Add($"{name}: its source {id} belongs to a later reading level and would give the story away.");
                }
            }
        }

        private void CheckStage(string name, ReadingLevel level)
        {
            if (level.Value >= catalogue.Info.ReadingStages.Length)
            {
                problems.Add($"{name}: the reading level {level} is not a stage of the collection.");
            }
        }

        private Entity? Known(string name, EntityId id, ReadingLevel level, string what)
        {
            var entity = catalogue.Find(id);
            Report(name, id.Value, entity?.Level, level, what);
            return entity;
        }

        private void Known(string name, EventId id, ReadingLevel level, string what) =>
            Report(name, id.Value, catalogue.Find(id)?.Level, level, what);

        private void Known(string name, RelationId id, ReadingLevel level, string what) =>
            Report(name, id.Value, catalogue.Find(id)?.Level, level, what);

        // A record may only refer to what a visitor at its own level may already see; otherwise showing the record
        // would reveal the later one.
        private void Report(string name, string id, ReadingLevel? found, ReadingLevel level, string what)
        {
            if (found is null)
            {
                problems.Add($"{name}: the {what} {id} does not exist.");
            }
            else if (!found.Value.IsWithin(level))
            {
                problems.Add($"{name}: its {what} {id} belongs to a later reading level and would give the story away.");
            }
        }
    }
}
