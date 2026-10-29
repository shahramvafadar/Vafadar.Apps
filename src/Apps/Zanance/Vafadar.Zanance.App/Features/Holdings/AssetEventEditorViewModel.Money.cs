using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using System.Globalization;

namespace Vafadar.Zanance.App.Features.Holdings;

public sealed partial class AssetEventEditorViewModel
{
    private readonly MoneyDraftInput _amountMoney = new(), _feeMoney = new(), _secondMoney = new(), _basisMoney = new();
    private CultureInfo? _quantityCulture;
    private long _quantityRevision, _moneyChoiceRevision;
    private string PaymentCurrency => Account?.CurrencyCode ?? _type?.PriceCurrencyCode ?? "EUR";
    private string PriceCurrency => _type?.PriceCurrencyCode ?? "EUR";
    private string MoneyRevision => $"{_amountMoney.Revision}:{_feeMoney.Revision}:{_secondMoney.Revision}:{_basisMoney.Revision}:{_quantityRevision}:{_moneyChoiceRevision}";
    private string MoneyMeaning => DraftFingerprint.Create(_amountMoney.Stamp, _feeMoney.Stamp, _secondMoney.Stamp, _basisMoney.Stamp,
        string.IsNullOrWhiteSpace(QuantityText) ? null : _quantityCulture?.NumberFormat.NumberDecimalSeparator);
    /// <summary>Explains the original quantity parser that also determines a per-unit payment's total.</summary>
    public string? QuantityFormatNote => !string.IsNullOrWhiteSpace(QuantityText) && _quantityCulture is { } culture
        && (culture.NumberFormat.NumberDecimalSeparator != _localization.CurrentCulture.NumberFormat.NumberDecimalSeparator
            || culture.NumberFormat.NumberGroupSeparator != _localization.CurrentCulture.NumberFormat.NumberGroupSeparator)
        ? _translator.Format("Plan_OriginalInputFormat", $"\u2066\u200E{1234.5m.ToString("N3", culture)}\u200E\u2069") : null;
    partial void OnQuantityTextChanging(string value)
    {
        if (!_loading && (string.IsNullOrWhiteSpace(QuantityText) || string.IsNullOrWhiteSpace(value)))
            _quantityCulture = CultureInfo.ReadOnly((CultureInfo)_localization.CurrentCulture.Clone());
    }
    /// <summary>Gets whether each applicable raw monetary field still has its original currency and unit.</summary>
    public bool MoneyContextsReady => (!HasMoney || !_amountMoney.NeedsReview(PaymentCurrency) && !_feeMoney.NeedsReview(PaymentCurrency)
        && (!HasSecondAmount || !_secondMoney.NeedsReview(PriceCurrency))) && (!HasBasis || !_basisMoney.NeedsReview(PriceCurrency));
    /// <summary>Gets the original payment input unit until complete clearing and reentry.</summary>
    public string AmountUnitName => _amountMoney.UnitName(PaymentCurrency);
    /// <summary>Gets the independent original fee input unit.</summary>
    public string FeeUnitName => _feeMoney.UnitName(PaymentCurrency);
    /// <summary>Gets the independent original price-currency amount unit.</summary>
    public string SecondUnitName => _secondMoney.UnitName(PriceCurrency);
    /// <summary>Gets the original optional opening/gift cost-basis unit.</summary>
    public string BasisUnitName => _basisMoney.UnitName(PriceCurrency);
    /// <summary>Explains the payment's original unit factor without consulting its new display preference.</summary>
    public string? AmountUnitNote => DisplayUnitNote.ForInput(_translator, _amountMoney, PaymentCurrency);
    /// <summary>Explains the fee's independent original unit factor.</summary>
    public string? FeeUnitNote => DisplayUnitNote.ForInput(_translator, _feeMoney, PaymentCurrency);
    /// <summary>Explains the price-currency amount's independent original unit factor.</summary>
    public string? SecondUnitNote => DisplayUnitNote.ForInput(_translator, _secondMoney, PriceCurrency);
    /// <summary>Explains the optional basis's independent original unit factor.</summary>
    public string? BasisUnitNote => DisplayUnitNote.ForInput(_translator, _basisMoney, PriceCurrency);
    /// <summary>Explains the payment's retained original numeric format.</summary>
    public string? AmountFormatNote => FormatNote(_amountMoney);
    /// <summary>Explains the fee's retained original numeric format.</summary>
    public string? FeeFormatNote => FormatNote(_feeMoney);
    /// <summary>Explains the separate price-currency amount's retained original numeric format.</summary>
    public string? SecondFormatNote => FormatNote(_secondMoney);
    /// <summary>Explains the optional basis's retained original numeric format.</summary>
    public string? BasisFormatNote => FormatNote(_basisMoney);
    /// <summary>Gets field-local payment currency/unit refusal.</summary>
    public string? AmountContextError => ContextError(_amountMoney, PaymentCurrency, HasMoney);
    /// <summary>Gets field-local fee currency/unit refusal independently of refund validation.</summary>
    public string? FeeContextError => ContextError(_feeMoney, PaymentCurrency, HasMoney);
    /// <summary>Gets field-local second-amount currency/unit refusal.</summary>
    public string? SecondContextError => ContextError(_secondMoney, PriceCurrency, HasSecondAmount);
    /// <summary>Gets invalid nonblank price-currency feedback without silently turning it into unknown value.</summary>
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    public partial string? SecondAmountError { get; set; }
    /// <summary>Gets field-local optional basis currency/unit refusal.</summary>
    public string? BasisContextError => ContextError(_basisMoney, PriceCurrency, HasBasis);
    private string? FormatNote(MoneyDraftInput input) => input.OriginalExample(_localization.CurrentCulture) is { } example
        ? _translator.Format("Plan_OriginalInputFormat", example) : null;
    private string? ContextError(MoneyDraftInput input, string currency, bool applicable) => applicable && input.NeedsReview(currency)
        ? _translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(currency)) : null;
    private void ChangeMoney(MoneyDraftInput input, string raw, string currency)
    {
        if (_loading) return;
        input.ChangeText(raw, currency, _localization.CurrentCulture); RefreshMoneyTexts();
    }
    /// <summary>Publishes current captions and refusals without changing raw amounts or their original meaning.</summary>
    public void RefreshMoneyTexts()
    {
        foreach (var name in new[] { nameof(AmountUnitName), nameof(FeeUnitName), nameof(SecondUnitName), nameof(BasisUnitName),
            nameof(AmountFormatNote), nameof(FeeFormatNote), nameof(SecondFormatNote), nameof(BasisFormatNote),
            nameof(AmountUnitNote), nameof(FeeUnitNote), nameof(SecondUnitNote), nameof(BasisUnitNote),
            nameof(AmountContextError), nameof(FeeContextError), nameof(SecondContextError), nameof(BasisContextError), nameof(MoneyContextsReady), nameof(QuantityFormatNote) })
            OnPropertyChanged(name);
        PublishState();
    }
    private void InitializeMoney()
    {
        _amountMoney.Initialize(AmountText, PaymentCurrency, _localization.CurrentCulture);
        _feeMoney.Initialize(FeeText, PaymentCurrency, _localization.CurrentCulture);
        _secondMoney.Initialize(SecondAmountText, PriceCurrency, _localization.CurrentCulture);
        _basisMoney.Initialize(BasisText, PriceCurrency, _localization.CurrentCulture);
        _quantityCulture = CultureInfo.ReadOnly((CultureInfo)_localization.CurrentCulture.Clone()); _quantityRevision++;
    }
    private static bool ParseMoney(MoneyDraftInput input, string currency, out long amount)
    {
        amount = 0;
        return !input.NeedsReview(currency) && input.TryParse(out amount);
    }
    partial void OnSecondAmountTextChanged(string value) { SecondAmountError = null; ChangeMoney(_secondMoney, value, PriceCurrency); UpdateEffect(); }
    partial void OnBasisTextChanged(string value) { ChangeMoney(_basisMoney, value, PriceCurrency); UpdateEffect(); }
    private void OnMoneyDisplayChanged(object? sender, EventArgs args) => RefreshDisplay();
    /// <summary>Tracks display captions only while the original actual page is presented.</summary>
    public void SetMoneyPresentationActive(bool active)
    {
        _localization.Changed -= OnMoneyDisplayChanged;
        if (active) _localization.Changed += OnMoneyDisplayChanged;
        RefreshMoneyTexts();
    }
}
