using Vafadar.Dastan.Core;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>What the pilot sample of the repository says once it is read: the facts later phases build on.</summary>
public sealed class PilotSampleTests
{
    private static readonly Lazy<Catalogue> Sample = new(() => SampleFiles.Read());

    private static Catalogue Catalogue => Sample.Value;

    [Fact]
    public void The_sample_is_read_completely()
    {
        Assert.Equal(18, Catalogue.Sources.Length);
        Assert.Equal(14, Catalogue.Entities.Length);
        Assert.Equal(20, Catalogue.Relations.Length);
        Assert.Equal(8, Catalogue.Events.Length);
        Assert.Equal(5, Catalogue.Media.Length);
    }

    [Fact]
    public void The_collection_knows_its_story_its_edition_and_its_world()
    {
        var info = Catalogue.Info;

        Assert.Equal("dastan-shahnameh-rostam-sohrab-pilot-v0.5", info.Id);
        Assert.Equal("world-shahnameh", info.WorldId);
        Assert.Equal(new EntityId("story-rostam-sohrab"), info.Subject);
        Assert.Equal(new EntityId("work-shahnameh"), info.Edition!.Work);
        Assert.Equal(new Uri("https://ganjoor.net/ferdousi/shahname/sohrab"), info.Edition.BaseUrl);
        Assert.Equal("en", info.Edition.SummaryLanguage);
    }

    [Fact]
    public void A_visitor_starts_at_the_opening_of_five_reading_stages()
    {
        var info = Catalogue.Info;

        Assert.Equal(ReadingLevel.Opening, info.DefaultLevel);
        Assert.Equal([0, 1, 2, 3, 4], info.ReadingStages.Select(stage => stage.Level.Value));
        Assert.Equal("Opening and orientation", info.ReadingStages[0].Label.Resolve("en").Value);
        Assert.Equal("Complete selected arc", info.ReadingStages[4].Label.Resolve("en").Value);
    }

    [Fact]
    public void The_sample_may_be_demonstrated_locally_and_is_not_approved_for_the_public()
    {
        Assert.True(Catalogue.Info.Publication.LocalDemoAllowed);
        Assert.False(Catalogue.Info.Publication.PublicReleaseApproved);
    }

    [Fact]
    public void Every_record_of_the_sample_keeps_its_reading_level()
    {
        Assert.Equal(ReadingLevel.Opening, Catalogue.Find(new EntityId("character-rostam"))!.Level);
        Assert.Equal(new ReadingLevel(1), Catalogue.Find(new EntityId("character-tahmineh"))!.Level);
        Assert.Equal(new ReadingLevel(2), Catalogue.Find(new EntityId("character-sohrab"))!.Level);
        Assert.Equal(new ReadingLevel(4), Catalogue.Find(new EventId("event-08"))!.Level);
        Assert.Equal(new ReadingLevel(4), Catalogue.Find(new MediaId("media-met-453265"))!.Level);
    }

    [Fact]
    public void The_types_of_the_sample_become_the_managed_types()
    {
        Assert.Same(EntityTypes.Work, Catalogue.Find(new EntityId("work-shahnameh"))!.Type);
        Assert.Same(EntityTypes.StoryArc, Catalogue.Find(new EntityId("story-rostam-sohrab"))!.Type);
        Assert.Same(EntityTypes.AnimalCharacter, Catalogue.Find(new EntityId("character-rakhsh"))!.Type);
        Assert.Same(EntityTypes.NarrativeRegion, Catalogue.Find(new EntityId("place-turan"))!.Type);
        Assert.Same(EntityTypes.NarrativeObject, Catalogue.Find(new EntityId("object-token"))!.Type);
    }

    [Fact]
    public void A_name_is_there_in_English_and_in_Persian()
    {
        var rostam = Catalogue.Find(new EntityId("character-rostam"))!;

        Assert.Equal("Rostam", rostam.Label.Resolve("en").Value);
        Assert.Equal("رستم", rostam.Label.Resolve("fa").Value);
        Assert.False(rostam.Label.Resolve("fa").IsFallback);
    }

    [Fact]
    public void A_Persian_name_nobody_confirmed_is_a_draft()
    {
        Assert.All(Catalogue.Entities, entity => Assert.Equal(TextReview.Draft, entity.Label.Resolve("fa").Review));
    }

    [Fact]
    public void Without_a_translation_a_summary_falls_back_to_English_and_says_so()
    {
        var summary = Catalogue.Find(new EntityId("character-rostam"))!.Summary.Resolve("fa", "en");

        Assert.Equal("en", summary.Language);
        Assert.True(summary.IsFallback);
    }

    [Fact]
    public void A_relation_keeps_its_direction_its_scope_and_its_evidence()
    {
        var father = Catalogue.Find(new RelationId("relation-02"))!;

        Assert.Equal(new EntityId("character-zal"), father.From);
        Assert.Equal(new EntityId("character-rostam"), father.To);
        Assert.Equal("parentOf", father.Type.Key);
        Assert.Equal(RelationScopeKind.BackgroundLineage, father.Scope.Kind);
        Assert.Equal(new EntityId("story-rostam-sohrab"), father.Scope.Story);
        Assert.Empty(father.Scope.Events);
        Assert.Equal([new SourceId("src-iranica-zal"), new SourceId("src-ganjoor-lineage")], father.Sources);
        Assert.Equal(ReviewStatus.SourcedReviewPending, father.Review);
        Assert.NotNull(father.PassageLocator);
    }

    [Fact]
    public void Marriage_and_combat_read_the_same_from_both_sides()
    {
        var symmetric = Catalogue.Relations.Where(relation => relation.Type.Directionality == Directionality.Symmetric);

        Assert.Equal(["opponentOf", "spouseOf"], symmetric.Select(relation => relation.Type.Key).Distinct().Order());
    }

    [Fact]
    public void A_relation_that_an_event_reveals_names_the_event()
    {
        var revealed = Catalogue.Relations.Where(relation => relation.Scope.Kind == RelationScopeKind.RevealedInEvent).ToList();

        Assert.NotEmpty(revealed);
        Assert.All(revealed, relation => Assert.NotEmpty(relation.Scope.Events));
    }

    [Fact]
    public void The_events_are_places_in_the_telling_of_the_story()
    {
        var positions = Catalogue.Events
            .Select(storyEvent => Assert.IsType<NarrativePosition>(Assert.IsType<Occurrence>(storyEvent.Time).At))
            .ToList();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], positions.Select(position => position.Ordinal));
        Assert.All(positions, position => Assert.Equal("story-rostam-sohrab", position.Sequence));
        Assert.All(positions, position => Assert.Equal(NarrativeOrder.Presentation, position.Order));
    }

    [Fact]
    public void No_event_of_the_story_has_a_calendar_date()
    {
        var year1010 = new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 1010));

        Assert.All(Catalogue.Events, storyEvent =>
            Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(storyEvent.Time, year1010).Order));
    }

    [Fact]
    public void An_event_may_condense_several_scenes_and_says_so()
    {
        Assert.All(Catalogue.Events, storyEvent => Assert.True(storyEvent.CondensesSeveralScenes));
    }

    [Fact]
    public void Naming_Rostam_as_the_father_does_not_bring_him_to_the_scene()
    {
        var lineage = Catalogue.Find(new EventId("event-03"))!;

        var rostam = Assert.Single(lineage.Associations, association => association.Entity == new EntityId("character-rostam"));

        Assert.Equal(Presence.MentionedOnly, rostam.Presence);
        Assert.Equal("father identified in the explanation", rostam.Role.Resolve("en").Value);
    }

    [Fact]
    public void Places_are_settings_and_the_token_is_an_object_in_every_event()
    {
        foreach (var storyEvent in Catalogue.Events)
        {
            foreach (var association in storyEvent.Associations)
            {
                var nature = Catalogue.Find(association.Entity)!.Type.Nature;
                Assert.Equal(nature == EntityNature.Place, association.Presence == Presence.Setting);
                Assert.Equal(nature == EntityNature.Object, association.Presence == Presence.Object);
            }
        }
    }

    [Fact]
    public void A_source_keeps_where_to_look_and_who_wrote_it()
    {
        var iranica = Catalogue.Find(new SourceId("src-iranica-zal"))!;

        Assert.Equal(SourceKind.ScholarlyReference, iranica.Kind);
        Assert.Equal(["Alireza Shapur Shahbazi", "Simone Cristoforetti"], iranica.Authors);
        Assert.Equal(new DateOnly(2009, 7, 20), iranica.PublishedOn);
        Assert.Equal(new DateOnly(2012, 10, 23), iranica.UpdatedOn);
        Assert.Equal(new DateOnly(2026, 11, 15), iranica.AccessedOn);
        Assert.Equal(Uri.UriSchemeHttps, iranica.Url.Scheme);
        Assert.NotNull(iranica.PassageLocator);
    }

    [Fact]
    public void Only_the_two_original_items_may_be_shown_and_both_wait_for_the_lineage()
    {
        var shown = Catalogue.Media.Where(item => item.DemoRenderAllowed).ToList();

        Assert.Equal(["media-dastan-parentage-video", "media-dastan-family-illustration"], shown.Select(item => item.Id.Value));
        Assert.All(shown, item => Assert.Equal(RightsStatus.OriginalProjectContent, item.Rights.Status));
        Assert.All(shown, item => Assert.Equal(new ReadingLevel(2), item.Level));
        Assert.All(shown, item => Assert.Equal(MediaRepresentation.PresentDayExplanatoryDiagram, item.Representation));
    }

    [Fact]
    public void A_museum_painting_is_a_reference_with_its_page_and_no_file_of_ours()
    {
        var met = Catalogue.Find(new MediaId("media-met-452419"))!;
        var umma = Catalogue.Find(new MediaId("media-umma-1963-1-46"))!;

        Assert.False(met.DemoRenderAllowed);
        Assert.Null(met.File);
        Assert.Equal("collectionapi.metmuseum.org", met.RemoteFile!.Host);
        Assert.Equal(new Uri("https://www.metmuseum.org/art/collection/search/452419"), met.CataloguePage);
        Assert.Equal(RightsStatus.OpenAccessVerified, met.Rights.Status);
        Assert.Equal("CC0-1.0", met.Rights.License);
        Assert.Equal(new Uri("https://www.metmuseum.org/hubs/open-access"), met.Rights.PolicyPage);
        Assert.Equal("1972.285.2", met.AccessionNumber);
        Assert.Equal(MediaRepresentation.LaterArtwork, met.Representation);

        Assert.Equal(RightsStatus.PermissionNotVerified, umma.Rights.Status);
        Assert.Null(umma.File);
        Assert.Null(umma.RemoteFile);
        Assert.Null(umma.AltText);
        Assert.Equal(AltTextStatus.NotPrepared, umma.AltTextStatus);
    }

    [Fact]
    public void A_painting_made_around_a_year_stays_approximate_and_names_no_calendar()
    {
        var met = Catalogue.Find(new MediaId("media-met-452419"))!;

        var created = Assert.IsType<Occurrence>(met.Created);
        var position = Assert.IsType<CalendarPosition>(created.At);

        Assert.Equal("circa 1450 CE", created.SourceText);
        Assert.Equal(1450, position.Year);
        Assert.Equal(Era.CommonEra, position.Era);
        Assert.Equal(CalendarSystem.Unspecified, position.Calendar);
        Assert.Equal(CalendarPrecision.Year, position.Precision);
        Assert.True(position.IsApproximate);
    }

    [Fact]
    public void A_diagram_of_today_knows_its_day()
    {
        var diagram = Catalogue.Find(new MediaId("media-dastan-family-illustration"))!;

        var position = Assert.IsType<CalendarPosition>(Assert.IsType<Occurrence>(diagram.Created).At);

        Assert.Equal(CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, 2026, 11, 15), position);
        Assert.False(position.IsApproximate);
    }

    [Fact]
    public void The_date_of_an_artwork_is_not_a_date_of_the_story()
    {
        var painting = Catalogue.Find(new MediaId("media-met-453265"))!;
        var death = Catalogue.Find(painting.DepictedEvents.Single())!;

        Assert.Equal(TemporalOrder.Incompatible, TemporalComparer.Order(death.Time, painting.Created).Order);
    }

    [Fact]
    public void The_files_of_an_item_are_named_inside_the_folder_of_the_collection()
    {
        var video = Catalogue.Find(new MediaId("media-dastan-parentage-video"))!;
        var diagram = Catalogue.Find(new MediaId("media-dastan-family-illustration"))!;

        Assert.Equal("media/dastan-parentage-explainer.mp4", video.File);
        Assert.Equal("media/dastan-parentage-poster.png", video.Poster);
        Assert.Equal([new CaptionTrack("en", "media/dastan-parentage-en.vtt")], video.Captions);
        Assert.Equal("media/dastan-parentage-transcript-en.txt", video.Transcript);
        Assert.Equal(19.958333m, video.DurationSeconds);
        Assert.False(video.HasAudio);
        Assert.Equal((1280, 720), (video.Width, video.Height));

        Assert.Equal("media/dastan-family-connections.png", diagram.File);
        Assert.Equal([("image/svg+xml", "media/dastan-family-connections.svg")], diagram.Renditions);
        Assert.Equal(AltTextStatus.ProjectAuthored, diagram.AltTextStatus);
        Assert.NotNull(diagram.AltText);
    }

    [Fact]
    public void Every_file_the_read_model_names_exists()
    {
        var files = Catalogue.Media
            .SelectMany(item => new[] { item.File, item.Poster, item.Transcript }
                .Concat(item.Captions.Select(track => track.File))
                .Concat(item.Renditions.Select(rendition => rendition.File)))
            .OfType<string>()
            .ToList();

        Assert.Equal(6, files.Count);
        Assert.All(files, file => Assert.True(File.Exists(Path.Combine(SampleFiles.Content, file)), file));
    }

    [Fact]
    public void A_document_for_reviewers_is_not_part_of_the_read_model()
    {
        var texts = Catalogue.Media
            .SelectMany(item => typeof(MediaItem).GetProperties().Select(property => property.GetValue(item)))
            .Select(value => value switch
            {
                string text => text,
                Uri address => address.OriginalString,
                _ => null,
            })
            .OfType<string>()
            .ToList();

        Assert.DoesNotContain(texts, text => text.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(texts, text => text.Contains("docs/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_diagram_names_the_relations_it_explains()
    {
        var diagram = Catalogue.Find(new MediaId("media-dastan-family-illustration"))!;

        Assert.Equal(["relation-02", "relation-04", "relation-05"], diagram.RelatedRelations.Select(relation => relation.Value).Order());
        Assert.All(diagram.RelatedRelations, relation => Assert.Equal("parentOf", Catalogue.Find(relation)!.Type.Key));
    }

    [Fact]
    public void Loading_the_file_gives_the_same_collection_as_reading_its_text()
    {
        var loaded = PilotSample.Load(SampleFiles.PilotPath);

        Assert.Equal(Catalogue.Info.Id, loaded.Info.Id);
        Assert.Equal(Catalogue.Entities.Select(entity => entity.Id), loaded.Entities.Select(entity => entity.Id));
        Assert.Equal(Catalogue.Media.Select(item => item.File), loaded.Media.Select(item => item.File));
    }
}
