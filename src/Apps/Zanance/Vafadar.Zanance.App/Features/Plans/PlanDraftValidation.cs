using System.Globalization;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Captures the existing editor's applicable inputs without changing the unsaved plan.</summary>
public sealed record PlanValidationInput
{
    /// <summary>Gets the plan's untrimmed display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the source amount text exactly as entered.</summary>
    public string AmountText { get; init; } = string.Empty;

    /// <summary>Gets whether the current amount mode requires a positive source amount.</summary>
    public bool AmountRequired { get; init; }

    /// <summary>Gets the selected source account identity, if any.</summary>
    public Guid? AccountId { get; init; }

    /// <summary>Gets the source account's ISO currency for the unchanged money parser.</summary>
    public string CurrencyCode { get; init; } = "EUR";

    /// <summary>Gets whether the plan moves money between accounts.</summary>
    public bool IsTransfer { get; init; }

    /// <summary>Gets the selected destination account identity, if any.</summary>
    public Guid? DestinationId { get; init; }

    /// <summary>Gets the destination account's ISO currency.</summary>
    public string DestinationCurrencyCode { get; init; } = "EUR";

    /// <summary>Gets the destination amount text exactly as entered.</summary>
    public string DestinationAmountText { get; init; } = string.Empty;

    /// <summary>Gets whether the editor exposes a required cross-currency destination amount.</summary>
    public bool DestinationAmountRequired { get; init; }

    /// <summary>Gets the editor's original recurrence rule for the existing recurrence validator.</summary>
    public required RecurrenceRule Rule { get; init; }
}

/// <summary>Contains all applicable field problems and the original parsed minor-unit amounts.</summary>
public sealed record PlanValidationResult
{
    /// <summary>Gets the name problem's existing translation key.</summary>
    public string? NameErrorKey { get; init; }

    /// <summary>Gets the source amount problem's localized translation key.</summary>
    public string? AmountErrorKey { get; init; }

    /// <summary>Gets the source account problem's existing translation key.</summary>
    public string? AccountErrorKey { get; init; }

    /// <summary>Gets the destination account problem's existing translation key.</summary>
    public string? DestinationErrorKey { get; init; }

    /// <summary>Gets the destination amount problem's existing translation key.</summary>
    public string? DestinationAmountErrorKey { get; init; }

    /// <summary>Gets the recurrence group's problem key from the existing rule validator.</summary>
    public string? RuleErrorKey { get; init; }

    /// <summary>Gets the positive parsed source amount, or null for an unknown amount or invalid input.</summary>
    public long? Amount { get; init; }

    /// <summary>Gets the positive parsed destination amount when a cross-currency amount is required.</summary>
    public long? DestinationAmount { get; init; }

    /// <summary>Gets whether any applicable input prevents the original save continuation.</summary>
    public bool HasErrors => NameErrorKey is not null || AmountErrorKey is not null || AccountErrorKey is not null
        || DestinationErrorKey is not null || DestinationAmountErrorKey is not null || RuleErrorKey is not null;
}

/// <summary>Collects independent plan-field problems in one pass before any entity mutation or persistence.</summary>
public static class PlanDraftValidation
{
    /// <summary>Uses the existing money and recurrence rules without replacing typed text or altering the rule.</summary>
    public static PlanValidationResult Validate(PlanValidationInput input, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(culture);
        long? amount = null;
        string? amountError = null;
        if (input.AmountRequired && input.AccountId is not null)
        {
            if (MoneyText.TryParse(input.AmountText, Currencies.Get(input.CurrencyCode), culture, out var parsed) && parsed > 0)
            { amount = parsed; }
            else { amountError = "Plan_AmountMustBePositive"; }
        }

        string? destinationError = null;
        string? destinationAmountError = null;
        long? destinationAmount = null;
        if (input.IsTransfer)
        {
            destinationError = input.DestinationId is null ? "LedgerError_DestinationRequired"
                : input.AccountId == input.DestinationId ? "LedgerError_SameAccountTransfer" : null;
            // A missing destination has no currency yet. Keep the amount hidden until the user selects one.
            if (input.DestinationAmountRequired && input.DestinationId is not null)
            {
                if (MoneyText.TryParse(input.DestinationAmountText, Currencies.Get(input.DestinationCurrencyCode), culture,
                    out var parsedTo) && parsedTo > 0)
                { destinationAmount = parsedTo; }
                else { destinationAmountError = "LedgerError_DestinationAmountRequired"; }
            }
        }

        return new PlanValidationResult
        {
            NameErrorKey = string.IsNullOrWhiteSpace(input.Name) ? "Plan_NameRequired" : null,
            AmountErrorKey = amountError,
            AccountErrorKey = input.AccountId is null ? "Entry_NoAccounts" : null,
            DestinationErrorKey = destinationError,
            DestinationAmountErrorKey = destinationAmountError,
            RuleErrorKey = Recurrence.Validate(input.Rule) is { } problem ? "Plan_" + problem : null,
            Amount = amount,
            DestinationAmount = destinationAmount,
        };
    }
}
