using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>
/// Creates or edits an asset type (ZEX-S0401): name, kind, weight or count, metal, purity (karat or fineness), unit weight
/// and divisibility of counted items, and the currency of its prices. Quantities add up only within one type, so 18 k
/// and 24 k gold are two types. After a new type the user records what they already own (an opening holding).
/// </summary>
public sealed partial class AssetTypeEditorViewModel : ViewModelBase, Presentation.IUnsavedChanges, Presentation.IThemeAware
{
    private static readonly Metal[] Metals = [Metal.Gold, Metal.Silver, Metal.Platinum, Metal.Palladium, Metal.Other, Metal.None];

    private readonly HoldingStore _holdings;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private AssetType? _existing;
    private readonly IAppInteraction _interaction;
    private HoldingTypeReview? _review;
    private Guid _id;
    private long _revision;
    private bool _initialized, _loadRequested, _editing, _invalidRoute, _routeRetired, _departed, _loading = true;
    private bool _publishing, _needsReload, _pendingReview, _resumeRead;
    private long _viewRevision, _readRevision, _pendingView;
    private CancellationTokenSource _readLifetime = new();
    private Task? _pendingRead;
    private AssetTypeEditorSnapshot? _source;
    private AssetTypeEditorOrigin? _origin;
    private string? _sourceFile, _feedbackKey;
    private Func<bool>? _ownsRoot;
    private Action? _publicationGuard;
    private Draft? _baselineDraft, _retainedReadDraft, _retainedReadBaseline;
    /// <summary>Gets whether complete original source publication succeeded for this form.</summary>
    [ObservableProperty] public partial bool IsReady { get; set; }
    /// <summary>Gets whether incomplete or failed source must cover all editable fields.</summary>
    public bool IsCovered => !IsReady;
    /// <summary>Gets whether the original acquisition is pending.</summary>
    public bool IsLoading => _loading;
    /// <summary>Gets whether an explicit read-only Retry may reload this same original form.</summary>
    public bool CanReload => !_departed && !_routeRetired && OwnsRoot && !_loading && !_publishing && !IsBusy && !Committed;
    /// <summary>Gets whether the failed original source offers an explicit read-only Retry.</summary>
    public bool ShowRetry => !Committed && _needsReload && !_invalidRoute && !_routeRetired;
    /// <summary>Gets whether the original form may close without affecting another frame.</summary>
    public bool CanLeave => !_departed && OwnsRoot && !_publishing && !IsBusy;
    /// <summary>Gets whether retained dirty input needs explicit review of changed original saved values.</summary>
    public bool RequiresReview => _pendingReview;
    /// <summary>Gets complete current saved editable values after a draft-preserving reload.</summary>
    [ObservableProperty] public partial string? ReloadedSavedSummary { get; set; }
    /// <summary>Gets whether the original type is known to have been saved, even after a following effect fails.</summary>
    public bool Committed { get; private set; }
    /// <summary>Gets the stable original saved identity without creating another type.</summary>
    public Guid? AcceptedId => Committed ? _id : null;
    /// <summary>Gets whether the current original form may submit.</summary>
    public bool CanSave => CanEdit && !_needsReload && !_pendingReview && _review is not null;
    /// <summary>Gets whether raw fields remain editable without changing original source authority.</summary>
    public bool CanEdit => !_departed && !_routeRetired && OwnsRoot && IsReady && !_loading && !_publishing && !IsBusy && !Committed;
    /// <summary>Gets the actual close/cancel caption for this original form.</summary>
    public string CloseText => _translator[Committed ? "Common_Close" : "Common_Cancel"];
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
    /// <summary>Displays a completed original native caption with permanently retired write authority.</summary>
    internal void ShowRetainedOutcome(string key, bool committed, bool isNew)
    { Committed = committed; _routeRetired = true; _review = null; IsReady = true; _loading = false; PublishState(); Title = _translator[isNew ? "AssetType_NewTitle" : "AssetType_EditTitle"]; Error = _translator[key]; }
#endif
    private void PublishState()
    {
        OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CloseText));
        foreach (var name in new[] { nameof(AcceptedId), nameof(IsDirty), nameof(IsCovered), nameof(IsLoading), nameof(CanReload), nameof(CanLeave), nameof(RequiresReview), nameof(ShowRetry) }) OnPropertyChanged(name);
        SaveCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged(); AcceptReviewCommand.NotifyCanExecuteChanged();
    }
    /// <summary>Retires departed continuations without replacing raw input or a known saved identity.</summary>
    public void RetireReview() => SetViewActive(false);
    /// <summary>Binds the actual native form to the root that published it, never a later replacement.</summary>
    public void BindRootOwner(Func<bool> ownsRoot) { ArgumentNullException.ThrowIfNull(ownsRoot); _ownsRoot = ownsRoot; }
    private bool OwnsRoot => _ownsRoot?.Invoke() != false;
    private bool OwnsView(long owner) => !_departed && OwnsRoot && owner == _viewRevision;
    /// <summary>Retires original reads and native continuations on actual form departure; same-form return resumes only required reads.</summary>
    public void SetViewActive(bool active)
    {
        if (active == !_departed) return;
        _departed = !active; _viewRevision++;
        if (!active) { _resumeRead = _loadRequested && _loading && !IsReady; RetireRead(); }
        else if (_loadRequested && !Committed && !_routeRetired && OwnsRoot && (_resumeRead || _source is not null))
        { _resumeRead = false; _ = LoadAsync(_baselineDraft is not null); }
        PublishState();
    }
    private void RetireRead()
    { _readRevision++; var old = _readLifetime; _readLifetime = new(); old.Cancel(); old.Dispose(); }
    private void InvalidateSource(string key)
    { _review = null; _needsReload = true; IsReady = false; SetFeedback(key); PublishState(); }
    private void SetFeedback(string? key) { _feedbackKey = key; Error = key is null ? null : _translator[key]; }
    private void Publish(Action action) { var guard = _publicationGuard; guard?.Invoke(); action(); guard?.Invoke(); }

    public AssetTypeEditorViewModel(HoldingStore holdings, ZananceStore store, Translator translator, ILocalizationService localization, IAppInteraction interaction)
    {
        _interaction = interaction;
        _holdings = holdings;
        ArgumentNullException.ThrowIfNull(store);
        _translator = translator;
        _localization = localization;
        Title = translator["AssetType_NewTitle"]; PurityIndex = Purity.Common.Count; RefreshDisplayCore();
    }

    /// <summary>Gets live original kind captions without changing stored enum identities.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> KindNames { get; set; } = [];
    /// <summary>Gets live original mass/count captions.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> DimensionNames { get; set; } = [];
    /// <summary>Gets live original metal captions in their unchanged original order.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> MetalNames { get; set; } = [];
    /// <summary>Gets original common purity labels plus the live explicit free-input caption.</summary>
    [ObservableProperty] public partial IReadOnlyList<string> PurityNames { get; set; } = [];

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    [ObservableProperty]
    public partial int DimensionIndex { get; set; }

    [ObservableProperty]
    public partial bool IsCount { get; set; }

    [ObservableProperty]
    public partial bool DimensionLocked { get; set; }

    [ObservableProperty]
    public partial bool HasMetal { get; set; } = true;

    [ObservableProperty]
    public partial int MetalIndex { get; set; }

    /// <summary>Gets or sets the common purity chosen (24, 22, 21, 18, 14 k) or the last index for free input.</summary>
    [ObservableProperty]
    public partial int PurityIndex { get; set; }

    /// <summary>Gets or sets a free purity: karat ("18") or fineness ("750", "999.9"); empty = unknown.</summary>
    [ObservableProperty]
    public partial string PurityText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsOtherPurity { get; set; } = true;

    [ObservableProperty]
    public partial string UnitWeightText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CountUnitName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Divisible { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; } = Currencies.Euro.Code;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    /// <summary>Captures one original requested identity; empty native redelivery never erases a draft.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var requested = query.TryGetValue("id", out var value); var id = value is Guid key && key != Guid.Empty ? key : (Guid?)null;
        var origin = query.TryGetValue("assetTypeEditorOrigin", out var supplied) ? supplied as AssetTypeEditorOrigin : null;
        if (_loadRequested)
        {
            if (!requested && origin is null) return;
            if (requested && (!_editing || id != _id) || origin?.DatabasePath != _origin?.DatabasePath)
            { _routeRetired = true; RetireRead(); InvalidateSource("AssetType_SourceChanged"); }
            return;
        }
        _loadRequested = true; _editing = requested; _id = id ?? Guid.CreateVersion7();
        _invalidRoute = requested && id is null || supplied is not null && origin is null;
        _origin = origin; _sourceFile = origin?.DatabasePath;
        Title = _translator[_editing ? "AssetType_EditTitle" : "AssetType_NewTitle"];
        await LoadAsync(false);
    }
    /// <summary>Shares only the current pending acquisition; a completed failure always waits for explicit Retry.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (!_loadRequested) await ApplyQueryAsync(new Dictionary<string, object>());
        else if (_pendingRead is { IsCompleted: false } pending && _pendingView == _viewRevision) await pending;
    }
    private Task LoadAsync(bool keepDraft)
    {
        if (_departed || _routeRetired || !OwnsRoot || Committed) return Task.CompletedTask;
        if (_pendingRead is { IsCompleted: false } pending && _pendingView == _viewRevision) return pending;
        var owner = _viewRevision; _pendingView = owner; var task = LoadCoreAsync(keepDraft);
        if (_pendingView == owner && _viewRevision == owner) _pendingRead = task;
        return task;
    }
    private async Task LoadCoreAsync(bool keepDraft)
    {
        RetireRead(); var read = _readRevision; var owner = _viewRevision; var token = _readLifetime.Token;
        AssetTypeEditorSnapshot? captured = null;
        void Current()
        {
            token.ThrowIfCancellationRequested();
            if (!OwnsView(owner) || _routeRetired || read != _readRevision) throw new OperationCanceledException("Original type read retired.", token);
            captured?.EnsureAccessCurrent();
        }
        var draft = keepDraft ? _retainedReadDraft ?? CaptureDraft() : null;
        var baseline = keepDraft ? _retainedReadBaseline ?? _baselineDraft : _baselineDraft; var previous = _source;
        if (draft is not null && baseline is not null) { _retainedReadDraft = draft; _retainedReadBaseline = baseline; }
        Current(); _loading = true; IsReady = false; SetFeedback(null); PublishState();
        try
        {
            if (_invalidRoute) throw new HoldingEventReviewChangedException("Original requested type identity is invalid.");
            if (_source is null) _origin?.EnsureCurrent(); Current();
            captured = await _holdings.ReadTypeEditorSnapshotAsync(_id, _editing, path =>
            { Current(); _sourceFile ??= path; return _sourceFile == path; }, token);
            Current(); await _holdings.ValidateTypeEditorSnapshotAsync(captured, token); Current();
            var changed = previous is not null && !await Task.Run(() => captured.HasSameInputs(previous), token);
            Current(); draft = _retainedReadDraft ?? draft; baseline = _retainedReadBaseline ?? baseline;
            _publicationGuard = Current; _publishing = true; _review = captured.Review; _existing = captured.Review.Inputs.Type;
            _purityCulture = CapturePhysicalCulture(); _weightCulture = CapturePhysicalCulture();
            var type = _existing; var purity = type?.PurityPer10000;
            var common = purity is null ? -1 : Purity.Common.ToList().FindIndex(item => item.Per10000 == purity);
            Publish(() => Title = _translator[_editing ? "AssetType_EditTitle" : "AssetType_NewTitle"]);
            Publish(() => Name = type?.Name ?? string.Empty);
            Publish(() => KindIndex = (int)(type?.Kind ?? AssetKind.PreciousMetal));
            Publish(() => DimensionIndex = (int)(type?.Dimension ?? AssetDimension.Mass));
            Publish(() => MetalIndex = type is null ? 0 : Math.Max(0, Array.IndexOf(Metals, type.Metal)));
            Publish(() => PurityIndex = common >= 0 ? common : Purity.Common.Count);
            Publish(() => PurityText = common < 0 && purity is { } value ? (value / 10m).ToString("0.#", _purityCulture) : string.Empty);
            Publish(() => UnitWeightText = type?.UnitWeightMg is { } weight ? (weight / 1000m).ToString("0.###", _weightCulture) : string.Empty);
            Publish(() => CountUnitName = type?.CountUnitName ?? string.Empty);
            Publish(() => Divisible = type?.Divisible ?? false);
            Publish(() => CurrencyCode = type?.PriceCurrencyCode ?? captured.Settings.DefaultCurrencyCode);
            Publish(() => Note = type?.Note ?? string.Empty);
            Publish(() => DimensionLocked = captured.Review.Inputs.Events.Count > 0);
            _baselineDraft = CaptureDraft(); _snapshot = Fingerprint(_baselineDraft);
            if (draft is not null && baseline is not null) MergeDraft(draft, baseline);
            Current(); _pendingReview = changed && draft is not null && baseline is not null && IsDirty;
            Publish(() => ReloadedSavedSummary = _pendingReview ? SavedSummary(type) : null);
            RefreshDisplayCore(); Current(); _source = captured; _initialized = true; _needsReload = false; Publish(() => IsReady = true);
            _retainedReadDraft = _retainedReadBaseline = null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested || !OwnsView(owner) || read != _readRevision) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsView(owner) && read == _readRevision) InvalidateSource("AssetType_LoadFailed"); }
        finally
        {
            if (_publicationGuard == (Action)Current) _publicationGuard = null;
            if (OwnsView(owner) && read == _readRevision) { _publishing = false; _loading = false; PublishState(); }
        }
    }
    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync()
    {
        if (!CanReload) return; IsBusy = true;
        try { await LoadAsync(true); } finally { IsBusy = false; PublishState(); }
    }
    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void AcceptReview()
    { if (!CanEdit) return; _pendingReview = false; ReloadedSavedSummary = null; PublishState(); }
    /// <summary>Refreshes live original captions while retaining every selected enum and raw parse context.</summary>
    public void RefreshDisplay()
    {
        if (_departed || !OwnsRoot) return;
        var pending = _loadRequested && _loading && !IsReady; _viewRevision++;
        if (pending) RetireRead(); _publicationGuard = null; RefreshDisplayCore();
        if (pending && !_routeRetired && !Committed) _ = LoadAsync(_baselineDraft is not null);
    }
    private void RefreshDisplayCore()
    {
        var publishing = _publishing; _publishing = true;
        var indices = (KindIndex, DimensionIndex, MetalIndex, PurityIndex);
        try
        {
            Publish(() => Title = _translator[_editing ? "AssetType_EditTitle" : "AssetType_NewTitle"]);
            Publish(() => KindNames = [.. Enum.GetValues<AssetKind>().Select(kind => _translator[$"AssetKind_{kind}"])]);
            Publish(() => DimensionNames = [_translator["AssetType_ByWeight"], _translator["AssetType_ByCount"]]);
            Publish(() => MetalNames = [.. Metals.Select(metal => _translator[$"Metal_{metal}"])]);
            Publish(() => PurityNames = [.. Purity.Common.Select(purity => purity.Label), _translator["AssetType_PurityOther"]]);
            Publish(() => KindIndex = indices.KindIndex); Publish(() => DimensionIndex = indices.DimensionIndex);
            Publish(() => MetalIndex = indices.MetalIndex); Publish(() => PurityIndex = indices.PurityIndex);
            Publish(() => HasMetal = (AssetKind)KindIndex is AssetKind.PreciousMetal or AssetKind.CoinOrBar);
            Publish(() => IsCount = DimensionIndex == (int)AssetDimension.Count);
            Publish(() => IsOtherPurity = PurityIndex >= Purity.Common.Count);
            if (_feedbackKey is { } key) Publish(() => Error = _translator[key]);
            if (_pendingReview) Publish(() => ReloadedSavedSummary = SavedSummary(_existing));
            RefreshNumericPresentation();
        }
        finally { _publishing = publishing; }
        PublishState();
    }
    Task Presentation.IThemeAware.RefreshThemeAsync() { RefreshDisplay(); return Task.CompletedTask; }
    private string SavedSummary(AssetType? type)
    {
        if (type is null) return _translator["AssetType_NewTitle"];
        string Pair(string key, string? value) => _translator[key] + ": " + (value ?? string.Empty);
        return string.Join(Environment.NewLine,
            Pair("AssetType_Name", type.Name), Pair("AssetType_Kind", _translator[$"AssetKind_{type.Kind}"]),
            Pair("AssetType_MeasuredBy", _translator[type.Dimension == AssetDimension.Count ? "AssetType_ByCount" : "AssetType_ByWeight"]),
            Pair("AssetType_Metal", _translator[$"Metal_{type.Metal}"]),
            Pair("AssetType_Purity", type.PurityPer10000 is { } purity ? Purity.Format(purity, _localization.CurrentCulture) : null),
            Pair("AssetType_UnitWeight", type.UnitWeightMg is { } weight ? (weight / 1000m).ToString("0.###", _localization.CurrentCulture) : null),
            Pair("AssetType_CountUnit", type.CountUnitName), Pair("AssetType_Divisible", _translator[type.Divisible ? "Common_Yes" : "Common_No"]),
            Pair("AssetType_PriceCurrency", type.PriceCurrencyCode), Pair("Entry_Note", type.Note));
    }

    partial void OnKindIndexChanged(int value)
    {
        if (_publishing) return;
        DraftChanged();
        var kind = (AssetKind)value;
        HasMetal = kind is AssetKind.PreciousMetal or AssetKind.CoinOrBar;
        RefreshNumericPresentation();

        // Coins and items are counted; metals are weighed. Both stay changeable until something is recorded.
        if (!DimensionLocked && _existing is null)
        {
            DimensionIndex = kind is AssetKind.CoinOrBar or AssetKind.CountableItem ? (int)AssetDimension.Count : (int)AssetDimension.Mass;
        }
    }

    partial void OnDimensionIndexChanged(int value)
    { if (_publishing) return; DraftChanged(); IsCount = value == (int)AssetDimension.Count; RefreshNumericPresentation(); }

    partial void OnPurityIndexChanged(int value)
    { if (_publishing) return; DraftChanged(); IsOtherPurity = value >= Purity.Common.Count; PurityError = null; RefreshNumericPresentation(); }

    private bool OwnsSource(AssetTypeEditorSnapshot source, long owner)
    {
        if (!OwnsView(owner) || _routeRetired || !ReferenceEquals(source, _source)) return false;
        try { source.EnsureAccessCurrent(); return true; }
        catch (Exception exception) when (exception is HoldingEventReviewChangedException or InvalidOperationException) { return false; }
    }
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!CanSave || _review is not { } review || _source is not { } source) return;
        var submitted = CaptureDraft(); var originalDraft = Fingerprint(submitted); var revision = _revision; var owner = _viewRevision;
        bool Current() => OwnsSource(source, owner) && revision == _revision && ReferenceEquals(_review, review) && originalDraft == Snapshot();
        void Accepted()
        {
            // D-642: record the actual submitted draft before Changed or a late native field callback.
            Committed = true; _baselineDraft = submitted; _snapshot = originalDraft; _needsReload = false; _pendingReview = false;
        }
        IsBusy = true; PublishState();
        try
        {
            SetFeedback(null);
            NameError = null; PurityError = null; UnitWeightError = null;
            var invalid = false;
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = _translator["AssetType_NameRequired"];
                invalid = true;
            }

            int? purity = null;
            if (HasMetal && PurityIndex < Purity.Common.Count)
            {
                purity = Purity.Common[PurityIndex].Per10000;
            }
            else if (HasMetal && !string.IsNullOrWhiteSpace(PurityText))
            {
                if (!Purity.TryParse(PurityText, _purityCulture ?? _localization.CurrentCulture, out var parsed))
                {
                    PurityError = _translator["AssetType_PurityInvalid"];
                    invalid = true;
                }

                purity = parsed;
            }

            long? unitWeight = null;
            if (IsCount && !string.IsNullOrWhiteSpace(UnitWeightText))
            {
                var probe = new AssetType { Name = Name, PriceCurrencyCode = CurrencyCode };
                if (!Quantities.TryParse(UnitWeightText, QuantityUnit.Gram, probe, _weightCulture ?? _localization.CurrentCulture, out var weight))
                {
                    UnitWeightError = _translator["AssetType_WeightInvalid"];
                    invalid = true;
                }

                unitWeight = weight;
            }


            if (!Current())
            {
                if (!OwnsView(owner)) return;
                if (!OwnsSource(source, owner)) InvalidateSource("AssetType_SourceChanged");
                return;
            }
            if (invalid) return;
            var type = _existing is { } original ? HoldingTypeReview.Copy(original)
                : new AssetType(_id) { Name = Name.Trim(), PriceCurrencyCode = CurrencyCode };
            type.Name = Name.Trim();
            type.Kind = (AssetKind)KindIndex;
            type.Dimension = (AssetDimension)DimensionIndex;
            type.Metal = HasMetal ? Metals[Math.Clamp(MetalIndex, 0, Metals.Length - 1)] : Metal.None;
            type.PurityPer10000 = purity;
            type.UnitWeightMg = IsCount ? unitWeight : null;
            type.CountUnitName = IsCount && !string.IsNullOrWhiteSpace(CountUnitName) ? CountUnitName.Trim() : null;
            type.Divisible = IsCount && Divisible;
            type.PriceCurrencyCode = CurrencyCode;
            type.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            var saved = await _holdings.SaveReviewedTypeAsync(review, type, Current, accepted: Accepted, originalSource: source);
            if (!saved) { if (OwnsView(owner)) SetFeedback("AssetType_DimensionLocked"); return; }
        }
        catch (CommittedWriteNotificationException)
        { if (!Committed) Accepted(); if (OwnsView(owner)) SetFeedback("AssetType_SavedNeedsReview"); }
        catch (HoldingEventReviewChangedException)
        { if (OwnsView(owner)) InvalidateSource("AssetType_SourceChanged"); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (OwnsView(owner))
            { _feedbackKey = null; Error = Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true); }
        }
        finally
        {
            if (Committed) _review = null;
            IsBusy = false; PublishState();
        }
        if (!Committed || !OwnsView(owner)) return;
        if (Snapshot() != originalDraft)
        {
            var notice = _translator["AssetType_SavedDraftChanged"];
            if (Error is null) SetFeedback("AssetType_SavedDraftChanged");
            else { _feedbackKey = null; Error += "\n\n" + notice; }
            return;
        }
        if (Error is not null) return;
        try
        {
            // A new accepted identity opens its original quantity form once; it never resubmits type creation.
            if (!review.Editing)
                await _interaction.NavigateAsync($"../{AppRoutes.AssetEventEditorRoute}", new Dictionary<string, object>
                { ["type"] = _id, ["kind"] = nameof(AssetEventKind.Opening) });
            else await _interaction.NavigateAsync("..");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsView(owner)) SetFeedback("AssetType_SavedNeedsReview"); }
    }

    [RelayCommand(CanExecute = nameof(CanLeave))]
    private async Task CancelAsync()
    {
        if (!CanLeave) return;
        var owner = _viewRevision; var draft = Snapshot();
        bool Current() => OwnsView(owner) && Snapshot() == draft;
        IsBusy = true; PublishState();
        try
        {
            if ((!IsDirty || await _interaction.ConfirmAsync(_translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"],
                _translator["Common_Discard"], _translator["Common_KeepEditing"])) && Current()) await _interaction.NavigateAsync("..");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsView(owner)) { _feedbackKey = null; Error = _translator["Error_Unexpected"]; } }
        finally { IsBusy = false; PublishState(); }
    }

    private string? _snapshot;
    /// <inheritdoc />
    public bool IsDirty => _snapshot is not null && Snapshot() != _snapshot;
    private sealed record Draft(string Name, int Kind, int Dimension, int Metal, int Purity, string PurityText,
        string Weight, string CountUnit, bool Divisible, string Currency, string Note, CultureInfo? PurityCulture, CultureInfo? WeightCulture);
    private Draft CaptureDraft() => new(Name, KindIndex, DimensionIndex, MetalIndex, PurityIndex, PurityText,
        UnitWeightText, CountUnitName, Divisible, CurrencyCode, Note, _purityCulture, _weightCulture);
    private static string CultureStamp(CultureInfo? culture) => culture is null ? string.Empty : Presentation.DraftFingerprint.Create(
        culture.NumberFormat.NumberDecimalSeparator, culture.NumberFormat.NumberGroupSeparator, culture.NumberFormat.PositiveSign,
        culture.NumberFormat.NegativeSign, string.Join(';', culture.NumberFormat.NumberGroupSizes));
    private static string Fingerprint(Draft draft) => Presentation.DraftFingerprint.Create(draft.Name, draft.Kind, draft.Dimension, draft.Metal,
        draft.Purity, draft.PurityText, draft.Weight, draft.CountUnit, draft.Divisible, draft.Currency, draft.Note,
        CultureStamp(draft.PurityCulture), CultureStamp(draft.WeightCulture));
    private string Snapshot() => Fingerprint(CaptureDraft());
    private void DraftChanged()
    {
        if (!_initialized || _publishing) return;
        _revision++;
        if (_loading && _retainedReadBaseline is not null) _retainedReadDraft = CaptureDraft();
        OnPropertyChanged(nameof(IsDirty));
    }
    private void MergeDraft(Draft draft, Draft baseline)
    {
        if (draft.Name != baseline.Name) Publish(() => Name = draft.Name);
        if (draft.Kind != baseline.Kind) Publish(() => KindIndex = draft.Kind);
        if (draft.Dimension != baseline.Dimension) Publish(() => DimensionIndex = draft.Dimension);
        if (draft.Metal != baseline.Metal) Publish(() => MetalIndex = draft.Metal);
        if (draft.Purity != baseline.Purity) Publish(() => PurityIndex = draft.Purity);
        if (draft.PurityText != baseline.PurityText || CultureStamp(draft.PurityCulture) != CultureStamp(baseline.PurityCulture))
        { _purityCulture = draft.PurityCulture; Publish(() => PurityText = draft.PurityText); }
        if (draft.Weight != baseline.Weight || CultureStamp(draft.WeightCulture) != CultureStamp(baseline.WeightCulture))
        { _weightCulture = draft.WeightCulture; Publish(() => UnitWeightText = draft.Weight); }
        if (draft.CountUnit != baseline.CountUnit) Publish(() => CountUnitName = draft.CountUnit);
        if (draft.Divisible != baseline.Divisible) Publish(() => Divisible = draft.Divisible);
        if (draft.Currency != baseline.Currency) Publish(() => CurrencyCode = draft.Currency);
        if (draft.Note != baseline.Note) Publish(() => Note = draft.Note);
    }
}
