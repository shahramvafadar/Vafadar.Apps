using Vafadar.Zanance.App.Presentation;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Core.Text;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>A stored rate for the list.</summary>
public sealed record RateRow(Guid Id, string Text, string DateText, bool IsEstimate)
{
    /// <summary>Gets what a screen reader says for the row: the rate and its date.</summary>
    public string Description => $"{Text}, {DateText}";
}

/// <summary>
/// Manual exchange rates (FX-02): 1 unit of one currency in another on a date, actual or estimated. Rates only value
/// totals in the report currency; recorded amounts, budgets and history are never converted (FX-04..06).
/// </summary>
public sealed partial class RatesViewModel(ZananceStore store, Translator translator, IDateFormatter dates,
    ILocalizationService localization, TimeProvider time, Interaction.IAppInteraction interaction) : ViewModelBase
{
    private ManualRatesReview? _review, _draftReview;
    private CultureInfo? _rateCulture;
    private string? _rateFrom, _rateTo, _errorKey, _rateErrorKey, _pairErrorKey;
    private long _revision, _lifetime, _load;
    private bool _active = true, _writing, _loading, _rootRetired;
    private Func<bool>? _rootOwner;
    private CancellationTokenSource _frame = new(), _read = new();
    private readonly SemaphoreSlim _readGate = new(1, 1);
    private string? _originalFile;
    private bool OwnsPage => _active && !_rootRetired && (_rootOwner?.Invoke() ?? true);
    /// <summary>Gets the original publication cover; failed reads require explicit read-only Refresh.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Binds only the actual original weak native root.</summary>
    internal void BindRootOwner(Func<bool> owner) { _rootOwner = owner ?? throw new ArgumentNullException(nameof(owner)); PublishState(); }
    /// <summary>Permanently retires reads and original commands of a replaced application root.</summary>
    internal void RetireRoot() { _rootRetired = true; SetPresentationActive(false); PublishState(); }
    private void RetireRead()
    {
        Interlocked.Increment(ref _load); var old = _read; _read = CancellationTokenSource.CreateLinkedTokenSource(_frame.Token);
        Loading = new(); _loading = false; old.Cancel(); old.Dispose(); PublishBusy();
    }
    /// <summary>Original stored-rate rows; rendering never changes their immutable private writer review.</summary>
    public SnapshotCollection<RateRow> Rates { get; } = [];
    /// <summary>Explicit known ISO currency choices; display-unit factors never multiply this plain ratio.</summary>
    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];
    /// <summary>Gets whether the current original native rate form is editable.</summary>
    public bool CanEdit => OwnsPage && Loading.IsReady && !IsBusy && _review is not null && !CaptionOnly;
    /// <summary>Gets whether a retired or failed read can be explicitly refreshed without a write.</summary>
    public bool CanReload => OwnsPage && !IsBusy && !CaptionOnly;
    /// <summary>Gets whether an explicit raw rate can be reviewed by its original writer.</summary>
    public bool CanAdd => CanEdit && _draftReview is not null && !string.IsNullOrWhiteSpace(RateText);
    /// <summary>Uses the actual original decimal parser for the visible plain-ratio example.</summary>
    public string RateExample => 0.92m.ToString("0.######", _rateCulture ?? localization.CurrentCulture);
    /// <summary>Explains a retained original numeric culture without grouping a plain decimal ratio.</summary>
    public string? RateFormatNote => !string.IsNullOrWhiteSpace(RateText) && _rateCulture is { } original &&
        (original.NumberFormat.NumberDecimalSeparator != localization.CurrentCulture.NumberFormat.NumberDecimalSeparator ||
         original.NumberFormat.NumberGroupSeparator != localization.CurrentCulture.NumberFormat.NumberGroupSeparator)
        ? translator.Format("Numeric_OriginalInputFormat", Isolated(1.25m.ToString("0.######", original))) : null;
    /// <summary>Refuses reinterpretation of a raw ratio under another pair or another actual financial file.</summary>
    public string? RateContextError => string.IsNullOrWhiteSpace(RateText) ? null
        : _draftReview is null || _review is null || !_draftReview.OwnsSameVersion(_review)
            ? _errorKey == "Rates_SourceChanged" ? null : translator["Rates_SourceChanged"]
        : _rateFrom != FromCurrency || _rateTo != ToCurrency
            ? translator.Format("Rates_OriginalPairChanged", Isolated(_rateFrom ?? ""), Isolated(_rateTo ?? "")) : null;

    [ObservableProperty] public partial string? FromCurrency { get; set; }
    [ObservableProperty] public partial string? ToCurrency { get; set; }
    [ObservableProperty] public partial string RateText { get; set; } = string.Empty;
    [ObservableProperty] public partial DateOnly Date { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().Date);
    [ObservableProperty] public partial bool IsEstimate { get; set; }
    /// <summary>Retains the actual feedback category so an open native form follows language changes without another command.</summary>
    public string? Error => _errorKey is null ? null : translator[_errorKey];
    /// <summary>Shows numeric/meaning validation beside the raw rate instead of its action button.</summary>
    public string? RateError => _rateErrorKey is null ? null : translator[_rateErrorKey];
    /// <summary>Shows invalid pair selection beside the two original currency choices.</summary>
    public string? PairError => _pairErrorKey is null ? null : translator[_pairErrorKey];
    [ObservableProperty] public partial string? MissingText { get; set; }
    [ObservableProperty] public partial bool HasRates { get; set; }

    partial void OnRateTextChanging(string value)
    {
        // D-437: neither partial typing nor a display refresh can renew an older pair/file/culture.
        if (string.IsNullOrWhiteSpace(RateText) || string.IsNullOrWhiteSpace(value))
        {
            _rateCulture = CultureInfo.ReadOnly((CultureInfo)localization.CurrentCulture.Clone());
            _rateFrom = FromCurrency; _rateTo = ToCurrency; _draftReview = _review;
        }
    }
    partial void OnRateTextChanged(string value) { Edited(); SetRateError(null); RefreshInput(); }
    partial void OnFromCurrencyChanged(string? value) { Edited(); SetPairError(null); RefreshInput(); }
    partial void OnToCurrencyChanged(string? value) { Edited(); SetPairError(null); RefreshInput(); }
    partial void OnDateChanged(DateOnly value) => Edited();
    partial void OnIsEstimateChanged(bool value) => Edited();
    private void SetFeedback(string? key) { _errorKey = key; OnPropertyChanged(nameof(Error)); RefreshInput(); }
    private void SetRateError(string? key) { _rateErrorKey = key; OnPropertyChanged(nameof(RateError)); }
    private void SetPairError(string? key) { _pairErrorKey = key; OnPropertyChanged(nameof(PairError)); }
    private void Edited() => Interlocked.Increment(ref _revision);
    private void PublishState()
    { OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CanAdd)); OnPropertyChanged(nameof(CanReload)); AddCommand.NotifyCanExecuteChanged(); ReloadCommand.NotifyCanExecuteChanged(); }
    private void PublishBusy() { IsBusy = _writing || _loading; PublishState(); }
    private void RefreshInput()
    {
        OnPropertyChanged(nameof(RateExample)); OnPropertyChanged(nameof(RateFormatNote)); OnPropertyChanged(nameof(RateContextError)); PublishState();
    }
    private static string Isolated(string value) => $"\u2066\u200E{value}\u200E\u2069";

    private bool CaptionOnly
    {
        get
        {
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
            return _captionOnly;
#else
            return false;
#endif
        }
    }
#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
    private bool _captionOnly;
    /// <summary>Displays only a retained source-verified result caption; input is disabled and no original acceptance frame is reconstructed.</summary>
    internal void ShowRetainedFeedback(string key)
    {
        if (key is not ("Rates_SourceChanged" or "Rates_ChangeFailed" or "Rates_SavedFollowUpFailed")) throw new ArgumentException("Explicit retained rate caption required.", nameof(key));
        _captionOnly = true; SetFeedback(key);
    }
#endif
    /// <summary>Opens separate explicit reference review; it never copies a quote into this unsaved manual draft.</summary>
    [RelayCommand] private async Task OpenReferencesAsync()
    {
        if (!CanEdit || _review is not { } original) return;
        var lifetime = _lifetime; var token = _frame.Token;
        bool Current() => OwnsPage && lifetime == _lifetime && !token.IsCancellationRequested && ReferenceEquals(_review, original) && Loading.IsReady;
        _writing = true; PublishBusy();
        try
        {
            await store.ValidateManualRatesAsync(original, token);
            if (Current()) await interaction.NavigateAsync(AppRoutes.MarketReferenceRoute);
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (Current()) await CoverFailureAsync(exception); }
        finally { _writing = false; PublishBusy(); }
    }
    private async Task CoverFailureAsync(Exception exception)
    {
        _review = null; Loading = new();
        try { await Loading.RunAsync(() => Task.FromException(exception), () => { }); }
        catch (Exception observed) when (ReferenceEquals(observed, exception)) { }
        SetFeedback(exception is ManualRateReviewChangedException ? "Rates_SourceChanged" : "Rates_ChangeFailed");
    }

    /// <summary>Loads only this native owner; display metadata refresh never renews a nonempty original raw draft.</summary>
    public async Task LoadAsync()
    {
        if (!OwnsPage) return;
        RetireRead(); var lifetime = _lifetime; var serial = _load; var token = _read.Token; var state = Loading;
        var entered = false; _loading = true; PublishBusy();
        bool Current() => OwnsPage && lifetime == _lifetime && serial == _load && !token.IsCancellationRequested && ReferenceEquals(state, Loading);
        try
        {
            await _readGate.WaitAsync(token); entered = true;
            ManualRatesReview? review = null; string[] missing = []; DateOnly today = default;
            await state.RunAsync(async () =>
            {
                today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                review = await store.ReadOriginalManualRatesAsync(path =>
                { if (!Current()) return false; _originalFile ??= path; return _originalFile == path; }, token);
                token.ThrowIfCancellationRequested();
                var table = new RateTable(review.Rates);
                missing = review.AccountCurrencies.Where(code => !table.TryGetRate(code, review.ReportCurrencyCode, today, out _, out _)).ToArray();
                await store.ValidateManualRatesAsync(review, token);
                if (today != DateOnly.FromDateTime(time.GetLocalNow().DateTime)) throw new ManualRateReviewChangedException();
            }, () =>
            {
                if (!Current()) throw new OperationCanceledException(token);
                _review = review ?? throw new ManualRateReviewChangedException();
                ToCurrency ??= review.ReportCurrencyCode;
                if (Date == default) Date = today;
                FromCurrency ??= missing.FirstOrDefault(code => code != ToCurrency)
                    ?? review.AccountCurrencies.FirstOrDefault(code => code != ToCurrency) ?? (ToCurrency == "USD" ? "EUR" : "USD");
                Render(review, missing); RefreshInput();
            });
        }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (Current()) { _review = null; SetFeedback(exception is ManualRateReviewChangedException ? "Rates_SourceChanged" : "Rates_ChangeFailed"); }
            throw;
        }
        finally { if (entered) _readGate.Release(); if (serial == _load) { _loading = false; PublishBusy(); } }
    }
    /// <summary>Retries only an explicit original list read; it never resubmits a known rate change.</summary>
    [RelayCommand(CanExecute = nameof(CanReload))]
    private async Task ReloadAsync()
    {
        if (!CanReload) return; SetFeedback(null);
        try { await LoadAsync(); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { /* Load retains translated failure and raw draft. */ }
    }

    private void Render(ManualRatesReview review, string[]? missing = null)
    {
        var culture = localization.CurrentCulture;
        // D-437: a valid small ratio must never be displayed as zero, or a rounded value be used for delete consent.
        var rows = review.Rates.Select(rate => new RateRow(rate.Id,
            Isolated(string.Create(culture, $"1 {rate.FromCurrencyCode} = {rate.Rate:0.############################} {rate.ToCurrencyCode}")),
            dates.Format(rate.Date, DateFormatStyle.Long), rate.IsEstimate)).ToArray();
        Rates.ReplaceAll(rows); HasRates = Rates.Count != 0;
        var table = new RateTable(review.Rates); var today = DateOnly.FromDateTime(time.GetLocalNow().Date);
        missing ??= review.AccountCurrencies.Where(code => !table.TryGetRate(code, review.ReportCurrencyCode, today, out _, out _)).ToArray();
        MissingText = missing.Length > 0 ? translator.Format("Rates_Missing", string.Join(", ", missing), review.ReportCurrencyCode) : null;
    }
    private void OnDisplayChanged(object? sender, EventArgs args)
    {
        if (OwnsPage && Loading.IsReady && _review is { } review) Render(review);
        OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(RateError)); OnPropertyChanged(nameof(PairError)); RefreshInput();
    }
    /// <summary>Owns display notifications only while the actual rate page exists; departure retires pending native work.</summary>
    public void SetPresentationActive(bool active)
    {
        localization.Changed -= OnDisplayChanged;
        if (_active != active)
        {
            _active = active; Interlocked.Increment(ref _lifetime); Edited();
            var old = _frame; _frame = new(); old.Cancel(); old.Dispose(); RetireRead();
        }
        if (active) localization.Changed += OnDisplayChanged;
        RefreshInput();
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        if (!CanAdd) return;
        SetFeedback(null); SetRateError(null); SetPairError(null);
        if (FromCurrency is null || ToCurrency is null || FromCurrency == ToCurrency) SetPairError("Rates_SameCurrency");
        var original = _draftReview; var culture = _rateCulture ?? localization.CurrentCulture;
        var text = Digits.ToAscii(RateText.Trim()).Replace(culture.NumberFormat.NumberDecimalSeparator, ".", StringComparison.Ordinal).Replace('\u066B', '.');
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
            if (_rateErrorKey is null) SetRateError("Rates_Invalid");
        if (original is null || PairError is not null || RateError is not null || RateContextError is not null) return;
        var draft = new ExchangeRate { FromCurrencyCode = FromCurrency!, ToCurrencyCode = ToCurrency!, Rate = rate, Date = Date, IsEstimate = IsEstimate };
        var revision = Interlocked.Read(ref _revision); var lifetime = _lifetime; var token = _frame.Token; var publication = _review;
        bool Current() => OwnsPage && !token.IsCancellationRequested && Loading.IsReady && lifetime == _lifetime && revision == Interlocked.Read(ref _revision) && ReferenceEquals(_draftReview, original) && ReferenceEquals(_review, publication);
        _writing = true; PublishBusy(); var accepted = false; var followUpFailed = false;
        try
        {
            try { _ = await store.SaveManualRateAsync(original, draft, Current, token); accepted = true; }
            catch (Vafadar.Zanance.Data.Commerce.CommittedWriteNotificationException<ManualRateReceipt>) { accepted = true; followUpFailed = true; }
            if (!OwnsPage || lifetime != _lifetime) return;
            // Retire only this known accepted raw value before fallible list readback; a new explicit entry remains possible.
            if (revision == Interlocked.Read(ref _revision)) RateText = string.Empty;
            await LoadAsync();
            if (followUpFailed) SetFeedback("Rates_SavedFollowUpFailed");
        }
        catch (ManualRateReviewChangedException exception) { if (OwnsPage && lifetime == _lifetime) await CoverFailureAsync(exception); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsPage && lifetime == _lifetime) SetFeedback(accepted ? "Rates_SavedFollowUpFailed" : "Rates_ChangeFailed"); }
        finally { _writing = false; PublishBusy(); }
    }

    [RelayCommand]
    private async Task DeleteAsync(RateRow row)
    {
        if (!CanEdit || _review is not { } original || !Rates.Any(current => ReferenceEquals(current, row))) return;
        var revision = Interlocked.Read(ref _revision); var lifetime = _lifetime; var token = _frame.Token; var publication = _review;
        bool Current() => OwnsPage && !token.IsCancellationRequested && Loading.IsReady && lifetime == _lifetime && revision == Interlocked.Read(ref _revision) && ReferenceEquals(_review, original);
        _writing = true; PublishBusy(); SetFeedback(null); var accepted = false; var followUpFailed = false;
        try
        {
            if (!await interaction.ConfirmAsync(translator["Rates_Delete"], row.Text, translator["Common_Delete"], translator["Common_Cancel"]) || !Current()) return;
            try { _ = await store.DeleteManualRateAsync(original, row.Id, Current, token); accepted = true; }
            catch (Vafadar.Zanance.Data.Commerce.CommittedWriteNotificationException<ManualRateReceipt>) { accepted = true; followUpFailed = true; }
            if (!OwnsPage || lifetime != _lifetime) return;
            await LoadAsync(); if (followUpFailed) SetFeedback("Rates_SavedFollowUpFailed");
        }
        catch (ManualRateReviewChangedException exception) { if (OwnsPage && lifetime == _lifetime) await CoverFailureAsync(exception); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (OwnsPage && lifetime == _lifetime) SetFeedback(accepted ? "Rates_SavedFollowUpFailed" : "Rates_ChangeFailed"); }
        finally { _writing = false; PublishBusy(); }
    }
}
