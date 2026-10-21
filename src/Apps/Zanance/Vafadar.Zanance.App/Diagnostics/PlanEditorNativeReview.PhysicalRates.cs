#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    private const string NumericPrepared = "physical-rates-prepared.json";
    private Guid _numericTypeId;
    private HoldingStore NumericHoldings => Services.GetRequiredService<HoldingStore>();
    /// <summary>Declares one actual independent type, never a cash account or ledger seed, before native input.</summary>
    internal async Task PreparePhysicalRatesAsync(CancellationToken ct)
    {
        Store.Changed -= Accepted; Plans.Changed -= Accepted;
        if ((await Store.GetAccountsAsync(cancellationToken: ct)).Count != 0 || (await NumericHoldings.GetTypesAsync(ct)).Count != 0 ||
            (await Store.GetRatesAsync(ct)).Count != 0) throw new IOException("Fresh independent physical/rate source required.");
        var type = new AssetType { Name = "Original physical numeric fixture", Kind = AssetKind.CoinOrBar,
            Dimension = AssetDimension.Count, Metal = Metal.Gold, PriceCurrencyCode = "EUR", UnitWeightMg = 1000,
            CountUnitName = "pieces", Divisible = true, Note = "Fictitious original numeric metadata", SortOrder = 17, Icon = "Diamond" };
        await NumericHoldings.SaveTypeAsync(type, ct); _numericTypeId = type.Id;
        await File.WriteAllTextAsync(Path.Combine(_folder, NumericPrepared), JsonSerializer.Serialize(new NumericPreparation(type.Id, await AllMoneyRowsAsync(ct))), ct);
    }
    private sealed record NumericPreparation(Guid Type, string Rows);
    /// <summary>Continues only the exact untouched original prepared type/source after an input observer stops.</summary>
    internal static async Task<PlanEditorNativeReview> ContinuePhysicalRatesAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = await ContinueEmptyAsync(host, folder, ct); result.Store.Changed -= result.Accepted; result.Plans.Changed -= result.Accepted;
        var prepared = JsonSerializer.Deserialize<NumericPreparation>(await File.ReadAllTextAsync(Path.Combine(folder, NumericPrepared), ct)) ?? throw new IOException("Original numeric preparation required.");
        result._numericTypeId = prepared.Type;
        if (prepared.Rows != await result.AllMoneyRowsAsync(ct)) throw new IOException("Original numeric source differs; never reseed or repeat an accepted action.");
        return result;
    }
    /// <summary>Gets only newly changed actual labels, preserving native text and renderer behavior.</summary>
    internal static HashSet<string> PhysicalRatesCaptions(Page page)
    {
        IEnumerable<string?> values = page.BindingContext switch
        {
            AssetTypeEditorViewModel type => [type.NameError, type.PurityFormatNote, type.PurityError, type.UnitWeightFormatNote, type.UnitWeightError],
            RatesViewModel rates => new[] { rates.RateFormatNote, rates.RateContextError, rates.RateError, rates.PairError, rates.Error }.Concat(rates.Rates.Select(row => row.Text)),
            _ => [],
        };
        return values.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    /// <summary>Identifies actual production numeric fields by their existing native semantic labels, including an empty weight example.</summary>
    internal Dictionary<string, string> PhysicalRatesInputs(Page page) => page.BindingContext switch
    {
        AssetTypeEditorViewModel type => new() { [Translator["AssetType_Name"]] = type.Name, [Translator["AssetType_Purity"]] = type.PurityText,
            [Translator["AssetType_UnitWeight"]] = type.UnitWeightText },
        RatesViewModel rates => new() { [Translator["Rates_Rate"]] = rates.RateText },
        _ => [],
    };
    /// <summary>Exercises actual original physical/rate editors with zero accepted commands; outcomes remain a separate continuation.</summary>
    internal async Task PhysicalRatesInputContextAsync(string context, Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var originalCulture = Localization.FormattingCultureName; var before = await AllMoneyRowsAsync(ct);
        string? route = null; var pendingDisplay = false;
        bool Done(string scene) => completed?.Invoke(context + "-" + scene) == true;
        try
        {
            if (!new[] { "physical-original", "physical-partial", "physical-invalid", "physical-cleared" }.All(Done))
            {
                await SetTabMoneyCultureAsync("de-DE", ct);
                var model = new AssetTypeEditorViewModel(NumericHoldings, Store, Translator, Localization, new MoneyInteraction());
                var page = new AssetTypeEditorPage(model); route = await ShowAsync(page, new() { ["id"] = _numericTypeId });
                await WaitAsync(() => model.CanEdit && page.Width > 0, ct);
                model.PurityIndex = Purity.Common.Count; model.PurityText = ""; model.PurityText = "18,5";
                model.UnitWeightText = ""; model.UnitWeightText = "1,234";
                Localization.SetFormattingCulture("en-US"); pendingDisplay = true;
                if (model.PurityFormatNote is null || model.UnitWeightFormatNote is null || model.PurityText != "18,5" || model.UnitWeightText != "1,234")
                    throw new IOException("Actual original physical format was replaced.");
                if (!Done("physical-original")) await observe(page, context + "-physical-original");
                model.PurityText = "18,6"; model.UnitWeightText = "1,235";
                if (!Done("physical-partial")) await observe(page, context + "-physical-partial");
                if (!Done("physical-invalid"))
                {
                    model.Name = ""; model.PurityText = "invalid purity"; model.UnitWeightText = "invalid grams";
                    await invoke(page, model.SaveCommand, null);
                    if (model.SaveCommand.ExecutionTask is { } save) await save.WaitAsync(ct);
                    if (model.NameError is null || model.PurityError is null || model.UnitWeightError is null || model.Committed || before != await AllMoneyRowsAsync(ct))
                        throw new IOException("Actual independent physical field refusal differs.");
                    await observe(page, context + "-physical-invalid");
                }
                model.Name = "Original physical numeric fixture"; model.PurityText = ""; model.UnitWeightText = "";
                if (model.PurityFormatNote is not null || model.UnitWeightFormatNote is not null || model.UnitWeightExample != "8.133")
                    throw new IOException("Actual explicit empty physical context did not renew its original example.");
                if (!Done("physical-cleared")) await observe(page, context + "-physical-cleared");
                await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false; Routing.UnRegisterRoute(route); _routes.Remove(route); route = null;
            }
            if (!new[] { "rate-original", "rate-partial", "rate-pair", "rate-invalid" }.All(Done))
            {
                await SetTabMoneyCultureAsync("de-DE", ct);
                var model = new RatesViewModel(Store, Translator, new Vafadar.Localization.Formatting.DateFormatter(Localization), Localization, Time, new MoneyInteraction());
                var page = new RatesPage(model); route = await ShowAsync(page); await WaitAsync(() => model.CanEdit && page.Width > 0, ct);
                model.FromCurrency = "USD"; model.ToCurrency = "EUR"; model.RateText = "1,25";
                Localization.SetFormattingCulture("en-US"); pendingDisplay = true;
                if (model.RateFormatNote is null || model.RateText != "1,25") throw new IOException("Actual original manual-ratio format was replaced.");
                if (!Done("rate-original")) await observe(page, context + "-rate-original");
                model.RateText = "1,250";
                if (!Done("rate-partial")) await observe(page, context + "-rate-partial");
                model.ToCurrency = "GBP";
                if (!Done("rate-pair"))
                {
                    await invoke(page, model.AddCommand, null); if (model.AddCommand.ExecutionTask is { } rejected) await rejected.WaitAsync(ct);
                    if (model.RateContextError is null || before != await AllMoneyRowsAsync(ct)) throw new IOException("Actual old manual pair was accepted.");
                    await observe(page, context + "-rate-pair");
                }
                model.RateText = ""; model.RateText = "invalid ratio";
                if (!Done("rate-invalid"))
                {
                    await invoke(page, model.AddCommand, null); if (model.AddCommand.ExecutionTask is { } rejected) await rejected.WaitAsync(ct);
                    if (model.RateError is null || model.RateContextError is not null || before != await AllMoneyRowsAsync(ct)) throw new IOException("Actual invalid plain ratio changed finance.");
                    await observe(page, context + "-rate-invalid");
                }
                await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false; Routing.UnRegisterRoute(route); _routes.Remove(route); route = null;
            }
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original physical/rate input review changed stored facts.");
        }
        finally
        {
            if (route is not null) { await ReturnMoneyTabAsync(pendingDisplay, ct); Routing.UnRegisterRoute(route); _routes.Remove(route); }
            await SetTabMoneyCultureAsync(originalCulture, ct);
        }
    }
}
#endif
