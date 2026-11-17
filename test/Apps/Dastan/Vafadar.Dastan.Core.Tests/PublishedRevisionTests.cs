namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// What everybody may see (GOV-021, D-044): only a published revision – a collection that says which revision it
/// is and holds nothing but reviewed statements – and of its media only what is allowed for the public.
/// </summary>
public sealed class PublishedRevisionTests
{
    private static Projection? Public(SmallStory story, int level = 2) =>
        Projection.Of(story.Build(), new ReadingLevel(level), Audience.Public);

    [Fact]
    public void An_editorial_sample_is_shown_to_nobody_outside_a_demonstration()
    {
        var story = new SmallStory();

        Assert.Null(Public(story));
        Assert.NotNull(Projection.Of(story.Build(), ReadingLevel.Opening, Audience.LocalDemonstration));
    }

    [Fact]
    public void A_published_revision_is_shown_to_the_public_within_the_reading_position()
    {
        var story = new SmallStory().Published();

        var opening = Public(story, 0)!;
        var all = Public(story)!;

        Assert.DoesNotContain(opening.Entities, entity => entity.Id.Value == "child");
        Assert.Contains(all.Entities, entity => entity.Id.Value == "child");
        Assert.Equal(3, all.Steps.Length);
        Assert.Equal(2, all.Relations.Length);
    }

    [Fact]
    public void A_collection_approved_for_the_public_without_being_a_revision_is_refused()
    {
        var story = new SmallStory().Published();
        story.Publication = new PublicationState(LocalDemoAllowed: true, PublicReleaseApproved: true);

        Assert.Contains("collection: only a published revision is approved for the public; it states its number and the day it was published.", story.Problems());
    }

    [Fact]
    public void The_number_of_a_revision_starts_at_one()
    {
        var story = new SmallStory().Published();
        story.Publication = story.Publication with { Revision = new PublicationRevision(0, new DateOnly(2026, 11, 17)) };

        Assert.Contains("collection: the number of a published revision starts at 1.", story.Problems());
    }

    [Fact]
    public void A_published_revision_holds_only_reviewed_statements()
    {
        // The story is approved as it stands: its relations and events are sourced, and nobody has reviewed them.
        var story = new SmallStory();
        story.Publication = new PublicationState(true, true, new PublicationRevision(1, new DateOnly(2026, 11, 17)));

        var problems = story.Problems();

        Assert.Contains("visits: a published revision holds only reviewed statements; this one is SourcedReviewPending.", problems);
        Assert.Contains("lineage: a published revision holds only reviewed statements; this one is SourcedReviewPending.", problems);
        Assert.Equal(5, problems.Count);
    }

    [Fact]
    public void A_disputed_statement_may_be_published_as_disputed_and_a_withdrawn_one_may_not()
    {
        ReviewStatus[] states = [ReviewStatus.Disputed, ReviewStatus.Withdrawn, ReviewStatus.Proposed];
        var refused = states.Select(state =>
        {
            var story = new SmallStory().Published();
            var visits = story.Relations[0];
            story.Relations[0] = new Relation { Id = visits.Id, From = visits.From, To = visits.To, Type = visits.Type, Label = visits.Label, Scope = visits.Scope, Sources = visits.Sources, Review = state, Level = visits.Level };
            try
            {
                story.Build();
                return false;
            }
            catch (CatalogueException)
            {
                return true;
            }
        });

        Assert.Equal([false, true, true], refused);
    }

    [Fact]
    public void An_editorial_sample_may_hold_statements_in_any_state_of_review()
    {
        var story = new SmallStory();
        var visits = story.Relations[0];
        story.Relations[0] = new Relation { Id = visits.Id, From = visits.From, To = visits.To, Type = visits.Type, Label = visits.Label, Scope = visits.Scope, Sources = visits.Sources, Review = ReviewStatus.Proposed, Level = visits.Level };

        Assert.NotNull(story.Build());
    }

    [Fact]
    public void The_public_sees_only_the_media_that_are_allowed_for_the_public()
    {
        var story = new SmallStory().Published();
        story.Media.Clear();
        story.Media.Add(SmallStory.Diagram("for-everybody", forPublic: true));
        story.Media.Add(SmallStory.Diagram("for-the-demonstration"));

        Assert.Equal(["for-everybody"], Public(story)!.Media.Select(item => item.Id.Value));
        Assert.Equal(["for-everybody"], Public(story)!.Timeline().Made.Select(item => item.Id.Value));
        Assert.Empty(Public(story, 1)!.Media);
        Assert.Equal(
            ["for-everybody", "for-the-demonstration"],
            Projection.Of(story.Build(), new ReadingLevel(2), Audience.LocalDemonstration)!.Media.Select(item => item.Id.Value));
    }

    [Fact]
    public void An_item_that_is_shown_to_the_public_needs_rights_that_allow_it_and_what_showing_it_needs()
    {
        var unverified = new SmallStory();
        unverified.Media.Add(SmallStory.Diagram("museum", allowed: false, rights: RightsStatus.PermissionNotVerified, file: null, forPublic: true));
        var bare = new SmallStory();
        bare.Media.Add(SmallStory.Diagram("bare", allowed: false, rights: RightsStatus.OpenAccessVerified, file: null, altText: AltTextStatus.NotPrepared, forPublic: true));
        var hidden = new SmallStory();
        hidden.Media.Add(SmallStory.Diagram("hidden", allowed: false, forPublic: true));

        Assert.Contains("museum: only original project content and items with verified open access are shown to the public.", unverified.Problems());
        Assert.Contains("museum: an item without established permission has no file and is not shown.", unverified.Problems());
        Assert.Contains("bare: an item that is shown needs its file.", bare.Problems());
        Assert.Contains("bare: an image that is shown needs a checked text alternative.", bare.Problems());
        Assert.Contains("hidden: an item that is shown to the public is shown in a demonstration as well.", hidden.Problems());
    }

    [Fact]
    public void An_open_access_item_may_be_shown_to_the_public_without_being_part_of_a_demonstration()
    {
        var story = new SmallStory().Published();
        story.Media.Add(SmallStory.Diagram("open", allowed: false, rights: RightsStatus.OpenAccessVerified, forPublic: true));

        Assert.Contains(Public(story)!.Media, item => item.Id.Value == "open");
        Assert.DoesNotContain(Projection.Of(story.Build(), new ReadingLevel(2), Audience.LocalDemonstration)!.Media, item => item.Id.Value == "open");
    }
}
