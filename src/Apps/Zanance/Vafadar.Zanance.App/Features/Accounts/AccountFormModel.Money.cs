using System.Globalization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Accounts;

public sealed partial class AccountFormModel
{
    private readonly MoneyDraftInput _openingMoney = new(), _installmentMoney = new();
    private bool _loadingMoney;
    private bool _openingContextError, _installmentContextError;
    private CultureInfo? _rateCulture;
    private long _rateRevision;
    private CultureInfo InputCulture => _localization?.CurrentCulture ?? CultureInfo.CurrentCulture;
    /// <summary>Gets the historical opening input's independent original unit.</summary>
    public string OpeningUnitName => _openingMoney.UnitName(CurrencyCode);
    /// <summary>Explains the optional repayment's original display-unit factor.</summary>
    public string? InstallmentUnitNote => DisplayUnitNote.ForInput(_translator, _installmentMoney, CurrencyCode);
    /// <summary>Gets the optional installment's independent original unit.</summary>
    public string InstallmentUnitName => _installmentMoney.UnitName(CurrencyCode);
    /// <summary>Explains the historical balance's retained parser after a display change.</summary>
    public string? OpeningFormatNote => !OpeningUnknown ? FormatNote(_openingMoney) : null;
    /// <summary>Explains the optional repayment input's retained parser independently of the principal.</summary>
    public string? InstallmentFormatNote => IsDebtType ? FormatNote(_installmentMoney) : null;
    /// <summary>Gets whether every applicable nonempty monetary input retains its original currency and unit.</summary>
    public bool MoneyContextsReady => (OpeningUnknown || !_openingMoney.NeedsReview(CurrencyCode))
        && (!IsDebtType || !_installmentMoney.NeedsReview(CurrencyCode));
    /// <summary>Gets an edit-and-return sensitive token for the original pending financial writer.</summary>
    public string MoneyRevision => $"{_openingMoney.Revision}:{_installmentMoney.Revision}:{_rateRevision}";
    private string MoneyMeaning => DraftFingerprint.Create(_openingMoney.Stamp, _installmentMoney.Stamp,
        string.IsNullOrWhiteSpace(RateText) ? null : _rateCulture?.NumberFormat.NumberDecimalSeparator);
    private string? FormatNote(MoneyDraftInput input) => input.OriginalExample(InputCulture) is { } example
        ? _translator.Format("Plan_OriginalInputFormat", example) : null;
    partial void OnOpeningTextChanged(string value)
    {
        if (_loadingMoney) return;
        _openingMoney.ChangeText(value, CurrencyCode, InputCulture); AmountError = null; RefreshMoneyTexts();
    }
    partial void OnInstallmentTextChanged(string value)
    {
        if (_loadingMoney) return;
        _installmentMoney.ChangeText(value, CurrencyCode, InputCulture); TermsError = null; RefreshMoneyTexts();
    }
    partial void OnRateTextChanging(string value)
    {
        if (!_loadingMoney && (string.IsNullOrWhiteSpace(RateText) || string.IsNullOrWhiteSpace(value)))
            _rateCulture = CultureInfo.ReadOnly((CultureInfo)InputCulture.Clone());
    }
    partial void OnRateTextChanged(string value) { if (!_loadingMoney) _rateRevision++; }
    /// <summary>Refreshes captions and original-format examples without renewing typed money.</summary>
    public void RefreshMoneyTexts()
    {
        if (_loadingMoney) return;
        var openingStale = !OpeningUnknown && _openingMoney.NeedsReview(CurrencyCode);
        var installmentStale = IsDebtType && _installmentMoney.NeedsReview(CurrencyCode);
        if (openingStale) AmountError = _translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(CurrencyCode));
        else if (_openingContextError) AmountError = null;
        if (installmentStale)
        {
            TermsError = _translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(CurrencyCode)); ShowDebtTerms = true;
        }
        else if (_installmentContextError) TermsError = null;
        _openingContextError = openingStale; _installmentContextError = installmentStale;
        UnitNote = DisplayUnitNote.ForInput(_translator, _openingMoney, CurrencyCode);
        foreach (var name in new[] { nameof(OpeningUnitName), nameof(InstallmentUnitName), nameof(OpeningFormatNote),
            nameof(InstallmentFormatNote), nameof(InstallmentUnitNote), nameof(MoneyContextsReady) }) OnPropertyChanged(name);
    }
    private string? ValidateMoney(MoneyDraftInput input, bool applicable, out long amount)
    {
        amount = 0;
        if (!applicable || string.IsNullOrWhiteSpace(input.Text)) return null;
        if (input.NeedsReview(CurrencyCode)) return _translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(CurrencyCode));
        return input.TryParse(out amount) ? null : _translator["Amount_Invalid"];
    }
}
