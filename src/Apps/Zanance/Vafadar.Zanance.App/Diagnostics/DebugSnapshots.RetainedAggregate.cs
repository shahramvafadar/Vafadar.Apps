#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Maui.Controls;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks retained aggregate fields in actual Windows controls at the affected layout widths.</summary>
    private static async Task ReviewRetainedAggregateWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var captures = new List<object>(); var local = services.GetRequiredService<ILocalizationService>();
        var text = services.GetRequiredService<Translator>();
        var result = await RetainedAggregateReview.RunAsync(app, services, folder, [360, 412, 1280],
            language => SetLanguageAsync(app, local, language), async (page, vm, context) =>
            {
                await Task.Delay(150); var scroll = VisualDescendants(page).OfType<ScrollView>().First();
                var toggle = VisualDescendants(page).OfType<Switch>().Single(value => SemanticProperties.GetDescription(value) == text["Entry_Aggregated"]);
                await ScrollToViewIfNeededAsync(scroll, toggle); await Task.Delay(100);
                for (var wait = 0; wait < 30 && (!Visible(toggle) || toggle.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ToggleSwitch { ActualWidth: > 0, ActualHeight: > 0 }); wait++)
                    await Task.Delay(50);
                if (!Visible(toggle) || !toggle.IsToggled || toggle.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ToggleSwitch { ActualWidth: > 0, ActualHeight: > 0 })
                    throw new IOException("Original retained aggregate native observation incomplete: " + JsonSerializer.Serialize(new
                    { Context = context, vm.IsAggregated, vm.CanAggregate, vm.ShowDetails, toggle.IsVisible, toggle.IsToggled,
                        Native = toggle.Handler?.PlatformView?.GetType().FullName, toggle.Width, toggle.Height }));
                foreach (var key in new[] { "Entry_Aggregated", "Entry_AggregatedFrom", "Entry_AggregatedTo" })
                {
                    var label = VisualDescendants(page).OfType<Label>().Single(value => value.Text == text[key]);
                    await ScrollToViewIfNeededAsync(scroll, label); CheckCaption(label, ProfileNativeReads);
                }
                var dates = VisualDescendants(page).OfType<DateField>().Where(value => value.AccessibilityLabel == text["Entry_AggregatedFrom"] || value.AccessibilityLabel == text["Entry_AggregatedTo"]).ToArray();
                if (dates.Length != 2 || dates.Any(value => !Visible(value) || value.Handler?.PlatformView is null)) throw new IOException("Original retained date fields unavailable.");
                var name = context + ".png"; var language = context.Split('-')[0]; var output = Path.Combine(folder, language); Directory.CreateDirectory(output);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, name)); captures.Add(new { Language = language, File = name });
            }, CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(folder, "aggregate-observations.json"), JsonSerializer.Serialize(result));
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, Captures = captures, FinancialRowsUnchangedAfterSeeding = true }));
    }
}
#endif
