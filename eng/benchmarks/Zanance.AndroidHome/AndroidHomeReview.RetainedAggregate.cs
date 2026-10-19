using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Diagnostics;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks only retained aggregate publication in the original Release/AOT editor, without a Save.</summary>
    private static async Task<object> ReviewRetainedAggregateAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var observations = new List<object>();
        var result = await RetainedAggregateReview.RunAsync(app, services, root, [412],
            language => SetLanguageAsync(app, local, language, ct), async (page, vm, context) =>
            {
                // The original modal owns input focus; require its native controls, not the covered Activity.
                await Task.Delay(150, ct);
                var scroll = ReimbursementViews(page).OfType<ScrollView>().First();
                var toggle = ReimbursementViews(page).OfType<Switch>().Single(value => SemanticProperties.GetDescription(value) == text["Entry_Aggregated"]);
                await scroll.ScrollToAsync(toggle, ScrollToPosition.Center, false); await Task.Delay(150, ct);
                if (!toggle.IsToggled || toggle.Handler?.PlatformView is not Android.Widget.CompoundButton { IsShown: true, Checked: true, HasWindowFocus: true })
                    throw new ReviewCheckException("Retained aggregate native toggle is hidden or unchecked.");
                foreach (var key in new[] { "Entry_Aggregated", "Entry_AggregatedFrom", "Entry_AggregatedTo" })
                {
                    var label = ReimbursementViews(page).OfType<Label>().Single(value => value.Text == text[key]);
                    await scroll.ScrollToAsync(label, ScrollToPosition.Center, false); await Task.Delay(100, ct);
                    if (label.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true } native)
                        throw new ReviewCheckException("Retained aggregate native caption unavailable.");
                    await WaitLayoutAsync(native, ct); var layout = native.Layout;
                    if (layout is null || Enumerable.Range(0, layout.LineCount).Any(index => layout.GetEllipsisCount(index) != 0))
                        throw new ReviewCheckException("Retained aggregate native caption is clipped.");
                }
                var dates = ReimbursementViews(page).OfType<DateField>().Where(value => value.AccessibilityLabel == text["Entry_AggregatedFrom"] || value.AccessibilityLabel == text["Entry_AggregatedTo"]).ToArray();
                if (dates.Length != 2 || dates.Any(value => value.Handler?.PlatformView is not Android.Views.View { IsShown: true }))
                    throw new ReviewCheckException("Retained aggregate original date controls unavailable.");
                observations.Add(new { Context = context, vm.IsAggregated, vm.CanAggregate, vm.ShowDetails, vm.AggregatedFrom, vm.AggregatedTo,
                    NativeToggleChecked = true, NativeCaptions = 3, OriginalDateControls = dates.Length });
            }, ct);
        await AwaitRootHomeAsync(ct);
        return new { Comparison = comparison, Review = result, Observations = observations, ActualConfiguration = DeviceEvidence(), SystemSettingsChanged = false };
    }
}
