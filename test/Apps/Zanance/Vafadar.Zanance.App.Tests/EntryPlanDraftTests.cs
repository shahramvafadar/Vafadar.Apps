using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Repeat creates a detached monthly plan and preserves the original date, currency and positive money.</summary>
public sealed class EntryPlanDraftTests
{
    [Theory, Trait("AT", "AT-116")]
    [InlineData(EntryKind.Expense, PeriodCalendar.Gregorian)]
    [InlineData(EntryKind.Expense, PeriodCalendar.Persian)]
    [InlineData(EntryKind.Expense, PeriodCalendar.Hijri)]
    [InlineData(EntryKind.Income, PeriodCalendar.Gregorian)]
    [InlineData(EntryKind.Transfer, PeriodCalendar.Gregorian)]
    public void Repeat_keeps_the_selected_first_date_and_creates_only_a_detached_schedule(EntryKind kind, PeriodCalendar calendar)
    {
        var draft = Fixture() with { Kind = kind };
        var plan = draft.CreateSchedule(calendar);

        Assert.Equal(draft.Kind, plan.Kind);
        Assert.Equal(draft.Name, plan.Name);
        Assert.Equal(draft.AccountId, plan.AccountId);
        Assert.Equal(draft.Amount, plan.Amount);
        Assert.Equal(draft.Note, plan.Note);
        Assert.Equal(draft.Date, plan.Rule.Start);
        Assert.Equal(calendar, plan.Rule.Calendar);
        Assert.Equal(Frequency.Monthly, plan.Rule.Frequency);
        Assert.Equal(MonthDayRule.SpecificDay, plan.Rule.DayRule);
        Assert.Equal(AmountMode.Fixed, plan.AmountMode);
        Assert.False(plan.AutoPost);
        Assert.False(plan.ReminderEnabled);
        Assert.Equal(kind == EntryKind.Transfer ? null : draft.CategoryId, plan.CategoryId);
        Assert.Equal(kind == EntryKind.Transfer ? draft.ToAccountId : null, plan.ToAccountId);
        Assert.Equal(kind == EntryKind.Transfer ? draft.ToAmount : null, plan.ToAmount);
        Assert.NotEqual(plan.Id, draft.CreateSchedule(calendar).Id);
    }

    [Theory, Trait("AT", "AT-116")]
    [InlineData("refund")]
    [InlineData("account")]
    [InlineData("amount")]
    [InlineData("date")]
    [InlineData("name")]
    [InlineData("destination")]
    [InlineData("same-account")]
    [InlineData("foreign-amount")]
    public void Unsupported_or_invalid_handoffs_cannot_create_a_plan(string invalid)
    {
        var original = Fixture();
        var draft = invalid switch
        {
            "refund" => original with { Kind = EntryKind.Refund },
            "account" => original with { AccountId = Guid.Empty },
            "amount" => original with { Amount = 0 },
            "date" => original with { Date = default },
            "name" => original with { Name = " " },
            "destination" => original with { Kind = EntryKind.Transfer, ToAccountId = null },
            "same-account" => original with { Kind = EntryKind.Transfer, ToAccountId = original.AccountId },
            "foreign-amount" => original with { Kind = EntryKind.Transfer, ToAmount = null },
            _ => throw new ArgumentException("Unknown fixture", nameof(invalid))
        };

        Assert.Throws<InvalidOperationException>(() => draft.CreateSchedule(PeriodCalendar.Gregorian));
        Assert.Equal(12_345, original.Amount);
        Assert.Equal(new DateOnly(2026, 10, 31), original.Date);
    }

    [Fact, Trait("AT", "AT-116")]
    public void Edits_to_the_open_plan_do_not_mutate_the_original_transaction_handoff()
    {
        var draft = Fixture();
        var plan = draft.CreateSchedule(PeriodCalendar.Gregorian);
        plan.Amount = 999;
        plan.Rule.Start = plan.Rule.Start.AddDays(3);
        plan.Note = "Changed plan only";

        Assert.Equal(12_345, draft.Amount);
        Assert.Equal(new DateOnly(2026, 10, 31), draft.Date);
        Assert.Equal("Original draft note", draft.Note);
    }

    private static EntryPlanDraft Fixture() => new()
    {
        Kind = EntryKind.Expense, AccountId = Guid.NewGuid(), CurrencyCode = "EUR", Amount = 12_345,
        Date = new(2026, 10, 31), Name = "Fictitious rent", CategoryId = Guid.NewGuid(), Note = "Original draft note",
        ToAccountId = Guid.NewGuid(), ToCurrencyCode = "USD", ToAmount = 15_000,
    };
}
