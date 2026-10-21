#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>One independent original file and actual production plan/account pages shared by native platform checks.</summary>
internal sealed class PlanEditorNativeReview : IDisposable
{
    /// <summary>Owns only the independent original fixture services.</summary>
    internal readonly ServiceProvider Services;
    /// <summary>Supplies fixed original-file contexts and bounded read faults.</summary>
    internal readonly ReviewFactory Factory;
    /// <summary>Uses the original fixture for actual account and ledger reads.</summary>
    internal readonly ZananceStore Store;
    /// <summary>Uses the original fixture for actual plan writers and reads.</summary>
    internal readonly PlanStore Plans;
    /// <summary>Supplies the actual injected form date without changing the device clock.</summary>
    internal readonly TimeProvider Time;
    private readonly IServiceProvider _host;
    private readonly string _folder;
    private readonly List<string> _routes = [];
    private string _phase = "prepared";
    private PlanEditorViewModel? _current;
    /// <summary>Gets native and source stage observations without granting any write authority.</summary>
    internal List<object> Observations { get; } = [];
    /// <summary>Gets the original committed plan after the one actual historical split.</summary>
    internal Guid ContinuationId { get; private set; }
    /// <summary>Gets current host translations for the actual production controls.</summary>
    internal Translator Translator => _host.GetRequiredService<Translator>();
    /// <summary>Gets the host display context without changing OS settings.</summary>
    internal ILocalizationService Localization => _host.GetRequiredService<ILocalizationService>();

    private PlanEditorNativeReview(IServiceProvider host, string folder)
    {
        _host = host; _folder = folder; Time = host.GetRequiredService<TimeProvider>();
        Services = new ServiceCollection().AddSingleton(Time).AddZananceData(Path.Combine(folder, "workflow.db")).BuildServiceProvider();
        Factory = new(Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var access = new ReviewAccess(); Store = new(Factory, Time, access); Plans = new(Factory, Store, access);
    }
    /// <summary>Seeds only a fresh separately owned empty advanced fixture; existing accepted work must be continued explicitly.</summary>
    internal static async Task<PlanEditorNativeReview> CreateAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        if (Directory.Exists(folder)) throw new IOException("Retain the original plan fixture; never seed or repeat accepted actions.");
        Directory.CreateDirectory(folder); var result = new PlanEditorNativeReview(host, folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "started.json"), JsonSerializer.Serialize(new { ProcessId = Environment.ProcessId }), ct);
        await Task.Run(() => result.Services.MigrateLocalDatabase<ZananceDbContext>(), ct);
        await result.Store.SaveSettingsAsync(new() { Mode = ExperienceMode.Advanced, OnboardingCompleted = true }, ct);
        await result.Store.SaveCategoryAsync(new() { Kind = CategoryKind.Expense, SystemKey = DefaultCategories.Uncategorized }, ct);
        await result.WriteRowsAsync("prepared", ct); result.Store.Changed += result.Accepted; result.Plans.Changed += result.Accepted;
        return result;
    }
    /// <summary>Continues only a completed native action checkpoint; never seeds or repeats an accepted writer.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        if (!File.Exists(Path.Combine(folder, "actions-complete.json"))) throw new IOException("Original completed action checkpoint required.");
        var result = new PlanEditorNativeReview(host, folder);
        if ((await result.Store.GetAccountsAsync(cancellationToken: ct)).Count != 1 || (await result.Store.GetEntriesAsync(cancellationToken: ct)).Count != 2
            || (await result.Plans.GetStatesAsync(cancellationToken: ct)).Count != 2) throw new IOException("Original action rows differ.");
        result.ContinuationId = (await result.Plans.GetSchedulesAsync(ct)).Single(s => s.PreviousScheduleId is not null).Id;
        using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "actions-complete.json"), ct));
        var expected = receipt.RootElement;
        using var actual = JsonDocument.Parse(await result.RowsAsync(ct));
        foreach (var name in new[] { "Accounts", "Settings", "Schedules", "States", "Entries" })
            if (expected.GetProperty(name).GetRawText() != actual.RootElement.GetProperty(name).GetRawText())
                throw new IOException("Original completed rows differ: " + name);
        return result;
    }
    /// <summary>Reopens only original empty prepared rows after an observer stopped before any accepted account write.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueEmptyAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        if (!File.Exists(Path.Combine(folder, "prepared.json")) || File.Exists(Path.Combine(folder, "account-create-accepted.json")))
            throw new IOException("Only original preparation before any accepted account action may continue.");
        var result = new PlanEditorNativeReview(host, folder);
        using var expected = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "prepared.json"), ct));
        using var actual = JsonDocument.Parse(await result.RowsAsync(ct));
        foreach (var name in new[] { "Accounts", "Settings", "Schedules", "States", "Entries" })
            if (expected.RootElement.GetProperty(name).GetRawText() != actual.RootElement.GetProperty(name).GetRawText())
                throw new IOException("The original empty prepared rows changed.");
        if (actual.RootElement.GetProperty("Accounts").GetArrayLength() != 0) throw new IOException("Original empty account set required.");
        await using (var db = await result.Factory.CreateDbContextAsync(ct))
            if (await db.Categories.CountAsync(ct) != 1 || !await db.Categories.AnyAsync(c => c.SystemKey == DefaultCategories.Uncategorized && c.Kind == CategoryKind.Expense, ct))
                throw new IOException("Original prepared category required.");
        result.Store.Changed += result.Accepted; result.Plans.Changed += result.Accepted;
        return result;
    }
    /// <summary>Creates the actual complete original form with explicit host-native effects and original fixture stores.</summary>
    internal PlanEditorViewModel Editor() => new(Store, Plans, Translator, Localization, _host.GetRequiredService<IDateFormatter>(),
        Time, _host.GetRequiredService<ReminderService>(), _host.GetRequiredService<IAppInteraction>());
    /// <summary>Retains the actual page route until its nested child and original return have completed.</summary>
    internal async Task<string> ShowAsync(Page page, Dictionary<string, object>? query = null)
    {
        var route = "original-plan-form-" + Guid.NewGuid().ToString("N");
        Routing.RegisterRoute(route, new PageFactory(() => page)); _routes.Add(route);
        await Shell.Current.GoToAsync(route, query ?? new()); return route;
    }
    /// <summary>Returns to the ordinary independent root and finishes its reader before fixture disposal.</summary>
    internal async Task LeaveAsync(string? route = null)
    {
        await Shell.Current.GoToAsync("//home", false);
        if (Shell.Current.CurrentPage is HomePage home) await home.PendingPublication;
        if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
    }
    private void Accepted(object? sender, EventArgs args)
    {
        var path = Path.Combine(_folder, _phase + "-accepted.json");
        if (File.Exists(path)) throw new IOException("An accepted original plan action was repeated.");
        // Known commit precedes this fallible observer. Its original typed rows are retained before any image/report check.
        Task.Run(() => WriteRowsAsync(_phase + "-accepted", CancellationToken.None)).GetAwaiter().GetResult();
    }
    private async Task WriteRowsAsync(string name, CancellationToken ct)
    {
        await using var db = await Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        var rows = new { ProcessId = Environment.ProcessId, Phase = _phase,
            Accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync(ct),
            Settings = await db.Settings.AsNoTracking().ToListAsync(ct), Schedules = await db.Schedules.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct),
            States = await db.OccurrenceStates.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct), Entries = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct) };
        await File.WriteAllTextAsync(Path.Combine(_folder, name + ".json"), JsonSerializer.Serialize(rows), ct);
    }
    /// <summary>Compares every original typed row around a read-only native context.</summary>
    internal async Task<string> RowsAsync(CancellationToken ct)
    {
        await using var db = await Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        return JsonSerializer.Serialize(new { Accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync(ct),
            Settings = await db.Settings.AsNoTracking().ToListAsync(ct), Schedules = await db.Schedules.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct),
            States = await db.OccurrenceStates.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct), Entries = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct) });
    }
    /// <summary>Checks covered reads, native Retry, raw readiness retention and retired query commands without financial Save.</summary>
    internal async Task ContextAsync(string context, Func<Page, System.Windows.Input.ICommand, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, bool resumeReady = false, bool resumeReviewed = false, bool resumeWorkFailed = false)
    {
        var before = await RowsAsync(ct); var parent = Editor(); var page = new PlanEditorPage(parent);
        Factory.GateRequested = false; var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!resumeReady) Factory.Gate = gate;
        var route = await ShowAsync(page, new() { ["id"] = ContinuationId });
        if (!resumeReady)
        {
            await WaitAsync(() => Factory.GateRequested && parent.Loading.IsLoading, ct);
            await observe(page, context + "-loading"); Factory.RefuseOnce = true; gate.SetResult();
            await WaitAsync(() => parent.Loading.HasFailed && parent.CanReloadForm, ct); await observe(page, context + "-read-failed");
            await invoke(page, parent.ReloadFormCommand); await WaitAsync(() => parent.CanInteract, ct);
            await observe(page, context + "-ready");
        }
        else await WaitAsync(() => parent.CanInteract, ct);
        parent.Name += " retained draft"; parent.Note = "Unsaved original note\nSecond line";
        await Task.Delay(150, ct);
        var raw = (parent.Name, parent.Note, parent.AmountText, parent.ApplyFrom, parent.Start, parent.ContractReference);
        if (!resumeReviewed)
        {
        Factory.RefuseOnce = true; var review = parent.ReviewWorkCommand; await invoke(page, review);
        await WaitAsync(() => review.ExecutionTask is not null, ct); await review.ExecutionTask!.WaitAsync(ct);
        await WaitAsync(() => !parent.CanSave && parent.WorkText == Translator["Editor_WorkReadFailed"], ct);
        await File.WriteAllTextAsync(Path.Combine(_folder, context + "-readiness-evidence-" + Environment.ProcessId + ".json"),
            JsonSerializer.Serialize(new { Before = new { raw.Name, raw.Note, raw.AmountText, raw.ApplyFrom, raw.Start, raw.ContractReference },
                After = new { parent.Name, parent.Note, parent.AmountText, parent.ApplyFrom, parent.Start, parent.ContractReference },
                parent.FormVisible, parent.CanInteract, parent.Loading.IsReady, parent.LoadError }), ct);
        if (!parent.FormVisible || raw != (parent.Name, parent.Note, parent.AmountText, parent.ApplyFrom, parent.Start, parent.ContractReference))
            throw new IOException("Readiness failure covered or changed the original complete raw draft.");
        if (!resumeWorkFailed) await observe(page, context + "-work-failed");
        await invoke(page, parent.ReviewWorkCommand); await WaitAsync(() => parent.CanSave, ct);
        if (raw != (parent.Name, parent.Note, parent.AmountText, parent.ApplyFrom, parent.Start, parent.ContractReference))
            throw new IOException("Readiness review replaced original raw fields.");
        await observe(page, context + "-work-reviewed");
        }
        var save = parent.SaveCommand; var disclosure = parent.ToggleContractCommand;
        await parent.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() });
        await WaitAsync(() => parent.Loading.HasFailed && parent.LoadError == Translator["Plan_SourceUnavailable"], ct);
        await save.ExecuteAsync(null); disclosure.Execute(null);
        if (parent.CanSave || parent.FormVisible) throw new IOException("Retired commands borrowed a missing query.");
        await observe(page, context + "-missing");
        if (before != await RowsAsync(ct)) throw new IOException("Read-only original context changed typed financial/reference rows.");
        await LeaveAsync(route);
    }
    /// <summary>Rereads only changed original loading/Retry/missing captions, with no financial or metadata action.</summary>
    internal async Task CaptionsAsync(string context, Func<Page, string, Task> observe, CancellationToken ct)
    {
        var before = await RowsAsync(ct); var model = Editor(); var page = new PlanEditorPage(model);
        Factory.GateRequested = false; var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); Factory.Gate = gate;
        var route = await ShowAsync(page, new() { ["id"] = ContinuationId });
        await WaitAsync(() => Factory.GateRequested && model.Loading.IsLoading, ct);
        await observe(page, context + "-loading"); Factory.RefuseOnce = true; gate.SetResult();
        await WaitAsync(() => model.Loading.HasFailed && model.CanReloadForm, ct); await observe(page, context + "-read-failed");
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() });
        await WaitAsync(() => model.Loading.HasFailed && model.LoadError == Translator["Plan_SourceUnavailable"], ct);
        await observe(page, context + "-missing");
        if (before != await RowsAsync(ct)) throw new IOException("Caption-only observation changed original rows.");
        await LeaveAsync(route);
    }
    /// <summary>Reads the original plan after one explicit fictional metadata boundary, without creating another account or writing money.</summary>
    internal async Task ArchivedReferenceAsync(Func<Page, string, Task> observe, CancellationToken ct)
    {
        _phase = "archived-reference"; await WriteRowsAsync("archive-before", ct);
        await using (var db = await Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            if (await db.Accounts.CountAsync(ct) != 1) throw new IOException("One original fictional account required.");
            await db.Accounts.ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsArchived, true), ct);
        }
        await WriteRowsAsync("archive-accepted", ct); var before = await RowsAsync(ct);
        var model = Editor(); var page = new PlanEditorPage(model); var route = await ShowAsync(page, new() { ["id"] = ContinuationId });
        await WaitAsync(() => model.FormVisible, ct);
        if (!model.HasFormContent || model.NeedsAccount || model.Account is null || model.FormReview?.Inputs.Accounts.Single().IsArchived != true)
            throw new IOException("An original archived plan account was replaced by a new-account alternative.");
        var picker = page.FindByName<Picker>("AccountInput");
        if (!ReferenceEquals(picker.SelectedItem, model.Account)) throw new IOException("Original archived picker selection differs.");
        await observe(page, "native-original-archived-account");
        if (before != await RowsAsync(ct)) throw new IOException("Archived reference display changed original rows.");
        await LeaveAsync(route);
    }
    /// <summary>Runs one native first-account return, plan create and historical split with known notification failure.</summary>
    internal async Task RunActionsAsync(Func<Page, System.Windows.Input.ICommand, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, bool skipNoAccountObservation = false)
    {
        var parent = Editor(); _current = parent; var page = new PlanEditorPage(parent); var route = await ShowAsync(page);
        await WaitAsync(() => parent.NeedsAccount && parent.CanInteract, ct);
        var draft = parent.FormDraftId; if (!skipNoAccountObservation) await observe(page, "native-no-account");
        AccountEditorViewModel? child = null;
        Routing.UnRegisterRoute(AppShell.AccountEditorRoute);
        Routing.RegisterRoute(AppShell.AccountEditorRoute, new PageFactory(() =>
        {
            child = new(Store, Plans, Translator, Localization, Time, _host.GetRequiredService<IAppInteraction>());
            return new AccountEditorPage(child);
        }));
        try
        {
            _phase = "account-create";
            var intention = Path.Combine(_folder, _phase + "-intention.json");
            if (File.Exists(intention)) intention = Path.Combine(_folder, _phase + "-intention-" + Environment.ProcessId + ".json");
            await File.WriteAllTextAsync(intention, JsonSerializer.Serialize(new { ParentDraft = draft, ProcessId = Environment.ProcessId }), ct);
            await invoke(page, parent.AddAccountCommand); await WaitAsync(() => Shell.Current.CurrentPage is AccountEditorPage && child?.CanSave == true, ct);
            child!.Form.Name = "D429 original payment account";
            await invoke(Shell.Current.CurrentPage, child.SaveCommand);
            await WaitAsync(() => ReferenceEquals(Shell.Current.CurrentPage, page) && parent.CanInteract, ct);
            if (parent.FormDraftId != draft || parent.Account?.Name != child.Form.Name || (await Plans.GetSchedulesAsync(ct)).Count != 0
                || (await Store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                throw new IOException("The original plan account return replaced the draft or wrote financial data.");
            await observe(page, "native-account-return");
        }
        finally { Routing.UnRegisterRoute(AppShell.AccountEditorRoute); Routing.RegisterRoute(AppShell.AccountEditorRoute, typeof(AccountEditorPage)); }
        var today = DateOnly.FromDateTime(Time.GetLocalNow().DateTime); var start = today.AddDays(3);
        parent.Name = "D429 original recurring plan"; parent.AmountText = MoneyText.ForInput(1234, "EUR", Localization.CurrentCulture);
        parent.Note = "Original native note\nRetained second line"; parent.PresetIndex = 3; parent.Start = start;
        parent.ShowContract = true; parent.ContractProvider = "D429 fictitious provider"; parent.ContractReference = "D429-original-reference";
        parent.ReminderEnabled = false;
        await observe(page, "native-plan-before-save");
        _phase = "new-plan-save";
        await File.WriteAllTextAsync(Path.Combine(_folder, _phase + "-intention.json"), JsonSerializer.Serialize(new { Id = parent.FormDraftId, Amount = 1234, Start = start }), ct);
        await invoke(page, parent.SaveCommand); await WaitAsync(() => parent.FormAccepted && !ReferenceEquals(Shell.Current.CurrentPage, page), ct);
        var original = (await Plans.GetSchedulesAsync(ct)).Single();
        if (original.Id != draft || original.Amount != 1234 || original.ContractReference != "D429-original-reference" || original.Rule.Start != start)
            throw new IOException("The native original plan create differs from the reviewed fields.");
        await LeaveAsync(route);
        // One atomic fictitious history seed. Native plan saves never create or change a ledger amount.
        _phase = "history-seed";
        var entries = new[] { start, start.AddMonths(1) }.Select((date, index) => new LedgerEntry
        { AccountId = original.AccountId, Kind = EntryKind.Expense, Amount = 1200 + index * 100, Date = date,
            ScheduleId = original.Id, OccurrenceDate = date, Title = "D429 original recorded payment " + index, Note = "Preserve this original financial row" }).ToArray();
        var states = entries.Select(e => new OccurrenceState { ScheduleId = original.Id, OriginalDate = e.Date,
            Status = OccurrenceStatus.Settled, EntryId = e.Id, PaidAmount = e.Amount }).ToArray();
        await File.WriteAllTextAsync(Path.Combine(_folder, "history-seed-intention.json"), JsonSerializer.Serialize(new { Entries = entries, States = states }), ct);
        await using (var db = await Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.Entries.AddRange(entries); db.OccurrenceStates.AddRange(states); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }
        await WriteRowsAsync("history-seed-accepted", ct);
        var beforeEntries = await Store.GetEntriesAsync(cancellationToken: ct); var beforeStates = await Plans.GetStatesAsync(cancellationToken: ct);
        parent = Editor(); _current = parent; page = new(parent); route = await ShowAsync(page, new() { ["id"] = original.Id });
        await WaitAsync(() => parent.CanInteract && parent.ShowApplyFrom, ct);
        parent.ApplyFrom = start.AddMonths(1); parent.Name = "D429 explicitly adjusted future plan";
        parent.AmountText = MoneyText.ForInput(2345, "EUR", Localization.CurrentCulture); parent.Note = "Explicitly retained future note";
        _phase = "split-save";
        var failures = 0;
        void RefuseNotification(object? sender, EventArgs args)
        {
            if (_phase != "split-save") return;
            if (_current?.FormAccepted != true) throw new IOException("Original split acceptance must precede notification.");
            failures++; throw new IOException("Deliberate original split notification refusal.");
        }
        Plans.Changed += RefuseNotification;
        await File.WriteAllTextAsync(Path.Combine(_folder, "split-save-intention.json"), JsonSerializer.Serialize(new { Id = parent.FormDraftId, Previous = original.Id, From = parent.ApplyFrom, Amount = 2345 }), ct);
        try { await invoke(page, parent.SaveCommand); await WaitAsync(() => parent.FormAccepted && !parent.IsBusy, ct); }
        finally { Plans.Changed -= RefuseNotification; }
        if (failures != 1 || parent.CanSave || parent.CanInteract || parent.IsDirty || parent.SaveError != Translator["Plan_SavedNeedsReview"])
            throw new IOException("Known native split acceptance was reclassified or rearmed after notification failure.");
        var schedules = await Plans.GetSchedulesAsync(ct); var next = schedules.Single(s => s.PreviousScheduleId == original.Id);
        ContinuationId = next.Id;
        var afterEntries = await Store.GetEntriesAsync(cancellationToken: ct); var afterStates = await Plans.GetStatesAsync(cancellationToken: ct);
        if (schedules.Count != 2 || next.Amount != 2345 || next.Rule.Start != original.Rule.Start || next.ActiveFrom != parent.ApplyFrom
            || afterEntries.Count != beforeEntries.Count || afterStates.Count != beforeStates.Count)
            throw new IOException("The original native split changed history shape or the recurrence anchor.");
        foreach (var row in beforeEntries)
        {
            var actual = afterEntries.Single(e => e.Id == row.Id).Copy();
            if (actual.ScheduleId != (row.OccurrenceDate >= parent.ApplyFrom ? next.Id : original.Id)) throw new IOException("The original payment owner did not move at its exact boundary.");
            actual.ScheduleId = row.ScheduleId; actual.CreatedAt = row.CreatedAt; actual.UpdatedAt = row.UpdatedAt;
            if (JsonSerializer.Serialize(actual) != JsonSerializer.Serialize(row)) throw new IOException("Native plan editing rewrote recorded money or metadata.");
        }
        foreach (var row in beforeStates)
        {
            var actual = afterStates.Single(s => s.Id == row.Id); var owner = actual.ScheduleId;
            if (owner != (row.OriginalDate >= parent.ApplyFrom ? next.Id : original.Id)) throw new IOException("The original occurrence owner did not move at its exact boundary.");
            actual.ScheduleId = row.ScheduleId; actual.CreatedAt = row.CreatedAt; actual.UpdatedAt = row.UpdatedAt;
            if (JsonSerializer.Serialize(actual) != JsonSerializer.Serialize(row)) throw new IOException("Native plan editing rewrote an original occurrence.");
            actual.ScheduleId = owner;
        }
        await observe(page, "native-split-saved-readonly"); await invoke(page, parent.CancelCommand);
        await WaitAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page), ct); await LeaveAsync(route);
        Observations.Add(new { Stage = "native-actions-complete", NativeAccountSaves = 1, NativePlanSaves = 2,
            NativeLedgerPosts = 0, FictitiousHistorySeedEntries = 2, PastAndFuturePaymentMoneyUnchanged = true, KnownNotificationRefusals = failures });
        await WriteRowsAsync("actions-complete", ct);
    }
    /// <summary>Waits only for this actual original process/view; a timeout never authorizes a replay.</summary>
    internal static async Task WaitAsync(Func<bool> ready, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (!ready()) { if (DateTime.UtcNow >= deadline) throw new IOException("Preserve the original pending plan process and accepted checkpoints."); await Task.Delay(50, ct); }
    }
    /// <summary>Enumerates actual production controls for platform-native observation; no substitute controls are constructed.</summary>
    internal static IEnumerable<Element> Views(Element root)
    {
        yield return root;
        if (root is IVisualTreeElement tree)
            foreach (var child in tree.GetVisualChildren().OfType<Element>()) foreach (var view in Views(child)) yield return view;
    }
    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= Accepted; Plans.Changed -= Accepted;
        foreach (var route in _routes) Routing.UnRegisterRoute(route);
        Services.Dispose();
    }
    private sealed class PageFactory(Func<Page> create) : RouteFactory
    {
        /// <inheritdoc />
        public override Element GetOrCreate() => create();
        /// <inheritdoc />
        public override Element GetOrCreate(IServiceProvider services) => create();
    }
    private sealed class ReviewAccess : ICommercialWriteAccessSource
    {
        /// <inheritdoc />
        public CommercialWriteAccess Capture(string databasePath) => CommercialWriteAccess.Inactive;
    }
    /// <summary>Defers or refuses one original context creation for visible loading/Retry checks on the fixed fixture file.</summary>
    internal sealed class ReviewFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        /// <summary>Defers one original read until its loading surface was observed.</summary>
        internal TaskCompletionSource? Gate;
        /// <summary>Records that the original deferred read actually reached its gate.</summary>
        internal bool GateRequested;
        /// <summary>Refuses only the next original read for an explicit native Retry check.</summary>
        internal bool RefuseOnce;
        /// <inheritdoc />
        public ZananceDbContext CreateDbContext() => original.CreateDbContext();
        /// <inheritdoc />
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            var gate = Gate; Gate = null;
            if (gate is not null) { GateRequested = true; await gate.Task.WaitAsync(cancellationToken); }
            if (RefuseOnce) { RefuseOnce = false; throw new IOException("Deliberate original plan read refusal."); }
            return await original.CreateDbContextAsync(cancellationToken);
        }
    }
}
#endif
