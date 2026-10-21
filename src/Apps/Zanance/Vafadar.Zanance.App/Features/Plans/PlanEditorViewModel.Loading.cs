using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

public sealed partial class PlanEditorViewModel
{
    private PlanEditorLoadSession? _loadSession;
    private PlanEditorSaveSession? _saveSession;
    private EntryPlanOrigin? _entryOrigin;
    private PlanAccountHandoff? _accountVisit;
    private bool _accountReturnPending;
    private Dictionary<string, object>? _originalQuery;
    private string? _queryIdentity;
    private string? _originalFile;
    private Task? _formRead;
    private long _formEpoch;
    private long _commandRevision;
    private bool _formDeparted;
    private bool _hasPublishedForm;
    private bool _originalSourceChanged;
    private bool _needsDebtPaymentAccount;
    private Action? _publicationGuard;
    private Schedule _draftIdentity = new() { Name = string.Empty };
    private Schedule? _splitDraft;
    /// <summary>Gets covered loading, explicit failed Retry and complete original publication.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets translated original-form feedback without native or financial exception details.</summary>
    [ObservableProperty]
    public partial string? LoadError { get; private set; }
    /// <summary>Gets whether complete original fields may be shown, including a read-only accepted form.</summary>
    public bool FormVisible => Loading.IsReady && !_formDeparted && _hasPublishedForm;
    /// <summary>Gets the separate complete no-account alternative after successful source publication.</summary>
    public bool NeedsAccount => FormVisible && HasNoAccounts;
    /// <summary>Explains the actual missing-account context without presenting an unfinished debt draft as a blank expense.</summary>
    public string AccountEmptyText => _translator[_needsDebtPaymentAccount ? "Loan_NoCashAccount" : "Entry_NoAccounts"];
    /// <summary>Gets the complete form body without exposing default fields during a pending read.</summary>
    public bool HasFormContent => FormVisible && !HasNoAccounts;
    /// <summary>Gets whether an explicit read-only Retry can retain this original raw draft.</summary>
    public bool CanReloadForm => !_formDeparted && !IsBusy && _loadSession is not null && Loading.HasFailed
        && !_originalSourceChanged && _saveSession?.Accepted != true;
    /// <summary>Gets the original view revision for bounded native observations and callbacks.</summary>
    internal long FormEpoch => _formEpoch;
    /// <summary>Gets known original plan acceptance for native qualification.</summary>
    internal bool FormAccepted => _saveSession?.Accepted == true;
    /// <summary>Gets the complete original packet only while its form is published.</summary>
    internal PlanEditorReadSnapshot? FormReview => Loading.IsReady ? _loadSession?.Review : null;
    /// <summary>Gets the actual routed plan/continuation draft identity, never writer authority.</summary>
    internal Guid FormDraftId => _existing is not null && !_hasHistory ? _existing.Id : _splitDraft?.Id ?? _draftIdentity.Id;

    /// <summary>Gets this original form's financial command; a new query receives a distinct command object.</summary>
    public IAsyncRelayCommand SaveCommand { get; private set; } = null!;
    /// <summary>Gets original Close/Cancel with its own confirmation lifetime.</summary>
    public IAsyncRelayCommand CancelCommand { get; private set; } = null!;
    /// <summary>Gets the original explicit nested account visit.</summary>
    public IAsyncRelayCommand AddAccountCommand { get; private set; } = null!;
    /// <summary>Gets read-only original readiness refresh without substituting a new source.</summary>
    public IAsyncRelayCommand ReviewWorkCommand { get; private set; } = null!;
    /// <summary>Gets failed read-only original Retry.</summary>
    public IAsyncRelayCommand ReloadFormCommand { get; private set; } = null!;
    /// <summary>Gets the current original rule disclosure command.</summary>
    public IRelayCommand ToggleRuleOptionsCommand { get; private set; } = null!;
    /// <summary>Gets the current original contract disclosure command.</summary>
    public IRelayCommand ToggleContractCommand { get; private set; } = null!;
    /// <summary>Gets only this original publication's category targets.</summary>
    public IRelayCommand<PlanCategoryChoice> SelectCategoryCommand { get; private set; } = null!;

    private void ResetFormCommands()
    {
        var epoch = _formEpoch; var session = _loadSession; var revision = ++_commandRevision;
        bool Frame() => epoch == _formEpoch && revision == _commandRevision && !_formDeparted && ReferenceEquals(session, _loadSession);
        bool Current() => Frame() && _entryOrigin?.IsOwned != false;
        SaveCommand = new AsyncRelayCommand(() => Current() ? SaveAsync() : Task.CompletedTask, () => Current() && CanSave);
        CancelCommand = new AsyncRelayCommand(() => Frame() && CanLeave ? CancelAsync() : Task.CompletedTask, () => Frame() && CanLeave);
        AddAccountCommand = new AsyncRelayCommand(() => Current() ? AddAccountAsync() : Task.CompletedTask,
            () => Current() && CanInteract && HasNoAccounts && !_reviewingWork && !_workFailed);
        ReviewWorkCommand = new AsyncRelayCommand(() => Current() && CanReviewWork ? ReviewWorkAsync() : Task.CompletedTask, () => Current() && CanReviewWork);
        ReloadFormCommand = new AsyncRelayCommand(() => Frame() && CanReloadForm ? ReloadOriginalFormAsync() : Task.CompletedTask, () => Frame() && CanReloadForm);
        ToggleRuleOptionsCommand = new RelayCommand(() => { if (Current() && CanInteract) ToggleRuleOptions(); }, () => Current() && CanInteract);
        ToggleContractCommand = new RelayCommand(() => { if (Current() && CanInteract) ToggleContract(); }, () => Current() && CanInteract);
        SelectCategoryCommand = new RelayCommand<PlanCategoryChoice>(choice =>
        { if (Current() && CanInteract && choice is not null && Categories.Any(c => ReferenceEquals(c, choice))) SelectCategory(choice); }, _ => Current() && CanInteract);
        foreach (var name in new[] { nameof(SaveCommand), nameof(CancelCommand), nameof(AddAccountCommand), nameof(ReviewWorkCommand),
            nameof(ReloadFormCommand), nameof(ToggleRuleOptionsCommand), nameof(ToggleContractCommand), nameof(SelectCategoryCommand) }) OnPropertyChanged(name);
    }
    partial void OnLoadingChanged(SnapshotLoadState value)
    {
        value.PropertyChanged += (_, _) => { if (ReferenceEquals(value, Loading)) PublishFormAvailability(); };
        PublishFormAvailability();
    }
    private void PublishFormAvailability()
    {
        OnPropertyChanged(nameof(FormVisible)); OnPropertyChanged(nameof(NeedsAccount)); OnPropertyChanged(nameof(HasFormContent));
        OnPropertyChanged(nameof(AccountEmptyText));
        OnPropertyChanged(nameof(CanReloadForm)); ReloadFormCommand.NotifyCanExecuteChanged(); RefreshAccess();
    }
    private bool AcceptFile(string path)
    {
        var old = Interlocked.CompareExchange(ref _originalFile, path, null); return old is null || old == path;
    }
    private static Guid? OriginalId(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value)) return null;
        if (Guid.TryParse(value?.ToString(), out var id) && id != Guid.Empty) return id;
        throw new ArgumentException("The original plan route identity is invalid.");
    }
    private async Task ApplyOriginalQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Count == 0 && _originalQuery is not null) return;
        var captured = new Dictionary<string, object>(query);
        query.TryGetValue("entryOrigin", out var origin);
        var identity = JsonSerializer.Serialize(captured.Where(p => p.Key != "entryOrigin").OrderBy(p => p.Key).ToDictionary());
        if (identity == _queryIdentity && ReferenceEquals(origin, _entryOrigin))
        { if (_formDeparted) await ResumeFormAsync(); return; }
        var prior = _saveSession; var oldVisit = _accountVisit;
        _publicationGuard = null; var epoch = ++_formEpoch; _formDeparted = false;
        bool Current() => epoch == _formEpoch && !_formDeparted;
        _loadSession?.Retire(); _loadSession = null; _saveSession = null; _accountVisit = null; _accountReturnPending = false;
        _originalQuery = captured; _queryIdentity = identity; _entryOrigin = origin as EntryPlanOrigin;
        _hasPublishedForm = false; _originalSourceChanged = false; _ready = false; _committed = false;
        _work = null; _workRetired = _workFailed = _reviewingWork = false; _formRead = null; IsBusy = false; Loading = new(); LoadError = null;
        Title = _translator[captured.ContainsKey("id") ? "Plan_EditTitle" : "Plan_NewTitle"];
        ResetFormCommands();
        try
        {
            if (prior is not null) await prior.WaitForIdleAsync();
            if (oldVisit is not null) await oldVisit.WaitForIdleAsync();
            if (!Current()) return;
            var id = OriginalId(captured, "id"); var debt = OriginalId(captured, "fromDebt"); var entry = OriginalId(captured, "of");
            if (new[] { id, debt, entry }.Count(v => v is not null) > 1
                || captured.ContainsKey("entryDraft") && (captured["entryDraft"] is not EntryPlanDraft || id is not null || debt is not null || entry is not null)
                || origin is not null && (origin is not EntryPlanOrigin || !captured.ContainsKey("entryDraft")))
                throw new ArgumentException("The original plan route is ambiguous or unsupported.");
            _entryOrigin?.EnsureCurrent();
            _loadSession = new(_plans, new(Today, id, debt, entry, _entryOrigin?.Source), AcceptFile, () => _entryOrigin?.IsOwned != false);
            _draftIdentity = new() { Name = string.Empty }; _splitDraft = null; ResetFormCommands();
            await ReadOriginalFormAsync();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (Current()) await FailOriginalReadAsync(Loading, error); }
    }
    /// <summary>Revalidates only this retained raw form after its exact pending writers finish.</summary>
    public async Task ResumeFormAsync()
    {
        if (_originalQuery is null || _loadSession is null) return;
        var session = _loadSession; var epoch = _formEpoch;
        if (_saveSession is { } save) await save.WaitForIdleAsync();
        if (_accountVisit is { } visit)
        {
            await visit.WaitForIdleAsync();
            if (!ReferenceEquals(session, _loadSession) || epoch != _formEpoch) return;
            if (visit.Accepted is not null) { session.AcceptAccountReturn(visit); _saveSession = null; _accountReturnPending = true; }
            else _accountVisit = null;
        }
        if (!ReferenceEquals(session, _loadSession) || epoch != _formEpoch) return;
        _formDeparted = false; ResetFormCommands();
        if (_saveSession?.Accepted == true) { _committed = true; PublishFormAvailability(); return; }
        await ReadOriginalFormAsync();
    }
    private Task ReloadOriginalFormAsync() => ReadOriginalFormAsync(reviewAccess: true);
    private Task ReadOriginalFormAsync(bool reviewAccess = false)
    {
        if (_formRead is { IsCompleted: false } pending) return pending;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _formRead = completion.Task; _ = CompleteOriginalReadAsync(completion, reviewAccess); return completion.Task;
    }
    private async Task CompleteOriginalReadAsync(TaskCompletionSource completion, bool reviewAccess)
    {
        try { await ReadAndPublishOriginalAsync(reviewAccess); completion.TrySetResult(); }
        catch (Exception error) { completion.TrySetException(error); }
        finally { if (ReferenceEquals(_formRead, completion.Task)) _formRead = null; }
    }
    private async Task ReadAndPublishOriginalAsync(bool reviewAccess)
    {
        var epoch = _formEpoch; var session = _loadSession; var query = _originalQuery;
        if (session is null || query is null || _formDeparted) return;
        var loading = Loading; var first = !session.HasPublished; PlanEditorReadSnapshot? source = null;
        bool Owner() => epoch == _formEpoch && !_formDeparted && ReferenceEquals(session, _loadSession);
        void Current() { if (!Owner()) throw new OperationCanceledException("The original plan form retired."); _entryOrigin?.EnsureCurrent(); }
        _ready = false; _loading = true; LoadError = null; ResetFormCommands(); PublishFormAvailability();
        try
        {
            await loading.RunAsync(async () =>
            {
                Current(); source = await session.ReadAsync(reviewAccess); Current();
                if (!source.IsAvailable) throw new InvalidOperationException("The explicitly requested original plan source is unavailable.");
                source.EnsureCurrent();
                if (query.TryGetValue("entryDraft", out var draft) && draft is EntryPlanDraft values)
                {
                    var account = source.Inputs.Accounts.SingleOrDefault(a => a.Id == values.AccountId);
                    var destination = source.Inputs.Accounts.SingleOrDefault(a => a.Id == values.ToAccountId);
                    if (account is null || account.IsArchived || account.CurrencyCode != values.CurrencyCode
                        || values.Kind == EntryKind.Transfer && (destination is null || destination.IsArchived || destination.CurrencyCode != values.ToCurrencyCode))
                        throw new PlanEditorSourceChangedException();
                    _ = values.CreateSchedule(Calendars.ToPeriod(_localization.CurrentCalendar));
                }
                await _plans.ValidateEditorSnapshotAsync(source); Current();
            }, () =>
            {
                Current(); var ready = source ?? throw new InvalidOperationException("Complete original plan inputs required.");
                Action guard = () => { Current(); ready.EnsureAccessCurrent(); }; _publicationGuard = guard;
                try
                {
                    if (first) PublishOriginalFields(ready, query);
                    else if (_accountReturnPending)
                    {
                        var selected = Account?.Id; var to = ToAccount?.Id;
                        PublishReferences(ready); guard();
                        if (selected is { } accountId) Accounts = EnsureChoice(Accounts, accountId);
                        if (to is { } destinationId) Accounts = EnsureChoice(Accounts, destinationId);
                        if (_needsDebtPaymentAccount && ready.Debt is { } debt)
                        {
                            var added = ready.Inputs.Accounts.SingleOrDefault(a => a.Id == _accountVisit?.AccountId
                                && !a.IsArchived && a.Type.CanBeDefault() && a.CurrencyCode == debt.CurrencyCode);
                            var choice = Accounts.FirstOrDefault(a => a.Id == added?.Id);
                            var debtChoice = Accounts.Single(a => a.Id == debt.Id);
                            Account = debt.Type == AccountType.Loan ? choice : debtChoice;
                            ToAccount = debt.Type == AccountType.Loan ? debtChoice : choice;
                            HasNoAccounts = _needsDebtPaymentAccount = choice is null;
                        }
                        else
                        {
                            Account = Accounts.FirstOrDefault(a => a.Id == (selected ?? _accountVisit?.AccountId));
                            ToAccount = Accounts.FirstOrDefault(a => a.Id == to);
                        }
                        guard();
                        _accountVisit = null; _accountReturnPending = false;
                    }
                    guard(); _loading = false; Update(); guard();
                    session.Publish(ready);
                    if (_saveSession is null || !ReferenceEquals(_saveSession.Source, ready)) _saveSession = new(_plans, ready);
                    _work = ready.Inputs.Work; _workRetired = _workFailed = false;
                    if (first) _snapshot = Snapshot();
                    _ready = _hasPublishedForm = true; ResetFormCommands(); guard();
                }
                finally { _loading = true; if (ReferenceEquals(_publicationGuard, guard)) _publicationGuard = null; }
            });
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!Owner()) return;
            session.Retire(); _originalSourceChanged = _entryOrigin?.IsOwned == false || error is PlanEditorSourceChangedException or EntryEditorSourceChangedException;
            LoadError = _translator[source is { IsAvailable: false } ? "Plan_SourceUnavailable"
                : _originalSourceChanged ? "Editor_SourceChanged" : "Editor_LoadFailed"];
        }
        finally { if (Owner()) { _loading = false; _publicationGuard = null; PublishFormAvailability(); } }
        if (Owner() && first && Loading.IsReady && source?.Request.DebtId is not null && ReminderEnabled)
            await GuardOriginalReminderAsync(++_reminderRevision);
    }
    private async Task FailOriginalReadAsync(SnapshotLoadState loading, Exception error)
    {
        _ready = false; _publicationGuard = null;
        LoadError = _translator[error is PlanEditorSourceChangedException or EntryEditorSourceChangedException ? "Editor_SourceChanged" : "Editor_LoadFailed"];
        try { await loading.RunAsync(() => Task.FromException(error), () => { }); } catch (Exception failure) when (failure is not OutOfMemoryException) { }
        PublishFormAvailability();
    }
    private void PublishReferences(PlanEditorReadSnapshot source)
    {
        _accounts = source.Inputs.Accounts.ToDictionary(a => a.Id);
        _categories = new(source.Inputs.Categories.ToList(), _translator);
        Accounts = [.. source.Inputs.Accounts.Where(a => !a.IsArchived).OrderBy(a => a.Name).Select(a => new AccountChoice(a.Id, a.Name, a.CurrencyCode))];
        HasNoAccounts = Accounts.Count == 0;
    }
    private void PublishOriginalFields(PlanEditorReadSnapshot source, IDictionary<string, object> query)
    {
        var settings = source.Inputs.Settings ?? new ZananceSettings(); var today = source.Request.Today;
        _existing = null; _hasHistory = false; _committed = false; _needsDebtPaymentAccount = false;
        PublishReferences(source);
        IsAdvanced = settings.Shows(Feature.PlanRules); ShowReminderOnDueDate = IsAdvanced; ShowContractCard = IsAdvanced;
        CanChangeKind = true; ToAccount = null; Account = null; SelectedCategory = null;
        PastIndex = 0; ShowApplyFrom = ShowRuleOptions = false; ApplyFrom = today; ReminderNote = null;
        NameError = AmountError = AccountError = ToAccountError = ToAmountError = RuleError = SaveError = null;
        Title = _translator["Plan_NewTitle"];
        Load(new() { Name = string.Empty, Rule = new() { Start = today, Frequency = Frequency.Once, Calendar = Calendars.ToPeriod(_localization.CurrentCalendar) },
            ReminderDaysBefore = settings.ReminderDaysBefore, ReminderTime = settings.ReminderTime });
        if (source.Inputs.Schedule is { } schedule)
        {
            _existing = schedule.CopyForEditing(); _hasHistory = source.HasHistory;
            Title = _translator["Plan_EditTitle"]; Load(_existing);
            // A historical reference remains usable for original display even when every account is archived.
            HasNoAccounts = Account is null;
            ApplyFrom = Occurrences.NextOpen(_existing, source.Inputs.States, today, today)?.OriginalDate ?? today;
        }
        else if (source.Debt is { } debt)
        {
            var choices = source.Inputs.Accounts.Where(a => !a.IsArchived && a.Type.CanBeDefault() && a.CurrencyCode == debt.CurrencyCode).ToArray();
            var money = choices.FirstOrDefault(a => a.Id == settings.DefaultAccountId) ?? choices.FirstOrDefault();
            if (money is not null) { Load(DebtSetup.Reminder(debt, money.Id, debt.DueDate ?? today.AddMonths(1), Calendars.ToPeriod(_localization.CurrentCalendar))); CanChangeKind = false; }
            else
            {
                // Retain the complete unsaved reminder intent without inventing a money-account identity.
                KindIndex = Array.IndexOf(Kinds, EntryKind.Transfer); Name = debt.Name; AmountModeIndex = (int)AmountMode.Unknown;
                var debtChoice = Accounts.Single(a => a.Id == debt.Id);
                Account = debt.Type == AccountType.Loan ? null : debtChoice;
                ToAccount = debt.Type == AccountType.Loan ? debtChoice : null;
                Start = debt.DueDate ?? today.AddMonths(1); PresetIndex = debt.Installment is > 0 ? 3 : 0;
                ReminderEnabled = true; AutoPost = false; CanChangeKind = false; BuildCategories(null);
                HasNoAccounts = _needsDebtPaymentAccount = true;
            }
        }
        else if (query.TryGetValue("entryDraft", out var raw) && raw is EntryPlanDraft draft) Load(draft.CreateSchedule(Calendars.ToPeriod(_localization.CurrentCalendar)));
        else if (source.Inputs.Entry is { } entry)
        {
            KindIndex = Math.Max(0, Array.IndexOf(Kinds, entry.Kind)); Name = entry.Title ?? _categories.Name(entry.CategoryId);
            Accounts = EnsureChoice(Accounts, entry.AccountId); Account = Accounts.FirstOrDefault(a => a.Id == entry.AccountId);
            if (entry.ToAccountId is { } to) Accounts = EnsureChoice(Accounts, to);
            ToAccount = Accounts.FirstOrDefault(a => a.Id == entry.ToAccountId);
            AmountText = MoneyText.ForInput(entry.Amount, CurrencyOf(entry.AccountId), _localization.CurrentCulture);
            ToAmountText = entry.ToAmount is { } value && ToAccount is not null ? MoneyText.ForInput(value, ToAccount.CurrencyCode, _localization.CurrentCulture) : string.Empty;
            PresetIndex = 3; Start = entry.Date.AddMonths(1); Note = entry.Note ?? string.Empty; BuildCategories(entry.CategoryId);
        }
        else
        {
            Account = Accounts.FirstOrDefault(a => a.Id == settings.DefaultAccountId) ?? Accounts.FirstOrDefault();
            ToAccount = Accounts.FirstOrDefault(a => a.Id != Account?.Id); BuildCategories(null);
        }
        ShowApplyFrom = _existing is not null && _hasHistory; PresetNames = _allPresets;
        ShowRuleOptions = _existing is not null && (_existing.Rule.DayRule != MonthDayRule.SpecificDay
            || _existing.Rule.SecondDay is not null || _existing.Rule.WeekendShift != WeekendShift.None
            || _existing.Rule.MissingDay != MissingDayPolicy.LastValidDay);
    }
    /// <summary>Retires pending original callbacks when this actual native form leaves, retaining its raw draft and accepted writer.</summary>
    public void RetireForm()
    {
        _publicationGuard = null; _formEpoch++; _formDeparted = true; _loadSession?.Retire(); _formRead = null;
        _ready = false; IsBusy = false; ResetFormCommands(); PublishFormAvailability();
    }
}
