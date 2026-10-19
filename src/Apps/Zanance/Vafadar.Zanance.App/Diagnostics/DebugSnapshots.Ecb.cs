#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    private static async Task ReviewEcbAsync(App app, IServiceProvider services, string folder)
    {
        var captures = new List<object>();
        var local = services.GetRequiredService<Vafadar.Localization.ILocalizationService>();
        var result = await EcbReferenceReview.RunAsync(app, services, folder, [360, 412, 1280],
            language => SetLanguageAsync(app, local, language), async (page, context) =>
            {
                await Task.Delay(100); var scroll = VisualDescendants(page).OfType<ScrollView>().Single();
                var text = services.GetRequiredService<Vafadar.Localization.Translator>();
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(value => Visible(value) && value.Text is { } caption &&
                    (caption.Contains(text["Market_EcbFreeOriginal"], StringComparison.Ordinal) || caption == text["Market_EcbOpenSource"])))
                {
                    await ScrollToViewIfNeededAsync(scroll, label); CheckCaption(label, ProfileNativeReads);
                    if (label.Handler?.PlatformView is not TextBlock { IsTextTrimmed: false }) throw new IOException("Original ECB disclosure is clipped.");
                }
                var source = VisualDescendants(page).OfType<Button>().Single(button => SemanticProperties.GetDescription(button) == text["Market_EcbOpenSource"]);
                if (source.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { ActualWidth: >= 44, ActualHeight: >= 44 }) throw new IOException("Original source link target unavailable.");
                await scroll.ScrollToAsync(0, 0, false);
                var language = context.Split('-')[0]; var name = context + ".png"; var destination = Path.Combine(folder, language); Directory.CreateDirectory(destination);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, name)); captures.Add(new { Language = language, File = name });
            }, async button =>
            {
                Element parent = button; while (parent is not ContentPage) parent = parent.Parent ?? throw new IOException();
                await ScrollToViewIfNeededAsync(VisualDescendants(parent).OfType<ScrollView>().Single(), button);
                InvokeChoiceReview(button, ProfileNativeReads);
            }, CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(folder, "ecb-observations.json"), JsonSerializer.Serialize(result));
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, Captures = captures, FinancialRowsUnchangedAfterSeeding = true, SourceAndPurchaseDisclosures = true }));
    }
}
#endif
