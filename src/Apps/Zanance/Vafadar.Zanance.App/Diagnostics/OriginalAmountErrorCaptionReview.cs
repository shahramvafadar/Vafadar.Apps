#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Qualifies only changed original-purchase error examples on actual native editors, without accepting money.</summary>
internal static class OriginalAmountErrorCaptionReview
{
    /// <summary>Retains one empty-ledger source and completed original error observations across a failed capture.</summary>
    internal static async Task<object> RunAsync(IServiceProvider services, string root, IReadOnlyList<int> widths,
        Func<string, ThemeChoice, int, Task> context, Func<Button, Task> invoke,
        Func<ContentPage, string, IReadOnlyList<Label>, Task> observe, CancellationToken ct)
    {
        var retained = Environment.GetEnvironmentVariable("VAFADAR_ORIGINAL_AMOUNT_ERROR_SOURCE");
        var source = string.IsNullOrEmpty(retained) ? root : retained;
        var recapture = Environment.GetEnvironmentVariable("VAFADAR_ORIGINAL_AMOUNT_ERROR_RECAPTURE");
        var path = Path.Combine(source, "original-amount-errors.db"); var existed = File.Exists(path);
        if (!string.IsNullOrEmpty(retained) && !existed) throw new IOException("Original error-caption source missing.");
        await using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (!existed)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var account = new Account { Name = "Original purchase feedback account", CurrencyCode = "EUR",
                OpeningDate = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime) };
            db.Accounts.Add(account); db.Settings.Add(new ZananceSettings { Mode = ExperienceMode.Advanced,
                OnboardingCompleted = true, DefaultAccountId = account.Id });
            await db.SaveChangesAsync(ct);
        }
        var store = fixture.GetRequiredService<ZananceStore>(); var originalAccount = (await store.GetAccountsAsync(cancellationToken: ct)).Single();
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var translator = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var interaction = services.GetRequiredService<IAppInteraction>(); var time = services.GetRequiredService<TimeProvider>();
        var observations = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in widths) foreach (var currency in new[] { "USD", "JPY", "KWD" })
        {
            var key = width + "-" + language + "-" + theme + "-" + currency;
            var receipt = Path.Combine(key == recapture ? root : source, key + "-complete.json");
            if (File.Exists(receipt)) { observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt, ct))); continue; }
            await context(language, theme, width);
            var model = new EntryEditorViewModel(store, translator, local, time, new UndoService(store, time),
                new AssetEntryConfirmation(translator, interaction), interaction, fixture.GetRequiredService<ReceiptWorkStore>());
            model.ApplyQueryAttributes(new Dictionary<string, object> { ["account"] = originalAccount.Id.ToString() });
            var page = new EntryEditorPage(model).WithFlowDirection(local);
            await Shell.Current.Navigation.PushModalAsync(page);
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => page.IsLoaded && model.IsFormEditable, ct);
                model.AmountText = "1"; model.ShowDetails = true; model.ForeignEnabled = true;
                model.ForeignCurrency = currency; model.ForeignAmountText = "not-money";
                var expected = translator.Format("Amount_InvalidWithExample", MoneyText.InputExample(Currencies.Get(currency), local.CurrentCulture, null));
                var save = PlanEditorNativeReview.Views(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, model.SaveCommand));
                await invoke(save);
                await PlanEditorNativeReview.WaitAsync(() => !model.IsBusy && model.ForeignAmountError is not null, ct);
                if (model.ForeignAmountError != expected || model.ForeignAmountText != "not-money" || model.AmountText != "1"
                    || !model.IsFormEditable || !string.IsNullOrEmpty(model.SaveError))
                    throw new IOException("Original purchase error lost its currency, parser or pending draft.");
                var label = PlanEditorNativeReview.Views(page).OfType<Label>().Single(item => item.Text == expected);
                await observe(page, key, [label]);
                if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Rejected original input changed stored source rows.");
                var result = new { Language = language, Theme = theme.ToString(), Width = width, Currency = currency,
                    OriginalError = expected, OriginalRawInputUnchanged = true, OriginalRowsUnchanged = true,
                    ObservationDirectory = root, FinancialSaves = 0 };
                await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result), ct); observations.Add(result);
            }
            finally { if (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page)) await Shell.Current.Navigation.PopModalAsync(); }
        }
        if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Original base installation changed.");
        return new { Scope = "original-amount-errors", Observations = observations, OriginalBaseUnchanged = true, FinancialSaves = 0 };
    }
}
#endif
