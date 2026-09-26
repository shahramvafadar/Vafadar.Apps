using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Features.Reports;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Core.Reports;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Accounts;

/// <summary>
/// Account details and manual reconciliation (UI-06, ACC-08): the recorded balance, what moved it this month, and a
/// comparison with the balance the user observes – with suggestions before an adjustment is recorded.
/// </summary>
public sealed partial class AccountDetailViewModel(
    FinanceStore store,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    TimeProvider time) : ViewModelBase, IQueryAttributable
{
    private Guid _id;
    private Account? _account;
    private ReconciliationResult? _result;

    public ObservableCollection<AmountLine> Movement { get; } = [];

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? TypeName { get; set; }

    [ObservableProperty]
    public partial Symbol Icon { get; set; }

    [ObservableProperty]
    public partial string? BalanceText { get; set; }

    [ObservableProperty]
    public partial string? ConfirmedText { get; set; }

    [ObservableProperty]
    public partial string? MonthText { get; set; }

    [ObservableProperty]
    public partial string ObservedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateOnly ReconcileDate { get; set; }

    [ObservableProperty]
    public partial string? ResultText { get; set; }

    [ObservableProperty]
    public partial string? HintText { get; set; }

    [ObservableProperty]
    public partial bool CanAdjust { get; set; }

    [ObservableProperty]
    public partial string Reason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            _id = id;
        }
    }

    public async Task LoadAsync()
    {
        _account = (await store.GetAccountsAsync()).FirstOrDefault(a => a.Id == _id);
        if (_account is not { } account)
        {
            return;
        }

        var culture = localization.CurrentCulture;
        var entries = await store.GetEntriesAsync();
        var today = Today;
        if (ReconcileDate == default)
        {
            ReconcileDate = today;
        }

        Name = account.Name;
        TypeName = translator[$"AccountType_{account.Type}"];
        Icon = Icons.Parse(account.Icon, Icons.For(account.Type));
        IsArchived = account.IsArchived;

        // Posted balance and, when unreviewed entries exist, the confirmed-only balance next to it (FIN-12).
        var balance = LedgerCalculator.Balance(account, entries, today);
        var confirmed = LedgerCalculator.Balance(account, entries, today, confirmedOnly: true);
        BalanceText = MoneyText.Format(balance, account.CurrencyCode, culture);
        ConfirmedText = confirmed != balance
            ? translator.Format("Account_ConfirmedOnly", MoneyText.Format(confirmed, account.CurrencyCode, culture))
            : null;

        var calendar = localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;
        var (year, month) = PeriodMath.MonthOf(today, calendar);
        var (from, to) = PeriodMath.MonthRange(year, month, calendar);
        MonthText = dates.Format(from, DateFormatStyle.MonthYear);
        var movement = ReportCalculator.AccountMovements([account], entries, from, to).Single();
        string Format(long value, bool plus = false) => MoneyText.Format(value, account.CurrencyCode, culture, showPlus: plus);
        Movement.Clear();
        Movement.Add(new AmountLine(translator["Report_Opening"], Format(movement.Opening), true));
        void Add(string key, long value, bool negative = false)
        {
            if (value != 0)
            {
                Movement.Add(new AmountLine(translator[key], Format(negative ? -value : value, plus: true), false));
            }
        }

        Add("Report_OpeningBalance", movement.OpeningBalanceAdded);
        Add("KindFilter_Income", movement.Income);
        Add("Report_Refunds", movement.Refunds);
        Add("KindFilter_Expenses", movement.Expense, negative: true);
        Add("EntryKind_IncomeReversal", movement.IncomeReversals, negative: true);
        Add("Report_TransfersIn", movement.TransfersIn);
        Add("Report_TransfersOut", movement.TransfersOut, negative: true);
        Add("Report_Adjustments", movement.Adjustments);
        Movement.Add(new AmountLine(translator["Report_Closing"], Format(movement.Closing), true));
    }

    [RelayCommand]
    private async Task CompareAsync()
    {
        Error = null;
        CanAdjust = false;
        if (_account is not { } account)
        {
            return;
        }

        var currency = Currencies.TryGet(account.CurrencyCode, out var known) ? known : Currencies.Euro;
        var text = ObservedText.Trim();
        var negative = text.StartsWith('-') || text.StartsWith('−');
        if (!MoneyAmount.TryParse(text.TrimStart('-', '−'), currency, localization.CurrentCulture, out var observed))
        {
            Error = translator["Amount_Invalid"];
            return;
        }

        observed = negative ? -observed : observed;
        _result = Reconciliation.Analyze(account, await store.GetEntriesAsync(), observed, ReconcileDate);
        var culture = localization.CurrentCulture;
        ResultText = _result.Difference == 0
            ? translator["Reconcile_Match"]
            : translator.Format("Reconcile_Difference", MoneyText.Format(_result.Recorded, account.CurrencyCode, culture),
                MoneyText.Format(_result.Difference, account.CurrencyCode, culture, showPlus: true));

        // First suggest reviewing entries, only then an adjustment (ACC-08).
        var hints = new List<string>();
        if (_result.UnreviewedCount > 0)
        {
            hints.Add(translator.Format("Reconcile_Unreviewed", _result.UnreviewedCount));
        }

        if (_result.PossibleDuplicates > 0)
        {
            hints.Add(translator.Format("Reconcile_Duplicates", _result.PossibleDuplicates));
        }

        if (_result.Difference != 0)
        {
            hints.Add(translator["Reconcile_Missing"]);
        }

        HintText = hints.Count > 0 ? string.Join(Environment.NewLine, hints) : null;
        CanAdjust = _result.Difference != 0;
    }

    [RelayCommand]
    private async Task AdjustAsync()
    {
        if (_account is null || _result is null || string.IsNullOrWhiteSpace(Reason))
        {
            Error = translator["Reconcile_ReasonRequired"];
            return;
        }

        if (!await Shell.Current.DisplayAlertAsync(translator["Reconcile_Adjust"], translator["Reconcile_AdjustMessage"], translator["Reconcile_Adjust"], translator["Common_Cancel"]))
        {
            return;
        }

        if (Reconciliation.CreateAdjustment(_account, _result, ReconcileDate, Reason) is { } adjustment)
        {
            var saved = await store.SaveEntryAsync(adjustment);
            if (!saved.Succeeded)
            {
                Error = string.Join(Environment.NewLine, saved.Errors.Select(e => translator[$"LedgerError_{e}"]));
                return;
            }
        }

        ObservedText = string.Empty;
        Reason = string.Empty;
        ResultText = translator["Reconcile_Done"];
        HintText = null;
        CanAdjust = false;
        await LoadAsync();
    }

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.AccountEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    [RelayCommand]
    private Task ShowEntriesAsync() => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["account"] = _id.ToString() });
}
