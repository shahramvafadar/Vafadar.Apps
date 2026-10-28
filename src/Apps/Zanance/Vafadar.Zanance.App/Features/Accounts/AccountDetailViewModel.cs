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
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>
/// Account details and manual reconciliation (UI-06, ACC-08): the recorded balance, what moved it this month, and a
/// comparison with the balance the user observes – with suggestions before an adjustment is recorded.
/// </summary>
public sealed partial class AccountDetailViewModel(
    ZananceStore store,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    TimeProvider time, Data.Commerce.LoanWorkStore loanReader, LoanInstallmentSession installment, AccountConversionSession conversion, Interaction.IAppInteraction interaction) : ViewModelBase, Presentation.IThemeAware
{
    private Guid _id;
    private bool _pageActive;
    private long _readRevision;
    private CancellationTokenSource _readLifetime = new();
    private AccountDetailSnapshot? _displayed;
    private string? _originalFile;
    private Data.Commerce.LoanReadReview? _loanReview;
    /// <summary>Gets covered complete publication; failed reads retain drafts behind explicit Retry.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets the original confirmed installment lifetime and accepted outcome.</summary>
    public LoanInstallmentSession Installment { get; } = installment;
    /// <summary>Owns original account-to-holding consent, accepted outcome and native follow-through.</summary>
    public AccountConversionSession Conversion { get; } = conversion;
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

    public SnapshotCollection<AmountLine> Movement { get; } = [];

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

    // Native semantic colors remain in the platform partial; all original financial policy is linked unchanged.
    partial void ApplyBalanceColor(bool negative);

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            if (_id != id) { RetireRead(); InvalidateReconciliation(); ObservedText = string.Empty; Reason = string.Empty; Name = null; }
            _id = id; Conversion.SetOwner(id, _pageActive); Installment.SetOwner(id, _pageActive);
        }
    }

    // Loading keeps the reconciliation being typed: the observed balance, the reason and a chosen date stay.
    async Task Presentation.IThemeAware.RefreshThemeAsync() { RetireRead(); await LoadAsync(); }

    public async Task LoadAsync()
    {
        if (!_pageActive || _id == Guid.Empty) return;
        var loading = Loading; var revision = _readRevision; var token = _readLifetime.Token; var id = _id;
        var display = DisplayIdentity(); var day = Today; Prepared? prepared = null;
        bool OwnsPage() => _pageActive && id == _id && revision == _readRevision && ReferenceEquals(loading, Loading);
        void RequireCurrent()
        {
            token.ThrowIfCancellationRequested();
            if (!OwnsPage() || display != DisplayIdentity() || day != Today)
                throw new OperationCanceledException("The original account detail read retired.", token);
        }
        try
        {
            await loading.RunAsync(async () =>
            {
                for (var attempt = 0; ; attempt++)
                {
                    RequireCurrent();
                    var snapshot = await loanReader.ReadAccountDetailAsync(token);
                    RequireCurrent(); _originalFile ??= snapshot.DatabasePath;
                    if (_originalFile != snapshot.DatabasePath) throw new InvalidOperationException("The original account detail cannot follow another profile.");
                    try
                    {
                        var result = await Task.Run(() => Prepare(snapshot, id, token), token);
                        await loanReader.ValidateAccountDetailAsync(snapshot, token); RequireCurrent();
                        snapshot.EnsureCurrent(); prepared = result; return;
                    }
                    catch (AccountDetailSupersededException) when (attempt < 2) { RequireCurrent(); }
                }
            }, () =>
            {
                RequireCurrent(); var result = prepared ?? throw new InvalidOperationException("Complete original account detail is missing.");
                void Publish(Action change)
                {
                    RequireCurrent(); result.Source.EnsureCurrent(); change();
                    RequireCurrent(); result.Source.EnsureCurrent();
                }
                Publish(() => _account = result.Account);
                Publish(() => Name = result.Name);
                Publish(() => UnitNote = result.UnitNote);
                Publish(() => TypeName = result.TypeName);
                Publish(() => DebtActionText = result.DebtActionText);
                Publish(() => CounterpartyText = result.CounterpartyText);
                Publish(() => IncompleteText = result.IncompleteText);
                Publish(() => ReconciledText = result.ReconciledText);
                Publish(() => BalanceText = result.BalanceText);
                Publish(() => EarmarkText = result.EarmarkText);
                Publish(() => ConfirmedText = result.ConfirmedText);
                Publish(() => MonthText = result.MonthText);
                Publish(() => LoanNextText = result.LoanNextText);
                Publish(() => LoanPayoffText = result.LoanPayoffText);
                Publish(() => LoanWorkFeedback = result.LoanWorkFeedback);
                Publish(() => LoanEssentialWarning = result.LoanEssentialWarning);
                Publish(() => ManualLoanActionText = result.ManualLoanActionText);
                Publish(() => IsArchived = result.IsArchived);
                Publish(() => IsDebt = result.IsDebt);
                Publish(() => CanConvert = result.CanConvert);
                Publish(() => MissingLoanTerms = result.MissingLoanTerms);
                Publish(() => HasLoanTerms = result.HasLoanTerms);
                Publish(() => ShowDebtAction = result.ShowDebtAction);
                Publish(() => ShowLoanEstimate = result.ShowLoanEstimate);
                Publish(() => Icon = result.Icon);
                Publish(() => ApplyBalanceColor(result.IsNegative));
                Publish(() => Movement.ReplaceAll(result.Movement));
                Publish(RefreshObservedMoney);
                if (ReconcileDate == default) Publish(() => ReconcileDate = result.Source.Loan.Day);
                _displayed = result.Source; _loanReview = result.Source.Loan;
            });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsPage()) LoanWorkFeedback = CommercialFailureMessage.OrFallback(exception, translator, "Account_ReadFailed"); }
    }

    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    /// <summary>Retires pending reads and native targets without changing the observed-balance or reason draft.</summary>
    private void RetireRead()
    {
        _readRevision++; _displayed = null; _loanReview = null;
        var old = _readLifetime; _readLifetime = new(); Loading = new(); old.Cancel(); old.Dispose();
        InvalidateReconciliation();
    }

    /// <summary>Explicitly refreshes the same original account without resetting reconciliation input.</summary>
    [RelayCommand]
    private Task RefreshLoanAsync() => LoadAsync();

    private Prepared Prepare(AccountDetailSnapshot snapshot, Guid id, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var reviewed = snapshot.Loan;
        var account = reviewed.Accounts.FirstOrDefault(item => item.Id == id)
            ?? throw new InvalidOperationException("The original account is no longer available.");
        var prepared = new Prepared(snapshot, account, translator, dates);
        var culture = localization.CurrentCulture;
        var settings = reviewed.Settings;
        var entries = reviewed.Entries;
        var today = reviewed.Day;

        prepared.Name = account.Name;
        prepared.UnitNote = DisplayUnitNote.For(translator, account.CurrencyCode);
        prepared.TypeName = translator[$"AccountType_{account.Type}"];
        prepared.Icon = Icons.Parse(account.Icon, Icons.For(account.Type));
        prepared.IsArchived = account.IsArchived;
        prepared.DebtActionText = account.IsArchived ? null : account.Type switch
        {
            AccountType.Loan => translator["Account_RecordRepayment"],
            AccountType.Lent => translator["Account_RecordReturn"],
            _ => null,
        };
        prepared.CounterpartyText = account.Counterparty is { } counterparty ? translator.Format(account.Type == AccountType.Loan ? "Account_LentBy" : "Account_BorrowedBy", counterparty) : null;
        prepared.IncompleteText = account.OpeningBalanceKnown ? null : translator["Account_IncompleteHint"];
        prepared.IsDebt = account.Type.IsDebt() && !account.IsArchived;
        prepared.ReconciledText = account.LastReconciledOn is { } reconciled
            ? translator.Format("Account_LastReconciled", dates.Format(reconciled, DateFormatStyle.Short))
            : translator["Account_NeverReconciled"];
        prepared.CanConvert = account.Type == AccountType.Asset && !account.IsArchived && settings.Shows(Feature.AccountConversion);

        // Posted balance and, when unreviewed entries exist, the confirmed-only balance next to it (FIN-12).
        var balance = LedgerCalculator.Balance(account, entries, today);
        var confirmed = LedgerCalculator.Balance(account, entries, today, confirmedOnly: true);
        prepared.BalanceText = MoneyText.Format(balance, account.CurrencyCode, culture);
        prepared.IsNegative = balance < 0;
        prepared.LoadLoanEstimate(account, culture);

        // Money set aside for goals in this account and what is still free (F2-GOAL-05).
        var earmark = Core.Goals.GoalCalculator.Accounts(snapshot.Inputs.Goals, snapshot.Inputs.Allocations, new Dictionary<Guid, long> { [account.Id] = balance })
            .FirstOrDefault(e => e.AccountId == account.Id);
        prepared.EarmarkText = earmark is null ? null
            : translator.Format("Account_Earmarked", MoneyText.Format(earmark.Earmarked, account.CurrencyCode, culture), MoneyText.Format(earmark.Unallocated, account.CurrencyCode, culture))
              + (earmark.Shortfall > 0 ? " · " + translator.Format("Goals_Shortfall", MoneyText.Format(earmark.Shortfall, account.CurrencyCode, culture)) : string.Empty);
        prepared.ConfirmedText = confirmed != balance
            ? translator.Format("Account_ConfirmedOnly", MoneyText.Format(confirmed, account.CurrencyCode, culture))
            : null;

        var calendar = Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
        var startDay = settings.MonthStartDay;
        var (year, month) = PeriodMath.MonthOf(today, calendar, startDay);
        var (from, to) = PeriodMath.MonthRange(year, month, calendar, startDay);
        prepared.MonthText = startDay > 1
            ? $"{dates.Format(from, DateFormatStyle.Short)} – {dates.Format(to, DateFormatStyle.Short)}"
            : dates.Format(from, DateFormatStyle.MonthYear);
        var movement = ReportCalculator.AccountMovements([account], entries, from, to).Single();
        string Format(long value, bool plus = false) => MoneyText.Format(value, account.CurrencyCode, culture, showPlus: plus);
        prepared.Movement.Clear();

        // A balance below zero is a problem or a debt (red, D-27); the movements in between keep their signs only.
        prepared.Movement.Add(new AmountLine(translator["Report_Opening"], Format(movement.Opening), true, movement.Opening < 0));
        void Add(string key, long value, bool negative = false)
        {
            if (value != 0)
            {
                prepared.Movement.Add(new AmountLine(translator[key], Format(negative ? -value : value, plus: true), false));
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
        prepared.Movement.Add(new AmountLine(translator["Report_Closing"], Format(movement.Closing), true, movement.Closing < 0));
        token.ThrowIfCancellationRequested(); return prepared;
    }

    /// <summary>Original figures privately staged on the worker before any native property/row publication.</summary>
    private sealed class Prepared(AccountDetailSnapshot source, Account account, Translator translator, IDateFormatter dates)
    {
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal AccountDetailSnapshot Source { get; } = source;
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal Account Account { get; } = account;
        private readonly LoanReadReview _loanReview = source.Loan;
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal List<AmountLine> Movement { get; } = [];
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? Name { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? UnitNote { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? TypeName { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? DebtActionText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? CounterpartyText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? IncompleteText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? ReconciledText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? BalanceText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? EarmarkText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? ConfirmedText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? MonthText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? LoanNextText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? LoanPayoffText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? LoanWorkFeedback { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? LoanEssentialWarning { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal string? ManualLoanActionText { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool IsArchived { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool IsDebt { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool CanConvert { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool MissingLoanTerms { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool HasLoanTerms { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool ShowDebtAction { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool ShowLoanEstimate { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal Symbol Icon { get; set; }
        /// <summary>Retains a fully staged original value until current-owner publication.</summary>
        internal bool IsNegative { get; set; }

        /// <summary>Runs the original permission, warning and schedule formulas only on the unpublished worker projection.</summary>
        internal void LoadLoanEstimate(Account account, System.Globalization.CultureInfo culture)
        {
            var reviewed = _loanReview ?? throw new InvalidOperationException("Missing original loan review.");
            var outstanding = reviewed.Outstanding(account);
            // D-319: a repaid balance stops estimates; it does not erase the terms already stored on the account.
            MissingLoanTerms = IsDebt && account.Installment is not > 0;
            HasLoanTerms = IsDebt && account.Installment is > 0 && outstanding > 0;
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
    }

    // D-171: the original calculated payment is revalidated by the same writer that commits its whole batch.
    [RelayCommand]
    private async Task RecordInstallmentAsync()
    {
        if (IsBusy || !_pageActive || !Loading.IsReady || !HasLoanTerms || _loanReview is not { } reviewed) return;
        var id = _id;
        bool Current() => _pageActive && _id == id && ReferenceEquals(_loanReview, reviewed) && Loading.IsReady;
        IsBusy = true;
        try { await Installment.RecordAsync(reviewed, id, Current); if (Current()) await LoadAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task ShowScheduleAsync() => HasLoanTerms && _loanReview is { } reviewed
        ? NavigateCurrentAsync(AppRoutes.LoanScheduleRoute, new Dictionary<string, object> { ["id"] = _id, ["loanReview"] = reviewed },
            () => reviewed.Demand(Core.Commerce.CommercialFeature.LoanAnalysis)) : Task.CompletedTask;

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
        var changed = _pageActive != active;
        _pageActive = active; Conversion.SetOwner(_id, active); Installment.SetOwner(_id, active);
        if (changed && !active) RetireRead();
        localization.Changed -= OnObservedDisplayChanged;
        if (active) localization.Changed += OnObservedDisplayChanged;
        else InvalidateReconciliation();
        RefreshObservedMoney();
    }
    private void OnObservedDisplayChanged(object? sender, EventArgs args)
    {
        RefreshObservedMoney(); Conversion.RefreshFeedback();
        RetireRead(); if (_pageActive) _ = LoadAsync();
    }
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
        if (IsBusy || !_pageActive || !Loading.IsReady || _account is not { } account || _displayed is not { } snapshot) return;
        var current = CurrentPublication(snapshot);
        Error = null; InvalidateReconciliation();
        if (_reconciliation.ObservedMoney.NeedsReview(account.CurrencyCode))
        { Error = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(account.CurrencyCode)); return; }
        IsBusy = true;
        try
        {
            var review = await _reconciliation.CompareObservedAsync(account.Id, account.CurrencyCode, ReconcileDate, current, snapshot.DatabasePath);
            if (review is null || !current()) return;
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
            if (!current()) return;
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
        if (IsBusy || !_pageActive || !Loading.IsReady || _displayed is not { } snapshot) return;
        var current = CurrentPublication(snapshot);
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
            if (!current()) return;
            if (saved is null) { CanAdjust = _reconciliation.Review is not null; return; }
            if (!saved.Succeeded)
            { Error = string.Join(Environment.NewLine, saved.Errors.Select(e => translator[$"LedgerError_{e}"])); return; }
            await PublishReconciliationAsync(current);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!current()) return;
            if (_reconciliation.Committed) await PublishReconciliationAsync(current);
            else { InvalidateReconciliation(); Error = CommercialFailureMessage.OrFallback(exception, translator, "Reconcile_ReviewAgain"); }
        }
        finally { IsBusy = false; }
    }

    /// <summary>Publishes the accepted outcome even if a subscriber failed; never repeats the financial operation.</summary>
    private async Task PublishReconciliationAsync(Func<bool> current)
    {
        if (!current()) return; ObservedText = string.Empty;
        if (!current()) return; Reason = string.Empty;
        if (!current()) return; CanAdjust = false;
        if (!current()) return; HintText = null;
        if (!current()) return; ResultText = translator["Reconcile_Done"];
        if (current()) await LoadAsync();
    }

    [RelayCommand]
    private Task DebtActionAsync()
    {
        if (_account is not { } account || !account.Type.IsDebt() || account.IsArchived) return Task.CompletedTask;
        var query = new Dictionary<string, object> { ["kind"] = nameof(EntryKind.Transfer) };
        query[account.Type == AccountType.Loan ? "to" : "from"] = account.Id;
        return NavigateCurrentAsync(AppRoutes.EntryEditorRoute, query);
    }

    /// <summary>Opens only the original current debt's unsaved reminder; it never posts the analytical estimate.</summary>
    [RelayCommand]
    private Task RepaymentReminderAsync() => _account is { } account && account.Type.IsDebt() && !account.IsArchived
        ? NavigateCurrentAsync(AppRoutes.PlanEditorRoute, new Dictionary<string, object> { ["fromDebt"] = account.Id })
        : Task.CompletedTask;

    private Func<bool> CurrentPublication(AccountDetailSnapshot snapshot)
    {
        var loading = Loading; var revision = _readRevision; var id = _id; var display = DisplayIdentity();
        return () => snapshot.Loan.Day == Today && _pageActive && id == _id && revision == _readRevision && ReferenceEquals(loading, Loading)
            && loading.IsReady && ReferenceEquals(snapshot, _displayed) && display == DisplayIdentity();
    }

    private async Task NavigateCurrentAsync(string route, IDictionary<string, object>? parameters, Action? demand = null)
    {
        if (IsBusy || !_pageActive || !Loading.IsReady || _displayed is not { } snapshot) return;
        var current = CurrentPublication(snapshot); var loading = Loading; var token = _readLifetime.Token;
        IsBusy = true;
        try
        {
            await loanReader.ValidateAccountDetailAsync(snapshot, token);
            if (!current()) return;
            try { demand?.Invoke(); }
            catch (CommercialWriteRejectedException rejected)
            {
                // Paid analysis refusal must not hide the original free balance, essential warning or repayment path.
                if (current()) LoanWorkFeedback = CommercialFailureMessage.OrFallback(rejected, translator, "Loan_ReadFailed");
                return;
            }
            if (current()) await interaction.NavigateAsync(route, parameters);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!current()) return;
            LoanWorkFeedback = CommercialFailureMessage.OrFallback(exception, translator, "Account_ReadFailed");
            if (!current()) return;
            try { await loading.RunAsync(() => Task.FromException(exception), () => { }); }
            catch (Exception original) when (ReferenceEquals(original, exception)) { }
            if (ReferenceEquals(loading, Loading)) { _displayed = null; _loanReview = null; }
        }
        finally { IsBusy = false; }
    }

    // D-628: only the original active page may confirm its own file-bound conversion; acceptance survives follow-up failure.
    [RelayCommand]
    private async Task ConvertToHoldingAsync()
    {
        if (IsBusy || !Loading.IsReady || !_pageActive || _account is not { } account || !CanConvert) return;
        var id = _id;
        bool Current() => _pageActive && _id == id && ReferenceEquals(_account, account) && Loading.IsReady && CanConvert;
        IsBusy = true;
        try
        {
            await Conversion.ConvertAsync(id, Current);
            if (Conversion.AcceptedOutcome?.AccountId == id && _pageActive && _id == id)
            {
                // The old page remains only when follow-through failed; reread it without another conversion.
                CanConvert = false; IsArchived = true; await LoadAsync();
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task EditAsync() => NavigateCurrentAsync(AppRoutes.AccountEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    // A new expense on this account: the account page is the context account of the entry (ZEX-S0103, S0202).
    [RelayCommand]
    private Task AddEntryHereAsync() => IsArchived ? Task.CompletedTask : NavigateCurrentAsync(AppRoutes.EntryEditorRoute, new Dictionary<string, object>
    {
        ["kind"] = nameof(EntryKind.Expense),
        ["account"] = _id,
    });

    [RelayCommand]
    private Task ShowEntriesAsync() => NavigateCurrentAsync("//transactions", new Dictionary<string, object> { ["account"] = _id.ToString() });
}
