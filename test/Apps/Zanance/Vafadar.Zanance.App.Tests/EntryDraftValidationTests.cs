using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Isolates the one display-unit parser scenario from other application fixtures' global preferences.</summary>
[CollectionDefinition("Entry validation units", DisableParallelization = true)]
public sealed class EntryValidationUnitCollection { }

/// <summary>Exercises the actual pre-save monetary validator, without reimplementing editor or ledger rules.</summary>
[Collection("Entry validation units")]
public sealed class EntryDraftValidationTests
{
    private static readonly Guid Source = Guid.Parse("721d4727-9651-4530-9c37-233d4d9b14a4");
    private static readonly Guid Destination = Guid.Parse("afc710b6-f05a-4fbf-a643-a7bdf4395fd0");
    private static CultureInfo Culture => CultureInfo.GetCultureInfo("en-US");

    [Fact, Trait("AT", "AT-105")]
    public void One_attempt_reports_amount_destination_and_fee_without_replacing_any_draft_input()
    {
        var input = Valid() with { Kind = EntryKind.Transfer, AmountText = "0", DestinationId = null, FeeText = "not money" };
        var before = JsonSerializer.Serialize(input);
        var result = EntryDraftValidation.Validate(input, Culture);
        Assert.True(result.HasErrors);
        Assert.Equal("Entry_AmountMustBePositive", result.AmountErrorKey);
        Assert.Equal("LedgerError_DestinationRequired", result.DestinationErrorKey);
        Assert.Equal("Amount_Invalid", result.FeeErrorKey);
        Assert.Null(result.Amount); Assert.Equal(before, JsonSerializer.Serialize(input));
    }

    [Theory]
    [InlineData("", "Entry_AmountMustBePositive"), InlineData("0", "Entry_AmountMustBePositive")]
    [InlineData("-1", "Amount_Invalid"), InlineData("not money", "Amount_Invalid")]
    [InlineData("9223372036854775808", "Amount_Invalid")]
    [Trait("AT", "AT-105")]
    public void Required_amount_distinguishes_nonpositive_input_from_unparseable_or_overflowing_text(string text, string key)
    {
        var result = EntryDraftValidation.Validate(Valid() with { AmountText = text }, Culture);
        Assert.True(result.HasErrors); Assert.Equal(key, result.AmountErrorKey); Assert.Null(result.Amount);
    }

    [Theory, InlineData(false, "Entry_ChooseAccount"), InlineData(true, "Entry_NoAccounts")]
    [Trait("AT", "AT-105")]
    public void Missing_source_reports_its_context_without_guessing_money_or_fee_currency(bool empty, string key)
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, AccountId = null, HasNoAccounts = empty, CurrencyCode = "unknown", AmountText = "not money", FeeText = "not money" }, Culture);
        Assert.Equal(key, result.AccountErrorKey); Assert.Null(result.AmountErrorKey); Assert.Null(result.FeeErrorKey);
        Assert.Null(result.Amount); Assert.Equal("LedgerError_DestinationRequired", result.DestinationErrorKey);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Source_as_destination_is_reported_with_the_other_independent_errors()
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, DestinationId = Source, AmountText = "0", FeeText = "not money" }, Culture);
        Assert.Equal("LedgerError_SameAccountTransfer", result.DestinationErrorKey);
        Assert.NotNull(result.AmountErrorKey); Assert.NotNull(result.FeeErrorKey);
    }

    [Theory, InlineData(""), InlineData("0"), InlineData("-1"), InlineData("not money"), InlineData("9223372036854775808")]
    [Trait("AT", "AT-105")]
    public void Cross_currency_destination_amount_does_not_postpone_source_and_fee_feedback(string text)
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, DestinationId = Destination, DestinationCurrencyCode = "JPY", DestinationAmountRequired = true,
            DestinationAmountText = text, AmountText = "0", FeeText = "not money" }, Culture);
        Assert.Equal("LedgerError_DestinationAmountRequired", result.DestinationAmountErrorKey);
        Assert.NotNull(result.AmountErrorKey); Assert.NotNull(result.FeeErrorKey); Assert.Null(result.DestinationAmount);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Missing_destination_does_not_guess_its_inapplicable_amount_or_fee_currency()
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, DestinationId = null, DestinationCurrencyCode = "unknown", DestinationAmountRequired = true,
            DestinationAmountText = "not money", DestinationFeeApplicable = true, DestinationFeeText = "not money" }, Culture);
        Assert.Equal("LedgerError_DestinationRequired", result.DestinationErrorKey);
        Assert.Null(result.DestinationAmountErrorKey); Assert.Null(result.DestinationFeeErrorKey);
    }

    [Theory, InlineData("not money", "Amount_Invalid"), InlineData("9223372036854775808", "Amount_Invalid")]
    [InlineData("-1", "Amount_Invalid")]
    [Trait("AT", "AT-105")]
    public void A_bad_source_fee_stays_rejected_with_its_own_field_feedback(string text, string key)
    {
        var result = EntryDraftValidation.Validate(Valid() with { Kind = EntryKind.Transfer, DestinationId = Destination, FeeText = text }, Culture);
        Assert.True(result.HasErrors); Assert.Equal(key, result.FeeErrorKey); Assert.Equal(0, result.Fee);
    }

    [Theory, InlineData("not money", "Amount_Invalid"), InlineData("9223372036854775808", "Amount_Invalid")]
    [InlineData("-1", "Amount_Invalid")]
    [Trait("AT", "AT-105")]
    public void An_applicable_bad_destination_fee_has_its_own_error(string text, string key)
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, DestinationId = Destination, DestinationFeeApplicable = true, DestinationFeeText = text }, Culture);
        Assert.True(result.HasErrors); Assert.Equal(key, result.DestinationFeeErrorKey); Assert.Equal(0, result.DestinationFee);
    }

    [Theory, InlineData(""), InlineData(" "), InlineData("0")]
    [Trait("AT", "AT-105")]
    public void Optional_blank_or_zero_fees_remain_no_fee(string text)
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, DestinationId = Destination, FeeText = text, DestinationFeeApplicable = true, DestinationFeeText = text }, Culture);
        Assert.False(result.HasErrors); Assert.Equal(0, result.Fee); Assert.Equal(0, result.DestinationFee);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Valid_cross_currency_amounts_and_each_fee_keep_their_own_minor_digits()
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, DestinationId = Destination, DestinationCurrencyCode = "JPY", DestinationAmountRequired = true,
            DestinationAmountText = "1800", FeeText = "0.75", DestinationFeeApplicable = true, DestinationFeeText = "2" }, Culture);
        Assert.False(result.HasErrors); Assert.Equal(1234L, result.Amount); Assert.Equal(1800L, result.DestinationAmount);
        Assert.Equal(75, result.Fee); Assert.Equal(2, result.DestinationFee);
    }

    [Theory, InlineData("unknown"), InlineData("eur")]
    [Trait("AT", "AT-105")]
    public void Original_currency_keeps_the_existing_known_and_different_currency_ledger_restriction(string currency)
    {
        var result = EntryDraftValidation.Validate(Valid() with { ForeignEnabled = true, ForeignCurrency = currency, ForeignAmountText = "100" }, Culture);
        Assert.True(result.HasErrors); Assert.Equal("LedgerError_InvalidOriginalCurrency", result.ForeignCurrencyErrorKey);
    }

    [Theory, InlineData(""), InlineData("0"), InlineData("-1"), InlineData("not money"), InlineData("9223372036854775808")]
    [Trait("AT", "AT-105")]
    public void Original_amount_must_remain_a_positive_valid_purchase_amount(string text)
    {
        var result = EntryDraftValidation.Validate(Valid() with { ForeignEnabled = true, ForeignCurrency = "USD", ForeignAmountText = text }, Culture);
        Assert.True(result.HasErrors); Assert.NotNull(result.ForeignAmountErrorKey); Assert.Null(result.ForeignAmount);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Expense_reports_main_original_currency_original_amount_and_reimbursement_problems_together()
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { AmountText = "0", ForeignEnabled = true, ForeignCurrency = "EUR", ForeignAmountText = "-1", ReimbursableEnabled = true, ReimbursableText = "not money" }, Culture);
        Assert.NotNull(result.AmountErrorKey); Assert.NotNull(result.ForeignCurrencyErrorKey);
        Assert.NotNull(result.ForeignAmountErrorKey); Assert.Equal("Entry_ReimbursableInvalid", result.ReimbursableErrorKey);
    }

    [Theory, InlineData("0"), InlineData("-1"), InlineData("not money"), InlineData("12.35")]
    [Trait("AT", "AT-105")]
    public void A_reimbursable_part_must_be_positive_and_no_larger_than_the_expense(string text)
    {
        var result = EntryDraftValidation.Validate(Valid() with { ReimbursableEnabled = true, ReimbursableText = text }, Culture);
        Assert.Equal("Entry_ReimbursableInvalid", result.ReimbursableErrorKey); Assert.Null(result.ReimbursableAmount);
    }

    [Fact, Trait("AT", "AT-105")]
    public void A_blank_reimbursable_part_means_the_whole_valid_expense()
    {
        var result = EntryDraftValidation.Validate(Valid() with { ReimbursableEnabled = true }, Culture);
        Assert.False(result.HasErrors); Assert.Equal(1234L, result.ReimbursableAmount);
    }

    [Fact, Trait("AT", "AT-105")]
    public void A_positive_reimbursement_is_not_compared_with_an_unparseable_main_amount()
    {
        var result = EntryDraftValidation.Validate(Valid() with { AmountText = "not money", ReimbursableEnabled = true, ReimbursableText = "5" }, Culture);
        Assert.NotNull(result.AmountErrorKey); Assert.Null(result.ReimbursableErrorKey); Assert.Equal(500L, result.ReimbursableAmount);
    }

    [Theory]
    [InlineData("en-US", "12.34"), InlineData("de-DE", "12,34"), InlineData("de-DE", "۱۲,۳۴"), InlineData("en-US", "١٢.٣٤")]
    [Trait("AT", "AT-105")]
    public void Regional_separators_and_native_digit_inputs_keep_the_existing_parser(string culture, string text)
    {
        var result = EntryDraftValidation.Validate(Valid() with { AmountText = text }, CultureInfo.GetCultureInfo(culture));
        Assert.False(result.HasErrors); Assert.Equal(1234L, result.Amount);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Original_iso_amount_is_independent_of_account_and_original_currency_display_units()
    {
        var previous = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([new("IRR", "toman", 1), new("USD", "ten dollars", 1)]);
            var result = EntryDraftValidation.Validate(Valid() with
            { CurrencyCode = "IRR", AmountText = "125", ForeignEnabled = true, ForeignCurrency = "USD", ForeignAmountText = "100" }, Culture);
            Assert.False(result.HasErrors); Assert.Equal(125000L, result.Amount); Assert.Equal(10000L, result.ForeignAmount);
        }
        finally { DisplayUnits.Set(previous); }
    }

    [Fact, Trait("AT", "AT-105")]
    public void Nontransfer_and_nonexpense_fields_do_not_acquire_hidden_requirements()
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Income, FeeText = "not money", DestinationAmountRequired = true, DestinationAmountText = "not money",
            DestinationFeeApplicable = true, DestinationFeeText = "not money", ReimbursableEnabled = true, ReimbursableText = "not money" }, Culture);
        Assert.False(result.HasErrors); Assert.Null(result.DestinationErrorKey); Assert.Null(result.ReimbursableAmount);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Transfer_does_not_parse_hidden_original_purchase_currency_fields()
    {
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, DestinationId = Destination, ForeignEnabled = true, ForeignCurrency = "unknown", ForeignAmountText = "not money" }, Culture);
        Assert.False(result.HasErrors); Assert.Null(result.ForeignCurrencyErrorKey); Assert.Null(result.ForeignAmount);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Corrections_clear_old_problems_in_a_fresh_result_without_altering_the_old_input()
    {
        var input = Valid() with { Kind = EntryKind.Transfer, AmountText = "0", DestinationId = null, FeeText = "not money" };
        var initial = EntryDraftValidation.Validate(input, Culture);
        var next = EntryDraftValidation.Validate(input with { AmountText = "12.34", DestinationId = Destination, FeeText = "0.75" }, Culture);
        Assert.True(initial.HasErrors); Assert.False(next.HasErrors); Assert.Equal("0", input.AmountText);
        Assert.Null(next.FeeErrorKey); Assert.Null(next.DestinationErrorKey); Assert.Equal(75, next.Fee);
    }

    [Fact, Trait("AT", "AT-105")]
    public void Valid_parsed_transfer_and_fees_remain_separate_entries_with_the_existing_ledger_validation()
    {
        var sourceAccount = new Account { Name = "Fictitious source", CurrencyCode = "EUR" };
        var destinationAccount = new Account { Name = "Fictitious destination", CurrencyCode = "EUR" };
        var result = EntryDraftValidation.Validate(Valid() with
        { Kind = EntryKind.Transfer, AccountId = sourceAccount.Id, DestinationId = destinationAccount.Id,
            FeeText = "0.75", DestinationFeeApplicable = true, DestinationFeeText = "0.25" }, Culture);
        Assert.False(result.HasErrors);
        var transfer = new LedgerEntry { AccountId = sourceAccount.Id, ToAccountId = destinationAccount.Id, Kind = EntryKind.Transfer,
            Amount = result.Amount!.Value, Date = new DateOnly(2026, 10, 10) };
        var sourceFee = EntryActions.SyncTransferFee(transfer, null, result.Fee, null);
        var destinationFee = EntryActions.SyncDestinationFee(transfer, null, result.DestinationFee, null);
        Assert.NotNull(sourceFee); Assert.NotNull(destinationFee);
        Assert.Equal(EntryKind.Transfer, transfer.Kind); Assert.Equal(EntryKind.Expense, sourceFee.Kind);
        Assert.Equal(EntryKind.Expense, destinationFee.Kind); Assert.Equal(75, sourceFee.Amount); Assert.Equal(25, destinationFee.Amount);
        Assert.Equal(transfer.GroupId, sourceFee.GroupId); Assert.Equal(transfer.GroupId, destinationFee.GroupId);
        var accounts = new Dictionary<Guid, Account>
        { [sourceAccount.Id] = sourceAccount, [destinationAccount.Id] = destinationAccount };
        foreach (var entry in new[] { transfer, sourceFee, destinationFee })
        { Assert.Empty(LedgerValidator.Validate(entry, accounts, new Dictionary<Guid, Vafadar.Zanance.Core.Categories.Category>())); }
    }

    private static EntryValidationInput Valid() => new() { Kind = EntryKind.Expense, AccountId = Source, CurrencyCode = "EUR", AmountText = "12.34" };
}
