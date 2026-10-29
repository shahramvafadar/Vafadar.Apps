using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>One asset type in the holdings list.</summary>
/// <param name="Id">The asset type.</param>
/// <param name="Name">Its name.</param>
/// <param name="Icon">Its icon.</param>
/// <param name="QuantityText">"20.000 g" or "3 coins".</param>
/// <param name="DetailText">Locations and purity, e.g. "Home safe · 750/1000".</param>
/// <param name="ValueText">"1,000.00 EUR · price of 2 Oct", or <see langword="null"/> without a price.</param>
/// <param name="DerivedText">The weight of counted coins, e.g. "(30.000 g)".</param>
public sealed record HoldingRow(Guid Id, string Name, Symbol Icon, string QuantityText, string? DetailText, string? ValueText, string? DerivedText)
{
    /// <summary>Gets a value indicating whether the value is unknown ("Value unknown – add a price"), never zero.</summary>
    public bool ValueUnknown => ValueText is null;

    /// <summary>Gets the row as one sentence for screen readers.</summary>
    public string Description => string.Join(", ", new[] { Name, QuantityText, DerivedText, DetailText, ValueText }.Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>
/// The holdings list (ZEX-S0407, design §7): every asset type with its quantity, where it is kept and its value or
/// "value unknown", the known value per currency with what is missing, and the fine-metal equivalent per metal. Holdings
/// never mix into money totals. Simple shows the list; creating types and events is in Advanced.
/// </summary>
public sealed partial class HoldingsViewModel(HoldingStore holdings, HoldingText text, Translator translator,
    TimeProvider time, IAppInteraction interaction) : ViewModelBase
{
    private CancellationTokenSource _lifetime = new();
    private int _revision;
    private HoldingOverviewSnapshot? _displayed;

    /// <summary>Gets one complete row publication, replacing the previous snapshot with a single native reset.</summary>
    public SnapshotCollection<HoldingRow> Rows { get; } = [];

    /// <summary>Gets the complete fine-metal readouts from that same original overview.</summary>
    public SnapshotCollection<string> FineMetals { get; } = [];

    /// <summary>Gets the initial/refresh cover and explicit retry state for this original page lifetime.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    /// <summary>Gets the known value per currency, never combined with account balances.</summary>
    [ObservableProperty]
    public partial string? KnownValueText { get; set; }

    /// <summary>Gets the count of held types whose value is unknown, never guessed as zero.</summary>
    [ObservableProperty]
    public partial string? IncompleteText { get; set; }

    /// <summary>Gets whether a complete successfully published overview has no active holding type.</summary>
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    /// <summary>Gets whether this original profile's existing settings permit the Advanced holding actions.</summary>
    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    /// <summary>Prepares a complete original-file overview off UI and publishes only under its original cover.</summary>
    public async Task LoadAsync()
    {
        var loading = Loading; var revision = _revision; var token = _lifetime.Token;
        Prepared? prepared = null;
        void RequireCurrent()
        {
            token.ThrowIfCancellationRequested();
            if (revision != _revision || !ReferenceEquals(loading, Loading))
                throw new OperationCanceledException("Original holding overview page retired.", token);
        }
        try
        {
            await loading.RunAsync(async () =>
            {
                string? file = null;
                for (var attempt = 0; ; attempt++)
                {
                    RequireCurrent();
                    var snapshot = await holdings.ReadOverviewSnapshotAsync(token);
                    file ??= snapshot.DatabasePath;
                    if (!string.Equals(file, snapshot.DatabasePath, StringComparison.Ordinal))
                        throw new InvalidOperationException("Original holding overview cannot follow another profile.");
                    try
                    {
                        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
                        var result = await Task.Run(() => Prepare(snapshot, today), token);
                        await holdings.ValidateOverviewSnapshotAsync(snapshot, token);
                        RequireCurrent(); snapshot.EnsureCurrent(); prepared = result; return;
                    }
                    catch (HoldingOverviewSupersededException) when (attempt < 2)
                    {
                        // D-418: retry only an original same-file source edit; never redirect profiles or hide failures.
                        RequireCurrent();
                    }
                }
            }, () =>
            {
                RequireCurrent(); var result = prepared ?? throw new InvalidOperationException("Complete holding overview is missing.");
                result.Source.EnsureCurrent();
                // Native collection callbacks cannot invoke a retained target until the complete cover is removed.
                void Publish(Action change)
                {
                    RequireCurrent(); result.Source.EnsureCurrent(); change();
                    RequireCurrent(); result.Source.EnsureCurrent();
                }
                Publish(() => Rows.ReplaceAll(result.Rows)); Publish(() => FineMetals.ReplaceAll(result.FineMetals));
                Publish(() => KnownValueText = result.Known); Publish(() => IncompleteText = result.Incomplete);
                Publish(() => IsAdvanced = result.Advanced); Publish(() => IsEmpty = Rows.Count == 0);
                _displayed = result.Source;
            });
        }
        catch (Exception ex) when (token.IsCancellationRequested && ex is not OutOfMemoryException)
        {
            // A retired read can fail after cancellation. Preserve diagnostics without reporting over a newer route.
            System.Diagnostics.Debug.WriteLine($"Retired original holding overview: {ex}");
        }
    }

    /// <summary>Retires pending reads/native row targets before departure; a later appearance owns a fresh cover.</summary>
    public void Retire()
    {
        _revision++; _displayed = null;
        var old = _lifetime; _lifetime = new(); Loading = new(); old.Cancel(); old.Dispose();
    }

    /// <summary>Builds all original quantities, places, values and fine-metal totals before any native state changes.</summary>
    private Prepared Prepare(HoldingOverviewSnapshot snapshot, DateOnly today)
    {
        var input = snapshot.Inputs; var types = input.Types.Where(t => !t.IsArchived).ToList();
        var locations = input.Locations.ToDictionary(l => l.Id, l => l.Name);
        var values = AssetValuationService.Values(types, input.Events, input.Valuations, today).ToDictionary(v => v.AssetType.Id);
        var positions = HoldingsLedger.Positions(input.Events, today);
        var rows = new List<HoldingRow>(types.Count);
        foreach (var type in types)
        {
            var quantity = values.TryGetValue(type.Id, out var value) ? value.Quantity : 0;
            var places = positions.Where(p => p.AssetTypeId == type.Id && p.Quantity > 0).Select(p => locations.GetValueOrDefault(p.LocationId, "?"));
            var detail = string.Join(" · ", places.Append(text.Purity(type)).Where(s => s is not null));
            string? valueText = value?.Value is { } known
                ? translator.Format(value.FromPurchase ? "Holding_ValuePurchase" : "Holding_Value", text.Money(known, type.PriceCurrencyCode), text.Date(value.PriceDate!.Value))
                : null;
            rows.Add(new HoldingRow(type.Id, type.Name, HoldingText.Icon(type), text.Quantity(quantity, type), detail.Length == 0 ? null : detail,
                quantity == 0 ? translator["Holding_None"] : valueText, text.DerivedWeight(quantity, type)));
        }
        var held = values.Values.ToList();
        var knownValue = held.Any(v => v.Value is not null)
            ? string.Join(" · ", held.Where(v => v.Value is not null).GroupBy(v => v.AssetType.PriceCurrencyCode).Select(g => text.Money(g.Sum(v => v.Value!.Value), g.Key))) : null;
        var unknown = held.Count(v => v.Value is null);
        var incomplete = unknown > 0 ? translator.Format("Holding_Incomplete", unknown) : null;
        var fine = AssetValuationService.FineMetals(held).Select(item => text.FineMetal(item)).ToArray();
        return new(snapshot, rows.ToArray(), fine, knownValue, incomplete,
            (input.Settings ?? new ZananceSettings()).Shows(Feature.HoldingDetails));
    }

    /// <summary>Owns the complete prepared original result privately until its synchronous covered publication.</summary>
    private sealed record Prepared(HoldingOverviewSnapshot Source, HoldingRow[] Rows, string[] FineMetals,
        string? Known, string? Incomplete, bool Advanced);

    [RelayCommand]
    private Task AddAsync() => IsAdvanced ? NavigateAsync(AppRoutes.AssetTypeEditorRoute, null) : Task.CompletedTask;

    [RelayCommand]
    private Task OpenAsync(HoldingRow row) => Rows.Any(current => ReferenceEquals(current, row))
        ? NavigateAsync(AppRoutes.HoldingDetailRoute, new Dictionary<string, object> { ["id"] = row.Id }) : Task.CompletedTask;

    /// <summary>Refuses covered/retired native commands and checks original access before ordinary navigation.</summary>
    private async Task NavigateAsync(string route, IDictionary<string, object>? parameters)
    {
        if (!Loading.IsReady || _displayed is not { } snapshot) return;
        var loading = Loading; var token = _lifetime.Token; var revision = _revision;
        bool Current() => !token.IsCancellationRequested && revision == _revision && ReferenceEquals(loading, Loading)
            && loading.IsReady && ReferenceEquals(snapshot, _displayed);
        try
        {
            await holdings.ValidateOverviewAccessAsync(snapshot, token);
            if (Current())
            {
                if (route == AppRoutes.AssetTypeEditorRoute)
                {
                    parameters = parameters is null ? new Dictionary<string, object>() : new Dictionary<string, object>(parameters);
                    parameters["assetTypeEditorOrigin"] = snapshot.CreateTypeEditorOrigin();
                }
                await interaction.NavigateAsync(route, parameters);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (!Current()) return;
            // The failed original target stays covered with explicit Retry; never use its identity in a later file.
            try { await loading.RunAsync(() => Task.FromException(ex), () => { }); }
            catch (Exception failure) when (ReferenceEquals(failure, ex)) { }
            await interaction.ShowFailureAsync(ex);
        }
    }
}
