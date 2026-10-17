#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original safety-row captions and native targets using display-only candidates in an independent package.</summary>
    private static async Task ReviewSafetyCopiesAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.safetycopies")
            throw new IOException("Independent safety presentation package required.");
        var complete = Path.Combine(folder, "safety-copies.complete.json");
        if (File.Exists(complete)) throw new IOException("Do not repeat completed safety presentation qualification.");
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var translator = services.GetRequiredService<Translator>(); var contexts = 0; var captions = new List<object>();
        var window = app.Windows[0];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            var vm = services.GetRequiredService<BackupViewModel>(); vm.IndependentRestoreReview = true;
            var title = translator.Format("Backup_SafetyFile", services.GetRequiredService<Vafadar.Localization.Formatting.IDateFormatter>()
                .Format(new DateOnly(2026, 10, 17)) + " 13:30");
            var candidate = new LocalBackup(new BackupFileInfo("safety-11111111111111111111111111111111.vbs",
                "safety-11111111111111111111111111111111.vbs", 25000, null), title, "25 KB", DeviceOnly: true);
            vm.SafetyBackups.Clear(); vm.SafetyBackups.Add(candidate); vm.HasSafetyBackups = true;
            var page = new BackupPage(vm).WithFlowDirection(local); var root = window.Page ?? throw new IOException("Review root is unavailable.");
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
            var outer = VisualDescendants(page).OfType<ScrollView>().First();
            foreach (var text in new[] { translator["Backup_SafetyTitle"], translator["Backup_SafetyHint"], title })
            {
                var label = VisualDescendants(page).OfType<Label>().Single(value => value.Text == text);
                await ScrollToViewIfNeededAsync(outer, label); CheckCaption(label, captions);
            }
            var button = VisualDescendants(page).OfType<Button>().Single(value => ReferenceEquals(value.CommandParameter, candidate));
            await ScrollToViewIfNeededAsync(outer, button);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement native || native.ActualWidth < 44 || native.ActualHeight < 44
                || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) != title || button.Command is null)
                throw new IOException("Original safety row lacks a complete named native target.");
            await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-safety.png"));
            vm.WatchAutomatic(false); await root.Navigation.PopModalAsync(false); contexts++;
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Contexts = contexts, NativeTargets = contexts,
            CaptionChecks = captions.Count, DisplayOnlyCandidates = true, NoRestoreOrFinancialCommandsInvoked = true,
            FullRestoreNotExercisedHere = true }));
    }
}
#endif
