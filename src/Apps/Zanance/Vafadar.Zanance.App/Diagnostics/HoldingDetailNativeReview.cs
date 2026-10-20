#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Qualifies the original detail over retained completed overview fixtures, without reseeding or financial commands.</summary>
internal static class HoldingDetailNativeReview
{
    /// <summary>Owns the retained fictitious source and original observations, without restoring financial authority.</summary>
    internal sealed record Original(ServiceProvider Fixture, Guid Id, string Rows, List<object> Cases);

    /// <summary>Opens only the exact prior fictitious source and verifies its known original facts before observing a new page.</summary>
    internal static async Task<Original> OpenAsync(IServiceProvider services, string source, CancellationToken ct)
    {
        var file = Path.Combine(source, "original-overview.db");
        if (!File.Exists(file) || new FileInfo(file).LinkTarget is not null || new DirectoryInfo(source).LinkTarget is not null)
            throw new IOException("Exact original retained overview fixture is required.");
        var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
        var store = fixture.GetRequiredService<HoldingStore>(); var snapshot = await store.ReadOverviewSnapshotAsync(ct);
        var type = snapshot.Inputs.Types.Single(t => t.Name == "D418 original counted holding");
        if (snapshot.Inputs.Types.Count != 3 || snapshot.Inputs.Events.Count != 2 || snapshot.Inputs.Valuations.Count != 1
            || snapshot.Inputs.Events.Single(e => e.AssetTypeId == type.Id).Quantity != 2000
            || snapshot.Inputs.Valuations.Single().PricePerUnitMilli != 2500000
            || snapshot.Inputs.Settings?.Mode != ExperienceMode.Advanced
            || (await fixture.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new IOException("Retained original detail input differs from the completed source.");
        return new(fixture, type.Id, await ImportFinancialNativeReview.StoredAsync(fixture, ct), []);
    }

    /// <summary>Exercises actual cover, expected read refusal, native Retry, raw refresh and original native navigation once.</summary>
    internal static async Task ContextAsync(IServiceProvider services, Original original, string stage, bool first,
        Func<HoldingDetailPage, Task> show, Func<HoldingDetailPage, Task> close,
        Func<HoldingDetailPage, string, Task> invoke, Func<HoldingDetailPage, string, Task<object>> observe, CancellationToken ct)
    {
        var factory = new HoldingOverviewNativeReview.Factory(original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>())
            { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously), FailOnce = true };
        var port = new HoldingOverviewNativeReview.Navigation();
        var model = Model(services, original, factory, port); var page = new HoldingDetailPage(model);
        var previous = Failures.Observed; var expected = false;
        void Failure(Exception error) { if (ReferenceEquals(error, factory.OriginalFailure)) expected = true; else previous?.Invoke(error); }
        Failures.Observed = Failure;
        async Task Record(string state)
        {
            var fact = await observe(page, stage + "-" + state);
            original.Cases.Add(new { Stage = stage, State = state, Observation = fact, ProcessId = Environment.ProcessId, FinancialCommands = 0, SeedsReplayed = 0 });
        }
        try
        {
            await show(page); await AwaitAsync(() => factory.Calls > 0, "original detail read", ct);
            await Record("loading"); factory.Gate.SetResult();
            await AwaitAsync(() => model.Loading.HasFailed && expected, "original detail refusal", ct);
            await Record("error"); factory.Gate = null;
            await invoke(page, "retry"); await AwaitAsync(() => model.Loading.IsReady || model.Loading.HasFailed, "original detail Retry", ct);
            RequireReady(model); await Record("ready");
            if (first)
            {
                // This is an explicit fictitious raw draft control, not acceptance of native typing or a price Save.
                model.PriceEditor.Raw = "77"; model.PriceEditor.ModeIndex = 1;
                model.Retire(); await model.LoadAsync(); RequireReady(model);
                if (model.PriceEditor.Raw != "77" || !model.PriceEditor.NeedsRefresh || model.PriceEditor.CanSubmit)
                    throw new IOException("Original raw price authority survived departure.");
                await Record("raw-retired"); await invoke(page, "refresh");
                await AwaitAsync(() => model.Loading.IsReady && !model.PriceEditor.Busy, "original raw explicit Refresh", ct);
                if (model.PriceEditor.Raw != "77" || !model.PriceEditor.CanSubmit) throw new IOException("Original explicit raw Refresh lost its draft.");
                await Record("raw-refreshed");
                await invoke(page, "record"); await AwaitAsync(() => port.Routes.Count == 1, "original Record navigation", ct);
                await invoke(page, "edit"); await AwaitAsync(() => port.Routes.Count == 2, "original type navigation", ct);
                if (port.Routes[0].Route != AppRoutes.AssetEventEditorRoute || !Equals(port.Routes[0].Parameters!["type"], original.Id)
                    || port.Routes[1].Route != AppRoutes.AssetTypeEditorRoute || !Equals(port.Routes[1].Parameters!["id"], original.Id))
                    throw new IOException("Original native detail target differs.");
                factory.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
                var calls = factory.Calls; var pending = model.LoadAsync();
                await AwaitAsync(() => factory.Calls > calls, "original pending detail retirement", ct); model.Retire();
                factory.Gate.SetResult(); await pending;
                if (model.Loading.IsReady || model.PriceEditor.CanSubmit || model.HistoryEditor.CanOpen || model.ArchiveEditor.CanArchive)
                    throw new IOException("Retired original native detail escaped its cover.");
                original.Cases.Add(new { Stage = stage, State = "native-targets-and-retirement", ProcessId = Environment.ProcessId,
                    NativeNavigationUsesExplicitPort = true, FinancialCommands = 0, SeedsReplayed = 0 });
            }
        }
        finally { Failures.Observed = previous; await close(page); }
        await RequireUnchangedAsync(original, ct);
    }

    /// <summary>Checks one missing type over the same original file, without creating settings, types or history.</summary>
    internal static async Task MissingAsync(IServiceProvider services, Original original,
        Func<HoldingDetailPage, Task> show, Func<HoldingDetailPage, Task> close,
        Func<HoldingDetailPage, string, Task<object>> observe, CancellationToken ct)
    {
        var model = Model(services, original, original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), new HoldingOverviewNativeReview.Navigation());
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = Guid.NewGuid() }); var page = new HoldingDetailPage(model);
        await show(page); await AwaitAsync(() => model.Loading.IsReady || model.Loading.HasFailed, "original missing detail", ct);
        if (!model.NotFound || !model.Loading.IsReady || model.Events.Count != 0 || model.Valuations.Count != 0
            || model.PriceEditor.CanSubmit || model.HistoryEditor.CanOpen || model.ArchiveEditor.CanArchive)
            throw new IOException("Original missing detail retained old facts or actions.");
        var fact = await observe(page, "original-missing");
        original.Cases.Add(new { State = "missing", Observation = fact, ProcessId = Environment.ProcessId, FinancialCommands = 0, SeedsReplayed = 0 });
        await close(page); await RequireUnchangedAsync(original, ct);
    }

    /// <summary>Refuses a partial or substituted original known holding before native ready observations.</summary>
    internal static void RequireReady(HoldingDetailViewModel model)
    {
        if (!model.Loading.IsReady || model.NotFound || !model.IsAdvanced || !model.HasStock || model.ValueUnknown
            || model.Events.Count != 1 || model.Valuations.Count != 1 || model.PerLocation.Count != 1)
            throw new IOException("Complete original native detail facts are unavailable.");
    }
    /// <summary>Checks only newly delivered type identity and native draft rebinding, without replaying completed matrix/navigation work.</summary>
    internal static async Task QueryAsync(IServiceProvider services, Original original, Func<HoldingDetailPage, Task> show,
        Func<HoldingDetailPage, Task> close, Func<HoldingDetailPage, string, Task<object>> observe, CancellationToken ct)
    {
        var model = Model(services, original, original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), new HoldingOverviewNativeReview.Navigation());
        var page = new HoldingDetailPage(model); await show(page);
        await AwaitAsync(() => model.Loading.IsReady || model.Loading.HasFailed, "original query source", ct); RequireReady(model);
        model.PriceEditor.Raw = "77"; var previous = model.PriceEditor;
        var before = await observe(page, "original-query-source");
        var next = (await original.Fixture.GetRequiredService<HoldingStore>().GetTypesAsync(ct)).Single(t => t.Name == "D418 original unknown holding");
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = next.Id }); await model.LoadAsync();
        if (!model.Loading.IsReady || model.NotFound || model.Name != next.Name || !model.ValueUnknown || model.ValueText is not null
            || model.PriceEditor.Raw != string.Empty || ReferenceEquals(previous, model.PriceEditor) || previous.CanSubmit || model.HistoryEditor.HasUndo)
            throw new IOException("Original query identity retained old draft, values or authority.");
        var after = await observe(page, "original-query-switch");
        original.Cases.Add(new { State = "query-identity", Before = before, After = after, ProcessId = Environment.ProcessId,
            FinancialCommands = 0, SeedsReplayed = 0, OldEditorTargetRetired = true });
        await close(page); await RequireUnchangedAsync(original, ct);
    }
    /// <summary>Checks only a final retired readback callback on the retained source; no accepted native case is replayed.</summary>
    internal static async Task<object> RetiredCallbackAsync(IServiceProvider services, Original original, CancellationToken ct)
    {
        var model = Model(services, original, original.Fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), new HoldingOverviewNativeReview.Navigation());
        await model.LoadAsync(); RequireReady(model); model.PriceEditor.Raw = "77";
        var callback = model.PriceEditor.ReloadView ?? throw new IOException("Original detail callback is missing.");
        model.Retire(); var refused = false;
        try { await callback(); } catch (OperationCanceledException) { refused = true; }
        if (!refused || model.Loading.IsReady || model.PriceEditor.CanSubmit || model.HistoryEditor.CanOpen
            || model.PriceEditor.Raw != "77") throw new IOException("Retired original callback reactivated its view.");
        await RequireUnchangedAsync(original, ct);
        return new { RetiredCallbackRefused = true, RawDraftKept = true, OriginalTypedRowsUnchanged = true,
            FinancialCommands = 0, SeedsReplayed = 0, ProcessId = Environment.ProcessId };
    }
    /// <summary>Compares every mapped typed row with the retained source before releasing the observation.</summary>
    internal static async Task RequireUnchangedAsync(Original original, CancellationToken ct)
    {
        if (original.Rows != await ImportFinancialNativeReview.StoredAsync(original.Fixture, ct))
            throw new IOException("Read-only original detail changed typed financial rows.");
    }
    private static HoldingDetailViewModel Model(IServiceProvider services, Original original,
        IDbContextFactory<ZananceDbContext> factory, IAppInteraction port)
    {
        var model = new HoldingDetailViewModel(new HoldingStore(factory, original.Fixture.GetRequiredService<ICommercialWriteAccessSource>()),
            services.GetRequiredService<HoldingText>(), services.GetRequiredService<Translator>(), services.GetRequiredService<ILocalizationService>(),
            services.GetRequiredService<TimeProvider>(), port);
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = original.Id }); return model;
    }
    private static async Task AwaitAsync(Func<bool> ready, string stage, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (!ready()) { if (DateTime.UtcNow > deadline) throw new IOException("Pending " + stage); await Task.Delay(50, ct); }
    }
}
#endif
