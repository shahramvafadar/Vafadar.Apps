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
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original native archive/cancel/refusal over an independent financial file, then retired captions only.</summary>
internal static class HoldingArchiveNativeReview
{
    private sealed class Dialogs : IAppInteraction
    {
        internal Func<Task<bool>>? Confirm;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Confirm?.Invoke() ?? Task.FromResult(true);
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected archive choice.");
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected archive alert.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected archive prompt.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("Original independent return failure.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected archive failure.", exception);
    }
    /// <summary>A completed caption with permanently retired original write authority.</summary>
    internal sealed record State(string Name, HoldingDetailViewModel Model);
    /// <summary>Exact original financial receipts, native observations and independently owned source file.</summary>
    internal sealed record Prepared(State[] States, object[] Cases, object[] Live, ServiceProvider Fixture, string Rows);
    private static string Digest(string rows) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rows)));
    private static HoldingDetailViewModel Model(IServiceProvider services, ServiceProvider fixture, IAppInteraction port) => new(
        fixture.GetRequiredService<HoldingStore>(), services.GetRequiredService<HoldingText>(),
        services.GetRequiredService<Translator>(), services.GetRequiredService<ILocalizationService>(), fixture.GetRequiredService<TimeProvider>(), port);
    private static async Task<State[]> StatesAsync(IServiceProvider services, ServiceProvider fixture, Guid id)
    {
        var states = new List<State>();
        foreach (var (name, key, committed) in new[] { ("source", "Holding_ArchiveSourceChanged", false), ("saved", "Holding_ArchiveSavedRefresh", true) })
        {
            var model = Model(services, fixture, new Dialogs());
            model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = id }); await model.LoadAsync();
            model.PriceEditor.Raw = "77"; model.PriceEditor.Retire(); model.HistoryEditor.Retire();
            model.ArchiveEditor.ShowRetainedOutcome(key, committed); states.Add(new(name, model));
        }
        return [.. states];
    }
    /// <summary>Runs each original command once and writes its receipt before fallible caption observations.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder, string invocation,
        Func<HoldingDetailPage, Task> show, Func<HoldingDetailPage, ICommand, object?, Task> invoke,
        Func<HoldingDetailPage, Task> close, Func<HoldingDetailPage, string, Task<object>> observe, CancellationToken ct)
    {
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-archive.db");
        if (File.Exists(file)) throw new IOException("Original archive file exists; continue its exact receipt instead.");
        var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var day = new DateOnly(2026, 10, 1); var settings = await store.GetSettingsAsync(ct);
        settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
        var cash = new Account { Name = "D415 original cash", CurrencyCode = "EUR", OpeningBalance = 90000, OpeningDate = day };
        await store.SaveAccountAsync(cash, ct);
        var type = new AssetType { Name = "D415 original holding", Dimension = AssetDimension.Count,
            Kind = AssetKind.CountableItem, PriceCurrencyCode = "EUR", Note = "Original type metadata" };
        await holdings.SaveTypeAsync(type, ct); var place = await holdings.EnsureDefaultLocationAsync("D415 original place", ct);
        var source = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = day,
            Kind = AssetEventKind.Purchase, Quantity = 2000, BasisAmount = 5000, Note = "D415 original source metadata" };
        var payment = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Date = day, Amount = 5000, Title = type.Name };
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Date = day, Amount = 200,
            CategoryId = await holdings.FeesCategoryAsync(ct), Title = "D415 original fee", Note = "D415 retained fee" };
        if (!(await holdings.SaveEventAsync(source, [payment, fee], true, ct)).Succeeded) throw new IOException("Original archive source failed.");
        var refund = EntryActions.CreateRefund(fee, 150, cash.Id, day); await store.SaveEntryAsync(refund, ct);
        await store.AddAttachmentAsync(new EntryAttachment { EntryId = fee.Id, FileName = "original.bin", ContentType = "application/octet-stream", Data = [1, 2, 3, 4] }, ct);
        var translator = services.GetRequiredService<Translator>(); var port = new Dialogs(); var model = Model(services, fixture, port);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await model.LoadAsync(); model.PriceEditor.Raw = "77";
        var page = new HoldingDetailPage(model); await show(page);
        var cases = new List<object>(); var live = new List<object>();
        async Task Receipt(string stage)
        {
            var receipt = new { Stage = stage, Invocation = invocation, ProcessId = Environment.ProcessId, type.Id };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(receipt), ct); cases.Add(receipt);
        }
        async Task Observe(string stage)
        {
            try { live.Add(await observe(page, stage)); }
            catch (Exception error) when (error is not OutOfMemoryException)
            { live.Add(new { Stage = stage, CaptionPending = true, Error = error.GetType().FullName, OriginalCommandsNeverReplayed = true }); }
        }
        async Task Run(IAsyncRelayCommand command)
        {
            await invoke(page, command, null); var deadline = DateTime.UtcNow.AddSeconds(15);
            while (command.ExecutionTask is null)
            { if (DateTime.UtcNow > deadline) throw new IOException("Original archive command remains pending."); await Task.Delay(20, ct); }
            await command.ExecutionTask;
        }
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        port.Confirm = () => Task.FromResult(false); await Run(model.ArchiveCommand);
        if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || !model.ArchiveEditor.CanArchive) throw new IOException("Archive Cancel changed data.");
        await Receipt("cancel"); await Observe("cancel");
        string? corrected = null;
        port.Confirm = async () => { type.Name = "D415 concurrent name correction"; await holdings.SaveTypeAsync(type, ct);
            corrected = await ImportFinancialNativeReview.StoredAsync(fixture, ct); return true; };
        await Run(model.ArchiveCommand);
        if (corrected != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || model.ArchiveEditor.Error != translator["Holding_ArchiveSourceChanged"]
            || model.ArchiveEditor.CanArchive) throw new IOException("Original changed archive source was not refused.");
        await Receipt("source-refused"); await Observe("source-refused");
        await Run(model.ArchiveEditor.RefreshCommand); port.Confirm = null;
        var entries = JsonSerializer.Serialize(await store.GetEntriesAsync(cancellationToken: ct));
        var events = JsonSerializer.Serialize(await holdings.GetEventsAsync(type.Id, ct));
        var prices = JsonSerializer.Serialize(await holdings.GetValuationsAsync(type.Id, ct));
        var attachments = JsonSerializer.Serialize(await store.GetAttachmentsAsync(fee.Id, ct));
        await Run(model.ArchiveCommand);
        if (!model.ArchiveEditor.Committed || model.ArchiveEditor.CanArchive || model.ArchiveEditor.Error != translator["Holding_ArchiveSavedRefresh"]
            || model.PriceEditor.Raw != "77") throw new IOException("Known archive lost its original result or draft.");
        var archived = (await holdings.GetTypesAsync(ct)).Single();
        if (!archived.IsArchived || archived.Name != type.Name || archived.Note != type.Note || archived.PriceCurrencyCode != "EUR"
            || entries != JsonSerializer.Serialize(await store.GetEntriesAsync(cancellationToken: ct))
            || events != JsonSerializer.Serialize(await holdings.GetEventsAsync(type.Id, ct))
            || prices != JsonSerializer.Serialize(await holdings.GetValuationsAsync(type.Id, ct))
            || attachments != JsonSerializer.Serialize(await store.GetAttachmentsAsync(fee.Id, ct))
            || LedgerCalculator.Balance(cash, await store.GetEntriesAsync(cancellationToken: ct), day) != 84950)
            throw new IOException("Original archive changed financial history or identity.");
        await Receipt("archive-accepted"); await Observe("archive-accepted");
        var accepted = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        await Run(model.ArchiveEditor.RefreshCommand);
        if (accepted != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || model.ArchiveEditor.CanArchive)
            throw new IOException("Read-only archive Refresh repeated the accepted write.");
        await Observe("archive-refreshed");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete-financial.json"), JsonSerializer.Serialize(new
        { Invocation = invocation, TypeId = type.Id, ProcessId = Environment.ProcessId, Cases = cases, Live = live, Digest = Digest(accepted) }), ct);
        await close(page);
        return new(await StatesAsync(services, fixture, type.Id), [.. cases], [.. live], fixture, accepted);
    }
    /// <summary>Reads only exact completed financial evidence; caption continuation never invokes financial commands.</summary>
    internal static async Task<Prepared> ContinueAsync(IServiceProvider services, string folder, string original, CancellationToken ct)
    {
        using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "complete-financial.json"), ct));
        var root = receipt.RootElement;
        if (root.GetProperty("Invocation").GetString() != original) throw new IOException("Original archive invocation differs.");
        var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(Path.Combine(folder, "original-archive.db")).BuildServiceProvider();
        var rows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        if (Digest(rows) != root.GetProperty("Digest").GetString()) throw new IOException("Original completed archive file changed.");
        return new(await StatesAsync(services, fixture, root.GetProperty("TypeId").GetGuid()),
            root.GetProperty("Cases").EnumerateArray().Select(e => (object)e.Clone()).ToArray(), [], fixture, rows);
    }
}
#endif
