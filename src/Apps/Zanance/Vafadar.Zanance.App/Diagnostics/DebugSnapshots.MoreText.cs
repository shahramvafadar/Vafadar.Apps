#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls.Primitives;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.More;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Measures the original read-only menu at native scale without invoking financial commands.</summary>
    private static async Task ReviewMoreTextAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.moretextreview"
            || !Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                "artifacts", "more-text-d335")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Independent owned menu review required.");
        var local = services.GetRequiredService<ILocalizationService>();
        var contexts = 0;
        var captions = 0;
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            var destination = Path.Combine(folder, language + "-" + theme + "-" + width);
            if (Directory.Exists(destination)) throw new IOException("Preserve the original menu observation.");
            Directory.CreateDirectory(destination);
            app.Windows[0].Width = width;
            await SetLanguageAsync(app, local, language);
            services.GetRequiredService<ThemeService>().Set(theme);
            var model = new MoreViewModel(services.GetRequiredService<Translator>());
            var page = new MorePage(model).WithFlowDirection(local);
            var root = app.Windows[0].Page ?? throw new IOException("Independent root unavailable.");
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
            try
            {
                await Task.Delay(500);
                var rows = VisualDescendants(page).OfType<Grid>().Where(row => row.BindingContext is MoreItem
                    && row.Children.OfType<Button>().Any()).ToArray();
                if (rows.Length != model.Groups.Sum(group => group.Items.Count))
                    throw new IOException("Every original menu row must be realized.");
                var observations = new List<object>();
                var failures = 0;
                await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, "menu-start.png"));
                foreach (var row in rows)
                {
                    if (page.Content is not ScrollView scroll) throw new IOException("Original menu viewport unavailable.");
                    await scroll.ScrollToAsync(row, ScrollToPosition.Start, animated: false);
                    await Task.Delay(80);
                    var item = (MoreItem)row.BindingContext;
                    var labels = row.Children.OfType<VerticalStackLayout>().Single().Children.OfType<Label>().ToArray();
                    var action = row.Children.OfType<Button>().Single();
                    var wired = ReferenceEquals(action.Command, model.OpenCommand) && ReferenceEquals(action.CommandParameter, item);
                    var spoken = action.Handler?.PlatformView is Microsoft.UI.Xaml.DependencyObject native
                        ? Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) : null;
                    var hint = SemanticProperties.GetHint(action);
                    if (!wired || action.Width < 44 || action.Height < 44 || spoken != item.Title || hint != item.Subtitle) failures++;
                    foreach (var label in labels)
                    {
                        if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
                            throw new IOException("Original native menu caption unavailable.");
                        text.UpdateLayout();
                        var slot = LayoutInformation.GetLayoutSlot(text);
                        var glyphs = new List<Windows.Foundation.Rect>();
                        for (var offset = 0; offset <= text.ContentEnd.Offset - text.ContentStart.Offset; offset++)
                        {
                            var pointer = text.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                            if (pointer is not null) glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward));
                        }
                        var complete = !text.IsTextTrimmed && text.IsTextScaleFactorEnabled && glyphs.Count > 0
                            && glyphs.Min(rect => rect.Left) >= -1 && glyphs.Max(rect => rect.Right) <= slot.Width + 1
                            && glyphs.Max(rect => rect.Bottom) <= slot.Height + 1;
                        if (!complete) failures++;
                        observations.Add(new { item.Route, label.Text, text.FontSize, text.IsTextTrimmed,
                            text.IsTextScaleFactorEnabled, text.ActualWidth, text.ActualHeight, slot.Width, slot.Height,
                            Complete = complete, Wired = wired, TargetWidth = action.Width, TargetHeight = action.Height,
                            Spoken = spoken, Hint = hint });
                        captions++;
                    }
                }
                await File.WriteAllTextAsync(Path.Combine(destination, "observed.json"), JsonSerializer.Serialize(observations));
                await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, "menu.png"));
                if (failures != 0) throw new IOException("Original menu captions or targets are incomplete: " + failures);
                contexts++;
            }
            finally { await root.Navigation.PopModalAsync(false); }
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            { Contexts = contexts, Captions = captions, FinancialCommandsInvoked = false, SystemSettingsChanged = false }));
    }
}
#endif
