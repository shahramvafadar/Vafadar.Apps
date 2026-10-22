#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks covered original goal lists on their own non-input desktop without financial writes.</summary>
    private static async Task ReviewGoalListWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var ct = CancellationToken.None;
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_SOURCE_RUN");
        if (string.IsNullOrEmpty(sourceRun)) throw new IOException("An exact original goal-list source is required.");
        var source = Path.Combine(Path.GetDirectoryName(folder)!, sourceRun, "original-secondary-fields");
        using var original = await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, source, ct);
        var before = await original.AllMoneyRowsAsync(ct); var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage.CultureName; var theme = themes.Choice;
        var captures = new List<object>();
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is not ScrollView { Handler.PlatformView: Microsoft.UI.Xaml.Controls.ScrollViewer native }) continue;
                if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement field) throw new IOException("Original list target is not realized.");
                var position = field.TransformToVisual(native).TransformPoint(new Windows.Foundation.Point());
                var wanted = Math.Clamp(native.VerticalOffset + position.Y, 0, native.ScrollableHeight);
                if (native.ScrollableHeight > 0 && Math.Abs(wanted - native.VerticalOffset) > 1
                    && FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Scroll) is IScrollProvider scroll)
                    scroll.SetScrollPercent(-1, wanted * 100 / native.ScrollableHeight);
                break;
            }
            await Task.Delay(100);
        }
        async Task Invoke(Page page, VisualElement target)
        {
            await Scroll(target);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)
                || target.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement { ActualWidth: >= 44, ActualHeight: >= 44 } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original owned native list action required.");
            action.Invoke(); await Task.Yield();
        }
        async Task Back(Page page)
        {
            var caption = services.GetRequiredService<Translator>()["Common_Back"];
            var target = PlanEditorNativeReview.Views(page).OfType<ImageButton>()
                .Single(button => SemanticProperties.GetDescription(button) == caption);
            await Invoke(page, target);
        }
        async Task Observe(Page page, string stage)
        {
            var labels = new List<Label>();
            if (page.BindingContext is GoalsViewModel model)
            {
                if (model.Loading.IsLoading) labels.Add(page.FindByName<Label>("LoadingCaption"));
                else if (model.Loading.HasFailed) labels.Add(page.FindByName<Label>("ErrorCaption"));
                else
                {
                    if (!model.Loading.IsReady || model.Active.Count == 0) throw new IOException("Original complete goal list required.");
                    labels.Add(PlanEditorNativeReview.Views(page).OfType<Label>().First(label => label.Text == model.Active[0].Name));
                    foreach (var row in model.Active)
                    {
                        var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(item => ReferenceEquals(item.CommandParameter, row));
                        if (!ReferenceEquals(button.Command, model.OpenCommand) || !button.IsEnabled || !model.OpenCommand.CanExecute(row))
                            throw new IOException("Original current goal row binding is not actionable.");
                    }
                }
            }
            else if (page.BindingContext is not GoalDetailViewModel { CanInteract: true, Summary: not null })
                throw new IOException("Original completed detail state required.");
            var readings = new List<object>();
            foreach (var label in labels)
            {
                await Scroll(label);
                await PlanEditorNativeReview.WaitAsync(() => label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0 }, ct);
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { IsTextTrimmed: false } native
                    || Vafadar.Core.Text.Digits.ToAscii(native.Text) != Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty))
                    throw new IOException("Original goal-list native caption is clipped or differs.");
                CheckCaption(label); readings.Add(new { label.Text, native.ActualWidth, native.ActualHeight });
            }
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "goals.png"));
            var packet = new { Stage = stage, NativeReadings = readings, ProcessId = Environment.ProcessId };
            await using (var file = new FileStream(Path.Combine(output, "observation.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await JsonSerializer.SerializeAsync(file, packet);
            captures.Add(packet);
        }
        try
        {
            foreach (var name in new[] { "en", "fa", "de" }) foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark }) foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetStoreThemeAsync(app, themes, choice); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await original.GoalListContextAsync(Path.GetFileName(folder) + "-" + name + "-" + choice + "-" + width, Invoke, Back, Observe, ct, navigation: width == 412);
            }
            if (before != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new IOException("Original list qualification changed source or base rows.");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            {
                Package = AppInfo.Current.PackageName, Scope = "goal-list", ProcessId = Environment.ProcessId, Captures = captures,
                OriginalBaseStoreUnchanged = true, OriginalSourceUnchanged = true, AcceptedWrites = 0, NativeDetailVisits = 6,
                NativeRetries = 18, NoAcceptedCommandReplayed = true, Observations = original.Observations,
            }));
        }
        finally { await original.LeaveAsync(); await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, local, language); }
    }
}
#endif
