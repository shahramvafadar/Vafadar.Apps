using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Core.Text;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Money;
using Vafadar.Localization;

namespace Vafadar.Finance.App.Features.Accounts;

/// <summary>
/// The editable fields of an account, shared by the account editor and onboarding (ACC-01, FIN-04, ONB-03).
/// </summary>
public sealed partial class AccountFormModel : ObservableObject
{
    private readonly Translator _translator;

    public AccountFormModel(Translator translator, TimeProvider time)
    {
        _translator = translator;
        OpeningDate = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        CurrencyCode = Currencies.Euro.Code;
        Name = string.Empty;
        OpeningText = string.Empty;
        TypeNames = [];
        TypeIndex = (int)AccountType.Checking;
        IncludeInTotals = true;
        RefreshTexts();
    }

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    [ObservableProperty]
    public partial IReadOnlyList<string> TypeNames { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial int TypeIndex { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; }

    [ObservableProperty]
    public partial string OpeningText { get; set; }

    [ObservableProperty]
    public partial bool OpeningIsNegative { get; set; }

    // The opening balance may be unknown; balances are then marked incomplete until a reconciliation (ACC-09).
    [ObservableProperty]
    public partial bool OpeningUnknown { get; set; }

    [ObservableProperty]
    public partial DateOnly OpeningDate { get; set; }

    [ObservableProperty]
    public partial bool IncludeInTotals { get; set; }

    [ObservableProperty]
    public partial bool CurrencyLocked { get; set; }

    // Loans and lent money (F2-DEBT-01).
    [ObservableProperty]
    public partial string Counterparty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDebtType { get; set; }

    // Optional loan terms for the repayment estimate (F2-DEBT-02).
    [ObservableProperty]
    public partial string RateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InstallmentText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? TermsError { get; set; }

    [ObservableProperty]
    public partial string? DebtHint { get; set; }

    private bool _isNew = true;

    // A new loan starts as money owed and outside the liquid total; both remain changeable (ACC-05).
    partial void OnTypeIndexChanged(int value)
    {
        IsDebtType = Type.IsDebt();
        DebtHint = Type switch
        {
            AccountType.Loan => _translator["Account_LoanHint"],
            AccountType.Lent => _translator["Account_LentHint"],
            AccountType.Asset => _translator["Account_AssetHint"],
            _ => null,
        };
        if (_isNew && Type.IsOutsideCash())
        {
            IncludeInTotals = false;
            OpeningIsNegative = Type == AccountType.Loan;
        }
    }

    /// <summary>Gets or sets the chosen icon; <see langword="null"/> uses the icon of the account type (ACC-01).</summary>
    [ObservableProperty]
    public partial string? IconKey { get; set; }

    [ObservableProperty]
    public partial bool ShowIconPicker { get; set; }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleIconPicker() => ShowIconPicker = !ShowIconPicker;

    [ObservableProperty]
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial string? AmountError { get; set; }

    /// <summary>Gets the selected account type.</summary>
    public AccountType Type => Enum.IsDefined((AccountType)TypeIndex) ? (AccountType)TypeIndex : AccountType.Checking;

    /// <summary>Re-translates the type names after a language change.</summary>
    public void RefreshTexts() =>
        TypeNames = [.. Enum.GetValues<AccountType>().Select(t => _translator[$"AccountType_{t}"])];

    /// <summary>Fills the form from an account.</summary>
    public void Load(Account account, CultureInfo culture, bool currencyLocked)
    {
        ArgumentNullException.ThrowIfNull(account);
        Name = account.Name;
        TypeIndex = (int)account.Type;
        CurrencyCode = account.CurrencyCode;
        OpeningText = account.OpeningBalance == 0 ? string.Empty : MoneyText.ForInput(account.OpeningBalance, account.CurrencyCode, culture);
        OpeningIsNegative = account.OpeningBalance < 0;
        OpeningUnknown = !account.OpeningBalanceKnown;
        OpeningDate = account.OpeningDate;
        IncludeInTotals = account.IncludeInTotals;
        CurrencyLocked = currencyLocked;
        IconKey = account.Icon;
        Counterparty = account.Counterparty ?? string.Empty;
        RateText = account.InterestRate is { } rate ? rate.ToString("0.###", culture) : string.Empty;
        InstallmentText = account.Installment is { } installment ? MoneyText.ForInput(installment, account.CurrencyCode, culture) : string.Empty;
        _isNew = false;
        OnTypeIndexChanged(TypeIndex);
    }

    /// <summary>Validates the input and writes it to <paramref name="target"/>.</summary>
    /// <returns><see langword="false"/> when the input is invalid; the error texts are set.</returns>
    public bool TryApply(Account target, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(target);
        NameError = string.IsNullOrWhiteSpace(Name) ? _translator["Account_NameRequired"] : null;

        long opening = 0;
        AmountError = null;
        if (!OpeningUnknown && !string.IsNullOrWhiteSpace(OpeningText) && !MoneyText.TryParse(OpeningText, Currencies.Get(CurrencyCode), culture, out opening))
        {
            AmountError = _translator["Amount_Invalid"];
        }

        decimal? rate = null;
        long installment = 0;
        TermsError = null;
        if (Type.IsDebt())
        {
            // A rate is a plain number with the culture's or a Latin decimal point, in any digit script.
            var rateText = Digits.ToAscii(RateText.Trim()).Replace(culture.NumberFormat.NumberDecimalSeparator, ".", StringComparison.Ordinal).Replace('٫', '.').TrimEnd('%', '٪').Trim();
            if (rateText.Length > 0)
            {
                rate = decimal.TryParse(rateText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) && parsed <= 100 ? parsed : null;
                TermsError = rate is null ? _translator["Account_RateInvalid"] : null;
            }

            if (!string.IsNullOrWhiteSpace(InstallmentText) && !MoneyText.TryParse(InstallmentText, Currencies.Get(CurrencyCode), culture, out installment))
            {
                TermsError = _translator["Amount_Invalid"];
            }
        }

        if (NameError is not null || AmountError is not null || TermsError is not null)
        {
            return false;
        }

        target.Name = Name.Trim();
        target.Type = Type;
        target.CurrencyCode = CurrencyCode;
        target.OpeningBalance = OpeningIsNegative ? -opening : opening;
        target.OpeningDate = OpeningDate;
        target.OpeningBalanceKnown = !OpeningUnknown;
        if (OpeningUnknown)
        {
            target.OpeningBalance = 0;
        }
        target.IncludeInTotals = IncludeInTotals;
        target.Icon = IconKey;
        target.Counterparty = Type.IsDebt() && !string.IsNullOrWhiteSpace(Counterparty) ? Counterparty.Trim() : null;
        target.InterestRate = Type.IsDebt() ? rate : null;
        target.Installment = Type.IsDebt() && installment > 0 ? installment : null;
        return true;
    }

    /// <summary>Returns a value that changes whenever the user changes something (for "discard changes?").</summary>
    public string Snapshot() => string.Join('|', Name, TypeIndex, CurrencyCode, OpeningText, OpeningIsNegative, OpeningUnknown, OpeningDate, IncludeInTotals, IconKey, Counterparty, RateText, InstallmentText);
}
