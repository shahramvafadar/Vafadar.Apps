using System.Globalization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-432: actual transaction validation must respect each original raw monetary interpretation.</summary>
[Trait("AT", "AT-432")]
[Collection("Entry validation units")]
public sealed class EntryMoneyContextTests
{
    private static EntryValidationInput Transfer()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        return new()
        {
            Kind = EntryKind.Transfer, AccountId = Guid.CreateVersion7(), CurrencyCode = "EUR", AmountText = "100",
            AmountContext = MoneyInputContext.Capture("EUR", culture), FeeText = "1.23", FeeContext = MoneyInputContext.Capture("EUR", culture),
            DestinationId = Guid.CreateVersion7(), DestinationCurrencyCode = "USD", DestinationAmountRequired = true, DestinationAmountText = "200",
            DestinationAmountContext = MoneyInputContext.Capture("USD", culture), DestinationFeeApplicable = true, DestinationFeeText = "2.34",
            DestinationFeeContext = MoneyInputContext.Capture("USD", culture),
        };
    }

    /// <summary>Old source, destination, fee and reimbursable raw fields must not accept a different display unit.</summary>
    [Theory]
    [InlineData("amount")] [InlineData("fee")] [InlineData("destination")] [InlineData("destination-fee")] [InlineData("reimbursable")]
    public void Changed_unit_requires_explicit_reentry_in_each_applicable_field(string field)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = Transfer();
            if (field == "reimbursable") input = input with { Kind = EntryKind.Expense, ReimbursableEnabled = true, ReimbursableText = "12.34",
                ReimbursableContext = MoneyInputContext.Capture("EUR", CultureInfo.GetCultureInfo("en-US")) };
            Assert.False(EntryDraftValidation.Validate(input, CultureInfo.GetCultureInfo("en-US")).HasErrors);
            var code = field.StartsWith("destination", StringComparison.Ordinal) ? "USD" : "EUR";
            DisplayUnits.Set([new(code, "Hundred units", 2)]);
            Assert.True(DisplayUnits.TryGet(code, out _));
            var result = EntryDraftValidation.Validate(input, CultureInfo.GetCultureInfo("en-US"));
            var error = field switch { "amount" => result.AmountErrorKey, "fee" => result.FeeErrorKey,
                "destination" => result.DestinationAmountErrorKey, "destination-fee" => result.DestinationFeeErrorKey, _ => result.ReimbursableErrorKey };
            Assert.NotNull(error);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>A number-format change cannot replace the main or destination field's original culture.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Formatting_changes_retain_the_independent_original_parser(bool destination)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = Transfer();
            input = destination ? input with { DestinationAmountText = "1,234" } : input with { AmountText = "1,234" };
            var result = EntryDraftValidation.Validate(input, CultureInfo.GetCultureInfo("de-DE"));
            Assert.Equal(123400, destination ? result.DestinationAmount : result.Amount);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Original purchase money remains ISO-only and keeps its independent input format.</summary>
    [Fact]
    public void Original_purchase_uses_its_original_culture_without_display_unit_scaling()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = Transfer() with { Kind = EntryKind.Expense, CurrencyCode = "USD", AmountText = "100",
                AmountContext = MoneyInputContext.Capture("USD", CultureInfo.GetCultureInfo("en-US")), ForeignEnabled = true,
                ForeignCurrency = "EUR", ForeignAmountText = "1,234", ForeignContext = MoneyInputContext.Capture("EUR", CultureInfo.GetCultureInfo("en-US")) };
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            var result = EntryDraftValidation.Validate(input, CultureInfo.GetCultureInfo("de-DE"));
            Assert.Equal(123400, result.ForeignAmount);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Renewing one amount never acknowledges the independent old fee on that currency's side.</summary>
    [Theory][InlineData(false)][InlineData(true)]
    public void Explicit_main_reentry_does_not_renew_an_untouched_fee(bool destination)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = Transfer(); var code = destination ? "USD" : "EUR";
            DisplayUnits.Set([new(code, "Hundred units", 2)]); var context = MoneyInputContext.Capture(code, CultureInfo.GetCultureInfo("en-US"));
            input = destination ? input with { DestinationAmountText = "2", DestinationAmountContext = context }
                : input with { AmountText = "1", AmountContext = context };
            var refused = EntryDraftValidation.Validate(input, CultureInfo.GetCultureInfo("en-US"));
            Assert.Null(destination ? refused.DestinationAmountErrorKey : refused.AmountErrorKey);
            Assert.NotNull(destination ? refused.DestinationFeeErrorKey : refused.FeeErrorKey);
            input = destination ? input with { DestinationFeeText = "0.0234", DestinationFeeContext = context }
                : input with { FeeText = "0.0123", FeeContext = context };
            var accepted = EntryDraftValidation.Validate(input, CultureInfo.GetCultureInfo("en-US")); Assert.False(accepted.HasErrors);
            Assert.Equal(10000, accepted.Amount); Assert.Equal(20000, accepted.DestinationAmount); Assert.Equal(123, accepted.Fee); Assert.Equal(234, accepted.DestinationFee);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Stale hidden transfer/detail fields do not block or leak into an ordinary expense.</summary>
    [Fact]
    public void Hidden_transfer_and_detail_amounts_are_not_reinterpreted()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); var input = Transfer(); DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            input = input with { Kind = EntryKind.Expense, AmountText = "1", AmountContext = MoneyInputContext.Capture("EUR", CultureInfo.GetCultureInfo("en-US")),
                FeeText = "not money", DestinationAmountText = "not money", DestinationFeeText = "not money", ForeignEnabled = false,
                ForeignAmountText = "not money", ReimbursableEnabled = false, ReimbursableText = "not money" };
            var result = EntryDraftValidation.Validate(input, CultureInfo.GetCultureInfo("en-US")); Assert.False(result.HasErrors);
            Assert.Equal(10000, result.Amount); Assert.Equal(0, result.Fee); Assert.Equal(0, result.DestinationFee);
            Assert.Null(result.DestinationAmount); Assert.Null(result.ForeignAmount); Assert.Null(result.ReimbursableAmount);
        }
        finally { DisplayUnits.Set(units); }
    }
}
