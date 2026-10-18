using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks original unsaved account, transaction and plan forms at actual authorized Android text scale.</summary>
    private static async Task<object> ReviewEditorTextAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var configuration = Platform.CurrentActivity?.Resources?.Configuration;
        if (configuration is null || Math.Abs(configuration.FontScale - 2f) > .001f || configuration.ScreenWidthDp != 412)
            throw new ReviewCheckException("Editor review requires actual OS scale2.0 at412dp.");
        var selectedForms = (Platform.CurrentActivity?.Intent?.GetStringExtra("review_editor_forms") ?? "account,debt,entry,transfer,plan,monthly").Split(',');
        if (selectedForms.Length == 0 || selectedForms.Distinct().Count() != selectedForms.Length
            || selectedForms.Any(value => value is not ("account" or "debt" or "entry" or "transfer" or "plan" or "monthly"
                or "goal" or "earmark" or "goal-share" or "goal-cut" or "budget" or "settlement")))
            throw new ReviewCheckException("Explicit known editor states required.");
        var workForms = selectedForms.Any(value => value is "goal" or "earmark" or "goal-share" or "goal-cut" or "budget" or "settlement");
        var fixtureName = workForms ? "work-editor-text" : "editor-text";
        var path = Path.Combine(root, fixtureName + ".db"); var fresh = !File.Exists(path);
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
        if (fresh)
        {
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var account = new Account { Name = "Gemeinschaftskonto für laufende Haushaltsausgaben", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            await store.SaveAccountAsync(account, ct);
            await store.SaveAccountAsync(new Account { Name = "Fictitious savings account", CurrencyCode = "EUR", OpeningDate = account.OpeningDate }, ct);
            await store.UpdateSettingsAsync(value => { value.Mode = ExperienceMode.Advanced; value.DefaultAccountId = account.Id; }, ct);
            if (workForms)
                await plans.SaveScheduleAsync(new Schedule { Name = "Fictitious household advance settlement", AccountId = account.Id,
                    Amount = 12345, Rule = new RecurrenceRule { Start = account.OpeningDate } }, ct);
            File.WriteAllText(Path.Combine(root, fixtureName + "-prepared.marker"), "Fictitious unsaved editor geometry only.");
        }
        if (!File.Exists(Path.Combine(root, fixtureName + "-prepared.marker"))) throw new ReviewCheckException("Incomplete editor fixture retained; never reseed.");
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Plans = await plans.GetSchedulesAsync(ct), Settings = await store.GetSettingsAsync(ct),
            Goals = await fixture.GetRequiredService<GoalStore>().GetGoalsAsync(ct), Budgets = await store.GetBudgetsAsync(ct) });
        var selectedContexts = (Platform.CurrentActivity?.Intent?.GetStringExtra("review_editor_contexts") ?? "en-Light,fa-Light,de-Light,en-Dark,fa-Dark,de-Dark").Split(',');
        if (selectedContexts.Distinct().Count() != selectedContexts.Length || selectedContexts.Any(value => value is not ("en-Light" or "fa-Light" or "de-Light" or "en-Dark" or "fa-Dark" or "de-Dark")))
            throw new ReviewCheckException("Known unique editor contexts required.");
        var before = await StoredAsync(); var inputHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(before))); var observations = new List<object>();
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell();
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            if (!selectedContexts.Any(value => value.EndsWith("-" + choice, StringComparison.Ordinal))) continue;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            foreach (var language in new[] { "en", "fa", "de" })
            {
                if (!selectedContexts.Contains(language + "-" + choice)) continue;
                await SetLanguageAsync(app, local, language, ct);
                foreach (var form in selectedForms)
                {
                    await AwaitReimbursementFocusAsync(ct);
                    if (Math.Abs(Platform.CurrentActivity!.Resources!.Configuration!.FontScale - 2f) > .001f)
                        throw new ReviewCheckException("Actual OS scale changed during editor review.");
                    var receiptPath = Path.Combine(root, $"editor-text-{comparison}-{choice}-{language}-{form}.json");
                    if (File.Exists(receiptPath))
                    {
                        using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(receiptPath, ct));
                        var packet = retained.RootElement;
                        if (packet.GetProperty("SourceMvid").GetGuid() != typeof(AndroidHomeReview).Module.ModuleVersionId
                            || packet.GetProperty("InputHash").GetString() != inputHash || before != await StoredAsync())
                            throw new ReviewCheckException("Retained editor case source or stored inputs changed; preserve the original receipt.");
                        observations.Add(packet.GetProperty("Summary").Clone());
                        continue;
                    }
                    ContentPage page;
                    if (form is "account" or "debt")
                    {
                        var vm = ActivatorUtilities.CreateInstance<AccountEditorViewModel>(services, store, plans);
                        await vm.ApplyQueryAsync(form == "debt" ? new Dictionary<string, object> { ["debt"] = true } : new Dictionary<string, object>());
                        vm.Form.Name = "D356 fictitious draft"; vm.Form.OpeningText = "1234.56";
                        page = new AccountEditorPage(vm);
                    }
                    else if (form is "entry" or "transfer")
                    {
                        var vm = ActivatorUtilities.CreateInstance<EntryEditorViewModel>(services, store, fixture.GetRequiredService<Vafadar.Zanance.Data.Commerce.ReceiptWorkStore>());
                        // The original explicit loader is private; await its real Task rather than race async-void route delivery.
                        var load = typeof(EntryEditorViewModel).GetMethod("ApplyQueryAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            ?? throw new ReviewCheckException("Original transaction loader unavailable.");
                        await ((Task)load.Invoke(vm, [new Dictionary<string, object> { ["kind"] = form == "transfer" ? "Transfer" : "Expense" }])!).WaitAsync(ct);
                        vm.AmountText = "1234.56"; vm.EntryTitle = "D356 fictitious draft";
                        page = new EntryEditorPage(vm);
                    }
                    else if (form is "goal" or "earmark" or "goal-share" or "goal-cut")
                    {
                        var vm = ActivatorUtilities.CreateInstance<GoalEditorViewModel>(services, store, fixture.GetRequiredService<GoalStore>(), fixture.GetRequiredService<HoldingStore>());
                        await vm.ApplyQueryAsync(new Dictionary<string, object>());
                        vm.Name = "Fictitious original goal draft"; vm.AmountText = "1234.56";
                        vm.TypeIndex = form == "earmark" ? 1 : 0;
                        vm.MethodIndex = form == "goal-share" ? 1 : form == "goal-cut" ? 2 : 0;
                        vm.ContributionText = "12.34"; vm.PercentText = "15"; vm.HasTargetDate = true;
                        if (vm.ReminderEnabled) throw new ReviewCheckException("No notification action is authorized.");
                        page = new GoalEditorPage(vm, local);
                    }
                    else if (form == "budget")
                    {
                        var vm = ActivatorUtilities.CreateInstance<BudgetEditorViewModel>(services, store);
                        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["year"] = 2026, ["month"] = 10, ["currency"] = "EUR" });
                        page = new BudgetEditorPage(vm, local);
                    }
                    else if (form == "settlement")
                    {
                        var vm = ActivatorUtilities.CreateInstance<SettlementViewModel>(services, store, plans);
                        var load = typeof(SettlementViewModel).GetMethod("ApplyQueryAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            ?? throw new ReviewCheckException("Original settlement loader unavailable.");
                        var schedule = (await plans.GetSchedulesAsync(ct)).Single();
                        await ((Task)load.Invoke(vm, [new Dictionary<string, object> { ["id"] = schedule.Id }])!).WaitAsync(ct);
                        vm.ActualText = "123.45";
                        page = new SettlementPage(vm);
                    }
                    else
                    {
                        var vm = ActivatorUtilities.CreateInstance<PlanEditorViewModel>(services, store, plans);
                        await vm.ApplyQueryAsync(new Dictionary<string, object>());
                        vm.Name = "D356 fictitious draft"; vm.AmountText = "1234.56";
                        if (form == "monthly") { vm.PresetIndex = 3; vm.ShowRuleOptions = true; }
                        if (vm.AutoPost || vm.ReminderEnabled) throw new ReviewCheckException("No automatic posting or notification action is authorized.");
                        page = new PlanEditorPage(vm);
                    }
                    page.FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    await Shell.Current.Navigation.PushModalAsync(page, false); await Task.Delay(150, ct);
                    var failures = new List<object>(); var controls = new List<object>();
                    foreach (var control in ReimbursementViews(page).OfType<VisualElement>().Where(ReimbursementEffectivelyVisible).ToArray())
                    {
                        string? expected = control switch { Label label => label.Text, Picker picker when picker.SelectedIndex >= 0 => picker.Items[picker.SelectedIndex],
                            Entry entry => entry.Text, Button button => button.Text, _ => null };
                        if (string.IsNullOrWhiteSpace(expected) || expected == "?" || control.Handler?.PlatformView is not Android.Widget.TextView native) continue;
                        await AwaitEditorWindowFocusAsync(page, ct);
                        if (!ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page)) throw new ReviewCheckException("Original editor retired during review.");
                        native.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, native.Width, native.Height), true);
                        await Task.Delay(75, ct); await WaitLayoutAsync(native, ct);
                        if (!native.HasWindowFocus) throw new ReviewCheckException("Original modal editor lost native focus after layout.");
                        var issue = SettingsTextIssue(native);
                        var ancestors = new List<object>();
                        for (Element? ancestor = control; ancestor is VisualElement visual; ancestor = ancestor.Parent)
                            ancestors.Add(new { Type = visual.GetType().FullName, visual.Width, visual.Height, DesiredWidth = ((IView)visual).DesiredSize.Width });
                        var item = new { Control = control.GetType().Name, Text = expected, Issue = issue, Width = native.Width, Height = native.Height,
                            NativeTextSize = native.TextSize, NativeLines = native.Layout?.LineCount, FullNativeText = native.Text,
                            NativeTextWidth = native.Paint?.MeasureText(expected), LogicalWidth = control.Width, DesiredWidth = ((IView)control).DesiredSize.Width,
                            ParentWidth = (control.Parent as VisualElement)?.Width, NativeLayoutHeight = native.Layout?.Height, Ancestors = ancestors };
                        controls.Add(item); if (issue is not null) failures.Add(item);
                    }
                    var nativeChoiceTargets = 0;
                    foreach (var chips in ReimbursementViews(page).OfType<Vafadar.Maui.Controls.ChoiceChips>().Where(ReimbursementEffectivelyVisible))
                    {
                        if (chips.ItemsSource is not { } items) throw new ReviewCheckException("Original choice source unavailable.");
                        var buttons = ReimbursementViews(chips).OfType<Button>().ToArray();
                        if (buttons.Length != items.Count) throw new ReviewCheckException("Original choice targets incomplete.");
                        for (var index = 0; index < buttons.Length; index++)
                        {
                            var name = items[index]?.ToString() ?? string.Empty;
                            var expected = chips.SelectedIndex == index ? services.GetRequiredService<Translator>().Format("Common_ChipSelected", name) : name;
                            if (buttons[index].Handler?.PlatformView is not Android.Views.View target)
                                throw new ReviewCheckException("Original native choice target unavailable.");
                            var actual = EditorNativeDescription(target);
                            if (actual != expected)
                            {
                                LogReport("editor-text-native-name", Path.GetFileName(root), new { Comparison = comparison, Form = form, Language = language, Index = index, Expected = expected, Actual = actual });
                                throw new ReviewCheckException("Original native choice name or selected state differs.");
                            }
                            nativeChoiceTargets++;
                        }
                    }
                    var debtToggleChecked = false;
                    if (form == "debt")
                    {
                        var vm = (AccountEditorViewModel)page.BindingContext;
                        var draft = vm.Form.Snapshot();
                        async Task CheckToggleAsync(System.Windows.Input.ICommand command, string nameKey, Func<bool> isOpen)
                        {
                            var button = ReimbursementViews(page).OfType<Button>().Single(value => ReferenceEquals(value.Command, command));
                            if (button.Handler?.PlatformView is not Android.Views.View target || EditorNativeDescription(target) != services.GetRequiredService<Translator>()[nameKey]
                                || target.Height < 44 || isOpen()) throw new ReviewCheckException("Original debt action name, initial state or target differs.");
                            target.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, target.Width, target.Height), true);
                            await Task.Delay(100, ct); await AwaitEditorWindowFocusAsync(page, ct);
                            if (!target.PerformClick()) throw new ReviewCheckException("Original debt action refused.");
                            await Task.Delay(100, ct);
                            if (!isOpen()) throw new ReviewCheckException("Original debt fields did not open.");
                            if (!target.PerformClick()) throw new ReviewCheckException("Original debt close action refused.");
                            await Task.Delay(100, ct);
                            if (isOpen() || draft != vm.Form.Snapshot()) throw new ReviewCheckException("Debt-options navigation changed draft values.");
                        }
                        await CheckToggleAsync(vm.Form.ToggleDebtTermsCommand, "Debt_EstimateOptions", () => vm.Form.ShowDebtTerms);
                        await CheckToggleAsync(vm.Form.ToggleAccountDetailsCommand, "Debt_AccountDetails", () => vm.Form.ShowAccountDetails);
                        debtToggleChecked = true;
                    }
                    if (before != await StoredAsync()) throw new ReviewCheckException("Unsaved editor geometry changed stored financial inputs.");
                    var summary = new { Comparison = comparison, Form = form, Language = language, Theme = choice.ToString(), CheckedControls = controls.Count,
                        Failures = failures, NativeChoiceTargetsChecked = nativeChoiceTargets, NativeDebtToggleOpenedAndClosed = debtToggleChecked,
                        StoredInputsUnchanged = true, SaveInvoked = false, SourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId };
                    var result = new { Summary = summary, InputHash = inputHash, Controls = controls, SourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId };
                    observations.Add(summary);
                    // Retain each finished original case before later navigation or a native failure.
                    await using (var receipt = new FileStream(receiptPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                        await JsonSerializer.SerializeAsync(receipt, result, cancellationToken: ct);
                    LogReport($"editor-text-{choice}-{language}-{form}", Path.GetFileName(root), summary);
                    await Shell.Current.Navigation.PopModalAsync(false);
                }
            }
        }
        return new { Comparison = comparison, OsFontScale = configuration.FontScale, WidthDp = configuration.ScreenWidthDp,
            SelectedForms = selectedForms, SelectedContexts = selectedContexts, Observations = observations, StoredInputsUnchanged = before == await StoredAsync(), MainPackageTouched = false, SaveInvoked = false };
    }

    /// <summary>Reads the node exposed to accessibility clients, including the original MAUI semantic delegate.</summary>
    private static string? EditorNativeDescription(Android.Views.View target)
    {
        // MAUI assigns the semantic description to node info, not the platform View.ContentDescription property.
        using var node = target.CreateAccessibilityNodeInfo();
        return node?.ContentDescription;
    }

    /// <summary>Requires the original modal window, which is separate from the underlying Activity window on Android.</summary>
    private static async Task AwaitEditorWindowFocusAsync(ContentPage page, CancellationToken ct)
    {
        // MAUI ModalNavigationManager presents a DialogFragment; requiring Activity focus would reject its valid modal.
        while (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page))
        {
            if (page.Handler?.PlatformView is Android.Views.View { HasWindowFocus: true }) return;
            await Task.Delay(100, ct);
        }
        throw new ReviewCheckException("Original editor modal retired before native focus.");
    }
}
