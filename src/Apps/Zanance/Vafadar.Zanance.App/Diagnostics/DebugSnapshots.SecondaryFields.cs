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
    private static async Task ReviewSecondaryFieldsWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var ct = CancellationToken.None; var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var captures = new List<object>();
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_SOURCE_RUN");
        var source = string.IsNullOrEmpty(sourceRun) ? folder : Path.Combine(Path.GetDirectoryName(folder)!, sourceRun);
        using var original = source == folder ? await PlanEditorNativeReview.CreateAsync(services, Path.Combine(source, "original-secondary-fields"), ct)
            : await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, Path.Combine(source, "original-secondary-fields"), ct);
        if (source == folder) await original.PrepareSecondaryFieldsAsync(ct);
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
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondaryFieldsCaptions(page).Contains(text)))
            {
                await Scroll(label);
                bool Ready() => label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock
                    { IsTextTrimmed: false, IsTextScaleFactorEnabled: true, ActualWidth: > 0, ActualHeight: > 0 } value
                    && Vafadar.Core.Text.Digits.ToAscii(value.Text ?? string.Empty) == Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty);
                // A newly visible translated note can precede its native layout. Retain the original strict check.
                var until = DateTime.UtcNow.AddSeconds(15);
                while (!Ready() && DateTime.UtcNow < until) await Task.Delay(100);
                if (!Ready())
                {
                    var native = label.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBlock;
                    await File.WriteAllTextAsync(Path.Combine(folder, stage + "-caption-failed.json"), JsonSerializer.Serialize(new
                    {
                        Stage = stage, label.Text, NativeText = native?.Text, Width = native?.ActualWidth, Height = native?.ActualHeight,
                        Trimmed = native?.IsTextTrimmed, Scaled = native?.IsTextScaleFactorEnabled, label.IsLoaded,
                        ProcessId = Environment.ProcessId,
                    }));
                    throw new IOException("Original secondary monetary caption did not become complete after native realization.");
                }
                CheckCaption(label); reads.Add(new { label.Text, label.Width, label.Height });
            }
            foreach (var input in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondaryFieldsRaw(page).Contains(text)))
            {
                await Scroll(input);
                if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox native || native.Text != input.Text)
                    throw new IOException("Native original secondary raw amount differs.");
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

            foreach (var name in new[] { "en", "fa", "de" }) foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark }) foreach (var width in new[] { 360, 412, 1280 })
            {
                var prefix = name + "-" + value + "-" + width;
                if (source != folder && !original.SecondaryBillReadbackPending && new[] { "goal-original", "goal-review", "price-original", "price-review", "occurrence-original", "occurrence-review", "bill-original", "bill-review" }.All(stage => Completed(prefix + "-" + stage))) continue;
                await SetStoreThemeAsync(app, theme, value); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await original.SecondaryFieldsContextAsync(name + "-" + value + "-" + width, Invoke, Observe, ct, source == folder ? null : Completed);
            }
            await original.VerifySecondaryFieldsAsync(ct);
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Original base installation changed.");

            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
                ProcessId = Environment.ProcessId, Captures = captures, OriginalBaseStoreUnchanged = true, NativeGoalAllocations = 1, NativeAssumedPriceSaves = 1, NativeOccurrenceOverrides = 1, NativePartialPayments = 1, NativeBillSaves = 1, NativeLedgerEntries = 3, NativeNewLedgerEntries = 2, NoAcceptedCommandReplayed = true, Observations = original.Observations }));
        }
        finally { await SetStoreThemeAsync(app, theme, choice); await SetLanguageAsync(app, local, language.CultureName); }
    }
}
#endif
