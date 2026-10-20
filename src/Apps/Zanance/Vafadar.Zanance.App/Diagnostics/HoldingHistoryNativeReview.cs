#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
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

/// <summary>Original native deletion/refusal/Undo over one independent store, followed only by retired captions.</summary>
internal static class HoldingHistoryNativeReview
{
    private sealed class Clock(TimeProvider source) : TimeProvider
    {
        private readonly DateTimeOffset _now = source.GetUtcNow();
        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => source.LocalTimeZone;
    }
    private sealed class Dialogs(Translator translator) : IAppInteraction
    {
        internal Func<Task>? FirstConfirmation;
        internal int Confirmations;
        public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        { Confirmations++; if (FirstConfirmation is { } first) { FirstConfirmation = null; await first(); } return true; }
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => Task.FromResult<string?>(translator["Common_Delete"]);
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected holding history alert.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected holding history prompt.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("Unexpected holding history navigation.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected holding history failure.", exception);
    }
    /// <summary>A retired actual application model displaying one retained original outcome.</summary>
    internal sealed record State(string Name, HoldingDetailViewModel Model, string ErrorKey, bool ShowUndo);
    /// <summary>Original receipts, detached caption models and the exact financial file kept alive during observation.</summary>
    internal sealed record Prepared(State[] States, object[] Cases, object[] Live, ServiceProvider Fixture, string Rows);
    private static string Digest(string rows) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rows)));
    private static HoldingDetailViewModel Model(IServiceProvider services, ServiceProvider fixture, TimeProvider time, IAppInteraction port) => new(
        fixture.GetRequiredService<HoldingStore>(), fixture.GetRequiredService<ZananceStore>(), services.GetRequiredService<HoldingText>(),
        services.GetRequiredService<Translator>(), services.GetRequiredService<ILocalizationService>(), time, port);
    private static async Task<State[]> StatesAsync(IServiceProvider services, ServiceProvider fixture, TimeProvider time,
        Guid typeId, HoldingGroup group, CancellationToken ct)
    {
        var result = new List<State>();
        foreach (var (name, key, offer) in new[] { ("source", "Holding_HistorySourceChanged", false),
            ("deleted", "Holding_HistoryDeletedRefresh", true), ("retry", "Holding_HistoryUndoFailed", true),
            ("restored", "Holding_HistoryRestoredRefresh", false) })
        {
            ct.ThrowIfCancellationRequested();
            var model = Model(services, fixture, time, new Dialogs(services.GetRequiredService<Translator>()));
            model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = typeId }); await model.LoadAsync();
            model.PriceEditor.Raw = "77"; model.PriceEditor.Retire();
            model.HistoryEditor.ShowRetainedOutcome(group, key, offer);
            result.Add(new(name, model, key, offer));
        }
        return result.ToArray();
    }
    /// <summary>Runs the original native financial sequence once; optional live-caption errors never replay its accepted commands.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder, string invocation,
        Func<HoldingDetailPage, Task> show, Func<HoldingDetailPage, ICommand, object?, Task> invoke,
        Func<HoldingDetailPage, Task> close, Func<HoldingDetailPage, string, Task<object>> observe, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent holding history identity required.");
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-history.db");
        if (File.Exists(file)) throw new IOException("Original history file exists; never replay accepted seeds or commands.");
        var time = new Clock(services.GetRequiredService<TimeProvider>()); var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var fixture = new ServiceCollection().AddZananceData(file).AddSingleton<TimeProvider>(time).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        await store.EnsureDefaultCategoriesAsync(ct); var settings = await store.GetSettingsAsync(ct);
        settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
        var cash = new Account { Name = "D414 original cash", CurrencyCode = "EUR", OpeningBalance = 90000, OpeningDate = day };
        await store.SaveAccountAsync(cash, ct);
        var type = new AssetType { Name = "D414 original holding", Dimension = AssetDimension.Count,
            Kind = AssetKind.CountableItem, PriceCurrencyCode = "EUR" };
        await holdings.SaveTypeAsync(type, ct); var place = await holdings.EnsureDefaultLocationAsync("D414 original place", ct);
        var source = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = day,
            Kind = AssetEventKind.Purchase, Quantity = 2000, BasisAmount = 5000, Note = "D414 original source metadata" };
        var payment = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Date = day, Amount = 5000, Title = type.Name };
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Date = day, Amount = 200,
            CategoryId = await holdings.FeesCategoryAsync(ct), Title = "D414 original fee", Note = "D414 original fee metadata", Tags = ["retained"] };
        if (!(await holdings.SaveEventAsync(source, [payment, fee], true, ct)).Succeeded) throw new IOException("Original history source failed.");
        var refund = EntryActions.CreateRefund(fee, 150, cash.Id, day); refund.Note = "D414 original linked refund";
        if (!(await store.SaveEntryAsync(refund, ct)).Succeeded) throw new IOException("Original history refund failed.");
        var attachment = new EntryAttachment { EntryId = fee.Id, FileName = "original-fictitious.bin",
            ContentType = "application/octet-stream", Data = [1, 2, 3, 4] };
        await store.AddAttachmentAsync(attachment, ct);
        var translator = services.GetRequiredService<Translator>(); var port = new Dialogs(translator);
        var model = Model(services, fixture, time, port);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await model.LoadAsync();
        model.PriceEditor.Raw = "77"; var draft = model.PriceEditor.Raw;
        var page = new HoldingDetailPage(model); await show(page);
        var cases = new List<object>(); var live = new List<object>();
        async Task Receipt(string stage, object facts)
        {
            var receipt = new { Stage = stage, Facts = facts, Invocation = invocation, Package = package, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(receipt), ct); cases.Add(receipt);
        }
        async Task Observe(string stage)
        {
            try { live.Add(await observe(page, stage)); }
            catch (Exception error) when (error is not OutOfMemoryException)
            { live.Add(new { Stage = stage, CaptionPending = true, ObserverError = error.GetType().FullName, OriginalCommandsNeverReplayed = true }); }
        }
        async Task Run(IAsyncRelayCommand command, object? parameter = null)
        {
            await invoke(page, command, parameter); var limit = DateTime.UtcNow.AddSeconds(15);
            while (command.ExecutionTask is null)
            { if (DateTime.UtcNow > limit) throw new IOException("Original native history command pending; preserve its receipt."); await Task.Delay(20, ct); }
            await command.ExecutionTask;
        }
        string? reviewedRows = null;
        port.FirstConfirmation = async () =>
        {
            var item = (await holdings.GetEventsAsync(type.Id, ct)).Single();
            var money = (await store.GetEntriesAsync(cancellationToken: ct)).Where(e => e.GroupId == item.GroupId).ToList();
            money.Single(e => e.Id == fee.Id).Note = "D414 concurrent original fee metadata";
            if (!(await holdings.SaveEventAsync(item, money, true, ct)).Succeeded) throw new IOException("Original concurrent fee control failed.");
            reviewedRows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        };
        await Run(model.OpenEventCommand, source.Id);
        if (reviewedRows is null || reviewedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct)
            || model.HistoryEditor.Error != translator["Holding_HistorySourceChanged"] || model.HistoryEditor.HasUndo
            || model.HistoryEditor.CanOpen || model.PriceEditor.Raw != draft) throw new IOException("Original stale history deletion changed data.");
        await Receipt("source-refused", new { WholeRowsUnchanged = true, RetainedRawPrice = draft }); await Observe("source-refused");
        await Run(model.HistoryEditor.RefreshCommand);
        var refresh = model.HistoryEditor.RefreshView;
        model.HistoryEditor.RefreshView = () => throw new IOException("Original independent history readback failure");
        await Run(model.OpenEventCommand, source.Id);
        var group = model.HistoryEditor.RetainedDeletion ?? throw new IOException("Original accepted deletion lost Undo.");
        var kept = (await store.GetEntriesAsync(cancellationToken: ct)).Single();
        if (!model.HistoryEditor.DeleteCommitted || !model.HistoryEditor.CanUndo || kept.Id != refund.Id || kept.RefundOfId is not null
            || LedgerCalculator.Balance(cash, [kept], day) != 90150 || (await holdings.GetEventsAsync(type.Id, ct)).Count != 0
            || model.PriceEditor.Raw != draft || model.HistoryEditor.Error != translator["Holding_HistoryDeletedRefresh"])
            throw new IOException("Original committed holding deletion lost its outcome or retained refund.");
        await Receipt("delete-accepted", new { source.Id, Fee = fee.Id, Refund = refund.Id, Balance = 90150, KnownCommit = true,
            UndoRetained = true, CaptionKey = "Holding_HistoryDeletedRefresh" }); await Observe("delete-accepted");
        model.HistoryEditor.RefreshView = refresh; await Run(model.HistoryEditor.RefreshCommand); await Observe("delete-refreshed");
        await using (var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectHoldingUndo BEFORE INSERT ON AssetEvents BEGIN SELECT RAISE(ABORT, 'Original independent undo persistence failure'); END;", ct);
        var deletedRows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        await Run(model.HistoryEditor.UndoCommand);
        if (deletedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || !model.HistoryEditor.CanUndo
            || model.HistoryEditor.UndoCommitted || model.HistoryEditor.Error != translator["Holding_HistoryUndoFailed"])
            throw new IOException("Original failed Undo lost its retry or changed money.");
        await Receipt("undo-refused", new { WholeRowsUnchanged = true, OriginalUndoRetained = true }); await Observe("undo-refused");
        await using (var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectHoldingUndo;", ct);
        model.HistoryEditor.RefreshView = () => throw new IOException("Original independent restored readback failure");
        await Run(model.HistoryEditor.UndoCommand);
        if (reviewedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || !model.HistoryEditor.UndoCommitted
            || model.HistoryEditor.HasUndo || model.HistoryEditor.CanUndo || model.PriceEditor.Raw != draft
            || model.HistoryEditor.Error != translator["Holding_HistoryRestoredRefresh"])
            throw new IOException("Original accepted Undo changed metadata, refund links or its known result.");
        await Receipt("undo-accepted", new { WholeOriginalRowsRestored = true, Balance = 84950, Quantity = 2000,
            Basis = 5000, OriginalRefundSource = fee.Id, OriginalAttachment = attachment.Id, KnownCommit = true }); await Observe("undo-accepted");
        model.HistoryEditor.RefreshView = refresh; await Run(model.HistoryEditor.RefreshCommand);
        if (reviewedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || model.HistoryEditor.Error is not null
            || !model.HistoryEditor.CanOpen || model.HistoryEditor.HasUndo || model.PriceEditor.Raw != draft)
            throw new IOException("Original restored read-only refresh changed rows or raw price.");
        await Receipt("refresh-complete", new { WholeRowsUnchanged = true, OriginalRawPrice = draft }); await Observe("refresh-complete");
        await close(page);
        var rows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        await File.WriteAllTextAsync(Path.Combine(folder, "complete-financial.json"), JsonSerializer.Serialize(new
        { Cases = cases, Live = live, Invocation = invocation, Package = package, ProcessId = Environment.ProcessId,
            Type = type.Id, Event = source.Id, Group = source.GroupId, Cash = cash.Id, ClockUtc = time.GetUtcNow(), RowsDigest = Digest(rows),
            OriginalCommandsCompleted = true, ConfirmationPortCount = port.Confirmations, StableFictitiousAuditClock = true }), ct);
        return new(await StatesAsync(services, fixture, time, type.Id, group, ct), cases.ToArray(), live.ToArray(), fixture, rows);
    }
    /// <summary>Rehydrates only completed original rows for retired captions; no Undo authority or accepted command is recovered.</summary>
    internal static async Task<Prepared> ContinueAsync(IServiceProvider services, string folder, string expectedInvocation, CancellationToken ct)
    {
        using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "complete-financial.json"), ct));
        var root = receipt.RootElement;
        if (!root.GetProperty("OriginalCommandsCompleted").GetBoolean() || root.GetProperty("Invocation").GetString() != expectedInvocation
            || root.GetProperty("Cases").GetArrayLength() != 5) throw new IOException("Exact original completed history receipt required.");
        var file = Path.Combine(folder, "original-history.db");
        if (!File.Exists(file)) throw new IOException("Original history file must not be reseeded.");
        var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
        var rows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        if (Digest(rows) != root.GetProperty("RowsDigest").GetString()) throw new IOException("Original completed history rows changed.");
        var holdings = fixture.GetRequiredService<HoldingStore>(); var store = fixture.GetRequiredService<ZananceStore>();
        var typeId = root.GetProperty("Type").GetGuid(); var id = root.GetProperty("Event").GetGuid(); var groupId = root.GetProperty("Group").GetGuid();
        var group = new HoldingGroup((await holdings.GetEventsAsync(typeId, ct)).Where(e => e.Id == id).ToList(),
            (await store.GetEntriesAsync(cancellationToken: ct)).Where(e => e.GroupId == groupId).ToList());
        if (group.Events.Count != 1 || group.Entries.Count != 2) throw new IOException("Original completed holding group is unavailable.");
        return new(await StatesAsync(services, fixture, services.GetRequiredService<TimeProvider>(), typeId, group, ct),
            root.GetProperty("Cases").EnumerateArray().Select(item => (object)item.Clone()).ToArray(),
            root.GetProperty("Live").EnumerateArray().Select(item => (object)item.Clone()).ToArray(), fixture, rows);
    }
}
#endif
