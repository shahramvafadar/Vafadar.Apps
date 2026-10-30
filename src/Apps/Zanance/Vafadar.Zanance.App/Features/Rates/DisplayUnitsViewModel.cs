using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>An original defined display unit and its unchanged example.</summary>
public sealed record DisplayUnitRow(string CurrencyCode, string Text, string Example);

/// <summary>Original explicit informal units; stored money, exports and exchange rates always retain their ISO currency.</summary>
public sealed partial class DisplayUnitsViewModel(ZananceStore store, Translator translator, ILocalizationService localization, IAppInteraction interaction) : Vafadar.Maui.Mvvm.ViewModelBase
{
    private static readonly int[] Exponents = [1, 2, 3, 4, 5, 6];
    private DisplayUnitsReview? _review, _draftReview, _acceptedOrigin, _acceptedReview;
    private DisplayUnit[] _publishedUnits = [];
    private string? _originalFile, _ambientWitness, _errorKey;
    private bool _active = true, _rootRetired, _writing, _loading;
    private long _lifetime, _revision, _load;
    private Func<bool>? _rootOwner;
    private CancellationTokenSource _frame = new(), _read = new();
    private readonly SemaphoreSlim _readGate = new(1, 1);
    private (string Name, string? Currency, int Factor, long Revision)? _acceptedInput;
    private bool OwnsPage => _active && !_rootRetired && (_rootOwner?.Invoke() ?? true);
    private bool OwnsAmbient => _ambientWitness is not null && _ambientWitness == DisplayUnitPublication.CaptureCurrent();
    /// <summary>Gets the original current source cover; Refresh never resubmits a known saved unit.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets whether this exact native page can edit its complete original unit publication.</summary>
    public bool CanEdit => OwnsPage && Loading.IsReady && !IsBusy && _review is not null;
    /// <summary>Gets whether the original raw name can be explicitly submitted against its own source.</summary>
    public bool CanAdd => CanEdit && !string.IsNullOrWhiteSpace(Name) && DraftContextError is null;
    /// <summary>Gets whether explicit read-only Refresh is available on this actual original page.</summary>
    public bool CanReload => OwnsPage && !IsBusy;
    /// <summary>Explains a retained unsaved name whose original definitions were replaced.</summary>
    public string? DraftContextError => string.IsNullOrWhiteSpace(Name) ? null
        : _draftReview is null || _review is null || !_draftReview.OwnsSameVersion(_review) ? translator["Unit_SourceChanged"] : null;
    /// <summary>Gets original published rows; replacing captions never grants an old row fresh consent.</summary>
    public SnapshotCollection<DisplayUnitRow> Units { get; } = [];
    /// <summary>Gets the existing explicit known currency choices.</summary>
    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];
    /// <summary>Gets current culture captions while the original exponent index keeps its exact meaning.</summary>
    public IReadOnlyList<string> FactorNames => [.. Exponents.Select(e => Math.Pow(10, e).ToString("N0", localization.CurrentCulture))];
    /// <summary>Gets or sets the original explicitly selected currency.</summary>
    [ObservableProperty] public partial string? CurrencyCode { get; set; }
    /// <summary>Gets or sets the exact unfinished unit name, without implicit language replacement.</summary>
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the original exponent choice independently of its localized caption.</summary>
    [ObservableProperty] public partial int FactorIndex { get; set; }
    /// <summary>Gets the existing plain unit preview; it is never an exchange rate.</summary>
    [ObservableProperty] public partial string? PreviewText { get; set; }
    /// <summary>Gets current translated source/validation/known-acceptance feedback.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    /// <summary>Gets whether this original publication contains defined units.</summary>
    [ObservableProperty] public partial bool HasUnits { get; set; }
    partial void OnNameChanging(string value)
    { if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(value)) _draftReview = _review; }
    partial void OnNameChanged(string value) { ++_revision; UpdatePreview(); PublishState(); }
    partial void OnCurrencyCodeChanged(string? value) { ++_revision; UpdatePreview(); PublishState(); }
    partial void OnFactorIndexChanged(int value) { ++_revision; UpdatePreview(); PublishState(); }
    private void Feedback(string? key) { _errorKey = key; Error = key is null ? null : translator[key]; }
    private void PublishState()
    { OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CanAdd)); OnPropertyChanged(nameof(CanReload)); OnPropertyChanged(nameof(DraftContextError)); AddCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged(); }
    private void PublishBusy() { IsBusy = _writing || _loading; PublishState(); }
    private void RetireRead()
    { ++_load; var old = _read; _read = CancellationTokenSource.CreateLinkedTokenSource(_frame.Token); Loading = new(); _loading = false; old.Cancel(); old.Dispose(); PublishBusy(); }
    /// <summary>Binds only the original weak actual application root.</summary>
    internal void BindRootOwner(Func<bool> owner) { _rootOwner = owner ?? throw new ArgumentNullException(nameof(owner)); PublishState(); }
    /// <summary>Permanently retires original reads and native consent when that root is replaced.</summary>
    internal void RetireRoot() { _rootRetired = true; Deactivate(); PublishState(); }
    /// <summary>Retires pending original unit reads and commands without reactivating through a later Load call.</summary>
    public void Deactivate() => SetPresentationActive(false);
    /// <summary>Only actual native appearance/return activates this original page; display changes do not renew its source.</summary>
    public void SetPresentationActive(bool active)
    {
        localization.Changed -= OnDisplayChanged;
        if (_active != active)
        { _active = active; ++_lifetime; var old = _frame; _frame = new(); old.Cancel(); old.Dispose(); RetireRead(); }
        if (active && !_rootRetired) localization.Changed += OnDisplayChanged;
        PublishState();
    }
    private void OnDisplayChanged(object? sender, EventArgs args)
    {
        if (!OwnsPage) return;
        if (_errorKey is not null) Error = translator[_errorKey];
        OnPropertyChanged(nameof(FactorNames)); UpdatePreview(); PublishState();
        if (Loading.IsReady && _review is not null)
        {
            if (OwnsAmbient) RefreshRows();
            else _ = CoverFailureAsync(new InvalidOperationException("The original unit publication changed."), "Unit_SourceChanged");
        }
    }
    /// <summary>Publishes only a freshly validated original file and keeps unfinished names across read failure/Retry.</summary>
    public async Task LoadAsync()
    {
        if (!OwnsPage || _writing) return;
        RetireRead(); var serial = _load; var lifetime = _lifetime; var token = _read.Token; var state = Loading; var entered = false;
        _loading = true; PublishBusy();
        bool Current() => OwnsPage && lifetime == _lifetime && serial == _load && !token.IsCancellationRequested && ReferenceEquals(state, Loading);
        try
        {
            await _readGate.WaitAsync(token); entered = true; DisplayUnitsReview? next = null;
            await state.RunAsync(async () =>
            {
                next = await store.ReadDisplayUnitsAsync(token, path => { if (!Current()) return false; _originalFile ??= path; return _originalFile == path; });
                await store.ValidateDisplayUnitsAsync(next, token);
            }, () =>
            {
                if (!Current() || next is null) throw new OperationCanceledException(token);
                if (next.SerializedUnits is { } serialized) DisplayUnitPublication.Apply(serialized);
                _review = next; _publishedUnits = DisplayUnits.All.ToArray(); _ambientWitness = DisplayUnitPublication.CaptureCurrent();
                CurrencyCode ??= next.DefaultCurrencyCode;
                // A known own commit can admit its subsequent explicit readback; an unrelated source never renews the raw draft.
                var ownReceipt = _acceptedReview is not null && next.OwnsSameVersion(_acceptedReview);
                if (ownReceipt && _acceptedInput is { } input && _revision == input.Revision && Name == input.Name && CurrencyCode == input.Currency && FactorIndex == input.Factor) Name = string.Empty;
                if (_draftReview is null || ownReceipt && _acceptedOrigin is not null && _draftReview.OwnsSameVersion(_acceptedOrigin)) _draftReview = next;
                _acceptedOrigin = null; _acceptedReview = null; _acceptedInput = null;
                Feedback(null); RefreshRows(); PublishState();
            });
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) { _review = null; Feedback("Unit_LoadFailed"); } throw; }
        finally { if (entered) _readGate.Release(); if (serial == _load) { _loading = false; PublishBusy(); } }
    }
    /// <summary>Retries only an original current source read; no accepted unit command is automatically repeated.</summary>
    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync()
    { if (!CanReload) return; try { await LoadAsync(); } catch (Exception exception) when (exception is not OutOfMemoryException) { } }
    private async Task CoverFailureAsync(Exception exception, string key)
    {
        _review = null; Loading = new();
        try { await Loading.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception observed) when (ReferenceEquals(observed, exception)) { }
        Feedback(key); PublishState();
    }
    /// <summary>Offers the existing explicit toman definition without deriving it from language or region.</summary>
    [RelayCommand]
    private void UseToman()
    { if (!CanEdit) return; CurrencyCode = "IRR"; Name = translator["Unit_TomanName"]; FactorIndex = 0; }
    /// <summary>Submits only the explicit original currency/name/exponent over its own published definitions.</summary>
    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        if (!CanAdd) return;
        if (!OwnsAmbient) { await CoverFailureAsync(new InvalidOperationException("The original unit publication changed."), "Unit_SourceChanged"); return; }
        Feedback(null); var name = Name.Trim();
        var unit = new DisplayUnit(CurrencyCode ?? string.Empty, name, Exponents[Math.Clamp(FactorIndex, 0, Exponents.Length - 1)]);
        if (name.IndexOfAny(DisplayUnitPublication.Reserved) >= 0 || !unit.IsValid) { Feedback("Unit_Invalid"); return; }
        await SaveUnitsAsync([.. _publishedUnits.Where(u => u.CurrencyCode != unit.CurrencyCode), unit], null);
    }
    /// <summary>Captures only the exact original current row for native removal consent.</summary>
    [RelayCommand]
    private Task RemoveAsync(DisplayUnitRow row) => !CanEdit || !Units.Any(current => ReferenceEquals(current, row)) ? Task.CompletedTask
        : SaveUnitsAsync([.. _publishedUnits.Where(unit => unit.CurrencyCode != row.CurrencyCode)], row);
    private async Task SaveUnitsAsync(DisplayUnit[] submitted, DisplayUnitRow? remove)
    {
        if (!CanEdit || _review is not { } original) return;
        if (!OwnsAmbient) { await CoverFailureAsync(new InvalidOperationException("The original unit publication changed."), "Unit_SourceChanged"); return; }
        var revision = _revision; var lifetime = _lifetime; var token = _frame.Token;
        var input = (Name, CurrencyCode, FactorIndex, revision);
        bool NativeCurrent() => OwnsPage && lifetime == _lifetime && !token.IsCancellationRequested && ReferenceEquals(_review, original);
        bool Current() => NativeCurrent() && Loading.IsReady && revision == _revision && OwnsAmbient;
        _writing = true; PublishBusy(); Feedback(null);
        try
        {
            if (remove is not null && !await interaction.ConfirmAsync(translator["Unit_Remove"], remove.Text, translator["Common_Delete"], translator["Common_Cancel"])) return;
            var saved = await DisplayUnitPublication.SaveAsync(store, submitted, original, Current, token);
            _acceptedOrigin = original; _acceptedReview = saved.Review; _acceptedInput = remove is null ? input : null;
            if (!NativeCurrent()) return;
            if (!saved.Published) { await CoverFailureAsync(new InvalidOperationException("The accepted original units require explicit readback."), "Unit_SavedFollowUpFailed"); return; }
            _review = saved.Review; _publishedUnits = DisplayUnits.All.ToArray(); _ambientWitness = DisplayUnitPublication.CaptureCurrent();
            if (_draftReview is not null && _draftReview.OwnsSameVersion(original)) _draftReview = saved.Review;
            if (revision == _revision && remove is null) Name = string.Empty;
            if (saved.NotificationFailed) Feedback("Unit_SavedFollowUpFailed");
            _acceptedOrigin = null; _acceptedReview = null; _acceptedInput = null; RefreshRows();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (NativeCurrent()) await CoverFailureAsync(exception, "Unit_SaveFailed"); }
        finally { _writing = false; PublishBusy(); }
    }
    private void RefreshRows()
    {
        var culture = localization.CurrentCulture;
        var rows = _publishedUnits.Select(unit =>
        {
            var currency = Currencies.Get(unit.CurrencyCode); var example = currency.MinorFactor * unit.Factor * 12_500;
            var iso = Isolated(MoneyAmount.Format(example, currency, culture), currency.Code);
            return new DisplayUnitRow(unit.CurrencyCode,
                translator.Format("Unit_Definition", unit.Name, Isolated(unit.Factor.ToString("N0", culture), unit.CurrencyCode)),
                translator.Format("Unit_Example", iso, MoneyText.Format(example, unit.CurrencyCode, culture)));
        }).ToArray();
        Units.ReplaceAll(rows); HasUnits = rows.Length != 0; UpdatePreview();
    }
    private void UpdatePreview()
    { var name = Name.Trim(); PreviewText = CurrencyCode is null || name.Length == 0 ? null : translator.Format("Unit_Definition", name, Isolated(FactorNames[Math.Clamp(FactorIndex, 0, FactorNames.Count - 1)], CurrencyCode)); }
    private static string Isolated(string number, string code) => $"\u2066\u200E{number}\u00A0{code}\u200E\u2069";
}
