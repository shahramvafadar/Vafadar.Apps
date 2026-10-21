#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Declares an independent unit-only source; all successful native writes require their original receipt.</summary>
    internal async Task PrepareSecondaryUnitsAsync(CancellationToken ct)
    {
        Store.Changed -= Accepted; Plans.Changed -= Accepted;
        await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-units-prepared.json"), await AllMoneyRowsAsync(ct), ct);
    }
    /// <summary>Reopens exact completed or prepared source rows; an uncertain accepted write never starts over.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueSecondaryUnitsAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = new PlanEditorNativeReview(host, folder);
        var checkpoint = File.Exists(Path.Combine(folder, "secondary-units-accepted.json")) ? "secondary-units-accepted.json" : "secondary-units-prepared.json";
        if (File.Exists(Path.Combine(folder, "secondary-units-accepted.marker")) && checkpoint != "secondary-units-accepted.json")
            throw new IOException("Retain uncertain original unit acceptance; inspect its source before any continuation.");
        if (await File.ReadAllTextAsync(Path.Combine(folder, checkpoint), ct) != await result.AllMoneyRowsAsync(ct))
            throw new IOException("Original secondary unit source rows differ.");
        return result;
    }
    /// <summary>Gets actual new error/definition/preview captions, without replaying unrelated page checks.</summary>
    internal HashSet<string> SecondaryUnitsCaptions(Page page) => page.BindingContext is DisplayUnitsViewModel model
        ? new[] { model.Error, model.PreviewText, Translator["Unit_UseToman"] }.Concat(model.Units.SelectMany(row => new[] { row.Text, row.Example }))
            .Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal) : [];
    /// <summary>Renders each original real page and negative writer once per display context; accepts one explicit unit definition total.</summary>
    internal async Task SecondaryUnitsContextAsync(string context, Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var units = DisplayUnits.All; string? route = null;
        var effects = new MoneyInteraction();
        var model = new DisplayUnitsViewModel(Store, Translator, Localization, effects); var page = new DisplayUnitsPage(model);
        async Task View(string stage) { if (completed?.Invoke(context + "-" + stage) != true) await observe(page, context + "-" + stage); }
        try
        {
            route = await ShowAsync(page); await WaitAsync(() => model.CanEdit && page.Width > 0, ct);
            model.CurrencyCode = "EUR"; model.FactorIndex = 1; model.Name = "Hundred euros";
            if (!File.Exists(Path.Combine(_folder, "secondary-units-accepted.json")))
            {
                if (File.Exists(Path.Combine(_folder, "secondary-units-accepted.marker"))) throw new IOException("Never replay uncertain native unit acceptance.");
                void KnownAcceptance(object? sender, EventArgs args)
                {
                    File.WriteAllText(Path.Combine(_folder, "secondary-units-accepted.marker"), "Original unit definition committed before owned notification failure.");
                    throw new IOException("Owned native unit notification failure.");
                }
                Store.Changed += KnownAcceptance;
                try { await invoke(page, model.AddCommand, null); await WaitAsync(() => !model.IsBusy && string.IsNullOrEmpty(model.Name), ct); }
                finally { Store.Changed -= KnownAcceptance; }
                if (model.Error != Translator["Unit_SavedFollowUpFailed"] || !model.CanEdit
                    || (await Store.GetSettingsAsync(ct)).DisplayUnits != "EUR|Hundred euros|2")
                    throw new IOException("Original native accepted unit result was not retained.");
                await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-units-accepted.json"), await AllMoneyRowsAsync(ct), ct);
                await View("unit-accepted");
            }
            var before = await AllMoneyRowsAsync(ct);
            model.Name = "Original unit draft"; await View("unit-definition");
            if (completed?.Invoke(context + "-unit-failed") != true)
            {
                Factory.RefuseOnce = true; await invoke(page, model.AddCommand, null);
                await WaitAsync(() => !model.IsBusy && !model.CanEdit && model.Error == Translator["Unit_SaveFailed"], ct);
                if (Factory.RefuseOnce || before != await AllMoneyRowsAsync(ct) || model.Name != "Original unit draft")
                    throw new IOException("Failed original native unit Save changed its source or raw draft.");
                await View("unit-failed");
            }
            // This is an explicit readback, not another accepted command or source seed.
            await model.LoadAsync();
            if (!model.CanEdit || model.Error is not null || before != await AllMoneyRowsAsync(ct)
                || DisplayUnits.All.Single().Name != "Hundred euros") throw new IOException("Native unit readback changed original source rows.");
            await View("unit-readback");
            if (completed?.Invoke(context + "-unit-action") != true)
            {
                await invoke(page, model.UseTomanCommand, null);
                if (model.CurrencyCode != "IRR" || model.FactorIndex != 0 || model.Name != Translator["Unit_TomanName"]
                    || before != await AllMoneyRowsAsync(ct)) throw new IOException("Original native unit suggestion did not preserve source rows.");
                await View("unit-action");
            }
            if (completed?.Invoke(context + "-unit-saved-feedback-caption") != true)
            {
                if (!File.Exists(Path.Combine(_folder, "secondary-units-accepted.json"))
                    || (await Store.GetSettingsAsync(ct)).DisplayUnits != "EUR|Hundred euros|2")
                    throw new IOException("A real original accepted unit receipt is required for translated caption-only review.");
                // Render the known original accepted outcome in each translation; this is not another Save or native failure replay.
                model.Error = Translator["Unit_SavedFollowUpFailed"];
                await View("unit-saved-feedback-caption");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Accepted-outcome caption review changed original rows.");
            }
        }
        finally
        {
            model.Deactivate(); Factory.RefuseOnce = false;
            await ReturnMoneyTabAsync(false, ct);
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(units);
        }
    }
}
#endif
