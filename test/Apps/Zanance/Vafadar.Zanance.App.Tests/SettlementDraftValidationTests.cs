using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Checks regional settlement input against real advance/refund calculations and immutable drafts.</summary>
public sealed class SettlementDraftValidationTests
{
    private static readonly DateOnly Day = new(2026, 10, 10);

    [Theory, InlineData(""), InlineData(" "), InlineData("-1"), InlineData("not money"), InlineData("9223372036854775808")]
    [Trait("AT", "AT-107")]
    public void Invalid_bill_and_reversed_period_report_both_problems_without_returning_a_payable_amount(string text)
    {
        var result = SettlementDraftValidation.Validate(Day.AddDays(1), Day, text, "EUR", CultureInfo.GetCultureInfo("en-US"));
        Assert.True(result.HasErrors); Assert.Null(result.Actual);
        Assert.Equal("Settlement_PeriodInvalid", result.PeriodErrorKey);
        Assert.Equal("Settlement_AmountInvalid", result.AmountErrorKey);
    }

    [Theory]
    [InlineData("en-US", "12.34", "EUR", 1234L)]
    [InlineData("de-DE", "12,34", "EUR", 1234L)]
    [InlineData("de-DE", "۱۲,۳۴", "EUR", 1234L)]
    [InlineData("en-US", "١٢.٣٤", "EUR", 1234L)]
    [InlineData("en-US", "1200", "JPY", 1200L)]
    [Trait("AT", "AT-107")]
    public void Same_day_period_keeps_currency_minor_units_and_the_existing_regional_digit_parser(string culture, string text, string currency, long expected)
    {
        var result = SettlementDraftValidation.Validate(Day, Day, text, currency, CultureInfo.GetCultureInfo(culture));
        Assert.False(result.HasErrors); Assert.Equal(expected, result.Actual);
    }

    [Fact, Trait("AT", "AT-107")]
    public void Correcting_period_and_bill_returns_fresh_problems_without_mutating_original_input()
    {
        var original = (From: Day.AddDays(1), To: Day, Text: "not money");
        var snapshot = JsonSerializer.Serialize(original);
        Assert.True(SettlementDraftValidation.Validate(original.From, original.To, original.Text, "EUR", CultureInfo.InvariantCulture).HasErrors);
        var next = SettlementDraftValidation.Validate(Day, Day, "0", "EUR", CultureInfo.InvariantCulture);
        Assert.False(next.HasErrors); Assert.Equal(0L, next.Actual);
        Assert.Equal(snapshot, JsonSerializer.Serialize(original));
    }

    [Theory, InlineData("0", 7500L), InlineData("50", 2500L), InlineData("75", 0L)]
    [Trait("AT", "AT-107")]
    public void A_nonnegative_bill_refunds_only_remaining_advances_and_never_becomes_income(string text, long expectedBack)
    {
        var plan = new Schedule { Name = "Fictitious utility advances", AccountId = Guid.NewGuid(), Amount = 10000 };
        var advance = new LedgerEntry { AccountId = plan.AccountId, ScheduleId = plan.Id, Amount = 10000, Date = Day, Kind = EntryKind.Expense };
        var existingRefund = EntryActions.CreateRefund(advance, 2500, advance.AccountId, Day);
        LedgerEntry[] entries = [advance, existingRefund];
        var before = JsonSerializer.Serialize(entries);
        var draft = SettlementDraftValidation.Validate(Day, Day, text, "EUR", CultureInfo.InvariantCulture);
        var result = AdvanceSettlement.Compute(plan, entries, Day, Day, draft.Actual!.Value);
        Assert.Equal(7500, result.Paid);
        var created = AdvanceSettlement.CreateEntries(plan, result, entries, Day, "Fictitious final bill");
        Assert.Equal(expectedBack, created.Sum(entry => entry.Amount));
        Assert.All(created, entry => { Assert.Equal(EntryKind.Refund, entry.Kind); Assert.Equal(advance.Id, entry.RefundOfId); });
        Assert.Equal(before, JsonSerializer.Serialize(entries));
    }
}
