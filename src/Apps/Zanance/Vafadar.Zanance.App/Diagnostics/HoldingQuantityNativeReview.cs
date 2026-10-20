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
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original quantity refusals and same-form corrections over separately owned actual application stores.</summary>
internal static class HoldingQuantityNativeReview
{
    private sealed class Dialogs : IAppInteraction
    {
        public int Navigations { get; private set; }
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected quantity alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Unexpected quantity confirmation.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected quantity prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected quantity menu.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        { if (route != "..") throw new IOException("Unexpected quantity route."); Navigations++; return Task.CompletedTask; }
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected quantity failure.", exception);
    }

    /// <summary>Invokes each original refused/corrected holding command once; presentation reuses retired raw drafts only.</summary>
    internal static async Task<HoldingRefundNativeReview.Prepared> PrepareAsync(IServiceProvider services, string folder,
        Func<AssetEventEditorPage, Task> show, Func<AssetEventEditorPage, ICommand, Task> invoke,
        Func<AssetEventEditorPage, Task> close,
        Func<AssetEventEditorPage, AssetEventEditorViewModel, string, string?, Task<object>> observe, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent quantity review identity required.");
        Directory.CreateDirectory(folder);
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var holdingText = services.GetRequiredService<HoldingText>(); var time = services.GetRequiredService<TimeProvider>();
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var states = new List<HoldingRefundNativeReview.State>(); var cases = new List<object>(); var live = new List<object>();
        foreach (var total in new[] { false, true })
        {
            var name = total ? "total-range" : "raw-range"; var raw = total ? "9223372036854775" : "79228162514264337593543950335";
            const string errorKey = "AssetEvent_QuantityTooLarge";
            var file = Path.Combine(folder, name + ".db");
            if (File.Exists(file)) throw new IOException("Original quantity fixture exists; preserve accepted cases.");
            await using var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            await store.EnsureDefaultCategoriesAsync(ct); var settings = await store.GetSettingsAsync(ct);
            settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
            var cash = new Account { Name = "D412 original cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 90000 };
            await store.SaveAccountAsync(cash, ct);
            var type = new AssetType { Name = "D412 original holding", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
            await holdings.SaveTypeAsync(type, ct); var place = await holdings.EnsureDefaultLocationAsync("D412 original place", ct);
            var original = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Kind = AssetEventKind.Purchase,
                Date = day, Quantity = 1000, BasisAmount = 2500, Note = "D412 original source metadata" };
            var payment = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Date = day, Amount = 2500, Title = type.Name };
            var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Date = day, Amount = 100,
                CategoryId = await holdings.FeesCategoryAsync(ct), Title = "D412 original fee", Note = "D412 original fee metadata" };
            if (!(await holdings.SaveEventAsync(original, [payment, fee], true, ct)).Succeeded) throw new IOException("Original quantity source seed failed.");
            var refund = EntryActions.CreateRefund(fee, 75, cash.Id, day); refund.Note = "D412 original linked refund"; refund.Tags = ["retained"];
            if (!(await store.SaveEntryAsync(refund, ct)).Succeeded) throw new IOException("Original quantity linked refund seed failed.");
            var originalMoney = JsonSerializer.Serialize(await store.GetEntriesAsync(cancellationToken: ct));
            var originalEvent = JsonSerializer.Serialize((await holdings.GetEventsAsync(cancellationToken: ct)).Single());
            var originalPrices = JsonSerializer.Serialize(await holdings.GetValuationsAsync(cancellationToken: ct));
            AssetEventEditorViewModel Model(Dialogs port) => new(holdings, store, holdingText, text, local, time, port);
            async Task Load(AssetEventEditorViewModel model) => await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Correction" });
            void RawDraft(AssetEventEditorViewModel model)
            { model.Date = day; model.QuantityText = raw; model.ReasonText = "D412 original measured correction"; model.Note = "D412 original " + name; }
            string Draft(AssetEventEditorViewModel model) => JsonSerializer.Serialize(new
            { model.QuantityText, model.Note, model.Date, model.ReasonText, model.DirectionIndex, model.KindIndex, model.UnitIndex,
                Location = model.Location?.Id, Account = model.Account?.Id });
            async Task Invoke(AssetEventEditorPage page, IAsyncRelayCommand command)
            {
                await invoke(page, command); var deadline = DateTime.UtcNow.AddSeconds(10);
                while (command.ExecutionTask is null)
                { if (DateTime.UtcNow >= deadline) throw new IOException("Observe the original quantity invocation without replay."); await Task.Delay(20, ct); }
                await command.ExecutionTask;
            }
            var port = new Dialogs(); var model = Model(port); await Load(model); RawDraft(model);
            var rawDraft = Draft(model); var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            if (model.EffectText is not null) throw new IOException("Unrepresentable quantity preview guessed a result.");
            var page = new AssetEventEditorPage(model); await show(page); await Invoke(page, model.SaveCommand);
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || model.Error != text[errorKey]
                || !model.CanSave || !model.CanEdit || !model.IsDirty || port.Navigations != 0 || Draft(model) != rawDraft)
                throw new IOException("Original quantity refusal lost rows, raw draft or correction authority.");
            live.Add(await observe(page, model, name + "-refused", errorKey));
            await File.WriteAllTextAsync(Path.Combine(folder, name + "-refused.json"), JsonSerializer.Serialize(new
            { Name = name, OriginalRawDraft = rawDraft, CompleteOriginalRowsUnchanged = true, OriginalReviewRetained = true,
                PreviewOmitted = true, ErrorKey = errorKey, Package = package, ProcessId = Environment.ProcessId }), ct);
            model.QuantityText = "2"; await Invoke(page, model.SaveCommand);
            var events = await holdings.GetEventsAsync(type.Id, ct); var added = events.Single(e => e.Id != original.Id);
            var rows = await store.GetEntriesAsync(cancellationToken: ct); var basis = HoldingsLedger.Basis(events, type.Id);
            if (events.Count != 2 || added.Kind != AssetEventKind.Correction || !added.IsIncrease || added.Quantity != 2000
                || added.GroupId is not null || added.BasisAmount is not null || added.ProceedsAmount is not null
                || added.Reason != model.ReasonText || added.Note != model.Note || added.Date != day
                || JsonSerializer.Serialize(events.Single(e => e.Id == original.Id)) != originalEvent
                || JsonSerializer.Serialize(rows) != originalMoney || JsonSerializer.Serialize(await holdings.GetValuationsAsync(cancellationToken: ct)) != originalPrices
                || HoldingsLedger.Quantity(events, type.Id, day) != 3000 || basis.Quantity != 3000 || basis.Basis != 2500
                || LedgerCalculator.Balance(cash, rows, day) != 87475 || model.CanSave || model.CanEdit || model.IsDirty
                || model.CloseText != text["Common_Close"] || port.Navigations != 1)
                throw new IOException("Original quantity correction changed source money, basis, price or accepted outcome.");
            live.Add(await observe(page, model, name + "-accepted", null));
            var result = new { Name = name, RawDraftRetainedOnRefusal = true, CompleteRowsUnchangedOnRefusal = true,
                SameOriginalFormCorrectedAndAcceptedOnce = true, OriginalFinancialRowsAndRefundUnchanged = true,
                OriginalSourceEventAndPricesUnchanged = true, QuantityBaseUnits = 3000, RemainingBasisMinor = 2500,
                RecordedBalanceMinor = 87475, AddedHoldingEvents = 1, FinancialRowsCreatedByCorrection = 0,
                AcceptedFormRetired = true, Package = package, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, name + "-accepted.json"), JsonSerializer.Serialize(result), ct);
            cases.Add(result); await close(page);
            if (!total)
            {
                // Exact original refusal presentation has no write authority after the accepted correction.
                var presentation = Model(new Dialogs()); var unchanged = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
                await Load(presentation); RawDraft(presentation); presentation.RetireReview(); presentation.Error = text[errorKey];
                if (Draft(presentation) != rawDraft || presentation.CanSave || unchanged != await ImportFinancialNativeReview.StoredAsync(fixture, ct))
                    throw new IOException("Original quantity presentation rehydration changed its retained draft or rows.");
                states.Add(new(name, presentation, errorKey));
            }
        }
        return new(states, cases, live);
    }
    /// <summary>Rehydrates only original accepted-case refusal presentation after a field-placement change, without invoking a writer.</summary>
    internal static async Task<HoldingRefundNativeReview.Prepared> ContinueAsync(IServiceProvider services, string financial,
        JsonElement completed, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent original quantity continuation required.");
        var originalCases = completed.GetProperty("Cases").EnumerateArray().ToArray();
        var originalLive = completed.GetProperty("Live").EnumerateArray().ToArray();
        if (originalCases.Length != 2 || originalLive.Length != 4) throw new IOException("Original completed quantity cases required.");
        foreach (var observation in originalLive)
        {
            var refused = observation.GetProperty("Stage").GetString()!.EndsWith("-refused", StringComparison.Ordinal);
            if (observation.GetProperty("ActualNativeSaveEnabled").GetBoolean() != refused
                || !observation.GetProperty("OriginalLiveOutcome").GetBoolean()
                || !observation.GetProperty("NoFinancialInvocationByObserver").GetBoolean())
                throw new IOException("Original quantity native outcomes differ.");
        }
        var states = new List<HoldingRefundNativeReview.State>(); var cases = new List<object>();
        foreach (var name in new[] { "raw-range", "total-range" })
        {
            using var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(financial, name + "-accepted.json"), ct));
            using var refused = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(financial, name + "-refused.json"), ct));
            var result = accepted.RootElement; var original = originalCases.Single(item => item.GetProperty("Name").GetString() == name);
            if (result.GetRawText() != original.GetRawText() || !result.GetProperty("SameOriginalFormCorrectedAndAcceptedOnce").GetBoolean()
                || !result.GetProperty("OriginalFinancialRowsAndRefundUnchanged").GetBoolean()
                || !result.GetProperty("OriginalSourceEventAndPricesUnchanged").GetBoolean()
                || result.GetProperty("QuantityBaseUnits").GetInt64() != 3000 || result.GetProperty("RecordedBalanceMinor").GetInt64() != 87475
                || !refused.RootElement.GetProperty("CompleteOriginalRowsUnchanged").GetBoolean())
                throw new IOException("Exact original quantity accepted/refused receipts required.");
            var file = Path.Combine(financial, name + ".db");
            if (!File.Exists(file) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Original owned quantity file required.");
            await using var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(services.GetRequiredService<TimeProvider>()).BuildServiceProvider();
            var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            var type = (await holdings.GetTypesAsync(ct)).Single(); var events = await holdings.GetEventsAsync(type.Id, ct);
            var rows = await store.GetEntriesAsync(cancellationToken: ct); var account = (await store.GetAccountsAsync(cancellationToken: ct)).Single();
            var payment = rows.Single(item => item.Kind == EntryKind.AssetPurchase); var fee = rows.Single(item => item.Kind == EntryKind.Expense);
            var refund = rows.Single(item => item.Kind == EntryKind.Refund); var source = events.Single(item => item.Kind == AssetEventKind.Purchase);
            var correction = events.Single(item => item.Kind == AssetEventKind.Correction);
            var prices = await holdings.GetValuationsAsync(cancellationToken: ct);
            if (rows.Count != 3 || events.Count != 2 || payment.Amount != 2500 || fee.Amount != 100 || refund.Amount != 75
                || refund.RefundOfId != fee.Id || refund.Note != "D412 original linked refund" || !refund.Tags.SequenceEqual(new[] { "retained" })
                || payment.GroupId != fee.GroupId || source.Quantity != 1000 || source.BasisAmount != 2500
                || correction.Quantity != 2000 || !correction.IsIncrease || correction.GroupId is not null
                || HoldingsLedger.Quantity(events, type.Id, DateOnly.MaxValue) != 3000 || HoldingsLedger.Basis(events, type.Id).Basis != 2500
                || LedgerCalculator.Balance(account, rows, source.Date) != 87475 || prices.Count != 1 || prices[0].PricePerUnitMilli != 2500000)
                throw new IOException("Original accepted quantity rows differ.");
            if (name == "raw-range")
            {
                var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
                var model = new AssetEventEditorViewModel(holdings, store, services.GetRequiredService<HoldingText>(), text, local,
                    services.GetRequiredService<TimeProvider>(), new Dialogs());
                await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Correction" });
                using var raw = JsonDocument.Parse(refused.RootElement.GetProperty("OriginalRawDraft").GetString()!); var draft = raw.RootElement;
                model.Date = JsonSerializer.Deserialize<DateOnly>(draft.GetProperty("Date").GetRawText());
                model.QuantityText = draft.GetProperty("QuantityText").GetString()!; model.Note = draft.GetProperty("Note").GetString()!;
                model.ReasonText = draft.GetProperty("ReasonText").GetString()!;
                model.DirectionIndex = draft.GetProperty("DirectionIndex").GetInt32(); model.UnitIndex = draft.GetProperty("UnitIndex").GetInt32();
                model.Location = model.Locations.Single(place => place.Id == draft.GetProperty("Location").GetGuid());
                model.Account = model.Accounts.Single(item => item.Id == draft.GetProperty("Account").GetGuid());
                model.RetireReview(); model.Error = text["AssetEvent_QuantityTooLarge"];
                var retained = JsonSerializer.Serialize(new { model.QuantityText, model.Note, model.Date, model.ReasonText,
                    model.DirectionIndex, model.KindIndex, model.UnitIndex, Location = model.Location?.Id, Account = model.Account?.Id });
                if (retained != refused.RootElement.GetProperty("OriginalRawDraft").GetString() || model.CanSave || model.FormError is not null
                    || model.QuantityError != model.Error || model.EffectText is not null)
                    throw new IOException("Original retired quantity draft differs.");
                states.Add(new(name, model, "AssetEvent_QuantityTooLarge"));
            }
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Read-only quantity continuation changed original rows.");
            cases.Add(result.Clone());
        }
        return new(states, cases, []);
    }

}
#endif
