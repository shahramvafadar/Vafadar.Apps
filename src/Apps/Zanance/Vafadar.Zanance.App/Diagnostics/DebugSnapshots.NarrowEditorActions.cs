#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies only changed icon/pattern actions in original shared consumers on the independent non-input desktop.</summary>
    private static async Task ReviewNarrowEditorActionsWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(), Plans = await plans.GetSchedulesAsync() });
        var before = await StoredAsync(); var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            await Shell.Current.GoToAsync("//home"); app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            var outputName = language + "-" + theme + "-" + width;
            var output = Path.Combine(folder, outputName); Directory.CreateDirectory(output);
            var cases = new List<object>();
            foreach (var form in new[] { "account", "debt-details", "monthly", "onboarding-account" })
            {
                ContentPage page; AccountFormModel? account = null; PlanEditorViewModel? plan = null;
                if (form is "account" or "debt-details")
                {
                    var vm = ActivatorUtilities.CreateInstance<AccountEditorViewModel>(services);
                    await vm.ApplyQueryAsync(form == "debt-details" ? new Dictionary<string, object> { ["debt"] = true } : new Dictionary<string, object>());
                    account = vm.Form; account.Name = "D364 fictitious unsaved draft"; account.OpeningText = "1234.56";
                    if (form == "debt-details") account.ExpandedAccountDetails = true;
                    page = new AccountEditorPage(vm);
                }
                else if (form == "onboarding-account")
                {
                    var vm = ActivatorUtilities.CreateInstance<OnboardingViewModel>(services); vm.Step = 3;
                    account = vm.Account; account.Name = "D364 first account"; account.OpeningText = "1234.56";
                    typeof(OnboardingViewModel).GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(vm, null);
                    page = new OnboardingPage(vm);
                }
                else
                {
                    plan = ActivatorUtilities.CreateInstance<PlanEditorViewModel>(services);
                    await plan.ApplyQueryAsync(new Dictionary<string, object>());
                    plan.Name = "D364 monthly draft"; plan.AmountText = "1234.56"; plan.PresetIndex = 3; plan.ShowRuleOptions = true;
                    page = new PlanEditorPage(plan);
                }
                page.FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                await Shell.Current.Navigation.PushModalAsync(page, false); await Task.Delay(200);
                var command = account is not null ? account.ToggleIconPickerCommand : plan!.ToggleRuleOptionsCommand;
                var action = VisualDescendants(page).OfType<WrappingAction>().Single(value => ReferenceEquals(value.Command, command));
                var button = VisualDescendants(action).OfType<Button>().Single(); var caption = VisualDescendants(action).OfType<Label>().Single();
                string Snapshot() => account is not null ? account.Snapshot() : (string)typeof(PlanEditorViewModel)
                    .GetMethod("Snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(plan, null)!;
                var draft = Snapshot(); var initial = account?.ShowIconPicker ?? plan!.ShowRuleOptions; var states = new List<object>();
                for (var step = 0; step < 3; step++)
                {
                    var scroll = VisualDescendants(page).OfType<ScrollView>().First();
                    await ScrollToViewIfNeededAsync(scroll, action); await Task.Delay(150);
                    CheckCaption(caption, ProfileNativeReads);
                    var opened = account?.ShowIconPicker ?? plan!.ShowRuleOptions;
                    var expected = translator[account is not null ? opened ? "Account_HideIcons" : "Account_ChooseIcon" : "Plan_ChangePattern"];
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                        || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native) is not { } peer
                        || !native.IsEnabled || native.ActualWidth < 44 || native.ActualHeight < 44 || peer.GetName() != expected || caption.Text != expected)
                        throw new IOException("Changed original action lost its complete native name, enabled target or caption.");
                    ProfileNativeReads.Add(peer);
                    states.Add(new { Opened = opened, Name = peer.GetName(), CaptionWidth = caption.Width, CaptionHeight = caption.Height,
                        TargetWidth = native.ActualWidth, TargetHeight = native.ActualHeight });
                    if (step == 2) break;
                    InvokeChoiceReview(button, ProfileNativeReads); await Task.Delay(150);
                    if ((account?.ShowIconPicker ?? plan!.ShowRuleOptions) == opened) throw new IOException("Original action did not change disclosure state.");
                }
                if (draft != Snapshot() || (account?.ShowIconPicker ?? plan!.ShowRuleOptions) != initial || before != await StoredAsync())
                    throw new IOException("Nonfinancial action changed draft/stored values or its original state.");
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, form + "-action.png"));
                cases.Add(new { Form = form, States = states, OriginalNativeInvokeTwice = true, OriginalDraftPreserved = true,
                    StoredInputsUnchanged = true, SaveInvoked = false });
                await Shell.Current.Navigation.PopModalAsync(false);
            }
            await File.WriteAllTextAsync(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(new { Cases = cases,
                Language = language, Theme = theme.ToString(), Width = width, WindowsLayoutStressOnly = true }));
            captures.Add(new { Language = outputName, File = "onboarding-account-action.png" });
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = before == await StoredAsync(), WindowsLayoutStressOnly = true }));
    }
}
#endif
