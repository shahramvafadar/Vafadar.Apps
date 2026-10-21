using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>
/// Account details and manual reconciliation (UI-06, ACC-08): the recorded balance, what moved it this month, and a
/// comparison with the balance the user observes – with suggestions before an adjustment is recorded.
/// </summary>
public sealed partial class AccountDetailViewModel(
    ZananceStore store,
    GoalStore goals,
    HoldingStore holdings,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    TimeProvider time, Data.Commerce.LoanWorkStore loanReader, LoanInstallmentSession installment, Interaction.IAppInteraction interaction) : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private Guid _id;
    private Data.Commerce.LoanReadReview? _loanReview;
    /// <summary>Gets covered complete publication; failed reads retain drafts behind explicit Retry.</summary>
    public SnapshotLoadState Loading { get; } = new();
    /// <summary>Gets the original confirmed installment lifetime and accepted outcome.</summary>
    public LoanInstallmentSession Installment { get; } = installment;
    /// <summary>Gets translated current loan access/read feedback.</summary>
    [ObservableProperty] public partial string? LoanWorkFeedback { get; private set; }
    /// <summary>Gets whether original paid estimates and installment tools can be shown.</summary>
    [ObservableProperty] public partial bool ShowLoanEstimate { get; private set; }
    /// <summary>Gets whether the debt actually lacks installment terms, independently of its remaining balance.</summary>
    [ObservableProperty] public partial bool MissingLoanTerms { get; private set; }
    /// <summary>Gets an essential original warning independently of analytical access.</summary>
    [ObservableProperty] public partial string? LoanEssentialWarning { get; private set; }
    /// <summary>Gets the original manual repayment caption without claiming an unavailable calculated installment.</summary>
    [ObservableProperty] public partial string? ManualLoanActionText { get; private set; }

    private Account? _account;
    private readonly AccountReconciliationSession _reconciliation = new(store);

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
    public partial DateOnly ReconcileDate { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

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

    /// <summary>Gets the day the balance was last compared with the bank, or "never" (ZEX-S0608).</summary>
    [ObservableProperty]
    public partial string? ReconciledText { get; set; }

    /// <summary>Gets a value indicating whether the account is an active valued asset that can become a holding (Advanced).</summary>
    [ObservableProperty]
    public partial bool CanConvert { get; set; }

    /// <summary>Gets the colour of the balance: red when it is negative, e.g. a loan (D-27).</summary>
    [ObservableProperty]
    public partial Color? BalanceColor { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            if (_id != id) { InvalidateReconciliation(); ObservedText = string.Empty; Reason = string.Empty; }
            _id = id;
        }
    }

    // Loading keeps the reconciliation being typed: the observed balance, the reason and a chosen date stay.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        try { await Loading.RunAsync(LoadCoreAsync, () => { }); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { LoanWorkFeedback = CommercialFailureMessage.OrFallback(exception, translator, "Account_ReadFailed"); }
    }

    /// <summary>Explicitly refreshes the same original account without resetting reconciliation input.</summary>
    [RelayCommand]
    private Task RefreshLoanAsync() => LoadAsync();

    private async Task LoadCoreAsync()
    {
        var reviewed = await loanReader.ReadAsync();
        if (_loanReview is not null) await loanReader.ValidateFileAsync(_loanReview);
        _loanReview = reviewed;
        _account = reviewed.Accounts.FirstOrDefault(a => a.Id == _id);
        if (_account is not { } account)
        {
            throw new InvalidOperationException("The original account is no longer available.");
        }

        var culture = localization.CurrentCulture;
        var settings = reviewed.Settings;
        var entries = reviewed.Entries;
        var today = reviewed.Day;
        if (ReconcileDate == default)
        {
            ReconcileDate = today;
        }

        Name = account.Name;
        UnitNote = DisplayUnitNote.For(translator, account.CurrencyCode);
        RefreshObservedMoney();
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
        ReconciledText = account.LastReconciledOn is { } reconciled
            ? translator.Format("Account_LastReconciled", dates.Format(reconciled, DateFormatStyle.Short))
            : translator["Account_NeverReconciled"];
        CanConvert = account.Type == AccountType.Asset && !account.IsArchived && settings.Shows(Feature.AccountConversion);

        // Posted balance and, when unreviewed entries exist, the confirmed-only balance next to it (FIN-12).
        var balance = LedgerCalculator.Balance(account, entries, today);
        var confirmed = LedgerCalculator.Balance(account, entries, today, confirmedOnly: true);
        BalanceText = MoneyText.Format(balance, account.CurrencyCode, culture);
        BalanceColor = balance < 0 ? EntryPresenter.DangerColor : Palette.AmountText;
        LoadLoanEstimate(account, culture);

        // Money set aside for goals in this account and what is still free (F2-GOAL-05).
        var earmark = Core.Goals.GoalCalculator.Accounts(await goals.GetGoalsAsync(), await goals.GetAllocationsAsync(), new Dictionary<Guid, long> { [account.Id] = balance })
            .FirstOrDefault(e => e.AccountId == account.Id);
        EarmarkText = earmark is null ? null
            : translator.Format("Account_Earmarked", MoneyText.Format(earmark.Earmarked, account.CurrencyCode, culture), MoneyText.Format(earmark.Unallocated, account.CurrencyCode, culture))
              + (earmark.Shortfall > 0 ? " · " + translator.Format("Goals_Shortfall", MoneyText.Format(earmark.Shortfall, account.CurrencyCode, culture)) : string.Empty);
        ConfirmedText = confirmed != balance
            ? translator.Format("Account_ConfirmedOnly", MoneyText.Format(confirmed, account.CurrencyCode, culture))
            : null;

        var calendar = Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
        var startDay = settings.MonthStartDay;
        var (year, month) = PeriodMath.MonthOf(today, calendar, startDay);
        var (from, to) = PeriodMath.MonthRange(year, month, calendar, startDay);
        MonthText = startDay > 1
            ? $"{dates.Format(from, DateFormatStyle.Short)} – {dates.Format(to, DateFormatStyle.Short)}"
            : dates.Format(from, DateFormatStyle.MonthYear);
        var movement = ReportCalculator.AccountMovements([account], entries, from, to).Single();
        string Format(long value, bool plus = false) => MoneyText.Format(value, account.CurrencyCode, culture, showPlus: plus);
        Movement.Clear();

        // A balance below zero is a problem or a debt (red, D-27); the movements in between keep their signs only.
        Movement.Add(new AmountLine(translator["Report_Opening"], Format(movement.Opening), true, movement.Opening < 0));
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
        Movement.Add(new AmountLine(translator["Report_Closing"], Format(movement.Closing), true, movement.Closing < 0));
        await loanReader.ValidateAsync(reviewed);
    }

    private void LoadLoanEstimate(Account account, System.Globalization.CultureInfo culture)
    {
        var reviewed = _loanReview ?? throw new InvalidOperationException("Missing original loan review.");
        _outstanding = reviewed.Outstanding(account);
        // D-319: a repaid balance stops estimates; it does not erase the terms already stored on the account.
        MissingLoanTerms = IsDebt && account.Installment is not > 0;
        HasLoanTerms = IsDebt && account.Installment is > 0 && _outstanding > 0;
        ShowDebtAction = DebtActionText is not null && !HasLoanTerms;
        ShowLoanEstimate = false; LoanNextText = LoanPayoffText = LoanWorkFeedback = null;
        LoanEssentialWarning = reviewed.HasUnderpaymentWarning(account) ? translator["Loan_NeverPaidOff"] : null;
        ManualLoanActionText = DebtActionText;
        if (!HasLoanTerms) return;
        if (reviewed.Permission(Core.Commerce.CommercialFeature.LoanAnalysis) != Core.Commerce.FeaturePermission.Allowed)
        {
            LoanWorkFeedback = CommercialFailureMessage.Describe(new Data.Commerce.CommercialWriteRejectedException(
                Core.Commerce.CommercialFeature.LoanAnalysis, reviewed.Permission(Core.Commerce.CommercialFeature.LoanAnalysis)), translator);
            return;
        }
        var (interest, principal) = reviewed.Split(account);
        string Money(long value) => MoneyText.Format(value, account.CurrencyCode, culture);
        LoanNextText = translator.Format("Loan_Next", Money(interest + principal), Money(principal), Money(interest));
        var schedule = reviewed.Schedule(account);
        LoanPayoffText = schedule.LastDate is { } last
            ? translator.Format("Loan_Payoff", schedule.Installments.Count, dates.Format(last, DateFormatStyle.MonthYear), Money(schedule.TotalInterest))
            : translator["Loan_NeverPaidOff"];
        ShowLoanEstimate = true; ManualLoanActionText = translator["Loan_OtherAmount"];
    }

    // D-171: the original calculated payment is revalidated by the same writer that commits its whole batch.
    [RelayCommand]
    private async Task RecordInstallmentAsync()
    {
        if (IsBusy || !Loading.IsReady || _loanReview is not { } reviewed) return;
        IsBusy = true;
        try { await Installment.RecordAsync(reviewed, _id); await LoadAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ShowScheduleAsync()
    {
        if (IsBusy || !Loading.IsReady || _loanReview is not { } reviewed) return;
        IsBusy = true;
        try
        {
            await loanReader.ValidateAsync(reviewed); reviewed.Demand(Core.Commerce.CommercialFeature.LoanAnalysis);
            await interaction.NavigateAsync(AppShell.LoanScheduleRoute, new Dictionary<string, object> { ["id"] = _id, ["loanReview"] = reviewed });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { LoanWorkFeedback = CommercialFailureMessage.OrFallback(exception, translator, "Loan_ReadFailed"); }
        finally { IsBusy = false; }
    }

    /// <summary>Retires a previous comparison immediately when its original amount or date changes.</summary>
    partial void OnObservedTextChanged(string value)
    {
        _reconciliation.ObservedMoney.ChangeText(value, _account?.CurrencyCode ?? Currencies.Euro.Code, localization.CurrentCulture);
        InvalidateReconciliation(); RefreshObservedMoney();
    }
    /// <summary>Gets the observed balance's original unit until explicit complete reentry.</summary>
    public string ObservedUnitName => _reconciliation.ObservedMoney.UnitName(_account?.CurrencyCode ?? Currencies.Euro.Code);
    /// <summary>Explains the retained original observed-balance parser after a display change.</summary>
    public string? ObservedFormatNote => _reconciliation.ObservedMoney.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;
    /// <summary>Retains raw observation while retiring pending native consent when this page leaves.</summary>
    public void SetReconciliationActive(bool active)
    {
        localization.Changed -= OnObservedDisplayChanged;
        if (active) localization.Changed += OnObservedDisplayChanged;
        else InvalidateReconciliation();
        RefreshObservedMoney();
    }
    private void OnObservedDisplayChanged(object? sender, EventArgs args) => RefreshObservedMoney();
    private void RefreshObservedMoney()
    {
        UnitNote = DisplayUnitNote.ForInput(translator, _reconciliation.ObservedMoney, _account?.CurrencyCode ?? Currencies.Euro.Code);
        OnPropertyChanged(nameof(ObservedUnitName)); OnPropertyChanged(nameof(ObservedFormatNote));
    }
    partial void OnReconcileDateChanged(DateOnly value) => InvalidateReconciliation();

    private void InvalidateReconciliation()
    {
        _reconciliation.Invalidate(); CanAdjust = false; ResultText = null; HintText = null;
    }

    [RelayCommand]
    private async Task CompareAsync()
    {
        if (IsBusy || !Loading.IsReady || _account is not { } account) return;
        Error = null; InvalidateReconciliation();
        if (_reconciliation.ObservedMoney.NeedsReview(account.CurrencyCode))
        { Error = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(account.CurrencyCode)); return; }
        IsBusy = true;
        try
        {
            var review = await _reconciliation.CompareObservedAsync(_id, account.CurrencyCode, ReconcileDate);
            if (review is null) return;
            var result = review.Result; var culture = localization.CurrentCulture;
            ResultText = result.Difference == 0 ? translator["Reconcile_Match"]
                : translator.Format("Reconcile_Difference", MoneyText.Format(result.Recorded, review.CurrencyCode, culture),
                    MoneyText.Format(result.Difference, review.CurrencyCode, culture, showPlus: true));
            var hints = new List<string>();
            if (result.UnreviewedCount > 0) hints.Add(translator.Format("Reconcile_Unreviewed", result.UnreviewedCount));
            if (result.PossibleDuplicates > 0) hints.Add(translator.Format("Reconcile_Duplicates", result.PossibleDuplicates));
            if (result.Difference != 0) hints.Add(translator["Reconcile_Missing"]);
            HintText = hints.Count > 0 ? string.Join(Environment.NewLine, hints) : null;
            CanAdjust = result.Difference != 0;
            if (result.Difference == 0)
            {
                IncompleteText = null;
                var date = account.LastReconciledOn is { } previous && previous > review.Date ? previous : review.Date;
                ReconciledText = translator.Format("Account_LastReconciled", dates.Format(date, DateFormatStyle.Short));
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            InvalidateReconciliation();
            Error = _reconciliation.Committed ? translator["Reconcile_Done"]
                : exception is FormatException ? translator["Amount_Invalid"]
                : CommercialFailureMessage.OrFallback(exception, translator, "Reconcile_ReviewAgain");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AdjustAsync()
    {
        if (IsBusy || !Loading.IsReady) return;
        Error = null;
        if (_account is { } original && _reconciliation.ObservedMoney.NeedsReview(original.CurrencyCode))
        {
            InvalidateReconciliation(); Error = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(original.CurrencyCode)); return;
        }
        if (_reconciliation.Review is null) { Error = translator["Reconcile_ReviewAgain"]; return; }
        if (string.IsNullOrWhiteSpace(Reason)) { Error = translator["Reconcile_ReasonRequired"]; return; }
        IsBusy = true;
        try
        {
            var saved = await _reconciliation.AdjustAsync(() => interaction.ConfirmAsync(translator["Reconcile_Adjust"],
                translator["Reconcile_AdjustMessage"], translator["Reconcile_Adjust"], translator["Common_Cancel"]), () => Reason);
            if (saved is null) { CanAdjust = _reconciliation.Review is not null; return; }
            if (!saved.Succeeded)
            { Error = string.Join(Environment.NewLine, saved.Errors.Select(e => translator[$"LedgerError_{e}"])); return; }
            await PublishReconciliationAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (_reconciliation.Committed) await PublishReconciliationAsync();
            else { InvalidateReconciliation(); Error = CommercialFailureMessage.OrFallback(exception, translator, "Reconcile_ReviewAgain"); }
        }
        finally { IsBusy = false; }
    }

    /// <summary>Publishes the accepted outcome even if a subscriber failed; never repeats the financial operation.</summary>
    private async Task PublishReconciliationAsync()
    {
        ObservedText = string.Empty; Reason = string.Empty; CanAdjust = false; HintText = null;
        ResultText = translator["Reconcile_Done"]; await LoadAsync();
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

    /// <summary>Opens an unsaved principal-only transfer reminder; the estimate never becomes an automatic payment.</summary>
    [RelayCommand]
    private Task RepaymentReminderAsync() => _account is { } account && account.Type.IsDebt()
        ? Shell.Current.GoToAsync(AppShell.PlanEditorRoute, new Dictionary<string, object> { ["fromDebt"] = account.Id })
        : Task.CompletedTask;

    // ZEX-S0408: a preview first; only a confirmation creates the holding and archives the account (restorable).
    [RelayCommand]
    private async Task ConvertToHoldingAsync()
    {
        if (_account is not { } account || !CanConvert)
        {
            return;
        }

        var balance = LedgerCalculator.Balance(account, await store.GetEntriesAsync(), Today);
        var value = balance > 0 ? MoneyText.Format(balance, account.CurrencyCode, localization.CurrentCulture) : translator["Holding_ValueUnknown"];
        if (!await Shell.Current.DisplayAlertAsync(translator["Account_ConvertToHolding"], translator.Format("Account_ConvertPreview", account.Name, value),
                translator["Account_ConvertConfirm"], translator["Common_Cancel"]))
        {
            return;
        }

        if (await holdings.ConvertAccountAsync(account.Id, balance, Today, translator["Holding_DefaultLocation"]) is { } type)
        {
            await Shell.Current.GoToAsync($"../{AppShell.HoldingDetailRoute}", new Dictionary<string, object> { ["id"] = type.Id });
        }
    }

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.AccountEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    // A new expense on this account: the account page is the context account of the entry (ZEX-S0103, S0202).
    [RelayCommand]
    private Task AddEntryHereAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object>
    {
        ["kind"] = nameof(EntryKind.Expense),
        ["account"] = _id,
    });

    [RelayCommand]
    private Task ShowEntriesAsync() => Shell.Current.GoToAsync("//transactions", new Dictionary<string, object> { ["account"] = _id.ToString() });
}
