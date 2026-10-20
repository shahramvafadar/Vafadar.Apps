#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Security.Cryptography;
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
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original type source refusal/edit/create over one independent financial file, then retired captions.</summary>
internal static class HoldingTypeNativeReview
{
    private sealed class Dialogs : IAppInteraction
    {
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(false);
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected type choice.");
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected type alert.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected type prompt.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("Original independent type return failure.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected type failure.", exception);
    }
    /// <summary>Only original raw fields and a completed caption; no financial writer or replay command.</summary>
    internal sealed record State(string Name, Guid Id, bool IsNew, bool Committed, string Key, JsonElement Raw);
    /// <summary>Exact original financial file, completed case receipts and read-only caption states.</summary>
    internal sealed record Prepared(State[] States, object[] Cases, object[] Live, ServiceProvider Fixture, string Rows);
    private static AssetTypeEditorViewModel Model(IServiceProvider services, ServiceProvider fixture) => new(
        fixture.GetRequiredService<HoldingStore>(), fixture.GetRequiredService<ZananceStore>(), services.GetRequiredService<Translator>(),
        services.GetRequiredService<ILocalizationService>(), new Dialogs());
    private static JsonElement Raw(AssetTypeEditorViewModel vm) => JsonSerializer.SerializeToElement(new
    { vm.Name, vm.KindIndex, vm.DimensionIndex, vm.MetalIndex, vm.PurityIndex, vm.PurityText,
        vm.UnitWeightText, vm.CountUnitName, vm.Divisible, vm.CurrencyCode, vm.Note });
    private static string Digest(string rows) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rows)));
    private static string ExceptTypes(string rows)
    {
        using var doc = JsonDocument.Parse(rows);
        return string.Join("\n", doc.RootElement.EnumerateObject().Where(p => p.Name != "AssetTypes").Select(p => p.Name + ":" + p.Value.GetRawText()));
    }
    /// <summary>Creates only a permanently retired caption in the current display language, using original raw fields.</summary>
    internal static async Task<AssetTypeEditorViewModel> CaptionAsync(IServiceProvider services, ServiceProvider fixture, State state)
    {
        var vm = Model(services, fixture); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = state.Id });
        var raw = state.Raw; vm.Name = raw.GetProperty("Name").GetString()!; vm.KindIndex = raw.GetProperty("KindIndex").GetInt32();
        vm.DimensionIndex = raw.GetProperty("DimensionIndex").GetInt32(); vm.MetalIndex = raw.GetProperty("MetalIndex").GetInt32();
        vm.PurityIndex = raw.GetProperty("PurityIndex").GetInt32(); vm.PurityText = raw.GetProperty("PurityText").GetString()!;
        vm.UnitWeightText = raw.GetProperty("UnitWeightText").GetString()!; vm.CountUnitName = raw.GetProperty("CountUnitName").GetString()!;
        vm.Divisible = raw.GetProperty("Divisible").GetBoolean(); vm.CurrencyCode = raw.GetProperty("CurrencyCode").GetString()!;
        vm.Note = raw.GetProperty("Note").GetString()!; vm.ShowRetainedOutcome(state.Key, state.Committed, state.IsNew);
        return vm;
    }
    /// <summary>Invokes each original native source/edit/create operation once and saves its receipt before caption observation.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder, string invocation,
        Func<AssetTypeEditorPage, Task> show, Func<AssetTypeEditorPage, ICommand, object?, Task> invoke,
        Func<AssetTypeEditorPage, Task> close, Func<AssetTypeEditorPage, string, Task<object>> observe, CancellationToken ct)
    {
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-type.db");
        if (File.Exists(file)) throw new IOException("Original type file exists; preserve its exact pending or completed receipt.");
        var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var translator = services.GetRequiredService<Translator>(); var day = new DateOnly(2026, 10, 1);
        var cash = new Account { Name = "D416 original cash", CurrencyCode = "EUR", OpeningBalance = 90000, OpeningDate = day };
        await store.SaveAccountAsync(cash, ct);
        var type = new AssetType { Name = "D416 original type", Dimension = AssetDimension.Count,
            Kind = AssetKind.CountableItem, PriceCurrencyCode = "EUR", Note = "Original metadata", Icon = "Diamond", SortOrder = 17 };
        await holdings.SaveTypeAsync(type, ct); var place = await holdings.EnsureDefaultLocationAsync("D416 original place", ct);
        var purchase = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = day, Kind = AssetEventKind.Purchase,
            Quantity = 2000, BasisAmount = 5000, Note = "Original retained purchase" };
        var payment = new LedgerEntry { AccountId = cash.Id, Kind = EntryKind.AssetPurchase, Date = day, Amount = 5000, Title = type.Name };
        var fee = new LedgerEntry { AccountId = cash.Id, Kind = EntryKind.Expense, Date = day, Amount = 200,
            CategoryId = await holdings.FeesCategoryAsync(ct), Title = "Original type fee" };
        if (!(await holdings.SaveEventAsync(purchase, [payment, fee], true, ct)).Succeeded) throw new IOException("Original type financial seed failed.");
        var refund = EntryActions.CreateRefund(fee, 150, cash.Id, day); await store.SaveEntryAsync(refund, ct);
        await store.AddAttachmentAsync(new EntryAttachment { EntryId = fee.Id, FileName = "original-type.bin", ContentType = "application/octet-stream", Data = [1, 2, 3, 4] }, ct);
        var cases = new List<object>(); var live = new List<object>(); var states = new List<State>();
        async Task Receipt(string stage, Guid id)
        {
            var receipt = new { Stage = stage, Id = id, Invocation = invocation, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(receipt), ct); cases.Add(receipt);
        }
        async Task Observe(AssetTypeEditorPage page, string stage)
        {
            try { live.Add(await observe(page, stage)); }
            catch (Exception error) when (error is not OutOfMemoryException)
            { live.Add(new { Stage = stage, CaptionPending = true, ObserverError = error.GetType().FullName, OriginalCommandsNeverReplayed = true }); }
        }
        async Task Run(AssetTypeEditorPage page, IAsyncRelayCommand command)
        {
            await invoke(page, command, null); var deadline = DateTime.UtcNow.AddSeconds(15);
            while (command.ExecutionTask is null)
            { if (DateTime.UtcNow > deadline) throw new IOException("Original native type command remains pending."); await Task.Delay(20, ct); }
            await command.ExecutionTask;
        }
        var old = Model(services, fixture); await old.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = type.Id });
        old.Name = "D416 rejected raw rename"; old.Note = "Original rejected raw note"; var originalRaw = Raw(old).GetRawText();
        var oldPage = new AssetTypeEditorPage(old); await show(oldPage);
        type.Note = "D416 concurrent metadata correction"; await holdings.SaveTypeAsync(type, ct);
        var corrected = await ImportFinancialNativeReview.StoredAsync(fixture, ct); await Run(oldPage, old.SaveCommand);
        if (corrected != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || originalRaw != Raw(old).GetRawText()
            || old.Error != translator["AssetType_SourceChanged"] || old.CanSave || old.Committed)
            throw new IOException("Original stale type source overwrote current facts or raw input.");
        await Receipt("source-refused", type.Id); await Observe(oldPage, "source-refused"); await close(oldPage);
        states.Add(new("source", type.Id, false, false, "AssetType_SourceChanged", Raw(old)));
        var edit = Model(services, fixture); await edit.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = type.Id });
        edit.Name = "D416 confirmed original rename"; var editPage = new AssetTypeEditorPage(edit); await show(editPage);
        void Fail(object? sender, EventArgs args) => throw new IOException("Original independent type notification failure");
        holdings.Changed += Fail; await Run(editPage, edit.SaveCommand); holdings.Changed -= Fail;
        if (!edit.Committed || edit.CanSave || edit.AcceptedId != type.Id || edit.IsDirty || edit.Error != translator["AssetType_SavedNeedsReview"])
            throw new IOException("Original accepted type edit lost its terminal outcome.");
        var saved = (await holdings.GetTypesAsync(ct)).Single();
        if (saved.Name != edit.Name || saved.Note != type.Note || saved.Icon != type.Icon || saved.SortOrder != type.SortOrder
            || ExceptTypes(corrected) != ExceptTypes(await ImportFinancialNativeReview.StoredAsync(fixture, ct)))
            throw new IOException("Original type edit changed retained metadata or financial rows.");
        await Receipt("edit-accepted", type.Id); await Observe(editPage, "edit-accepted"); await close(editPage);
        states.Add(new("edit-saved", type.Id, false, true, "AssetType_SavedNeedsReview", Raw(edit)));
        var create = Model(services, fixture); await create.ApplyQueryAsync(new Dictionary<string, object>());
        create.Name = "D416 original new coin"; create.KindIndex = (int)AssetKind.CoinOrBar; create.DimensionIndex = (int)AssetDimension.Count;
        create.MetalIndex = 0; create.PurityIndex = Purity.Common.Count; create.PurityText = "18"; create.UnitWeightText = "1.25";
        create.CountUnitName = "Original coin"; create.Divisible = true; create.CurrencyCode = "GBP"; create.Note = "Original retained new metadata";
        var newPage = new AssetTypeEditorPage(create); await show(newPage); await Run(newPage, create.SaveCommand);
        if (!create.Committed || create.CanSave || create.IsDirty || create.AcceptedId is not { } created || create.Error != translator["AssetType_SavedNeedsReview"])
            throw new IOException("Original accepted new type lost its stable identity or terminal outcome.");
        var all = await holdings.GetTypesAsync(ct); var coin = all.Single(t => t.Id == created);
        if (all.Count != 2 || coin.Name != create.Name || coin.PurityPer10000 != 7500 || coin.UnitWeightMg != 1250
            || coin.CountUnitName != create.CountUnitName || !coin.Divisible || coin.PriceCurrencyCode != "GBP"
            || ExceptTypes(corrected) != ExceptTypes(await ImportFinancialNativeReview.StoredAsync(fixture, ct))
            || LedgerCalculator.Balance(cash, await store.GetEntriesAsync(cancellationToken: ct), day) != 84950)
            throw new IOException("Original new type or retained financial data differs.");
        await Receipt("new-accepted", created); await Observe(newPage, "new-accepted"); await close(newPage);
        states.Add(new("new-saved", created, true, true, "AssetType_SavedNeedsReview", Raw(create)));
        var rows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        await File.WriteAllTextAsync(Path.Combine(folder, "complete-financial.json"), JsonSerializer.Serialize(new
        { Invocation = invocation, ProcessId = Environment.ProcessId, Cases = cases, Live = live, States = states, Digest = Digest(rows) }), ct);
        return new([.. states], [.. cases], [.. live], fixture, rows);
    }
    /// <summary>Loads only the exact completed financial receipt; no accepted command, seed or authority is reconstructed.</summary>
    internal static async Task<Prepared> ContinueAsync(IServiceProvider services, string folder, string original, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "complete-financial.json"), ct)); var root = doc.RootElement;
        if (root.GetProperty("Invocation").GetString() != original) throw new IOException("Original type invocation differs.");
        var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(Path.Combine(folder, "original-type.db")).BuildServiceProvider();
        var rows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        if (Digest(rows) != root.GetProperty("Digest").GetString()) throw new IOException("Original completed type rows changed.");
        var states = root.GetProperty("States").EnumerateArray().Select(s => new State(s.GetProperty("Name").GetString()!,
            s.GetProperty("Id").GetGuid(), s.GetProperty("IsNew").GetBoolean(), s.GetProperty("Committed").GetBoolean(),
            s.GetProperty("Key").GetString()!, s.GetProperty("Raw").Clone())).ToArray();
        return new(states, root.GetProperty("Cases").EnumerateArray().Select(c => (object)c.Clone()).ToArray(), [], fixture, rows);
    }
}
#endif
