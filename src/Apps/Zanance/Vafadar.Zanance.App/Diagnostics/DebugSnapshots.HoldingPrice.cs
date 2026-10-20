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
    private static async Task ReviewHoldingPriceWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var originalTheme = theme.Choice;
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(),
            Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var originalRun = Environment.GetEnvironmentVariable("VAFADAR_HOLDING_PRICE_CONTINUATION");
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
            await (FindScrollView(page) ?? throw new IOException("Original price viewport missing.")).ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            await Task.Delay(150);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original native price Invoke missing.");
            action.Invoke(); await Task.Yield();
        }
        async Task Observe(HoldingDetailPage page, string stage)
        {
            await Task.Delay(200);
            var model = (HoldingDetailViewModel)page.BindingContext; var editor = model.PriceEditor;
            var input = page.FindByName<Entry>("ManualPriceInput"); var feedback = page.FindByName<Label>("ManualPriceFeedback");
            if (input.Text != editor.Raw || !ReferenceEquals(input.Parent, feedback.Parent))
                throw new IOException("Original price draft or field feedback binding changed.");
            if (editor.Error is not null)
            {
                await (FindScrollView(page) ?? throw new IOException("Original price viewport missing.")).ScrollToAsync(feedback, ScrollToPosition.MakeVisible, false);
                var limit = DateTime.UtcNow.AddSeconds(15);
                while (feedback.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } native
                    || native.Text != NativeDigits.Apply(editor.Error))
                { if (DateTime.UtcNow > limit) throw new IOException("Original complete price feedback missing."); await Task.Delay(50); }
            }
            var save = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, editor.SaveCommand) && Visible(b));
            if (save.IsEnabled != editor.CanSubmit) throw new IOException("Original price Save state differs.");
            var directory = Path.Combine(folder, stage); Directory.CreateDirectory(directory);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "price.png"));
            captures.Add(new { Language = stage, File = "price.png", editor.Raw, editor.Error, editor.CanSubmit, editor.NeedsRefresh });
        }
        HoldingPriceNativeReview.Prepared? prepared = null;
        try
        {
            prepared = !string.IsNullOrEmpty(originalRun)
                ? await HoldingPriceNativeReview.ContinueAsync(services, Path.Combine(Directory.GetParent(folder)!.FullName, originalRun), CancellationToken.None)
                : await HoldingPriceNativeReview.PrepareAsync(services, Path.Combine(folder, "financial"), Show, Invoke, Close,
                (page, stage) => Observe(page, "original-" + stage), CancellationToken.None);
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                prepared.Model.PriceEditor.Retire();
                prepared.Model.PriceEditor.Error = services.GetRequiredService<Translator>()["Holding_PriceSourceChanged"];
                var page = new HoldingDetailPage(prepared.Model); await Show(page);
                await Observe(page, name + "-" + choice + "-" + width); await Close(page);
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
        { Cases = prepared!.Cases, Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true,
            OriginalThemeAndLanguageRestored = true, ReadOnlyCaptions = 18, ConfirmationUsesExplicitPort = true, OriginalPriceContinuation = !string.IsNullOrEmpty(originalRun),
            PriceCommandsInvoked = string.IsNullOrEmpty(originalRun) ? 4 : 0 }));
    }
}
#endif
