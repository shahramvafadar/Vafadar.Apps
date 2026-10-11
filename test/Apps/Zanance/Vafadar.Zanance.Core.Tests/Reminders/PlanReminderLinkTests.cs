using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reminders;

namespace Vafadar.Zanance.Core.Tests.Reminders;

/// <summary>AT-142: bounded original notification identities, independent of due-date changes or cached text.</summary>
[Trait("AT", "AT-142")]
public sealed class PlanReminderLinkTests
{
    [Theory]
    [InlineData(1), InlineData(30)]
    public void Scoped_members_round_trip_without_exposing_a_mutable_collection(int count)
    {
        var originals = Enumerable.Range(0, count).Select(i => new PlanReminderTarget(Guid.NewGuid(), new DateOnly(2026, 10, 10).AddDays(i))).ToArray();
        var parsed = PlanReminderLink.Parse(PlanReminderLink.Format(new string('a', 64), originals))!;
        Assert.Equal(originals, parsed.Targets); originals[0] = new(Guid.NewGuid(), null);
        Assert.False(parsed.IsContract); Assert.NotEqual(originals[0], parsed.Targets[0]);
    }

    [Fact]
    public void Contract_identity_has_no_occurrence_date_and_cannot_mix_with_an_occurrence_group()
    {
        var contract = new PlanReminderTarget(Guid.NewGuid(), null);
        Assert.True(PlanReminderLink.Parse(PlanReminderLink.Format(new string('a', 64), [contract]))!.IsContract);
        Assert.Throws<ArgumentException>(() => PlanReminderLink.Format(new string('a', 64), [contract, new(Guid.NewGuid(), new(2026, 10, 10))]));
    }

    [Theory]
    [InlineData("empty"), InlineData("oversized"), InlineData("duplicate"), InlineData("no-identity"), InlineData("no-scope")]
    public void Formatting_rejects_unsafe_identity_sets(string kind)
    {
        var target = new PlanReminderTarget(Guid.NewGuid(), new(2026, 10, 10));
        PlanReminderTarget[] targets = kind switch
        {
            "empty" => [], "oversized" => Enumerable.Range(0, 31).Select(_ => new PlanReminderTarget(Guid.NewGuid(), target.OriginalDate)).ToArray(),
            "duplicate" => [target, target], "no-identity" => [new(Guid.Empty, target.OriginalDate)], _ => [target],
        };
        Assert.Throws<ArgumentException>(() => PlanReminderLink.Format(kind == "no-scope" ? "" : new string('a', 64), targets));
    }

    [Theory]
    [InlineData("plans"), InlineData(""), InlineData("plan-reminder-v1|a|bad")]
    [InlineData("plan-reminder-v2|aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa|11111111222233334444555555555555@2026-10-10")]
    [InlineData("plan-reminder-v1|aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa|11111111222233334444555555555555@2026-02-30")]
    [InlineData("plan-reminder-v1|aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa|00000000000000000000000000000000@2026-10-10")]
    public void Unsupported_or_malformed_payloads_are_not_guessed(string link) => Assert.Null(PlanReminderLink.Parse(link));

    [Fact]
    public void Real_hashed_occurrence_ids_and_second_reminders_always_have_a_distinct_stable_snooze()
    {
        var identity = Guid.Parse("11111111-2222-3333-4444-555555555555");
        for (var i = 0; i < 100; i++)
        {
            var id = ReminderPlanner.StableId(identity, new DateOnly(2026, 10, 10).AddDays(i));
            Assert.NotEqual(id, ReminderSnoozes.IdFor(id));
            var second = ReminderPlanner.DueDateId(id); Assert.NotEqual(second, ReminderSnoozes.IdFor(second));
            Assert.Equal(ReminderSnoozes.IdFor(id), ReminderSnoozes.IdFor(ReminderSnoozes.IdFor(id)));
        }
    }

    [Fact]
    public void Resolution_preserves_original_rule_identity_when_effective_due_date_moves_beyond_the_range_window()
    {
        var original = new DateOnly(2026, 10, 10); var plan = new Schedule { Name = "Owned plan", Rule = new() { Start = original } };
        var occurrence = Occurrences.Find(plan, [new() { ScheduleId = plan.Id, OriginalDate = original, DueDate = original.AddYears(2), Amount = 1234 }], original, original)!;
        Assert.Equal(original, occurrence.OriginalDate); Assert.Equal(original.AddYears(2), occurrence.DueDate); Assert.Equal(1234, occurrence.Amount);
        Assert.Null(Occurrences.Find(plan, [], original.AddDays(1), original));
        plan.ActiveUntil = original.AddDays(-1); Assert.Null(Occurrences.Find(plan, [], original, original));
    }
}
