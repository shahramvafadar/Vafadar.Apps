using System.Globalization;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Captures the editor's applicable monetary inputs without changing its draft or ledger entities.</summary>
public sealed record EntryValidationInput
{
    /// <summary>Gets the original main monetary interpretation captured when its raw input was introduced.</summary>
    public MoneyInputContext? AmountContext { get; init; }
    /// <summary>Gets the independently captured original destination interpretation.</summary>
    public MoneyInputContext? DestinationAmountContext { get; init; }
    /// <summary>Gets the original source fee interpretation.</summary>
    public MoneyInputContext? FeeContext { get; init; }
    /// <summary>Gets the independent original destination fee interpretation.</summary>
    public MoneyInputContext? DestinationFeeContext { get; init; }
    /// <summary>Gets the original currency/culture for ISO-only purchase input; its display unit is never applied.</summary>
    public MoneyInputContext? ForeignContext { get; init; }
    /// <summary>Gets the reimbursable field's independent original monetary interpretation.</summary>
    public MoneyInputContext? ReimbursableContext { get; init; }
    /// <summary>Gets the selected entry kind, including fixed refund and reversal kinds.</summary>
    public EntryKind Kind { get; init; }
    /// <summary>Gets the selected source account identity, if any.</summary>
    public Guid? AccountId { get; init; }
    /// <summary>Gets whether the profile has no selectable source accounts.</summary>
    public bool HasNoAccounts { get; init; }
    /// <summary>Gets the source account's ISO currency for the existing display-unit money parser.</summary>
    public string CurrencyCode { get; init; } = "EUR";
    /// <summary>Gets the main amount exactly as entered.</summary>
    public string AmountText { get; init; } = string.Empty;
    /// <summary>Gets the selected transfer destination identity, if any.</summary>
    public Guid? DestinationId { get; init; }
    /// <summary>Gets the selected destination account's ISO currency.</summary>
    public string DestinationCurrencyCode { get; init; } = "EUR";
    /// <summary>Gets whether a positive destination amount is required by the current transfer fields.</summary>
    public bool DestinationAmountRequired { get; init; }
    /// <summary>Gets the destination amount exactly as entered.</summary>
    public string DestinationAmountText { get; init; } = string.Empty;
    /// <summary>Gets the optional source fee text; blank or zero means no fee.</summary>
    public string FeeText { get; init; } = string.Empty;
    /// <summary>Gets whether the current editor continuation applies a destination fee.</summary>
    public bool DestinationFeeApplicable { get; init; }
    /// <summary>Gets the optional destination fee in that account's display unit.</summary>
    public string DestinationFeeText { get; init; } = string.Empty;
    /// <summary>Gets whether an original purchase currency and amount are enabled.</summary>
    public bool ForeignEnabled { get; init; }
    /// <summary>Gets the original ISO currency, independently of the source account's display unit.</summary>
    public string ForeignCurrency { get; init; } = "EUR";
    /// <summary>Gets the original amount text, in the ISO currency's major units.</summary>
    public string ForeignAmountText { get; init; } = string.Empty;
    /// <summary>Gets whether the expense has a reimbursable part.</summary>
    public bool ReimbursableEnabled { get; init; }
    /// <summary>Gets the reimbursable amount; blank means the whole valid expense amount.</summary>
    public string ReimbursableText { get; init; } = string.Empty;
}

/// <summary>Contains all independent applicable field problems and unchanged parsed minor-unit amounts.</summary>
public sealed record EntryValidationResult
{
    /// <summary>Gets the source account problem key.</summary>
    public string? AccountErrorKey { get; init; }
    /// <summary>Gets the main amount problem key.</summary>
    public string? AmountErrorKey { get; init; }
    /// <summary>Gets the transfer destination account problem key.</summary>
    public string? DestinationErrorKey { get; init; }
    /// <summary>Gets the cross-currency destination amount problem key.</summary>
    public string? DestinationAmountErrorKey { get; init; }
    /// <summary>Gets the source fee problem key.</summary>
    public string? FeeErrorKey { get; init; }
    /// <summary>Gets the destination fee problem key.</summary>
    public string? DestinationFeeErrorKey { get; init; }
    /// <summary>Gets the original currency problem key from the existing ledger rules.</summary>
    public string? ForeignCurrencyErrorKey { get; init; }
    /// <summary>Gets the original amount problem key.</summary>
    public string? ForeignAmountErrorKey { get; init; }
    /// <summary>Gets the reimbursable expense amount problem key.</summary>
    public string? ReimbursableErrorKey { get; init; }
    /// <summary>Gets the positive source amount, or null when it cannot be accepted.</summary>
    public long? Amount { get; init; }
    /// <summary>Gets the positive destination amount where required.</summary>
    public long? DestinationAmount { get; init; }
    /// <summary>Gets the optional nonnegative source fee in source minor units.</summary>
    public long Fee { get; init; }
    /// <summary>Gets the applicable nonnegative destination fee in destination minor units.</summary>
    public long DestinationFee { get; init; }
    /// <summary>Gets the original positive purchase amount in ISO minor units.</summary>
    public long? ForeignAmount { get; init; }
    /// <summary>Gets the positive reimbursable part, or the whole valid amount when its field is blank.</summary>
    public long? ReimbursableAmount { get; init; }
    /// <summary>Gets whether any applicable field prevents the existing save continuation.</summary>
    public bool HasErrors => AccountErrorKey is not null || AmountErrorKey is not null || DestinationErrorKey is not null
        || DestinationAmountErrorKey is not null || FeeErrorKey is not null || DestinationFeeErrorKey is not null
        || ForeignCurrencyErrorKey is not null || ForeignAmountErrorKey is not null || ReimbursableErrorKey is not null;
}

/// <summary>Collects monetary field feedback before an editor mutates its entry or synchronizes fee entries.</summary>
public static class EntryDraftValidation
{
    /// <summary>Uses the existing regional/display-unit parsers and ledger restrictions without replacing input text.</summary>
    public static EntryValidationResult Validate(EntryValidationInput input, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(culture);
        bool Parse(string text, Currency currency, MoneyInputContext? original, out long minor)
        {
            if (original is null) return MoneyText.TryParse(text, currency, culture, out minor);
            minor = 0;
            // A captured raw amount can retain its parser, but cannot silently adopt a different current unit.
            return original.MatchesCurrent(currency.Code) && original.TryParse(text, out minor);
        }
        bool ParseOriginal(string text, Currency currency, MoneyInputContext? original, out long minor)
        {
            minor = 0;
            if (original is not null && !string.Equals(original.Currency.Code, currency.Code, StringComparison.OrdinalIgnoreCase)) return false;
            return MoneyAmount.TryParse(text, currency, original?.Culture ?? culture, out minor);
        }
        long? amount = null, destinationAmount = null, foreignAmount = null, reimbursable = null;
        long fee = 0, destinationFee = 0;
        string? amountError = null, feeError = null, destinationError = null, destinationAmountError = null;
        string? destinationFeeError = null, foreignCurrencyError = null, foreignAmountError = null, reimbursableError = null;
        var transfer = input.Kind == EntryKind.Transfer;
        // Keep the editor's existing source-currency fallback, but never guess an amount when no account is selected.
        var currency = Currencies.TryGet(input.CurrencyCode, out var known) ? known : Currencies.Euro;
        if (input.AccountId is not null)
        {
            var parsed = Parse(input.AmountText, currency, input.AmountContext, out var value);
            if (parsed && value > 0) { amount = value; }
            else { amountError = parsed || string.IsNullOrWhiteSpace(input.AmountText) ? "Entry_AmountMustBePositive" : "Amount_Invalid"; }
            if (transfer && !string.IsNullOrWhiteSpace(input.FeeText))
            {
                if (!Parse(input.FeeText, currency, input.FeeContext, out fee) || fee < 0)
                { feeError = "Amount_Invalid"; fee = 0; }
            }
        }
        if (transfer)
        {
            destinationError = input.DestinationId is null ? "LedgerError_DestinationRequired"
                : input.DestinationId == input.AccountId ? "LedgerError_SameAccountTransfer" : null;
            // A missing destination supplies no currency; its hidden dependent amounts are not guessed.
            if (input.DestinationId is not null)
            {
                if (input.DestinationAmountRequired)
                {
                    if (Parse(input.DestinationAmountText, Currencies.Get(input.DestinationCurrencyCode), input.DestinationAmountContext,
                        out var value) && value > 0) { destinationAmount = value; }
                    else { destinationAmountError = "LedgerError_DestinationAmountRequired"; }
                }
                if (input.DestinationFeeApplicable && !string.IsNullOrWhiteSpace(input.DestinationFeeText))
                {
                    if (!Parse(input.DestinationFeeText, Currencies.Get(input.DestinationCurrencyCode), input.DestinationFeeContext, out destinationFee)
                        || destinationFee < 0) { destinationFeeError = "Amount_Invalid"; destinationFee = 0; }
                }
            }
        }
        if (input.ForeignEnabled && !transfer)
        {
            if (!Currencies.TryGet(input.ForeignCurrency, out var original)) { foreignCurrencyError = "LedgerError_InvalidOriginalCurrency"; }
            else
            {
                if (input.AccountId is not null && string.Equals(input.ForeignCurrency, input.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                { foreignCurrencyError = "LedgerError_InvalidOriginalCurrency"; }
                // Original purchase amounts stay in ISO units, not the source/destination display unit (FX-01).
                var parsed = ParseOriginal(input.ForeignAmountText, original, input.ForeignContext, out var value);
                if (parsed && value > 0) { foreignAmount = value; }
                else { foreignAmountError = parsed || string.IsNullOrWhiteSpace(input.ForeignAmountText) ? "Entry_AmountMustBePositive" : "Amount_Invalid"; }
            }
        }
        if (input.ReimbursableEnabled && input.Kind == EntryKind.Expense && input.AccountId is not null)
        {
            if (string.IsNullOrWhiteSpace(input.ReimbursableText)) { reimbursable = amount; }
            else if (!Parse(input.ReimbursableText, currency, input.ReimbursableContext, out var value) || value <= 0
                || amount is { } total && value > total) { reimbursableError = "Entry_ReimbursableInvalid"; }
            else { reimbursable = value; }
        }
        return new EntryValidationResult
        {
            AccountErrorKey = input.AccountId is null ? input.HasNoAccounts ? "Entry_NoAccounts" : "Entry_ChooseAccount" : null,
            AmountErrorKey = amountError, DestinationErrorKey = destinationError, DestinationAmountErrorKey = destinationAmountError,
            FeeErrorKey = feeError, DestinationFeeErrorKey = destinationFeeError, ForeignCurrencyErrorKey = foreignCurrencyError,
            ForeignAmountErrorKey = foreignAmountError, ReimbursableErrorKey = reimbursableError,
            Amount = amount, DestinationAmount = destinationAmount, Fee = fee, DestinationFee = destinationFee,
            ForeignAmount = foreignAmount, ReimbursableAmount = reimbursable,
        };
    }
}
