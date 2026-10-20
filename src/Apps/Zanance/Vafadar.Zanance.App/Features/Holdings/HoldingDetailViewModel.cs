using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>One change of a holding in the history.</summary>
public sealed partial record HoldingEventRow(Guid Id, string Title, string DateText, string QuantityText, string? Detail, int QuantitySign)
{
    public string Description => string.Join(", ", new[] { Title, DateText, QuantityText, Detail }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>One dated price of the type.</summary>
public sealed record ValuationRow(Guid Id, string PriceText, string DateText, string? SourceText)
{
    public string Description => string.Join(", ", new[] { PriceText, DateText, SourceText }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>
/// One asset type (ZEX-S0407, design §7.5): the quantity in total and per location, the value from the latest price or
/// "value unknown", the average cost and the unrealised result (both labelled as estimates), the history of events with
/// edit and delete (Undo), and the prices. Buying, selling, moving, adding and removing are Advanced; a correction is
/// also offered in Simple.
/// </summary>
public sealed partial class HoldingDetailViewModel(
    HoldingStore holdings,
    HoldingText text,
    Translator translator,
    ILocalizationService localization,
    TimeProvider time,
    IAppInteraction interaction) : ViewModelBase, Presentation.IThemeAware
{
    private Guid _id;
    private int _revision;
    private CancellationTokenSource _lifetime = new();
    private HoldingDetailSnapshot? _original, _displayed;

    /// <summary>Gets the cover for one complete original detail publication and explicit Retry.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    public SnapshotCollection<string> PerLocation { get; } = [];

    public SnapshotCollection<HoldingEventRow> Events { get; } = [];

    public SnapshotCollection<ValuationRow> Valuations { get; } = [];

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial Symbol Icon { get; set; } = Symbol.Diamond;

    [ObservableProperty]
    public partial string? QuantityText { get; set; }

    [ObservableProperty]
    public partial string? DerivedText { get; set; }

    /// <summary>Gets the purity and the fine-metal amount, e.g. "750/1000 · ≈ 15.000 g fine gold".</summary>
    [ObservableProperty]
    public partial string? PurityText { get; set; }

    [ObservableProperty]
    public partial string? ValueText { get; set; }

    [ObservableProperty]
    public partial bool ValueUnknown { get; set; }

    [ObservableProperty]
    public partial string? AverageCostText { get; set; }

    [ObservableProperty]
    public partial string? UnrealisedText { get; set; }

    [ObservableProperty]
    public partial string? RealisedText { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial bool HasStock { get; set; }

    /// <summary>Owns the original manual price form and its accepted outcome.</summary>
    public HoldingPriceEditor PriceEditor { get; private set; } = new(holdings, text, translator, localization, time, interaction);

    /// <summary>Owns the actual history row review and file-bound short Undo.</summary>
    public HoldingHistoryEditor HistoryEditor { get; private set; } = new(holdings, text, translator, interaction);

    /// <summary>Owns original archive confirmation and its accepted result.</summary>
    public HoldingArchiveEditor ArchiveEditor { get; private set; } = new(holdings, translator, interaction);

    /// <summary>Gets the original archive action with its actual source/confirmation enablement.</summary>
    public IAsyncRelayCommand ArchiveCommand => ArchiveEditor.ArchiveCommand;

    /// <summary>Gets the original history command with its actual source/confirmation enablement.</summary>
    public IAsyncRelayCommand<Guid> OpenEventCommand => HistoryEditor.OpenCommand;

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            if (_id != id)
            {
                Retire(); _original = null;
                if (_id != Guid.Empty)
                {
                    // D-419: a different routed identity owns different drafts/outcomes, never the old short Undo.
                    PriceEditor = new(holdings, text, translator, localization, time, interaction);
                    HistoryEditor = new(holdings, text, translator, interaction);
                    ArchiveEditor = new(holdings, translator, interaction);
                    OnPropertyChanged(nameof(PriceEditor)); OnPropertyChanged(nameof(HistoryEditor)); OnPropertyChanged(nameof(ArchiveEditor));
                    OnPropertyChanged(nameof(ArchiveCommand)); OnPropertyChanged(nameof(OpenEventCommand));
                }
                _id = id;
            }
        }
    }

    /// <summary>Rebinds only native semantic colors; a theme change never rereads or retires a raw price draft.</summary>
    Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        if (Loading.IsReady) Events.ReplaceAll(Events.ToArray());
        return Task.CompletedTask;
    }

    /// <summary>Retires pending reads and native targets, preserving the separate raw price and short Undo receipt.</summary>
    public void Retire()
    {
        _revision++; _displayed = null;
        var old = _lifetime; _lifetime = new(); Loading = new();
        PriceEditor.Leave(); HistoryEditor.Retire(); ArchiveEditor.Retire(); old.Cancel(); old.Dispose();
    }

    /// <summary>Reads/prepares one original detail off UI, then publishes every visible field under its original cover.</summary>
    public async Task LoadAsync()
    {
        var revision = _revision; var loading = Loading; var token = _lifetime.Token; var id = _id;
        var priceOwner = PriceEditor; var historyOwner = HistoryEditor; var archiveOwner = ArchiveEditor;
        priceOwner.ViewReady = () => Loading.IsReady && !NotFound && _id == id && ReferenceEquals(PriceEditor, priceOwner);
        historyOwner.ViewReady = () => Loading.IsReady && !NotFound && _id == id && ReferenceEquals(HistoryEditor, historyOwner);
        archiveOwner.ViewReady = () => Loading.IsReady && !NotFound && _id == id && ReferenceEquals(ArchiveEditor, archiveOwner);
        void Availability(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(SnapshotLoadState.IsReady)) return;
            PriceEditor.NotifyViewReady(); HistoryEditor.NotifyViewReady(); ArchiveEditor.NotifyViewReady();
        }
        loading.PropertyChanged += Availability;
        Prepared? prepared = null;
        void Current()
        {
            token.ThrowIfCancellationRequested();
            if (_revision != revision || !ReferenceEquals(loading, Loading) || id != _id)
                throw new OperationCanceledException("Original holding detail retired.", token);
        }
        try
        {
            await loading.RunAsync(async () =>
            {
                Current(); _displayed = null;
                if (!PriceEditor.ReadingExplicitRefresh) PriceEditor.Leave();
                HistoryEditor.Retire(); ArchiveEditor.Retire();
                // A known commit can finish after departure; its following read cannot activate a retired view.
                PriceEditor.ReloadView = async () => { Current(); await LoadAsync(); Current(); };
                HistoryEditor.RefreshView = async () =>
                {
                    Current(); await priceOwner.RefreshCommand.ExecuteAsync(null); Current();
                    return Loading.IsReady && !NotFound && priceOwner.CanSubmit ? priceOwner.DisplayedReview : null;
                };
                ArchiveEditor.RefreshView = async () =>
                {
                    Current(); await priceOwner.RefreshCommand.ExecuteAsync(null); Current();
                    return Loading.IsReady && !NotFound && !priceOwner.NeedsRefresh ? priceOwner.DisplayedReview : null;
                };
                HoldingDetailSnapshot? read = null;
                PriceEditor.ReadOriginal = async requested =>
                {
                    Current(); read = await holdings.ReadDetailSnapshotAsync(requested, token); Current();
                    if (_original is { } original && original.DatabasePath != read.DatabasePath)
                        throw new HoldingEventReviewChangedException("Original detail cannot follow another profile.");
                    await holdings.ValidateDetailSnapshotAsync(read, token); Current();
                    return read.Review ?? throw new MissingOriginalDetailException();
                };
                HoldingPriceInputs? inputs = null;
                try { inputs = await PriceEditor.LoadAsync(id); }
                catch (MissingOriginalDetailException) when (read is { Review: null }) { }
                var source = read ?? _original ?? throw new InvalidOperationException("Original detail source is missing.");
                if (source.TypeId != id || !ReferenceEquals(source.Review, PriceEditor.DisplayedReview))
                {
                    if (source.Review is not null) throw new InvalidOperationException("Original detail and price draft differ.");
                }
                // A retained raw price keeps its original displayed facts, but may never follow another opened file.
                await holdings.ValidateDetailAccessAsync(source, token); Current(); source.EnsureCurrent();
                var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                prepared = inputs is null ? Prepared.Missing(source) : await Task.Run(() => Prepare(source, inputs, day), token);
                if (read is not null) await holdings.ValidateDetailSnapshotAsync(source, token);
                Current(); source.EnsureCurrent();
            }, () =>
            {
                var result = prepared ?? throw new InvalidOperationException("Complete original detail is missing.");
                void Publish(Action action) { Current(); result.Source.EnsureCurrent(); action(); Current(); result.Source.EnsureCurrent(); }
                Publish(() => Name = result.Name); Publish(() => Icon = result.Icon);
                Publish(() => QuantityText = result.Quantity); Publish(() => DerivedText = result.Derived);
                Publish(() => HasStock = result.HasStock); Publish(() => PurityText = result.Purity);
                Publish(() => PerLocation.ReplaceAll(result.Places)); Publish(() => ValueUnknown = result.Unknown);
                Publish(() => ValueText = result.Value); Publish(() => AverageCostText = result.Average);
                Publish(() => UnrealisedText = result.Unrealised); Publish(() => RealisedText = result.Realised);
                Publish(() => Events.ReplaceAll(result.Events)); Publish(() => Valuations.ReplaceAll(result.Prices));
                Publish(() => IsAdvanced = result.Advanced); Publish(() => NotFound = result.Source.Review is null);
                Publish(() => HistoryEditor.PublishDisplayed(result.Source.Review));
                Publish(() => ArchiveEditor.PublishDisplayed(result.Source.Review));
                _original = _displayed = result.Source;
            });
        }
        catch (Exception ex) when (token.IsCancellationRequested && ex is not OutOfMemoryException)
        { System.Diagnostics.Debug.WriteLine($"Retired original holding detail: {ex}"); }
        catch
        {
            PriceEditor.Retire(); HistoryEditor.Retire(); ArchiveEditor.Retire(); throw;
        }
        finally { loading.PropertyChanged -= Availability; }
    }

    /// <summary>Prepares detached original financial text before any native property or collection is changed.</summary>
    private Prepared Prepare(HoldingDetailSnapshot source, HoldingPriceInputs inputs, DateOnly today)
    {
        var type = inputs.Type;

        var events = inputs.Events;
        var valuations = inputs.Valuations;
        var locations = inputs.Locations.ToDictionary(l => l.Id, l => l.Name);
        var value = AssetValuationService.Values([type], events, valuations, today).FirstOrDefault();
        var quantity = value?.Quantity ?? 0;

        var name = type.Name;
        var icon = HoldingText.Icon(type);
        var quantityText = text.Quantity(quantity, type);
        var derivedText = text.DerivedWeight(quantity, type);
        var hasStock = quantity > 0;
        var fine = Quantities.FineMassMg(quantity, type) is { } mg && type.Metal != Metal.None && quantity > 0
            ? text.FineMetal(new FineMetal(type.Metal, mg, 1, 0), withCount: false)
            : null;
        var purityText = string.Join(" · ", new[] { text.Purity(type), fine }.Where(s => s is not null));
        purityText = purityText.Length == 0 ? null : purityText;

        var perLocation = new List<string>();
        foreach (var position in HoldingsLedger.Positions(events, today).Where(p => p.Quantity > 0))
        {
            perLocation.Add(translator.Format("AssetEvent_At", text.Quantity(position.Quantity, type), locations.GetValueOrDefault(position.LocationId, "?")));
        }

        // A value needs a price; without one it is "unknown", never zero (ZEX-AS06).
        var valueUnknown = quantity > 0 && value?.Value is null;
        var valueText = value?.Value is { } known
            ? translator.Format(value.FromPurchase ? "Holding_ValuePurchase" : "Holding_Value", text.Money(known, type.PriceCurrencyCode), text.Date(value.PriceDate!.Value))
            : null;

        // Average cost and results are estimates from the recorded prices (ZEX-AS09).
        var basis = HoldingsLedger.Basis(events, type.Id);
        var averageCostText = basis.PerUnitMilli is { } perUnit && basis.Quantity > 0 ? translator.Format("Holding_AverageCost", text.Price(perUnit, type)) : null;
        var unrealisedText = basis.Basis is { } cost && value?.Value is { } current && basis.Quantity > 0
            ? translator.Format("Holding_Unrealised", text.Money(current - cost, type.PriceCurrencyCode, showPlus: true))
            : null;
        var realised = basis.Sales.Where(s => s.Result is not null).ToList();
        var realisedText = realised.Count > 0 ? translator.Format("Holding_Realised", text.Money(realised.Sum(s => s.Result!.Value), type.PriceCurrencyCode, showPlus: true)) : null;

        var eventRows = new List<HoldingEventRow>();
        foreach (var assetEvent in events.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt))
        {
            var place = locations.GetValueOrDefault(assetEvent.LocationId, "?");
            var detail = assetEvent.Kind == AssetEventKind.LocationTransfer
                ? translator.Format("Holding_Moved", place, locations.GetValueOrDefault(assetEvent.ToLocationId ?? Guid.Empty, "?"))
                : string.Join(" · ", new[] { place, assetEvent.Reason }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var money = assetEvent.Kind == AssetEventKind.Sale ? assetEvent.ProceedsAmount : assetEvent.BasisAmount;
            if (money is { } amount)
            {
                detail += " · " + text.Money(amount, type.PriceCurrencyCode);
            }

            var effect = assetEvent.TotalEffect;
            eventRows.Add(new HoldingEventRow(assetEvent.Id, text.EventKind(assetEvent.Kind), text.Date(assetEvent.Date), text.SignedQuantity(assetEvent, type), detail,
                Math.Sign(effect)));
        }

        var priceRows = new List<ValuationRow>();
        foreach (var valuation in valuations.OrderByDescending(v => v.Date))
        {
            priceRows.Add(new ValuationRow(valuation.Id, text.Price(valuation.PricePerUnitMilli, type), text.Date(valuation.Date),
                valuation.Source == ValuationSource.Purchase ? translator["Holding_FromPurchase"] : valuation.Note));
        }
        return new(source, name, icon, quantityText, derivedText, hasStock, purityText, perLocation.ToArray(),
            valueUnknown, valueText, averageCostText, unrealisedText, realisedText, eventRows.ToArray(), priceRows.ToArray(),
            (source.Settings ?? new ZananceSettings()).Shows(Feature.HoldingDetails));
    }

    private sealed record Prepared(HoldingDetailSnapshot Source, string? Name, Symbol Icon, string? Quantity,
        string? Derived, bool HasStock, string? Purity, string[] Places, bool Unknown, string? Value,
        string? Average, string? Unrealised, string? Realised, HoldingEventRow[] Events, ValuationRow[] Prices, bool Advanced)
    {
        internal static Prepared Missing(HoldingDetailSnapshot source) =>
            new(source, null, Symbol.Diamond, null, null, false, null, [], false, null, null, null, null, [], [], false);
    }

    private sealed class MissingOriginalDetailException : Exception;

    [RelayCommand]
    private Task RecordAsync(string kind)
    {
        if (!Enum.TryParse<AssetEventKind>(kind, out var parsed) || !Enum.IsDefined(parsed)
            || !IsAdvanced && parsed != AssetEventKind.Correction) return Task.CompletedTask;
        return NavigateAsync(AppRoutes.AssetEventEditorRoute, new Dictionary<string, object> { ["type"] = _id, ["kind"] = kind });
    }

    [RelayCommand]
    private Task EditTypeAsync() => IsAdvanced
        ? NavigateAsync(AppRoutes.AssetTypeEditorRoute, new Dictionary<string, object> { ["id"] = _id }) : Task.CompletedTask;

    [RelayCommand]
    private Task DeletePriceAsync(ValuationRow row) => Loading.IsReady && Valuations.Any(item => ReferenceEquals(item, row))
        ? PriceEditor.DeleteAsync(row.Id, row.Description) : Task.CompletedTask;

    /// <summary>Rechecks original native route ownership and actual opened file before using a retained type identity.</summary>
    private async Task NavigateAsync(string route, IDictionary<string, object> parameters)
    {
        if (!Loading.IsReady || NotFound || _displayed is not { } source) return;
        var revision = _revision; var token = _lifetime.Token; var loading = Loading;
        bool Current() => !token.IsCancellationRequested && revision == _revision && ReferenceEquals(loading, Loading)
            && loading.IsReady && ReferenceEquals(source, _displayed);
        try
        {
            await holdings.ValidateDetailAccessAsync(source, token);
            if (Current()) await interaction.NavigateAsync(route, parameters);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (!Current()) return;
            PriceEditor.Retire(); HistoryEditor.Retire(); ArchiveEditor.Retire();
            try { await loading.RunAsync(() => Task.FromException(ex), () => { }); }
            catch (Exception failure) when (ReferenceEquals(failure, ex)) { }
            await interaction.ShowFailureAsync(ex);
        }
    }
}
