#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original refunded-fee corrections once, followed only by retained raw-error caption presentation.</summary>
internal static class HoldingRefundNativeReview
{
    /// <summary>Exact rejected draft rehydrated read-only from its original receipt; no financial authority is claimed.</summary>
    internal sealed record State(string Name, AssetEventEditorViewModel Model, string ErrorKey);
    /// <summary>Original live observations, accepted results and presentation-only states.</summary>
    internal sealed record Prepared(IReadOnlyList<State> States, IReadOnlyList<object> Cases, IReadOnlyList<object> Live);
    /// <summary>Reads only the original production field feedback for retained caption observation.</summary>
    internal static string? Feedback(AssetEventEditorViewModel model, string errorKey) =>
        errorKey == "AssetEvent_QuantityTooLarge" ? model.Error : errorKey == "LedgerError_RefundCurrencyMismatch" ? model.AccountError : model.FeeError;
    /// <summary>Rehydrates only a recorded rejected draft's field caption, never financial authority.</summary>
    internal static void SetFeedback(AssetEventEditorViewModel model, string errorKey, string value)
    { if (errorKey == "AssetEvent_QuantityTooLarge") model.Error = value; else if (errorKey == "LedgerError_RefundCurrencyMismatch") model.AccountError = value; else model.FeeError = value; }
    private sealed class Dialogs : IAppInteraction
    {
        public int Navigations { get; private set; }
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected refunded-fee alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Unexpected refunded-fee confirmation.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected refunded-fee prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected refunded-fee menu.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        { if (route != "..") throw new IOException("Unexpected refunded-fee route."); Navigations++; return Task.CompletedTask; }
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected refunded-fee failure.", exception);
    }

    /// <summary>Uses fresh owned independent fixtures and never replays completed financial commands for presentation.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder,
        Func<AssetEventEditorPage, Task> show, Func<AssetEventEditorPage, ICommand, Task> invoke,
        Func<AssetEventEditorPage, Task> close,
        Func<AssetEventEditorPage, AssetEventEditorViewModel, string, string?, Task<object>> observe, CancellationToken ct, bool feeInput = false)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent refunded-fee review identity required.");
        Directory.CreateDirectory(folder);
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var holdingText = services.GetRequiredService<HoldingText>(); var time = services.GetRequiredService<TimeProvider>();
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime); var states = new List<State>(); var cases = new List<object>(); var live = new List<object>();
        foreach (var currency in new[] { false, true })
        {
            var name = feeInput ? (currency ? "negative" : "malformed") : currency ? "currency" : "amount";
            var errorKey = feeInput ? "Amount_Invalid" : currency ? "LedgerError_RefundCurrencyMismatch" : "LedgerError_RefundExceedsPurchase";
            var file = Path.Combine(folder, name + ".db");
            if (File.Exists(file)) throw new IOException("Original refunded-fee fixture exists; preserve accepted cases.");
            await using var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            await store.EnsureDefaultCategoriesAsync(ct); var settings = await store.GetSettingsAsync(ct);
            settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
            var eur = new Account { Name = "D409 EUR cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 90000 };
            var usd = new Account { Name = "D409 USD cash", CurrencyCode = "USD", OpeningDate = day };
            await store.SaveAccountAsync(eur, ct); await store.SaveAccountAsync(usd, ct);
            var type = new AssetType { Name = "D409 original item", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
            await holdings.SaveTypeAsync(type, ct); var place = await holdings.EnsureDefaultLocationAsync("D409 original place", ct);
            var item = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Kind = AssetEventKind.Purchase,
                Date = day, Quantity = 1000, BasisAmount = 2500, Note = "D409 original holding note" };
            var payment = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = eur.Id, Date = day, Amount = 2500, Title = type.Name };
            var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = eur.Id, Date = day, Amount = 100,
                CategoryId = await holdings.FeesCategoryAsync(ct), Title = "D409 original fee", Note = "D409 fee metadata" };
            if (!(await holdings.SaveEventAsync(item, [payment, fee], true, ct)).Succeeded) throw new IOException("Original refunded-fee seed failed.");
            var refund = EntryActions.CreateRefund(fee, 75, eur.Id, day); refund.Note = "D409 retained refund"; refund.Tags = ["D409", "keep"];
            if (!(await store.SaveEntryAsync(refund, ct)).Succeeded) throw new IOException("Original linked fee refund failed.");
            var originalRefund = (await store.GetEntryAsync(refund.Id, ct))!;
            AssetEventEditorViewModel Model(Dialogs port) => new(holdings, store, holdingText, text, local, time, port);
            async Task Load(AssetEventEditorViewModel model) => await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = item.Id });
            void RejectedDraft(AssetEventEditorViewModel model)
            {
                model.Note = "D409 original rejected draft";
                if (feeInput) model.FeeText = currency ? "-" + MoneyText.ForInput(100, "EUR", local.CurrentCulture) : "not money";
                else if (currency) model.Account = model.Accounts.Single(a => a.Id == usd.Id);
                else model.FeeText = MoneyText.ForInput(50, "EUR", local.CurrentCulture);
            }
            string Draft(AssetEventEditorViewModel model) => JsonSerializer.Serialize(new
            { model.QuantityText, model.AmountText, model.FeeText, model.Note, model.SecondAmountText, model.Date,
                Account = model.Account?.Id, Location = model.Location?.Id, model.PriceModeIndex });
            async Task Invoke(AssetEventEditorPage page, IAsyncRelayCommand command)
            {
                await invoke(page, command); var deadline = DateTime.UtcNow.AddSeconds(10);
                while (command.ExecutionTask is null)
                { if (DateTime.UtcNow >= deadline) throw new IOException("Observe the original refunded-fee invocation without replay."); await Task.Delay(20, ct); }
                await command.ExecutionTask;
            }
            var port = new Dialogs(); var model = Model(port); await Load(model); RejectedDraft(model);
            var originalDraft = Draft(model); var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            var page = new AssetEventEditorPage(model); await show(page); await Invoke(page, model.SaveCommand);
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || Feedback(model, errorKey) != text[errorKey]
                || !model.CanSave || !model.CanEdit || model.IsBusy || port.Navigations != 0 || Draft(model) != originalDraft)
                throw new IOException("Original refunded-fee refusal did not preserve rows, raw draft and retry authority.");
            live.Add(await observe(page, model, name + "-refused", errorKey));
            await File.WriteAllTextAsync(Path.Combine(folder, name + "-refused.json"), JsonSerializer.Serialize(new
            { CompleteOriginalRowsUnchanged = true, OriginalRawDraft = originalDraft, OriginalRawDraftKept = true,
                OriginalCanCorrectSameForm = true, ErrorKey = errorKey, Package = package, ProcessId = Environment.ProcessId }), ct);
            // Correct the same original live form; its rejected command did not consume the original review.
            if (feeInput) model.FeeText = MoneyText.ForInput(100, "EUR", local.CurrentCulture);
            else if (currency) model.Account = model.Accounts.Single(a => a.Id == eur.Id);
            else model.FeeText = MoneyText.ForInput(75, "EUR", local.CurrentCulture);
            model.Note = "D409 explicit corrected " + name;
            await Invoke(page, model.SaveCommand);
            var rows = await store.GetEntriesAsync(cancellationToken: ct);
            var kept = rows.Single(e => e.Id == refund.Id); var savedFee = rows.Single(e => e.Id == fee.Id);
            var events = await holdings.GetEventsAsync(cancellationToken: ct); var valuations = await holdings.GetValuationsAsync(cancellationToken: ct);
            var balance = LedgerCalculator.Balance(eur, rows, day); var totals = LedgerCalculator.Totals([eur], rows, new(day, day)).Single();
            var expectedFee = feeInput || currency ? 100 : 75; var expectedBalance = feeInput || currency ? 87475 : 87500;
            if (rows.Count != 3 || savedFee.Amount != expectedFee || savedFee.AccountId != eur.Id || kept.RefundOfId != fee.Id
                || JsonSerializer.Serialize(kept) != JsonSerializer.Serialize(originalRefund) || balance != expectedBalance
                || totals.NetIncome != 0 || totals.NetExpense != expectedFee - 75 || events.Count != 1 || events.Single().Quantity != 1000
                || events.Single().BasisAmount != 2500 || valuations.Count != 1 || valuations.Single().PricePerUnitMilli != 2500000
                || model.CanSave || model.CanEdit || model.IsDirty || model.CloseText != text["Common_Close"] || port.Navigations != 1)
                throw new IOException("Original same-form fee correction did not preserve exact quantity, money, price and refund identity.");
            live.Add(await observe(page, model, name + "-accepted", null));
            var result = new { Kind = name, OriginalStaleOrGuessedMerge = false, CompleteRowsUnchangedOnRefusal = true,
                RawDraftKept = true, SameOriginalFormCorrectedAndAcceptedOnce = true, RefundIdentityAmountLinkCurrencyAndMetadataPreserved = true,
                FeeMinor = expectedFee, RefundMinor = kept.Amount, RecordedBalanceMinor = balance, IncomeMinor = totals.NetIncome,
                ExpenseMinor = totals.NetExpense, QuantityBaseUnits = events.Single().Quantity, BasisMinor = events.Single().BasisAmount,
                DerivedPriceMinorMilli = valuations.Single().PricePerUnitMilli, AcceptedFormRetired = true, Package = package, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, name + "-accepted.json"), JsonSerializer.Serialize(result), ct);
            cases.Add(result); await close(page);
            // Rehydrate only the exact original rejected draft for captions. Its financial command is never replayed.
            var presentation = Model(new Dialogs()); var beforePresentation = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            await Load(presentation); RejectedDraft(presentation); presentation.RetireReview(); SetFeedback(presentation, errorKey, text[errorKey]);
            if (Draft(presentation) != originalDraft || presentation.CanSave
                || beforePresentation != await ImportFinancialNativeReview.StoredAsync(fixture, ct))
                throw new IOException("Original rejected-draft caption rehydration changed money or raw input.");
            // D-410's two invalid inputs share one caption; retain only one read-only matrix after both original live cases.
            if (!feeInput || !currency) states.Add(new(name, presentation, errorKey));
        }
        return new(states, cases, live);
    }
}
#endif
