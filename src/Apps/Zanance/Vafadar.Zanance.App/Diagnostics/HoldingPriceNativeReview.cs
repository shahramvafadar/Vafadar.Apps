#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original manual-price native commands over one independent actual application database.</summary>
internal static class HoldingPriceNativeReview
{
    private sealed class Confirmation : IAppInteraction
    {
        internal int Count;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        { Count++; return Task.FromResult(true); }
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected price alert.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected price prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected price menu.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("Unexpected price navigation.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected price failure.", exception);
    }
    internal sealed record Prepared(HoldingDetailViewModel Model, object[] Cases, string File, string Rows, ServiceProvider Fixture);
    /// <summary>Continues only caption/state observation from exact completed original financial receipts.</summary>
    internal static async Task<Prepared> ContinueAsync(IServiceProvider services, string originalFolder, CancellationToken ct)
    {
        using var completed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(originalFolder, "complete.json"), ct));
        var cases = completed.RootElement.GetProperty("Cases").EnumerateArray().Select(item => item.Clone()).ToArray();
        if (cases.Length != 3 || cases[0].GetProperty("Stage").GetString() != "stale-refused"
            || cases[1].GetProperty("Stage").GetString() != "price-accepted" || cases[2].GetProperty("Stage").GetString() != "price-deleted")
            throw new IOException("Exact completed original price receipts required.");
        var accepted = cases[1].GetProperty("Facts").GetProperty("Id").GetGuid();
        var removed = cases[2].GetProperty("Facts").GetProperty("Removed").GetGuid();
        var file = Path.Combine(originalFolder, "financial", "original-price.db");
        if (!File.Exists(file)) throw new IOException("Original price fixture must not be reseeded.");
        var time = services.GetRequiredService<TimeProvider>();
        var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var type = (await holdings.GetTypesAsync(ct)).Single(); var events = await holdings.GetEventsAsync(type.Id, ct);
        var price = (await holdings.GetValuationsAsync(type.Id, ct)).Single();
        if (type.Name != "D413 original holding" || type.PriceCurrencyCode != "EUR" || events.Count != 2
            || events.Sum(item => item.Quantity) != 3000 || events.Sum(item => item.BasisAmount ?? 0) != 5000
            || events.Single(item => item.Quantity == 2000).Note != "D413 retained original source"
            || events.Single(item => item.Quantity == 1000).Note != "D413 concurrent original quantity"
            || price.Id != accepted || price.Id == removed || price.PricePerUnitMilli != 2000000
            || price.Source != ValuationSource.Manual || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new IOException("Original accepted price data changed.");
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        var translator = services.GetRequiredService<Translator>();
        var model = new HoldingDetailViewModel(holdings, services.GetRequiredService<HoldingText>(), translator,
            services.GetRequiredService<ILocalizationService>(), time, new Confirmation());
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await model.LoadAsync();
        model.PriceEditor.Raw = "60"; model.PriceEditor.ModeIndex = 1; model.PriceEditor.Retire();
        model.PriceEditor.Error = translator["Holding_PriceSourceChanged"];
        if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Original price continuation changed rows.");
        return new(model, cases.Cast<object>().ToArray(), file, before, fixture);
    }
    /// <summary>Performs only the original pending commands; every acceptance receipt precedes fallible observation.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder,
        Func<HoldingDetailPage, Task> show, Func<HoldingDetailPage, ICommand, object?, Task> invoke,
        Func<HoldingDetailPage, Task> close, Func<HoldingDetailPage, string, Task> observe, CancellationToken ct, bool resumeBeforeInvoke = false)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent price identity required.");
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-price.db");
        if (File.Exists(file) != resumeBeforeInvoke || resumeBeforeInvoke && Directory.EnumerateFiles(folder, "*.json").Any())
            throw new IOException("Original price fixture or pending-command receipts do not match continuation.");
        var time = services.GetRequiredService<TimeProvider>(); var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
        if (!resumeBeforeInvoke) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        AssetType type; AssetLocation place; AssetValuation original;
        if (resumeBeforeInvoke)
        {
            type = (await holdings.GetTypesAsync(ct)).Single(); place = (await holdings.GetLocationsAsync(ct)).Single();
            original = (await holdings.GetValuationsAsync(type.Id, ct)).Single();
            var retained = await holdings.GetEventsAsync(type.Id, ct);
            if (type.Name != "D413 original holding" || type.PriceCurrencyCode != "EUR" || retained.Count != 2
                || retained.Single(item => item.Quantity == 2000).BasisAmount != 5000
                || retained.Single(item => item.Quantity == 1000).Note != "D413 concurrent original quantity"
                || original.PricePerUnitMilli != 2500000 || original.Note != "D413 original confirmed price"
                || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                throw new IOException("Retained original pre-invocation price fixture changed.");
        }
        else
        {
            var settings = await store.GetSettingsAsync(ct); settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
            type = new AssetType { Name = "D413 original holding", Kind = AssetKind.CountableItem,
                Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
            await holdings.SaveTypeAsync(type, ct); place = await holdings.EnsureDefaultLocationAsync("D413 original place", ct);
            var source = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = day,
                Kind = AssetEventKind.Opening, Quantity = 2000, BasisAmount = 5000, Note = "D413 retained original source" };
            if (!(await holdings.SaveEventAsync(source, [], ct)).Succeeded) throw new IOException("Original price source failed.");
            original = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = day.AddDays(-1),
                PricePerUnitMilli = 2500000, Note = "D413 original confirmed price" };
            await holdings.SaveValuationAsync(original, ct);
        }
        var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var port = new Confirmation();
        var model = new HoldingDetailViewModel(holdings, services.GetRequiredService<HoldingText>(), translator, local, time, port);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = type.Id }); await model.LoadAsync();
        var editor = model.PriceEditor; editor.Raw = "60"; editor.ModeIndex = 1; editor.Date = day;
        var page = new HoldingDetailPage(model); await show(page);
        if (resumeBeforeInvoke)
        {
            // The original quantity seed is retained. A new metadata-only control invalidates this original review;
            // the first failed observer never invoked a user command and must not replay its seed.
            type.Name = "D413 retained original holding"; await holdings.SaveTypeAsync(type, ct);
        }
        else
        {
            var change = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Date = day,
                Kind = AssetEventKind.Opening, Quantity = 1000, Note = "D413 concurrent original quantity" };
            if (!(await holdings.SaveEventAsync(change, [], ct)).Succeeded) throw new IOException("Original concurrent quantity failed.");
        }
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        async Task Run(IAsyncRelayCommand command, object? argument = null)
        {
            await invoke(page, command, argument); var limit = DateTime.UtcNow.AddSeconds(15);
            while (command.ExecutionTask is null)
            { if (DateTime.UtcNow > limit) throw new IOException("Original price command pending; do not replay."); await Task.Delay(20, ct); }
            await command.ExecutionTask;
        }
        var cases = new List<object>();
        async Task Receipt(string name, object facts)
        {
            var receipt = new { Stage = name, Facts = facts, Package = package, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), JsonSerializer.Serialize(receipt), ct); cases.Add(receipt);
        }
        await Run(editor.SaveCommand);
        if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || editor.Raw != "60"
            || editor.Error != translator["Holding_PriceSourceChanged"] || editor.CanSubmit || !editor.NeedsRefresh)
            throw new IOException("Original stale price refusal lost raw input or rows.");
        await Receipt("stale-refused", new { WholeRowsUnchanged = true, OriginalRaw = editor.Raw, OriginalQuantity = resumeBeforeInvoke ? 3000 : 2000, CurrentQuantity = 3000, MetadataControlAfterRetainedObserverFailure = resumeBeforeInvoke });
        await observe(page, "stale-refused"); await Run(editor.RefreshCommand);
        if (editor.Raw != "60" || !editor.CanSubmit || editor.Error is not null) throw new IOException("Original price refresh lost draft.");
        var events = JsonSerializer.Serialize(await holdings.GetEventsAsync(type.Id, ct));
        await Run(editor.SaveCommand);
        var accepted = (await holdings.GetValuationsAsync(type.Id, ct)).Single(p => p.Id != original.Id);
        if (accepted.Id != editor.AcceptedId || accepted.PricePerUnitMilli != 2000000 || accepted.Source != ValuationSource.Manual
            || editor.Raw != "" || !editor.CanSubmit || events != JsonSerializer.Serialize(await holdings.GetEventsAsync(type.Id, ct))
            || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new IOException("Original accepted price changed money, quantity or basis.");
        await Receipt("price-accepted", new { accepted.Id, accepted.PricePerUnitMilli, OriginalEventsUnchanged = true, NoLedgerEntries = true });
        await observe(page, "price-accepted"); editor.Raw = "70";
        var row = model.Valuations.Single(v => v.Id == original.Id);
        await Run(model.DeletePriceCommand, row);
        var kept = (await holdings.GetValuationsAsync(type.Id, ct)).Single();
        if (kept.Id != accepted.Id || editor.AcceptedId != original.Id || !editor.LastWasDeletion || editor.Raw != "70"
            || !editor.CanSubmit || port.Count != 1 || events != JsonSerializer.Serialize(await holdings.GetEventsAsync(type.Id, ct)))
            throw new IOException("Original confirmed price deletion lost history or separate draft.");
        await Receipt("price-deleted", new { Removed = original.Id, Retained = kept.Id, RetainedRaw = editor.Raw,
            ConfirmationPortCount = port.Count, OriginalEventsUnchanged = true });
        await observe(page, "price-deleted"); await close(page);
        // Only retained refusal presentation is reused across captions; no accepted financial command is repeated.
        editor.Raw = "60"; editor.Retire(); editor.Error = translator["Holding_PriceSourceChanged"];
        var rows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        return new(model, cases.ToArray(), file, rows, fixture);
    }
}
#endif
