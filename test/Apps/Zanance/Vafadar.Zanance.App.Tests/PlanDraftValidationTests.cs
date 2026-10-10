using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Verifies the editor's real pre-save validation across independent fields and existing money/rule modes.</summary>
public sealed class PlanDraftValidationTests
{
    private static readonly Guid Source = Guid.Parse("dbd368a5-2b7c-4055-99ed-5e15b20d9f10");
    private static readonly Guid Destination = Guid.Parse("0180336f-da64-480f-8f07-721323989d48");
    private static readonly DateOnly Start = new(2026, 10, 10);
    private static CultureInfo Culture => CultureInfo.GetCultureInfo("en-US");

    [Fact, Trait("AT", "AT-104")]
    public void One_attempt_reports_name_amount_destination_and_recurrence_without_altering_the_draft()
    {
        var input = Valid() with
        {
            Name = " ", AmountText = "-12", IsTransfer = true, DestinationId = null,
            Rule = new RecurrenceRule { Start = Start, Frequency = Frequency.Monthly, Interval = 0 },
        };
        var original = JsonSerializer.Serialize(input);
        var result = PlanDraftValidation.Validate(input, Culture);
        Assert.True(result.HasErrors);
        Assert.Equal("Plan_NameRequired", result.NameErrorKey);
        Assert.Equal("Plan_AmountMustBePositive", result.AmountErrorKey);
        Assert.Equal("LedgerError_DestinationRequired", result.DestinationErrorKey);
        Assert.Equal("Plan_IntervalMustBePositive", result.RuleErrorKey);
        Assert.Null(result.Amount); Assert.Null(result.DestinationAmount);
        Assert.Equal(original, JsonSerializer.Serialize(input));
    }

    [Theory, InlineData(""), InlineData("0"), InlineData("-1"), InlineData("not money"), InlineData("9223372036854775808")]
    [Trait("AT", "AT-104")]
    public void Required_amount_rejects_empty_nonpositive_invalid_and_overflowing_text(string text)
    {
        var result = PlanDraftValidation.Validate(Valid() with { AmountText = text }, Culture);
        Assert.True(result.HasErrors); Assert.Equal("Plan_AmountMustBePositive", result.AmountErrorKey);
        Assert.Null(result.Amount); Assert.Null(result.NameErrorKey); Assert.Null(result.RuleErrorKey);
    }

    [Fact, Trait("AT", "AT-104")]
    public void Unknown_amount_ignores_hidden_text_but_still_requires_a_transfer_destination()
    {
        var result = PlanDraftValidation.Validate(Valid() with
        { AmountRequired = false, AmountText = "not money", IsTransfer = true, DestinationId = null }, Culture);
        Assert.Null(result.Amount); Assert.Null(result.AmountErrorKey);
        Assert.Equal("LedgerError_DestinationRequired", result.DestinationErrorKey);
        Assert.True(result.HasErrors);
    }

    [Fact, Trait("AT", "AT-104")]
    public void Missing_source_has_its_own_problem_and_never_guesses_the_amount_currency()
    {
        var result = PlanDraftValidation.Validate(Valid() with { AccountId = null, CurrencyCode = "not a currency" }, Culture);
        Assert.Equal("Entry_NoAccounts", result.AccountErrorKey); Assert.Null(result.AmountErrorKey);
        Assert.Null(result.Amount); Assert.True(result.HasErrors);
    }

    [Fact, Trait("AT", "AT-104")]
    public void A_transfer_to_the_source_is_reported_with_the_other_independent_problems()
    {
        var result = PlanDraftValidation.Validate(Valid() with
        { Name = "", AmountText = "0", IsTransfer = true, DestinationId = Source }, Culture);
        Assert.Equal("LedgerError_SameAccountTransfer", result.DestinationErrorKey);
        Assert.NotNull(result.NameErrorKey); Assert.NotNull(result.AmountErrorKey);
    }

    [Theory, InlineData(""), InlineData("0"), InlineData("-1"), InlineData("not money"), InlineData("9223372036854775808")]
    [Trait("AT", "AT-104")]
    public void Cross_currency_destination_amount_is_validated_without_hiding_other_field_problems(string text)
    {
        var result = PlanDraftValidation.Validate(Valid() with
        {
            Name = "", AmountText = "0", IsTransfer = true, DestinationId = Destination,
            DestinationCurrencyCode = "JPY", DestinationAmountRequired = true, DestinationAmountText = text,
        }, Culture);
        Assert.Equal("LedgerError_DestinationAmountRequired", result.DestinationAmountErrorKey);
        Assert.NotNull(result.NameErrorKey); Assert.NotNull(result.AmountErrorKey);
        Assert.Null(result.DestinationAmount); Assert.Null(result.DestinationErrorKey);
    }

    [Fact, Trait("AT", "AT-104")]
    public void Missing_destination_does_not_parse_an_inapplicable_hidden_destination_amount()
    {
        var result = PlanDraftValidation.Validate(Valid() with
        {
            IsTransfer = true, DestinationId = null, DestinationAmountRequired = true,
            DestinationCurrencyCode = "not a currency", DestinationAmountText = "not money",
        }, Culture);
        Assert.Equal("LedgerError_DestinationRequired", result.DestinationErrorKey);
        Assert.Null(result.DestinationAmountErrorKey); Assert.Null(result.DestinationAmount);
    }

    [Fact, Trait("AT", "AT-104")]
    public void Same_currency_transfer_preserves_minor_units_and_ignores_a_hidden_destination_amount()
    {
        var result = PlanDraftValidation.Validate(Valid() with
        { IsTransfer = true, DestinationId = Destination, DestinationAmountText = "not money" }, Culture);
        Assert.False(result.HasErrors); Assert.Equal(1234L, result.Amount); Assert.Null(result.DestinationAmount);
    }

    [Fact, Trait("AT", "AT-104")]
    public void Cross_currency_transfer_preserves_each_accounts_original_minor_digits()
    {
        var result = PlanDraftValidation.Validate(Valid() with
        {
            IsTransfer = true, DestinationId = Destination, DestinationCurrencyCode = "JPY",
            DestinationAmountRequired = true, DestinationAmountText = "1800",
        }, Culture);
        Assert.False(result.HasErrors); Assert.Equal(1234L, result.Amount); Assert.Equal(1800L, result.DestinationAmount);
    }

    [Theory]
    [InlineData("en-US", "12.34"), InlineData("de-DE", "12,34"), InlineData("de-DE", "۱۲,۳۴"), InlineData("en-US", "١٢.٣٤")]
    [Trait("AT", "AT-104")]
    public void Valid_amounts_keep_the_existing_regional_parser_and_accepted_digit_shapes(string culture, string text)
    {
        var result = PlanDraftValidation.Validate(Valid() with { AmountText = text }, CultureInfo.GetCultureInfo(culture));
        Assert.False(result.HasErrors); Assert.Equal(1234L, result.Amount);
    }

    [Fact, Trait("AT", "AT-104")]
    public void Corrected_inputs_return_a_fresh_result_without_stale_destination_or_amount_problems()
    {
        var original = Valid() with { IsTransfer = true, Name = "", AmountText = "0", DestinationId = null };
        Assert.True(PlanDraftValidation.Validate(original, Culture).HasErrors);
        var result = PlanDraftValidation.Validate(original with { Name = "Monthly transfer", AmountText = "12.34", DestinationId = Destination }, Culture);
        Assert.False(result.HasErrors); Assert.Null(result.DestinationErrorKey); Assert.Null(result.AmountErrorKey);
        Assert.Null(result.NameErrorKey); Assert.Equal(1234L, result.Amount);
    }

    [Fact, Trait("AT", "AT-104")]
    public void Expense_with_unknown_amount_does_not_acquire_hidden_transfer_requirements()
    {
        var result = PlanDraftValidation.Validate(Valid() with
        { AmountRequired = false, AmountText = "not money", DestinationAmountRequired = true, DestinationAmountText = "not money" }, Culture);
        Assert.False(result.HasErrors); Assert.Null(result.Amount); Assert.Null(result.DestinationAmount);
    }

    [Fact, Trait("AT", "AT-104")]
    public void The_existing_recurrence_problem_is_kept_in_its_field_group()
    {
        var result = PlanDraftValidation.Validate(Valid() with
        { Rule = new RecurrenceRule { Start = Start, Frequency = Frequency.Monthly, End = EndKind.OnDate, EndDate = Start.AddDays(-1) } }, Culture);
        Assert.True(result.HasErrors); Assert.Equal("Plan_EndBeforeStart", result.RuleErrorKey);
        Assert.Equal(1234L, result.Amount);
    }

    private static PlanValidationInput Valid() => new()
    {
        Name = "Monthly rent", AmountText = "12.34", AmountRequired = true, AccountId = Source,
        CurrencyCode = "EUR", Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = Start },
    };
}
