using System.Collections.Immutable;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// A small made-up story for the tests: a hero comes to a town (level 0), a keepsake changes hands (level 1) and a
/// child learns who the father is (level 2). Tests change one record and build the catalogue again.
/// </summary>
internal sealed class SmallStory
{
    public const string Sequence = "story";

    public List<Entity> Entities { get; } =
    [
        Entity("work", EntityTypes.Work),
        Entity("story", EntityTypes.StoryArc),
        Entity("hero", EntityTypes.Character),
        Entity("town", EntityTypes.NarrativePlace),
        Entity("keepsake", EntityTypes.NarrativeObject, level: 1),
        Entity("child", EntityTypes.Character, level: 2, source: "passage-2"),
    ];

    public List<Relation> Relations { get; } =
    [
        Relation("visits", "hero", "town", "visits", events: ["arrival"]),
        Relation("parent", "hero", "child", "parentOf", level: 2, source: "passage-2", events: ["lineage"], kind: RelationScopeKind.RevealedInEvent),
    ];

    public List<StoryEvent> Events { get; } =
    [
        Event("arrival", 1, 0, "passage-0", ("hero", Presence.Acts), ("town", Presence.Setting)),
        Event("gift", 2, 1, "passage-1", ("hero", Presence.Acts), ("keepsake", Presence.Object)),
        Event("lineage", 3, 2, "passage-2", ("child", Presence.Acts), ("hero", Presence.MentionedOnly)),
    ];

    public List<Source> Sources { get; } = [Source("passage-0", 0), Source("passage-1", 1), Source("passage-2", 2)];

    public List<MediaItem> Media { get; } = [Diagram("diagram")];

    public int Stages { get; set; } = 3;

    public int DefaultLevel { get; set; }

    public PublicationState Publication { get; set; } = new(LocalDemoAllowed: true, PublicReleaseApproved: false);

    public ImmutableArray<RevisionEntry> History { get; set; } = [];

    public Catalogue Build() => new(
        new CatalogueInfo
        {
            Id = "small-story",
            WorldId = "world",
            Story = new EntityId("story"),
            Edition = new EditionContext
            {
                Id = "edition",
                Work = new EntityId("work"),
                BaseText = "A made-up text.",
                BaseUrl = new Uri("https://example.org/text"),
                CriticalEditionNote = "None.",
                Scope = "Three events.",
                SummaryLanguage = "en",
            },
            ReadingStages = [.. Enumerable.Range(0, Stages).Select(level => new ReadingStage(new ReadingLevel(level), Text($"Stage {level}")))],
            DefaultLevel = new ReadingLevel(DefaultLevel),
            Publication = Publication,
            History = History,
        },
        Entities,
        Relations,
        Events,
        Sources,
        Media);

    /// <summary>
    /// Makes the story a published revision: approved for the public, with a number and a day, and every relation
    /// and event reviewed.
    /// </summary>
    public SmallStory Published()
    {
        Publication = new PublicationState(LocalDemoAllowed: true, PublicReleaseApproved: true, new PublicationRevision(1, new DateOnly(2026, 11, 17)));
        for (var index = 0; index < Relations.Count; index++)
        {
            var relation = Relations[index];
            Relations[index] = new Relation
            {
                Id = relation.Id,
                From = relation.From,
                To = relation.To,
                Type = relation.Type,
                Label = relation.Label,
                Scope = relation.Scope,
                Sources = relation.Sources,
                PassageLocator = relation.PassageLocator,
                Review = ReviewStatus.Reviewed,
                Reviewed = new ReviewRecord("an editor", new DateOnly(2026, 11, 16)),
                Level = relation.Level,
            };
        }

        for (var index = 0; index < Events.Count; index++)
        {
            var storyEvent = Events[index];
            Events[index] = new StoryEvent
            {
                Id = storyEvent.Id,
                Label = storyEvent.Label,
                Summary = storyEvent.Summary,
                Time = storyEvent.Time,
                Associations = storyEvent.Associations,
                CondensesSeveralScenes = storyEvent.CondensesSeveralScenes,
                Sources = storyEvent.Sources,
                Review = ReviewStatus.Reviewed,
                Reviewed = new ReviewRecord("an editor", new DateOnly(2026, 11, 16)),
                Level = storyEvent.Level,
            };
        }

        return this;
    }

    /// <summary>Builds the catalogue and returns the problems it is refused for.</summary>
    public IReadOnlyList<string> Problems() => Assert.Throws<CatalogueException>(() => Build()).Problems;

    public static LocalizedText Text(string english) => LocalizedText.Of("en", english);

    public static Entity Entity(string id, EntityType type, int level = 0, string source = "passage-0") => new()
    {
        Id = new EntityId(id),
        Type = type,
        Label = Text(id),
        Summary = Text($"About {id}."),
        Sources = [new SourceId(source)],
        Level = new ReadingLevel(level),
    };

    public static Relation Relation(
        string id,
        string from,
        string to,
        string type,
        int level = 0,
        string source = "passage-0",
        string[]? events = null,
        RelationScopeKind kind = RelationScopeKind.ActionInEvent) => new()
    {
        Id = new RelationId(id),
        From = new EntityId(from),
        To = new EntityId(to),
        Type = RelationTypes.Find(type)!,
        Label = Text(type),
        Scope = new RelationScope { Kind = kind, Story = new EntityId("story"), Events = [.. (events ?? []).Select(e => new EventId(e))] },
        Sources = [new SourceId(source)],
        Review = ReviewStatus.SourcedReviewPending,
        Level = new ReadingLevel(level),
    };

    public static StoryEvent Event(string id, int ordinal, int level, string source, params (string Entity, Presence Presence)[] associations) => new()
    {
        Id = new EventId(id),
        Label = Text(id),
        Summary = Text($"What happens in {id}."),
        Time = new Occurrence(NarrativePosition.Of(Sequence, NarrativeOrder.Presentation, ordinal)),
        Associations = [.. associations.Select(a => new EventAssociation(new EntityId(a.Entity), Text("role"), a.Presence))],
        Sources = [new SourceId(source)],
        Review = ReviewStatus.SourcedReviewPending,
        Level = new ReadingLevel(level),
    };

    public static Source Source(string id, int level) => new()
    {
        Id = new SourceId(id),
        Title = id,
        Url = new Uri($"https://example.org/{id}"),
        Kind = SourceKind.PrimaryText,
        AccessedOn = new DateOnly(2026, 11, 15),
        Level = new ReadingLevel(level),
    };

    /// <summary>An original diagram of the parentage, shown from level 2 on.</summary>
    public static MediaItem Diagram(
        string id,
        int level = 2,
        bool allowed = true,
        RightsStatus rights = RightsStatus.OriginalProjectContent,
        string? file = "media/diagram.png",
        AltTextStatus altText = AltTextStatus.ProjectAuthored,
        MediaKind kind = MediaKind.Image,
        ImmutableArray<CaptionTrack>? captions = null,
        string? transcript = null,
        decimal? seconds = null,
        string[]? depicted = null,
        string[]? relations = null,
        bool forPublic = false) => new()
    {
        Id = new MediaId(id),
        Kind = kind,
        Label = Text(id),
        Caption = Text("A diagram made today."),
        AltText = Text("Two names joined by a line."),
        AltTextStatus = altText,
        File = file,
        Creator = "The project",
        Rights = new MediaRights(rights, null, "Made for the tests.", new DateOnly(2026, 11, 15)),
        Representation = MediaRepresentation.PresentDayExplanatoryDiagram,
        Created = new Occurrence(CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, 2026, 11, 15)),
        DepictedEntities = [.. (depicted ?? ["hero", "child"]).Select(e => new EntityId(e))],
        RelatedRelations = [.. (relations ?? ["parent"]).Select(relation => new RelationId(relation))],
        Sources = [new SourceId("passage-2")],
        Level = new ReadingLevel(level),
        DemoRenderAllowed = allowed,
        PublicRenderAllowed = forPublic,
        Captions = captions ?? [],
        Transcript = transcript,
        DurationSeconds = seconds,
    };
}
