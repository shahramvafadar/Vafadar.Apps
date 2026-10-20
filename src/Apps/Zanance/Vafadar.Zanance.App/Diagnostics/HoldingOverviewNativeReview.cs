#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original holding overview loading/failure/retry/read-only navigation over one independent SQLite file.</summary>
internal static class HoldingOverviewNativeReview
{
    /// <summary>Retains one original fixture and all native observations; no accepted financial command exists to replay.</summary>
    internal sealed record Prepared(ServiceProvider Fixture, ServiceProvider Empty, string Rows, string EmptyRows, List<object> Cases, string SourceFolder);

    /// <summary>Only explicit independent navigation is intercepted; production list logic and SQLite are unchanged.</summary>
    internal sealed class Navigation : IAppInteraction
    {
        internal readonly List<(string Route, IDictionary<string, object>? Parameters)> Routes = [];
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) { Routes.Add((route, parameters)); return Task.CompletedTask; }
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected original overview command failure.", exception);
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected overview alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Unexpected overview confirmation.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected overview choice.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("Unexpected overview prompt.");
    }

    /// <summary>Gates only the actual context lease and one expected original read failure.</summary>
    internal sealed class Factory(IDbContextFactory<ZananceDbContext> actual) : IDbContextFactory<ZananceDbContext>
    {
        internal TaskCompletionSource? Gate;
        internal bool FailOnce;
        internal readonly IOException OriginalFailure = new("Original independent overview read refusal.");
        internal int Calls;
        public ZananceDbContext CreateDbContext() => actual.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Gate is { } gate) await gate.Task.WaitAsync(cancellationToken);
            if (FailOnce) { FailOnce = false; throw OriginalFailure; }
            return await actual.CreateDbContextAsync(cancellationToken);
        }
    }

    /// <summary>Creates only independent fictitious holding facts once; list reads must never modify any table.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder, CancellationToken ct)
    {
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-overview.db");
        var emptyFile = Path.Combine(folder, "original-empty.db");
        if (File.Exists(file) || File.Exists(emptyFile)) throw new IOException("Original overview fixtures exist; preserve original completed/pending receipts.");
        ServiceProvider Fixture(string path) => new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(path).BuildServiceProvider();
        var fixture = Fixture(file); var empty = Fixture(emptyFile);
        fixture.MigrateLocalDatabase<ZananceDbContext>(); empty.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<HoldingStore>(); var day = new DateOnly(2026, 10, 1);
        var known = new AssetType { Name = "D418 original counted holding", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", Note = "Original overview metadata" };
        var unknown = new AssetType { Name = "D418 original unknown holding", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "USD" };
        var archived = new AssetType { Name = "D418 original archived holding", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", IsArchived = true };
        foreach (var type in new[] { known, unknown, archived }) await store.SaveTypeAsync(type, ct);
        var place = await store.EnsureDefaultLocationAsync("D418 original safe", ct);
        foreach (var type in new[] { known, unknown })
            if (!(await store.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
                Kind = AssetEventKind.Opening, Date = day, Quantity = type.Id == known.Id ? 2000 : 1000 }, [], ct)).Succeeded)
                throw new IOException("Original overview quantity seed failed.");
        await store.SaveValuationAsync(new AssetValuation { AssetTypeId = known.Id, CurrencyCode = "EUR", Date = day, PricePerUnitMilli = 2500000 }, ct);
        await fixture.GetRequiredService<ZananceStore>().UpdateSettingsAsync(settings => settings.Mode = ExperienceMode.Advanced, ct);
        return new(fixture, empty, await ImportFinancialNativeReview.StoredAsync(fixture, ct),
            await ImportFinancialNativeReview.StoredAsync(empty, ct), [], folder);
    }

    /// <summary>Reopens only the exact original terminal observer fixture, retaining original case/PID receipts without seeding again.</summary>
    internal static async Task<Prepared> ContinueAsync(IServiceProvider services, string source, string output, CancellationToken ct)
    {
        var oldRoot = Directory.GetParent(source)?.FullName ?? throw new IOException("Original overview root missing.");
        if (!File.Exists(Path.Combine(oldRoot, "error.txt"))
            || !File.ReadAllText(Path.Combine(oldRoot, "error.txt")).Contains("Original overview row binding/name differs.", StringComparison.Ordinal))
            throw new IOException("Original terminal overview caption refusal required.");
        using var identity = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(oldRoot, "process.json"), ct));
        var originalProcess = identity.RootElement.GetProperty("ProcessId").GetInt32();
        ServiceProvider Fixture(string name)
        {
            var file = Path.Combine(source, name);
            if (!File.Exists(file) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Exact original ordinary overview file required.");
            return new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
        }
        var fixture = Fixture("original-overview.db"); var empty = Fixture("original-empty.db");
        var store = fixture.GetRequiredService<HoldingStore>(); var types = await store.GetTypesAsync(ct);
        var events = await store.GetEventsAsync(cancellationToken: ct); var prices = await store.GetValuationsAsync(cancellationToken: ct);
        if (types.Count != 3 || events.Count != 2 || prices.Count != 1
            || types.Count(t => t.IsArchived) != 1 || prices[0].PricePerUnitMilli != 2500000
            || events.Single(e => e.AssetTypeId == types.Single(t => t.Name == "D418 original counted holding").Id).Quantity != 2000
            || events.Single(e => e.AssetTypeId == types.Single(t => t.Name == "D418 original unknown holding").Id).Quantity != 1000
            || (await fixture.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new IOException("Original completed overview source differs; never replace it with another seed.");
        var cases = new List<object>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(source, "receipts"), "*.json").Order(StringComparer.Ordinal))
        {
            using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct)); var item = receipt.RootElement;
            if (item.GetProperty("ProcessId").GetInt32() != originalProcess || item.GetProperty("FinancialCommands").GetInt32() != 0
                || !item.GetProperty("ReadOnly").GetBoolean()) throw new IOException("Original overview case receipt differs.");
            cases.Add(item.Clone());
        }
        if (cases.Count == 0) throw new IOException("Original overview case receipts missing.");
        var prepared = new Prepared(fixture, empty, await ImportFinancialNativeReview.StoredAsync(fixture, ct),
            await ImportFinancialNativeReview.StoredAsync(empty, ct), cases, source);
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "retained-input-checkpoint.json"), JsonSerializer.Serialize(new
        { OriginalProcessId = originalProcess, OriginalSource = source, CasesRetainOriginalProcessIds = true, OriginalCases = cases.Count,
            CapturedAfterTerminalCaptionRefusal = true, OriginalSeedsReplayed = 0, FinancialCommands = 0,
            SourceDigest = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(prepared.Rows))),
            EmptyDigest = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(prepared.EmptyRows))) }), ct);
        return prepared;
    }

    /// <summary>Opens only the original completed Android overview's owned files for changed-caption reads; never migrates or seeds them.</summary>
    internal static async Task<Prepared> ContinueNativeAsync(IServiceProvider services, string source, string reportPath,
        string originalComparison, string output, CancellationToken ct)
    {
        using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath, ct)); var report = saved.RootElement;
        if (report.GetProperty("Comparison").GetString() != originalComparison
            || report.GetProperty("Package").GetString() != "pro.vafadar.zanance.androidhomereview"
            || !report.GetProperty("OriginalOverviewAndEmptyRowsUnchanged").GetBoolean()
            || !report.GetProperty("OriginalBaseStoreUnchanged").GetBoolean() || report.GetProperty("FinancialCommands").GetInt32() != 0)
            throw new IOException("Exact original completed overview report required.");
        ServiceProvider Fixture(string name)
        {
            var file = Path.Combine(source, name);
            if (!File.Exists(file) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Original ordinary overview file required; no replacement seed.");
            return new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
        }
        var fixture = Fixture("original-overview.db"); var empty = Fixture("original-empty.db");
        var reader = fixture.GetRequiredService<HoldingStore>(); var inputs = (await reader.ReadOverviewSnapshotAsync(ct)).Inputs;
        if (inputs.Types.Count != 3 || inputs.Events.Count != 2 || inputs.Valuations.Count != 1 || inputs.Types.Count(t => t.IsArchived) != 1
            || inputs.Settings?.Mode != ExperienceMode.Advanced || inputs.Valuations[0].PricePerUnitMilli != 2500000
            || inputs.Events.Single(e => e.AssetTypeId == inputs.Types.Single(t => t.Name == "D418 original counted holding").Id).Quantity != 2000
            || inputs.Events.Single(e => e.AssetTypeId == inputs.Types.Single(t => t.Name == "D418 original unknown holding").Id).Quantity != 1000)
            throw new IOException("Original completed overview facts differ.");
        var emptyInputs = (await empty.GetRequiredService<HoldingStore>().ReadOverviewSnapshotAsync(ct)).Inputs;
        if (emptyInputs.Settings is not null || emptyInputs.Types.Count != 0 || emptyInputs.Events.Count != 0 || emptyInputs.Locations.Count != 0)
            throw new IOException("Original empty overview was initialized or replaced.");
        var cases = report.GetProperty("Cases").EnumerateArray().Select(item => (object)item.Clone()).ToList();
        var prepared = new Prepared(fixture, empty, await ImportFinancialNativeReview.StoredAsync(fixture, ct),
            await ImportFinancialNativeReview.StoredAsync(empty, ct), cases, source);
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { OriginalComparison = originalComparison,
            OriginalProcessId = report.GetProperty("ProcessId").GetInt32(), OriginalCasesRetainProcessIds = true,
            OriginalSeedsReplayed = 0, FinancialCommands = 0,
            SourceDigest = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(prepared.Rows))),
            EmptyDigest = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(prepared.EmptyRows))) }), ct);
        return prepared;
    }

    /// <summary>Qualifies only changed loading/Retry captions on the retained original fixture; no ready row, navigation or seed is replayed.</summary>
    internal static async Task RunCoverCaptionsAsync(IServiceProvider services, Prepared prepared, string stage,
        Func<HoldingsPage, Task> show, Func<HoldingsPage, Task> close, Func<HoldingsPage, string, Task<object>> observe, CancellationToken ct)
    {
        var current = Page(services, prepared.Fixture); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        current.Factory.Gate = gate; current.Factory.FailOnce = true;
        var previous = Failures.Observed; var expected = 0; var unexpected = false;
        Failures.Observed = failure =>
        {
            if (ReferenceEquals(failure, current.Factory.OriginalFailure)) expected++;
            else { unexpected = true; previous?.Invoke(failure); }
        };
        try
        {
            await show(current.Page); await AwaitAsync(() => current.Factory.Calls == 1, "changed original loading caption", ct);
            await observe(current.Page, stage + "-loading");
            prepared.Cases.Add(new { Stage = stage, Outcome = "loading-caption", CaptionOnly = true, FinancialCommands = 0, OriginalSeedsReplayed = 0, ProcessId = Environment.ProcessId });
            gate.SetResult();
            await AwaitAsync(() => current.Model.Loading.HasFailed && !current.Model.Loading.IsLoading && (expected == 1 || unexpected), "changed original Retry caption", ct);
            if (expected != 1 || unexpected) throw new IOException("Original changed-caption refusal differs.");
            await observe(current.Page, stage + "-error");
            prepared.Cases.Add(new { Stage = stage, Outcome = "retry-caption", CaptionOnly = true, FinancialCommands = 0, OriginalSeedsReplayed = 0, ProcessId = Environment.ProcessId });
            await close(current.Page); await ValidateAsync(prepared, ct);
        }
        finally { Failures.Observed = previous; }
    }

    /// <summary>Continues only a pending ready caption over the original completed file, without another fault/retry/seed sequence.</summary>
    internal static async Task RunReadyCaptionAsync(IServiceProvider services, Prepared prepared, string stage,
        Func<HoldingsPage, Task> show, Func<HoldingsPage, Task> close, Func<HoldingsPage, string, Task<object>> observe, CancellationToken ct)
    {
        var current = Page(services, prepared.Fixture); await show(current.Page);
        await AwaitAsync(() => current.Model.Loading.IsReady || current.Model.Loading.HasFailed, "pending original ready caption", ct);
        RequireReady(current.Model); await observe(current.Page, stage + "-ready"); await close(current.Page);
        prepared.Cases.Add(new { Stage = stage, Outcome = "ready-caption-continuation", FinancialCommands = 0,
            OriginalSeedsReplayed = 0, OriginalFaultAndRetryNotReplayed = true, ProcessId = Environment.ProcessId });
        await ValidateAsync(prepared, ct);
    }

    /// <summary>Creates the actual original list model with real persistence and an explicit native-effect port.</summary>
    internal static (HoldingsPage Page, HoldingsViewModel Model, Factory Factory, Navigation Navigation) Page(
        IServiceProvider services, ServiceProvider fixture)
    {
        var factory = new Factory(fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var navigation = new Navigation();
        var model = new HoldingsViewModel(new HoldingStore(factory, fixture.GetRequiredService<ICommercialWriteAccessSource>()),
            services.GetRequiredService<HoldingText>(), services.GetRequiredService<Translator>(), services.GetRequiredService<TimeProvider>(), navigation);
        return (new HoldingsPage(model), model, factory, navigation);
    }

    /// <summary>Waits for the same native page/load; an observation timeout never starts another load or recreates a fixture.</summary>
    internal static async Task AwaitAsync(Func<bool> condition, string boundary, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (!condition())
        { if (DateTime.UtcNow > deadline) throw new IOException("Original overview boundary remains pending: " + boundary); await Task.Delay(30, ct); }
    }

    /// <summary>Checks complete ready state against the original actual file, without selecting or writing financial data.</summary>
    internal static void RequireReady(HoldingsViewModel model)
    {
        if (!model.Loading.IsReady || model.IsEmpty || !model.IsAdvanced || model.Rows.Count != 2
            || model.Rows.Any(r => r.Name.Contains("archived", StringComparison.Ordinal))
            || model.Rows.Count(r => r.ValueUnknown) != 1 || model.KnownValueText is null || model.IncompleteText is null)
            throw new IOException("Original ready overview is incomplete.");
    }

    /// <summary>Exercises the actual changed loading/error/retry surfaces in one native display context.</summary>
    internal static async Task RunContextAsync(IServiceProvider services, Prepared prepared, string stage, string receipts,
        bool original, Func<HoldingsPage, Task> show, Func<HoldingsPage, Task> close,
        Func<HoldingsPage, string, object?, Task> invoke, Func<HoldingsPage, string, Task<object>> observe, CancellationToken ct)
    {
        var current = Page(services, prepared.Fixture); var page = current.Page; var model = current.Model; var factory = current.Factory;
        factory.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); factory.FailOnce = true;
        async Task Record(string outcome)
        {
            var receipt = new { Stage = stage, Outcome = outcome, ProcessId = Environment.ProcessId,
                ReadOnly = true, FinancialCommands = 0, factory.Calls, model.Loading.IsReady, model.Loading.IsLoading, model.Loading.HasFailed, Rows = model.Rows.Count };
            prepared.Cases.Add(receipt); Directory.CreateDirectory(receipts);
            await File.WriteAllTextAsync(Path.Combine(receipts, stage + "-" + outcome + ".json"), JsonSerializer.Serialize(receipt), ct);
        }
        var previousObserver = Failures.Observed; var expectedFailures = 0; var unexpectedFailure = false;
        Failures.Observed = error =>
        {
            if (ReferenceEquals(error, factory.OriginalFailure)) expectedFailures++;
            else { unexpectedFailure = true; previousObserver?.Invoke(error); }
        };
        try
        {
        await show(page); await AwaitAsync(() => factory.Calls == 1, "original loading context", ct);
        await Record("loading"); await observe(page, stage + "-loading"); factory.Gate.SetResult();
        await AwaitAsync(() => model.Loading.HasFailed && !model.Loading.IsLoading && (expectedFailures == 1 || unexpectedFailure), "original read refusal", ct);
        if (model.Rows.Count != 0 || model.Loading.IsReady || expectedFailures != 1)
            throw new IOException("Failed original overview uncovered a false result or lost its original refusal.");
        await Record("error"); await observe(page, stage + "-error");
        factory.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); var before = factory.Calls;
        await invoke(page, "retry", null); await AwaitAsync(() => factory.Calls > before, "original explicit Retry", ct);
        if (!model.Loading.IsLoading || model.Loading.IsReady || model.Loading.HasFailed)
            throw new IOException("Original Retry did not preserve its cover.");
        await Record("retry-loading"); factory.Gate.SetResult();
        await AwaitAsync(() => model.Loading.IsReady || model.Loading.HasFailed, "original retry readback", ct);
        RequireReady(model); await Record("ready"); await observe(page, stage + "-ready");
        if (original)
        {
            var row = model.Rows.Single(r => !r.ValueUnknown);
            await invoke(page, "open", row); await AwaitAsync(() => current.Navigation.Routes.Count == 1, "original native row action", ct);
            if (current.Navigation.Routes[0].Route != AppRoutes.HoldingDetailRoute
                || !Equals(current.Navigation.Routes[0].Parameters!["id"], row.Id)) throw new IOException("Original overview native row targeted another holding.");
            await invoke(page, "add", null); await AwaitAsync(() => current.Navigation.Routes.Count == 2, "original native Add", ct);
            if (current.Navigation.Routes[1].Route != AppRoutes.AssetTypeEditorRoute) throw new IOException("Original overview Add targeted another route.");
            factory.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously); before = factory.Calls;
            var retiring = model.LoadAsync(); await AwaitAsync(() => factory.Calls > before, "original retained reload", ct);
            await observe(page, stage + "-departure-loading"); await close(page); await retiring;
            if (model.Loading.IsReady || current.Navigation.Routes.Count != 2) throw new IOException("Departed original overview retained native readiness.");
            await Record("retired");
        }
        else await close(page);
        await ValidateAsync(prepared, ct);
        }
        finally { Failures.Observed = previousObserver; }
    }

    /// <summary>Qualifies a genuinely empty original file without creating settings, types, places or money.</summary>
    internal static async Task RunEmptyAsync(IServiceProvider services, Prepared prepared,
        Func<HoldingsPage, Task> show, Func<HoldingsPage, Task> close, Func<HoldingsPage, string, Task<object>> observe, CancellationToken ct)
    {
        var current = Page(services, prepared.Empty); await show(current.Page);
        await AwaitAsync(() => current.Model.Loading.IsReady || current.Model.Loading.HasFailed, "original empty overview", ct);
        if (!current.Model.Loading.IsReady || !current.Model.IsEmpty || current.Model.IsAdvanced || current.Model.Rows.Count != 0)
            throw new IOException("Original empty overview differs.");
        await observe(current.Page, "original-empty"); await close(current.Page); await ValidateAsync(prepared, ct);
        prepared.Cases.Add(new { Stage = "original-empty", NoInitializationWrite = true, FinancialCommands = 0, ProcessId = Environment.ProcessId });
    }

    /// <summary>Verifies exact original stored rows after all native loading/failure/retry/departure/caption reads.</summary>
    internal static async Task ValidateAsync(Prepared prepared, CancellationToken ct)
    {
        if (prepared.Rows != await ImportFinancialNativeReview.StoredAsync(prepared.Fixture, ct)
            || prepared.EmptyRows != await ImportFinancialNativeReview.StoredAsync(prepared.Empty, ct))
            throw new IOException("Original overview read/native observation changed persisted data.");
    }
}
#endif
