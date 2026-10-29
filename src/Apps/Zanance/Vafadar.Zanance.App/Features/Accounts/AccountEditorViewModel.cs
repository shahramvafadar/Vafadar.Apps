using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Features.Entries;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Original explicit account/debt form with native effects separated from committed financial writes.</summary>
public sealed partial class AccountEditorViewModel : ViewModelBase
{
    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly TimeProvider _time;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private Account _account;
    private string _snapshot = string.Empty;
    private readonly IAppInteraction _interaction;
    private bool _committed;
    private EditorWorkReview? _work;
    private bool _reviewingWork;
    private bool _workFailed;
    private bool _workRetired;
    private bool _loading = true;
    private IOriginalAccountHandoff? _originalAccountHandoff;
    private bool _presented = true;
    private long _presentationRevision;
    private long _readRevision, _pendingPresentation, _workReadRevision;
    private CancellationTokenSource _readLifetime = new();
    private Task? _pendingRead;
    private Dictionary<string, object>? _route;
    private Guid? _requestedId;
    private bool _requestedEdit, _routeInvalid, _routeRetired, _formInitialized;
    private string? _sourceFile;
    private Guid? _originalDefault;
    private AccountEditorSnapshot? _editorSource;
    private IAccountEditorOrigin? _origin;
    private Func<bool>? _rootCurrent;

    /// <summary>Binds only the actual page adapter's original native root; headless explicit ports need no global Shell.</summary>
    internal void BindRootOwner(Func<bool> current) => _rootCurrent = current ?? throw new ArgumentNullException(nameof(current));
    private bool OwnsRoot => _rootCurrent?.Invoke() != false;

    /// <summary>Gets the original form's whole loading/error cover; Retry never invokes a metadata or financial writer.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    /// <summary>Gets whether the same captured route can retry its original-file reader.</summary>
    public bool CanReload => _route is not null && !_routeRetired && _presented && !IsBusy && !_committed && !Loading.IsLoading;

    /// <summary>Retires this nested child's pending callbacks on departure without discarding an uncommitted raw form.</summary>
    internal void SetPresentationActive(bool active)
    {
        var resumed = active && !_presented;
        if (_presented != active)
        {
            _presented = active; _presentationRevision++;
            if (!active) { RetireRead(); _workReadRevision++; _reviewingWork = false; }
        }
        _localization.Changed -= OnDisplayChanged;
        if (active)
        {
            _localization.Changed += OnDisplayChanged;
            Form.RefreshTexts();
            // D-639: initial appearance can follow a completed failure; only an interrupted pending read resumes.
            if (resumed && _route is not null && Loading.IsLoading && !_committed) _ = LoadAsync();
        }
        RefreshAccess();
    }
    private void OnDisplayChanged(object? sender, EventArgs args)
    {
        var pendingRead = Loading.IsLoading;
        _presentationRevision++; RetireRead(); Form.RefreshTexts(); RefreshTitle();
        // Translation/format changes preserve already captured source and raw monetary input; only an unfinished read restarts.
        if (pendingRead && _presented && _route is not null && !_committed) _ = LoadAsync();
        RefreshAccess();
    }
    private void RetireRead()
    {
        _readRevision++; var old = _readLifetime; _readLifetime = new(); old.Cancel(); old.Dispose();
    }
    private bool OwnsPresentation(long revision) => _presented && OwnsRoot && revision == _presentationRevision
        && (_originalAccountHandoff is null || _originalAccountHandoff.IsOwned);
    private bool OwnsChild(long revision) => !_routeRetired && OwnsPresentation(revision);
    private void RefreshTitle() => Title = _translator[Form.IsDebtType
        ? (_requestedEdit ? "Debt_EditTitle" : "Debt_NewTitle")
        : (_requestedEdit ? "Account_EditTitle" : "Account_NewTitle")];

    /// <summary>Creates the original form without starting database or native work.</summary>
    public AccountEditorViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, TimeProvider time, IAppInteraction interaction)
    {
        _interaction = interaction;
        PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IsBusy)) RefreshAccess(); };
        _store = store;
        _plans = plans;
        _time = time;
        _translator = translator;
        _localization = localization;
        Form = new AccountFormModel(translator, time, localization);
        _account = new Account { Name = string.Empty, CurrencyCode = Form.CurrencyCode, OpeningDate = Form.OpeningDate };
        Title = translator["Account_NewTitle"];
        Form.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AccountFormModel.MoneyContextsReady)) RefreshAccess();
            if (e.PropertyName == nameof(AccountFormModel.TypeIndex))
            {
                UpdateCanBeDefault();
                Title = _translator[Form.IsDebtType ? (IsExisting ? "Debt_EditTitle" : "Debt_NewTitle") : (IsExisting ? "Account_EditTitle" : "Account_NewTitle")];
            }
        };
        UpdateCanBeDefault();
    }

    public AccountFormModel Form { get; }

    /// <summary>Gets whether user input can change this loaded, not-yet-committed form.</summary>
    public bool CanInteract => !_loading && Loading.IsReady && !IsBusy && !_committed && OwnsChild(_presentationRevision);
    /// <summary>Gets whether a financial Save can run; a committed account cannot be resubmitted.</summary>
    public bool CanSave => CanInteract && Form.MoneyContextsReady && !_reviewingWork && !_workFailed && !_workRetired && _work is not null && WorkRestriction is null;
    /// <summary>Gets whether a pending writer must finish before leaving this form.</summary>
    public bool CanLeave => !IsBusy;
    /// <summary>Gets the actual Close/Cancel caption after the commit boundary.</summary>
    public string CloseText => _translator[_committed ? "Common_Close" : "Common_Cancel"];
    private Core.Commerce.WorkRestriction? WorkRestriction => _work?.AccountRestriction(_account.Id, IsArchived);
    /// <summary>Gets a captured reason before Save; no financial field validation or write occurs here.</summary>
    public string? WorkText => _workFailed ? _translator["Editor_WorkReadFailed"]
        : _workRetired ? _translator["Editor_WorkRetired"]
        : WorkRestriction is { } restriction ? Presentation.CommercialFailureMessage.Describe(restriction, _translator) : null;
    /// <summary>Gets whether the original availability can be explicitly refreshed without changing any draft.</summary>
    public bool CanReviewWork => _route is not null && _presented && !_routeRetired && !IsBusy && !_committed && !_reviewingWork && !Loading.IsLoading;
    private async Task ReadWorkAsync()
    {
        if (!_formInitialized) { await LoadAsync(); return; }
        var presentation = _presentationRevision; var source = _editorSource; var workRead = ++_workReadRevision;
        _reviewingWork = true; RefreshAccess();
        try
        {
            EditorWorkReview next; AccountEditorSnapshot? fresh = null;
            if (_originalAccountHandoff is { } child)
            {
                child.EnsureCurrent(); next = await child.ReadWorkAsync(_store); child.EnsureCurrent();
            }
            else
            {
                if (source is null) throw new AccountEditorUnavailableException();
                fresh = await _store.ReadAccountEditorAsync(_requestedId, path => path == source.DatabasePath);
                if (!fresh.HasSameInputs(source)) throw new AccountEditorUnavailableException();
                next = fresh.Work;
            }
            if (workRead != _workReadRevision || !OwnsChild(presentation) || !ReferenceEquals(source, _editorSource)) return;
            next.EnsureCurrent(); if (fresh is not null) _editorSource = fresh;
            _work = next; _workFailed = false; _workRetired = false;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (workRead == _workReadRevision && OwnsChild(presentation) && ReferenceEquals(source, _editorSource)) { _work = null; _workFailed = true; }
        }
        finally { if (workRead == _workReadRevision && OwnsChild(presentation)) { _reviewingWork = false; RefreshAccess(); } }
    }
    [RelayCommand(CanExecute = nameof(CanReviewWork))]
    private Task ReviewWorkAsync() => CanReviewWork ? ReadWorkAsync() : Task.CompletedTask;
    [RelayCommand(CanExecute = nameof(CanReload))]
    private Task ReloadAsync() => CanReload ? LoadAsync() : Task.CompletedTask;

    private void RefreshAccess()
    {
        OnPropertyChanged(nameof(CanReload)); ReloadCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(WorkText)); OnPropertyChanged(nameof(CanReviewWork)); ReviewWorkCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanLeave)); OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CloseText));
        SaveCommand.NotifyCanExecuteChanged(); ToggleArchiveCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    [ObservableProperty]
    public partial bool IsDefault { get; set; }

    /// <summary>
    /// Gets a value indicating whether the account can be the default of new entries: a money account that is not archived
    /// (<see cref="EntryAccountContract.IsValidDefault"/>). A loan, money lent or an asset is never offered.
    /// </summary>
    [ObservableProperty]
    public partial bool CanBeDefault { get; set; }

    partial void OnIsArchivedChanged(bool value) => UpdateCanBeDefault();

    private void UpdateCanBeDefault() => CanBeDefault = Form.Type.CanBeDefault() && !IsArchived;

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    /// <summary>Gets a value indicating whether the user changed something.</summary>
    public bool IsDirty => Snapshot() != _snapshot;

    // The form and the default switch: switching the default and cancelling asks like any other change.
    private string Snapshot() => $"{Form.Snapshot()}|{IsDefault}";

    /// <summary>Loads the actual draft through the native page adapter without a write or route transition.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("accountEditorOrigin", out var parentOrigin))
        {
            // Retain only a small authority handle, not the parent's complete account/ledger snapshot.
            var normalizedOrigin = parentOrigin switch
            {
                AccountEditorOrigin captured => captured,
                AccountOverviewSnapshot overview => overview.EditorOrigin,
                AccountDetailSnapshot detail => detail.EditorOrigin,
                _ => throw new ArgumentException("The original account editor parent source is invalid.")
            };
            query = new Dictionary<string, object>(query) { ["accountEditorOrigin"] = normalizedOrigin };
        }
        if (_route is not null)
        {
            if (_route.Count != query.Count || _route.Any(pair => !query.TryGetValue(pair.Key, out var value) || !Equals(value, pair.Value)))
            {
                _routeRetired = true; RetireRead(); _loading = true; _editorSource = null; _work = null;
                SaveError = _translator["Account_Unavailable"]; var failed = Loading = new();
                try { await failed.RunAsync(() => Task.FromException(new AccountEditorUnavailableException()), () => { }); }
                catch (AccountEditorUnavailableException) { }
                RefreshAccess();
            }
            return;
        }
        _route = new(query); _presentationRevision++;
        if (query.TryGetValue("accountEditorOrigin", out var origin))
        {
            if (origin is not AccountEditorOrigin captured)
                throw new ArgumentException("The original account editor parent source is invalid.");
            _origin = captured; _sourceFile = captured.DatabasePath;
        }
        _requestedEdit = query.TryGetValue("id", out var requested);
        if (_requestedEdit)
        {
            if (requested is Guid id && id != Guid.Empty) _requestedId = id;
            else _routeInvalid = true;
        }
        var originalVisit = query.TryGetValue("originalAccountHandoff", out var handoff);
        if (originalVisit || query.TryGetValue("entryAccountHandoff", out handoff))
        {
            if (handoff is not IOriginalAccountHandoff visit || query.ContainsKey("id") || _origin is not null
                || originalVisit && query.ContainsKey("entryAccountHandoff"))
                throw new ArgumentException("The original nested account route is invalid.");
            _originalAccountHandoff = visit;
        }
        RefreshTitle(); await LoadAsync();
    }

    /// <summary>Shares only the same live presentation read; a retired pending reader never blocks its new owner.</summary>
    public Task LoadAsync()
    {
        if (_route is null || !OwnsChild(_presentationRevision) || _committed || IsBusy) return Task.CompletedTask;
        if (_pendingRead is { IsCompleted: false } && _pendingPresentation == _presentationRevision) return _pendingRead;
        _pendingPresentation = _presentationRevision;
        return _pendingRead = LoadCoreAsync();
    }

    private async Task LoadCoreAsync()
    {
        RetireRead(); _workReadRevision++; var revision = _readRevision; var presentation = _presentationRevision;
        var token = _readLifetime.Token; var state = Loading = new();
        _loading = true; _reviewingWork = true; SaveError = null;
        AccountEditorSnapshot? captured = null; ZananceSettings? settings = null; EditorWorkReview? work = null;
        Account? original = null; var currencyLock = AccountCurrencyLock.None;
        void Current()
        {
            token.ThrowIfCancellationRequested();
            if (revision != _readRevision || !OwnsChild(presentation)) throw new OperationCanceledException("The original account reader retired.", token);
            captured?.EnsureCurrent(); _origin?.EnsureCurrent(); _originalAccountHandoff?.EnsureCurrent();
        }
        try
        {
            var pending = state.RunAsync(async () =>
            {
                Current(); if (_routeInvalid) throw new AccountEditorUnavailableException();
                if (_originalAccountHandoff is { } child)
                {
                    settings = child.Settings ?? new ZananceSettings(); work = await child.ReadWorkAsync(_store); Current();
                }
                else
                {
                    captured = await _store.ReadAccountEditorAsync(_requestedId, path =>
                    {
                        _sourceFile ??= path; return _sourceFile == path;
                    }, token);
                    Current();
                    await _store.ValidateAccountEditorAsync(captured, token); Current();
                    settings = captured.Settings; work = captured.Work; currencyLock = captured.Inputs.CurrencyLock;
                    original = _requestedId is { } id ? captured.Inputs.Accounts.Single(row => row.Id == id) : null;
                    if (_formInitialized && (!captured.MatchesAccount(_account) || settings.DefaultAccountId != _originalDefault
                        || currencyLock.IsLocked && !string.Equals(Form.CurrencyCode, _account.CurrencyCode, StringComparison.OrdinalIgnoreCase)))
                        throw new AccountEditorUnavailableException();
                }
                work!.EnsureCurrent(); Current();
            }, () =>
            {
                void Publish(Action action) { Current(); action(); Current(); }
                Publish(() => Form.ShowAdvanced = settings!.Shows(Feature.AccountOptions));
                if (!_formInitialized)
                {
                    if (original is not null)
                    {
                        _account = original.CopyForEditing();
                        Publish(() => IsExisting = true); Publish(() => IsArchived = original.IsArchived);
                        Publish(() => Form.Load(_account, _localization.CurrentCulture, currencyLock));
                        Publish(() => IsDefault = settings!.DefaultAccountId == _account.Id);
                    }
                    else
                    {
                        Publish(() => Form.CurrencyCode = _originalAccountHandoff?.SuggestedCurrencyCode ?? settings!.DefaultCurrencyCode);
                        if (_route!.TryGetValue("debt", out var debt))
                        {
                            if (debt is true) Publish(() => Form.TypeIndex = (int)AccountType.Loan);
                            else if (debt is AccountType type && type.IsDebt()) Publish(() => Form.TypeIndex = (int)type);
                        }
                    }
                    Current(); _originalDefault = settings!.DefaultAccountId; _snapshot = Snapshot(); _formInitialized = true;
                }
                Publish(() => Form.RefreshCurrencyLock(currencyLock)); Publish(RefreshTitle);
                _editorSource = captured; _work = work; _workFailed = false; _workRetired = false;
            });
            RefreshAccess(); await pending;
        }
        catch (OperationCanceledException) when (revision != _readRevision || !OwnsChild(presentation)) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (revision == _readRevision && OwnsChild(presentation))
            {
                _editorSource = null; _work = null; _workFailed = true;
                SaveError = exception is AccountEditorUnavailableException ? _translator["Account_Unavailable"]
                    : CommercialFailureMessage.OrFallback(exception, _translator, "Error_Unexpected");
            }
        }
        finally
        {
            if (revision == _readRevision && OwnsChild(presentation))
            { _loading = !state.IsReady; _reviewingWork = false; RefreshAccess(); }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!CanInteract) return;
        Form.RefreshMoneyTexts();
        if (!CanSave) return;
        var presentation = _presentationRevision;
        var raw = Snapshot(); var moneyRevision = Form.MoneyRevision; var originalWork = _work!;
        bool CurrentDraft() => OwnsChild(presentation) && ReferenceEquals(_work, originalWork)
            && Snapshot() == raw && Form.MoneyRevision == moneyRevision && Form.MoneyContextsReady;
        IsBusy = true; SaveError = null;
        var target = _account.CopyForEditing(); Account? acceptedAccount = null;
        void AcceptOriginal(Account actual)
        {
            // Record the exact writer result before fallible observers, without publishing a second financial command.
            acceptedAccount = actual; _account = actual; _committed = true; _snapshot = raw;
        }
        try
        {
            // Disable input before the account read, so another native Save cannot race validation.
            _originalAccountHandoff?.EnsureCurrent();
            var accounts = _originalAccountHandoff?.Accounts ?? _editorSource?.Inputs.Accounts
                ?? throw new AccountEditorUnavailableException();
            if (!Form.TryApply(target, _localization.CurrentCulture, accounts)) return;
            if (!CurrentDraft()) throw new InvalidOperationException("The original account input changed before Save.");
            if (_originalAccountHandoff is { } child)
            {
                await child.SaveAsync(_store, target, IsDefault, CurrentDraft);
                _account = target; _committed = true; _snapshot = raw; RefreshAccess();
                if (child.NotificationFailed) { SaveError = _translator["Account_SavedNeedsReview"]; return; }
            }
            else if (!await _store.SaveAccountAsync(target, isCurrent: CurrentDraft, originalWork: originalWork, originalSource: _editorSource,
                makeDefault: IsDefault, accepted: AcceptOriginal))
            {
                SaveError = Form.CurrencyLockText ?? _translator["Account_CurrencyLocked"];
                return;
            }
            if (_originalAccountHandoff is null)
            {
                _account = acceptedAccount ?? target; _committed = true; _snapshot = raw; RefreshAccess();
            }
        }
        catch (CommittedWriteNotificationException)
        {
            _account = acceptedAccount ?? target; _committed = true; _snapshot = raw; RefreshAccess();
            SaveError = _translator["Account_SavedNeedsReview"]; return;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!_committed && exception is CommercialWriteRejectedException) { _workRetired = true; RefreshAccess(); }
            SaveError = _committed ? _translator["Account_SavedNeedsReview"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true);
            return;
        }
        finally { IsBusy = false; }

        try
        {
            if (!OwnsChild(presentation)) return;
            if (Snapshot() != raw) { SaveError = _translator["Account_SavedDraftChanged"]; return; }
            await _interaction.NavigateAsync("..");
            if (_originalAccountHandoff is null && !IsExisting && Form.IsDebtType && OwnsRoot && _editorSource is { } source)
            {
                await _store.ValidateAccountEditorAccessAsync(source);
                if (OwnsRoot) await _interaction.NavigateAsync(AppRoutes.AccountDetailRoute, new Dictionary<string, object> { ["id"] = _account.Id });
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            SaveError = _translator["Editor_SavedReturnFailed"]; if (OwnsRoot) await _interaction.ShowFailureAsync(exception);
        }
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task ToggleArchiveAsync()
    {
        if (!CanInteract || !IsExisting || _editorSource is not { } source) return;
        var presentation = _presentationRevision; var raw = Snapshot();
        bool Current() => OwnsChild(presentation) && ReferenceEquals(source, _editorSource) && Snapshot() == raw;
        IsBusy = true; SaveError = null; AccountArchiveResult? accepted = null;
        try
        {
            await _store.ValidateAccountEditorAsync(source);
            if (!Current()) return;
            var reviewed = await _store.ReadAccountArchiveAsync(_account.Id);
            if (!Current()) return;
            if (reviewed.IsArchived != IsArchived) throw new InvalidOperationException("The original archive state changed.");
            var archive = !IsArchived; var action = _translator[archive ? "Account_Archive" : "Account_Unarchive"];
            if (archive || IsDirty)
            {
                var message = reviewed.PlanNames.Count == 0 ? _translator["Account_ArchiveMessage"]
                    : _translator.Format("Account_ArchiveWithPlans", reviewed.PlanNames.Count, string.Join(", ", reviewed.PlanNames.Take(5)));
                if (!archive) message = _translator["Account_UnarchiveDraft"];
                if (IsDirty) message += "\n\n" + _translator["Account_ArchiveDraftDiscard"];
                if (!await _interaction.ConfirmAsync(action, message, action, _translator["Common_KeepEditing"]) || !Current()) return;
            }
            accepted = await _store.WriteAccountArchiveAsync(reviewed, archive, _plans, isCurrent: Current, originalSource: source);
            var laterDraft = Snapshot() != raw;
            AcceptArchive(accepted);
            if (laterDraft)
            {
                _snapshot = raw; SaveError = _translator["Account_SavedDraftChanged"]; return;
            }
            if (!OwnsChild(presentation)) return;
            Form.Load(_account, _localization.CurrentCulture, accepted.CurrencyLock);
            _snapshot = Snapshot();
            if (accepted.DefaultCleared && OwnsChild(presentation)) await _interaction.AlertAsync(_translator["Account_Archive"], _translator["Account_DefaultCleared"], _translator["Common_Ok"]);
        }
        catch (AccountArchivePublicationException exception)
        {
            var laterDraft = Snapshot() != raw;
            accepted = exception.Result; AcceptArchive(accepted);
            if (laterDraft) _snapshot = raw;
            SaveError = _translator["Account_ArchiveSavedReview"] + (laterDraft ? "\n\n" + _translator["Account_SavedDraftChanged"] : string.Empty);
            return;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            SaveError = accepted is not null ? _translator["Account_ArchiveSavedReview"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Account_ArchiveNeedsReview", draftKept: true);
            return;
        }
        finally { IsBusy = false; }
        try { if (OwnsChild(presentation)) await _interaction.NavigateAsync(".."); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsChild(presentation)) { SaveError = _translator["Editor_SavedReturnFailed"]; await _interaction.ShowFailureAsync(exception); } }
    }

    private void AcceptArchive(AccountArchiveResult result)
    {
        _account = result.Account; IsArchived = _account.IsArchived;
        if (result.DefaultCleared) IsDefault = false;
        _committed = true; _snapshot = Snapshot(); RefreshAccess();
    }

    [RelayCommand(CanExecute = nameof(CanLeave))]
    private async Task CancelAsync()
    {
        var presentation = _presentationRevision;
        if (!OwnsPresentation(presentation)) return;
        if (!_formInitialized || !IsDirty || await ConfirmDiscardAsync())
        {
            if (!OwnsPresentation(presentation)) return;
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
        }
    }

    /// <summary>Asks whether unsaved changes may be discarded.</summary>
    public async Task<bool> ConfirmDiscardAsync()
    {
        var presentation = _presentationRevision; var raw = Snapshot();
        bool Current() => OwnsPresentation(presentation) && Snapshot() == raw;
        return Current() && await _interaction.ConfirmAsync(_translator["Common_DiscardTitle"],
            _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]) && Current();
    }

}
