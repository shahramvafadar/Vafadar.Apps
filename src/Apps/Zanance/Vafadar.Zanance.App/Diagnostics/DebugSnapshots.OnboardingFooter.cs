#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks only the corrected original footer through all three wizard steps, retaining native command ownership and busy state.</summary>
    private static async Task ReviewOnboardingFooterWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>(); var local = services.GetRequiredService<ILocalizationService>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await StoredAsync(); var captures = new List<object>();
        // Continue only explicit unfinished contexts; retain completed cohorts and their original process.
        var requested = Environment.GetEnvironmentVariable("VAFADAR_EDITOR_ONBOARDING_CONTEXTS");
        var contexts = string.IsNullOrEmpty(requested) ? null : requested.Split(',').ToHashSet(StringComparer.Ordinal);
        var validContexts = (from language in new[] { "en", "fa", "de" }
            from theme in new[] { "Light", "Dark" } from width in new[] { "360", "412", "1280" }
            select language + "-" + theme + "-" + width).ToHashSet(StringComparer.Ordinal);
        if (contexts is not null && (contexts.Count != requested!.Split(',').Length || !contexts.IsSubsetOf(validContexts)))
            throw new IOException("Footer continuation contexts are invalid or duplicated.");
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            var outputName = language + "-" + theme + "-" + width;
            if (contexts is not null && !contexts.Contains(outputName)) continue;
            void Stage(string stage, int step = 0) => File.WriteAllText(Path.Combine(folder, "progress.json"),
                JsonSerializer.Serialize(new { ProcessId = Environment.ProcessId, Context = outputName, Step = step, Stage = stage }));
            Stage("original-home-navigation");
            await Shell.Current.GoToAsync("//home"); app.Windows[0].Width = width;
            Stage("original-theme-publication");
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            Stage("original-language-publication");
            await SetLanguageAsync(app, local, language);
            var output = Path.Combine(folder, outputName); Directory.CreateDirectory(output); var cases = new List<object>();
            foreach (var step in new[] { 1, 2, 3 })
            {
                Stage("original-wizard-construction", step);
                var originalShell = Shell.Current; var themeService = services.GetRequiredService<ThemeService>(); var constructorChanges = 0;
                void ChangedDuringConstruction(object? sender, EventArgs args) => constructorChanges++;
                OnboardingViewModel vm; OnboardingPage page;
                themeService.Changed += ChangedDuringConstruction;
                try
                {
                    vm = ActivatorUtilities.CreateInstance<OnboardingViewModel>(services); vm.Step = step;
                    typeof(OnboardingViewModel).GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(vm, null);
                    page = new OnboardingPage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                }
                finally { themeService.Changed -= ChangedDuringConstruction; }
                if (constructorChanges != 0 || !ReferenceEquals(originalShell, Shell.Current))
                    throw new IOException("Original wizard construction changed the active theme or retired its modal owner.");
                Stage("original-modal-publication", step);
                await originalShell.Navigation.PushModalAsync(page, false); await Task.Delay(200);
                if (!ReferenceEquals(originalShell, app.Windows[0].Page) || !originalShell.Navigation.ModalStack.Contains(page))
                    throw new IOException("Original wizard lost its live modal owner during publication.");
                Stage("native-footer-caption-and-command", step);
                var footer = page.FindByName<FlexLayout>("Footer"); var actions = footer.Children.OfType<WrappingAction>().Where(Visible).ToArray();
                if (actions.Length != (vm.CanGoBack ? 2 : 1)) throw new IOException("Original footer visibility differs.");
                var states = new List<object>();
                foreach (var action in actions)
                {
                    var label = VisualDescendants(action).OfType<Label>().Single(); var button = VisualDescendants(action).OfType<Button>().Single();
                    CheckCaption(label, ProfileNativeReads);
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                        || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native) is not { } peer
                        || peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider
                        || native.ActualWidth < 44 || native.ActualHeight < 44 || !native.IsEnabled || peer.GetName() != action.Text
                        || (!ReferenceEquals(button.Command, vm.BackCommand) && !ReferenceEquals(button.Command, vm.NextCommand)))
                        throw new IOException("Original responsive footer command, name or target is incomplete.");
                    ProfileNativeReads.Add(peer);
                    states.Add(new { Caption = action.Text, NativeName = peer.GetName(), TargetWidth = native.ActualWidth, TargetHeight = native.ActualHeight,
                        CaptionWidth = label.Width, CaptionHeight = label.Height, OriginalCommandBound = true });
                }
                var next = actions.Single(action => ReferenceEquals(action.Command, vm.NextCommand));
                var nextButton = VisualDescendants(next).OfType<Button>().Single(); vm.IsBusy = true; await Task.Delay(100);
                if (nextButton.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: false }) throw new IOException("Busy onboarding still enables its action.");
                vm.IsBusy = false; await Task.Delay(100);
                if (nextButton.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } || before != await StoredAsync())
                    throw new IOException("Onboarding busy preview changed stored inputs or failed to restore enablement.");
                Stage("own-window-render", step);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "step-" + step + "-footer.png"));
                cases.Add(new { Step = step, Actions = states, BusyDisablesNext = true, OriginalBusyStateRestored = true,
                    ConstructionThemeChanges = constructorChanges, OriginalModalOwnerPreserved = true, NavigationOrFinancialSaveInvoked = false });
                Stage("original-modal-return", step);
                await Shell.Current.Navigation.PopModalAsync(false);
            }
            await File.WriteAllTextAsync(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(new { Cases = cases, StoredInputsUnchanged = true,
                Language = language, Theme = theme.ToString(), Width = width, WindowsLayoutStressOnly = true }));
            captures.Add(new { Language = outputName, File = "step-3-footer.png" });
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = before == await StoredAsync(), WindowsLayoutStressOnly = true }));
    }
}
#endif
