#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Measures actual original form publication and actions on a separate non-input Windows desktop.</summary>
    private static async Task ReviewPlanEditorWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice; var captures = new List<object>();
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_SOURCE_RUN");
        var source = string.IsNullOrEmpty(sourceRun) ? folder : Path.Combine(Path.GetDirectoryName(folder)!, sourceRun);
        var observationRun = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_OBSERVATION_RUN");
        var captionsOnly = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_CAPTIONS_ONLY") == "1";
        var archivedOnly = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_ARCHIVED_ONLY") == "1";
        var moneyOnly = Environment.GetEnvironmentVariable("VAFADAR_PLAN_MONEY_REVIEW") == "1";
        var debtOnly = Environment.GetEnvironmentVariable("VAFADAR_PLAN_MONEY_DEBT_ONLY") == "1";
        var submissionOnly = Environment.GetEnvironmentVariable("VAFADAR_PLAN_MONEY_SUBMISSION_ONLY") == "1";
        var retained = string.IsNullOrEmpty(observationRun) ? source : Path.Combine(Path.GetDirectoryName(folder)!, observationRun);
        bool Completed(string stage)
        {
            foreach (var path in new[] { Path.Combine(source, stage, "observation.json"), Path.Combine(retained, stage, "observation.json") })
            {
                if (!File.Exists(path)) continue;
                if (!moneyOnly) return true;
                using var receipt = JsonDocument.Parse(File.ReadAllText(path));
                if (receipt.RootElement.GetProperty("NativeReadings").EnumerateArray().Any(row => row.TryGetProperty("Text", out var text) && text.GetString() == "1.234")) return true;
            }
            return false;
        }
        using var original = source == folder
            ? await PlanEditorNativeReview.CreateAsync(services, Path.Combine(source, "original-plan-form"), CancellationToken.None)
            : moneyOnly ? await PlanEditorNativeReview.ContinueMoneyAsync(services, Path.Combine(source, "original-plan-form"), CancellationToken.None)
            : await PlanEditorNativeReview.ContinueAsync(services, Path.Combine(source, "original-plan-form"), CancellationToken.None);
        bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        Task ScrollOriginalAsync(ScrollView viewport, VisualElement target)
        {
            if (viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native
                || target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement field)
                throw new IOException("Original native plan viewport required.");
            var position = field.TransformToVisual(native).TransformPoint(new Windows.Foundation.Point());
            var wanted = Math.Clamp(native.VerticalOffset + position.Y, 0, native.ScrollableHeight);
            // WinUI emits no ViewChanged for a redundant/clamped scroll. Use its real Scroll pattern without awaiting that absent event.
            if (native.ScrollableHeight > 0 && Math.Abs(wanted - native.VerticalOffset) > 1
                && FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Scroll) is IScrollProvider scroll)
                scroll.SetScrollPercent(-1, wanted * 100 / native.ScrollableHeight);
            return Task.Delay(30);
        }
        async Task Invoke(Page page, System.Windows.Input.ICommand command)
        {
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command));
            for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await ScrollOriginalAsync(viewport, button); break; }
            await Task.Delay(100);
            if (!Visible(button) || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original plan native action unavailable.");
            invoke.Invoke(); await Task.Yield();
        }
        async Task Observe(Page page, string stage)
        {
            if (moneyOnly && Completed(stage)) return;
            await Task.Delay(150);
            var readings = new List<object>();
            if (page.BindingContext is PlanEditorViewModel model)
            {
                if (page.FindByName<ScrollView>("ValidationViewport").IsVisible != model.HasFormContent)
                    throw new IOException("Original complete plan body cover differs.");
                if (!model.Loading.IsReady && (model.FormVisible || model.CanSave)) throw new IOException("Incomplete original plan remained writable.");
                var viewport = page.FindByName<ScrollView>("ValidationViewport");
                foreach (var input in PlanEditorNativeReview.Views(page).OfType<InputView>().Where(Visible))
                {
                    await ScrollOriginalAsync(viewport, input); await Task.Delay(25);
                    if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox native || native.Text.Replace("\r\n", "\n").Replace('\r', '\n') != (input.Text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n'))
                    {
                        await File.WriteAllTextAsync(Path.Combine(folder, "input-failure.json"), JsonSerializer.Serialize(new
                        { Stage = stage, Type = input.GetType().Name, input.Text, NativeType = input.Handler?.PlatformView?.GetType().Name,
                            NativeText = (input.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBox)?.Text }));
                        throw new IOException("Original native plan input differs.");
                    }
                    readings.Add(new { input.Text, native.ActualWidth, native.ActualHeight });
                }
                foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(l => l.Text == model.LoadError || l.Text == model.SaveError || l.Text == model.WorkText))
                    CheckCaption(label);
                if (captionsOnly || moneyOnly)
                {
                    if (model.Loading.IsLoading && !PlanEditorNativeReview.Views(page).OfType<Label>().Any(l => Visible(l) && l.Text == original.Translator["Plan_FormLoading"]))
                        throw new IOException("Original plan loading caption was not translated.");
                    foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible)) CheckCaption(label);
                    foreach (var button in PlanEditorNativeReview.Views(page).OfType<Button>().Where(Visible))
                    {
                        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { ActualWidth: >= 44, ActualHeight: >= 44 } native)
                            throw new IOException("Original plan caption action target unavailable.");
                        if (ReferenceEquals(button.Command, model.ReloadFormCommand)
                            && (FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetName() != original.Translator["Editor_RetryRead"]))
                            throw new IOException("Original plan Retry caption was not translated.");
                    }
                }
                if (viewport.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer { ScrollableHeight: > 0, VerticalOffset: > 0 } nativeViewport
                    && FrameworkElementAutomationPeer.CreatePeerForElement(nativeViewport)?.GetPattern(PatternInterface.Scroll) is IScrollProvider originalScroll)
                    originalScroll.SetScrollPercent(-1, 0);
            }
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "plan.png"));
            var observation = new { Stage = stage, NativeReadings = readings, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(output, "observation.json"), JsonSerializer.Serialize(observation));
            original.Observations.Add(observation); captures.Add(new { Language = stage, File = "plan.png" });
        }
        try
        {
            if (moneyOnly && source == folder) await original.PrepareMoneyAsync(CancellationToken.None);
            else if (source == folder) await original.RunActionsAsync(Invoke, Observe, CancellationToken.None);
            var moneySaved = File.Exists(Path.Combine(source, "original-plan-form", "money-save-verified.json"));
            if (archivedOnly) await original.ArchivedReferenceAsync(Observe, CancellationToken.None);
            else if (!debtOnly && !submissionOnly) foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetStoreThemeAsync(app, theme, value); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var context = name + "-" + value + "-" + width;
                if (moneyOnly) { await original.MoneyContextAsync(context, Invoke, Observe, !moneySaved, CancellationToken.None); moneySaved = true; continue; }
                if (captionsOnly) { await original.CaptionsAsync(context, Observe, CancellationToken.None); continue; }
                if (Completed(context + "-missing")) continue;
                await original.ContextAsync(context, Invoke, Observe, CancellationToken.None,
                    resumeReady: Completed(context + "-ready"), resumeReviewed: Completed(context + "-work-reviewed"));
            }
            if (submissionOnly)
            {
                await SetStoreThemeAsync(app, theme, ThemeChoice.Light); await SetLanguageAsync(app, local, "en");
                app.Windows[0].Width = 412; app.Windows[0].Height = 820;
                await original.SubmissionMoneyAsync(Invoke, Observe, CancellationToken.None);
            }
            if (debtOnly)
            {
                await SetStoreThemeAsync(app, theme, ThemeChoice.Light); await SetLanguageAsync(app, local, "en");
                app.Windows[0].Width = 412; app.Windows[0].Height = 820;
                using var debt = await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "money-debt-fixture"), CancellationToken.None);
                await debt.PrepareMoneyAsync(CancellationToken.None); await debt.DebtMoneyAsync(Observe, CancellationToken.None);
            }
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None))
                throw new IOException("Original base installation rows changed.");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            {
                Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
                OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true,
                NativeAccountSaves = moneyOnly ? 0 : 1, NativePlanSaves = debtOnly || submissionOnly ? 0 : moneyOnly ? 1 : 2, NativeLedgerPosts = 0, NoAcceptedCommandReplayed = true,
                Observations = original.Observations,
            }));
        }
        finally
        {
            // Monetary cases already retired their fixture page. Its host Home uses separate services and is not part of this observation.
            if (!moneyOnly) await original.LeaveAsync();
            await SetStoreThemeAsync(app, theme, choice);
            await SetLanguageAsync(app, local, language.CultureName);
        }
    }
}
#endif
