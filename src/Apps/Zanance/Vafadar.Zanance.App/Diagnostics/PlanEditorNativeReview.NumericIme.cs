#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Retains the complete original source around additional adapter-only checks without replaying forms.</summary>
    internal Task<string> NumericImeSourceAsync(CancellationToken ct) => AllMoneyRowsAsync(ct);

    /// <summary>Opens actual original numeric controls for native IME commits without invoking a writer.</summary>
    internal async Task NumericImeInputsAsync(Func<Page, string, string, string, Func<string>, Task> input, bool followup, CancellationToken ct)
    {
        var before = await AllMoneyRowsAsync(ct); var culture = Localization.FormattingCultureName; string? route = null;
        async Task Leave()
        {
            if (route is null) return;
            await ReturnMoneyTabAsync(false, ct); Routing.UnRegisterRoute(route); _routes.Remove(route); route = null;
        }
        try
        {
            await SetTabMoneyCultureAsync("de-DE", ct);
            var type = new AssetTypeEditorViewModel(NumericHoldings, Store, Translator, Localization, new MoneyInteraction());
            var physical = new AssetTypeEditorPage(type); route = await ShowAsync(physical, new() { ["id"] = _numericTypeId });
            await WaitAsync(() => type.CanEdit && physical.Width > 0, ct);
            type.PurityIndex = Purity.Common.Count;
            if (!followup)
            {
                await input(physical, "purity-comma", Translator["AssetType_Purity"], "18,5", () => type.PurityText);
                await input(physical, "weight-comma", Translator["AssetType_UnitWeight"], "1,234", () => type.UnitWeightText);
            }
            else
            {
                await input(physical, "purity-partial", Translator["AssetType_Purity"], "18,", () => type.PurityText);
                await input(physical, "weight-grouped", Translator["AssetType_UnitWeight"], "1.234,567", () => type.UnitWeightText);
                await input(physical, "weight-persian", Translator["AssetType_UnitWeight"], "۱,۲۳۴", () => type.UnitWeightText);
                await input(physical, "weight-negative", Translator["AssetType_UnitWeight"], "-1,234", () => type.UnitWeightText);
            }
            await Leave();

            var ratio = NumericRates(); var rates = new RatesPage(ratio); route = await ShowAsync(rates);
            await WaitAsync(() => ratio.CanEdit && rates.Width > 0, ct);
            if (!followup) await input(rates, "rate-comma", Translator["Rates_Rate"], "1,25", () => ratio.RateText);
            else
            {
                await input(rates, "rate-invalid-suffix", Translator["Rates_Rate"], "1,2x", () => ratio.RateText);
                await input(rates, "rate-double-comma", Translator["Rates_Rate"], "1,,25", () => ratio.RateText);
                await input(rates, "rate-spaces", Translator["Rates_Rate"], " 1,25 ", () => ratio.RateText);
            }
            await Leave();

            await SetTabMoneyCultureAsync("en-US", ct);
            var account = new AccountEditorViewModel(Store, Plans, Translator, Localization, Time,
                new AccountMoneyEffects(_host.GetRequiredService<Interaction.IAppInteraction>()));
            var accountPage = new AccountEditorPage(account); route = await ShowAsync(accountPage, []);
            await WaitAsync(() => account.CanInteract && accountPage.Width > 0, ct);
            if (!followup) await input(accountPage, "opening-negative", account.Form.OpeningLabel, "-12.34", () => account.Form.OpeningText);
            else
            {
                await input(accountPage, "opening-partial-sign", account.Form.OpeningLabel, "-", () => account.Form.OpeningText);
                await input(accountPage, "opening-invalid-suffix", account.Form.OpeningLabel, "12x.34", () => account.Form.OpeningText);
            }
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only native IME probe changed original source rows.");
        }
        finally { await Leave(); await SetTabMoneyCultureAsync(culture, ct); }
    }
}
#endif
