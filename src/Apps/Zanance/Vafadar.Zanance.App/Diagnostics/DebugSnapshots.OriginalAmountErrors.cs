#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies only changed original-purchase amount error examples on the independent desktop.</summary>
    private static async Task ReviewOriginalAmountErrorsWindowsAsync(App app, IServiceProvider services, string folder)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(20)); var ct = timeout.Token;
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var oldLanguage = local.CurrentLanguage.CultureName; var oldTheme = themes.Choice; var window = app.Windows[0];
        var oldWidth = window.Width; var captures = new List<object>();
        try
        {
            var result = await OriginalAmountErrorCaptionReview.RunAsync(services, folder, [360, 412, 1280], async (language, theme, width) =>
            {
                await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, local, language);
                window.Width = width; await Task.Delay(180, ct);
            }, async button =>
            {
                for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                    if (parent is ScrollView viewport) { await ScrollToViewIfNeededAsync(viewport, button); break; }
                // Invoke only the actual original Save; its deliberately invalid purchase amount must be refused.
                await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 }, ct);
                InvokeChoiceReview(button);
            }, async (page, key, labels) =>
            {
                var source = Environment.GetEnvironmentVariable("VAFADAR_ORIGINAL_AMOUNT_ERROR_SOURCE");
                if (key != Environment.GetEnvironmentVariable("VAFADAR_ORIGINAL_AMOUNT_ERROR_RECAPTURE")
                    && !string.IsNullOrEmpty(source) && File.Exists(Path.Combine(source, key + ".png")))
                {
                    File.Copy(Path.Combine(source, key + ".png"), Path.Combine(folder, key + ".png"));
                    captures.Add(new { Language = ".", File = key + ".png", RetainedSource = source }); return;
                }
                foreach (var label in labels)
                {
                    for (Element? parent = label.Parent; parent is not null; parent = parent.Parent)
                        if (parent is ScrollView viewport)
                        {
                            // Error publication can grow the scroll extent after the first scroll command.
                            // Require the actual native error rectangle inside that viewport before capture.
                            var visible = false;
                            for (var attempt = 0; attempt < 10 && !visible; attempt++)
                            {
                                await ScrollToViewIfNeededAsync(viewport, label);
                                if (viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native
                                    || label.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement element)
                                    throw new IOException("Original native error viewport missing.");
                                var position = element.TransformToVisual(native).TransformPoint(new Windows.Foundation.Point(0, 0));
                                visible = position.Y >= -1 && position.Y + element.ActualHeight <= native.ActualHeight + 1;
                                if (!visible) await Task.Delay(150, ct);
                            }
                            if (!visible) throw new IOException("Original error remains outside its native viewport.");
                            break;
                        }
                    await PlanEditorNativeReview.WaitAsync(() => label.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement { ActualHeight: > 0, ActualWidth: > 0 }, ct);
                    CheckCaption(label);
                }
                await CaptureWindowAsync(window, Path.Combine(folder, key + ".png")); captures.Add(new { Language = ".", File = key + ".png" });
            }, ct);
            // A continuation reuses completed native images and their original per-context receipts;
            // opening a fresh host must never replay an already qualified editor to repair capture bookkeeping.
            var retained = Environment.GetEnvironmentVariable("VAFADAR_ORIGINAL_AMOUNT_ERROR_SOURCE");
            if (!string.IsNullOrEmpty(retained))
            {
                foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
                foreach (var width in new[] { 360, 412, 1280 }) foreach (var currency in new[] { "USD", "JPY", "KWD" })
                {
                    var key = width + "-" + language + "-" + theme + "-" + currency;
                    if (File.Exists(Path.Combine(folder, key + ".png"))) continue;
                    var receiptPath = Path.Combine(retained, key + "-complete.json");
                    if (!File.Exists(receiptPath)) throw new IOException("Original completed error observation missing.");
                    using var original = JsonDocument.Parse(await File.ReadAllTextAsync(receiptPath, ct));
                    var imageFolder = original.RootElement.TryGetProperty("ObservationDirectory", out var location)
                        ? location.GetString() ?? throw new IOException("Original image directory missing.") : retained;
                    File.Copy(Path.Combine(imageFolder, key + ".png"), Path.Combine(folder, key + ".png"));
                    captures.Add(new { Language = language, File = key + ".png", RetainedSource = imageFolder });
                }
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Scope = "original-amount-errors", Captures = captures,
                OriginalBaseStoreUnchanged = true, FinancialSaves = 0, Evidence = result }), ct);
        }
        catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "original-failure.txt"), error.ToString(), CancellationToken.None); throw; }
        finally
        {
            try { window.Width = oldWidth; await SetStoreThemeAsync(app, themes, oldTheme); await SetLanguageAsync(app, local, oldLanguage); }
            catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "cleanup-failure.txt"), error.ToString(), CancellationToken.None); }
        }
    }
}
#endif
