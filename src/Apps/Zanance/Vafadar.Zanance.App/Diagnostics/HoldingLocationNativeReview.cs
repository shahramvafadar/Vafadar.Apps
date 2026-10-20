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

/// <summary>One original independent place creation and read-only recovery, followed by retired native captions.</summary>
internal static class HoldingLocationNativeReview
{
    private sealed class Dialogs : IAppInteraction
    {
        internal TaskCompletionSource<string?> Input = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Prompts;
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
        { if (++Prompts != 1 || initialValue is not null || maxLength != 60) throw new IOException("Original place prompt differs."); return Input.Task; }
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("Unexpected place confirmation.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("Unexpected place choice.");
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected place alert.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("Place review never saves finance or navigates.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected place failure.", exception);
    }
    /// <summary>Exact original accepted identity/rows and a live model used only to copy its retired caption.</summary>
    internal sealed record Prepared(ServiceProvider Fixture, AssetEventEditorViewModel Original, Guid TypeId, string Rows, object[] Cases);
    private static AssetEventEditorViewModel Model(IServiceProvider services, ServiceProvider fixture, IAppInteraction dialogs) => new(
        fixture.GetRequiredService<HoldingStore>(), fixture.GetRequiredService<ZananceStore>(), services.GetRequiredService<HoldingText>(),
        services.GetRequiredService<Translator>(), services.GetRequiredService<ILocalizationService>(), services.GetRequiredService<TimeProvider>(), dialogs);
    private static string Raw(AssetEventEditorViewModel vm) => JsonSerializer.Serialize(new { vm.QuantityText, vm.BasisText, vm.Date, vm.Note });
    private static string ExceptPlaces(string rows)
    {
        using var doc = JsonDocument.Parse(rows);
        return string.Join("\n", doc.RootElement.EnumerateObject().Where(p => p.Name != "AssetLocations").Select(p => p.Name + ":" + p.Value.GetRawText()));
    }
    /// <summary>Runs metadata once and retains its acceptance before any native observer can fail.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder,
        Func<AssetEventEditorPage, Task> show, Func<AssetEventEditorPage, ICommand, Task> invoke,
        Func<AssetEventEditorPage, Task> close, Func<AssetEventEditorPage, string, Task<object>> observe, CancellationToken ct)
    {
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-location.db");
        if (File.Exists(file)) throw new IOException("Preserve original place file and receipt; never replay a completed create.");
        var fixture = new ServiceCollection().AddSingleton(services.GetRequiredService<TimeProvider>()).AddZananceData(file).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var holdings = fixture.GetRequiredService<HoldingStore>(); var store = fixture.GetRequiredService<ZananceStore>();
        var settings = await store.GetSettingsAsync(ct); settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
        var type = new AssetType { Name = "D420 original item", Dimension = AssetDimension.Count, Kind = AssetKind.CountableItem, PriceCurrencyCode = "EUR" };
        await holdings.SaveTypeAsync(type, ct); await holdings.EnsureDefaultLocationAsync("D420 original safe", ct);
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct); var dialogs = new Dialogs(); var model = Model(services, fixture, dialogs);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        model.QuantityText = "2"; model.BasisText = "50"; model.Note = "D420 unchanged raw note"; var raw = Raw(model);
        var page = new AssetEventEditorPage(model); await show(page); var cases = new List<object>();
        await invoke(page, model.AddLocationCommand);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (dialogs.Prompts != 1) { if (DateTime.UtcNow > deadline) throw new IOException("Original place invocation still pending."); await Task.Delay(25, ct); }
        if (model.CanSave || !model.IsBusy || model.CanEdit) throw new IOException("Original prompt did not own its form.");
        cases.Add(await observe(page, "prompt-pending"));
        void Fail(object? sender, EventArgs args) => throw new IOException("Original deliberate postcommit place notification failure.");
        holdings.Changed += Fail;
        try { dialogs.Input.SetResult("D420 accepted safe"); await model.AddLocationCommand.ExecutionTask!; }
        finally { holdings.Changed -= Fail; }
        var acceptedRows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
        if (model.SavedLocationId is not { } id || !model.HasSavedLocationPending || model.CanSave || model.CanAddLocation || !model.CanUseSavedLocation
            || raw != Raw(model) || ExceptPlaces(before) != ExceptPlaces(acceptedRows) || (await holdings.GetLocationsAsync(ct)).Count != 2)
            throw new IOException("Original place acceptance/raw/financial boundary differs.");
        await File.WriteAllTextAsync(Path.Combine(folder, "accepted.json"), JsonSerializer.Serialize(new
        { Id = id, TypeId = type.Id, OriginalRaw = raw, KnownMetadataOnly = true, ProcessId = Environment.ProcessId, Rows = acceptedRows }), ct);
        cases.Add(await observe(page, "saved-needs-refresh"));
        await invoke(page, model.UseSavedLocationCommand);
        deadline = DateTime.UtcNow.AddSeconds(15);
        while (model.UseSavedLocationCommand.ExecutionTask is null) { if (DateTime.UtcNow > deadline) throw new IOException("Original read-only refresh pending."); await Task.Delay(25, ct); }
        await model.UseSavedLocationCommand.ExecutionTask;
        if (!model.CanSave || model.HasSavedLocationPending || model.Location?.Id != id || raw != Raw(model)
            || acceptedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || dialogs.Prompts != 1)
            throw new IOException("Original read-only place refresh changed rows, identity or raw input.");
        cases.Add(await observe(page, "saved-choice-ready")); await close(page);
        await File.WriteAllTextAsync(Path.Combine(folder, "complete-metadata.json"), JsonSerializer.Serialize(new
        { Cases = cases, Id = id, OriginalRaw = raw, NoFinancialCommand = true, NoCreateReplay = true, ProcessId = Environment.ProcessId }), ct);
        return new(fixture, model, type.Id, acceptedRows, [.. cases]);
    }
    /// <summary>Copies a known original caption into a newly translated, explicitly retired form.</summary>
    internal static async Task<AssetEventEditorViewModel> CaptionAsync(IServiceProvider services, Prepared prepared)
    {
        var model = Model(services, prepared.Fixture, new Dialogs());
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = prepared.TypeId, ["kind"] = "Opening" });
        model.QuantityText = prepared.Original.QuantityText; model.BasisText = prepared.Original.BasisText;
        model.Date = prepared.Original.Date; model.Note = prepared.Original.Note; model.ShowRetainedLocationOutcome(prepared.Original);
        if (model.CanSave || model.CanUseSavedLocation || model.CanAddLocation || !model.HasSavedLocationPending) throw new IOException("Retired place caption rearmed authority.");
        return model;
    }
}
#endif
