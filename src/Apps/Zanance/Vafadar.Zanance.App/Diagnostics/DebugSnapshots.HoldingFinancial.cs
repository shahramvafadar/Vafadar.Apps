#if DEBUG && WINDOWS
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Reuses two accepted original financial cases across read-only language/theme/width presentation contexts.</summary>
    private static async Task ReviewHoldingFinancialWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var originalLanguage = local.CurrentLanguage; var originalTheme = theme.Choice;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await BaseRows(); var captures = new List<object>();
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        Task Show(AssetEventEditorPage page) => Shell.Current.Navigation.PushAsync(page, false);
        async Task Close(AssetEventEditorPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original holding page changed.");
            await Shell.Current.Navigation.PopAsync(false);
        }
        async Task Invoke(AssetEventEditorPage page, ICommand command)
        {
            await Task.Delay(200);
            var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command) && Visible(item));
            var scroll = FindScrollView(page) ?? throw new IOException("Original holding viewport missing.");
            var inViewport = false;
            for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                if (ReferenceEquals(parent, scroll)) { inViewport = true; break; }
            if (inViewport) await scroll.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original native holding Invoke unavailable.");
            action.Invoke(); await Task.Yield();
        }
        HoldingEventNativeReview.Prepared? prepared = null;
        try
        {
            await SetTheme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en");
            app.Windows[0].Width = 412; app.Windows[0].Height = 820;
            var originalRun = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_FINANCIAL_CONTINUATION");
            var financialFolder = Path.Combine(folder, "financial");
            if (!string.IsNullOrEmpty(originalRun))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(originalRun, "^[0-9a-f]{32}$") || Path.GetFileName(folder) == originalRun)
                    throw new IOException("Exact different original holding run required.");
                var originalFolder = Path.Combine(Path.GetDirectoryName(folder)!, originalRun);
                if (!File.ReadAllText(Path.Combine(originalFolder, "primary-error.txt")).Contains("Original native split Invoke unavailable.", StringComparison.Ordinal)
                    || !File.ReadAllText(Path.Combine(originalFolder, "error.txt")).Contains("Original holding type no longer exists.", StringComparison.Ordinal)
                    || File.Exists(Path.Combine(originalFolder, "complete.json"))
                    || (File.GetAttributes(originalFolder) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Preserved original pre-command holding failure required.");
                financialFolder = Path.Combine(originalFolder, "financial");
                await File.WriteAllTextAsync(Path.Combine(folder, "original-continuation.json"), JsonSerializer.Serialize(new
                { OriginalRun = originalRun, OriginalFixtureRetained = true, NoFinancialReplay = true }));
            }
            prepared = await HoldingEventNativeReview.PrepareAsync(services, financialFolder, Show, Invoke, Close, CancellationToken.None,
                retainBeforeFirstCommand: !string.IsNullOrEmpty(originalRun));
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, language);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var context = language + "-" + choice + "-" + width; var directory = Path.Combine(folder, context); Directory.CreateDirectory(directory);
                foreach (var state in prepared.States)
                {
                    // Retain the actual original accepted/refused session. Only its translated feedback is republished.
                    state.Model.RetireReview();
                    state.Model.Error = services.GetRequiredService<Translator>()[state.ErrorKey];
                    var page = new AssetEventEditorPage(state.Model); await Show(page); await Task.Delay(150);
                    var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == state.Model.Error && Visible(item));
                    var scroll = FindScrollView(page) ?? throw new IOException("Original holding viewport missing.");
                    await scroll.ScrollToAsync(label, ScrollToPosition.MakeVisible, false);
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } native
                        || native.Text != state.Model.Error)
                    {
                        if (DateTime.UtcNow >= deadline) throw new IOException("Original holding feedback is incomplete; retain original financial receipts.");
                        await Task.Delay(50);
                    }
                    if (state.Model.CanSave || state.Model.CloseText != services.GetRequiredService<Translator>()[state.Name == "accepted" ? "Common_Close" : "Common_Cancel"])
                        throw new IOException("Original retained outcome became financially actionable.");
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, state.Name + ".png"));
                    captures.Add(new { Language = context, File = state.Name + ".png" }); await Close(page);
                }
            }
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally
        {
            if (Shell.Current.CurrentPage is AssetEventEditorPage) await Shell.Current.Navigation.PopAsync(false);
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, originalLanguage.CultureName);
        }
        if (before != await BaseRows()) throw new IOException("Original independent base store changed.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Cases = prepared!.Cases, Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true, OriginalThemeAndLanguageRestored = true,
            ActualNativeInvoke = true, ReadOnlyPresentationAfterTwoFinancialCases = true, NoOwnerDesktopInput = true }));
    }
}
#endif
