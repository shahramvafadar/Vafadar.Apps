#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    private HoldingStore? _holdingMoneyStore;
    private HoldingStore HoldingMoneyStore => _holdingMoneyStore ??= new(Factory, new ReviewAccess());
    /// <summary>Seeds one declared payment account/type/place; the three accepted native actions remain unseeded.</summary>
    internal async Task PrepareHoldingMoneyAsync(CancellationToken ct)
    {
        if ((await Store.GetAccountsAsync(cancellationToken: ct)).Count != 0) throw new IOException("Fresh holding monetary fixture required.");
        // This batch declares several setup writes; the plan batch's one-action phase receipt cannot own them.
        Store.Changed -= Accepted; Plans.Changed -= Accepted;
        await Store.EnsureDefaultCategoriesAsync(ct);
        await Store.SaveAccountAsync(new Account { Name = "Original USD holding cash", CurrencyCode = "USD", OpeningBalance = 900000,
            OpeningDate = DateOnly.FromDateTime(Time.GetLocalNow().DateTime) }, ct);
        await HoldingMoneyStore.SaveTypeAsync(new AssetType { Name = "Original monetary holding", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" }, ct);
        await HoldingMoneyStore.EnsureDefaultLocationAsync("Original monetary place", ct);
        Store.Changed += HoldingMoneyAccepted; HoldingMoneyStore.Changed += HoldingMoneyAccepted;
        await WriteHoldingMoneyCheckpointAsync(ct);
    }
    private void HoldingMoneyAccepted(object? sender, EventArgs args) =>
        Task.Run(() => WriteHoldingMoneyCheckpointAsync(CancellationToken.None)).GetAwaiter().GetResult();
    private async Task WriteHoldingMoneyCheckpointAsync(CancellationToken ct) =>
        await File.WriteAllTextAsync(Path.Combine(_folder, "holding-money-latest.json"), await AllMoneyRowsAsync(ct), ct);
    /// <summary>Distinguishes an actual accepted manual price from the purchase's automatically recorded price.</summary>
    internal async Task<bool> HoldingMoneyHasManualPriceAsync(CancellationToken ct) =>
        (await HoldingMoneyStore.GetValuationsAsync((await HoldingMoneyStore.GetTypesAsync(ct)).Single().Id, ct))
            .Any(value => value.Source == ValuationSource.Manual);
    /// <summary>Proves actual final accepted rows before reporting native action counts.</summary>
    internal async Task VerifyHoldingMoneyFinalAsync(CancellationToken ct)
    {
        var type = (await HoldingMoneyStore.GetTypesAsync(ct)).Single();
        var events = await HoldingMoneyStore.GetEventsAsync(type.Id, ct); var entries = await Store.GetEntriesAsync(cancellationToken: ct);
        var values = await HoldingMoneyStore.GetValuationsAsync(type.Id, ct);
        if (events.Count != 2 || events.Any(item => item.Quantity != 1000 || item.BasisAmount != 123400)
            || events.Count(item => item.Kind == AssetEventKind.Purchase) != 1 || events.Count(item => item.Kind == AssetEventKind.Opening) != 1
            || entries.Count != 2 || entries.Single(item => item.Kind == EntryKind.AssetPurchase).Amount != 123400
            || entries.Single(item => item.Kind == EntryKind.Expense).Amount != 100
            || values.Count(item => item.Source == ValuationSource.Manual && item.PricePerUnitMilli == 123400000 && item.CurrencyCode == "EUR") != 1)
            throw new IOException("Original native monetary actions are incomplete; automatic purchase price is not manual price acceptance.");
        if (await File.ReadAllTextAsync(Path.Combine(_folder, "holding-money-latest.json"), ct) != await AllMoneyRowsAsync(ct))
            throw new IOException("Final holding monetary source checkpoint differs.");
    }
    /// <summary>Continues only exact original accepted source rows, without another seed or completed financial action.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueHoldingMoneyAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var original = new PlanEditorNativeReview(host, folder);
        if (!File.Exists(Path.Combine(folder, "holding-money-latest.json")))
        {
            // Resume the exact failed initial observer: its USD fixture account committed, but no type/place/native action ran.
            var accounts = await original.Store.GetAccountsAsync(cancellationToken: ct);
            if (!File.Exists(Path.Combine(folder, "started.json")) || !File.Exists(Path.Combine(folder, "prepared-accepted.json"))
                || accounts.Count != 1 || accounts[0].Name != "Original USD holding cash" || accounts[0].CurrencyCode != "USD"
                || accounts[0].OpeningBalance != 900000 || (await original.Store.GetEntriesAsync(cancellationToken: ct)).Count != 0
                || (await original.HoldingMoneyStore.GetTypesAsync(ct)).Count != 0
                || (await original.HoldingMoneyStore.GetEventsAsync(cancellationToken: ct)).Count != 0)
                throw new IOException("Only the exact declared incomplete holding setup may continue.");
            await original.HoldingMoneyStore.SaveTypeAsync(new AssetType { Name = "Original monetary holding", Kind = AssetKind.CountableItem,
                Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" }, ct);
            await original.HoldingMoneyStore.EnsureDefaultLocationAsync("Original monetary place", ct);
            await original.WriteHoldingMoneyCheckpointAsync(ct);
        }
        if (await File.ReadAllTextAsync(Path.Combine(folder, "holding-money-latest.json"), ct) != await original.AllMoneyRowsAsync(ct))
            throw new IOException("Original holding monetary checkpoint differs.");
        original.Store.Changed += original.HoldingMoneyAccepted; original.HoldingMoneyStore.Changed += original.HoldingMoneyAccepted;
        return original;
    }
    /// <summary>Gets original monetary captions from the actual event and detail pages.</summary>
    internal static HashSet<string> HoldingMoneyCaptions(Page page)
    {
        IEnumerable<string?> values = page.BindingContext switch
        {
            AssetEventEditorViewModel vm => [vm.AmountUnitName, vm.FeeUnitName, vm.SecondUnitName, vm.BasisUnitName,
                vm.AmountUnitNote, vm.FeeUnitNote, vm.SecondUnitNote, vm.BasisUnitNote, vm.QuantityFormatNote,
                vm.AmountFormatNote, vm.FeeFormatNote, vm.SecondFormatNote, vm.BasisFormatNote,
                vm.AmountContextError, vm.FeeContextError, vm.SecondContextError, vm.BasisContextError, vm.SecondAmountError, vm.Error],
            HoldingDetailViewModel vm => [vm.PriceEditor.InputUnitName, vm.PriceEditor.InputUnitNote, vm.PriceEditor.InputFormatNote,
                vm.PriceEditor.InputContextError, vm.PriceEditor.Error],
            _ => [],
        };
        return values.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Gets actual raw monetary inputs for native presenter equality.</summary>
    internal static HashSet<string> HoldingMoneyRaw(Page page)
    {
        IEnumerable<string?> values = page.BindingContext switch
        {
            AssetEventEditorViewModel vm => [vm.QuantityText, vm.AmountText, vm.FeeText, vm.SecondAmountText, vm.BasisText],
            HoldingDetailViewModel vm => [vm.PriceEditor.Raw],
            _ => [],
        };
        return values.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Checks six actual form stages per context and accepts each original purchase/opening/price at most once.</summary>
    internal async Task HoldingMoneyContextAsync(string context, Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName;
        var before = await AllMoneyRowsAsync(ct); string? route = null; var pendingDisplay = false;
        var effects = new AccountMoneyEffects(_host.GetRequiredService<Interaction.IAppInteraction>());
        var text = _host.GetRequiredService<HoldingText>(); var holdings = HoldingMoneyStore;
        var type = (await holdings.GetTypesAsync(ct)).Single();
        async Task Leave()
        {
            await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false;
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); route = null; }
        }
        async Task View(Page page, string name)
        {
            if (completed?.Invoke(context + "-" + name) != true) await observe(page, context + "-" + name);
        }
        try
        {
            foreach (var kind in new[] { AssetEventKind.Purchase, AssetEventKind.Opening })
            {
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var existing = (await holdings.GetEventsAsync(type.Id, ct)).SingleOrDefault(item => item.Kind == kind);
                var vm = new AssetEventEditorViewModel(holdings, Store, text, Translator, Localization, Time, effects);
                var page = new AssetEventEditorPage(vm);
                route = await ShowAsync(page, existing is null ? new() { ["type"] = type.Id, ["kind"] = kind.ToString() } : new() { ["id"] = existing.Id });
                await WaitAsync(() => vm.CanEdit && page.Width > 0, ct);
                vm.QuantityText = "1";
                if (kind == AssetEventKind.Purchase) { vm.AmountText = "1,234"; vm.FeeText = "1"; vm.SecondAmountText = "1,234"; }
                else vm.BasisText = "1,234";
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; vm.RefreshMoneyTexts();
                if (kind == AssetEventKind.Purchase && (vm.AmountFormatNote is null || vm.SecondFormatNote is null)
                    || kind == AssetEventKind.Opening && vm.BasisFormatNote is null) throw new IOException("Original holding parser caption missing.");
                var prefix = kind == AssetEventKind.Purchase ? "purchase" : "opening";
                await View(page, prefix + "-original");
                if (kind == AssetEventKind.Purchase && completed?.Invoke(context + "-quantity-original") != true)
                {
                    vm.QuantityText = "1,234"; vm.RefreshMoneyTexts();
                    if (vm.QuantityFormatNote is null || vm.QuantityFormatNote.Contains("EUR", StringComparison.Ordinal))
                        throw new IOException("Original physical quantity example must be numeric, without monetary units.");
                    await View(page, "quantity-original"); vm.QuantityText = "1";
                }
                if (kind == AssetEventKind.Purchase && completed?.Invoke(context + "-purchase-invalid") != true)
                {
                    vm.SecondAmountText = "not money";
                    await invoke(page, vm.SaveCommand, null);
                    if (!vm.CanEdit || vm.SecondAmountError is null || before != await AllMoneyRowsAsync(ct))
                        throw new IOException("Nonblank invalid holding price was accepted as unknown.");
                    await View(page, "purchase-invalid"); vm.SecondAmountText = "1,234";
                }
                DisplayUnits.Set([new("USD", "Hundred USD", 2), new("EUR", "Hundred EUR", 2)]); vm.RefreshMoneyTexts();
                if (vm.CanSave || kind == AssetEventKind.Purchase && (vm.AmountContextError is null || vm.FeeContextError is null || vm.SecondContextError is null)
                    || kind == AssetEventKind.Opening && vm.BasisContextError is null) throw new IOException("Stale holding money became writable.");
                await View(page, prefix + "-review");
                if (kind == AssetEventKind.Purchase)
                {
                    vm.AmountText = string.Empty; vm.AmountText = "12,34"; vm.FeeText = string.Empty; vm.FeeText = "0,01";
                    vm.SecondAmountText = string.Empty; vm.SecondAmountText = "12,34";
                }
                else { vm.BasisText = string.Empty; vm.BasisText = "12,34"; }
                if (!vm.CanSave || before != await AllMoneyRowsAsync(ct)) throw new IOException("Explicit holding reentry/source equality failed.");
                if (existing is null)
                {
                    await File.WriteAllTextAsync(Path.Combine(_folder, prefix + "-money-intention.json"), context, ct);
                    await invoke(page, vm.SaveCommand, null); await WaitAsync(() => !vm.IsBusy && !vm.CanEdit, ct);
                    var accepted = (await holdings.GetEventsAsync(type.Id, ct)).Single(item => item.Kind == kind);
                    var entries = await Store.GetEntriesAsync(cancellationToken: ct);
                    if (accepted.Quantity != 1000 || accepted.BasisAmount != 123400 || entries.Count != 2
                        || entries.Single(item => item.Kind == EntryKind.AssetPurchase).Amount != 123400
                        || entries.Single(item => item.Kind == EntryKind.Expense).Amount != 100) throw new IOException("Original native holding money differs.");
                    await WriteHoldingMoneyCheckpointAsync(ct); before = await AllMoneyRowsAsync(ct);
                    await View(page, prefix + "-saved");
                }
                await Leave();
            }
            DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
            var detail = new HoldingDetailViewModel(holdings, text, Translator, Localization, Time, effects);
            detail.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id });
            var details = new HoldingDetailPage(detail); route = await ShowAsync(details, []);
            await WaitAsync(() => detail.Loading.IsReady && details.Width > 0, ct);
            var price = detail.PriceEditor; price.Raw = "1,234"; price.ModeIndex = 0;
            Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; price.RefreshMoneyTexts();
            if (price.InputFormatNote is null) throw new IOException("Original manual price parser caption missing.");
            await View(details, "price-original");
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); price.RefreshMoneyTexts();
            if (price.InputContextError is null || price.InputUnitName != "EUR") throw new IOException("Original manual price unit lost.");
            await View(details, "price-review");
            price.Raw = string.Empty; price.Raw = "12,34";
            if (!await HoldingMoneyHasManualPriceAsync(ct))
            {
                await File.WriteAllTextAsync(Path.Combine(_folder, "price-money-intention.json"), context, ct);
                await invoke(details, price.SaveCommand, null); await WaitAsync(() => !price.Busy && price.Raw.Length == 0, ct);
                var accepted = (await holdings.GetValuationsAsync(type.Id, ct)).Single(item => item.Source == ValuationSource.Manual);
                if (accepted.PricePerUnitMilli != 123400000 || accepted.CurrencyCode != "EUR") throw new IOException("Original native manual price differs.");
                await WriteHoldingMoneyCheckpointAsync(ct); before = await AllMoneyRowsAsync(ct);
                await View(details, "price-saved");
            }
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only holding observations changed source rows.");
            await Leave();
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(_folder, "holding-money-failed-" + Environment.ProcessId + ".json"),
                JsonSerializer.Serialize(new { context, Error = error.ToString(), ProcessId = Environment.ProcessId }), CancellationToken.None); throw;
        }
        finally { await Leave(); DisplayUnits.Set(units); await SetTabMoneyCultureAsync(culture, ct); }
    }
}
#endif
