using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data;

/// <summary>
/// Turns the JSON of one sample into the records of the read model and collects every problem on the way. What the
/// sample states is checked here (versions, known values, addresses, files, dates); whether the records fit together
/// is checked by <see cref="Catalogue"/> afterwards.
/// </summary>
internal sealed partial class PilotReader(PilotSampleOptions options, List<string> problems)
{
    private const string ContentRootName = "the folder of the collection";
    private const string ReviewRootName = "the folder of the editors' documents";

    private static readonly FrozenDictionary<string, EntityType> EntityTypeNames = Named(
        ("Work", EntityTypes.Work),
        ("NarrativeArc", EntityTypes.StoryArc),
        ("Character", EntityTypes.Character),
        ("AnimalCharacter", EntityTypes.AnimalCharacter),
        ("NarrativePlace", EntityTypes.NarrativePlace),
        ("NarrativeRegion", EntityTypes.NarrativeRegion),
        ("NarrativeObject", EntityTypes.NarrativeObject),

        // What is not of the story but of the world it was told in (step 10.6).
        ("Person", EntityTypes.Person),
        ("Organisation", EntityTypes.Organisation),
        ("Place", EntityTypes.Place),
        ("Region", EntityTypes.Region));

    private static readonly FrozenDictionary<string, RelationType> RelationTypeNames =
        RelationTypes.All.ToFrozenDictionary(type => type.Key, StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, Directionality> DirectionNames = Names(
        ("directed", Directionality.Directed),
        ("symmetric", Directionality.Symmetric));

    private static readonly FrozenDictionary<string, RelationScopeKind> ScopeKindNames = Names(
        ("work_structure", RelationScopeKind.WorkStructure),
        ("background_lineage", RelationScopeKind.BackgroundLineage),
        ("relationship_revealed_in_beat", RelationScopeKind.RevealedInEvent),
        ("observed_narrative_context", RelationScopeKind.ObservedContext),
        ("selected_beat_action", RelationScopeKind.ActionInEvent),
        ("reported_in_selected_beat", RelationScopeKind.ReportedInEvent),
        ("outside_the_story", RelationScopeKind.OutsideTheStory));

    private static readonly FrozenDictionary<string, Presence> PresenceNames = Names(
        ("acts_in_selected_scenes", Presence.Acts),
        ("involved_in_selected_scenes", Presence.Involved),
        ("setting", Presence.Setting),
        ("object_in_selected_scenes", Presence.Object),
        ("mentioned_only", Presence.MentionedOnly),
        ("subject_of_report", Presence.SubjectOfReport),
        ("deceased_subject", Presence.DeceasedSubject));

    private static readonly FrozenDictionary<string, ReviewStatus> ReviewNames = Names(
        ("source_backed_editorial_review_pending", ReviewStatus.SourcedReviewPending),
        ("reviewed", ReviewStatus.Reviewed),
        ("disputed", ReviewStatus.Disputed),
        ("withdrawn", ReviewStatus.Withdrawn));

    private static readonly string[] PublicationStates = ["editorial_sample", "published_revision"];

    // A dated statement is written like a statement of a comparison, with the same states of review.
    private static readonly FrozenDictionary<string, ReviewStatus> DatedReviewNames =
        ComparisonSample.ReviewNames.ToFrozenDictionary(name => name, name => Enum.Parse<ReviewStatus>(name, ignoreCase: true), StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, SourceKind> SourceKindNames = Names(
        ("primary_literary_text_transcription", SourceKind.PrimaryText),
        ("scholarly_reference", SourceKind.ScholarlyReference),
        ("museum_object_record", SourceKind.MuseumRecord),
        ("institutional_rights_policy", SourceKind.RightsPolicy));

    private static readonly FrozenDictionary<string, NameKind> NameKindNames = Names(
        ("alternative", NameKind.Alternative),
        ("former", NameKind.Former),
        ("abbreviation", NameKind.Abbreviation),
        ("title", NameKind.Title),
        ("original_script", NameKind.OriginalScript));

    private static readonly FrozenDictionary<string, PositionPrecision> PrecisionNames = Names(
        ("exact", PositionPrecision.Exact),
        ("approximate", PositionPrecision.Approximate),
        ("centre_of_area", PositionPrecision.CentreOfArea));

    private static readonly FrozenDictionary<string, IdentifierMatch> MatchNames = Names(
        ("exact", IdentifierMatch.Exact),
        ("close", IdentifierMatch.Close),
        ("uncertain", IdentifierMatch.Uncertain));

    private static readonly FrozenDictionary<string, MediaKind> MediaKindNames = Names(
        ("image", MediaKind.Image),
        ("video", MediaKind.Video));

    private static readonly FrozenDictionary<string, RightsStatus> RightsNames = Names(
        ("original_project_content", RightsStatus.OriginalProjectContent),
        ("verified_open_access_metadata", RightsStatus.OpenAccessVerified),
        ("permission_not_verified", RightsStatus.PermissionNotVerified));

    private static readonly FrozenDictionary<string, MediaRepresentation> RepresentationNames = Names(
        ("later_artwork_of_literary_narrative", MediaRepresentation.LaterArtwork),
        ("present_day_explanatory_diagram", MediaRepresentation.PresentDayExplanatoryDiagram),
        ("photograph_of_historical_object", MediaRepresentation.PhotographOfHistoricalObject),
        ("work_of_its_time", MediaRepresentation.WorkOfItsTime),
        ("later_depiction_of_history", MediaRepresentation.LaterDepictionOfHistory),
        ("present_day_reconstruction", MediaRepresentation.PresentDayReconstruction),
        ("present_day_map", MediaRepresentation.PresentDayMap));

    private static readonly FrozenDictionary<string, AltTextStatus> AltTextNames = Names(
        ("not_prepared_image_not_cleared", AltTextStatus.NotPrepared),
        ("draft_pending_visual_review", AltTextStatus.DraftPendingReview),
        ("project_authored_for_exact_diagram", AltTextStatus.ProjectAuthored));

    private static readonly FrozenDictionary<string, Era> EraNames = Names(
        ("CE", Era.CommonEra),
        ("BCE", Era.BeforeCommonEra));

    private static readonly FrozenDictionary<string, CalendarSystem> CalendarNames = Names(
        ("Gregorian", CalendarSystem.Gregorian));

    private static readonly FrozenSet<string> ErasMistakenForCalendars = FrozenSet.ToFrozenSet(["CE", "BCE", "AD", "BC"], StringComparer.Ordinal);

    // The names of languages the first files used; any language tag is understood beside them.
    private static readonly FrozenDictionary<string, string> LanguageNames = Names(
        ("English", "en"),
        ("en", "en"),
        ("Persian", "fa"),
        ("fa", "fa"));

    private static readonly FrozenDictionary<string, TextReview> TextReviewNames = Names(
        ("draft", TextReview.Draft),
        ("reviewed", TextReview.Reviewed));

    private readonly List<Translation> _translations = [];
    private readonly List<Source> _sources = [];
    private readonly List<Entity> _entities = [];
    private readonly List<Relation> _relations = [];
    private readonly List<StoryEvent> _events = [];
    private readonly List<MediaItem> _media = [];
    private readonly List<DiscoveryThread> _threads = [];
    private readonly List<StageCorrespondence> _correspondences = [];
    private CatalogueInfo? _info;
    private ImmutableArray<RevisionEntry> _history = [];
    private bool _published;
    private string _language = "en";
    private string? _worldId;
    private EntityId? _subject;
    private ImmutableArray<EntityId> _stories = [];

    /// <summary>Reads the whole sample. Problems go to the list; the records are complete only when it stays empty.</summary>
    public void Read(JsonElement root)
    {
        var sample = new JsonRecord(root, "sample", problems);
        var version = sample.Text("schemaVersion");
        if (version != PilotSample.SchemaVersion)
        {
            if (version is not null)
            {
                sample.Problem($"the format {version} is not understood; this reader reads {PilotSample.SchemaVersion}.");
            }

            // Nothing else is read from a format the reader does not know: its fields could mean something else.
            return;
        }

        var datasetId = sample.Identifier("datasetId");
        _worldId = sample.Identifier("worldId");
        sample.Notes("documentVersion", "preparedAt", "status", "reviewNotes");

        // A collection that follows a text says which; one that tells no story may say its language alone.
        var editionRecord = sample.Child("editionContext", required: false);
        var edition = ReadEdition(editionRecord);
        if (sample.Text("language", required: false) is { } written)
        {
            if (editionRecord is not null)
            {
                sample.Problem("language is said by editionContext.summaryLanguage; it is said once.");
            }
            else if (Language(written) is { } tag)
            {
                _language = tag;
            }
            else
            {
                sample.Problem($"language {written} is no language tag; write en, fa, ar or the like.");
            }
        }

        var subject = sample.Identifier("subjectId", required: false);
        ReadTranslations(datasetId);
        var stages = ReadReadingPolicy(sample.Child("spoilerPolicy"), out var defaultLevel);
        var publication = ReadPublication(sample.Child("publication"));
        ReadInputContract(sample.Child("inputContract"));

        foreach (var record in sample.Children("sources"))
        {
            Add(_sources, ReadSource(record));
        }

        foreach (var record in sample.Children("entities"))
        {
            Add(_entities, ReadEntity(record));
        }

        // What the collection is about: the one story it tells, or what subjectId names – a company, a club,
        // a person, or one of several stories. The stories begin with the one a visitor enters first.
        var arcs = _entities.Where(entity => entity.Type == EntityTypes.StoryArc).Select(entity => entity.Id).ToList();
        if (subject is not null)
        {
            _subject = new EntityId(subject);
        }
        else if (arcs.Count == 1)
        {
            _subject = arcs[0];
        }
        else
        {
            sample.Problem(arcs.Count == 0
                ? "a collection that tells no story says in subjectId what it is about."
                : $"a collection that tells {arcs.Count} stories says in subjectId what it is about.");
        }

        _stories = [.. arcs.OrderBy(arc => arc == _subject ? 0 : 1)];
        if (arcs.Count > 0 && editionRecord is null)
        {
            sample.Problem("editionContext is missing: a collection that tells a story says which text it follows.");
        }

        foreach (var record in sample.Children("relations"))
        {
            Add(_relations, ReadRelation(record));
        }

        foreach (var record in sample.Children("events"))
        {
            Add(_events, ReadEvent(record));
        }

        // What was withdrawn was read – its texts and its translations are accounted for – and is left out.
        _relations.RemoveAll(relation => relation.Review == ReviewStatus.Withdrawn);
        _events.RemoveAll(storyEvent => storyEvent.Review == ReviewStatus.Withdrawn);

        foreach (var record in sample.Children("media"))
        {
            Add(_media, ReadMedia(record));
        }

        // Threads are optional: a collection has them when its editors laid some.
        foreach (var record in sample.Children("threads", required: false))
        {
            Add(_threads, ReadThread(record));
        }

        sample.Finish();
        foreach (var translation in _translations)
        {
            foreach (var key in translation.Unused)
            {
                problems.Add($"translation {translation.Language}: {key} translates nothing the sample has.");
            }
        }

        if (datasetId is not null && _worldId is not null && _subject is not null && (edition is not null || editionRecord is null) && defaultLevel is not null && publication is not null)
        {
            _info = new CatalogueInfo
            {
                Id = datasetId,
                WorldId = _worldId,
                Subject = _subject.Value,
                Stories = _stories,
                Language = _language,
                Edition = edition,
                ReadingStages = stages,
                Correspondences = [.. _correspondences],
                DefaultLevel = defaultLevel.Value,
                Publication = publication,
                History = _history,
            };
        }
    }

    // The kinds of thing a relation type holds between, in the words of the list for writers.
    private static string Natures(ImmutableArray<EntityNature> natures) => natures.IsDefaultOrEmpty ? "anything" : string.Join(" or ", natures.Select(nature => nature switch
    {
        EntityNature.Being => "a person, character or animal",
        EntityNature.Group => "an organisation",
        EntityNature.Place => "a place or region",
        EntityNature.Work => "a work",
        EntityNature.Object => "an object",
        EntityNature.Role => "an office or honour",
        EntityNature.Subject => "a subject of knowledge",
        _ => "a competition, season, event or period",
    }));

    // The types the first files named in words of their own; every other type is written as its key with a capital.
    private static FrozenDictionary<string, EntityType> Named(params (string Name, EntityType Value)[] names)
    {
        var all = names.ToDictionary(name => name.Name, name => name.Value, StringComparer.Ordinal);
        foreach (var type in EntityTypes.All.Except(all.Values))
        {
            all.Add(char.ToUpperInvariant(type.Key[0]) + type.Key[1..], type);
        }

        return all.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>Returns the name a file gives a type of entity.</summary>
    internal static string NameOf(EntityType type) => EntityTypeNames.First(name => name.Value == type).Key;

    /// <summary>Returns the type a record names with a word of the file, or nothing for a word that names none.</summary>
    internal static EntityType? TypeNamed(string written) => EntityTypeNames.GetValueOrDefault(written);

    /// <summary>Returns the fields that take one of a fixed set of values, with the values this reader understands.</summary>
    internal static ImmutableArray<ContentChoices> Choices()
    {
        static ImmutableArray<ContentChoice> Of(IEnumerable<string> names) => [.. names.Order(StringComparer.Ordinal).Select(name => new ContentChoice(name))];

        return
        [
            new("spoilerPolicy.kind", "How the reading stages are meant.", Of(["minimum_reader_progress_level"])),
            new("publication.state", "What kind of file this is: the working file of the editors, or a revision that is published.", Of(PublicationStates)),
            new("publication.history[].changes[].kind", "What a published revision did to a record.", Of(ChangeKindNames.Keys)),
            new("publication.history[].changes[].recordKind", "What kind of record a change is about.", Of(RecordKinds)),
            new("editionContext.summaryLanguage", "The language the texts of the file are written in.", Of(LanguageNames.Keys)),
            new("sources[].kind", "What kind of source it is.", Of(SourceKindNames.Keys)),
            new(
                "entities[].type",
                "What kind of thing an entity is.",
                [.. EntityTypeNames.OrderBy(name => name.Key, StringComparer.Ordinal).Select(name => new ContentChoice(name.Key, name.Value.Meaning))]),
            new(
                "relations[].type",
                "What a relation says. A type has one meaning; similar wordings are not merged into it.",
                [
                    .. RelationTypes.All.OrderBy(type => type.Key, StringComparer.Ordinal).Select(type => new ContentChoice(
                        type.Key,
                        (type.Directionality == Directionality.Symmetric ? "symmetric" : "directed, from → to")
                            + type.Kinship switch
                            {
                                Kinship.Parentage => "; a parent and a child in a family tree",
                                Kinship.Partnership => "; partners in a family tree",
                                _ => string.Empty,
                            }
                            + (type.Meaning is null ? string.Empty : "; " + type.Meaning)
                            + (type.From.IsDefaultOrEmpty && type.To.IsDefaultOrEmpty ? string.Empty : $"; from {Natures(type.From)} to {Natures(type.To)}"))),
                ]),
            new(
                "entities[].dated[].quantity.unit",
                "The unit of a quantity a statement states.",
                [.. QuantityUnits.All.OrderBy(unit => unit.Key, StringComparer.Ordinal).Select(unit => new ContentChoice(unit.Key, unit.Meaning))]),
            new("entities[].position.precision", "How exactly the position of a place is meant.", Of(PrecisionNames.Keys)),
            new("entities[].names[].kind", "What kind of other name a name of an entity is.", Of(NameKindNames.Keys)),
            new("entities[].identifiers[].match", "How sure the editors are that the record of another catalogue is about the entity.", Of(MatchNames.Keys)),
            new("relations[].directionality", "Whether the relation reads from one entity to the other; it has to agree with the type.", Of(DirectionNames.Keys)),
            new("relations[].scope.kind", "What the relation is a statement about.", Of(ScopeKindNames.Keys)),
            new("relations[].reviewStatus, events[].reviewStatus", "How far a statement has been checked.", Of(ReviewNames.Keys)),
            new("entities[].dated[].reviewStatus", "How far a statement with a date has been checked.", [.. ComparisonSample.ReviewNames.Select(name => new ContentChoice(name))]),
            new("events[].orderBasis", "What the order of the events follows.", Of(["narrative"])),
            new("events[].temporalExpression.kind", "The time of a story event: its place in the telling, never a date.", Of(["narrative_position"])),
            new("events[].grouping", "Optional: the event stands for several scenes.", Of(["editorial_beat_may_condense_multiple_scenes"])),
            new("events[].entityAssociations[].presence", "What an entity has to do with an event.", Of(PresenceNames.Keys)),
            new("media[].type", "An image or a video.", Of(MediaKindNames.Keys)),
            new("media[].rights.status", "Whether the item may be shown.", Of(RightsNames.Keys)),
            new("media[].depictedContext.kind", "What kind of picture the item is: of a story, or of a history – a photograph of a thing of the past, a work of its time, a later picture, a reconstruction, a map.", Of(RepresentationNames.Keys)),
            new("media[].altTextStatus", "Optional: the state of the text alternative.", Of(AltTextNames.Keys)),
            new("media[].creationDate.precision", "How exactly the date an item was made is known.", Of(["year", "day"])),
            new("media[].creationDate.era", "The era of that date.", Of(EraNames.Keys)),
            new("media[].creationDate.calendar", "The calendar of that date. Dates are stored in the Gregorian calendar (D-043); a year whose calendar the catalogue does not name leaves the field out.", Of(CalendarNames.Keys)),
            new("translation: review", "The state of a translation document as a whole.", Of(TextReviewNames.Keys)),
        ];
    }

    /// <summary>Builds the collection from the records. Only called when reading found no problem.</summary>
    public Catalogue Build() => new(_info!, _entities, _relations, _events, _sources, _media, _threads);

    private static FrozenDictionary<string, T> Names<T>(params (string Name, T Value)[] names) =>
        names.ToFrozenDictionary(name => name.Name, name => name.Value, StringComparer.Ordinal);

    private static void Add<T>(List<T> records, T? record)
        where T : class
    {
        if (record is not null)
        {
            records.Add(record);
        }
    }

    /// <summary>Reads the identifier of a record and reports the record's problems under it from then on.</summary>
    private static string? Named(JsonRecord record)
    {
        var id = record.Identifier("id");
        if (id is not null)
        {
            record.Name = id;
        }

        return id;
    }

    private static ReadingLevel? Level(JsonRecord record, string field = "spoilerLevel")
    {
        var level = record.Integer(field);
        if (level < 0)
        {
            record.Problem($"{field} is negative.");
            return null;
        }

        return level is null ? null : new ReadingLevel(level.Value);
    }

    /// <summary>Reads a list of identifiers.</summary>
    private static ImmutableArray<string> Ids(JsonRecord record, string field, bool required = true)
    {
        var ids = record.Texts(field, required);
        if (ids.Any(id => id.Any(char.IsWhiteSpace)))
        {
            record.Problem($"{field} contains an identifier with a blank.");
            return [];
        }

        return ids;
    }

    // The tag of a language as a file may name it: a name the first files used, or a language tag.
    private static string? Language(string stated) =>
        LanguageNames.TryGetValue(stated, out var tag) ? tag : LanguagePattern().IsMatch(stated) ? stated : null;

    private EditionContext? ReadEdition(JsonRecord? record)
    {
        if (record is null)
        {
            return null;
        }

        var id = record.Identifier("id");
        var work = record.Identifier("workId");
        var baseText = record.Text("baseText");
        var baseUrl = Address(record, "baseUrl");
        var critical = record.Text("criticalPrintEdition");
        var scope = record.Text("scope");
        var stated = record.Text("summaryLanguage");
        var language = stated is null ? null : Language(stated);
        var languageKnown = language is not null;
        if (stated is not null && !languageKnown)
        {
            record.Problem($"summaryLanguage {stated} is no language tag; write en, fa, ar or the like.");
        }
        record.Nothing("calendarMapping", "calendarMapping would give the story a calendar; told order is never turned into dates.");
        record.Notes("upstreamAttributedByHost", "evidencePolicy", "translationPolicy", "editorialReview");
        record.Finish();
        if (id is null || work is null || baseText is null || baseUrl is null || critical is null || scope is null || !languageKnown)
        {
            return null;
        }

        _language = language!;
        return new EditionContext
        {
            Id = id,
            Work = new EntityId(work),
            BaseText = baseText,
            BaseUrl = baseUrl,
            CriticalEditionNote = critical,
            Scope = scope,
            SummaryLanguage = language!,
        };
    }

    private ImmutableArray<ReadingStage> ReadReadingPolicy(JsonRecord? record, out ReadingLevel? defaultLevel)
    {
        defaultLevel = null;
        if (record is null)
        {
            return [];
        }

        var kind = record.Text("kind");
        if (kind is not null && kind != "minimum_reader_progress_level")
        {
            record.Problem($"the reading policy {kind} is not understood.");
        }

        var stages = ImmutableArray.CreateBuilder<ReadingStage>();
        foreach (var stage in record.Children("levels"))
        {
            var level = Level(stage, "level");
            var label = stage.Text("label");
            stage.Finish();
            if (level is not null && label is not null)
            {
                stages.Add(new ReadingStage(level.Value, Localized(label, $"stages/{level.Value}/label")));
            }
        }

        defaultLevel = Level(record, "defaultLevel");

        // What the editors state about another telling of the same world: stage by stage.
        foreach (var stated in record.Children("correspondences", required: false))
        {
            var collection = stated.Identifier("collection");
            var levels = ImmutableSortedDictionary.CreateBuilder<int, int>();
            var complete = collection is not null;
            foreach (var pair in stated.Children("levels"))
            {
                var here = pair.Integer("here");
                var there = pair.Integer("there");
                pair.Finish();
                if (here is null || there is null)
                {
                    complete = false;
                }
                else if (!levels.TryAdd(here.Value, there.Value))
                {
                    stated.Problem($"the stage {here.Value} is named twice.");
                    complete = false;
                }
            }

            stated.Notes("note");
            stated.Finish();
            if (complete)
            {
                _correspondences.Add(new StageCorrespondence(collection!, levels.ToImmutable()));
            }
        }

        record.Notes("rule", "readerKnowledgeNote", "distributionNote");
        record.Finish();
        return stages.ToImmutable();
    }

    private PublicationState? ReadPublication(JsonRecord? record)
    {
        if (record is null)
        {
            return null;
        }

        var state = record.Text("state");
        if (state is not null && !PublicationStates.Contains(state, StringComparer.Ordinal))
        {
            record.Problem($"the publication state {state} is not understood; a file is an editorial_sample or a published_revision.");
        }

        _published = state == "published_revision";
        var local = record.Flag("localDemoAllowed");
        var approved = record.Flag("publicReleaseApproved");

        // A published revision says which one it is; an editorial sample is none and cannot be approved.
        PublicationRevision? revision = null;
        var revisionComplete = true;
        if (state == "published_revision")
        {
            if (record.Child("revision") is { } stated)
            {
                var number = stated.Integer("number");
                var published = stated.Day("publishedAt");
                stated.Notes("note");
                stated.Finish();
                revisionComplete = number is not null && published is not null;
                revision = revisionComplete ? new PublicationRevision(number!.Value, published!.Value) : null;
            }
            else
            {
                revisionComplete = false;
            }

            _history = ReadHistory(record);
        }
        else
        {
            record.Absent("history", "history belongs to a published revision; an editorial sample has none.");
            record.Absent("revision", "revision belongs to a published revision; an editorial sample is none.");
            if (approved == true)
            {
                record.Problem("an editorial sample cannot be approved for public release.");
            }
        }

        record.Notes("note");
        record.Finish();
        return local is null || approved is null || !revisionComplete ? null : new PublicationState(local.Value, approved.Value, revision);
    }

    private static void ReadInputContract(JsonRecord? record)
    {
        if (record is null)
        {
            return;
        }

        if (record.Flag("rawDatasetPubliclyServable") == true)
        {
            record.Problem("the sample itself is editorial input and can never be served to visitors.");
        }

        if (record.Flag("reviewerFilesPubliclyServable") == true)
        {
            record.Problem("the documents for reviewers can never be served to visitors.");
        }

        record.Notes(
            "datasetIdentityNote",
            "narrativeContext",
            "eventAssociations",
            "publicationAndSpoilers",
            "mediaEligibility",
            "sourceLinks",
            "reviewerProvenance");
        record.Finish();
    }

    private Source? ReadSource(JsonRecord record)
    {
        var id = Named(record);
        var title = record.Text("title");
        var url = Address(record, "url");
        var kindKnown = record.Choice("kind", SourceKindNames, out var kind);
        var accessed = record.Day("accessedAt");
        var locator = record.Text("passageLocator", required: false);
        var reuse = record.Text("reuseNote", required: false);
        var authors = record.Texts("authors", required: false);
        var published = record.Day("publishedAt", required: false);
        var updated = record.Day("updatedAt", required: false);
        var level = Level(record);
        record.Finish();
        if (id is null || title is null || url is null || !kindKnown || accessed is null || level is null)
        {
            return null;
        }

        return new Source
        {
            Id = new SourceId(id),
            Title = title,
            Url = url,
            Kind = kind,
            AccessedOn = accessed.Value,
            PassageLocator = locator,
            ReuseNote = reuse,
            Authors = authors,
            PublishedOn = published,
            UpdatedOn = updated,
            Level = level.Value,
        };
    }

    private Entity? ReadEntity(JsonRecord record)
    {
        var id = Named(record);
        var typeKnown = record.Choice("type", EntityTypeNames, out var type);
        var label = record.Text("label");
        var persianLabel = record.Text("labelFa", required: false);
        var summary = record.Text("summary");

        // A theme of a gallery is a grouping of the editors: it may name sources and does not have to.
        var sources = Ids(record, "sourceIds", required: !(typeKnown && type == EntityTypes.Theme));
        var level = Level(record);
        var dated = ImmutableArray.CreateBuilder<DatedStatement>();
        var datedComplete = true;
        foreach (var statement in record.Children("dated", required: false))
        {
            if (ReadDated(statement) is { } read)
            {
                if (read.Review != ReviewStatus.Withdrawn)
                {
                    dated.Add(read);
                }
            }
            else
            {
                datedComplete = false;
            }
        }

        var names = ImmutableArray.CreateBuilder<EntityName>();
        var identifiers = ImmutableArray.CreateBuilder<ExternalIdentifier>();
        var partsComplete = true;
        foreach (var part in record.Children("names", required: false))
        {
            if (ReadName(part, level) is { } other)
            {
                names.Add(other);
            }
            else
            {
                partsComplete = false;
            }
        }

        foreach (var part in record.Children("identifiers", required: false))
        {
            if (ReadIdentifier(part) is { } identifier)
            {
                identifiers.Add(identifier);
            }
            else
            {
                partsComplete = false;
            }
        }

        GeoPosition? position = null;
        if (record.Child("position", required: false) is { } where)
        {
            var latitude = where.Number("latitude");
            var longitude = where.Number("longitude");
            var precisionKnown = where.Choice("precision", PrecisionNames, out var precision);
            where.Finish();
            if (latitude is null || longitude is null || !precisionKnown)
            {
                partsComplete = false;
            }
            else
            {
                position = new GeoPosition(latitude.Value, longitude.Value, precision);
            }
        }

        // The profile (ADR 0017): the picture, and the facts of the kind.
        var picture = record.Identifier("picture", required: false);
        var facts = ImmutableArray.CreateBuilder<ProfileFact>();
        foreach (var part in record.Children("facts", required: false))
        {
            if (typeKnown && id is not null && ReadFact(part, id, type!, level) is { } fact)
            {
                facts.Add(fact);
            }
            else
            {
                partsComplete = false;
            }
        }

        record.Finish();
        if (id is null || !typeKnown || label is null || summary is null || level is null || !datedComplete || !partsComplete)
        {
            return null;
        }

        var name = Localized(label, $"entities/{id}/label");
        if (persianLabel is not null)
        {
            if (_language == "fa")
            {
                record.Problem("labelFa repeats the language the file is written in; label is the Persian name.");
            }
            else if (name.In("fa") is not null)
            {
                record.Problem("the Persian name is given twice, as labelFa and by the translation.");
            }
            else
            {
                // The name follows the review state of the Persian translation; without one nobody has confirmed it.
                var review = _translations.Find(translation => translation.Language == "fa")?.Review ?? TextReview.Draft;
                if (!(options.ReviewedTextsOnly && review == TextReview.Draft))
                {
                    name = name.With("fa", persianLabel, review);
                }
            }
        }

        return new Entity
        {
            Id = new EntityId(id),
            Type = type!,
            Label = name,
            Summary = Localized(summary, $"entities/{id}/summary"),
            Sources = [.. sources.Select(source => new SourceId(source))],
            Level = level.Value,
            Dated = dated.ToImmutable(),
            Names = names.ToImmutable(),
            Identifiers = identifiers.ToImmutable(),
            Position = position,
            Facts = facts.ToImmutable(),
            Picture = picture is null ? null : new MediaId(picture),
        };
    }

    /// <summary>
    /// Reads a fact of the profile of an entity: its kind – one the type of the entity has – and exactly what
    /// that kind of fact says: <c>time</c>, <c>entityId</c> or <c>text</c>. Without a stage of its own it is
    /// told with its entity.
    /// </summary>
    private ProfileFact? ReadFact(JsonRecord record, string entity, EntityType type, ReadingLevel? ofEntity)
    {
        var key = record.Identifier("fact");
        FactKind? kind = null;
        if (key is not null)
        {
            record.Name = $"{entity}, the fact {key}";
            kind = ProfileFacts.Of(type).Find(key);
            if (kind is null)
            {
                record.Problem($"a {type.Key} has no such fact; it has {string.Join(", ", ProfileFacts.Of(type).All.Select(of => of.Key))}.");
            }
        }

        var sources = Ids(record, "sourceIds", required: false);
        var level = record.IsStated("spoilerLevel") ? Level(record) : ofEntity;
        TemporalExpression? time = null;
        string? node = null;
        string? words = null;
        var said = true;
        switch (kind?.Value)
        {
            case FactValue.Time:
                time = ReadTime(record, "time is a place in a telling: a fact of a profile is not dated by a place in the story.");
                said = time is not null;
                record.Absent("entityId", "this fact says a time.");
                record.Absent("text", "this fact says a time.");
                break;
            case FactValue.Words:
                words = record.Text("text");
                said = words is not null;
                record.Absent("time", "this fact is said in words.");
                record.Absent("entityId", "this fact is said in words.");
                break;
            case FactValue.Place or FactValue.Node:
                node = record.Identifier("entityId");
                said = node is not null;
                record.Absent("time", "this fact names an entity.");
                record.Absent("text", "this fact names an entity.");
                break;
            default:
                record.Notes("time", "entityId", "text");
                break;
        }

        record.Finish();
        if (kind is null || level is null || !said)
        {
            return null;
        }

        return new ProfileFact
        {
            Kind = kind,
            Time = time,
            Node = node is null ? null : new EntityId(node),
            Words = words is null ? null : Localized(words, $"entities/{entity}/facts/{kind.Key}"),
            Sources = [.. sources.Select(source => new SourceId(source))],
            Level = level.Value,
        };
    }

    /// <summary>Reads another name of an entity. Without a stage of its own it is shown with its entity.</summary>
    private static EntityName? ReadName(JsonRecord record, ReadingLevel? ofEntity)
    {
        var name = record.Text("name");
        if (name is not null)
        {
            record.Name = "the name " + name;
        }

        var language = record.Text("language");
        if (language is not null && !LanguageTag().IsMatch(language))
        {
            record.Problem($"language {language} is no language tag; write en, fa, ar or the like.");
            language = null;
        }

        var kindKnown = record.Choice("kind", NameKindNames, out var kind);
        var sources = Ids(record, "sourceIds", required: false);
        var level = record.IsStated("spoilerLevel") ? Level(record) : ofEntity;
        var timed = record.IsStated("time");
        var time = timed ? ReadTime(record, "time is a place in a telling: a name is not dated by a place in the story.") : null;
        record.Finish();
        if (name is null || language is null || !kindKnown || level is null || (timed && time is null))
        {
            return null;
        }

        return new EntityName(name, language, kind, time, [.. sources.Select(source => new SourceId(source))], level.Value);
    }

    /// <summary>Reads what an entity is called in the catalogue of somebody else.</summary>
    private static ExternalIdentifier? ReadIdentifier(JsonRecord record)
    {
        var issuer = record.Text("issuer");
        var value = record.Text("value");
        if (issuer is not null && value is not null)
        {
            record.Name = $"the identifier {value} of {issuer}";
        }

        var url = Address(record, "url", required: false);
        var matchKnown = record.Choice("match", MatchNames, out var match);
        record.Finish();
        return issuer is null || value is null || !matchKnown ? null : new ExternalIdentifier(issuer, value, url, match);
    }

    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$")]
    private static partial Regex LanguageTag();

    /// <summary>
    /// Reads a statement about an entity with a time of the calendar. The time is written as in a comparison and
    /// is never the place of a step in the story: what the story tells has an order, not a date.
    /// </summary>
    private static TemporalExpression? ReadTime(JsonRecord record, string refusal, string name = "time")
    {
        if (record.Raw(name) is not { } stated)
        {
            return null;
        }

        TemporalExpression? time = null;
        try
        {
            time = stated.Deserialize<TemporalExpression>(ComparisonSample.TimeOptions);
        }
        catch (Exception refused) when (refused is JsonException or ArgumentException or NotSupportedException)
        {
            record.Problem($"{name} cannot be read: {refused.Message}");
        }

        // A year of a calendar and a depth of time are times of the world (ADR 0021); a place in a telling is not.
        if (time is Occurrence { At: NarrativePosition })
        {
            record.Problem(refusal);
            time = null;
        }

        return time;
    }

    private DatedStatement? ReadDated(JsonRecord record)
    {
        var key = record.Identifier("key");
        if (key is not null)
        {
            record.Name = key;
        }

        var label = record.Text("label");
        var summary = record.Text("summary", required: false);
        var sources = Ids(record, "sourceIds");
        var level = Level(record);
        var reviewKnown = record.Choice("reviewStatus", DatedReviewNames, out var review);
        var reviewed = ReadReview(record);
        CheckWithdrawn(record, review, reviewed);
        // A statement says when, how much, or both; a quantity alone needs no time.
        var quantified = record.Child("quantity", required: false) is { } stated;
        var quantity = quantified ? ReadQuantity(record.Child("quantity", required: false)!) : null;
        var timed = record.IsStated("time");
        var time = timed ? ReadTime(record, "time is a place in a telling: a statement with a date is not a step of the story.") : null;
        if (!timed && !quantified)
        {
            record.Problem("time is missing.");
        }

        record.Finish();
        if (key is null || label is null || level is null || !reviewKnown || (timed && time is null) || (quantified && quantity is null) || (!timed && !quantified))
        {
            return null;
        }

        return new DatedStatement
        {
            Key = key,
            Label = Localized(label, $"dated/{key}/label"),
            Summary = summary is null ? null : Localized(summary, $"dated/{key}/summary"),
            Time = time,
            Quantity = quantity,
            Sources = [.. sources.Select(source => new SourceId(source))],
            Review = review,
            Reviewed = reviewed,
            Level = level.Value,
        };
    }

    /// <summary>Reads a quantity: a number, its unit from the list, and whether it is stated as approximate.</summary>
    private static Quantity? ReadQuantity(JsonRecord record)
    {
        var value = record.Number("value");
        var unit = record.Text("unit");
        if (unit is not null && !QuantityUnits.Has(unit))
        {
            record.Problem($"unit has the unknown value \"{unit}\".");
            unit = null;
        }

        var approximate = record.Flag("approximate", required: false) ?? false;
        var currency = record.Text("currency", required: false);
        if (unit == QuantityUnits.Money && (currency is null || !Currency().IsMatch(currency)))
        {
            record.Problem("an amount of money names its currency with three capital letters, such as EUR.");
            unit = null;
        }
        else if (unit is not null && unit != QuantityUnits.Money && currency is not null)
        {
            record.Problem("currency belongs to an amount of money only.");
            unit = null;
        }

        record.Finish();
        return value is null || unit is null ? null : new Quantity(value.Value, unit, approximate, currency);
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Currency();

    private Relation? ReadRelation(JsonRecord record)
    {
        var id = Named(record);
        var from = record.Identifier("from");
        var to = record.Identifier("to");
        var typeKnown = record.Choice("type", RelationTypeNames, out var type);
        var label = record.Text("label");
        var sources = Ids(record, "sourceIds");
        var level = Level(record);
        var locator = record.Text("passageLocator", required: false);
        var reviewKnown = record.Choice("reviewStatus", ReviewNames, out var review);
        var reviewed = ReadReview(record);
        CheckWithdrawn(record, review, reviewed);
        var directionKnown = record.Choice("directionality", DirectionNames, out var direction);
        if (typeKnown && directionKnown && type!.Directionality != direction)
        {
            var expected = type.Directionality == Directionality.Symmetric ? "symmetric" : "directed";
            record.Problem($"directionality contradicts the relation type {type.Key}, which is {expected}.");
        }

        var scope = ReadScope(record.Child("scope"));

        // When a relation held: only for what is said of the world outside the story, and as a time of the calendar.
        var timed = record.IsStated("time");
        var time = timed ? ReadTime(record, "time is a place in a telling: a relation is not dated by a place in the story.") : null;
        if (timed && scope is not null && scope.Kind != RelationScopeKind.OutsideTheStory)
        {
            record.Problem("time dates a relation of the story; only a relation with the scope outside_the_story says when it held.");
            time = null;
            timed = true;
        }

        // How long it held, where a source states a length and no years: a length of time, never a date.
        var lasted = record.Child("length", required: false) is not null;
        var length = lasted ? ReadQuantity(record.Child("length", required: false)!) : null;
        if (length is not null && length.Unit is not ("years" or "months" or "days"))
        {
            record.Problem("length is a length of time: its unit is years, months or days.");
            length = null;
        }

        if (length is { Value: <= 0 })
        {
            record.Problem("length is more than nothing.");
            length = null;
        }

        var note = record.Text("note", required: false);

        record.Finish();
        if ((timed && time is null) || (lasted && length is null))
        {
            return null;
        }

        if (id is null || from is null || to is null || !typeKnown || label is null || level is null || !reviewKnown || !directionKnown || scope is null)
        {
            return null;
        }

        return new Relation
        {
            Id = new RelationId(id),
            From = new EntityId(from),
            To = new EntityId(to),
            Type = type!,
            Label = Localized(label, $"relations/{id}/label"),
            Scope = scope,
            Sources = [.. sources.Select(source => new SourceId(source))],
            PassageLocator = locator,
            Time = time,
            Length = length,
            Note = note is null ? null : Localized(note, $"relations/{id}/note"),
            Review = review,
            Reviewed = reviewed,
            Level = level.Value,
        };
    }

    // A statement the editors took back stays in their working file – with who withdrew it, when and why – and
    // is no part of the collection: no page shows it, and the next revision names it in its history (GOV-004).
    private void CheckWithdrawn(JsonRecord record, ReviewStatus review, ReviewRecord? reviewed)
    {
        if (review != ReviewStatus.Withdrawn)
        {
            return;
        }

        if (_published)
        {
            record.Problem("a withdrawn statement is no part of a published revision; its history names it with the reason.");
        }
        else if (reviewed?.Note is null)
        {
            record.Problem("a withdrawn statement says in its review who withdrew it and when, and in the note of the review why.");
        }
    }

    /// <summary>Reads who reviewed a statement and when, where the file says so.</summary>
    private static ReviewRecord? ReadReview(JsonRecord record)
    {
        if (record.Child("review", required: false) is not { } review)
        {
            return null;
        }

        var by = review.Text("by");
        var on = review.Day("on");
        var note = review.Text("note", required: false);
        review.Finish();
        return by is null || on is null ? null : new ReviewRecord(by, on.Value, note);
    }

    private static readonly FrozenDictionary<string, RevisionChangeKind> ChangeKindNames = Names(
        ("added", RevisionChangeKind.Added),
        ("changed", RevisionChangeKind.Changed),
        ("removed", RevisionChangeKind.Removed),
        ("withdrawn", RevisionChangeKind.Withdrawn));

    private static readonly string[] RecordKinds = ["source", "entity", "relation", "event", "media"];

    /// <summary>Reads the history of a published revision: every revision up to this one with what it changed.</summary>
    private ImmutableArray<RevisionEntry> ReadHistory(JsonRecord publication)
    {
        var history = ImmutableArray.CreateBuilder<RevisionEntry>();
        foreach (var entry in publication.Children("history", required: false))
        {
            var number = entry.Integer("number");
            var published = entry.Day("publishedAt");
            var note = entry.Text("note", required: false);
            var changes = ImmutableArray.CreateBuilder<RevisionChange>();
            foreach (var change in entry.Children("changes"))
            {
                var kindKnown = change.Choice("kind", ChangeKindNames, out var kind);
                var id = change.Identifier("record");
                var recordKind = change.Text("recordKind");
                if (recordKind is not null && !RecordKinds.Contains(recordKind, StringComparer.Ordinal))
                {
                    change.Problem($"recordKind has the unknown value \"{recordKind}\".");
                    recordKind = null;
                }

                var label = change.Text("label");
                var level = Level(change);
                var reason = change.Text("reason", required: false);
                change.Finish();
                if (kindKnown && id is not null && recordKind is not null && label is not null && level is not null)
                {
                    changes.Add(new RevisionChange(kind, id, recordKind, LocalizedText.Of(_language, label), level.Value, reason));
                }
            }

            entry.Finish();
            if (number is not null && published is not null)
            {
                history.Add(new RevisionEntry(number.Value, published.Value, note, changes.ToImmutable()));
            }
        }

        return history.ToImmutable();
    }

    private static RelationScope? ReadScope(JsonRecord? record)
    {
        if (record is null)
        {
            return null;
        }

        var kindKnown = record.Choice("kind", ScopeKindNames, out var kind);

        // What is said of the world outside a story need not name a story.
        var story = record.Identifier("storyId", required: !kindKnown || kind != RelationScopeKind.OutsideTheStory);
        var events = Ids(record, "eventIds");
        var interpretation = record.Text("interpretation", required: false);
        record.Nothing("calendarPeriod", "calendarPeriod would date a relation of the story; told order is never turned into dates.");
        record.Finish();
        if (!kindKnown || (story is null && kind != RelationScopeKind.OutsideTheStory))
        {
            return null;
        }

        return new RelationScope
        {
            Kind = kind,
            Story = story is null ? null : new EntityId(story),
            Events = [.. events.Select(id => new EventId(id))],
            Interpretation = interpretation,
        };
    }

    private StoryEvent? ReadEvent(JsonRecord record)
    {
        var id = Named(record);
        var label = record.Text("label");
        var summary = record.Text("summary");
        var order = record.Integer("order");
        if (order < 1)
        {
            record.Problem("order is not a positive whole number.");
            order = null;
        }

        var basis = record.Text("orderBasis");
        if (basis is not null && basis != "narrative")
        {
            record.Problem($"orderBasis is {basis}; the order of a story event is the order of the telling (narrative).");
        }

        record.Absent("participantIds", "participantIds does not say what each entity has to do with the event; entityAssociations does.");
        var sources = Ids(record, "sourceIds");
        var level = Level(record);
        var reviewKnown = record.Choice("reviewStatus", ReviewNames, out var review);
        var reviewed = ReadReview(record);
        CheckWithdrawn(record, review, reviewed);
        var timeIsPlaceInStory = ReadStoryTime(record.Child("temporalExpression"));

        // The story the event is a step of: the one story of the collection, or the one storyId names.
        EntityId? story = null;
        var storyId = record.Identifier("storyId", required: false);
        if (_stories.IsEmpty)
        {
            record.Problem("the collection tells no story; an event is a step of the telling of one.");
        }
        else if (storyId is null)
        {
            if (_stories.Length == 1)
            {
                story = _stories[0];
            }
            else
            {
                record.Problem("storyId is missing: the collection tells several stories.");
            }
        }
        else if (_stories.Contains(new EntityId(storyId)))
        {
            story = new EntityId(storyId);
        }
        else
        {
            record.Problem($"storyId {storyId} is no story of the collection.");
        }

        var associations = ImmutableArray.CreateBuilder<EventAssociation>();
        var associationsComplete = true;
        foreach (var association in record.Children("entityAssociations"))
        {
            var entity = association.Identifier("entityId");
            var role = association.Text("role");
            var presenceKnown = association.Choice("presence", PresenceNames, out var presence);
            association.Finish();
            if (entity is null || role is null || !presenceKnown)
            {
                associationsComplete = false;
                continue;
            }

            associations.Add(new EventAssociation(new EntityId(entity), Localized(role, $"events/{id}/roles/{entity}"), presence));
        }

        var condenses = false;
        var grouping = record.Text("grouping", required: false);
        if (grouping == "editorial_beat_may_condense_multiple_scenes")
        {
            condenses = true;
        }
        else if (grouping is not null)
        {
            record.Problem($"grouping has the unknown value \"{grouping}\".");
        }

        record.Finish();
        if (id is null || label is null || summary is null || order is null || level is null || !reviewKnown || !timeIsPlaceInStory || !associationsComplete || story is null)
        {
            return null;
        }

        return new StoryEvent
        {
            Id = new EventId(id),
            Label = Localized(label, $"events/{id}/label"),
            Summary = Localized(summary, $"events/{id}/summary"),
            Time = new Occurrence(NarrativePosition.Of(story.Value.Value, NarrativeOrder.Presentation, order.Value)),
            Associations = associations.ToImmutable(),
            CondensesSeveralScenes = condenses,
            Sources = [.. sources.Select(source => new SourceId(source))],
            Review = review,
            Reviewed = reviewed,
            Level = level.Value,
        };
    }

    /// <summary>
    /// Checks that the time of a story event is nothing but its place in the story. A year, a date, a boundary or a
    /// duration next to it would be invented precision: the poem tells an order, not a calendar.
    /// </summary>
    private static bool ReadStoryTime(JsonRecord? record)
    {
        if (record is null)
        {
            return false;
        }

        var kind = record.Text("kind");
        var isPlaceInStory = kind == "narrative_position";
        if (kind is not null && !isPlaceInStory)
        {
            record.Problem($"the time of a story event is its place in the story (narrative_position), not {kind}.");
        }

        record.Nothing("calendarMapping", "calendarMapping would turn a place in the story into a date.");
        record.Nothing("duration", "duration would give a place in the story a length nobody stated.");
        foreach (var invented in new[] { "year", "date", "start", "end" })
        {
            record.Absent(invented, $"{invented} would turn a place in the story into a date.");
        }

        record.Finish();
        return isPlaceInStory;
    }

    private MediaItem? ReadMedia(JsonRecord record)
    {
        var id = Named(record);
        var kindKnown = record.Choice("type", MediaKindNames, out var kind);
        var label = record.Text("label");
        var caption = record.Text("caption");
        var altText = record.Text("altText", required: false);
        record.Choice("altTextStatus", AltTextNames, out var altTextStatus, required: false);

        string? file = null;
        Uri? remoteFile = null;
        var url = record.Text("url", required: false);
        if (url is not null)
        {
            if (HasScheme(url))
            {
                remoteFile = Https(record, "url", url);
            }
            else
            {
                file = Local(record, "url", url, options.ContentRoot, ContentRootName);
            }
        }

        Uri? page = null;
        var sourceUrl = record.Text("sourceUrl", required: false);
        if (sourceUrl is not null)
        {
            if (HasScheme(sourceUrl))
            {
                page = Https(record, "sourceUrl", sourceUrl);
            }
            else
            {
                record.Problem("sourceUrl is the public page of an item; a local document cannot be one.");
            }
        }

        var creator = record.Text("creator");
        var rights = ReadRights(record.Child("rights"));
        var depictedEntities = Ids(record, "depictedEntityIds", required: false);
        var depictedEvents = Ids(record, "depictedEventIds", required: false);
        var relatedRelations = Ids(record, "relatedRelationIds", required: false);

        // The themes the item is gathered under (R-MED-02): entities of the type Theme, which the check of the collection asks for.
        var themes = Ids(record, "themeIds", required: false);

        // A caption of its own in a place the item stands in (R-MED-07): the place names the entity or the event,
        // which the item says it shows; the item is referred to, not copied.
        var places = ImmutableArray.CreateBuilder<MediaPlace>();
        foreach (var place in record.Children("placements", required: false))
        {
            var entity = place.Identifier("entityId", required: false);
            var storyEvent = place.Identifier("eventId", required: false);
            var words = place.Text("caption");
            var supports = Ids(place, "sourceIds", required: false);
            place.Finish();
            if (words is null || id is null)
            {
                continue;
            }

            if ((entity is null) == (storyEvent is null))
            {
                record.Problem("a placement names one entity or one event.");
            }
            else if (entity is not null ? !depictedEntities.Contains(entity) : !depictedEvents.Contains(storyEvent!))
            {
                record.Problem($"a placement names {entity ?? storyEvent}, which the item does not say it shows.");
            }
            else if (places.Any(other => other.Entity?.Value == entity && other.Event?.Value == storyEvent))
            {
                record.Problem($"two placements name {entity ?? storyEvent}.");
            }
            else
            {
                places.Add(new MediaPlace(
                    entity is null ? null : new EntityId(entity),
                    storyEvent is null ? null : new EventId(storyEvent),
                    Localized(words, $"media/{id}/places/{entity ?? storyEvent}"),
                    [.. supports.Select(source => new SourceId(source))]));
            }
        }

        var representationKnown = ReadDepictedContext(record.Child("depictedContext"), out var representation, out var representationScope);
        var created = ReadCreation(record.Child("creationDate"));

        // The time an item shows is a time of the world, apart from the time it was made (ADR 0021).
        var shows = record.IsStated("depictedTime")
            ? ReadTime(record, "depictedTime is a place in a telling: the steps an item shows are its depictedEventIds.", "depictedTime")
            : null;

        // The times of the collection, apart from the times of the item and of what it shows (MED-006).
        var broughtIn = record.Day("addedAt", required: false);
        var changed = record.Day("changedAt", required: false);
        if (broughtIn is { } added && changed is { } later && later < added)
        {
            record.Problem("changedAt is before addedAt: what is said of an item cannot change before the item is there.");
        }

        var sources = Ids(record, "sourceIds");
        var level = Level(record);
        record.Notes("status", "deliveryStatus");
        var catalogueTitle = record.Text("catalogTitle", required: false);
        var accession = record.Text("accessionNumber", required: false);
        var credit = record.Text("creditLine", required: false);
        foreach (var unread in new[] { "digitalCaptureDate", "uploadDate", "publicationDate", "sharedAt" })
        {
            record.Nothing(unread, $"{unread} is not read by this reader; of an item only the creation date and the time it shows are.");
        }

        bool? allowed = null;
        string? reason = null;
        if (record.Child("demoRenderEligibility") is { } eligibility)
        {
            allowed = eligibility.Flag("allowed");
            reason = eligibility.Text("reason", required: false);
            eligibility.Finish();
        }

        var forPublic = false;
        if (record.Child("publicRenderEligibility", required: false) is { } published)
        {
            forPublic = published.Flag("allowed") == true;
            published.Notes("reason");
            published.Finish();
        }

        var poster = LocalFile(record, "posterUrl");
        var captions = ImmutableArray.CreateBuilder<CaptionTrack>();
        foreach (var track in record.Children("captions", required: false))
        {
            var language = track.Text("language");
            var trackFile = LocalFile(track, "url", required: true);
            track.Finish();
            if (language is not null && trackFile is not null)
            {
                captions.Add(new CaptionTrack(language, trackFile));
            }
        }

        string? transcript = null;
        var transcriptUrl = record.Text("transcriptUrl", required: false);
        var transcriptFormat = record.Text("transcriptFormat", required: false);
        if (transcriptUrl is null)
        {
            if (transcriptFormat is not null)
            {
                record.Problem("transcriptFormat is stated without a transcript.");
            }
        }
        else if (transcriptFormat != "text/plain" || !transcriptUrl.EndsWith(".txt", StringComparison.Ordinal))
        {
            record.Problem("the transcript for visitors is a plain text file (text/plain, .txt); a working document of the editors is not one.");
        }
        else
        {
            transcript = Local(record, "transcriptUrl", transcriptUrl, options.ContentRoot, ContentRootName);
        }

        var duration = record.Number("durationSeconds", required: false);
        var audio = record.Flag("audioTrack", required: false);
        int? width = null;
        int? height = null;
        if (record.Child("dimensions", required: false) is { } dimensions)
        {
            width = dimensions.Integer("width");
            height = dimensions.Integer("height");
            dimensions.Finish();
        }

        // A document for reviewers is checked and then left out: the read model has no place for it, so no page can
        // show it by accident.
        var provenance = record.Text("reviewerProvenancePath", required: false);
        var audience = record.Text("reviewerProvenanceAudience", required: false);
        if (provenance is null)
        {
            if (audience is not null)
            {
                record.Problem("reviewerProvenanceAudience is stated without a document.");
            }
        }
        else
        {
            if (audience != "reviewer_only")
            {
                record.Problem("a document for reviewers needs the audience reviewer_only.");
            }

            Local(record, "reviewerProvenancePath", provenance, options.ReviewRoot, ReviewRootName);
        }

        var renditions = ImmutableArray.CreateBuilder<(string Format, string File)>();
        foreach (var rendition in record.Children("renditions", required: false))
        {
            var format = rendition.Text("format");
            var renditionFile = LocalFile(rendition, "url", required: true);
            rendition.Finish();
            if (format is not null && renditionFile is not null)
            {
                renditions.Add((format, renditionFile));
            }
        }

        // The smaller sizes of an image: the same picture, narrower.
        var sizes = ImmutableArray.CreateBuilder<(int Width, string File)>();
        foreach (var size in record.Children("sizes", required: false))
        {
            var sizeWidth = size.Integer("width");
            var sizeFile = LocalFile(size, "url", required: true);
            size.Finish();
            if (sizeWidth is { } narrow && sizeFile is not null)
            {
                if (narrow <= 0 || (width is { } whole && narrow >= whole))
                {
                    record.Problem("a smaller size is narrower than the image and at least one pixel wide.");
                }
                else if (sizes.Any(other => other.Width == narrow))
                {
                    record.Problem($"two smaller sizes are {narrow} pixels wide.");
                }
                else
                {
                    sizes.Add((narrow, sizeFile));
                }
            }
        }

        if (sizes.Count > 0 && kindKnown && kind != MediaKind.Image)
        {
            record.Problem("only an image has smaller sizes.");
        }

        record.Finish();
        if (id is null || !kindKnown || label is null || caption is null || creator is null || rights is null
            || !representationKnown || created is null || level is null || allowed is null)
        {
            return null;
        }

        return new MediaItem
        {
            Id = new MediaId(id),
            Kind = kind,
            Label = Localized(label, $"media/{id}/label"),
            Caption = Localized(caption, $"media/{id}/caption"),
            AltText = altText is null ? null : Localized(altText, $"media/{id}/altText"),
            AltTextStatus = altTextStatus,
            File = file,
            RemoteFile = remoteFile,
            CataloguePage = page,
            Creator = creator,
            Rights = rights,
            Representation = representation,
            RepresentationScope = representationScope,
            Created = created,
            Shows = shows,
            BroughtIn = broughtIn,
            Changed = changed,
            DepictedEntities = [.. depictedEntities.Select(entity => new EntityId(entity))],
            DepictedEvents = [.. depictedEvents.Select(storyEvent => new EventId(storyEvent))],
            RelatedRelations = [.. relatedRelations.Select(relation => new RelationId(relation))],
            Themes = [.. themes.Select(theme => new EntityId(theme))],
            Places = places.ToImmutable(),
            Sources = [.. sources.Select(source => new SourceId(source))],
            Level = level.Value,
            DemoRenderAllowed = allowed.Value,
            PublicRenderAllowed = forPublic,
            DemoRenderReason = reason,
            Poster = poster,
            Captions = captions.ToImmutable(),
            Transcript = transcript,
            Renditions = renditions.ToImmutable(),
            Sizes = [.. sizes.OrderBy(size => size.Width)],
            DurationSeconds = duration,
            HasAudio = audio,
            Width = width,
            Height = height,
            CatalogueTitle = catalogueTitle,
            AccessionNumber = accession,
            CreditLine = credit,
        };
    }

    private MediaRights? ReadRights(JsonRecord? record)
    {
        if (record is null)
        {
            return null;
        }

        var statusKnown = record.Choice("status", RightsNames, out var status);
        var license = record.Text("license", required: false);
        var statement = record.Text("statement");
        var policy = Address(record, "sourceUrl", required: false);
        var checkedOn = record.Day("checkedAt");
        record.Finish();
        if (!statusKnown || statement is null || checkedOn is null)
        {
            return null;
        }

        return new MediaRights(status, license, statement, checkedOn.Value) { PolicyPage = policy };
    }

    private bool ReadDepictedContext(JsonRecord? record, out MediaRepresentation representation, out string? scope)
    {
        representation = default;
        scope = null;
        if (record is null)
        {
            return false;
        }

        var known = record.Choice("kind", RepresentationNames, out representation);
        var world = record.Identifier("worldId");
        if (world is not null && _worldId is not null && world != _worldId)
        {
            record.Problem($"the item shows the world {world}, not the world of the sample.");
        }

        scope = record.Text("scope", required: false);
        record.Nothing("calendarDate", "calendarDate would date the story by an artwork; the date an artwork was made is not a date of the story.");
        record.Finish();
        return known;
    }

    /// <summary>
    /// Reads when an item was made, as exactly as the catalogue states it: a year stays a year, "circa" stays
    /// approximate, and a calendar the catalogue does not name stays unnamed.
    /// </summary>
    private static TemporalExpression? ReadCreation(JsonRecord? record)
    {
        if (record is null)
        {
            return null;
        }

        var label = record.Text("label");
        var precision = record.Text("precision");
        var approximate = record.Flag("approximate");
        var eraKnown = record.Choice("era", EraNames, out var era);
        record.Nothing("uncertaintyBounds", "uncertaintyBounds is not read by this reader; state the date as exactly as it is known.");
        record.Notes("refersTo", "calendarStatus");

        CalendarSystem? calendar = CalendarSystem.Unspecified;
        var calendarName = record.Text("calendar", required: false);
        if (calendarName is not null)
        {
            if (CalendarNames.TryGetValue(calendarName, out var named))
            {
                calendar = named;
            }
            else
            {
                calendar = null;
                record.Problem(ErasMistakenForCalendars.Contains(calendarName)
                    ? $"calendar is {calendarName}, which is an era and not a calendar."
                    : $"calendar has the unknown value \"{calendarName}\".");
            }
        }

        CalendarPosition? position = null;
        switch (precision)
        {
            case "year":
                var year = record.Integer("year");
                foreach (var finer in new[] { "month", "day", "date" })
                {
                    record.Absent(finer, $"a date known to the year has no {finer}.");
                }

                if (year is not null && eraKnown && calendar is not null)
                {
                    position = Position(record, () => CalendarPosition.OfYear(calendar.Value, era, year.Value));
                }

                break;

            case "day":
                var day = record.Day("date");
                record.Absent("year", "a date known to the day is stated once, as date.");
                if (calendar == CalendarSystem.Unspecified)
                {
                    record.Problem("a date known to the day names its calendar.");
                }
                else if (eraKnown && era != Era.CommonEra)
                {
                    record.Problem("a day written as year-month-day is a day of the common era.");
                }
                else if (day is not null && eraKnown && calendar is not null)
                {
                    position = Position(record, () => CalendarPosition.OfDay(calendar.Value, era, day.Value.Year, day.Value.Month, day.Value.Day));
                }

                break;

            case null:
                record.Notes("year", "month", "day", "date");
                break;

            default:
                // The parts of the date are known fields; what they mean depends on the precision nobody understood.
                record.Notes("year", "month", "day", "date");
                record.Problem($"precision has the unknown value \"{precision}\".");
                break;
        }

        record.Finish();
        if (position is null || approximate is null || label is null)
        {
            return null;
        }

        return new Occurrence(approximate.Value ? position.Approximately() : position) { SourceText = label };
    }

    private static CalendarPosition? Position(JsonRecord record, Func<CalendarPosition> create)
    {
        try
        {
            return create();
        }
        catch (ArgumentException exception)
        {
            record.Problem(exception.Message);
            return null;
        }
    }

    private static Uri? Address(JsonRecord record, string field, bool required = true)
    {
        var text = record.Text(field, required);
        return text is null ? null : Https(record, field, text);
    }

    /// <summary>Accepts an address of the open web: https, with a host and without a user name or password in it.</summary>
    private static Uri? Https(JsonRecord record, string field, string text)
    {
        if (Uri.TryCreate(text, UriKind.Absolute, out var address)
            && address.Scheme == Uri.UriSchemeHttps
            && address.Host.Length > 0
            && address.UserInfo.Length == 0)
        {
            return address;
        }

        record.Problem($"{field} is not an https address without credentials.");
        return null;
    }

    private static bool HasScheme(string text) => SchemePattern().IsMatch(text);

    private string? LocalFile(JsonRecord record, string field, bool required = false)
    {
        var text = record.Text(field, required);
        return text is null ? null : Local(record, field, text, options.ContentRoot, ContentRootName);
    }

    /// <summary>
    /// Resolves a path of the sample against the sample's folder and returns it relative to the root it has to stay
    /// in, with forward slashes. A path that leaves the root, an absolute path and a missing file are problems.
    /// </summary>
    private string? Local(JsonRecord record, string field, string text, string root, string rootName)
    {
        if (HasScheme(text) || text.Contains('\\', StringComparison.Ordinal) || text.Any(char.IsControl) || Path.IsPathRooted(text))
        {
            record.Problem($"{field} is not a path relative to the sample, written with forward slashes.");
            return null;
        }

        var target = Path.GetFullPath(Path.Combine(options.SampleDirectory, text));
        var relative = Path.GetRelativePath(Path.GetFullPath(root), target);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            record.Problem($"{field} leaves {rootName}.");
            return null;
        }

        if (options.CheckFiles && !File.Exists(target))
        {
            record.Problem($"{field} names the file {text}, which does not exist.");
            return null;
        }

        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>Gives a text of the sample its translations.</summary>
    // A thread: its name, what it is about, and its stops – each one thing, with the reason it follows. The
    // note of a stop is translated under the identifier of what the stop names.
    private DiscoveryThread? ReadThread(JsonRecord record)
    {
        var id = Named(record);
        var label = record.Text("label");
        var summary = record.Text("summary");
        var level = Level(record);
        var stops = ImmutableArray.CreateBuilder<ThreadStop>();
        var complete = true;
        foreach (var (stop, index) in record.Children("stops").Select((stop, index) => (stop, index)))
        {
            stop.Name = $"{record.Name}, stop {index + 1}";
            var entity = stop.Identifier("entityId", required: false);
            var storyEvent = stop.Identifier("eventId", required: false);
            var item = stop.Identifier("mediaId", required: false);
            var note = stop.Text("note");
            stop.Finish();
            if (new[] { entity, storyEvent, item }.Count(target => target is not null) != 1)
            {
                stop.Problem("a stop names one thing: entityId, eventId or mediaId.");
                complete = false;
            }
            else if (note is null || id is null)
            {
                complete = false;
            }
            else
            {
                stops.Add(new ThreadStop(
                    entity is null ? null : new EntityId(entity),
                    storyEvent is null ? null : new EventId(storyEvent),
                    item is null ? null : new MediaId(item),
                    Localized(note, $"threads/{id}/notes/{entity ?? storyEvent ?? item}")));
            }
        }

        record.Notes("note");
        record.Finish();
        if (id is null || label is null || summary is null || level is null || !complete)
        {
            return null;
        }

        return new DiscoveryThread
        {
            Id = id,
            Label = Localized(label, $"threads/{id}/label"),
            Summary = Localized(summary, $"threads/{id}/summary"),
            Level = level.Value,
            Stops = stops.ToImmutable(),
        };
    }

    private LocalizedText Localized(string text, string key)
    {
        var localized = LocalizedText.Of(_language, text);
        foreach (var translation in _translations)
        {
            // A draft is asked for as well, so that what it names counts as named, and then left out for the public.
            if (translation.Take(key) is { } translated && !(options.ReviewedTextsOnly && translation.Review == TextReview.Draft))
            {
                localized = localized.With(translation.Language, translated, translation.Review);
            }
        }

        return localized;
    }

    private void ReadTranslations(string? datasetId)
    {
        for (var index = 0; index < options.Translations.Count; index++)
        {
            var name = $"translation {index + 1}";
            using var document = PilotSample.Parse(options.Translations[index], name, problems);
            if (document is null)
            {
                continue;
            }

            var record = new JsonRecord(document.RootElement, name, problems);
            var version = record.Text("formatVersion");
            if (version != PilotSample.TranslationVersion)
            {
                if (version is not null)
                {
                    record.Problem($"the format {version} is not understood; this reader reads {PilotSample.TranslationVersion}.");
                }

                continue;
            }

            var language = record.Text("language");
            if (language is not null)
            {
                record.Name = $"translation {language}";
                if (!LanguagePattern().IsMatch(language))
                {
                    record.Problem("language is not a language tag such as fa or de.");
                    language = null;
                }
                else if (language == _language)
                {
                    record.Problem("it translates into the language the sample is written in.");
                    language = null;
                }
                else if (_translations.Exists(translation => translation.Language == language))
                {
                    record.Problem("the sample has two translations into this language.");
                    language = null;
                }
            }

            var dataset = record.Text("datasetId");
            if (dataset is not null && datasetId is not null && dataset != datasetId)
            {
                record.Problem($"it belongs to {dataset}, not to this sample.");
            }

            var reviewKnown = record.Choice("review", TextReviewNames, out var review);
            record.Notes("note");
            var translation = new Translation(language ?? name, review);
            foreach (var (level, text) in record.NamedTexts("readingStages", required: false))
            {
                translation.Add($"stages/{level}/label", text);
            }

            foreach (var texts in Translated)
            {
                ReadTranslated(record, translation, texts);
            }

            record.Finish();
            if (language is not null && reviewKnown)
            {
                _translations.Add(translation);
            }
        }
    }

    /// <summary>The texts a translation may translate. The tool of the editors makes its inputs from this list.</summary>
    internal static readonly ImmutableArray<ContentTexts> Translated =
    [
        new("entities", ["label", "summary"], Facts: true),
        new("relations", ["label", "note"]),
        new("events", ["label", "summary"], Roles: true),
        new("media", ["label", "caption", "altText"], Places: true),
        new("dated", ["label", "summary"]),
        new("threads", ["label", "summary"], Notes: true),
    ];

    private static void ReadTranslated(JsonRecord record, Translation translation, ContentTexts translated)
    {
        var group = translated.Group;
        foreach (var (id, texts) in record.Named(group, required: false))
        {
            foreach (var field in translated.Fields)
            {
                if (texts.Text(field, required: false) is { } text)
                {
                    translation.Add($"{group}/{id}/{field}", text);
                }
            }

            if (translated.Notes)
            {
                foreach (var (stop, note) in texts.NamedTexts("notes", required: false))
                {
                    translation.Add($"{group}/{id}/notes/{stop}", note);
                }
            }

            if (translated.Roles)
            {
                foreach (var (entity, role) in texts.NamedTexts("roles", required: false))
                {
                    translation.Add($"{group}/{id}/roles/{entity}", role);
                }
            }

            if (translated.Places)
            {
                // The captions an item has in its places, by the identifier of the entity or the event (R-MED-07).
                foreach (var (place, words) in texts.NamedTexts("places", required: false))
                {
                    translation.Add($"{group}/{id}/places/{place}", words);
                }
            }

            if (translated.Facts)
            {
                // The facts of a profile that are said in words, by the key of the fact (ADR 0017).
                foreach (var (fact, words) in texts.NamedTexts("facts", required: false))
                {
                    translation.Add($"{group}/{id}/facts/{fact}", words);
                }
            }

            texts.Finish();
        }
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9+.-]*:")]
    private static partial Regex SchemePattern();

    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$")]
    private static partial Regex LanguagePattern();

    /// <summary>The texts of one translation document, by the place of the text they translate.</summary>
    private sealed class Translation(string language, TextReview review)
    {
        private readonly Dictionary<string, string> _texts = new(StringComparer.Ordinal);
        private readonly HashSet<string> _used = new(StringComparer.Ordinal);

        public string Language => language;

        public TextReview Review => review;

        /// <summary>Gets the places that no text of the sample asked for.</summary>
        public IEnumerable<string> Unused => _texts.Keys.Where(key => !_used.Contains(key)).Order(StringComparer.Ordinal);

        public void Add(string key, string text) => _texts[key] = text;

        public string? Take(string key)
        {
            if (!_texts.TryGetValue(key, out var text))
            {
                return null;
            }

            _used.Add(key);
            return text;
        }
    }
}
