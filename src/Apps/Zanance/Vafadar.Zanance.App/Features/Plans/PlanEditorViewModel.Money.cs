using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Plans;

public sealed partial class PlanEditorViewModel
{
    private MoneyInputContext? _amountInput;
    private MoneyInputContext? _destinationInput;
    private bool _amountContextError;
    private bool _destinationContextError;
    private long _moneyRevision;
    private string SourceMoneyCode => Account?.CurrencyCode ?? Currencies.Euro.Code;
    private string DestinationMoneyCode => ToAccount?.CurrencyCode ?? SourceMoneyCode;
    private bool SourceMoneyNeedsReview => ShowAmount && Account is not null && AmountText.Length > 0 && _amountInput is { } original && !original.MatchesCurrent(SourceMoneyCode);
    private bool DestinationMoneyNeedsReview => ShowToAmount && ToAccount is not null && ToAmountText.Length > 0 && _destinationInput is { } original && !original.MatchesCurrent(DestinationMoneyCode);
    private bool MoneyContextsReady => !SourceMoneyNeedsReview && !DestinationMoneyNeedsReview;

    /// <summary>Gets the original amount label until actual monetary reentry, even after another account/unit is selected.</summary>
    public string AmountUnitName => AmountText.Length > 0 && _amountInput is { } original ? original.UnitName : MoneyText.UnitName(SourceMoneyCode);
    /// <summary>Gets the independent original destination label, never silently relabeled by the source account.</summary>
    public string DestinationAmountUnitName => ToAmountText.Length > 0 && _destinationInput is { } original ? original.UnitName : MoneyText.UnitName(DestinationMoneyCode);
    /// <summary>Explains the retained source parsing format after a display-culture change.</summary>
    public string? AmountFormatNote => ShowAmount && AmountText.Length > 0 && _amountInput is { } original && !original.UsesFormatOf(_localization.CurrentCulture)
        ? _translator.Format("Plan_OriginalInputFormat", original.Example) : null;
    /// <summary>Explains the separately retained destination parsing format.</summary>
    public string? DestinationAmountFormatNote => ShowToAmount && ToAmountText.Length > 0 && _destinationInput is { } original && !original.UsesFormatOf(_localization.CurrentCulture)
        ? _translator.Format("Plan_OriginalInputFormat", original.Example) : null;

    private void InitializeMoneyInputs()
    {
        _amountInput = MoneyInputContext.Capture(SourceMoneyCode, _localization.CurrentCulture);
        _destinationInput = MoneyInputContext.Capture(DestinationMoneyCode, _localization.CurrentCulture);
        _amountContextError = _destinationContextError = false;
    }

    partial void OnAmountTextChanged(string oldValue, string newValue)
    {
        if (_loading) return;
        _moneyRevision++;
        // A changed currency/unit requires clearing the old text, not merely appending a digit to it.
        if (oldValue.Length == 0 || newValue.Length == 0)
            _amountInput = MoneyInputContext.Capture(SourceMoneyCode, _localization.CurrentCulture);
        _amountContextError = false; AmountError = null; RefreshMoneyInputs();
    }
    partial void OnToAmountTextChanged(string oldValue, string newValue)
    {
        if (_loading) return;
        _moneyRevision++;
        if (oldValue.Length == 0 || newValue.Length == 0)
            _destinationInput = MoneyInputContext.Capture(DestinationMoneyCode, _localization.CurrentCulture);
        _destinationContextError = false; ToAmountError = null; RefreshMoneyInputs();
    }

    private void RefreshMoneyInputs()
    {
        if (_loading) return;
        var accepted = _committed || _saveSession?.Accepted == true;
        var amount = !accepted && SourceMoneyNeedsReview;
        var destination = !accepted && DestinationMoneyNeedsReview;
        if (amount) AmountError = _translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(SourceMoneyCode));
        else if (_amountContextError) AmountError = null;
        if (destination) ToAmountError = _translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(DestinationMoneyCode));
        else if (_destinationContextError) ToAmountError = null;
        _amountContextError = amount; _destinationContextError = destination;
        foreach (var name in new[] { nameof(AmountUnitName), nameof(DestinationAmountUnitName), nameof(AmountFormatNote), nameof(DestinationAmountFormatNote), nameof(CanSave) })
            OnPropertyChanged(name);
        SaveCommand.NotifyCanExecuteChanged();
    }
}
