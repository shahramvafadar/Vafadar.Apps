#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;
namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies actual original event loading/error/Retry controls and routed draft rebinding.</summary>
    private static async Task ReviewHoldingEventPublicationWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var captures = new List<object>();
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(), Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var before = await BaseRows();
        bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        async Task SetTheme(ThemeChoice value)
        {
            if (theme.Choice == value) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(value); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Show(AssetEventEditorPage page) { await Shell.Current.Navigation.PushAsync(page, false); await Task.Delay(200); }
        async Task Close(AssetEventEditorPage page)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original event page changed.");
            await Shell.Current.Navigation.PopAsync(false);
        }
        async Task Retry(AssetEventEditorPage page)
        {
            var button = page.FindByName<Button>("RetryLoadButton");
            if (!Visible(button)) throw new IOException("Original event Retry is covered.");
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider action)
                throw new IOException("Original event native Retry Invoke missing.");
            action.Invoke(); await Task.Yield();
        }
        async Task Observe(AssetEventEditorPage page, string stage)
        {
            await Task.Delay(200); var vm = (AssetEventEditorViewModel)page.BindingContext;
            if (Visible(page.FindByName<Grid>("ReadySurface")) != vm.Loading.IsReady
                || Visible(page.FindByName<VerticalStackLayout>("LoadingSurface")) != vm.Loading.IsLoading
                || Visible(page.FindByName<VerticalStackLayout>("ErrorSurface")) != vm.Loading.HasFailed)
                throw new IOException("Original event native cover differs.");
            var label = vm.Loading.IsLoading ? page.FindByName<Label>("LoadingCaption") : vm.Loading.HasFailed ? page.FindByName<Label>("ErrorCaption") : null;
            var text = vm.Loading.IsLoading ? services.GetRequiredService<Translator>()["Holdings_Loading"] : vm.LoadError;
            if (label is not null)
            {
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } native
                    || native.Text != NativeDigits.Apply(text))
                { if (DateTime.UtcNow > deadline) throw new IOException("Original complete event cover caption missing."); await Task.Delay(50); }
            }
            if (vm.Loading.IsReady)
            {
                if (page.FindByName<Entry>("QuantityInput").Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox entry || entry.Text != vm.QuantityText)
                    throw new IOException("Original native event quantity binding differs.");
                var save = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, vm.SaveCommand));
                if (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.IsEnabled != vm.CanSave)
                    throw new IOException("Original event native Save enablement differs.");
            }
            var directory = Path.Combine(folder, stage); Directory.CreateDirectory(directory);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "event.png")); captures.Add(new { Language = stage, File = "event.png" });
        }
        var original = await HoldingEventPublicationNativeReview.PrepareAsync(services, Path.Combine(folder, "original"), CancellationToken.None);
        try
        {
            var first = true;
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await HoldingEventPublicationNativeReview.ContextAsync(services, original, name + "-" + value + "-" + width, first, Show, Retry, Close, Observe, CancellationToken.None); first = false;
            }
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally
        {
            if (Shell.Current.CurrentPage is AssetEventEditorPage page) await Close(page);
            await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName);
        }
        if (original.Rows != await ImportFinancialNativeReview.StoredAsync(original.Fixture, CancellationToken.None) || before != await BaseRows())
            throw new IOException("Original event native observation changed fixture/base rows.");
        await original.Fixture.DisposeAsync();
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, OriginalTypedRowsUnchanged = true, OriginalBaseStoreUnchanged = true, OnlySeparateFictitiousLedgerChanged = true, NoFinancialOrMetadataCommand = true, ControlledReadOnWorker = true, OriginalLanguageAndThemeRestored = true }));
    }
}
#endif
