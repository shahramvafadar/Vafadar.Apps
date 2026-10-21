#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    private const string NumericActions = "physical-rate-actions-complete.json";
    private static readonly string[] NumericFeedback = ["Rates_SourceChanged", "Rates_ChangeFailed", "Rates_SavedFollowUpFailed"];
    private sealed record NumericActionsProof(Guid Type, string Rows, string[] Feedback);
    /// <summary>Reopens exact completed native metadata actions solely for remaining read-only caption observations.</summary>
    internal static async Task<PlanEditorNativeReview> ContinuePhysicalRateOutcomesAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        if (!File.Exists(Path.Combine(folder, NumericActions))) return await ContinuePhysicalRatesAsync(host, folder, ct);
        var result = await ContinueEmptyAsync(host, folder, ct); result.Store.Changed -= result.Accepted; result.Plans.Changed -= result.Accepted;
        var proof = JsonSerializer.Deserialize<NumericActionsProof>(await File.ReadAllTextAsync(Path.Combine(folder, NumericActions), ct))
            ?? throw new IOException("Original actual native numeric outcome receipt required.");
        var final = Path.Combine(folder, "rate-native-delete-accepted.json");
        var expected = File.Exists(final) ? await File.ReadAllTextAsync(final, ct) : proof.Rows;
        if (!proof.Feedback.SequenceEqual(NumericFeedback) || expected != await result.AllMoneyRowsAsync(ct))
            throw new IOException("Original completed numeric outcome rows differ; never replay an accepted writer.");
        result._numericTypeId = proof.Type; return result;
    }
    private RatesViewModel NumericRates() => new(Store, Translator, new DateFormatter(Localization), Localization, Time, _host.GetRequiredService<IAppInteraction>());
    /// <summary>Performs each actual native metadata action once, retaining typed row checkpoints before fallible observations.</summary>
    internal async Task PhysicalRateActionsAsync(Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct)
    {
        if (File.Exists(Path.Combine(_folder, NumericActions))) return;
        var started = Path.Combine(_folder, "physical-rate-actions-started.json");
        if (File.Exists(started)) throw new IOException("Incomplete original numeric actions require source inspection; never repeat accepted Save.");
        await File.WriteAllTextAsync(started, await AllMoneyRowsAsync(ct), ct);
        var culture = Localization.FormattingCultureName; string? route = null;
        async Task Checkpoint(string name) => await File.WriteAllTextAsync(Path.Combine(_folder, name + ".json"), await AllMoneyRowsAsync(ct), ct);
        async Task Leave()
        {
            await ReturnMoneyTabAsync(false, ct);
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); route = null; }
        }
        try
        {
            await SetTabMoneyCultureAsync("de-DE", ct);
            var type = new AssetTypeEditorViewModel(NumericHoldings, Store, Translator, Localization, new MoneyInteraction());
            var physical = new AssetTypeEditorPage(type); route = await ShowAsync(physical, new() { ["id"] = _numericTypeId });
            await WaitAsync(() => type.CanEdit && physical.Width > 0, ct);
            type.PurityIndex = Vafadar.Zanance.Core.Holdings.Purity.Common.Count; type.PurityText = ""; type.PurityText = "18,6";
            type.UnitWeightText = ""; type.UnitWeightText = "1,235";
            await invoke(physical, type.SaveCommand, null); if (type.SaveCommand.ExecutionTask is { } saved) await saved.WaitAsync(ct);
            var actual = (await NumericHoldings.GetTypesAsync(ct)).Single();
            if (!type.Committed || actual.Id != _numericTypeId || actual.UnitWeightMg != 1235 || actual.PurityPer10000 != 7750)
                throw new IOException("Actual physical correction was not committed to the original identity.");
            await Checkpoint("physical-native-correction-accepted");
            await Leave();
            await SetTabMoneyCultureAsync("en-US", ct);
            var rates = NumericRates(); var page = new RatesPage(rates); route = await ShowAsync(page);
            await WaitAsync(() => rates.CanEdit && page.Width > 0, ct);
            const decimal precise = 0.00000001234567890123456789m;
            rates.FromCurrency = "USD"; rates.ToCurrency = "EUR"; rates.RateText = precise.ToString(System.Globalization.CultureInfo.InvariantCulture); rates.IsEstimate = true;
            void RefuseNotice(object? sender, EventArgs args) => throw new IOException("Fictitious accepted numeric notification refusal.");
            Store.Changed += RefuseNotice;
            try { await invoke(page, rates.AddCommand, null); if (rates.AddCommand.ExecutionTask is { } add) await add.WaitAsync(ct); }
            finally { Store.Changed -= RefuseNotice; }
            var accepted = (await Store.GetRatesAsync(ct)).Single();
            if (accepted.Rate != precise || rates.RateText != "" || rates.CanAdd || rates.Error != Translator["Rates_SavedFollowUpFailed"])
                throw new IOException("Actual accepted rate outcome was lost or rearmed.");
            await Checkpoint("rate-native-accepted-notification-failed"); await observe(page, "rate-native-accepted-notification-failed");
            // This is an explicit read-only native retry, never another Add.
            await invoke(page, rates.ReloadCommand, null); if (rates.ReloadCommand.ExecutionTask is { } reload) await reload.WaitAsync(ct);
            if (rates.Error is not null || (await Store.GetRatesAsync(ct)).Single().Id != accepted.Id) throw new IOException("Actual read retry changed rate identity.");
            await Checkpoint("rate-native-read-retried"); await observe(page, "rate-native-read-retried");
            rates.RateText = "2.5";
            var review = await Store.ReadManualRatesAsync(ct); accepted.Rate = precise * 2;
            await Store.SaveManualRateAsync(review, accepted, () => true, ct); // One deliberate later version tests the old original review.
            await Checkpoint("rate-deliberate-later-version");
            await invoke(page, rates.AddCommand, null); if (rates.AddCommand.ExecutionTask is { } refused) await refused.WaitAsync(ct);
            if (rates.Error != Translator["Rates_SourceChanged"] || (await Store.GetRatesAsync(ct)).Single().Rate != precise * 2)
                throw new IOException("Actual old native rate review was not refused.");
            await Checkpoint("rate-native-stale-refused"); await Leave();
            rates = NumericRates(); page = new RatesPage(rates); route = await ShowAsync(page); await WaitAsync(() => rates.CanEdit && page.Width > 0, ct);
            rates.FromCurrency = "GBP"; rates.ToCurrency = "EUR"; rates.RateText = "1.5";
            var before = await AllMoneyRowsAsync(ct);
            await using (var db = await Factory.CreateDbContextAsync(ct))
                await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER NumericRefusal BEFORE INSERT ON ExchangeRates BEGIN SELECT RAISE(ABORT, 'Fictitious native rate refusal'); END;", ct);
            try { await invoke(page, rates.AddCommand, null); if (rates.AddCommand.ExecutionTask is { } failed) await failed.WaitAsync(ct); }
            finally { await using var db = await Factory.CreateDbContextAsync(ct); await db.Database.ExecuteSqlRawAsync("DROP TRIGGER NumericRefusal;", ct); }
            if (rates.Error != Translator["Rates_ChangeFailed"] || rates.RateText != "1.5" || before != await AllMoneyRowsAsync(ct))
                throw new IOException("Actual native rate failure changed rows or lost raw correction.");
            await Checkpoint("rate-native-persistence-refused"); await Leave();
            await File.WriteAllTextAsync(Path.Combine(_folder, NumericActions), JsonSerializer.Serialize(new NumericActionsProof(_numericTypeId, await AllMoneyRowsAsync(ct), NumericFeedback)), ct);
        }
        finally { if (route is not null) await Leave(); await SetTabMoneyCultureAsync(culture, ct); }
    }
    /// <summary>Shows source-proven feedback categories and complete stored precision without repeating the original native actions.</summary>
    internal async Task PhysicalRateFeedbackAsync(string context, Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool> completed)
    {
        var before = await AllMoneyRowsAsync(ct);
        foreach (var key in NumericFeedback)
        {
            var stage = context + "-" + key; if (completed(stage)) continue;
            var model = NumericRates(); var page = new RatesPage(model); var route = await ShowAsync(page);
            try
            {
                await WaitAsync(() => model.CanEdit && page.Width > 0, ct); model.ShowRetainedFeedback(key);
                if (model.CanEdit || model.CanReload || model.CanAdd) throw new IOException("Retained caption review must be read-only.");
                await observe(page, stage);
            }
            finally { await ReturnMoneyTabAsync(false, ct); Routing.UnRegisterRoute(route); _routes.Remove(route); }
        }
        if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Read-only native feedback changed original rows.");
    }
    /// <summary>Confirms Cancel and then one explicit original delete, after all full-precision observations are complete.</summary>
    internal async Task FinishPhysicalRateActionsAsync(Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct)
    {
        var final = Path.Combine(_folder, "rate-native-delete-accepted.json");
        if (File.Exists(final)) throw new IOException("Original final delete already accepted; do not repeat it.");
        var model = NumericRates(); var page = new RatesPage(model); var route = await ShowAsync(page);
        try
        {
            await WaitAsync(() => model.CanEdit && page.Width > 0, ct);
            var row = model.Rates.Single(); var before = await AllMoneyRowsAsync(ct);
            await invoke(page, model.DeleteCommand, false); if (model.DeleteCommand.ExecutionTask is { } cancel) await cancel.WaitAsync(ct);
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Native delete Cancel changed original rows.");
            await File.WriteAllTextAsync(Path.Combine(_folder, "rate-native-delete-cancelled.json"), before, ct);
            await observe(page, "rate-native-delete-cancelled");
            await invoke(page, model.DeleteCommand, true); if (model.DeleteCommand.ExecutionTask is { } delete) await delete.WaitAsync(ct);
            if ((await Store.GetRatesAsync(ct)).Count != 0 || model.Rates.Count != 0 || (await Store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                throw new IOException("Actual native rate delete changed finance or retained the original rate.");
            await File.WriteAllTextAsync(final, await AllMoneyRowsAsync(ct), ct);
            await observe(page, "rate-native-delete-accepted");
        }
        finally { await ReturnMoneyTabAsync(false, ct); Routing.UnRegisterRoute(route); _routes.Remove(route); }
    }

    /// <summary>Checks only the newly repaired original purity-guide publication after hiding and restoring its actual kind.</summary>
    internal async Task PhysicalRestoredGuideAsync(Func<Page, string, Task> observe, CancellationToken ct)
    {
        var before = await AllMoneyRowsAsync(ct); var culture = Localization.FormattingCultureName; string? route = null; var pending = false;
        try
        {
            await SetTabMoneyCultureAsync("de-DE", ct);
            var model = new AssetTypeEditorViewModel(NumericHoldings, Store, Translator, Localization, new MoneyInteraction());
            var page = new AssetTypeEditorPage(model); route = await ShowAsync(page, new() { ["id"] = _numericTypeId });
            await WaitAsync(() => model.CanEdit && page.Width > 0, ct);
            model.PurityIndex = Vafadar.Zanance.Core.Holdings.Purity.Common.Count; model.PurityText = ""; model.PurityText = "18,5";
            model.KindIndex = (int)Vafadar.Zanance.Core.Holdings.AssetKind.CountableItem;
            Localization.SetFormattingCulture("en-US"); pending = true;
            model.KindIndex = (int)Vafadar.Zanance.Core.Holdings.AssetKind.CoinOrBar;
            await WaitAsync(() => Views(page).OfType<Label>().Any(label => label.IsVisible && label.Text == model.PurityFormatNote && label.Text is not null), ct);
            if (model.PurityFormatNote is null || model.PurityText != "18,5") throw new IOException("Original restored purity guide was not republished.");
            await observe(page, "physical-restored-guide");
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Native restored guide wrote original rows.");
        }
        finally
        {
            if (route is not null) { await ReturnMoneyTabAsync(pending, ct); Routing.UnRegisterRoute(route); _routes.Remove(route); }
            await SetTabMoneyCultureAsync(culture, ct);
        }
    }

}
#endif
