#if DEBUG && WINDOWS
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    private static async Task ReviewHoldingHistoryWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var originalTheme = theme.Choice;
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(),
            Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var originalRun = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_HISTORY_CONTINUATION");
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
        Task Show(HoldingDetailPage page) => Shell.Current.Navigation.PushAsync(page, false);
        async Task Close(HoldingDetailPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original price page changed.");
            await Shell.Current.Navigation.PopAsync(false);
        }
        async Task Invoke(HoldingDetailPage page, ICommand command, object? argument)
        {
            await Task.Delay(150);
            var button = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command)
                && Equals(b.CommandParameter, argument) && Visible(b));
            var scroll = FindScrollView(page) ?? throw new IOException("Original history viewport missing.");
            var inside = false;
            for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                if (ReferenceEquals(parent, scroll)) { inside = true; break; }
            if (inside) await scroll.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original native price Invoke missing.");
            action.Invoke(); await Task.Yield();
        }
        async Task<object> Observe(HoldingDetailPage page, string stage, bool capture = true)
        {
            await Task.Delay(200);
            var model = (HoldingDetailViewModel)page.BindingContext; var editor = model.HistoryEditor;
            if (model.PriceEditor.Raw != "77") throw new IOException("Original history changed the separate price draft.");
            var feedback = page.FindByName<Label>("HoldingHistoryFeedback");
            if (editor.Error is not null)
            {
                var limit = DateTime.UtcNow.AddSeconds(15);
                while (feedback.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } native
                    || native.Text != NativeDigits.Apply(editor.Error))
                { if (DateTime.UtcNow > limit) throw new IOException("Original complete history feedback missing."); await Task.Delay(50); }
            }
            var undo = VisualDescendants(page).OfType<Button>().Where(b => ReferenceEquals(b.Command, editor.UndoCommand) && Visible(b)).ToList();
            if (undo.Count != (editor.HasUndo ? 1 : 0) || undo.Any(button => button.IsEnabled != editor.CanUndo))
                throw new IOException("Original native Undo offer state differs from the actual receipt.");
            if (capture)
            {
                var directory = Path.Combine(folder, stage); Directory.CreateDirectory(directory);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "history.png"));
                captures.Add(new { Language = stage, File = "history.png" });
            }
            return new { Stage = stage, editor.Error, editor.HasUndo, editor.CanUndo, editor.CanOpen, editor.NeedsRefresh,
                OriginalRawPrice = model.PriceEditor.Raw, NativeUndoStateChecked = true, ProcessId = Environment.ProcessId };
        }
        HoldingHistoryNativeReview.Prepared? prepared = null;
        try
        {
            prepared = !string.IsNullOrEmpty(originalRun)
                ? await HoldingHistoryNativeReview.ContinueAsync(services,
                    Path.Combine(Directory.GetParent(folder)!.FullName, originalRun, "financial"), originalRun, CancellationToken.None)
                : await HoldingHistoryNativeReview.PrepareAsync(services, Path.Combine(folder, "financial"), Path.GetFileName(folder),
                    Show, Invoke, Close, (page, stage) => Observe(page, "original-" + stage), CancellationToken.None);
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                foreach (var state in prepared.States)
                {
                    var page = new HoldingDetailPage(state.Model); await Show(page);
                    if (state.Model.HistoryEditor.CanUndo || state.Model.HistoryEditor.CanOpen || state.Model.PriceEditor.CanSubmit)
                        throw new IOException("Retired history caption recovered financial authority.");
                    await Observe(page, name + "-" + choice + "-" + width + "-" + state.Name); await Close(page);
                }
            }
            if (prepared.Rows != await ImportFinancialNativeReview.StoredAsync(prepared.Fixture, CancellationToken.None))
                throw new IOException("Original price caption observation changed rows.");
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally
        {
            if (Shell.Current.CurrentPage is HoldingDetailPage) await Shell.Current.Navigation.PopAsync(false);
            if (prepared is not null) await prepared.Fixture.DisposeAsync();
            await SetTheme(originalTheme); await SetLanguageAsync(app, local, language.CultureName);
        }
        if (before != await BaseRows()) throw new IOException("Independent base store changed during price review.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Cases = prepared!.Cases, Live = prepared.Live, Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true,
            OriginalThemeAndLanguageRestored = true, ReadOnlyCaptions = 72, ConfirmationUsesExplicitPort = true, OriginalHistoryContinuation = !string.IsNullOrEmpty(originalRun),
            FinancialCommandsInvokedByContinuation = string.IsNullOrEmpty(originalRun) ? (int?)null : 0 }));
    }
}
#endif
