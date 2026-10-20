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

/// <summary>Original holding financial forms once, retaining their exact outcomes for native presentation.</summary>
internal static class HoldingEventNativeReview
{
    /// <summary>One original model remains bound to its actual source and accepted/refused result.</summary>
    internal sealed record State(string Name, AssetEventEditorViewModel Model, string ErrorKey);
    /// <summary>Original financial results and read-only presentation states.</summary>
    internal sealed record Prepared(IReadOnlyList<State> States, IReadOnlyList<object> Cases);
    private sealed class Dialogs : IAppInteraction
    {
        public int Navigations { get; private set; }
        public int Prompts { get; private set; }
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected holding alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Unexpected holding confirmation.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
        { if (++Prompts != 1 || initialValue is not null || maxLength != 60) throw new IOException("Only the original new-location prompt is supplied."); return Task.FromResult<string?>("D408 explicit new place"); }
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected holding menu.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        { if (route != "..") throw new IOException("Unexpected holding route."); Navigations++; return Task.CompletedTask; }
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected holding native failure.", exception);
    }

    /// <summary>Creates only fresh independent owned cases, never overwriting or replaying completed finance.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder,
        Func<AssetEventEditorPage, Task> show, Func<AssetEventEditorPage, ICommand, Task> invoke,
        Func<AssetEventEditorPage, Task> close, CancellationToken ct, bool retainBeforeFirstCommand = false)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent holding review identity required.");
        Directory.CreateDirectory(folder);
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var holdingText = services.GetRequiredService<HoldingText>(); var time = services.GetRequiredService<TimeProvider>();
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime); var states = new List<State>(); var cases = new List<object>();
        foreach (var sale in new[] { false, true })
        {
            var name = sale ? "sale" : "purchase"; var file = Path.Combine(folder, name + ".db");
            var retained = retainBeforeFirstCommand && !sale;
            if (File.Exists(file) != retained) throw new IOException("Preserve the original holding fixture; only an exact pre-command continuation may reuse it.");
            await using var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
            if (!retained) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            Account account; AssetType type; AssetLocation place;
            if (retained)
            {
                var accounts = await store.GetAccountsAsync(); var types = await holdings.GetTypesAsync(ct); var places = await holdings.GetLocationsAsync(ct);
                if (accounts.Count != 1 || types.Count != 1 || places.Count != 1
                    || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0 || (await holdings.GetEventsAsync(cancellationToken: ct)).Count != 0
                    || (await holdings.GetValuationsAsync(cancellationToken: ct)).Count != 0 || Directory.EnumerateFiles(folder, "*.json").Any())
                    throw new IOException("Original holding case already progressed; never replay a completed command.");
                account = accounts.Single(); type = types.Single(); place = places.Single();
                if (account.Name != "D408 fictitious purchase" || account.CurrencyCode != "EUR" || account.OpeningBalance != 90000
                    || type.Name != "D408 original item" || type.Dimension != AssetDimension.Count || type.PriceCurrencyCode != "EUR"
                    || type.Note != "D408 original type note" || place.Name != "D408 original place"
                    || (await store.GetSettingsAsync(ct)).Mode != ExperienceMode.Advanced)
                    throw new IOException("Original pre-command holding source changed; do not reseed.");
            }
            else
            {
                await store.EnsureDefaultCategoriesAsync(ct); var settings = await store.GetSettingsAsync(ct);
                settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
                account = new Account { Name = "D408 fictitious " + name, CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 90000 };
                await store.SaveAccountAsync(account, ct);
                type = new AssetType { Name = "D408 original item", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", Note = "D408 original type note" };
                await holdings.SaveTypeAsync(type, ct); place = await holdings.EnsureDefaultLocationAsync("D408 original place", ct);
                if (sale && !(await holdings.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
                    Kind = AssetEventKind.Opening, Quantity = 2000, Date = day, BasisAmount = 4000 }, [], ct)).Succeeded)
                    throw new IOException("Original independent opening failed.");
            }
            AssetEventEditorViewModel Model(Dialogs port) => new(holdings, store, holdingText, text, local, time, port);
            async Task Load(AssetEventEditorViewModel model)
            {
                await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = sale ? "Sale" : "Purchase" });
                model.QuantityText = "1"; model.AmountText = MoneyText.ForInput(sale ? 3000 : 2500, "EUR", local.CurrentCulture);
                model.FeeText = MoneyText.ForInput(100, "EUR", local.CurrentCulture); model.Note = "D408 original draft note";
                if (!model.CanSave) throw new IOException("Original holding review did not publish.");
            }
            async Task Invoke(AssetEventEditorPage page, IAsyncRelayCommand command)
            {
                await invoke(page, command); var deadline = DateTime.UtcNow.AddSeconds(10);
                while (command.ExecutionTask is null)
                { if (DateTime.UtcNow >= deadline) throw new IOException("Observe original holding invocation without replay."); await Task.Delay(20, ct); }
                await command.ExecutionTask;
            }
            string Draft(AssetEventEditorViewModel model) => JsonSerializer.Serialize(new
            { model.QuantityText, model.AmountText, model.FeeText, model.Note, Location = model.Location?.Id, Account = model.Account?.Id });
            var refusedPort = new Dialogs(); var refused = Model(refusedPort); await Load(refused);
            if (!sale)
            {
                var originalAmounts = (refused.QuantityText, refused.AmountText, refused.FeeText, refused.Note);
                var locationPage = new AssetEventEditorPage(refused); await show(locationPage); await Invoke(locationPage, refused.AddLocationCommand);
                if (refusedPort.Prompts != 1 || refused.Location?.Name != "D408 explicit new place" || !refused.CanSave
                    || originalAmounts != (refused.QuantityText, refused.AmountText, refused.FeeText, refused.Note))
                    throw new IOException("Original explicit location did not preserve raw financial inputs.");
                await File.WriteAllTextAsync(Path.Combine(folder, "explicit-location.json"), JsonSerializer.Serialize(new
                { OriginalRawDraftRetained = true, OnlyExplicitNewLocationAdopted = true, NoFinancialEventSaved = (await holdings.GetEventsAsync(cancellationToken: ct)).Count == 0 }), ct);
                await close(locationPage);
                // Closing retires that page. Use its explicit fresh original model before the source-change scenario.
                refusedPort = new Dialogs(); refused = Model(refusedPort); await Load(refused);
            }
            var draft = Draft(refused); type.Note = "D408 newer original type note"; await holdings.SaveTypeAsync(type, ct);
            var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            var refusedPage = new AssetEventEditorPage(refused); await show(refusedPage); await Invoke(refusedPage, refused.SaveCommand);
            if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || refused.Error != text["AssetEvent_SourceChanged"]
                || refused.CanSave || draft != Draft(refused) || refusedPort.Navigations != 0)
                throw new IOException("Original holding refusal did not preserve complete source and raw draft.");
            await File.WriteAllTextAsync(Path.Combine(folder, name + "-refused.json"), JsonSerializer.Serialize(new
            { OriginalSourceUnchanged = true, OriginalRawDraftKept = true, OriginalCommandRefused = true, Package = package, ProcessId = Environment.ProcessId }), ct);
            await close(refusedPage); if (!sale) states.Add(new("refused", refused, "AssetEvent_SourceChanged"));
            var acceptedPort = new Dialogs(); var accepted = Model(acceptedPort); await Load(accepted);
            var page = new AssetEventEditorPage(accepted); await show(page);
            EventHandler broken = (_, _) => throw new IOException("Deliberate original holding postcommit notification failure.");
            if (!sale) holdings.Changed += broken;
            try { await Invoke(page, accepted.SaveCommand); } finally { if (!sale) holdings.Changed -= broken; }
            var events = await holdings.GetEventsAsync(cancellationToken: ct); var entries = await store.GetEntriesAsync(cancellationToken: ct);
            var own = events.Single(e => e.Kind == (sale ? AssetEventKind.Sale : AssetEventKind.Purchase));
            var valuations = await holdings.GetValuationsAsync(cancellationToken: ct);
            var balance = LedgerCalculator.Balance(account, entries, day);
            var totals = LedgerCalculator.Totals([account], entries, new(day, day)).Single();
            if (entries.Count != 2 || own.Quantity != 1000 || own.Note != accepted.Note || balance != (sale ? 92900 : 87400)
                || totals.NetIncome != 0 || totals.NetExpense != 100 || entries.Any(e => e.GroupId != own.GroupId)
                || accepted.CanSave || accepted.CanEdit || accepted.IsDirty || accepted.CloseText != text["Common_Close"]
                || (sale ? acceptedPort.Navigations != 1 : accepted.Error != text["AssetEvent_SavedNeedsReview"]))
                throw new IOException("Original holding acceptance is not exact and terminal.");
            if ((!sale && (valuations.Count != 1 || valuations.Single().PricePerUnitMilli != 2500000)) || (sale && valuations.Count != 0))
                throw new IOException("Original holding derived price changed.");
            var result = new { Kind = name, StaleRefusalBeforeMoney = true, CompleteOriginalRowsUnchangedOnRefusal = true,
                RawDraftKept = true, FreshReviewAcceptedOnce = true, QuantityBaseUnits = own.Quantity,
                MoneyRows = entries.Count, RecordedBalanceMinor = balance, IncomeMinor = totals.NetIncome, ExpenseMinor = totals.NetExpense,
                ExactGroupAndDerivedPrice = true, AcceptedPostCommitNotificationFailure = !sale, AcceptedFormRetired = true,
                Package = package, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, name + "-accepted.json"), JsonSerializer.Serialize(result), ct);
            cases.Add(result); await close(page); if (!sale) states.Add(new("accepted", accepted, "AssetEvent_SavedNeedsReview"));
        }
        return new(states, cases);
    }
}
#endif
