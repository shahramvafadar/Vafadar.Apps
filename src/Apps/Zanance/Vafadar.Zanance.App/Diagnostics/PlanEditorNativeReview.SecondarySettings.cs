#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Prepares only a fresh separately owned settings source before native input.</summary>
    internal async Task PrepareSecondarySettingsAsync(CancellationToken ct)
    {
        Store.Changed -= Accepted; Plans.Changed -= Accepted;
        await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-settings-prepared.json"), await AllMoneyRowsAsync(ct), ct);
    }
    /// <summary>Continues exact source facts and refuses uncertain accepted settings commands.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueSecondarySettingsAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = new PlanEditorNativeReview(host, folder);
        var accepted = File.Exists(Path.Combine(folder, "secondary-settings-accepted.json"));
        if (!accepted && File.Exists(Path.Combine(folder, "secondary-settings-accepted.marker")))
            throw new IOException("Inspect original estimate acceptance before continuation; never replay Save.");
        if (await File.ReadAllTextAsync(Path.Combine(folder, accepted ? "secondary-settings-accepted.json" : "secondary-settings-prepared.json"), ct) != await result.AllMoneyRowsAsync(ct))
            throw new IOException("Original estimate source differs from its exact checkpoint.");
        return result;
    }
    /// <summary>Gets actual estimate labels affected by the original unit/format, not unrelated Settings controls.</summary>
    internal static HashSet<string> SecondarySettingsCaptions(Page page) => page.BindingContext is SettingsViewModel model
        ? new[] { model.EssentialCurrencyText, model.EssentialUnitNote, model.EssentialFormatNote, model.EssentialError, model.EssentialSavedText }
            .Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal) : [];
    /// <summary>Gets only the original estimate input for native raw-value comparison.</summary>
    internal static HashSet<string> SecondarySettingsRaw(Page page) => page.BindingContext is SettingsViewModel model ? [model.EssentialText] : [];
    /// <summary>Qualifies real Settings and original fixed-caption native forecast prompts; only one estimate Save is accepted overall.</summary>
    internal async Task SecondarySettingsContextAsync(string context, Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName; string? route = null; var pendingDisplay = false;
        async Task View(Page page, string stage) { if (completed?.Invoke(context + "-" + stage) != true) await observe(page, context + "-" + stage); }
        try
        {
            var before = await AllMoneyRowsAsync(ct);
            DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
            var model = new SettingsViewModel(Localization, Translator, new DateFormatter(Localization), Time, Store,
                _host.GetRequiredService<ReminderService>(), _host.GetRequiredService<AppLockService>(),
                _host.GetRequiredService<ThemeService>(), _host.GetRequiredService<ProfileService>(), new MoneyInteraction());
            var page = new SettingsPage(model, Localization); route = await ShowAsync(page);
            await WaitAsync(() => model.Loading.IsReady && page.Width > 0, ct);
            model.EssentialText = "1,234";
            Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; model.RefreshDisplay();
            if (model.EssentialFormatNote is null) throw new IOException("Original essential estimate format note missing.");
            await View(page, "estimate-original");
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); model.RefreshDisplay();
            if (model.EssentialError is null) throw new IOException("Original essential estimate borrowed the current unit.");
            await invoke(page, model.SaveEssentialCommand, null);
            await WaitAsync(() => model.SaveEssentialCommand.ExecutionTask is { IsCompleted: true }, ct);
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Stale essential estimate changed source rows.");
            await View(page, "estimate-review");
            if (!File.Exists(Path.Combine(_folder, "secondary-settings-accepted.json")))
            {
                if (File.Exists(Path.Combine(_folder, "secondary-settings-accepted.marker"))) throw new IOException("Do not replay original estimate acceptance.");
                model.EssentialText = ""; model.EssentialText = "0,1234";
                void Committed(object? sender, EventArgs args) => File.WriteAllText(Path.Combine(_folder, "secondary-settings-accepted.marker"), "Original estimate metadata committed.");
                Store.Changed += Committed;
                try
                {
                    await invoke(page, model.SaveEssentialCommand, null);
                    await WaitAsync(() => model.SaveEssentialCommand.ExecutionTask is { IsCompleted: true } && model.EssentialSavedText is not null, ct);
                }
                finally { Store.Changed -= Committed; }
                if ((await Store.GetSettingsAsync(ct)).EssentialEstimate != 1234 || !File.Exists(Path.Combine(_folder, "secondary-settings-accepted.marker")))
                    throw new IOException("Original native estimate Save changed its monetary meaning.");
                await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-settings-accepted.json"), await AllMoneyRowsAsync(ct), ct);
                before = await AllMoneyRowsAsync(ct); await View(page, "estimate-accepted");
            }
            foreach (var accept in new[] { true, false })
            {
                var stage = accept ? "forecast-accept" : "forecast-cancel";
                if (completed?.Invoke(context + "-" + stage) == true) continue;
                DisplayUnits.Set([]); Localization.SetFormattingCulture("en-US"); pendingDisplay = true;
                var prompt = ForecastAmountPrompt.ReadAsync("EUR", Localization.CurrentCulture, 1234, (unit, initial) => new MauiForecastInteraction().PromptAsync(
                    Translator["Forecast_AssumeAmount"], Translator.Format("Forecast_AssumeAmountMessage", unit), Translator["Common_Ok"], Translator["Common_Cancel"], initial, 20, true));
                // The already-open native prompt retains its original caption, input and parser after these display changes.
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); Localization.SetFormattingCulture("de-DE"); pendingDisplay = true;
                await observe(Shell.Current.CurrentPage, context + "-" + stage);
                var amount = await prompt.WaitAsync(ct);
                if (accept ? amount != 1234 : amount is not null) throw new IOException("Native fixed-caption forecast prompt changed original meaning.");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Native forecast assumption prompt changed financial rows.");
                Localization.SetFormattingCulture("en-US"); pendingDisplay = true;
            }
        }
        finally
        {
            await ReturnMoneyTabAsync(pendingDisplay, ct);
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(units); await SetTabMoneyCultureAsync(culture, ct);
        }
    }
    /// <summary>Verifies the one original metadata Save without pretending that prompt observations post financial entries.</summary>
    internal async Task VerifySecondarySettingsAsync(CancellationToken ct)
    {
        if (!File.Exists(Path.Combine(_folder, "secondary-settings-accepted.json"))
            || (await Store.GetSettingsAsync(ct)).EssentialEstimate != 1234 || (await Store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new IOException("Original estimate or prompt source facts differ.");
    }
}
#endif
