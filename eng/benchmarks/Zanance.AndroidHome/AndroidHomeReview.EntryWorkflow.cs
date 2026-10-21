using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies actual nested account/receipt retention and individually reviewed aggregate Save/Undo in independent AOT.</summary>
    private static async Task<object> ReviewEntryWorkflowAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new ReviewCheckException("Owned native workflow observation requires API 29.");
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var original = Path.Combine(folder, "workflow-original");
        var resume = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_workflow_resume", false) == true;
        if (Directory.Exists(original) != resume) throw new ReviewCheckException("Retain the original workflow and accepted actions; an explicit original continuation is required.");
        Directory.CreateDirectory(original);
        if (!resume) await File.WriteAllTextAsync(Path.Combine(original, "started.json"), JsonSerializer.Serialize(new { ProcessId = Environment.ProcessId }), ct);
        using var fixture = new ServiceCollection().AddZananceData(Path.Combine(original, "workflow.db")).BuildServiceProvider();
        if (!resume) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var time = services.GetRequiredService<TimeProvider>();
        var translator = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var interaction = services.GetRequiredService<IAppInteraction>(); var undo = new UndoService(store, time);
        if (!resume)
        {
            await store.SaveSettingsAsync(new() { Mode = ExperienceMode.Advanced, OnboardingCompleted = true }, ct);
            await store.SaveCategoryAsync(new() { SystemKey = DefaultCategories.Uncategorized, Kind = CategoryKind.Expense }, ct);
        }
        else if (!File.Exists(Path.Combine(original, "parent-after-cancel.json"))
            || !File.Exists(Path.Combine(original, "aggregate-seed-accepted.json"))
            || File.Exists(Path.Combine(original, "aggregate-save-accepted.json"))
            || (await store.GetAccountsAsync(cancellationToken: ct)).Count != 1
            || (await store.GetEntriesAsync(cancellationToken: ct)) is not { Count: 2 } prior || prior.Any(e => !e.IsAggregated))
            throw new ReviewCheckException("Only the exact accepted account/seed and uncommitted proposal may continue.");
        var observations = new List<object>(); var phase = "account-create";
        void Accepted(object? sender, EventArgs args)
        {
            var path = Path.Combine(original, phase + "-accepted.json");
            if (!File.Exists(path)) File.WriteAllText(path, JsonSerializer.Serialize(new { Phase = phase, ProcessId = Environment.ProcessId,
                Accounts = Task.Run(() => store.GetAccountsAsync()).GetAwaiter().GetResult(), Entries = Task.Run(() => store.GetEntriesAsync()).GetAwaiter().GetResult() }));
        }
        store.Changed += Accepted;
        async Task Wait(Func<bool> ready)
        {
            var until = DateTime.UtcNow.AddSeconds(45);
            while (!ready()) { if (DateTime.UtcNow > until) throw new ReviewCheckException("Preserve original pending workflow at " + phase); await Task.Delay(50, ct); }
        }
        IEnumerable<Element> Views(Element root)
        {
            yield return root;
            if (root is IVisualTreeElement tree)
                foreach (var child in tree.GetVisualChildren().OfType<Element>()) foreach (var view in Views(child)) yield return view;
        }
        Button Action(Page page, System.Windows.Input.ICommand command) => Views(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command));
        async Task Invoke(Button button)
        {
            for (Element? ancestor = button.Parent; ancestor is not null; ancestor = ancestor.Parent)
                if (ancestor is ScrollView viewport) { await viewport.ScrollToAsync(button, ScrollToPosition.MakeVisible, false); break; }
            await Task.Delay(80, ct);
            await Wait(() => button.Handler?.PlatformView is Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true });
            var native = (Android.Widget.Button)button.Handler!.PlatformView!;
            if (native.Context?.PackageName != Package || native.Height / (double)(native.Resources?.DisplayMetrics?.Density ?? 1) < 43.5 || !native.PerformClick())
                throw new ReviewCheckException("Original workflow native action refused.");
            await Task.Yield();
        }
        async Task Observe(Page page, string stage)
        {
            var readings = Views(page).OfType<InputView>().Where(v => v.Handler?.PlatformView is Android.Widget.TextView { IsShown: true })
                .Select(v => (View: v, Native: (Android.Widget.TextView)v.Handler!.PlatformView!)).Select(v => new
                { v.View.Text, NativeText = v.Native.Text, v.Native.Width, v.Native.Height, v.Native.HasWindowFocus }).ToArray();
            if (readings.Any(r => (r.Text ?? string.Empty) != (r.NativeText ?? string.Empty))) throw new ReviewCheckException("Original workflow native input differs.");
            var observation = new { Stage = stage, NativeReadings = readings, ProcessId = Environment.ProcessId };
            observations.Add(observation); await File.WriteAllTextAsync(Path.Combine(original, stage + ".json"), JsonSerializer.Serialize(observation), ct);
            LogReport("entry-workflow-observation", comparison, observation);
        }
        var routes = new List<string>();
        async Task Show(Page page, Dictionary<string, object>? query = null)
        {
            var route = "original-android-workflow-" + Guid.NewGuid().ToString("N");
            Routing.RegisterRoute(route, new WorkflowPageFactory(page)); routes.Add(route);
            await Shell.Current.GoToAsync(route, query ?? new() { ["kind"] = EntryKind.Expense });
        }
        EntryEditorViewModel Editor() => new(store, translator, local, time, undo,
            new AssetEntryConfirmation(translator, interaction), interaction,
            new ReceiptWorkStore(fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), fixture.GetRequiredService<ICommercialWriteAccessSource>()));
        var transactions = new TransactionsViewModel(store, translator, local, services.GetRequiredService<IDateFormatter>(), time, undo, interaction);
        var list = new TransactionsPage(transactions); await Show(list); await Wait(() => !transactions.IsBusy);
        var parent = Editor(); var page = new EntryEditorPage(parent);
        Guid draftId;
        if (!resume)
        {
        var document = new PendingAttachment("D428 fictitious pending receipt.pdf", "application/pdf", [1, 2, 3, 4]);
        var receipt = ReceiptParser.Parse("D428 fictitious receipt\nTOTAL EUR 15.67") with { Merchant = "D428 fictitious receipt" };
        if (receipt.AmountStatus != ReceiptAmountStatus.Found || receipt.Candidates.Count != 1)
            throw new ReviewCheckException("Complete fictitious receipt evidence required.");
        await Show(page, new() { ["kind"] = EntryKind.Expense, ["receipt"] = receipt, ["receiptFile"] = document });
        await Wait(() => parent.IsFormEditable);
        parent.AmountText = MoneyText.ForInput(1456, "EUR", local.CurrentCulture); parent.Note = "Original Android raw note"; parent.TagsText = "OriginalAndroidRaw";
        draftId = parent.FormDraftId; await Observe(page, "parent-before-account");
        AccountEditorViewModel? child = null;
        Routing.UnRegisterRoute(AppShell.AccountEditorRoute);
        Routing.RegisterRoute(AppShell.AccountEditorRoute, new WorkflowAccountFactory(() =>
        {
            child = new(store, fixture.GetRequiredService<PlanStore>(), translator, local, time, interaction);
            return new AccountEditorPage(child);
        }));
        try
        {
            await Invoke(Action(page, parent.AddAccountCommand));
            await Wait(() => Shell.Current.CurrentPage is AccountEditorPage && child?.CanSave == true);
            child!.Form.Name = "D428 Android explicit first account"; child.IsDefault = true;
            await Invoke(Action(Shell.Current.CurrentPage, child.SaveCommand));
            await Wait(() => ReferenceEquals(Shell.Current.CurrentPage, page) && parent.IsFormEditable);
            var pending = (PendingAttachment?)typeof(EntryEditorViewModel).GetField("_pendingAttachment", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(parent);
            if (parent.FormDraftId != draftId || parent.AmountText != MoneyText.ForInput(1456, "EUR", local.CurrentCulture)
                || parent.Note != "Original Android raw note" || parent.TagsText != "OriginalAndroidRaw" || parent.Payee != "D428 fictitious receipt"
                || parent.Account?.Name != "D428 Android explicit first account" || pending?.FileName != document.FileName
                || !pending.Data.SequenceEqual(document.Data) || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                throw new ReviewCheckException("Original account return replaced raw receipt intent or financial identity.");
            await Observe(page, "parent-after-account");
            phase = "account-cancel"; await parent.AddAccountCommand.ExecuteAsync(null);
            await Wait(() => Shell.Current.CurrentPage is AccountEditorPage && child?.CanSave == true);
            await Invoke(Action(Shell.Current.CurrentPage, child!.CancelCommand));
            await Wait(() => ReferenceEquals(Shell.Current.CurrentPage, page) && parent.IsFormEditable);
            if (parent.FormDraftId != draftId || parent.AmountText != MoneyText.ForInput(1456, "EUR", local.CurrentCulture) || (await store.GetAccountsAsync(cancellationToken: ct)).Count != 1)
                throw new ReviewCheckException("Cancelled original account child changed the parent.");
            await Observe(page, "parent-after-cancel");
        }
        finally { Routing.UnRegisterRoute(AppShell.AccountEditorRoute); Routing.RegisterRoute(AppShell.AccountEditorRoute, typeof(AccountEditorPage)); }
        await Shell.Current.GoToAsync("..", false);
        }
        var account = (await store.GetAccountsAsync(cancellationToken: ct)).Single(); var category = (await store.GetCategoriesAsync(ct)).Single();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var aggregates = resume ? (await store.GetEntriesAsync(cancellationToken: ct)).OrderBy(e => e.Id).ToArray()
            : new[] { 12000L, 24000L }.Select(amount => new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, CategoryId = category.Id,
            Date = today, Amount = amount, IsAggregated = true, AggregatedFrom = new(2020, 1, 1), AggregatedTo = new(2035, 12, 31), Title = "D428 Android aggregate " + amount }).OrderBy(e => e.Id).ToArray();
        if (!resume) { phase = "aggregate-seed"; await store.SaveEntriesAsync(aggregates, [], ct); }
        var homeModel = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store, undo);
        var home = new HomePage(homeModel); await Show(home); await home.PendingPublication.WaitAsync(ct);
        parent = Editor(); page = new(parent); await Show(page, new() { ["account"] = account.Id }); await Wait(() => parent.IsFormEditable);
        // The original2500 proposal retired without a financial commit after its failed observer disposed the fixture.
        // An explicit revised2600 draft continues the same accounts/aggregates, never replaying accepted metadata.
        var proposalAmount = resume ? 2600L : 2500L;
        parent.AmountText = MoneyText.ForInput(proposalAmount, "EUR", local.CurrentCulture); draftId = parent.FormDraftId; phase = "aggregate-save";
        await File.WriteAllTextAsync(Path.Combine(original, resume ? "revised-proposal.json" : "original-proposal.json"), JsonSerializer.Serialize(new { DraftId = draftId, Amount = proposalAmount }), ct);
        var offerFailures = 0;
        void RefuseOfferPublication(object? sender, EventArgs args)
        {
            if (phase == "aggregate-save" && undo.CanUndo && offerFailures++ == 0)
            {
                if (!parent.FormAccepted) throw new ReviewCheckException("The financial boundary was not consumed before Undo publication.");
                File.WriteAllText(Path.Combine(original, "undo-offer-publication-refusal.json"), JsonSerializer.Serialize(new
                { DraftId = draftId, KnownFinancialAcceptance = true, ProcessId = Environment.ProcessId }));
                throw new IOException("Deliberate original Undo offer publication refusal.");
            }
        }
        undo.Changed += RefuseOfferPublication;
        await Invoke(Action(page, parent.SaveCommand));
        IEnumerable<Android.Views.View> NativeNodes(Android.Views.View root)
        {
            yield return root;
            if (root is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++) if (group.GetChildAt(i) is { } child) foreach (var node in NativeNodes(child)) yield return node;
        }
        var choiceIndex = 0;
        async Task Choose(string caption)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new ReviewCheckException("Owned native dialog inspection requires API 29.");
            Android.Widget.TextView? answer = null;
            await Wait(() =>
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return false;
                answer = Android.Views.Inspectors.WindowInspector.GlobalWindowViews.SelectMany(NativeNodes).OfType<Android.Widget.TextView>()
                    .FirstOrDefault(b => b.IsShown && b.HasWindowFocus && b.Enabled && b.Context?.PackageName == Package && b.Text == caption);
                return answer is not null;
            });
            await File.WriteAllTextAsync(Path.Combine(original, "choice-" + choiceIndex++ + ".json"), JsonSerializer.Serialize(new
            { answer!.Text, answer.Width, answer.Height, DraftId = draftId, BeforeNativeInvoke = true, ProcessId = Environment.ProcessId }), ct);
            var invoked = false;
            if (answer is Android.Widget.Button button) invoked = button.PerformClick();
            else
            {
                Android.Views.View item = answer;
                while (item.Parent is Android.Views.View parent && parent is not Android.Widget.AdapterView) item = parent;
                if (item.Parent is Android.Widget.AdapterView adapter)
                {
                    var position = adapter.GetPositionForView(item);
                    if (position >= 0) invoked = adapter.PerformItemClick(item, position, adapter.GetItemIdAtPosition(position));
                }
            }
            if (!invoked) throw new ReviewCheckException("Owned original aggregate choice refused."); await Task.Yield();
        }
        await Choose(translator["Common_Next"]); await Choose(translator["Aggregate_Replace"]);
        // The first Link consumes this detail; the second overlap now correctly refuses another Link.
        await Choose(translator["Import_KeepBothShort"]);
        await Wait(() => parent.FormAccepted && !parent.IsBusy && ReferenceEquals(Shell.Current.CurrentPage, page));
        if (parent.IsFormEditable || parent.IsDirty || parent.SaveError is null || offerFailures != 1)
            throw new ReviewCheckException("Known financial acceptance was reclassified after Undo publication refusal.");
        await File.WriteAllTextAsync(Path.Combine(original, "saved-readonly-before-close.json"), JsonSerializer.Serialize(new
        { DraftId = draftId, parent.SaveError, parent.IsFormEditable, parent.IsDirty, KnownFinancialAcceptance = parent.FormAccepted }), ct);
        undo.Changed -= RefuseOfferPublication;
        await Invoke(Action(page, parent.CancelCommand));
        await Wait(() => ReferenceEquals(Shell.Current.CurrentPage, home) && homeModel.ShowUndo);
        var undoCaption = home.FindByName<Label>("UndoCaption");
        await Wait(() => undoCaption.Handler?.PlatformView is Android.Widget.TextView { IsShown: true, HasWindowFocus: true } text && text.Text == translator["Aggregate_UndoNotice"]);
        phase = "aggregate-undo"; await Invoke(Action(home, homeModel.UndoEntryCommand));
        if (homeModel.UndoEntryCommand.ExecutionTask is { } pendingUndo) await pendingUndo.WaitAsync(ct);
        var saved = await store.GetEntriesAsync(cancellationToken: ct);
        if (saved.Count != 3 || saved.Single(e => e.Id == draftId).Amount != proposalAmount
            || aggregates.Any(a => saved.Single(e => e.Id == a.Id).Amount != a.Amount) || undo.CanUndo)
            throw new ReviewCheckException("Native original adjustment Undo changed detail or unrelated aggregate money.");
        await Observe(home, "aggregate-after-undo");
        phase = "warning-document";
        var guarded = saved.Where(e => e.IsAggregated).OrderBy(e => e.Id).First();
        await store.AddAttachmentAsync(new() { EntryId = guarded.Id, FileName = "D428 Android fictitious aggregate document.pdf",
            ContentType = "application/pdf", Data = [1, 2] }, ct);
        var rowBaseline = await Diagnostics.ImportFinancialNativeReview.StoredAsync(fixture, ct);
        var theme = services.GetRequiredService<ThemeService>(); var originalLanguage = local.CurrentLanguage; var originalTheme = theme.Choice;
        async Task SetTheme(ThemeChoice value)
        {
            if (theme.Choice == value) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Changed(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Changed;
            try { theme.Set(value); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Changed; }
        }
        try
        {
            await Shell.Current.GoToAsync("//home", false);
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                phase = "caption-" + language + "-" + value;
                await SetTheme(value); await SetLanguageAsync(app, local, language, ct);
                parent = Editor(); page = new(parent); await Show(page, new() { ["account"] = account.Id });
                await Wait(() => parent.IsFormEditable); parent.AmountText = MoneyText.ForInput(40000, "EUR", local.CurrentCulture);
                draftId = parent.FormDraftId; var previewCommand = parent.SaveCommand;
                await Invoke(Action(page, previewCommand));
                Android.Widget.TextView? caption = null;
                await Wait(() =>
                {
                    if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return false;
                    caption = Android.Views.Inspectors.WindowInspector.GlobalWindowViews.SelectMany(NativeNodes).OfType<Android.Widget.TextView>()
                        .FirstOrDefault(v => v.IsShown && v.HasWindowFocus && v.Context?.PackageName == Package
                            && v.Text?.Contains(translator["Aggregate_LinkUnavailable"], StringComparison.Ordinal) == true && v.Layout is not null);
                    return caption is not null;
                });
                var layout = caption!.Layout!;
                var positive = Android.Views.Inspectors.WindowInspector.GlobalWindowViews.SelectMany(NativeNodes).OfType<Android.Widget.Button>()
                    .Single(b => b.IsShown && b.HasWindowFocus && b.Context?.PackageName == Package && b.Text == translator["Import_KeepBothShort"]);
                var actionLayout = positive.Layout;
                var ellipsized = Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) > 0);
                if (ellipsized || caption.Height < layout.Height + caption.TotalPaddingTop + caption.TotalPaddingBottom
                    || actionLayout is null || Enumerable.Range(0, actionLayout.LineCount).Any(line => actionLayout.GetEllipsisCount(line) > 0))
                    throw new ReviewCheckException("The realized original aggregate refusal caption is clipped.");
                var observation = new { Stage = phase, caption.Text, caption.Width, caption.Height, Lines = layout.LineCount,
                    Ellipsized = ellipsized, ProcessId = Environment.ProcessId };
                await File.WriteAllTextAsync(Path.Combine(original, phase + "-before-cancel.json"), JsonSerializer.Serialize(observation), ct);
                await Choose(translator["Common_Cancel"]);
                if (previewCommand.ExecutionTask is { } pendingPreview) await pendingPreview.WaitAsync(ct);
                if (parent.FormAccepted || rowBaseline != await Diagnostics.ImportFinancialNativeReview.StoredAsync(fixture, ct))
                    throw new ReviewCheckException("A cancelled original overlap changed financial or document ownership.");
                observations.Add(observation); LogReport("entry-workflow-observation", comparison, observation);
                await File.WriteAllTextAsync(Path.Combine(original, phase + "-complete.json"), JsonSerializer.Serialize(observation), ct);
                await Shell.Current.GoToAsync("..", false);
                foreach (var host in new[] { "home", "transactions" })
                {
                    var displayOffer = new UndoService(store, time); Page noticePage;
                    if (host == "home") noticePage = new HomePage(ActivatorUtilities.CreateInstance<HomeViewModel>(services, store, displayOffer));
                    else noticePage = new TransactionsPage(new(store, translator, local, services.GetRequiredService<IDateFormatter>(), time, displayOffer, interaction));
                    await Show(noticePage);
                    if (noticePage is HomePage homePage) await homePage.PendingPublication.WaitAsync(ct);
                    await Wait(() => noticePage.BindingContext is HomeViewModel { Loading.IsReady: true }
                        or TransactionsViewModel { Loading.IsReady: true });
                    // A readonly status fixture; the actual financial Home Undo was already invoked once above.
                    displayOffer.Offer(() => Task.CompletedTask, time.GetUtcNow(), UndoNoticeKind.AggregateAdjusted);
                    var label = noticePage.FindByName<Label>("UndoCaption"); Android.Widget.TextView? notice = null;
                    await Wait(() => (notice = label.Handler?.PlatformView as Android.Widget.TextView) is { IsShown: true, HasWindowFocus: true }
                        && notice.Text == translator["Aggregate_UndoNotice"] && notice.Layout is not null);
                    var noticeLayout = notice!.Layout!;
                    if (Enumerable.Range(0, noticeLayout.LineCount).Any(line => noticeLayout.GetEllipsisCount(line) > 0)
                        || notice.Height < noticeLayout.Height + notice.TotalPaddingTop + notice.TotalPaddingBottom)
                        throw new ReviewCheckException("Original transient Undo meaning is clipped.");
                    var noticeObservation = new { Stage = phase + "-" + host + "-undo", notice.Text, notice.Width, notice.Height,
                        PresentationOnly = true, NativeUndoInvocations = 0, ProcessId = Environment.ProcessId };
                    observations.Add(noticeObservation); LogReport("entry-workflow-observation", comparison, noticeObservation);
                    displayOffer.Dismiss();
                    if (rowBaseline != await Diagnostics.ImportFinancialNativeReview.StoredAsync(fixture, ct))
                        throw new ReviewCheckException("An Undo caption display fixture changed stored rows.");
                    await File.WriteAllTextAsync(Path.Combine(original, phase + "-" + host + "-undo-complete.json"), JsonSerializer.Serialize(noticeObservation), ct);
                    await Shell.Current.GoToAsync("..", false);
                }
            }
        }
        finally { await SetTheme(originalTheme); await SetLanguageAsync(app, local, originalLanguage.CultureName, ct); }
        phase = "blocked-keep-both-save";
        homeModel = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store, undo);
        home = new(homeModel); await Show(home); await home.PendingPublication.WaitAsync(ct);
        parent = Editor(); page = new(parent); await Show(page, new() { ["account"] = account.Id });
        await Wait(() => parent.IsFormEditable); parent.AmountText = MoneyText.ForInput(45000, "EUR", local.CurrentCulture);
        draftId = parent.FormDraftId; await Invoke(Action(page, parent.SaveCommand)); await Choose(translator["Import_KeepBothShort"]);
        // Keeping the document-bearing first aggregate does not decide the second independent overlap.
        await Choose(translator["Common_Next"]); await Choose(translator["Aggregate_KeepBoth"]);
        await Wait(() => parent.FormAccepted && !parent.IsBusy && ReferenceEquals(Shell.Current.CurrentPage, home));
        var finalRows = await store.GetEntriesAsync(cancellationToken: ct);
        if (finalRows.Count != 4 || finalRows.Single(e => e.Id == draftId).Amount != 45000 || undo.CanUndo
            || aggregates.Any(a => finalRows.Single(e => e.Id == a.Id).Amount != a.Amount)
            || (await store.GetAttachmentsAsync(guarded.Id, ct)).Count != 1 || (await store.GetAttachmentsAsync(draftId, ct)).Count != 0)
            throw new ReviewCheckException("Explicit native Keep both changed an original aggregate or document owner.");
        var keepBoth = new { Stage = phase, DraftId = draftId, DetailAmount = 45000, OriginalAggregatesAndDocumentRetained = true, ProcessId = Environment.ProcessId };
        observations.Add(keepBoth); LogReport("entry-workflow-observation", comparison, keepBoth);
        await File.WriteAllTextAsync(Path.Combine(original, "blocked-keep-both-complete.json"), JsonSerializer.Serialize(keepBoth), ct);
        // Finish the actual returned Home reader before disposing its fixture; the ordinary review shell owns its own store.
        await home.PendingPublication.WaitAsync(ct);
        await Shell.Current.GoToAsync("//home", false);
        if (Shell.Current.CurrentPage is HomePage originalHome) await originalHome.PendingPublication.WaitAsync(ct);
        store.Changed -= Accepted;
        foreach (var route in routes) Routing.UnRegisterRoute(route);
        return new { Comparison = comparison, Observations = observations, ProcessId = Environment.ProcessId, NativeAccountSaves = resume ? 0 : 1, NativeFinancialSaves = 2, NativeUndo = 1,
            OriginalAccountAndSeedRetained = resume,
            RawAmountAndPendingReceiptRetained = true, NativeCancelHadNoWrite = true, NativeRefusalCaptionContexts = 6, NativeUndoCaptionContexts = 12,
            KnownUndoOfferPublicationRefusals = offerFailures, AcceptedFormClosedWithoutDiscard = true,
            OnlyIndependentFictitiousRowsChanged = true, NoAcceptedCommandReplayed = true, Device = DeviceEvidence() };
    }

    /// <summary>Retains the actual reviewed page route until its original nested navigation has finished.</summary>
    private sealed class WorkflowPageFactory(Page page) : RouteFactory
    {
        public override Element GetOrCreate() => page;
        public override Element GetOrCreate(IServiceProvider services) => page;
    }
    /// <summary>Creates each actual child with the original independent fixture dependencies.</summary>
    private sealed class WorkflowAccountFactory(Func<AccountEditorPage> create) : RouteFactory
    {
        public override Element GetOrCreate() => create();
        public override Element GetOrCreate(IServiceProvider services) => create();
    }
}
