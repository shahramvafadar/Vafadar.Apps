using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
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
    }

    /// <summary>Validates the input and writes it to <paramref name="target"/>.</summary>
    /// <returns><see langword="false"/> when the input is invalid; the error texts are set.</returns>
    public bool TryApply(Account target, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(target);
        NameError = string.IsNullOrWhiteSpace(Name) ? _translator["Account_NameRequired"] : null;

        long opening = 0;
        AmountError = null;
        if (!OpeningUnknown && !string.IsNullOrWhiteSpace(OpeningText) && !MoneyAmount.TryParse(OpeningText, Currencies.Get(CurrencyCode), culture, out opening))
        {
            AmountError = _translator["Amount_Invalid"];
        }

        if (NameError is not null || AmountError is not null)
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
        return true;
    }

    /// <summary>Returns a value that changes whenever the user changes something (for "discard changes?").</summary>
    public string Snapshot() => string.Join('|', Name, TypeIndex, CurrencyCode, OpeningText, OpeningIsNegative, OpeningUnknown, OpeningDate, IncludeInTotals, IconKey);
}
