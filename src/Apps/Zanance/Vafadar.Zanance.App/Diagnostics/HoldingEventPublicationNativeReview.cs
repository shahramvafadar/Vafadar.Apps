#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Checks complete original event covers/Retry and new routed drafts without financial or metadata commands.</summary>
internal static class HoldingEventPublicationNativeReview
{
    private sealed class Dialogs : IAppInteraction
    {
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected original event alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Unexpected original event confirmation.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected original event menu.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("No new location command in original event publication review.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("No financial Save/Cancel command in original publication review.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected original publication effect.", exception);
    }
    /// <summary>Controls only the original actual SQLite context read; no fake application data or controls.</summary>
    internal sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal Func<Task>? Before;
        internal readonly ConcurrentBag<int> Threads = [];
        public ZananceDbContext CreateDbContext() => original.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { Threads.Add(Environment.CurrentManagedThreadId); if (Before is { } before) await before(); return await original.CreateDbContextAsync(cancellationToken); }
    }
    /// <summary>Owns one new independently seeded original file and immutable source rows for read-only contexts.</summary>
    internal sealed record Prepared(ServiceProvider Fixture, Factory Factory, HoldingStore Store, Guid First, Guid Second, string Rows, int UiThread);
    /// <summary>Seeds only the new independent file once, before recording its complete source packet.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder, CancellationToken ct)
    {
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-event-publication.db");
        if (File.Exists(file)) throw new IOException("Preserve original event file; never replay a completed seed.");
        var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
        var ui = Environment.CurrentManagedThreadId; await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        await store.UpdateSettingsAsync(settings => settings.Mode = ExperienceMode.Advanced, ct);
        var first = new AssetType { Name = "D421 original EUR item", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        var second = new AssetType { Name = "D421 original USD item", Kind = AssetKind.CountableItem, Dimension = AssetDimension.Count, PriceCurrencyCode = "USD" };
        await holdings.SaveTypeAsync(first, ct); await holdings.SaveTypeAsync(second, ct); await holdings.EnsureDefaultLocationAsync("D421 original safe", ct);
        var factory = new Factory(fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        return new(fixture, factory, new HoldingStore(factory, fixture.GetRequiredService<ICommercialWriteAccessSource>()),
            first.Id, second.Id, await ImportFinancialNativeReview.StoredAsync(fixture, ct), ui);
    }
    /// <summary>Exercises one original covered failure/native Retry, then optionally changes only that original query.</summary>
    internal static async Task ContextAsync(IServiceProvider services, Prepared source, string context, bool query,
        Func<AssetEventEditorPage, Task> show, Func<AssetEventEditorPage, Task> retry, Func<AssetEventEditorPage, Task> close,
        Func<AssetEventEditorPage, string, Task> observe, CancellationToken ct)
    {
        var model = new AssetEventEditorViewModel(source.Store, source.Fixture.GetRequiredService<ZananceStore>(), services.GetRequiredService<HoldingText>(),
            services.GetRequiredService<Translator>(), services.GetRequiredService<ILocalizationService>(), services.GetRequiredService<TimeProvider>(), new Dialogs());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.Factory.Before = async () => { entered.TrySetResult(); await release.Task.WaitAsync(ct); throw new IOException("Original controlled read failure."); };
        var load = model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = source.First, ["kind"] = "Opening" });
        await entered.Task.WaitAsync(ct); var page = new AssetEventEditorPage(model); await show(page);
        try { await observe(page, context + "-loading"); } finally { release.TrySetResult(); }
        await load; source.Factory.Before = null;
        if (!model.Loading.HasFailed || model.CanSave || !model.CanReload) throw new IOException("Original failed event cover differs.");
        await observe(page, context + "-error"); await retry(page);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (model.ReloadCommand.ExecutionTask is null) { if (DateTime.UtcNow > deadline) throw new IOException("Original native event Retry pending."); await Task.Delay(25, ct); }
        await model.ReloadCommand.ExecutionTask;
        if (!model.Loading.IsReady || !model.CanSave || model.Locations.Count != 1 || model.TypeName != "D421 original EUR item" || model.QuantityText.Length != 0)
            throw new IOException("Original complete event form did not publish before uncovering.");
        await observe(page, context + "-ready");
        if (query)
        {
            model.QuantityText = "77"; model.BasisText = "50"; model.Note = "D421 original unsaved draft";
            await observe(page, context + "-raw");
            await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = source.Second, ["kind"] = "Opening" });
            if (!model.Loading.IsReady || !model.CanSave || model.QuantityText.Length != 0 || model.BasisText.Length != 0 || model.Note.Length != 0
                || model.TypeName != "D421 original USD item" || model.IsDirty) throw new IOException("Original new query inherited old financial inputs.");
            await observe(page, context + "-query");
        }
        await close(page);
        if (source.Rows != await ImportFinancialNativeReview.StoredAsync(source.Fixture, ct)) throw new IOException("Read-only event publication changed original typed rows.");
        if (source.Factory.Threads.Any(thread => thread == source.UiThread)) throw new IOException("Original event database reader ran on UI.");
    }
}
#endif
