using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

public sealed partial class EntryEditorViewModel
{
    private EntryEditorLoadSession? _loadSession;
    private Dictionary<string, object>? _originalQuery;
    private string? _queryIdentity;
    private string? _originalEditorFile;
    private bool _hasPublishedForm;
    private bool _formDeparted;
    private long _formEpoch;
    private Action? _publicationGuard;
    private Task? _formLoadTask;
    private object? _receiptRouteIdentity;
    private object? _templateRouteIdentity;
    private object? _editorRouteIdentity;
    private bool _originalSourceChanged;
    private EntryAccountHandoff? _accountVisit;
    private bool _accountReturnPending;

    /// <summary>Gets the loading cover, explicit failed Retry and complete ready publication.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets original form feedback without exposing native or financial values.</summary>
    [ObservableProperty]
    public partial string? LoadError { get; private set; }
    /// <summary>Gets whether the complete original source owns editable input and commands.</summary>
    public bool IsFormEditable => Loading.IsReady && !_formDeparted && IsNotBusy
        && _loadSession?.Review is { Accepted: false, IsPending: false } && _saveSession is { Accepted: false };
    /// <summary>Gets whether explicit Retry can reread the original form without changing a published raw draft.</summary>
    public bool CanReloadForm => !_formDeparted && IsNotBusy && _originalQuery is not null
        && Loading.HasFailed && _loadSession is not null && !_originalSourceChanged && _saveSession?.Accepted != true;
    /// <summary>Gets Close after known financial acceptance; leaving that form cannot cancel saved money.</summary>
    public string CloseText => _translator[_saveSession?.Accepted == true ? "Common_Close" : "Common_Cancel"];
    /// <summary>Gets the original view epoch for bounded native focus/publication callbacks.</summary>
    internal long FormEpoch => _formEpoch;
    /// <summary>Gets whether raw input has been completely published at least once.</summary>
    internal bool HasPublishedForm => _hasPublishedForm;
    /// <summary>Gets known original financial acceptance for bounded native qualification.</summary>
    internal bool FormAccepted => _saveSession?.Accepted == true;
    /// <summary>Gets the current original draft identity without exposing writer authority.</summary>
    internal Guid FormDraftId => _entry.Id;
    /// <summary>Gets the original complete review for native qualification, never pending facts.</summary>
    internal EntryEditorReadSnapshot? FormReview => Loading.IsReady ? _loadSession?.Review : null;

    partial void OnLoadingChanged(SnapshotLoadState value) { ObserveLoading(value); PublishFormAvailability(); }
    private void ObserveLoading(SnapshotLoadState value) => value.PropertyChanged += (_, _) =>
    { if (ReferenceEquals(value, Loading)) PublishFormAvailability(); };
    private void PublishFormAvailability()
    {
        OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CanRepeat)); OnPropertyChanged(nameof(IsFormEditable));
        OnPropertyChanged(nameof(CanReloadForm)); OnPropertyChanged(nameof(NeedsAccount));
        OnPropertyChanged(nameof(CloseText));
    }
    private bool AcceptOriginalEditorFile(string file)
    {
        var original = Interlocked.CompareExchange(ref _originalEditorFile, file, null);
        return original is null || string.Equals(original, file, StringComparison.Ordinal);
    }

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Count == 0 && _originalQuery is not null) return;
        var captured = new Dictionary<string, object>(query);
        query.TryGetValue("receiptVisit", out var receiptIdentity); query.TryGetValue("templateVisit", out var templateIdentity);
        query.TryGetValue("editorOrigin", out var editorIdentity);
        if (receiptIdentity is Receipts.ReceiptDraft documentVisit)
            captured["receiptVisit"] = documentVisit with { Suggestion = documentVisit.Suggestion with { Candidates = documentVisit.Suggestion.Candidates.ToArray() },
                File = documentVisit.File is { } compact ? compact with { Data = compact.Data.ToArray() } : null };
        if (captured.TryGetValue("receiptFile", out var file) && file is PendingAttachment attachment)
            captured["receiptFile"] = attachment with { Data = attachment.Data.ToArray() };
        if (captured.TryGetValue("receipt", out var rawReceipt) && rawReceipt is ReceiptSuggestion receipt)
            captured["receipt"] = receipt with { Candidates = receipt.Candidates.ToArray() };
        var identity = JsonSerializer.Serialize(captured.Where(p => p.Key is "id" or "duplicate" or "refundOf" or "of" or "kind" or "template" or "account" or "from" or "to" or "reimburse" or "receipt")
            .OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));
        bool SameDocument()
        {
            var old = _originalQuery?.GetValueOrDefault("receiptFile") as PendingAttachment;
            var current = captured.GetValueOrDefault("receiptFile") as PendingAttachment;
            return old is null ? current is null : current is not null && old.FileName == current.FileName
                && old.ContentType == current.ContentType && old.Data.AsSpan().SequenceEqual(current.Data);
        }
        if (identity == _queryIdentity && _originalQuery is not null && ReferenceEquals(receiptIdentity, _receiptRouteIdentity)
            && ReferenceEquals(templateIdentity, _templateRouteIdentity) && ReferenceEquals(editorIdentity, _editorRouteIdentity) && SameDocument())
        { if (_formDeparted) await ResumeFormAsync(); return; }
        _publicationGuard = null; _saveRevision++; var queryEpoch = ++_formEpoch; _formDeparted = false;
        bool CurrentQuery() => queryEpoch == _formEpoch && !_formDeparted;
        _loadSession?.Retire(); _loadSession = null; _quick.Retire(); if (!CurrentQuery()) return;
        _saveSession = null; _hasPublishedForm = false; _accountVisit = null; _accountReturnPending = false;
        _receiptRouteIdentity = receiptIdentity; _templateRouteIdentity = templateIdentity; _editorRouteIdentity = editorIdentity; _originalSourceChanged = false;
        _originalQuery = captured; _queryIdentity = identity; _formLoadTask = null;
        IsBusy = false; if (!CurrentQuery()) return;
        Loading = new(); if (!CurrentQuery()) return;
        LoadError = null; if (!CurrentQuery()) return;
        ResetFormCommands(); if (!CurrentQuery()) return;
        try
        {
            foreach (var key in new[] { "id", "duplicate", "refundOf", "of", "template", "account", "from", "to" })
                if (captured.ContainsKey(key) && (Get(captured, key) is not { } id || id == Guid.Empty))
                    throw new ArgumentException("The original editor identity is invalid.");
            var kind = EntryKind.Expense;
            if (captured.TryGetValue("kind", out var rawKind) && (!Enum.TryParse(rawKind?.ToString(), out kind) || !Enum.IsDefined(kind)))
                throw new ArgumentException("The original editor kind is invalid.");
            var request = new EntryEditorReadRequest(Today, Get(captured, "id"), Get(captured, "duplicate"),
                Get(captured, "refundOf"), Get(captured, "of"), kind, Get(captured, "template"));
            if (!CurrentQuery()) return;
            if (request.SourceId is null && kind is not (EntryKind.Expense or EntryKind.Income or EntryKind.Transfer or EntryKind.IncomeReversal))
                throw new ArgumentException("A new editor kind requires its own original workflow.");
            if (captured.TryGetValue("receiptVisit", out var handoff) && handoff is not Receipts.ReceiptDraft
                || captured.TryGetValue("templateVisit", out var templateHandoff) && templateHandoff is not TemplateVisit
                || captured.TryGetValue("editorOrigin", out var editorHandoff) && editorHandoff is not EntryEditorOrigin)
                throw new ArgumentException("The original editor handoff is unsupported.");
            bool Accept(string path)
            {
                if (captured.TryGetValue("receiptVisit", out var visit) && visit is Receipts.ReceiptDraft document
                    && !document.Review.IsOriginalFile(path)) return false;
                if (captured.TryGetValue("templateVisit", out var chosen) && chosen is TemplateVisit template
                    && !template.IsOriginalFile(path)) return false;
                return AcceptOriginalEditorFile(path);
            }
            _loadSession = new(_store, request, Accept, captured.GetValueOrDefault("editorOrigin") as EntryEditorOrigin); ResetFormCommands();
            if (!CurrentQuery()) return;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!CurrentQuery()) return;
            _loadSession = null; ResetFormCommands();
            if (CurrentQuery()) await FailFormReadAsync(Loading, error); return;
        }
        await LoadOriginalFormAsync();
    }

    /// <summary>Revalidates only the retained original form after returning; it never replaces published raw input.</summary>
    public async Task ResumeFormAsync()
    {
        if (!_formDeparted || _originalQuery is null) return;
        _formDeparted = false; Loading = new(); LoadError = null; ResetFormCommands();
        var epoch = _formEpoch; var save = _saveSession;
        if (save is not null) await save.WaitForIdleAsync();
        if (epoch != _formEpoch || _formDeparted || !ReferenceEquals(save, _saveSession)) return;
        if (_accountVisit is { } visit)
        {
            await visit.WaitForIdleAsync();
            if (epoch != _formEpoch || _formDeparted || !ReferenceEquals(visit, _accountVisit)) return;
            try
            {
                if (visit.Accepted is { } next)
                {
                    _loadSession!.AcceptAccountReturn(visit); _accountReturnPending = true;
                    _saveSession = new(_store, next.Inputs.Resources, next);
                }
                else _accountVisit = null;
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            { await FailFormReadAsync(Loading, error); return; }
        }
        if (_saveSession?.Accepted == true)
        {
            SaveError = _translator["Occurrence_SavedRefreshFailed"];
            await Loading.RunAsync(() => Task.CompletedTask, () => { }); PublishFormAvailability(); return;
        }
        await LoadOriginalFormAsync();
    }
    /// <summary>Retries the exact original read, preserving raw fields after a successful publication.</summary>
    public Task ReloadFormAsync() => CanReloadForm ? LoadOriginalFormAsync() : Task.CompletedTask;

    private Task LoadOriginalFormAsync()
    {
        if (_formLoadTask is { } pending) return pending;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _formLoadTask = completion.Task; _ = CompleteFormReadAsync(completion); return completion.Task;
    }
    private async Task CompleteFormReadAsync(TaskCompletionSource completion)
    {
        try { await LoadOriginalFormCoreAsync(); completion.TrySetResult(); }
        catch (Exception error) { completion.TrySetException(error); }
        finally { if (ReferenceEquals(_formLoadTask, completion.Task)) _formLoadTask = null; }
    }
    private async Task FailFormReadAsync(SnapshotLoadState loading, Exception error)
    {
        _originalSourceChanged = error is EntryEditorSourceChangedException;
        LoadError = _originalSourceChanged ? _translator["Editor_SourceChanged"]
            : CommercialFailureMessage.OrFallback(error, _translator, "Editor_LoadFailed", draftKept: _hasPublishedForm);
        try { await loading.RunAsync(() => Task.FromException(error), () => { }); }
        catch (Exception failure) when (failure is not OutOfMemoryException) { }
        PublishFormAvailability();
    }
    private async Task LoadOriginalFormCoreAsync()
    {
        if (_loadSession is not { } session || _originalQuery is not { } query) return;
        var epoch = _formEpoch; var loading = Loading; var reset = !_hasPublishedForm;
        bool Owner() => epoch == _formEpoch && !_formDeparted && ReferenceEquals(session, _loadSession) && ReferenceEquals(loading, Loading);
        void Current() { if (!Owner()) throw new OperationCanceledException("The original entry form retired."); }
        EntryEditorReadSnapshot? source = null; _loading = true; LoadError = null; PublishFormAvailability();
        try
        {
            await loading.RunAsync(async () =>
            {
                Current();
                if (query.TryGetValue("receiptVisit", out var originalVisit) && originalVisit is Receipts.ReceiptDraft originalReceipt)
                { await Task.Run(() => originalReceipt.ValidateAsync(_receiptReader, Get(query, "id"))); Current(); }
                source = await session.ReadAsync(); Current();
                if (!source.IsAvailable) throw new InvalidOperationException("The original transaction form source is unavailable.");
                if (query.TryGetValue("templateVisit", out var handoff) && handoff is TemplateVisit template)
                {
                    if (template.TemplateId != source.Request.TemplateId) throw new InvalidOperationException("The original template destination changed.");
                    await Task.Run(() => _store.ResolveTemplateVisitAsync(template)); Current();
                }
                if (query.TryGetValue("receiptVisit", out var visit) && visit is Receipts.ReceiptDraft receipt)
                { await Task.Run(() => receipt.ValidateAsync(_receiptReader, source.Request.EditId)); Current(); }
                await _store.ValidateEntryEditorSnapshotAsync(source); Current(); source.EnsureAccessCurrent();
            }, () =>
            {
                Current(); var ready = source ?? throw new InvalidOperationException("Complete original editor source required.");
                Action guard = () => { Current(); ready.EnsureAccessCurrent(); };
                _publicationGuard = guard;
                try
                {
                    if (reset) PublishOriginalForm(ready, query);
                    else if (_accountReturnPending)
                    {
                        ready.EnsureCurrent();
                        var from = Account?.Id; var to = ToAccount?.Id;
                        _quick.PublishOriginal(ready.Inputs.Resources); guard(); PublishReferenceData(ready); guard();
                        if (from is { } fromId) Accounts = EnsureChoice(Accounts, fromId);
                        if (to is { } toId) ToAccounts = EnsureChoice(ToAccounts, toId);
                        UpdateKindState(); guard();
                        Account = Accounts.FirstOrDefault(a => a.Id == (from ?? _accountVisit?.AccountId)); guard();
                        ToAccount = ToAccounts.FirstOrDefault(a => a.Id == to); guard();
                        // Explicit first-account creation resolves receipt units through the existing selection path.
                        // Keep the publication cover and preserve any amount already typed by the user.
                        if (from is null && Account is not null)
                        {
                            _loading = false;
                            try { OnAccountChanged(null, Account); guard(); }
                            finally { _loading = true; }
                        }
                        _accountReturnPending = false; _accountVisit = null;
                    }
                    else ready.EnsureCurrent();
                    guard(); session.Publish(ready); _saveSession ??= new(_store, ready.Inputs.Resources, ready);
                    guard(); ResetFormCommands(); guard();
                }
                finally { if (ReferenceEquals(_publicationGuard, guard)) _publicationGuard = null; }
            });
            Current(); _hasPublishedForm = true;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!Owner()) return;
            session.Retire(); _quick.Retire(); _originalSourceChanged = error is EntryEditorSourceChangedException;
            LoadError = _translator[source is { IsAvailable: false } ? "Editor_SourceUnavailable"
                : error is EntryEditorSourceChangedException ? "Editor_SourceChanged" : "Editor_LoadFailed"];
            System.Diagnostics.Debug.WriteLine(error.GetType().FullName);
        }
        finally { if (Owner()) { _loading = false; _publicationGuard = null; PublishFormAvailability(); } }
    }

    private void ResetOriginalFields(EntryEditorReadSnapshot source)
    {
        _fee = _destinationFee = _refundOf = null; _entry = new(); _isNew = source.Request.EditId is null;
        _pendingAttachment = null; _receipt = null; _receiptAppliedText = _receiptAppliedCurrency = null; _receiptDisplayFactor = 1;
        _previousAccount = null; _previousAmountText = null; _previousAmountMeaning = null; _categoryChosen = false; _beforeSuggestion = null;
        Title = _translator["Entry_NewTitle"]; AmountText = EntryTitle = Payee = Note = ToAmountText = FeeText = DestinationFeeText = string.Empty;
        ForeignAmountText = ReimbursableText = ReimbursedBy = TagsText = string.Empty;
        IconKey = null;
        ForeignCurrency = Currencies.Euro.Code; ForeignEnabled = ShowDetails = ReimbursableEnabled = HasReimbursementDue = IsAggregated = false;
        Date = source.Request.Today; ReimbursementDue = Date.AddDays(30); AggregatedFrom = AggregatedTo = Date;
        FixedKindText = CurrencyNotice = AccountNotice = RuleHint = RefundInfo = null;
        ReceiptNote = ReceiptSource = ReceiptUnitNotice = null; ReceiptSourceDirection = FlowDirection.LeftToRight; ReceiptChoices.Clear();
        AmountError = AccountError = ToAccountError = ToAmountError = FeeError = DestinationFeeError
            = ForeignCurrencyError = ForeignAmountError = ReimbursableError = SaveError = null;
        Templates = []; TemplateChips = []; HasTemplates = false; CanChangeKind = false; KindIndex = 0;
        Account = ToAccount = null; CanAggregate = false;
    }

    private void PublishOriginalForm(EntryEditorReadSnapshot source, Dictionary<string, object> query)
    {
        ResetOriginalFields(source); _quick.PublishOriginal(source.Inputs.Resources); _publicationGuard?.Invoke();
        PublishReferenceData(source); var settings = source.Inputs.Resources.Settings ?? new ZananceSettings();
        _canAggregate = settings.Shows(Feature.AggregatedEntries); _showsFee = settings.Shows(Feature.TransferFee); _startDay = settings.MonthStartDay;
        var active = Accounts; var today = source.Request.Today;
        if (source.Request.EditId is not null && source.Source is { } existing)
        {
            _entry = existing.Copy(); _fee = EntryActions.FindTransferFee(existing, source.Inputs.Related)?.Copy();
            _destinationFee = EntryActions.FindDestinationFee(existing, source.Inputs.Related)?.Copy();
            _refundOf = source.Inputs.Purchase?.Copy(); Title = _translator["Entry_EditTitle"]; LoadFrom(_entry);
        }
        else if (source.Request.DuplicateId is not null && source.Source is { } original)
        {
            _entry = EntryActions.Duplicate(original, today); LoadFrom(_entry);
            if (EntryActions.FindTransferFee(original, source.Inputs.Related) is { } fee)
                FeeText = _feeMoney.Format(fee.Amount, CurrencyOf(original.AccountId), _localization.CurrentCulture);
            if (EntryActions.FindDestinationFee(original, source.Inputs.Related) is { } destination && ToAccount is not null)
                DestinationFeeText = _destinationFeeMoney.Format(destination.Amount, ToAccount.CurrencyCode, _localization.CurrentCulture);
        }
        else if (source.Request.RefundOfId is not null && source.Source is { } purchase)
        {
            _refundOf = purchase.Copy(); var refunds = source.Inputs.Paybacks;
            if (query.ContainsKey("reimburse") && EntryActions.OpenReimbursement(purchase, refunds) is > 0 and var open)
            { _entry = EntryActions.CreateReimbursement(purchase, open, purchase.AccountId, today); Title = _translator["Entry_ReimbursementTitle"]; }
            else { _entry = EntryActions.CreateRefund(purchase, EntryActions.Refundable(purchase, refunds), purchase.AccountId, today); Title = _translator["Entry_RefundTitle"]; }
            LoadFrom(_entry);
        }
        else
        {
            var kind = source.Request.Kind;
            var choice = EntryAccountContract.Choose([.. _accounts.Values], null, Get(query, "account") ?? Get(query, "from"), settings.DefaultAccountId);
            var account = active.FirstOrDefault(a => a.Id == choice.AccountId);
            _entry = new() { Kind = kind, Date = today, AccountId = account?.Id ?? Guid.Empty };
            if (kind == EntryKind.IncomeReversal && source.Source is { } income)
            { _entry.AccountId = income.AccountId; _entry.CategoryId = income.CategoryId; _entry.Title = income.Title; LoadFrom(_entry); }
            else
            {
                CanChangeKind = kind != EntryKind.IncomeReversal; KindIndex = Math.Max(0, Array.IndexOf(ChipKinds, kind));
                FixedKindText = CanChangeKind ? null : _translator[$"EntryKind_{kind}"];
                ShowDetails = settings.Shows(Feature.EntryDetails); Account = account; ToAccount = active.FirstOrDefault(a => a.Id != account?.Id);
                if (Get(query, "from") is { } fromId && active.FirstOrDefault(a => a.Id == fromId) is { } from)
                { Account = from; _entry.AccountId = from.Id; if (ToAccount?.Id == from.Id) ToAccount = active.FirstOrDefault(a => a.Id != from.Id); }
                if (Get(query, "to") is { } toId && active.FirstOrDefault(a => a.Id == toId) is { } to)
                { ToAccount = to; if (Account?.Id == to.Id) { Account = active.FirstOrDefault(a => a.Id != to.Id); _entry.AccountId = Account?.Id ?? Guid.Empty; } }
                Templates = kind is EntryKind.Income or EntryKind.Expense or EntryKind.Transfer ? source.Inputs.Resources.Templates : [];
                HasTemplates = Templates.Count > 0;
                TemplateChips = [.. Templates.Select(t => t.Kind == EntryKind.Transfer
                    ? new TemplateChip(t, FluentIcons.Common.Symbol.ArrowSwap, Palette.TransferText) { CanApply = source.Inputs.Resources.Work.CanApplyTemplate(t.Id) }
                    : new TemplateChip(t, Icons.Parse(t.Icon, _categories.Icon(t.CategoryId)), _categories.Color(t.CategoryId)) { CanApply = source.Inputs.Resources.Work.CanApplyTemplate(t.Id) })];
                if (source.Request.TemplateId is { } templateId) { ApplyReviewedTemplate(Templates.Single(t => t.Id == templateId)); _loading = true; }
            }
        }
        // Complete derived currency/fee state while the loading cover still owns the whole form.
        SynchronizeMoneyInputs(); _loading = false; UpdateKindState(); RefreshMoneyInputs(); UpdateEffect();
        HasNoAccounts = Accounts.Count == 0; UpdateRefundInfo(source); _snapshot = Snapshot();
        if (query.TryGetValue("receiptVisit", out var visit) && visit is Receipts.ReceiptDraft receipt)
        {
            var copied = new Dictionary<string, object>(query) { ["receipt"] = receipt.Suggestion with { Candidates = receipt.Suggestion.Candidates.ToArray() } };
            if (receipt.File is { } document) copied["receiptFile"] = new PendingAttachment(document.Name, document.ContentType, document.Data.ToArray());
            ApplyReceipt(copied);
        }
        else ApplyReceipt(query);
        _saveSession = new(_store, source.Inputs.Resources, source);
    }
}
