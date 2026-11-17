namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// Disputed and withdrawn statements (step 10.2, GOV-004): a disputed statement says what the sources disagree
/// about, a withdrawn one is no part of a collection and is told by the history, with its reason.
/// </summary>
public sealed class DisputedAndWithdrawnTests
{
    private static readonly DateOnly Published = new(2026, 11, 17);

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

    [Fact]
    public void A_disputed_statement_of_a_revision_says_what_the_sources_disagree_about()
    {
        var story = new SmallStory().Published();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.Disputed, new ReviewRecord("an editor", Published));

        Assert.Equal(["visits: the review of a disputed statement says in its note what the sources disagree about."], story.Problems());
    }

    [Fact]
    public void The_evidence_of_a_disputed_statement_tells_the_dispute_and_not_who_reviewed()
    {
        var story = new SmallStory().Published();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.Disputed, new ReviewRecord("an editor", Published, "Two manuscripts name different towns."));
        var projection = Projection.Of(story.Build(), ReadingLevel.Opening, Audience.Public)!;

        var disputed = projection.Evidence(new RelationId("visits"))!;
        var settled = projection.Evidence(new EventId("arrival"))!;

        Assert.Equal((ReviewStatus.Disputed, "Two manuscripts name different towns."), (disputed.Review, disputed.Dispute));
        Assert.Equal((ReviewStatus.Reviewed, null), (settled.Review, settled.Dispute));
    }

    [Fact]
    public void The_note_of_a_review_that_found_no_dispute_is_not_told_as_one()
    {
        var story = new SmallStory().Published();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.Reviewed, new ReviewRecord("an editor", Published, "Read against the text."));

        var evidence = Projection.Of(story.Build(), ReadingLevel.Opening, Audience.Public)!.Evidence(new RelationId("visits"))!;

        Assert.Null(evidence.Dispute);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_withdrawn_statement_is_no_part_of_a_collection(bool published)
    {
        var story = published ? new SmallStory().Published() : new SmallStory();
        story.Relations[0] = With(story.Relations[0], ReviewStatus.Withdrawn, new ReviewRecord("an editor", Published, "The source does not say this."));

        Assert.Equal(["visits: a withdrawn statement is no part of a collection; the history of revisions names it."], story.Problems());
    }

    [Fact]
    public void A_withdrawal_in_the_history_says_why_and_nothing_else_has_a_reason()
    {
        RevisionChange Change(RevisionChangeKind kind, string? reason) =>
            new(kind, "old-claim", "relation", SmallStory.Text("An old claim"), ReadingLevel.Opening, reason);

        var story = new SmallStory().Published();

        story.History = [new RevisionEntry(1, Published, null, [Change(RevisionChangeKind.Withdrawn, "The source does not say this.")])];
        Assert.Equal("The source does not say this.", story.Build().Info.History[0].Changes[0].Reason);

        story.History = [new RevisionEntry(1, Published, null, [Change(RevisionChangeKind.Withdrawn, null)])];
        Assert.Equal(["history of old-claim: a withdrawal says why, and only a withdrawal has a reason."], story.Problems());

        story.History = [new RevisionEntry(1, Published, null, [Change(RevisionChangeKind.Removed, "No reason belongs here.")])];
        Assert.Equal(["history of old-claim: a withdrawal says why, and only a withdrawal has a reason."], story.Problems());
    }

    [Fact]
    public void A_withdrawal_of_a_later_part_is_told_to_nobody_who_has_not_read_that_far()
    {
        var story = new SmallStory().Published();
        story.History =
        [
            new RevisionEntry(1, Published, null,
            [
                new RevisionChange(RevisionChangeKind.Withdrawn, "old-secret", "relation", SmallStory.Text("A secret"), new ReadingLevel(2), "A reason that tells the secret."),
            ]),
        ];
        var collection = story.Build();

        Assert.Empty(Projection.Of(collection, ReadingLevel.Opening, Audience.Public)!.Revisions()[0].Changes);
        Assert.Equal("A reason that tells the secret.", Projection.Of(collection, new ReadingLevel(2), Audience.Public)!.Revisions()[0].Changes[0].Reason);
    }
}
