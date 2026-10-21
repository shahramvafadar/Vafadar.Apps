#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies original monetary captions on the application's own isolated non-input Windows desktop.</summary>
    private static async Task ReviewAccountMoneyWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var ct = CancellationToken.None; var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var captures = new List<object>();
        var currencyOnly = Environment.GetEnvironmentVariable("VAFADAR_ACCOUNT_CURRENCY_ONLY") == "1";
        var defaultsOnly = Environment.GetEnvironmentVariable("VAFADAR_ACCOUNT_DEFAULT_ONLY") == "1";
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_SOURCE_RUN");
        var source = string.IsNullOrEmpty(sourceRun) ? folder : Path.Combine(Path.GetDirectoryName(folder)!, sourceRun);
        using var original = source == folder ? await PlanEditorNativeReview.CreateAsync(services, Path.Combine(source, "original-account-money"), ct)
            : await PlanEditorNativeReview.ContinueAccountMoneyAsync(services, Path.Combine(source, "original-account-money"), ct);
        if (source == folder) await original.PrepareAccountMoneyAsync(ct);
        var observedRun = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_OBSERVATION_RUN");
        var observed = string.IsNullOrEmpty(observedRun) ? source : Path.Combine(Path.GetDirectoryName(folder)!, observedRun);
        bool Completed(string stage) => File.Exists(Path.Combine(source, stage, "observation.json")) || File.Exists(Path.Combine(observed, stage, "observation.json"));
        static bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is not ScrollView { Handler.PlatformView: Microsoft.UI.Xaml.Controls.ScrollViewer native }) continue;
                if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement field) throw new IOException("Original monetary field not realized.");
                var position = field.TransformToVisual(native).TransformPoint(new Windows.Foundation.Point());
                var wanted = Math.Clamp(native.VerticalOffset + position.Y, 0, native.ScrollableHeight);
                if (native.ScrollableHeight > 0 && Math.Abs(wanted - native.VerticalOffset) > 1
                    && FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Scroll) is IScrollProvider scroll)
                    scroll.SetScrollPercent(-1, wanted * 100 / native.ScrollableHeight);
                await Task.Delay(35); return;
            }
        }
        async Task Invoke(Page page, System.Windows.Input.ICommand command, bool? consent)
        {
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(item => Visible(item) && ReferenceEquals(item.Command, command));
            await Scroll(button); await Task.Delay(100);
            if (!Visible(button) || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original transaction native Save unavailable.");
            invoke.Invoke(); await Task.Yield();
            if (consent is { } answer)
            {
                if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                    throw new IOException("Original account confirmation root required.");
                Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
                await PlanEditorNativeReview.WaitAsync(() =>
                {
                    dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                        .Select(popup => popup.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault();
                    return dialog is not null;
                }, ct);
                var buttonAnswer = FindNativeButton(dialog!, services.GetRequiredService<Translator>()[answer ? "Reconcile_Adjust" : "Common_Cancel"])
                    ?? throw new IOException("Original account monetary answer required.");
                if (FrameworkElementAutomationPeer.CreatePeerForElement(buttonAnswer)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider reply)
                    throw new IOException("Original account confirmation Invoke required.");
                reply.Invoke();
            }
        }
        async Task Observe(Page page, string stage)
        {
            if (source != folder && Completed(stage)) return;
            var reads = new List<object>(); await Task.Delay(150);
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.AccountMoneyCaptions(page).Contains(text)))
            { await Scroll(label); CheckCaption(label); reads.Add(new { label.Text, label.Width, label.Height }); }
            foreach (var input in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.AccountMoneyRaw(page).Contains(text)))
            {
                await Scroll(input);
                if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox native || native.Text != input.Text)
                    throw new IOException("Native transaction raw input differs.");
                reads.Add(new { input.Text, native.ActualWidth, native.ActualHeight });
            }
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "money.png"));
            var observation = new { Stage = stage, NativeReadings = reads, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(output, "observation.json"), JsonSerializer.Serialize(observation));
            original.Observations.Add(observation); captures.Add(new { Language = stage, File = "money.png" });
        }
        try
        {

            if (currencyOnly)
            {
                await original.AccountCurrencyOwnershipAsync(Invoke, Observe, ct);
            }
            else if (defaultsOnly)
            {
                app.Windows[0].Width = 412; app.Windows[0].Height = 820;
                await original.AccountDefaultOwnershipAsync("file", Invoke, Observe, ct);
                await original.AccountDefaultOwnershipAsync("departure", Invoke, Observe, ct);
            }
            else foreach (var name in new[] { "en", "fa", "de" }) foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark }) foreach (var width in new[] { 360, 412, 1280 })
            {
                var prefix = name + "-" + value + "-" + width;
                if (source != folder && new[] { "account-original", "account-review", "debt-original", "debt-review", "observed-original", "observed-review" }.All(stage => Completed(prefix + "-" + stage))) continue;
                await SetStoreThemeAsync(app, theme, value); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await original.AccountMoneyContextAsync(name + "-" + value + "-" + width, Invoke, Observe, ct, source == folder ? null : Completed);
            }
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Original base installation changed.");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
                ProcessId = Environment.ProcessId, Captures = captures, OriginalBaseStoreUnchanged = true, NativeAccountSaves = currencyOnly ? 0 : defaultsOnly ? 2 : 1, NativeAdjustments = defaultsOnly || currencyOnly ? 0 : 1, NoAcceptedCommandReplayed = true, Observations = original.Observations }));
        }
        finally { await SetStoreThemeAsync(app, theme, choice); await SetLanguageAsync(app, local, language.CultureName); }
    }
}
#endif
