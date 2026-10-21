#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Exercises actual nested pages, individual native aggregate choices and the existing transaction Undo surface.</summary>
    private static async Task ReviewEntryWorkflowWindowsAsync(App app, IServiceProvider services, string folder)
    {
        await app.WaitForStartupAsync().WaitAsync(TimeSpan.FromSeconds(45));
        app.ShowMainShell();
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_WORKFLOW_SOURCE_RUN");
        if (!string.IsNullOrEmpty(sourceRun) && !System.Text.RegularExpressions.Regex.IsMatch(sourceRun, "^[0-9a-f]{32}$"))
            throw new IOException("An exact original workflow source is required.");
        var continuation = !string.IsNullOrEmpty(sourceRun);
        var aggregateOnly = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_WORKFLOW_AGGREGATE_ONLY") == "1";
        var captionsOnly = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_WORKFLOW_CAPTIONS_ONLY") == "1";
        if (aggregateOnly && !continuation) throw new IOException("Retained original aggregates are required.");
        var original = Path.Combine(continuation ? Path.Combine(Path.GetDirectoryName(folder)!, sourceRun!) : folder, "original-workflow");
        var accountAccepted = File.Exists(Path.Combine(original, "account-create-accepted.json"));
        var additionalAccepted = File.Exists(Path.Combine(original, "account-create-additional-accepted.json"));
        var seedAccepted = File.Exists(Path.Combine(original, "aggregate-seed-accepted.json"));
        var financeAccepted = File.Exists(Path.Combine(original, "aggregate-save-accepted.json"))
            || File.Exists(Path.Combine(original, "aggregate-save-revised-accepted.json"));
        if (!continuation && Directory.Exists(original) || continuation && !accountAccepted
            || continuation && !aggregateOnly && !captionsOnly && additionalAccepted || aggregateOnly && (!seedAccepted || financeAccepted))
            throw new IOException("Retain this original workflow and accepted-action checkpoints; no new seed or command replay.");
        Directory.CreateDirectory(original);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(folder, "workflow-started.json"), JsonSerializer.Serialize(new { ProcessId = Environment.ProcessId, SourceRun = sourceRun, continuation }));
        using var fixture = new ServiceCollection().AddZananceData(Path.Combine(original, "workflow.db")).BuildServiceProvider();
        if (!continuation) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>());
        var store = fixture.GetRequiredService<ZananceStore>(); var now = services.GetRequiredService<TimeProvider>();
        if (captionsOnly)
        {
            await ReviewEntryWorkflowCaptionsWindowsAsync(app, services, fixture, store, original, folder, baseline); return;
        }
        var translator = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var native = services.GetRequiredService<IAppInteraction>(); var undo = new UndoService(store, now);
        if (!continuation)
        {
            await store.SaveSettingsAsync(new() { Mode = ExperienceMode.Advanced, OnboardingCompleted = true });
            await store.SaveCategoryAsync(new() { SystemKey = DefaultCategories.Uncategorized, Kind = CategoryKind.Expense });
        }
        var priorAccounts = await store.GetAccountsAsync();
        if (continuation && (priorAccounts.Count != (aggregateOnly ? 2 : 1)
            || !priorAccounts.Any(a => a.Name == "D428 explicit nested account") || (await store.GetEntriesAsync()).Count != (aggregateOnly ? 2 : 0)))
            throw new IOException("The original accepted account or uncommitted financial boundary differs.");
        var captures = new List<object>(); var cases = new List<object>(); var phase = "prepared";
        void Accepted(object? sender, EventArgs args)
        {
            // This receipt precedes any fallible native observation; accepted metadata/finance is never replayed.
            var path = Path.Combine(original, phase + "-accepted.json");
            if (!File.Exists(path)) File.WriteAllText(path, JsonSerializer.Serialize(new { Phase = phase, ProcessId = Environment.ProcessId,
                Accounts = Task.Run(() => store.GetAccountsAsync()).GetAwaiter().GetResult(),
                Entries = Task.Run(() => store.GetEntriesAsync()).GetAwaiter().GetResult() }));
        }
        store.Changed += Accepted;
        async Task Wait(Func<bool> ready)
        {
            var until = DateTime.UtcNow.AddSeconds(45);
            while (!ready()) { if (DateTime.UtcNow > until) throw new IOException("Retain original live workflow at " + phase); await Task.Delay(50); }
        }
        async Task Invoke(Button button)
        {
            await Wait(() => button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 });
            var control = (Microsoft.UI.Xaml.Controls.Button)button.Handler!.PlatformView!;
            if (FrameworkElementAutomationPeer.CreatePeerForElement(control)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original workflow native Invoke unavailable.");
            action.Invoke(); await Task.Yield();
        }
        Button Action(Page page, System.Windows.Input.ICommand command) => VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command));
        async Task Capture(string stage)
        {
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "workflow.png"));
            captures.Add(new { Language = stage, File = "workflow.png" });
        }
        var registeredRoutes = new List<string>();
        async Task Show(Page page, Dictionary<string, object>? query = null)
        {
            var route = "original-workflow-" + Guid.NewGuid().ToString("N"); Routing.RegisterRoute(route, new WorkflowPageFactory(page));
            registeredRoutes.Add(route);
            // Shell resolves the parent's complete URI again when a real nested child returns.
            // Retain its registration until that original navigation stack has finished.
            await Shell.Current.GoToAsync(route, query ?? new() { ["kind"] = EntryKind.Expense });
        }
        EntryEditorViewModel Editor() => new(store, translator, local, now, undo,
            new AssetEntryConfirmation(translator, native), native,
            new ReceiptWorkStore(fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), fixture.GetRequiredService<ICommercialWriteAccessSource>()));
        var transactions = new TransactionsViewModel(store, translator, local, services.GetRequiredService<IDateFormatter>(), now, undo, native);
        var list = new TransactionsPage(transactions); await Show(list); await Wait(() => !transactions.IsBusy);
        var parent = Editor(); var page = new EntryEditorPage(parent); Guid draft;
        if (!aggregateOnly)
        {
        await Show(page);
        await Wait(() => parent.IsFormEditable);
        if (continuation) parent.Account = null;
        parent.AmountText = MoneyText.ForInput(1234, "EUR", local.CurrentCulture); parent.Note = "Retained original raw note"; parent.TagsText = "OriginalRaw";
        draft = parent.FormDraftId;
        await Capture("parent-before-account");
        AccountEditorViewModel? child = null;
        Routing.UnRegisterRoute(AppShell.AccountEditorRoute);
        Routing.RegisterRoute(AppShell.AccountEditorRoute, new WorkflowAccountFactory(() =>
        {
            child = new(store, fixture.GetRequiredService<PlanStore>(), translator, local, now, native);
            return new AccountEditorPage(child);
        }));
        try
        {
            phase = continuation ? "account-create-additional" : "account-create";
            if (continuation) await parent.AddAccountCommand.ExecuteAsync(null); else await Invoke(Action(page, parent.AddAccountCommand));
            await Wait(() => Shell.Current.CurrentPage is AccountEditorPage && child?.CanSave == true);
            var accountPage = (AccountEditorPage)Shell.Current.CurrentPage;
            var createdName = continuation ? "D428 explicit additional nested account" : "D428 explicit nested account";
            child!.Form.Name = createdName; child.IsDefault = true;
            await Invoke(Action(accountPage, child.SaveCommand));
            await Wait(() => ReferenceEquals(Shell.Current.CurrentPage, page) && parent.IsFormEditable);
            if (parent.FormDraftId != draft || parent.AmountText != MoneyText.ForInput(1234, "EUR", local.CurrentCulture)
                || parent.Note != "Retained original raw note" || parent.TagsText != "OriginalRaw"
                || parent.Account?.Name != createdName || (await store.GetAccountsAsync()).Count != priorAccounts.Count + 1
                || (await store.GetEntriesAsync()).Count != 0)
                throw new IOException("Nested account return replaced raw input, financial rows or original account identity.");
            await Capture("parent-after-account"); cases.Add(new { Stage = phase, RawDraftRetained = true, FinancialRows = 0 });
            phase = "account-cancel";
            // The same original Add command may now be hidden by the resolved first-account state.
            // An explicit original child visit still uses the production command and actual route.
            await parent.AddAccountCommand.ExecuteAsync(null);
            await Wait(() => Shell.Current.CurrentPage is AccountEditorPage && child?.CanSave == true);
            await Invoke(Action(Shell.Current.CurrentPage, child!.CancelCommand));
            await Wait(() => ReferenceEquals(Shell.Current.CurrentPage, page) && parent.IsFormEditable);
            if (parent.FormDraftId != draft || parent.Note != "Retained original raw note" || (await store.GetAccountsAsync()).Count != priorAccounts.Count + 1)
                throw new IOException("Cancelled nested account changed its original parent.");
            cases.Add(new { Stage = phase, NoMetadataWrite = !File.Exists(Path.Combine(original, phase + "-accepted.json")) });
        }
        finally { Routing.UnRegisterRoute(AppShell.AccountEditorRoute); Routing.RegisterRoute(AppShell.AccountEditorRoute, typeof(AccountEditorPage)); }
        await Shell.Current.GoToAsync("..", false);
        }
        var account = (await store.GetAccountsAsync()).Single(a => aggregateOnly ? a.Name == "D428 explicit additional nested account" : !priorAccounts.Any(old => old.Id == a.Id));
        var category = (await store.GetCategoriesAsync()).Single();
        var today = DateOnly.FromDateTime(now.GetLocalNow().DateTime);
        var aggregates = aggregateOnly ? (await store.GetEntriesAsync()).OrderBy(e => e.Id).ToArray() : new[] { 10000L, 20000L }.Select(amount => new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id,
            CategoryId = category.Id, Amount = amount, Date = today, IsAggregated = true, AggregatedFrom = new(2020, 1, 1),
            AggregatedTo = new(2035, 12, 31), Title = "D428 aggregate " + amount }).OrderBy(e => e.Id).ToArray();
        if (aggregateOnly)
        {
            using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(original, "aggregate-seed-accepted.json")));
            if (JsonSerializer.Serialize(aggregates.OrderBy(e => e.Id)) != JsonSerializer.Serialize(seed.RootElement.GetProperty("Entries").Deserialize<LedgerEntry[]>()!.OrderBy(e => e.Id)))
                throw new IOException("The retained original aggregate seed changed.");
        }
        else { phase = "aggregate-seed"; await store.SaveEntriesAsync(aggregates, []); }
        parent = Editor(); page = new EntryEditorPage(parent); await Show(page, new() { ["account"] = account.Id });
        var reviewedAmount = aggregateOnly ? 3000 : 2000;
        await Wait(() => parent.IsFormEditable); parent.AmountText = MoneyText.ForInput(reviewedAmount, "EUR", local.CurrentCulture);
        draft = parent.FormDraftId; phase = aggregateOnly ? "aggregate-save-revised" : "aggregate-save";
        await File.WriteAllTextAsync(Path.Combine(folder, "original-proposal.json"), JsonSerializer.Serialize(new { DraftId = draft, Amount = reviewedAmount,
            PriorUncommittedProposalRetiredWithItsProcess = aggregateOnly, OriginalSeedReplayed = false, OriginalAccountSavesReplayed = false }));
        await Invoke(Action(page, parent.SaveCommand));
        var choiceIndex = 0;
        async Task Choose(string caption)
        {
            Microsoft.UI.Xaml.FrameworkElement? target = null; Action? answer = null;
            IEnumerable<Microsoft.UI.Xaml.DependencyObject> Nodes(Microsoft.UI.Xaml.DependencyObject root)
            {
                yield return root;
                for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); i++)
                    foreach (var child in Nodes(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i))) yield return child;
            }
            await Wait(() =>
            {
                if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) return false;
                var nodes = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).SelectMany(p => Nodes(p.Child))
                    .OfType<Microsoft.UI.Xaml.FrameworkElement>().ToArray();
                foreach (var element in nodes.Where(e => e.IsLoaded && e.ActualWidth > 0 && e.ActualHeight > 0))
                {
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
                    if (peer?.GetName() != caption) continue;
                    if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke) answer = invoke.Invoke;
                    else if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selection) answer = selection.Select;
                    if (answer is not null) { target = element; break; }
                }
                if (nodes.Length > 0 && !File.Exists(Path.Combine(folder, "aggregate-popup-" + choiceIndex + ".json")))
                    File.WriteAllText(Path.Combine(folder, "aggregate-popup-" + choiceIndex + ".json"), JsonSerializer.Serialize(nodes.Select(e => new
                    { Type = e.GetType().Name, Name = FrameworkElementAutomationPeer.CreatePeerForElement(e)?.GetName(), e.ActualWidth, e.ActualHeight })));
                return target is not null && answer is not null;
            });
            if (answer is null)
                throw new IOException("Original aggregate decision Invoke unavailable.");
            var answerPath = Path.Combine(folder, "aggregate-choice-" + choiceIndex++ + ".json");
            await File.WriteAllTextAsync(answerPath, JsonSerializer.Serialize(new { Caption = caption, DraftId = draft, Amount = reviewedAmount,
                target!.ActualWidth, target.ActualHeight, BeforeNativeInvoke = true, ProcessId = Environment.ProcessId }));
            answer(); await Task.Yield();
        }
        await Choose(translator["Aggregate_Replace"]); await Choose(translator["Aggregate_KeepBoth"]);
        await Wait(() => parent.FormAccepted && ReferenceEquals(Shell.Current.CurrentPage, list) && transactions.ShowUndo);
        phase = "aggregate-undo"; await Invoke(Action(list, transactions.UndoDeleteCommand));
        if (transactions.UndoDeleteCommand.ExecutionTask is { } pending) await pending.WaitAsync(TimeSpan.FromSeconds(20));
        var rows = await store.GetEntriesAsync();
        if (rows.Count != 3 || rows.Single(e => e.Id == draft).Amount != reviewedAmount
            || aggregates.Any(a => rows.Single(e => e.Id == a.Id).Amount != a.Amount) || undo.CanUndo)
            throw new IOException("Native original adjustment Undo changed detail or unrelated aggregate money.");
        await Capture("aggregate-after-undo"); cases.Add(new { Stage = phase, OriginalAggregatesRestored = true, DetailRetained = true, Rows = rows.Count });
        store.Changed -= Accepted;
        var liveAccounts = await store.GetAccountsAsync();
        if (priorAccounts.Any(old => JsonSerializer.Serialize(old) != JsonSerializer.Serialize(liveAccounts.Single(a => a.Id == old.Id))))
            throw new IOException("The continuation changed the originally accepted account.");
        if (baseline != await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None))
            throw new IOException("The separate workflow changed its base installation.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Captures = captures, Cases = cases,
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true,
            NativeAccountSaves = aggregateOnly ? 0 : 1, NativeFinancialSaves = 1, NativeUndo = 1, NoAcceptedCommandReplayed = true,
            SourceRun = sourceRun, OriginalAcceptedAccountRetained = continuation, OriginalFirstAccountReturnAccepted = !continuation,
            AdditionalAccountRouteWasProgrammatic = continuation }));
        foreach (var route in registeredRoutes) Routing.UnRegisterRoute(route);
    }
    /// <summary>Returns only the exact original reviewed page through Shell's actual native lifecycle.</summary>
    private sealed class WorkflowPageFactory(Page page) : RouteFactory
    {
        public override Element GetOrCreate() => page;
        public override Element GetOrCreate(IServiceProvider services) => page;
    }
    /// <summary>Constructs each real child with this independent fixture's original persistence dependencies.</summary>
    private sealed class WorkflowAccountFactory(Func<AccountEditorPage> create) : RouteFactory
    {
        public override Element GetOrCreate() => create();
        public override Element GetOrCreate(IServiceProvider services) => create();
    }
}
#endif
