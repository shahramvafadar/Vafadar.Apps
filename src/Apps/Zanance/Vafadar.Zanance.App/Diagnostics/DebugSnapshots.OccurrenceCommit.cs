#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Invokes original occurrence commands and their actual native post-commit dialogs in an isolated installation.</summary>
    private static async Task ReviewOccurrenceCommitAsync(App app, IServiceProvider services, string folder, bool stateConflict = false)
    {
        if (!AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)) throw new IOException("Independent occurrence review required.");
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var theme = services.GetRequiredService<ThemeService>(); var store = services.GetRequiredService<ZananceStore>();
        await SetStoreThemeAsync(app, theme, ThemeChoice.Light); await SetLanguageAsync(app, local, "en"); app.Windows[0].Width = 412;
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await BaseRows();
        var root = (app.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window)?.Content.XamlRoot ?? throw new IOException("Original native root unavailable.");
        IEnumerable<Microsoft.UI.Xaml.DependencyObject> Tree(Microsoft.UI.Xaml.DependencyObject item)
        {
            yield return item;
            for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(item); i++)
                foreach (var child in Tree(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(item, i))) yield return child;
        }
        void Invoke(Microsoft.UI.Xaml.Controls.Button button)
        {
            if (!button.IsEnabled || button.XamlRoot != root || button.Visibility != Microsoft.UI.Xaml.Visibility.Visible)
                throw new IOException("Original owned native button unavailable.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
            if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action) throw new IOException("Original native Invoke unavailable.");
            action.Invoke();
        }
        async Task Answer(OccurrenceCommitNativeReview.Dialog expected, string context)
        {
            Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
            for (var i = 0; i < 200; i++)
            {
                dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(p => p.Child)
                    .OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault(d => d.Title as string == expected.Title);
                if (dialog is not null && Tree(dialog).OfType<Microsoft.UI.Xaml.Controls.TextBlock>().Any(t => t.Text == expected.Message)) break;
                await Task.Delay(50);
            }
            if (dialog is null) throw new IOException("The original native occurrence dialog did not open.");
            await Task.Delay(100);
            var body = Tree(dialog).OfType<Microsoft.UI.Xaml.Controls.TextBlock>().Single(t => t.Text == expected.Message);
            if (body.IsTextTrimmed || body.ActualWidth < 1 || body.ActualHeight < 1) throw new IOException("Original native saved feedback is incomplete.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, context + ".png"));
            Invoke(FindNativeButton(dialog, expected.Accept) ?? throw new IOException("The original native dialog answer is missing."));
        }
        Func<IServiceProvider, string, Func<Features.Plans.OccurrencePage, Task>, Func<Features.Plans.OccurrencePage, System.Windows.Input.ICommand, object?, Task>, Func<OccurrenceCommitNativeReview.Dialog, string, Task>, Func<Features.Plans.OccurrencePage, Task>, CancellationToken, Task<object[]>> review = stateConflict ? OccurrenceStateNativeReview.RunAsync : OccurrenceCommitNativeReview.RunAsync;
        var cases = await review(services, folder,
            async page => await Shell.Current.Navigation.PushAsync(page, false),
            async (page, command, parameter) =>
            {
                var target = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command)
                    && (parameter is null || ReferenceEquals(b.CommandParameter, parameter)));
                await ScrollToViewIfNeededAsync(page.FindByName<ScrollView>("ValidationViewport"), target); await Task.Delay(100);
                Invoke(target.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button ?? throw new IOException("Original occurrence button not attached."));
            }, Answer,
            async page => { if (ReferenceEquals(Shell.Current.CurrentPage, page)) await Shell.Current.Navigation.PopAsync(false); }, CancellationToken.None);
        if (before != await BaseRows()) throw new IOException("The occurrence fixture changed the original base store.");
        var captures = new List<object>(); var native = services.GetRequiredService<IAppInteraction>();
        // Inspect translations/widths through the original native adapter only; never replay accepted financial commands.
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in stateConflict ? new[] { 412d } : new[] { 360d, 412d, 1280d })
        {
            await SetStoreThemeAsync(app, theme, choice); await SetLanguageAsync(app, local, language); app.Windows[0].Width = width;
            var context = language + "-" + choice + "-" + width; var path = Path.Combine(folder, context); Directory.CreateDirectory(path);
            var expected = new OccurrenceCommitNativeReview.Dialog(false, text["Error_Title"], text[stateConflict ? "Error_Unexpected" : "Occurrence_SavedNotificationFailed"], text["Common_Ok"], text["Common_Ok"]);
            var showing = native.AlertAsync(expected.Title, expected.Message, expected.Accept);
            await Answer(expected, context + "-saved-message"); await showing;
            File.Move(Path.Combine(folder, context + "-saved-message.png"), Path.Combine(path, "saved-message.png"));
            captures.Add(new { Language = context, File = "saved-message.png" });
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures, Cases = cases,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = before == await BaseRows(),
            NoFinancialReplayForTranslatedMessages = true }));
    }
}
#endif
