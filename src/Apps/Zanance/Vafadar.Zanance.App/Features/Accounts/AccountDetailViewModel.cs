using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>
/// Account details and manual reconciliation (UI-06, ACC-08): the recorded balance, what moved it this month, and a
/// comparison with the balance the user observes – with suggestions before an adjustment is recorded.
/// </summary>
public sealed partial class AccountDetailViewModel(
    ZananceStore store,
    GoalStore goals,
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
    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    [ObservableProperty]
    public partial string? IncompleteText { get; set; }

    [ObservableProperty]
    public partial string? EarmarkText { get; set; }

    // Loans and lent money (F2-DEBT-01): repayments are transfers; the button prepares one.
    [ObservableProperty]
    public partial string? DebtActionText { get; set; }

    [ObservableProperty]
    public partial string? CounterpartyText { get; set; }

    // Repayment estimate from the rate and installment the user entered (F2-DEBT-02).
    [ObservableProperty]
    public partial bool IsDebt { get; set; }

    [ObservableProperty]
    public partial bool HasLoanTerms { get; set; }

    // With known terms the installment is the main action; a repayment of another amount stays available in the card.
    [ObservableProperty]
    public partial bool ShowDebtAction { get; set; }

    [ObservableProperty]
    public partial string? LoanNextText { get; set; }

    [ObservableProperty]
    public partial string? LoanPayoffText { get; set; }

    private long _outstanding;

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

    /// <summary>Gets the colour of the balance: red when it is negative, e.g. a loan (D-27).</summary>
    [ObservableProperty]
    public partial Color? BalanceColor { get; set; }

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
        UnitNote = DisplayUnitNote.For(translator, account.CurrencyCode);
        TypeName = translator[$"AccountType_{account.Type}"];
        Icon = Icons.Parse(account.Icon, Icons.For(account.Type));
        IsArchived = account.IsArchived;
        DebtActionText = account.IsArchived ? null : account.Type switch
        {
            AccountType.Loan => translator["Account_RecordRepayment"],
            AccountType.Lent => translator["Account_RecordReturn"],
            _ => null,
        };
        CounterpartyText = account.Counterparty is { } counterparty ? translator.Format(account.Type == AccountType.Loan ? "Account_LentBy" : "Account_BorrowedBy", counterparty) : null;
        IncompleteText = account.OpeningBalanceKnown ? null : translator["Account_IncompleteHint"];
        IsDebt = account.Type.IsDebt() && !account.IsArchived;

        // Posted balance and, when unreviewed entries exist, the confirmed-only balance next to it (FIN-12).
        var balance = LedgerCalculator.Balance(account, entries, today);
        var confirmed = LedgerCalculator.Balance(account, entries, today, confirmedOnly: true);
        BalanceText = MoneyText.Format(balance, account.CurrencyCode, culture);
        BalanceColor = balance < 0 ? EntryPresenter.DangerColor : Palette.AmountText;
        LoadLoanEstimate(account, balance, today, culture);
        // Money set aside for goals in this account and what is still free (F2-GOAL-05).
        var earmark = Core.Goals.GoalCalculator.Accounts(await goals.GetGoalsAsync(), await goals.GetAllocationsAsync(), new Dictionary<Guid, long> { [account.Id] = balance })
            .FirstOrDefault(e => e.AccountId == account.Id);
        EarmarkText = earmark is null ? null
            : translator.Format("Account_Earmarked", MoneyText.Format(earmark.Earmarked, account.CurrencyCode, culture), MoneyText.Format(earmark.Unallocated, account.CurrencyCode, culture))
              + (earmark.Shortfall > 0 ? " · " + translator.Format("Goals_Shortfall", MoneyText.Format(earmark.Shortfall, account.CurrencyCode, culture)) : string.Empty);
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

    private void LoadLoanEstimate(Account account, long balance, DateOnly today, System.Globalization.CultureInfo culture)
    {
        _outstanding = LoanCalculator.Outstanding(account.Type, balance);
        HasLoanTerms = IsDebt && account.Installment is > 0 && _outstanding > 0;
        ShowDebtAction = DebtActionText is not null && !HasLoanTerms;
        LoanNextText = LoanPayoffText = null;
        if (!HasLoanTerms)
        {
            return;
        }

        string Money(long value) => MoneyText.Format(value, account.CurrencyCode, culture);
        var rate = account.InterestRate ?? 0;
        var (interest, principal) = LoanCalculator.Split(_outstanding, rate, account.Installment!.Value);
        LoanNextText = translator.Format("Loan_Next", Money(interest + principal), Money(principal), Money(interest));
        var schedule = LoanCalculator.Schedule(_outstanding, rate, account.Installment.Value, today.AddMonths(1));
        LoanPayoffText = schedule.LastDate is { } last
            ? translator.Format("Loan_Payoff", schedule.Installments.Count, dates.Format(last, DateFormatStyle.MonthYear), Money(schedule.TotalInterest))
            : translator["Loan_NeverPaidOff"];
    }

    // One installment as a principal transfer and a separate interest entry, after the user chose the account (F2-DEBT-02).
    [RelayCommand]
    private async Task RecordInstallmentAsync()
    {
        if (_account is not { Installment: > 0 } account || _outstanding <= 0)
        {
            return;
        }

        var cash = (await store.GetAccountsAsync())
            .Where(a => !a.IsArchived && !a.Type.IsOutsideCash() && a.CurrencyCode == account.CurrencyCode).ToList();
        if (cash.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync(translator["Loan_RecordInstallment"], translator["Loan_NoCashAccount"], translator["Common_Ok"]);
            return;
        }

        var loan = account.Type == AccountType.Loan;
        var chosen = cash.Count == 1 ? cash[0].Name
            : await Shell.Current.DisplayActionSheetAsync(translator[loan ? "Loan_PayFrom" : "Loan_ReceiveInto"], translator["Common_Cancel"], null, [.. cash.Select(a => a.Name)]);
        if (cash.FirstOrDefault(a => a.Name == chosen) is not { } from)
        {
            return;
        }

        var culture = localization.CurrentCulture;
        string Money(long value) => MoneyText.Format(value, account.CurrencyCode, culture);
        var rate = account.InterestRate ?? 0;
        var (interest, principal) = LoanCalculator.Split(_outstanding, rate, account.Installment.Value);
        var message = translator.Format(loan ? "Loan_ConfirmPay" : "Loan_ConfirmReceive", Money(principal), Money(interest), from.Name);
        if (!await Shell.Current.DisplayAlertAsync(translator["Loan_RecordInstallment"], message, translator["Common_Save"], translator["Common_Cancel"]))
        {
            return;
        }

        var categories = await store.GetCategoriesAsync();
        var category = loan
            ? categories.First(c => c.Kind == CategoryKind.Expense && c.SystemKey == DefaultCategories.Fees)
            : categories.FirstOrDefault(c => c.Kind == CategoryKind.Income && c.SystemKey == "Interest")
              ?? categories.First(c => c.Kind == CategoryKind.Income && c.SystemKey == DefaultCategories.Uncategorized);
        var entries = LoanCalculator.CreateInstallment(account, from.Id, _outstanding, rate, account.Installment.Value, Today, category.Id, translator.Format("Loan_InterestTitle", account.Name));
        var saved = await store.SaveEntriesAsync(entries, []);
        Error = saved.Succeeded ? null : string.Join(Environment.NewLine, saved.Errors.Select(e => translator[$"LedgerError_{e}"]));
        await LoadAsync();
    }

    [RelayCommand]
    private Task ShowScheduleAsync() => Shell.Current.GoToAsync(AppShell.LoanScheduleRoute, new Dictionary<string, object> { ["id"] = _id });

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
        if (!MoneyText.TryParse(text.TrimStart('-', '−'), currency, localization.CurrentCulture, out var observed))
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

        // A matching balance confirms the account, so an unknown opening balance no longer makes it incomplete.
        if (_result.Difference == 0 && !account.OpeningBalanceKnown)
        {
            account.OpeningBalanceKnown = true;
            await store.SaveAccountAsync(account);
            IncompleteText = null;
        }
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

        // A reconciled balance is known from its date on, so the account is no longer incomplete (ACC-09).
        if (!_account.OpeningBalanceKnown)
        {
            _account.OpeningBalanceKnown = true;
            await store.SaveAccountAsync(_account);
        }

        ObservedText = string.Empty;
        Reason = string.Empty;
        ResultText = translator["Reconcile_Done"];
        HintText = null;
        CanAdjust = false;
        await LoadAsync();
    }

    [RelayCommand]
    private Task DebtActionAsync()
    {
        if (_account is null)
        {
            return Task.CompletedTask;
        }

        var query = new Dictionary<string, object> { ["kind"] = nameof(EntryKind.Transfer) };
        query[_account.Type == AccountType.Loan ? "to" : "from"] = _account.Id;
        return Shell.Current.GoToAsync(AppShell.EntryEditorRoute, query);
    }

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.AccountEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    [RelayCommand]
    private Task ShowEntriesAsync() => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["account"] = _id.ToString() });
}
