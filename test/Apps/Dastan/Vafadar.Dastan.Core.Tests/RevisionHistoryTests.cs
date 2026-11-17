using System.Collections.Immutable;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The record of a review and the history of revisions (step 10.1, GOV-005): a published revision says who
/// reviewed each statement and when, and what each revision changed – to a visitor only as far as the visitor
/// may see the records that changed.
/// </summary>
public sealed class RevisionHistoryTests
{
    private static readonly DateOnly Published = new(2026, 11, 17);

    private static RevisionChange Change(RevisionChangeKind kind, string record, string recordKind, int level) =>
        new(kind, record, recordKind, SmallStory.Text($"The {record}"), new ReadingLevel(level));

    private static RevisionEntry Entry(int number, DateOnly? day = null, params RevisionChange[] changes) =>
        new(number, day ?? Published, null, [.. changes]);

    private static Relation With(Relation relation, ReviewStatus review, ReviewRecord? record) => new()
    {
        Id = relation.Id,
        From = relation.From,
        To = relation.To,
        Type = relation.Type,
        Label = relation.Label,
        Scope = relation.Scope,
        Sources = relation.Sources,
        Review = review,
        Reviewed = record,
        Level = relation.Level,
    };

    // --- the record of a review ---

    [Fact]
    public void A_published_revision_says_who_reviewed_each_statement_and_when()
    {
        var story = new SmallStory().Published();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.Reviewed, null);

        Assert.Equal(["visits: a published revision says who reviewed a statement and when."], story.Problems());
    }

    [Fact]
    public void A_review_is_not_dated_after_the_revision_it_is_published_in()
    {
        var story = new SmallStory().Published();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.Reviewed, new ReviewRecord("an editor", Published.AddDays(1)));

        Assert.Equal(["visits: the review is dated after the revision was published."], story.Problems());
    }

    [Fact]
    public void A_review_is_recorded_only_for_a_statement_that_was_reviewed()
    {
        var story = new SmallStory();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.SourcedReviewPending, new ReviewRecord("an editor", Published));

        Assert.Equal(["visits: a review is recorded for a statement that is SourcedReviewPending."], story.Problems());
    }

    [Fact]
    public void An_editorial_sample_may_hold_reviewed_statements_with_their_record_before_it_is_published()
    {
        var story = new SmallStory();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.Reviewed, new ReviewRecord("an editor", Published, "Checked against the text."));

        var relation = story.Build().Find(new RelationId("visits"))!;

        Assert.Equal(new ReviewRecord("an editor", Published, "Checked against the text."), relation.Reviewed);
    }

    // --- the history ---

    [Fact]
    public void The_history_of_a_revision_names_every_revision_up_to_it_in_order()
    {
        var story = new SmallStory().Published();
        story.Publication = story.Publication with { Revision = new PublicationRevision(3, Published) };

        story.History = [Entry(1, Published.AddDays(-9)), Entry(2, Published.AddDays(-4)), Entry(3)];
        Assert.Equal(3, story.Build().Info.History.Length);

        story.History = [Entry(1), Entry(3)];
        Assert.Contains("collection: the history names the revisions from 1 on, in their order and without a gap.", story.Problems());

        story.History = [Entry(1), Entry(2)];
        Assert.Contains("collection: the last entry of the history is the revision the collection is.", story.Problems());

        story.History = [Entry(1), Entry(2, Published.AddDays(-30)), Entry(3)];
        Assert.Contains("collection: revision 2 is dated before the revision it follows.", story.Problems());
    }

    [Fact]
    public void An_editorial_sample_has_no_history()
    {
        var story = new SmallStory { History = [Entry(1)] };

        Assert.Contains("collection: a history of revisions belongs to a published revision.", story.Problems());
    }

    [Fact]
    public void A_change_of_the_history_has_a_reading_level_of_the_collection()
    {
        var story = new SmallStory().Published();
        story.History = [Entry(1, null, Change(RevisionChangeKind.Added, "ghost", "entity", 9))];

        Assert.Contains("history of ghost: the reading level 9 is not a stage of the collection.", story.Problems());
    }

    [Fact]
    public void A_visitor_gets_the_revisions_newest_first_with_the_changes_of_what_the_visitor_may_see()
    {
        var story = new SmallStory().Published();
        story.Publication = story.Publication with { Revision = new PublicationRevision(2, Published) };
        story.History =
        [
            Entry(1, Published.AddDays(-5)),
            Entry(
                2,
                null,
                Change(RevisionChangeKind.Changed, "town", "entity", 0),
                Change(RevisionChangeKind.Added, "child", "entity", 2),
                Change(RevisionChangeKind.Removed, "old-secret", "relation", 2)),
        ];
        var collection = story.Build();

        var opening = Projection.Of(collection, ReadingLevel.Opening, Audience.Public)!.Revisions();
        var all = Projection.Of(collection, new ReadingLevel(2), Audience.Public)!.Revisions();

        Assert.Equal([2, 1], opening.Select(entry => entry.Number));
        Assert.Equal(["town"], opening[0].Changes.Select(change => change.Record));
        Assert.Equal(["town", "child", "old-secret"], all[0].Changes.Select(change => change.Record));
        Assert.Empty(all[1].Changes);
    }

    [Fact]
    public void A_collection_that_is_no_revision_has_no_revisions_to_show()
    {
        var revisions = Projection.Of(new SmallStory().Build(), new ReadingLevel(2), Audience.LocalDemonstration)!.Revisions();

        Assert.Equal(ImmutableArray<RevisionEntry>.Empty, revisions);
    }
}
