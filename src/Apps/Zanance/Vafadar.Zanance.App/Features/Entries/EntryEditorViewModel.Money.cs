using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Entries;

public sealed partial class EntryEditorViewModel
{
    private readonly MoneyDraftInput _amountMoney = new(), _destinationMoney = new(), _feeMoney = new(), _destinationFeeMoney = new(), _reimbursableMoney = new();
    private readonly MoneyDraftInput _foreignMoney = new(usesDisplayUnit: false);
    private readonly HashSet<string> _moneyContextErrors = [];
    private MoneyDraftSnapshot? _previousAmountMeaning;
    private string SourceMoneyCode => Account?.CurrencyCode ?? Currencies.Euro.Code;
    private string DestinationMoneyCode => ToAccount?.CurrencyCode ?? SourceMoneyCode;
    private bool RepeatMoneyReady => Account is null || !_amountMoney.NeedsReview(SourceMoneyCode)
        && (!ShowToAmount || !_destinationMoney.NeedsReview(DestinationMoneyCode));
    /// <summary>Gets readiness for the original plan handoff; unrelated fee/receipt detail fields are not plan amounts.</summary>
    public bool CanRepeat => IsFormEditable && Account is not null && _saveSession is { Accepted: false } && RepeatMoneyReady;
    private bool MoneyContextsReady => Account is null || !_amountMoney.NeedsReview(SourceMoneyCode)
        && (!IsTransfer || !_feeMoney.NeedsReview(SourceMoneyCode))
        && (!ShowToAmount || !_destinationMoney.NeedsReview(DestinationMoneyCode))
        && (!ShowDestinationFee || !_destinationFeeMoney.NeedsReview(DestinationMoneyCode))
        && (!ForeignEnabled || IsTransfer || !Currencies.TryGet(ForeignCurrency, out _) || !_foreignMoney.NeedsReview(ForeignCurrency))
        && (!ReimbursableEnabled || !IsExpense || !_reimbursableMoney.NeedsReview(SourceMoneyCode));
    private string MoneyRevision => string.Join(':', new[] { _amountMoney.Revision, _destinationMoney.Revision, _feeMoney.Revision,
        _destinationFeeMoney.Revision, _foreignMoney.Revision, _reimbursableMoney.Revision });
    private string MoneyMeaning => DraftFingerprint.Create(_amountMoney.Stamp, _destinationMoney.Stamp, _feeMoney.Stamp,
        _destinationFeeMoney.Stamp, _foreignMoney.Stamp, _reimbursableMoney.Stamp);
    /// <summary>Gets the main amount's original unit until complete reentry.</summary>
    public string AmountUnitName => _amountMoney.UnitName(SourceMoneyCode);
    /// <summary>Gets the destination amount's independent original unit.</summary>
    public string DestinationAmountUnitName => _destinationMoney.UnitName(DestinationMoneyCode);
    /// <summary>Gets the source fee's independent original unit.</summary>
    public string FeeUnitName => _feeMoney.UnitName(SourceMoneyCode);
    /// <summary>Gets the destination fee's original unit.</summary>
    public string DestinationFeeUnitName => _destinationFeeMoney.UnitName(DestinationMoneyCode);
    /// <summary>Gets the original ISO-only purchase currency.</summary>
    public string ForeignUnitName => _foreignMoney.UnitName(ForeignCurrency);
    /// <summary>Gets the reimbursable amount's original unit.</summary>
    public string ReimbursableUnitName => _reimbursableMoney.UnitName(SourceMoneyCode);
    /// <summary>Explains the main field's retained original number format.</summary>
    public string? AmountFormatNote => FormatNote(_amountMoney);
    /// <summary>Explains the destination field's retained original number format.</summary>
    public string? DestinationAmountFormatNote => ShowToAmount ? FormatNote(_destinationMoney) : null;
    /// <summary>Explains the source fee's retained format when applicable.</summary>
    public string? FeeFormatNote => IsTransfer ? FormatNote(_feeMoney) : null;
    /// <summary>Explains the destination fee's retained original format.</summary>
    public string? DestinationFeeFormatNote => ShowDestinationFee ? FormatNote(_destinationFeeMoney) : null;
    /// <summary>Explains the ISO-only original purchase format.</summary>
    public string? ForeignFormatNote => ForeignEnabled && !IsTransfer ? FormatNote(_foreignMoney) : null;
    /// <summary>Explains the original reimbursable format.</summary>
    public string? ReimbursableFormatNote => ReimbursableEnabled && IsExpense ? FormatNote(_reimbursableMoney) : null;
    /// <summary>Observes display changes only while this original native frame is visible.</summary>
    public void SetMoneyDisplayActive(bool active)
    {
        _localization.Changed -= OnMoneyDisplayChanged;
        if (active) _localization.Changed += OnMoneyDisplayChanged;
    }
    private void OnMoneyDisplayChanged(object? sender, EventArgs args) { RefreshMoneyInputs(); UpdateEffect(); }
    private string? FormatNote(MoneyDraftInput input) => input.OriginalExample(_localization.CurrentCulture) is { } example
        ? _translator.Format("Plan_OriginalInputFormat", example) : null;
    private void RawMoneyChanged(MoneyDraftInput input, string value, string currency)
    {
        if (_loading) return;
        input.ChangeText(value, currency, _localization.CurrentCulture);
        // Update the parser before publishing cleared feedback: a reentrant native observer may type newer raw text.
        if (ReferenceEquals(input, _amountMoney)) AmountError = null;
        else if (ReferenceEquals(input, _destinationMoney)) ToAmountError = null;
        else if (ReferenceEquals(input, _feeMoney)) FeeError = null;
        else if (ReferenceEquals(input, _destinationFeeMoney)) DestinationFeeError = null;
        else if (ReferenceEquals(input, _foreignMoney)) ForeignAmountError = null;
        else if (ReferenceEquals(input, _reimbursableMoney)) ReimbursableError = null;
        RefreshMoneyInputs();
    }
    partial void OnAmountTextChanged(string value)
    {
        if (!_loading && _receiptAppliedText != value) _receiptAppliedText = null;
        RawMoneyChanged(_amountMoney, value, SourceMoneyCode);
    }
    partial void OnToAmountTextChanged(string value) => RawMoneyChanged(_destinationMoney, value, DestinationMoneyCode);
    partial void OnFeeTextChanged(string value) => RawMoneyChanged(_feeMoney, value, SourceMoneyCode);
    partial void OnDestinationFeeTextChanged(string value) => RawMoneyChanged(_destinationFeeMoney, value, DestinationMoneyCode);
    partial void OnForeignAmountTextChanged(string value) => RawMoneyChanged(_foreignMoney, value, ForeignCurrency);
    partial void OnReimbursableTextChanged(string value) => RawMoneyChanged(_reimbursableMoney, value, SourceMoneyCode);
    private void SynchronizeMoneyInputs()
    {
        void Sync(MoneyDraftInput input, string text, string currency)
        {
            if (input.Text != text || string.IsNullOrWhiteSpace(text)) input.Initialize(text, currency, _localization.CurrentCulture);
        }
        Sync(_amountMoney, AmountText, SourceMoneyCode); Sync(_destinationMoney, ToAmountText, DestinationMoneyCode);
        Sync(_feeMoney, FeeText, SourceMoneyCode); Sync(_destinationFeeMoney, DestinationFeeText, DestinationMoneyCode);
        Sync(_foreignMoney, ForeignAmountText, ForeignCurrency); Sync(_reimbursableMoney, ReimbursableText, SourceMoneyCode);
    }
    private void RefreshMoneyInputs()
    {
        if (_loading) return;
        void Feedback(string name, MoneyDraftInput input, string currency, bool applicable, Action<string?> assign)
        {
            var stale = applicable && _saveSession?.Accepted != true && input.NeedsReview(currency);
            if (stale) { _moneyContextErrors.Add(name); assign(_translator.Format("Plan_InputUnitChanged", ReferenceEquals(input, _foreignMoney) ? currency : MoneyText.UnitName(currency))); }
            else if (_moneyContextErrors.Remove(name)) assign(null);
        }
        Feedback(nameof(AmountError), _amountMoney, SourceMoneyCode, Account is not null, value => AmountError = value);
        Feedback(nameof(ToAmountError), _destinationMoney, DestinationMoneyCode, ShowToAmount, value => ToAmountError = value);
        Feedback(nameof(FeeError), _feeMoney, SourceMoneyCode, IsTransfer && Account is not null, value => FeeError = value);
        Feedback(nameof(DestinationFeeError), _destinationFeeMoney, DestinationMoneyCode, ShowDestinationFee, value => DestinationFeeError = value);
        Feedback(nameof(ForeignAmountError), _foreignMoney, ForeignCurrency, ForeignEnabled && !IsTransfer && Currencies.TryGet(ForeignCurrency, out _), value => ForeignAmountError = value);
        Feedback(nameof(ReimbursableError), _reimbursableMoney, SourceMoneyCode, ReimbursableEnabled && IsExpense, value => ReimbursableError = value);
        foreach (var name in new[] { nameof(AmountUnitName), nameof(DestinationAmountUnitName), nameof(FeeUnitName), nameof(DestinationFeeUnitName), nameof(ForeignUnitName),
            nameof(ReimbursableUnitName), nameof(AmountFormatNote), nameof(DestinationAmountFormatNote), nameof(FeeFormatNote), nameof(DestinationFeeFormatNote),
            nameof(ForeignFormatNote), nameof(ReimbursableFormatNote), nameof(CanSave), nameof(CanRepeat) }) OnPropertyChanged(name);
    }
    private EntryValidationInput WithMoneyContexts(EntryValidationInput input) => input with
    {
        AmountContext = _amountMoney.Context, DestinationAmountContext = _destinationMoney.Context, FeeContext = _feeMoney.Context,
        DestinationFeeContext = _destinationFeeMoney.Context, ForeignContext = _foreignMoney.Context, ReimbursableContext = _reimbursableMoney.Context,
    };
}
