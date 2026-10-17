using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Rates;

namespace Vafadar.Zanance.App.Features.Rates;

/// <summary>One local asset caption, never an outgoing provider symbol or user holding quantity.</summary>
public sealed record MarketAssetChoice(Guid Id, string Caption);
/// <summary>One retained source packet selected explicitly; no amount is silently copied into a financial form.</summary>
public sealed record MarketCachedChoice(MarketQuote Quote, string Caption);
/// <summary>Display-only retained source; selecting history cannot supply a new writable reference.</summary>
public sealed record MarketHistoryChoice(Guid Id, string Caption, string Details);

/// <summary>Explicit reference lookup, offline selection and exact original financial review; no automatic provider dispatch or price update.</summary>
public sealed partial class MarketReferenceViewModel(MarketReferenceStore store, HoldingStore holdings, IMarketRuntime runtime,
    Translator text, ILocalizationService local, IDateFormatter dates, TimeProvider time) : ViewModelBase
{
    private MarketUiAccess? access;
    private IMarketRemote? remote;
    private MarketCatalog? catalog;
    private MarketValueReview? review;
    private int epoch;
    private bool active;
    private int historyOffset;
    /// <summary>Explicit original public symbols, populated only after a deliberate metadata lookup.</summary>
    public ObservableCollection<MarketSymbol> Symbols { get; } = [];
    /// <summary>Device-private retained references, readable without HTTP or sign-in.</summary>
    public ObservableCollection<MarketCachedChoice> Cached { get; } = [];
    /// <summary>Exact local asset definitions; no default asset selection or provider disclosure.</summary>
    public ObservableCollection<MarketAssetChoice> Assets { get; } = [];
    /// <summary>Bounded pages of original accepted sources, including values later changed or removed.</summary>
    public ObservableCollection<MarketHistoryChoice> History { get; } = [];
    /// <summary>Explicit original history selection, separate from writable cached references.</summary>
    [ObservableProperty] public partial MarketHistoryChoice? SavedSource { get; set; }
    /// <summary>Whether another explicit saved-history page may be requested.</summary>
    [ObservableProperty] public partial bool HasMoreHistory { get; private set; }
    /// <summary>Explicit original selected public symbol; no private asset/account name enters the request.</summary>
    [ObservableProperty] public partial MarketSymbol? Symbol { get; set; }
    /// <summary>Explicit effective reference request date, initialized before a DateField is constructed.</summary>
    [ObservableProperty] public partial DateOnly Date { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().Date);
    /// <summary>Explicit retained packet selection, never inferred from the currently selected provider revision.</summary>
    [ObservableProperty] public partial MarketCachedChoice? Selected { get; set; }
    /// <summary>Exact original local asset choice for a price; irrelevant to currency pairs.</summary>
    [ObservableProperty] public partial MarketAssetChoice? Asset { get; set; }
    /// <summary>Explicit gram/piece identity acknowledgment; fine-metal purity conversion does not infer retail premiums.</summary>
    [ObservableProperty] public partial bool ExactItemAcknowledged { get; set; }
    /// <summary>Explicit acknowledgment of aged retained reference data; false after each new selection.</summary>
    [ObservableProperty] public partial bool StaleAcknowledged { get; set; }
    /// <summary>Input remains covered until original scope/cache/assets publish together.</summary>
    [ObservableProperty] public partial bool Loaded { get; private set; }
    /// <summary>Visible failure feedback with explicit retry and manual fallback.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Actual service availability/source policy, never invented provider rights.</summary>
    [ObservableProperty] public partial string? Availability { get; private set; }
    /// <summary>Complete selected source, unit, number and observation/retrieval/retention facts.</summary>
    [ObservableProperty] public partial string? SourceText { get; private set; }
    /// <summary>Literal pending target/conversion text; changing any target retires it.</summary>
    [ObservableProperty] public partial string? ReviewText { get; private set; }
    /// <summary>Known original committed result, distinct from any notification failure.</summary>
    [ObservableProperty] public partial string? Outcome { get; private set; }
    /// <summary>Whether an explicit metadata request is possible in this selected network edition.</summary>
    public bool CanCheck => active && Loaded && !IsBusy && runtime.NetworkAvailable;
    /// <summary>Shows an explicit existing-account destination only in the selected network edition.</summary>
    public bool NetworkEnabled => runtime.NetworkAvailable;
    /// <summary>Owner-driven account navigation, separate from metadata/fetch and all native price consent.</summary>
    [RelayCommand] private Task AccountAsync() => CanCheck ? runtime.OpenAccountAsync() : Task.CompletedTask;
    /// <summary>Whether the exact source/date/symbol may be deliberately fetched.</summary>
    public bool CanFetch => CanCheck && remote is not null && catalog is not null && Symbol is not null;
    /// <summary>Whether an explicitly selected retained reference has a supported target to review.</summary>
    public bool CanReview => active && Loaded && !IsBusy && Selected is not null &&
        (Selected.Quote.Symbol.Unit == MarketUnit.Currency || Asset is not null);
    /// <summary>Only an unchanged literal review can reach native confirmation and the original transactional writer.</summary>
    public bool CanApply => active && Loaded && !IsBusy && review is not null;
    partial void OnSymbolChanged(MarketSymbol? value) { Invalidate(); Refresh(); }
    partial void OnDateChanged(DateOnly value) { Invalidate(); Refresh(); }
    partial void OnAssetChanged(MarketAssetChoice? value) { Invalidate(); Refresh(); }
    partial void OnExactItemAcknowledgedChanged(bool value) { Invalidate(); Refresh(); }
    partial void OnSelectedChanged(MarketCachedChoice? value)
    { Invalidate(); StaleAcknowledged = false; SourceText = value is null ? null : Describe(value.Quote); Refresh(); }
    private void Invalidate() { review = null; ReviewText = null; Outcome = null; }
    private void Refresh()
    { OnPropertyChanged(nameof(CanCheck)); OnPropertyChanged(nameof(CanFetch)); OnPropertyChanged(nameof(CanReview)); OnPropertyChanged(nameof(CanApply)); }
    private void Validate(int originalEpoch)
    { if (!active || originalEpoch != epoch || access is null) throw new OperationCanceledException("Original reference page retired."); access.Validate(); }
    /// <summary>Reads only the actual original file's cache/assets; never calls a provider on appearance.</summary>
    [RelayCommand] public async Task LoadAsync()
    {
        if (IsBusy) return; active = true; var originalEpoch = ++epoch; Loaded = false; IsBusy = true; Error = null;
        remote?.Dispose(); remote = null; catalog = null; access = null; Symbols.Clear(); Symbol = null; Selected = null; Asset = null;
        ExactItemAcknowledged = false; StaleAcknowledged = false; Invalidate(); Refresh();
        try
        {
            var original = runtime.Capture(); access = original;
            var cached = await runtime.ReadAsync(original, original.Cancellation); Validate(originalEpoch);
            var types = await holdings.GetTypesAsync(original.Cancellation); Validate(originalEpoch);
            var history = await store.HistoryAsync(ct: original.Cancellation); Validate(originalEpoch);
            History.Clear(); SavedSource = null; historyOffset = history.Count; PublishHistory(history); HasMoreHistory = history.Count == 20;
            Cached.Clear(); foreach (var quote in cached) Cached.Add(Choice(quote));
            Assets.Clear(); foreach (var type in types.Where(t => !t.IsArchived)) Assets.Add(new(type.Id, type.Name + " · " + type.PriceCurrencyCode));
            Loaded = true; Availability = text[runtime.NetworkAvailable ? "Market_ExplicitCheck" : "Market_OfflineEdition"];
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (active && epoch == originalEpoch) { Loaded = false; Error = text["Market_ReadFailure"]; } }
        finally { if (epoch == originalEpoch) { IsBusy = false; Refresh(); } }
    }
    /// <summary>Reads only the next finite saved-source page; never refreshes a provider or changes a financial row.</summary>
    [RelayCommand] public async Task MoreHistoryAsync()
    {
        if (!active || !Loaded || IsBusy || !HasMoreHistory || access is null) return;
        var originalEpoch = epoch; IsBusy = true; Error = null;
        try
        {
            var rows = await store.HistoryAsync(historyOffset, ct: access.Cancellation); Validate(originalEpoch);
            historyOffset += rows.Count; PublishHistory(rows); HasMoreHistory = rows.Count == 20;
        }
        catch (Exception error) when (error is not OutOfMemoryException) { if (active && epoch == originalEpoch) Error = text["Market_ReadFailure"]; }
        finally { if (epoch == originalEpoch) { IsBusy = false; Refresh(); } }
    }
    private void PublishHistory(IReadOnlyList<MarketSavedSource> rows)
    {
        foreach (var item in rows)
        {
            if (History.Any(existing => existing.Id == item.Document.Id)) continue;
            var quote = JsonSerializer.Deserialize(item.Document.QuoteJson, MarketJson.Default.MarketQuote)!;
            var mapping = JsonSerializer.Deserialize(item.Document.MappingJson, MarketMappingJson.Default.MarketAppliedMapping)!;
            var status = text[item.MatchesCurrent ? "Market_HistoryCurrent" : "Market_HistoryChanged"];
            var target = mapping.Asset?.Name ?? mapping.FromCurrency + "/" + mapping.Currency;
            var mapped = mapping.Asset is null
                ? text.Format("Market_TargetRate", mapping.FromCurrency!, MoneyText.ReferencePrice(mapping.Rate!.Value, mapping.Currency, local.CurrentCulture))
                : text.Format("Market_TargetAsset", mapping.Asset.Name, MoneyText.ReferencePrice(mapping.PricePerUnitMilli!.Value /
                    (1000m * Currencies.All.Single(currency => currency.Code == mapping.Currency).MinorFactor), mapping.Currency, local.CurrentCulture),
                    text[mapping.Asset.Dimension == AssetDimension.Mass ? "Market_Unit_Gram" : "Market_Unit_Piece"]);
            History.Add(new(item.Document.Id, target + " · " + dates.Format(mapping.Date, DateFormatStyle.Long),
                status + "\n" + target + "\n" + text.Format("Market_TargetDate", dates.Format(mapping.Date, DateFormatStyle.Long)) +
                "\n" + mapped + "\n" + Describe(quote) + "\n" + item.Document.AcceptedAt.ToString("u", local.CurrentCulture)));
        }
    }
    /// <summary>Explicit source metadata only; no public symbol or local financial value is uploaded yet.</summary>
    [RelayCommand] public async Task CheckAsync()
    {
        if (!CanCheck || access is null) return; var originalEpoch = epoch; IsBusy = true; Error = null; Refresh(); IMarketRemote? opened = null;
        try
        {
            opened = await runtime.OpenAsync(access, access.Cancellation); var availability = await opened.AvailabilityAsync(access.Cancellation); Validate(originalEpoch);
            remote?.Dispose(); remote = opened; opened = null; catalog = availability.Catalog; Symbols.Clear(); Symbol = null;
            if (catalog is null) Availability = text["Market_Unconfigured"];
            else { foreach (var symbol in catalog.Symbols) Symbols.Add(symbol); Availability = remote.Destination + "\n" + catalog.Name + "\n" + catalog.Attribution; }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (active && epoch == originalEpoch) { remote?.Dispose(); remote = null; catalog = null; Error = text["Market_NetworkFailure"]; } }
        finally { opened?.Dispose(); if (epoch == originalEpoch) { IsBusy = false; Refresh(); } }
    }
    /// <summary>Fetches the exact original public symbol/date once; it cannot write or choose a local price.</summary>
    [RelayCommand] public async Task FetchAsync()
    {
        if (!CanFetch || access is null || remote is null || catalog is null || Symbol is null) return;
        var originalEpoch = epoch; var source = catalog; var symbol = Symbol; var date = Date; var connection = remote;
        IsBusy = true; Error = null; Refresh();
        try
        {
            var quote = await connection.FetchAsync(source, new(source.Provider, source.Revision, symbol.Id, date), access.Cancellation); Validate(originalEpoch);
            // A user may have edited a picker during a delayed response; the packet remains cache, never substituted consent.
            if (catalog != source || Symbol != symbol || Date != date) throw new InvalidOperationException("Original reference selection changed.");
            var prior = Cached.FirstOrDefault(item => item.Quote.Request == quote.Request); if (prior is not null) Cached.Remove(prior);
            var chosen = Choice(quote); Cached.Insert(0, chosen); Selected = chosen;
        }
        catch (Exception error) when (error is not OutOfMemoryException) { if (active && epoch == originalEpoch) Error = text["Market_NetworkFailure"]; }
        finally { if (epoch == originalEpoch) { IsBusy = false; Refresh(); } }
    }
    /// <summary>Prepares exact original source/target mapping without changing rate/price/history.</summary>
    [RelayCommand] public async Task ReviewAsync()
    {
        if (!CanReview || Selected is null || access is null) return;
        var originalEpoch = epoch; var selected = Selected; var asset = Asset; var exact = ExactItemAcknowledged; IsBusy = true; Error = null; Invalidate(); Refresh();
        try
        {
            var original = selected.Quote.Symbol.Unit == MarketUnit.Currency
                ? await store.ReviewRateAsync(selected.Quote, access.Cancellation)
                : await store.ReviewPriceAsync(selected.Quote, asset!.Id, exact, access.Cancellation);
            Validate(originalEpoch);
            if (Selected != selected || Asset != asset || ExactItemAcknowledged != exact || original.File != access.File)
                throw new InvalidOperationException("Original reference mapping changed.");
            review = original; ReviewText = Describe(original.Quote) + "\n\n" + text.Format("Market_TargetDate", dates.Format(original.Mapping.Date, DateFormatStyle.Long)) + "\n" +
                (original.Mapping.Target == MarketReferenceTarget.ExchangeRate
                    ? text.Format("Market_TargetRate", original.Mapping.FromCurrency!, MoneyText.ReferencePrice(original.Mapping.Rate!.Value, original.Mapping.Currency, local.CurrentCulture))
                    : text.Format("Market_TargetAsset", original.Mapping.Asset!.Name, MoneyText.ReferencePrice(original.Mapping.PricePerUnitMilli!.Value /
                        (1000m * Currencies.All.Single(currency => currency.Code == original.Mapping.Currency).MinorFactor), original.Mapping.Currency, local.CurrentCulture),
                        text[original.Mapping.Asset.Dimension == AssetDimension.Mass ? "Market_Unit_Gram" : "Market_Unit_Piece"])) +
                (original.Mapping.Asset?.PurityPer10000 is { } purity ? "\n" + text.Format("Market_ReviewedPurity", (purity / 100m).ToString("G29", local.CurrentCulture)) : "") +
                "\n" + text[original.ReplacesExisting ? "Market_Replaces" : "Market_NewDatedValue"] + "\n" + text["Market_OnlyReference"];
        }
        catch (Exception error) when (error is not OutOfMemoryException) { if (active && epoch == originalEpoch) { Invalidate(); Error = text["Market_ReviewFailure"]; } }
        finally { if (epoch == originalEpoch) { IsBusy = false; Refresh(); } }
    }
    /// <summary>Native literal confirmation followed by the exact original writer; no provider/cache/live picker substitution.</summary>
    [RelayCommand] public async Task ApplyAsync()
    {
        if (!CanApply || review is null || ReviewText is null || access is null) return;
        var originalEpoch = epoch; var original = review; var terms = ReviewText; var stale = StaleAcknowledged; var native = access;
        var committed = false;
        IsBusy = true; Error = null; Refresh();
        try
        {
            Validate(originalEpoch);
            if (MarketReference.IsStale(original.Quote, time.GetUtcNow()) && !stale) { Error = text["Market_StaleRequired"]; return; }
            if (!await runtime.ConfirmAsync(text["Market_Title"], terms, text["Market_Apply"], text["Common_Cancel"])) return;
            Validate(originalEpoch);
            if (review != original || ReviewText != terms || StaleAcknowledged != stale) throw new InvalidOperationException("Original reference consent changed.");
            await store.ApplyAsync(original, stale, native.Validate, native.Cancellation); committed = true; Validate(originalEpoch); Invalidate(); Outcome = text["Market_Applied"];
            var history = await store.HistoryAsync(ct: native.Cancellation); Validate(originalEpoch);
            History.Clear(); SavedSource = null; historyOffset = history.Count; PublishHistory(history); HasMoreHistory = history.Count == 20;
        }
        catch (CommittedWriteNotificationException<MarketReferenceReceipt>)
        { if (active && epoch == originalEpoch) { Invalidate(); Outcome = text["Market_AppliedRefreshFailure"]; } }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (active && epoch == originalEpoch) { Invalidate(); if (committed) Outcome = text["Market_AppliedRefreshFailure"]; else Error = text["Market_ReviewFailure"]; } }
        finally { if (epoch == originalEpoch) { IsBusy = false; Refresh(); } }
    }
    private MarketCachedChoice Choice(MarketQuote quote) => new(quote, quote.Symbol.Label + " · " + dates.Format(quote.Observation.Date, DateFormatStyle.Long));
    private string Describe(MarketQuote quote) => text.Format("Market_SourceDetails", quote.Catalog.Name, quote.Symbol.Label,
        MoneyText.ReferencePrice(quote.Observation.Value, quote.Symbol.Currency, local.CurrentCulture), text["Market_Unit_" + quote.Symbol.Unit],
        dates.Format(quote.Observation.Date, DateFormatStyle.Long), quote.Observation.ObservedAt.ToString("u", local.CurrentCulture),
        quote.RetrievedAt.ToString("u", local.CurrentCulture), quote.FreshUntil.ToString("u", local.CurrentCulture),
        quote.RetainUntil.ToString("u", local.CurrentCulture), quote.Catalog.SourceUrl, quote.Catalog.LicenceUrl, quote.Catalog.Attribution) +
        "\n" + text.Format("Market_SourceIdentity", quote.Catalog.Provider, quote.Catalog.Revision, quote.Observation.QuoteId);
    /// <summary>Retires this original form without erasing cache/history, canceling accepted writes or creating replacement consent.</summary>
    public void Retire() { active = false; epoch++; remote?.Dispose(); remote = null; access = null; Loaded = false; IsBusy = false; Invalidate(); Refresh(); }
}
