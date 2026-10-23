using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Allows only the declared fictional Undo consent; no file, export, prompt or unrelated navigation.</summary>
    private sealed class GroupUndoInteraction : IAppInteraction, IDataFileInteraction
    {
        internal int Confirmations;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        { Confirmations++; return Task.FromResult(true); }
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected group alert.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("No group prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("No group menu.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("No group route.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected original group failure.", exception);
        public Task<PickedDataFile?> PickCsvAsync(string title) => throw new IOException("Source is already a declared fictional import.");
        public Task<bool> ConfirmExportAsync(string reason) => throw new IOException("No group export.");
        public Task ShareCsvAsync(string name, string title, string content) => throw new IOException("No group share.");
    }

    private sealed record GroupUndoSource(Guid BatchId, string ExpectedRows, bool Conflict);

    /// <summary>Checks original trimmed native journals, history buttons and translated Undo outcomes without touching Main.</summary>
    private static async Task<object> ReviewImportGroupUndoAsync(App app, IServiceProvider services, string root,
        string comparison, CancellationToken ct)
    {
        if (AppInfo.Current.PackageName != Package) throw new IOException("Independent grouped import identity required.");
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var original = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var clock = services.GetRequiredService<TimeProvider>(); var results = new List<JsonElement>();
        foreach (var name in new[] { "later-fee", "existing-transfer", "closed-group", "holding-original", "holding-changed", "legacy-external" })
        {
            var folder = Path.Combine(root, name); Directory.CreateDirectory(folder);
            var complete = Path.Combine(folder, "complete.json");
            if (File.Exists(complete)) { results.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(complete, ct))); continue; }
            var path = Path.Combine(folder, "original.db"); var exists = File.Exists(path);
            await using var fixture = new ServiceCollection().AddSingleton(clock).AddZananceData(path).BuildServiceProvider();
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            var marker = Path.Combine(folder, "source.json"); GroupUndoSource source;
            if (File.Exists(marker)) source = JsonSerializer.Deserialize<GroupUndoSource>(await File.ReadAllTextAsync(marker, ct)) ?? throw new IOException("Original group source missing.");
            else
            {
                if (exists) throw new IOException("Retain partial grouped source; never reseed an accepted setup.");
                var day = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
                var account = new Account { Name = "Original group source", CurrencyCode = "EUR", OpeningDate = day.AddDays(-1), OpeningBalance = 10000 };
                var destination = new Account { Name = "Original group destination", CurrencyCode = "EUR", OpeningDate = day.AddDays(-1) };
                await store.SaveAccountAsync(account, ct); await store.SaveAccountAsync(destination, ct); await store.GetSettingsAsync(ct);
                var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = account.Id, ToAccountId = destination.Id,
                    Amount = 2500, ToAmount = 2500, Date = day, GroupId = Guid.NewGuid(), Title = "Original group transfer" };
                AssetEvent? item = null;
                if (name.StartsWith("holding-", StringComparison.Ordinal))
                {
                    var type = new AssetType { Name = "Original counted holding", Kind = AssetKind.CountableItem,
                        Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
                    if (!await holdings.SaveTypeAsync(type, ct)) throw new IOException("Original holding type refused.");
                    var location = await holdings.EnsureDefaultLocationAsync("Original location", ct);
                    item = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Date = day,
                        Kind = AssetEventKind.Purchase, Quantity = 1000, BasisAmount = 2500, Note = "Original holding note" };
                    transfer = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = account.Id, Amount = 2500, Date = day };
                    if (!(await holdings.SaveEventAsync(item, [transfer], ct)).Succeeded) throw new IOException("Original holding source refused.");
                }
                else if (name is "existing-transfer" or "legacy-external")
                    if ((await store.SaveEntryAsync(transfer, ct)).Errors.Count != 0) throw new IOException("Original transfer refused.");
                var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
                var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = day, Amount = 100,
                    GroupId = transfer.GroupId, Title = "Original group fee" };
                var imported = await store.ImportAsync(name == "later-fee" ? [transfer] : name == "closed-group" ? [transfer, fee] : [fee], ct);
                if (imported.BatchId is not { } batch || imported.Errors.Count != 0 || imported.Conflict is not null)
                    throw new IOException("Original grouped source import refused.");
                if (name == "later-fee")
                {
                    var parent = (await store.GetEntryAsync(transfer.Id, ct))!;
                    if ((await store.SaveEntriesAsync([parent, fee], [], ct)).Errors.Count != 0) throw new IOException("Later original fee refused.");
                }
                if (name == "holding-changed")
                {
                    item = (await holdings.GetEventsAsync(cancellationToken: ct)).Single(); item.Note = "Later original holding note";
                    if (!(await holdings.SaveEventAsync(item, await store.GetGroupAsync(item.GroupId!.Value, ct), ct)).Succeeded)
                        throw new IOException("Original holding metadata correction refused.");
                }
                if (name == "legacy-external")
                {
                    await using var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                    var journal = await db.ImportLinks.SingleAsync(ct); var state = JsonNode.Parse(journal.StateJson)!.AsObject();
                    state["Version"] = 1; state.Remove("GroupDependencyDigest"); journal.StateJson = state.ToJsonString(); await db.SaveChangesAsync(ct);
                }
                var conflict = name is "later-fee" or "holding-changed" or "legacy-external";
                source = new(batch, conflict ? await ImportFinancialNativeReview.StoredAsync(fixture, ct) : before, conflict);
                await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(source), ct);
            }
            var interaction = new GroupUndoInteraction();
            var vm = new ImportExportViewModel(store, text, new DateFormatter(local), local, clock, holdings, interaction, interaction);
            await vm.LoadAsync();
            var outcomeFile = Path.Combine(folder, "outcome.json");
            var retainedOutcome = File.Exists(outcomeFile)
                ? JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(outcomeFile, ct)) : (JsonElement?)null;
            if (retainedOutcome is { } saved)
            {
                vm.UndoError = saved.GetProperty("UndoError").GetString();
                vm.ImportResult = saved.GetProperty("ImportResult").GetString();
            }
            var page = new ImportExportPage(vm); await Shell.Current.Navigation.PushModalAsync(page);
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => !vm.IsBusy && PlanEditorNativeReview.Views(page).OfType<Label>().Any(label =>
                    label.Handler?.PlatformView is Android.Widget.TextView { IsShown: true, HasWindowFocus: true }), ct);
                var scroll = PlanEditorNativeReview.Views(page).OfType<ScrollView>().Single(view => view.Orientation == ScrollOrientation.Vertical);
                async Task Reveal(VisualElement target)
                {
                    var inside = false;
                    for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
                        if (ReferenceEquals(parent, scroll)) { inside = true; break; }
                    // Original result/action docks are siblings of the viewport, not scroll targets.
                    if (inside) await scroll.ScrollToAsync(target, ScrollToPosition.MakeVisible, false);
                    await Task.Delay(100, ct);
                }
                if (retainedOutcome is null)
                {
                    var row = vm.Batches.Single(batch => batch.BatchId == source.BatchId);
                    var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(value => ReferenceEquals(value.Command, vm.UndoCommand)
                        && value.CommandParameter is BatchRow batch && batch.BatchId == row.BatchId);
                    await Reveal(button);
                    if (button.Handler?.PlatformView is not Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true } native
                        || native.Context?.PackageName != Package || !native.PerformClick()) throw new IOException("Original native group Undo unavailable.");
                    if (vm.UndoCommand.ExecutionTask is { } pending) await pending;
                    await File.WriteAllTextAsync(outcomeFile, JsonSerializer.Serialize(new { vm.UndoError, vm.ImportResult, interaction.Confirmations }), ct);
                }
                var confirmations = retainedOutcome?.GetProperty("Confirmations").GetInt32() ?? interaction.Confirmations;
                if (confirmations != 1 || (source.Conflict ? vm.UndoError != text["Import_UndoConflict"] : vm.ImportResult != text["Import_UndoDone"]))
                    throw new IOException("Original grouped Undo outcome differs.");
                if (source.ExpectedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Grouped Undo changed the wrong original rows.");
                var caption = source.Conflict ? vm.UndoError : vm.ImportResult;
                var label = PlanEditorNativeReview.Views(page).OfType<Label>().Single(value => value.Text == caption && value.IsVisible);
                await Reveal(label);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true } rendered || rendered.Text != caption)
                    throw new IOException("Original group feedback unavailable.");
                var result = JsonSerializer.SerializeToElement(new { Case = name, Conflict = source.Conflict, OriginalRowsMatch = true,
                    NativeUndoActions = 1, NativeActionsThisPass = retainedOutcome is null ? 1 : 0,
                    RetainedOutcomeForReadOnlyDisplay = retainedOutcome is not null, FictitiousConsent = true, IndependentFixtureOnly = true });
                await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(result), ct); results.Add(result);
            }
            finally { if (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page)) await Shell.Current.Navigation.PopModalAsync(); }
        }
        if (original != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Independent base rows changed.");
        return new { Comparison = comparison, Cases = results, OriginalBaseRowsUnchanged = true, FinancialFixtureWritesOnly = true };
    }
}
