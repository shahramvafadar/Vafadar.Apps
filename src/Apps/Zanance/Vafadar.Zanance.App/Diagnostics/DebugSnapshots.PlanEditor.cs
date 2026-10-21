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
        var retained = string.IsNullOrEmpty(observationRun) ? source : Path.Combine(Path.GetDirectoryName(folder)!, observationRun);
        bool Completed(string stage) => File.Exists(Path.Combine(source, stage, "observation.json")) || File.Exists(Path.Combine(retained, stage, "observation.json"));
        using var original = source == folder
            ? await PlanEditorNativeReview.CreateAsync(services, Path.Combine(source, "original-plan-form"), CancellationToken.None)
            : await PlanEditorNativeReview.ContinueAsync(services, Path.Combine(source, "original-plan-form"), CancellationToken.None);
        bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        async Task Invoke(Page page, System.Windows.Input.ICommand command)
        {
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command));
            for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(button, ScrollToPosition.MakeVisible, false); break; }
            await Task.Delay(100);
            if (!Visible(button) || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original plan native action unavailable.");
            invoke.Invoke(); await Task.Yield();
        }
        async Task Observe(Page page, string stage)
        {
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
                    await viewport.ScrollToAsync(input, ScrollToPosition.MakeVisible, false); await Task.Delay(25);
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
                if (captionsOnly)
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
                await viewport.ScrollToAsync(0, 0, false);
            }
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "plan.png"));
            var observation = new { Stage = stage, NativeReadings = readings, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(output, "observation.json"), JsonSerializer.Serialize(observation));
            original.Observations.Add(observation); captures.Add(new { Language = stage, File = "plan.png" });
        }
        try
        {
            if (source == folder) await original.RunActionsAsync(Invoke, Observe, CancellationToken.None);
            if (archivedOnly) await original.ArchivedReferenceAsync(Observe, CancellationToken.None);
            else foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetStoreThemeAsync(app, theme, value); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var context = name + "-" + value + "-" + width;
                if (captionsOnly) { await original.CaptionsAsync(context, Observe, CancellationToken.None); continue; }
                if (Completed(context + "-missing")) continue;
                await original.ContextAsync(context, Invoke, Observe, CancellationToken.None,
                    resumeReady: Completed(context + "-ready"), resumeReviewed: Completed(context + "-work-reviewed"));
            }
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None))
                throw new IOException("Original base installation rows changed.");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            {
                Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
                OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true,
                NativeAccountSaves = 1, NativePlanSaves = 2, NativeLedgerPosts = 0, NoAcceptedCommandReplayed = true,
                Observations = original.Observations,
            }));
        }
        finally
        {
            await original.LeaveAsync(); await SetStoreThemeAsync(app, theme, choice);
            await SetLanguageAsync(app, local, language.CultureName);
        }
    }
}
#endif
