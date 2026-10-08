using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Core.Text;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Accounts;

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
        DueDate = OpeningDate.AddMonths(1);
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

    /// <summary>Gets what keeps the currency from changing, e.g. "2 entries, 1 plan" (ZEX-S0105).</summary>
    [ObservableProperty]
    public partial string? CurrencyLockText { get; set; }

    /// <summary>Gets or sets a value indicating whether the money of the account can pay bills (ZEX-P17).</summary>
    [ObservableProperty]
    public partial bool UsableForPayments { get; set; } = true;

    /// <summary>Gets or sets the optional country code of the account (ZEX-P18), information only.</summary>
    [ObservableProperty]
    public partial string CountryCode { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the Advanced fields (usable for payments, country) are shown.</summary>
    [ObservableProperty]
    public partial bool ShowAdvanced { get; set; }

    // Loans and lent money (F2-DEBT-01).
    [ObservableProperty]
    public partial string Counterparty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDebtType { get; set; }

    /// <summary>Gets or sets a value indicating whether money lent has a day it should be paid back by (ZEX-K12).</summary>
    [ObservableProperty]
    public partial bool HasDueDate { get; set; }

    [ObservableProperty]
    public partial DateOnly DueDate { get; set; }

    [ObservableProperty]
    public partial bool IsLent { get; set; }

    // Optional loan terms for the repayment estimate (F2-DEBT-02).
    [ObservableProperty]
    public partial string RateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InstallmentText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? TermsError { get; set; }

    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    partial void OnCurrencyCodeChanged(string value) => UnitNote = DisplayUnitNote.For(_translator, value);

    [ObservableProperty]
    public partial string? DebtHint { get; set; }

    /// <summary>Gets the two plain-language directions in the dedicated debt form.</summary>
    public IReadOnlyList<string> DebtDirectionNames => [_translator["Debt_IOwe"], _translator["Debt_OwedToMe"]];

    /// <summary>Gets or sets the debt direction independently of generic account types.</summary>
    [ObservableProperty]
    public partial int DebtDirectionIndex { get; set; }

    /// <summary>Gets or sets whether optional interest and repayment estimates are expanded.</summary>
    [ObservableProperty]
    public partial bool ShowDebtTerms { get; set; }

    /// <summary>Gets or sets whether a debt's icon and account options are expanded.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAccountDetails))]
    public partial bool ExpandedAccountDetails { get; set; }

    /// <summary>Gets a value indicating whether account details are visible without cluttering the debt form.</summary>
    public bool ShowAccountDetails => !IsDebtType || ExpandedAccountDetails;

    /// <summary>Gets a value indicating whether the general account explanation belongs in the main form.</summary>
    public bool ShowAccountingHint => !IsDebtType && DebtHint is not null;

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleAccountDetails() => ExpandedAccountDetails = !ExpandedAccountDetails;

    /// <summary>Gets a value indicating whether a generic account's sign can be entered.</summary>
    public bool ShowNegative => !IsDebtType && !OpeningUnknown;

    /// <summary>Gets the amount label at the historical reference date, never the current ledger balance.</summary>
    public string OpeningLabel => _translator[Type == AccountType.Loan ? "Debt_BalanceOwed" : Type == AccountType.Lent ? "Debt_BalanceReceivable" : "Account_OpeningBalance"];

    /// <summary>Gets the counterparty label matching the chosen direction.</summary>
    public string CounterpartyLabel => _translator[IsLent ? "Debt_WhoOwes" : "Debt_ToWhom"];

    /// <summary>Gets a reference-date label suited to debts.</summary>
    public string OpeningDateLabel => _translator[IsDebtType ? "Debt_AsOf" : "Account_OpeningDate"];

    /// <summary>Gets the unknown-amount checkbox wording.</summary>
    public string UnknownLabel => _translator[IsDebtType ? "Debt_AmountUnknown" : "Account_OpeningUnknown"];

    /// <summary>Gets the explanation of recording an existing balance rather than a new money movement.</summary>
    public string OpeningHint => _translator[IsDebtType ? "Debt_OpeningHint" : "Account_OpeningHint"];

    /// <summary>Gets the contextual amount-help topic.</summary>
    public string OpeningHelpTopic => IsDebtType ? "DebtOpening" : "OpeningBalance";

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleDebtTerms() => ShowDebtTerms = !ShowDebtTerms;

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleUnknown() => OpeningUnknown = !OpeningUnknown;

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleNegative() => OpeningIsNegative = !OpeningIsNegative;

    partial void OnDebtDirectionIndexChanged(int value)
    {
        if (IsDebtType)
        {
            TypeIndex = (int)(value == 1 ? AccountType.Lent : AccountType.Loan);
        }
    }

    partial void OnOpeningUnknownChanged(bool value) => OnPropertyChanged(nameof(ShowNegative));

    private bool _isNew = true;

    // A new loan starts as money owed and outside the liquid total; both remain changeable (ACC-05).
    partial void OnTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(PreviewIcon));
        IsDebtType = Type.IsDebt();
        IsLent = Type == AccountType.Lent;
        if (IsDebtType)
        {
            DebtDirectionIndex = IsLent ? 1 : 0;
        }

        foreach (var property in new[] { nameof(ShowAccountingHint), nameof(ShowAccountDetails), nameof(ShowNegative), nameof(OpeningLabel), nameof(CounterpartyLabel), nameof(OpeningDateLabel),
            nameof(UnknownLabel), nameof(OpeningHint), nameof(OpeningHelpTopic) })
        {
            OnPropertyChanged(property);
        }
        DebtHint = Type switch
        {
            AccountType.Loan => _translator["Account_LoanHint"],
            AccountType.Lent => _translator["Account_LentHint"],
            AccountType.Asset => _translator["Account_AssetHint"],
            _ => null,
        };
        OnPropertyChanged(nameof(ShowAccountingHint));
        if (_isNew && Type.IsOutsideCash())
        {
            IncludeInTotals = false;
            OpeningIsNegative = Type == AccountType.Loan;
        }

        // Cash, checking and savings pay bills by default; cards, debts, receivables and assets do not (ZEX-P17).
        if (_isNew)
        {
            UsableForPayments = Type.IsUsableByDefault();
        }
    }

    /// <summary>Gets or sets the chosen icon; <see langword="null"/> uses the icon of the account type (ACC-01).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewIcon))]
    public partial string? IconKey { get; set; }

    /// <summary>Gets the icon the account will show: the chosen one or that of its type.</summary>
    public FluentIcons.Common.Symbol PreviewIcon => Icons.Parse(IconKey, Icons.For(Type));

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

    /// <summary>Re-translates account types and debt intent labels without replacing the form draft.</summary>
    public void RefreshTexts()
    {
        TypeNames = [.. Enum.GetValues<AccountType>().Select(t => _translator[$"AccountType_{t}"])];
        foreach (var property in new[] { nameof(DebtDirectionNames), nameof(OpeningLabel), nameof(CounterpartyLabel),
            nameof(OpeningDateLabel), nameof(UnknownLabel), nameof(OpeningHint), nameof(OpeningHelpTopic) })
        {
            OnPropertyChanged(property);
        }
    }

    /// <summary>Fills the form from an account.</summary>
    public void Load(Account account, CultureInfo culture, AccountCurrencyLock currencyLock)
    {
        ArgumentNullException.ThrowIfNull(currencyLock);
        ArgumentNullException.ThrowIfNull(account);
        Name = account.Name;
        TypeIndex = (int)account.Type;
        CurrencyCode = account.CurrencyCode;
        OpeningText = account.OpeningBalance == 0 ? string.Empty : MoneyText.ForInput(account.OpeningBalance, account.CurrencyCode, culture);
        OpeningIsNegative = account.OpeningBalance < 0;
        OpeningUnknown = !account.OpeningBalanceKnown;
        OpeningDate = account.OpeningDate;
        IncludeInTotals = account.IncludeInTotals;
        CurrencyLocked = currencyLock.IsLocked;
        CurrencyLockText = currencyLock.IsLocked ? _translator.Format("Account_CurrencyLockedBy", LockReasons(currencyLock)) : null;
        UsableForPayments = account.UsableForPayments;
        CountryCode = account.CountryCode ?? string.Empty;
        IconKey = account.Icon;
        Counterparty = account.Counterparty ?? string.Empty;
        HasDueDate = account.DueDate is not null;
        DueDate = account.DueDate ?? DueDate;
        RateText = account.InterestRate is { } rate ? rate.ToString("0.##########", culture) : string.Empty;
        InstallmentText = account.Installment is { } installment ? MoneyText.ForInput(installment, account.CurrencyCode, culture) : string.Empty;
        _isNew = false;
        OnTypeIndexChanged(TypeIndex);
        ShowDebtTerms = account.InterestRate is not null || account.Installment is not null;
    }

    /// <summary>Validates the input and writes it to <paramref name="target"/>.</summary>
    /// <param name="target">The account to change.</param>
    /// <param name="culture">The culture of the typed amounts.</param>
    /// <param name="others">The accounts of the profile, so a name cannot be used twice (<see cref="AccountNames"/>).</param>
    /// <returns><see langword="false"/> when the input is invalid; the error texts are set.</returns>
    public bool TryApply(Account target, CultureInfo culture, IEnumerable<Account>? others = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        NameError = string.IsNullOrWhiteSpace(Name) ? _translator["Account_NameRequired"]
            : others is not null && AccountNames.IsTaken(others, Name, target.Id) ? _translator["Account_NameTaken"]
            : null;

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
            if (opening < 0)
            {
                AmountError = _translator["Debt_EnterPositive"];
            }

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
            ShowDebtTerms |= TermsError is not null;
            return false;
        }

        var signedOpening = Type.IsDebt() ? DebtSetup.OpeningBalance(Type, opening) : OpeningIsNegative ? -opening : opening;
        // Older accounts can have a reversed opening balance (e.g. a loan credit). Editing their name/terms must
        // preserve that historical sign when the type and amount have not changed.
        if (!_isNew && Type.IsDebt() && target.Type == Type && (target.OpeningBalance == opening || target.OpeningBalance == -opening))
        {
            signedOpening = target.OpeningBalance;
        }

        target.Name = Name.Trim();
        target.Type = Type;
        target.CurrencyCode = CurrencyCode;
        target.OpeningBalance = signedOpening;
        target.OpeningDate = OpeningDate;
        target.OpeningBalanceKnown = !OpeningUnknown;
        if (OpeningUnknown)
        {
            target.OpeningBalance = 0;
        }

        target.IncludeInTotals = IncludeInTotals;
        target.UsableForPayments = UsableForPayments;
        var country = CountryCode.Trim().ToUpperInvariant();
        target.CountryCode = country.Length == 2 && country.All(char.IsAsciiLetterUpper) ? country : null;
        target.Icon = IconKey;
        target.Counterparty = Type.IsDebt() && !string.IsNullOrWhiteSpace(Counterparty) ? Counterparty.Trim() : null;
        target.DueDate = Type == AccountType.Lent && HasDueDate ? DueDate : null;
        target.InterestRate = Type.IsDebt() ? rate : null;
        target.Installment = Type.IsDebt() && installment > 0 ? installment : null;
        return true;
    }

    /// <summary>Returns a value that changes whenever the user changes something (for "discard changes?").</summary>
    public string Snapshot() => Presentation.UnsavedChanges.Fingerprint( Name, TypeIndex, CurrencyCode, OpeningText, OpeningIsNegative, OpeningUnknown, OpeningDate, IncludeInTotals, IconKey, Counterparty, RateText, InstallmentText, UsableForPayments, CountryCode, HasDueDate, DueDate);

    // "3 entries, 1 plan and the loan installment" in the current language.
    private string LockReasons(AccountCurrencyLock currencyLock)
    {
        var parts = new List<string>();
        void Add(int count, string key)
        {
            if (count > 0)
            {
                parts.Add(_translator.Format(key, count));
            }
        }

        Add(currencyLock.Entries, "Account_LockEntries");
        Add(currencyLock.Plans, "Account_LockPlans");
        Add(currencyLock.Templates, "Account_LockTemplates");
        Add(currencyLock.Earmarks, "Account_LockEarmarks");
        Add(currencyLock.Budgets, "Account_LockBudgets");
        if (currencyLock.Installment)
        {
            parts.Add(_translator["Account_LockInstallment"]);
        }

        return string.Join(_translator["Reminder_ListSeparator"], parts);
    }
}
