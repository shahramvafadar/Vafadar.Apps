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
    /// <summary>Checks covered original goal details on their own non-input desktop without financial writes.</summary>
    private static async Task ReviewGoalDetailWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var ct = CancellationToken.None;
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_PLAN_FORM_SOURCE_RUN");
        if (string.IsNullOrEmpty(sourceRun)) throw new IOException("An exact original goal-detail source is required.");
        var source = Path.Combine(Path.GetDirectoryName(folder)!, sourceRun, "original-secondary-fields");
        using var original = await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, source, ct);
        var before = await original.AllMoneyRowsAsync(ct); var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage.CultureName; var theme = themes.Choice;
        var editFailureOnly = Environment.GetEnvironmentVariable("VAFADAR_GOAL_DETAIL_EDIT_FAILURE") == "1";
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
            if (page.BindingContext is not GoalDetailViewModel model) throw new IOException("Original detail model required.");
            if (model.IsLoading) labels.Add(page.FindByName<Label>("LoadingCaption"));
            else if (model.HasReadFailed) labels.Add(page.FindByName<Label>("ErrorCaption"));
            else
            {
                if (!model.CanInteract || model.Summary is null) throw new IOException("Complete original detail required.");
                static bool Visible(Element item)
                {
                    for (Element? parent = item; parent is not null; parent = parent.Parent)
                        if (parent is VisualElement visual && !visual.IsVisible) return false;
                    return true;
                }
                var captions = PlanEditorNativeReview.GoalDetailCaptions(model);
                labels.AddRange(PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(label => captions.Contains(label.Text ?? string.Empty)));
                if (captions.Any(caption => !labels.Any(label => label.Text == caption)))
                    throw new IOException("An original detail financial caption is absent from its visible page.");
                var raw = new[] { model.AmountText, model.AssumedPriceText }.Where(value => !string.IsNullOrEmpty(value)).ToHashSet(StringComparer.Ordinal);
                foreach (var input in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(input => raw.Contains(input.Text ?? string.Empty)))
                {
                    await Scroll(input);
                    if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox { ActualWidth: > 0, ActualHeight: > 0 } native || native.Text != input.Text)
                        throw new IOException("Original native detail draft differs.");
                }
            }
            var readings = new List<object>();
            foreach (var label in labels)
            {
                await Scroll(label);
                await PlanEditorNativeReview.WaitAsync(() => label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0 }, ct);
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { IsTextTrimmed: false } native
                    || Vafadar.Core.Text.Digits.ToAscii(native.Text) != Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty))
                    throw new IOException("Original goal-detail native caption is clipped or differs.");
                CheckCaption(label); readings.Add(new { label.Text, native.ActualWidth, native.ActualHeight });
            }
            var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "detail.png"));
            var packet = new { Stage = stage, NativeReadings = readings, ProcessId = Environment.ProcessId };
            await using (var file = new FileStream(Path.Combine(output, "observation.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await JsonSerializer.SerializeAsync(file, packet);
            captures.Add(packet);
        }
        try
        {
            if (editFailureOnly)
            {
                await SetStoreThemeAsync(app, themes, ThemeChoice.Light); await SetLanguageAsync(app, local, "en");
                app.Windows[0].Width = 412; app.Windows[0].Height = 820;
                await original.GoalDetailEditFailureContextAsync(Path.GetFileName(folder) + "-edit", Invoke, Back, Observe, ct);
            }
            else
            foreach (var name in new[] { "en", "fa", "de" }) foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark }) foreach (var width in new[] { 360, 412, 1280 }) foreach (var quantity in new[] { false, true })
            {
                await SetStoreThemeAsync(app, themes, choice); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                await original.GoalDetailContextAsync(Path.GetFileName(folder) + "-" + name + "-" + choice + "-" + width + (quantity ? "-quantity" : "-earmark"), quantity, Invoke, Back, Observe, ct);
            }
            if (before != await original.AllMoneyRowsAsync(ct) || baseline != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new IOException("Original list qualification changed source or base rows.");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            {
                Package = AppInfo.Current.PackageName, Scope = editFailureOnly ? "goal-detail-edit-failure" : "goal-detail", ProcessId = Environment.ProcessId, Captures = captures,
                OriginalBaseStoreUnchanged = true, OriginalSourceUnchanged = true, AcceptedWrites = 0, NativeBacks = editFailureOnly ? 1 : 36,
                NativeRetries = editFailureOnly ? 1 : 36, NoAcceptedCommandReplayed = true, Observations = original.Observations,
            }));
        }
        finally { await original.LeaveAsync(); await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, local, language); }
    }
}
#endif
