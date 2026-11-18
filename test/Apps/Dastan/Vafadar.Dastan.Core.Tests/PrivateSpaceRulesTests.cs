using Vafadar.Dastan.Core.Personal;
using Vafadar.Temporal;

namespace Vafadar.Dastan.Core.Tests;

/// <summary>
/// The rules of private spaces (step 15.2, document 06): the ten acceptance examples of the document, as far as
/// rules can say them without an account system, and the rules they rest on. Everybody here is made up.
/// </summary>
public sealed class PrivateSpaceRulesTests
{
    private static readonly AccountId Mina = new("mina");
    private static readonly AccountId Omid = new("omid");
    private static readonly AccountId Sara = new("sara");
    private static readonly AccountId Stranger = new("stranger");
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly EntityId University = new("university-of-the-tests");

    private static CalendarPosition Y(int year) => CalendarPosition.OfYear(CalendarSystem.Unspecified, Era.CommonEra, year);

    private static YearOfEra Ce(int year) => new(Era.CommonEra, year);

    private static SocialRelationship Friendship()
    {
        var friendship = new SocialRelationship("friendship-1", Mina, Omid, "close friend");
        Assert.Equal(ActOutcome.Done, friendship.Request(Mina));
        return friendship;
    }

    // --- 1. Friendship ---

    [Fact]
    public void A_friendship_that_is_asked_for_is_seen_by_its_two_sides_only()
    {
        var friendship = Friendship();

        Assert.True(friendship.IsVisibleTo(Mina));
        Assert.True(friendship.IsVisibleTo(Omid));
        Assert.False(friendship.IsVisibleTo(Sara));
        Assert.False(friendship.IsVisibleTo(null));
    }

    [Fact]
    public void After_acceptance_only_who_both_sides_allow_sees_it()
    {
        var friendship = Friendship();
        Assert.Equal(ActOutcome.Done, friendship.Accept(Omid));

        // Accepted, and still shown to nobody else: acceptance is not visibility to everyone.
        Assert.False(friendship.IsVisibleTo(Sara));

        // One side opens it to everybody; the other to a circle. The narrower one holds.
        Assert.Equal(ActOutcome.Done, friendship.Show(Mina, AccessScope.Everybody(Mina)));
        Assert.False(friendship.IsVisibleTo(Sara));
        Assert.Equal(ActOutcome.Done, friendship.Show(Omid, AccessScope.Circle(Omid, Sara)));
        Assert.True(friendship.IsVisibleTo(Sara));
        Assert.False(friendship.IsVisibleTo(Stranger));
        Assert.False(friendship.IsVisibleTo(null));
    }

    [Fact]
    public void Nobody_accepts_for_somebody_else_and_nobody_sets_another_sides_audience()
    {
        var friendship = Friendship();

        Assert.Equal(ActOutcome.Refused, friendship.Accept(Mina));
        Assert.Equal(ActOutcome.Refused, friendship.Accept(Sara));
        Assert.Equal(ActOutcome.Refused, friendship.Show(Mina, AccessScope.Everybody(Omid)));
        Assert.Equal(ActOutcome.Refused, friendship.Show(Sara, AccessScope.Everybody(Sara)));
        Assert.Equal(RelationshipState.Requested, friendship.State);
    }

    [Fact]
    public void An_act_done_twice_changes_nothing_and_tells_nobody_again()
    {
        var friendship = Friendship();

        Assert.Equal(ActOutcome.AlreadyDone, friendship.Request(Mina));
        Assert.Equal(ActOutcome.Done, friendship.Accept(Omid));
        Assert.Equal(ActOutcome.AlreadyDone, friendship.Accept(Omid));

        Assert.Equal(2, friendship.Notifications);
        Assert.Equal(RelationshipState.Accepted, friendship.State);
    }

    [Fact]
    public void An_acceptance_that_comes_after_the_request_was_taken_back_creates_nothing()
    {
        var friendship = Friendship();
        Assert.Equal(ActOutcome.Done, friendship.Cancel(Mina));

        Assert.Equal(ActOutcome.Refused, friendship.Accept(Omid));
        Assert.Equal(RelationshipState.Cancelled, friendship.State);
        Assert.False(friendship.IsVisibleTo(Sara));
    }

    // --- 2. Invitation ---

    [Fact]
    public void Somebody_who_registers_by_an_invitation_has_an_account_and_no_relationship()
    {
        var invitation = new Invitation("inv-1", Mina, InvitationPurpose.Join, Today.AddDays(14));
        var friendship = new SocialRelationship("friendship-2", Mina, Omid, "close friend");
        friendship.Request(Mina);

        Assert.Equal(ActOutcome.Done, invitation.Use(Omid, Today));
        Assert.Equal(ActOutcome.Done, friendship.Reject(Omid));

        // The account exists; the relationship does not become visible to anybody else.
        Assert.Equal((InvitationState.Used, Omid), (invitation.State, invitation.UsedBy));
        Assert.Equal(RelationshipState.Rejected, friendship.State);
        Assert.False(friendship.IsVisibleTo(Sara));
        Assert.False(friendship.IsVisibleTo(null));
    }

    [Fact]
    public void An_invitation_is_used_once_runs_out_and_can_be_taken_back()
    {
        var used = new Invitation("inv-2", Mina, InvitationPurpose.Join, Today.AddDays(14));
        var late = new Invitation("inv-3", Mina, InvitationPurpose.Join, Today.AddDays(-1));
        var taken = new Invitation("inv-4", Mina, InvitationPurpose.Join, Today.AddDays(14));

        Assert.Equal(ActOutcome.Done, used.Use(Omid, Today));
        Assert.Equal(ActOutcome.AlreadyDone, used.Use(Omid, Today));
        Assert.Equal(ActOutcome.Refused, used.Use(Sara, Today));
        Assert.Equal(ActOutcome.Refused, used.Revoke(Mina));

        Assert.Equal(ActOutcome.Refused, late.Use(Omid, Today));

        Assert.Equal(ActOutcome.Refused, taken.Revoke(Omid));
        Assert.Equal(ActOutcome.Done, taken.Revoke(Mina));
        Assert.Equal(ActOutcome.Refused, taken.Use(Omid, Today));
    }

    // --- 3. Identity ---

    [Fact]
    public void Two_records_with_one_mail_address_are_a_suggestion_and_no_merge()
    {
        var one = new PersonRecord(new PersonId("p-1"), "S. Example", "sara@example.test", AccessScope.OnlyMe(Mina));
        var other = new PersonRecord(new PersonId("p-2"), "Sara Example", "SARA@example.test ", AccessScope.Circle(Omid, Sara));
        var third = new PersonRecord(new PersonId("p-3"), "Somebody else", null, AccessScope.OnlyMe(Omid));

        var suggestion = Assert.Single(Matching.Suggest([one, other, third]));

        Assert.Equal((one.Id, other.Id, "the same mail address"), (suggestion.One, suggestion.Other, suggestion.Clue));

        // Nothing was merged: each record still has its own scope, and neither grew.
        Assert.False(one.Scope.Allows(Sara));
        Assert.False(other.Scope.Allows(Mina));
    }

    [Fact]
    public void A_claim_on_a_record_is_confirmed_by_somebody_else_and_once()
    {
        var claim = new IdentityClaim(new PersonId("p-1"), Sara);

        Assert.Equal(ActOutcome.Refused, claim.Review(Sara, confirmed: true));
        Assert.Equal(ClaimState.Requested, claim.State);
        Assert.Equal(ActOutcome.Done, claim.Review(Mina, confirmed: true));
        Assert.Equal((ClaimState.Confirmed, Mina), (claim.State, claim.ReviewedBy));
        Assert.Equal(ActOutcome.Refused, claim.Review(Omid, confirmed: false));
    }

    // --- 4. University ---

    private static Affiliation Studied(string person, AccountId account, int from, int to, AccessScope? findable = null, AccessScope? dates = null, int? graduated = null) => new(
        new PersonId(person),
        account,
        University,
        new Period(PeriodStart.At(Y(from)), PeriodEnd.At(Y(to))),
        graduated is { } year ? new Occurrence(Y(year)) : null,
        findable ?? AccessScope.Everybody(account),
        dates ?? AccessScope.Everybody(account));

    [Fact]
    public void Somebody_whose_affiliation_is_private_is_in_no_result_and_in_no_count()
    {
        // Sara's name may be seen elsewhere; that she studied here is hers alone.
        var affiliations = new[]
        {
            Studied("p-mina", Mina, 2001, 2005),
            Studied("p-sara", Sara, 2001, 2005, findable: AccessScope.OnlyMe(Sara)),
        };

        var seenByOmid = AffiliationSearch.Find(affiliations, University, Ce(2003), AttendanceQuestion.AttendedDuring, Omid);
        var seenByNobody = AffiliationSearch.Find(affiliations, University, Ce(2003), AttendanceQuestion.AttendedDuring, null);
        var seenBySara = AffiliationSearch.Find(affiliations, University, Ce(2003), AttendanceQuestion.AttendedDuring, Sara);

        Assert.Equal(["p-mina"], seenByOmid.People.Select(found => found.Person.Value));
        Assert.Equal(1, seenByOmid.Count);
        Assert.Equal(1, seenByNobody.Count);
        Assert.Equal(2, seenBySara.Count);
        Assert.Equal(1, AffiliationSearch.Find(affiliations, University, Omid).Count);
    }

    [Fact]
    public void Who_hides_the_dates_is_found_at_the_place_and_by_no_year()
    {
        var affiliations = new[] { Studied("p-mina", Mina, 2001, 2005, dates: AccessScope.OnlyMe(Mina)) };

        Assert.Equal(1, AffiliationSearch.Find(affiliations, University, Omid).Count);
        Assert.Equal(0, AffiliationSearch.Find(affiliations, University, Ce(2003), AttendanceQuestion.AttendedDuring, Omid).Count);
        Assert.Equal(0, AffiliationSearch.Find(affiliations, University, Ce(2001), AttendanceQuestion.EnteredIn, Omid).Count);
        Assert.Equal(1, AffiliationSearch.Find(affiliations, University, Ce(2001), AttendanceQuestion.EnteredIn, Mina).Count);
    }

    // --- 5. Time ---

    [Fact]
    public void Attending_during_a_year_entering_in_it_and_graduating_in_it_are_three_questions()
    {
        var affiliations = new[]
        {
            Studied("p-mina", Mina, 2001, 2005, graduated: 2005),
            Studied("p-omid", Omid, 2003, 2007),
        };

        string[] Found(int year, AttendanceQuestion question) =>
            [.. AffiliationSearch.Find(affiliations, University, Ce(year), question, Sara).People.Select(found => found.Person.Value)];

        // Both attended in 2004; only one entered in 2003; one graduation is recorded, and an attendance that
        // ended is no graduation.
        Assert.Equal(["p-mina", "p-omid"], Found(2004, AttendanceQuestion.AttendedDuring));
        Assert.Equal(["p-omid"], Found(2003, AttendanceQuestion.EnteredIn));
        Assert.Equal(["p-mina"], Found(2001, AttendanceQuestion.EnteredIn));
        Assert.Equal(["p-mina"], Found(2005, AttendanceQuestion.GraduatedIn));
        Assert.Empty(Found(2007, AttendanceQuestion.GraduatedIn));
        Assert.Empty(Found(2010, AttendanceQuestion.AttendedDuring));
    }

    [Fact]
    public void An_attendance_known_only_between_two_times_is_found_as_possible()
    {
        var uncertain = new Affiliation(
            new PersonId("p-sara"), Sara, University,
            new Period(PeriodStart.Between(Y(2000), Y(2002)), PeriodEnd.Unknown), null,
            AccessScope.Everybody(Sara), AccessScope.Everybody(Sara));

        var early = AffiliationSearch.Find([uncertain], University, Ce(2001), AttendanceQuestion.AttendedDuring, Omid);
        var entered = AffiliationSearch.Find([uncertain], University, Ce(2001), AttendanceQuestion.EnteredIn, Omid);

        Assert.Equal(Holding.Possibly, Assert.Single(early.People).Holding);
        Assert.Equal(Holding.Possibly, Assert.Single(entered.People).Holding);
        Assert.Empty(AffiliationSearch.Find([uncertain], University, Ce(1999), AttendanceQuestion.AttendedDuring, Omid).People);
    }

    // --- 6. Photo ---

    [Fact]
    public void An_approved_tag_shows_a_photograph_to_nobody_who_could_not_see_it_before()
    {
        var omidRecord = new PersonRecord(new PersonId("p-omid"), "Omid", null, AccessScope.Everybody(Omid));
        var photo = new SharedMedia("photo-1", AccessScope.Circle(Mina, Omid), new DateOnly(2010, 5, 1), Today);
        photo.Tag(omidRecord.Id, Omid);

        // Sara is Omid's friend and has no access to Mina's photograph.
        Assert.False(photo.IsInLifeOf(omidRecord, Omid));
        Assert.Equal(ActOutcome.Refused, photo.Approve(omidRecord.Id, Sara));
        Assert.Equal(ActOutcome.Done, photo.Approve(omidRecord.Id, Omid));

        Assert.True(photo.IsInLifeOf(omidRecord, Omid));
        Assert.True(photo.IsInLifeOf(omidRecord, Mina));
        Assert.False(photo.IsInLifeOf(omidRecord, Sara));
        Assert.False(photo.MayOpen(Sara, MediaPart.Thumbnail));
        Assert.False(photo.IsInLifeOf(omidRecord, null));

        // The tag taken away, the photograph is in his life for nobody – and still Mina's to show.
        Assert.Equal(ActOutcome.Done, photo.RemoveTag(omidRecord.Id, Omid));
        Assert.False(photo.IsInLifeOf(omidRecord, Mina));
        Assert.True(photo.MayOpen(Omid));
    }

    // --- 7. Publication ---

    [Fact]
    public void Before_a_private_story_is_made_public_what_stands_in_the_way_is_named()
    {
        var hero = new PersonRecord(new PersonId("p-grandfather"), "A grandfather", null, AccessScope.Everybody(Mina));
        var living = new PersonRecord(new PersonId("p-cousin"), "A cousin", null, AccessScope.Circle(Mina, Omid));
        var friendship = Friendship();
        friendship.Accept(Omid);
        var open = new SharedMedia("photo-public", AccessScope.Everybody(Mina), new DateOnly(1950, 1, 1), Today);
        var closed = new SharedMedia("photo-family", AccessScope.Circle(Mina, Omid), new DateOnly(1950, 1, 1), Today);

        var blockers = PublicationCheck.Blockers([hero, living], [friendship], [open, closed]);

        Assert.Equal(["person p-cousin", "relationship friendship-1", "media photo-family"], blockers.Select(finding => finding.What));
        Assert.Equal(5, PublicationCheck.Of([hero, living], [friendship], [open, closed]).Length);

        // The check published nothing and changed nothing.
        Assert.False(closed.MayOpen(null));
        Assert.False(friendship.IsVisibleTo(null));
    }

    // --- 8. Revocation ---

    [Fact]
    public void After_access_was_taken_away_nothing_is_returned_any_more()
    {
        var friendship = Friendship();
        friendship.Accept(Omid);
        friendship.Show(Mina, AccessScope.Circle(Mina, Sara));
        friendship.Show(Omid, AccessScope.Circle(Omid, Sara));
        var photo = new SharedMedia("photo-2", AccessScope.Circle(Mina, Sara), new DateOnly(2020, 1, 1), Today);
        var affiliations = new[] { Studied("p-mina", Mina, 2001, 2005, findable: AccessScope.Circle(Mina, Sara)) };
        Assert.True(friendship.IsVisibleTo(Sara));
        Assert.True(photo.MayOpen(Sara));
        Assert.Equal(1, AffiliationSearch.Find(affiliations, University, Sara).Count);

        // Mina takes Sara out of every circle.
        friendship.Show(Mina, friendship.FromScope.Without(Sara));
        photo.Share(Mina, photo.Scope.Without(Sara));
        affiliations = [affiliations[0] with { FindableHere = affiliations[0].FindableHere.Without(Sara) }];

        Assert.False(friendship.IsVisibleTo(Sara));
        Assert.All(Enum.GetValues<MediaPart>(), part => Assert.False(photo.MayOpen(Sara, part)));
        Assert.Equal(0, AffiliationSearch.Find(affiliations, University, Sara).Count);
        Assert.Empty(SharedGallery.RecentShares([photo], Today.AddDays(-7), Sara));
    }

    [Fact]
    public void A_relationship_that_ended_and_an_item_that_was_withdrawn_are_shown_to_nobody_else()
    {
        var friendship = Friendship();
        friendship.Accept(Omid);
        friendship.Show(Mina, AccessScope.Everybody(Mina));
        friendship.Show(Omid, AccessScope.Everybody(Omid));
        var photo = new SharedMedia("photo-3", AccessScope.Everybody(Mina), new DateOnly(2020, 1, 1), Today);
        Assert.True(friendship.IsVisibleTo(null));

        Assert.Equal(ActOutcome.Refused, friendship.End(Sara));
        Assert.Equal(ActOutcome.Done, friendship.End(Omid));
        Assert.Equal(ActOutcome.Refused, photo.Withdraw(Omid));
        Assert.Equal(ActOutcome.Done, photo.Withdraw(Mina));

        Assert.False(friendship.IsVisibleTo(null));
        Assert.False(friendship.IsVisibleTo(Sara));
        Assert.True(friendship.IsVisibleTo(Mina));
        Assert.False(photo.MayOpen(null));
        Assert.False(photo.MayOpen(Mina));
    }

    // --- 9. Recent shares ---

    [Fact]
    public void An_old_photograph_shared_lately_is_a_recent_share_and_keeps_the_day_it_was_taken()
    {
        var old = new SharedMedia("photo-old", AccessScope.Circle(Mina, Omid), new DateOnly(1985, 3, 20), Today);
        var earlier = new SharedMedia("photo-earlier", AccessScope.Circle(Mina, Omid), new DateOnly(2025, 1, 1), Today.AddDays(-40));
        var friendship = new SocialRelationship("friendship-3", Mina, Sara, "close friend");
        friendship.Request(Mina);
        friendship.Accept(Sara);

        var forOmid = SharedGallery.RecentShares([earlier, old], Today.AddDays(-7), Omid);
        var forSara = SharedGallery.RecentShares([earlier, old], Today.AddDays(-7), Sara);

        Assert.Equal(["photo-old"], forOmid.Select(item => item.Id));
        Assert.Equal(new DateOnly(1985, 3, 20), forOmid[0].Taken);

        // A friendship accepted just now grants no audience the share did not choose.
        Assert.Equal(RelationshipState.Accepted, friendship.State);
        Assert.Empty(forSara);
    }

    // --- 10. Video route ---

    [Fact]
    public void Who_may_not_see_a_video_gets_no_part_of_it_by_any_way()
    {
        var video = new SharedMedia("video-1", AccessScope.Circle(Mina, Omid), new DateOnly(2024, 6, 1), Today);

        Assert.All(Enum.GetValues<MediaPart>(), part =>
        {
            Assert.True(video.MayOpen(Omid, part));
            Assert.False(video.MayOpen(Sara, part));
            Assert.False(video.MayOpen(null, part));
        });
        Assert.False(video.MayOpen(Omid, (MediaPart)99));
        Assert.Empty(SharedGallery.RecentShares([video], Today.AddDays(-7), Sara));
    }

    // --- The scope every rule rests on ---

    [Fact]
    public void A_scope_names_accounts_and_a_viewer_without_one_sees_what_is_public()
    {
        Assert.True(AccessScope.OnlyMe(Mina).Allows(Mina));
        Assert.False(AccessScope.OnlyMe(Mina).Allows(Omid));
        Assert.False(AccessScope.OnlyMe(Mina).Allows(null));
        Assert.True(AccessScope.Circle(Mina, Omid).Allows(Omid));
        Assert.True(AccessScope.Circle(Mina, Omid).Allows(Mina));
        Assert.False(AccessScope.Circle(Mina, Omid).Allows(Sara));
        Assert.False(AccessScope.Circle(Mina, Omid).Allows(null));
        Assert.True(AccessScope.Everybody(Mina).Allows(null));
        Assert.False(AccessScope.Circle(Mina, Omid).Without(Omid).Allows(Omid));
    }

    [Fact]
    public void Only_the_owner_of_an_item_changes_who_sees_it()
    {
        var photo = new SharedMedia("photo-4", AccessScope.OnlyMe(Mina), new DateOnly(2020, 1, 1), Today);

        Assert.Equal(ActOutcome.Refused, photo.Share(Omid, AccessScope.Everybody(Omid)));
        Assert.Equal(ActOutcome.Refused, photo.Share(Mina, AccessScope.Everybody(Omid)));
        Assert.False(photo.MayOpen(Omid));
        Assert.Equal(ActOutcome.Done, photo.Share(Mina, AccessScope.Circle(Mina, Omid)));
        Assert.True(photo.MayOpen(Omid));
    }
}
