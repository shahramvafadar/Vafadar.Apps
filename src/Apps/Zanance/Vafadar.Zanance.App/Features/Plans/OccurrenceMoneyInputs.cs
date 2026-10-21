using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>Actual occurrence payment and override inputs, independent of native list presentation.</summary>
public sealed class OccurrenceMoneyInputs(ILocalizationService localization)
{
    /// <summary>Gets the required actual payment's independent raw interpretation.</summary>
    public MoneyDraftInput Payment { get; } = new();
    /// <summary>Gets the optional occurrence override's independent raw interpretation.</summary>
    public MoneyDraftInput Override { get; } = new();
    /// <summary>Gets the original account currency selected by the covered occurrence publication.</summary>
    public string Currency { get; private set; } = "EUR";
    /// <summary>Changes the current target without relabeling retained nonempty drafts.</summary>
    public void SelectCurrency(string currency) => Currency = currency;
    /// <summary>Initializes only a new canonical occurrence, never a retained tab refresh.</summary>
    public void Initialize(long? payment, long? replacement)
    {
        if (payment is { } amount and > 0) Payment.Format(amount, Currency, localization.CurrentCulture);
        else Payment.Initialize("", Currency, localization.CurrentCulture);
        if (replacement is { } overridden) Override.Format(overridden, Currency, localization.CurrentCulture);
        else Override.Initialize("", Currency, localization.CurrentCulture);
    }
    /// <summary>Captures actual typing in the payment without borrowing the override's meaning.</summary>
    public void ChangePayment(string text) => Payment.ChangeText(text, Currency, localization.CurrentCulture);
    /// <summary>Captures actual typing in the override without borrowing the payment's meaning.</summary>
    public void ChangeOverride(string text) => Override.ChangeText(text, Currency, localization.CurrentCulture);
    /// <summary>Validates only the field belonging to the attempted original action.</summary>
    public OccurrenceAmountResult Validate(bool payment)
    {
        var input = payment ? Payment : Override;
        if (input.NeedsReview(Currency)) return new(null, "Plan_InputUnitChanged");
        return OccurrenceAmountValidation.Validate(input.Text, Currency, localization.CurrentCulture, payment, input.Context);
    }
}
