#if DEBUG && WINDOWS
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // Source-local geometry review is separate from the already completed financial handoff/return scenarios.
    private static async Task ReviewEntryRepeatPresentationAsync(App app, IServiceProvider services, EntryEditorPage page,
        string folder, string language)
    {
        var vm = (EntryEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var translator = services.GetRequiredService<Translator>();
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
            Schedules = await plans.GetSchedulesAsync(), Filters = await store.GetSavedFiltersAsync() });
        string Draft() => (string)(typeof(EntryEditorViewModel).GetMethod("Snapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(vm, null) ?? throw new InvalidOperationException("The original editor fingerprint is unavailable."));
        var original = (vm.KindIndex, vm.ToAccount, vm.AmountText, vm.ToAmountText, vm.EntryTitle, vm.Date);
        var originalCategory = vm.Categories.FirstOrDefault(category => category.IsSelected)?.Id;
        var originalDraft = Draft();
        var originalDirty = vm.IsDirty;
        var stored = await StoredAsync();
        var scroll = page.FindByName<ScrollView>("ValidationViewport");
        var checks = new List<object>();
        try
        {
            for (var index = 0; index < 3; index++)
            {
                vm.KindIndex = index;
                var source = vm.Account ?? throw new InvalidOperationException("A fictitious source account is required.");
                vm.ToAccount = vm.ToAccounts.FirstOrDefault(account => account.Id != source.Id)
                    ?? throw new InvalidOperationException("A fictitious transfer destination is required.");
                vm.AmountText = MoneyText.ForInput(12_345, source.CurrencyCode, culture);
                vm.ToAmountText = MoneyText.ForInput(15_000, vm.ToAccount.CurrencyCode, culture);
                vm.EntryTitle = "Fictitious repeating draft";
                vm.Date = new DateOnly(2026, 10, 31);
                // Drain queued caret/arrangement work before using the actual native scroll geometry (D-100).
                await Task.Delay(250);
                await CaptureAsync(app, folder, language + "-entry-repeat-arranged-" + index);
                var action = WrappingActions(page).Single(candidate => candidate.Command == vm.RepeatCommand);
                if (action.Content is not Grid grid || grid.Children.LastOrDefault() is not Button button
                    || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                    || scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer viewport)
                { throw new InvalidOperationException("The actual Repeat button/viewport is unavailable."); }
                var scrollPeer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(viewport);
                if (scrollPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll)
                    is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider provider)
                { throw new InvalidOperationException("The actual Repeat viewport lacks native Scroll."); }
                var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
                Windows.Foundation.Rect Bounds() => native.TransformToVisual(viewport)
                    .TransformBounds(new Windows.Foundation.Rect(0, 0, native.ActualWidth, native.ActualHeight));
                bool Visible(Windows.Foundation.Rect box) => box.Left >= -1 && box.Right <= viewport.ActualWidth + 1
                    && box.Top >= -1 && box.Bottom <= viewport.ActualHeight + 1;
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var box = Bounds();
                    var offset = viewport.VerticalOffset + box.Top - (viewport.ActualHeight - box.Height) / 2;
                    if (viewport.ScrollableHeight > 0)
                    { provider.SetScrollPercent(-1, Math.Clamp(offset, 0, viewport.ScrollableHeight) / viewport.ScrollableHeight * 100); }
                    await Task.Delay(250);
                    await CaptureAsync(app, folder, language + "-entry-repeat-visible-" + index + "-" + attempt);
                    if (Visible(Bounds())) { break; }
                }
                var target = Bounds();
                if (!Visible(target) || target.Width < 44 || target.Height < 44 || !peer.IsEnabled()
                    || action.Text != translator["Entry_Repeat"] || peer.GetName() != translator["Entry_Repeat"]
                    || button.Command != vm.RepeatCommand || peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is null)
                {
                    File.WriteAllText(Path.Combine(folder, language + "-entry-repeat-visibility-failure.json"), JsonSerializer.Serialize(new
                    { target, viewport.ActualWidth, viewport.ActualHeight, viewport.VerticalOffset, viewport.ScrollableHeight }));
                    throw new InvalidOperationException("The complete native Repeat target is not visible inside its actual viewport.");
                }
                checks.Add(new { Kind = vm.Kind.ToString(), Target = target, ViewportWidth = viewport.ActualWidth,
                    ViewportHeight = viewport.ActualHeight, ActualNativeScroll = true, CompleteVisibleTarget = true });
            }
            await CaptureHelpAsync(app, folder, language, ["EntryRepeat"]);
        }
        finally
        {
            (vm.KindIndex, vm.ToAccount, vm.AmountText, vm.ToAmountText, vm.EntryTitle, vm.Date) = original;
            foreach (var category in vm.Categories) { category.IsSelected = category.Id == originalCategory; }
            if (Draft() != originalDraft || vm.IsDirty != originalDirty || stored != await StoredAsync())
            { throw new InvalidOperationException("The focused presentation review changed its original draft or stored rows."); }
        }
        File.WriteAllText(Path.Combine(folder, language + "-entry-repeat-presentation.json"), JsonSerializer.Serialize(new
        { Pass = true, Cases = checks, CompleteStoredRowsUnchanged = true, CompleteDraftRestored = true,
            NoSave = true, FinancialHandoffsNotRepeated = true }));
    }

    // AT-116 invokes real Repeat/Cancel controls on fictitious drafts; no ledger or plan Save is invoked.
    private static async Task ReviewEntryRepeatAsync(App app, IServiceProvider services, EntryEditorPage page, string folder, string language)
    {
        var vm = (EntryEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var translator = services.GetRequiredService<Translator>();
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
            Schedules = await plans.GetSchedulesAsync(), Filters = await store.GetSavedFiltersAsync() });
        string Draft() => (string)(typeof(EntryEditorViewModel).GetMethod("Snapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(vm, null) ?? throw new InvalidOperationException("The original editor fingerprint is unavailable."));
        Page Top() => app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() ?? Shell.Current.CurrentPage;
        var original = (vm.KindIndex, vm.Account, vm.ToAccount, vm.AmountText, vm.ToAmountText, vm.EntryTitle, vm.Date,
            vm.Note, vm.Payee, vm.TagsText, vm.FeeText);
        var originalCategory = vm.Categories.FirstOrDefault(category => category.IsSelected)?.Id;
        var originalDraft = Draft();
        var originalDirty = vm.IsDirty;
        var stored = await StoredAsync();
        var panel = page.FindByName<VerticalStackLayout>("RepeatDraftPanel");
        var scroll = FindScrollView(page) ?? throw new InvalidOperationException("The Repeat form has no native viewport.");
        var checks = new List<object>();
        try
        {
            vm.AmountText = "not money";
            await ShowActionAsync("invalid");
            InvokeWrappingAction(page, vm.RepeatCommand);
            if (vm.RepeatCommand.ExecutionTask is { } invalid) { await invalid.WaitAsync(TimeSpan.FromSeconds(5)); }
            if (!ReferenceEquals(Top(), page) || vm.AmountError is null || stored != await StoredAsync())
            { throw new InvalidOperationException("Invalid Repeat input navigated or wrote stored data."); }

            for (var index = 0; index < 3; index++)
            {
                vm.KindIndex = index;
                var source = vm.Account ?? throw new InvalidOperationException("A fictitious source account is required.");
                vm.ToAccount = vm.ToAccounts.FirstOrDefault(account => account.Id != source.Id)
                    ?? throw new InvalidOperationException("A fictitious transfer destination is required.");
                vm.AmountText = MoneyText.ForInput(12_345, source.CurrencyCode, culture);
                vm.ToAmountText = MoneyText.ForInput(15_000, vm.ToAccount.CurrencyCode, culture);
                vm.EntryTitle = "Fictitious repeating draft";
                vm.Date = new DateOnly(2026, 10, 31);
                vm.Note = "Original repeating draft note";
                vm.Payee = "Draft-only payee";
                vm.TagsText = "draft-only-tag";
                vm.FeeText = "0.25";
                var typed = Draft();
                var dirty = vm.IsDirty;
                await ShowActionAsync("kind-" + index);
                InvokeWrappingAction(page, vm.RepeatCommand);
                if (vm.RepeatCommand.ExecutionTask is { } repeating) { await repeating.WaitAsync(TimeSpan.FromSeconds(5)); }
                for (var attempt = 0; attempt < 100 && (Top() is not PlanEditorPage editor
                    || editor.BindingContext is not PlanEditorViewModel { Account: not null }); attempt++) { await Task.Delay(50); }
                var planPage = Top() as PlanEditorPage ?? throw new InvalidOperationException("Native Repeat did not open the actual plan editor.");
                var plan = (PlanEditorViewModel)planPage.BindingContext;
                if (plan.Account?.Id != source.Id || plan.Name != vm.EntryTitle || plan.Start != vm.Date || plan.KindIndex != index
                    || plan.PresetIndex != 3 || plan.AutoPost || plan.ReminderEnabled || plan.Note != vm.Note
                    || !MoneyText.TryParse(plan.AmountText, Currencies.Get(source.CurrencyCode), culture, out var amount) || amount != 12_345
                    || plan.Preview.Count == 0 || string.IsNullOrWhiteSpace(plan.RuleSummary) || plan.IsDirty)
                { throw new InvalidOperationException("The prefilled plan lost a supported field, selected date or safe draft defaults."); }
                if (index == 2 && (plan.ToAccount?.Id != vm.ToAccount.Id
                    || (vm.ShowToAmount && (!MoneyText.TryParse(plan.ToAmountText, Currencies.Get(vm.ToAccount.CurrencyCode), culture,
                        out var destinationAmount) || destinationAmount != 15_000))))
                { throw new InvalidOperationException("The repeating transfer lost its destination amount/account."); }
                await CaptureAsync(app, folder, language + "-entry-repeat-plan-" + index);
                var cancel = VisualDescendants(planPage).OfType<Button>().Single(button => button.Command == plan.CancelCommand);
                if (cancel.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeCancel)
                { throw new InvalidOperationException("The actual plan Cancel target is unavailable."); }
                var cancelPeer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(nativeCancel);
                if (cancelPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                    is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invokeCancel)
                { throw new InvalidOperationException("The actual plan Cancel target lacks Invoke."); }
                invokeCancel.Invoke();
                if (plan.CancelCommand.ExecutionTask is { } canceling) { await canceling.WaitAsync(TimeSpan.FromSeconds(5)); }
                for (var attempt = 0; attempt < 100 && !ReferenceEquals(Top(), page); attempt++) { await Task.Delay(50); }
                if (!ReferenceEquals(Top(), page) || Draft() != typed || vm.IsDirty != dirty || stored != await StoredAsync())
                { throw new InvalidOperationException("Returning from Repeat changed the original draft or complete stored rows."); }
                checks.Add(new { Kind = vm.Kind.ToString(), SelectedDate = vm.Date, Monthly = true, SafeDefaults = true,
                    OriginalDraftRetained = true, CompleteStoredRowsUnchanged = true, NativeRepeatCancel = true });
            }
        }
        finally
        {
            (vm.KindIndex, vm.Account, vm.ToAccount, vm.AmountText, vm.ToAmountText, vm.EntryTitle, vm.Date,
                vm.Note, vm.Payee, vm.TagsText, vm.FeeText) = original;
            foreach (var category in vm.Categories) { category.IsSelected = category.Id == originalCategory; }
            if (Draft() != originalDraft || vm.IsDirty != originalDirty || stored != await StoredAsync())
            { throw new InvalidOperationException("The Repeat review did not restore its complete original draft and stored rows."); }
        }
        await CaptureHelpAsync(app, folder, language, ["EntryRepeat"]);
        File.WriteAllText(Path.Combine(folder, language + "-entry-repeat.json"), JsonSerializer.Serialize(new
        { Pass = true, InvalidInputBlocked = true, Cases = checks, CompleteStoredRowsUnchanged = true, NoSave = true }));

        async Task ShowActionAsync(string label)
        {
            await scroll.ScrollToAsync(panel, ScrollToPosition.Center, animated: false);
            await Task.Delay(350);
            await CaptureAsync(app, folder, language + "-entry-repeat-action-" + label);
            var action = WrappingActions(page).Single(candidate => candidate.Command == vm.RepeatCommand);
            if (action.Content is not Grid grid || grid.Children.LastOrDefault() is not Button button
                || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || panel.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativePanel)
            { throw new InvalidOperationException("The real Repeat target is not realized."); }
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
            var box = native.TransformToVisual(nativePanel).TransformBounds(new Windows.Foundation.Rect(0, 0, native.ActualWidth, native.ActualHeight));
            if (!vm.ShowRepeat || peer.GetName() != translator["Entry_Repeat"] || action.Text != translator["Entry_Repeat"]
                || box.Width < 44 || box.Height < 44 || box.Left < -1 || box.Right > nativePanel.ActualWidth + 1)
            { throw new InvalidOperationException("The Repeat caption/name/target is incomplete or unavailable."); }
        }
    }
}
#endif
