#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies selected changed budget wording or retained-scope notes on the independent desktop.</summary>
    private static async Task ReviewBudgetPeriodCaptionsWindowsAsync(App app, IServiceProvider services, string folder, bool scopeOnly = false)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10)); var ct = timeout.Token;
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var oldLanguage = local.CurrentLanguage.CultureName; var oldTheme = themes.Choice; var window = app.Windows[0];
        var oldWidth = window.Width; var captures = new List<object>();
        try
        {
            var result = await BudgetPeriodCaptionReview.RunAsync(services, folder, [360, 412, 1280], async (language, theme, width) =>
            {
                await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, local, language);
                window.Width = width; await Task.Delay(180, ct);
            }, async button =>
            {
                for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                    if (parent is ScrollView viewport) { await ScrollToViewIfNeededAsync(viewport, button); break; }
                // The existing MoveButton is explicitly 40px; this caption review does not qualify touch-target sizing.
                await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 40, ActualHeight: >= 40 }, ct);
                InvokeChoiceReview(button);
            }, async (page, key, labels) =>
            {
                var source = Environment.GetEnvironmentVariable("VAFADAR_BUDGET_PERIOD_SOURCE");
                if (!string.IsNullOrEmpty(source) && File.Exists(Path.Combine(source, key + ".png")))
                {
                    File.Copy(Path.Combine(source, key + ".png"), Path.Combine(folder, key + ".png"));
                    captures.Add(new { Language = ".", File = key + ".png", RetainedSource = source }); return;
                }
                foreach (var label in labels)
                {
                    for (Element? parent = label.Parent; parent is not null; parent = parent.Parent)
                        if (parent is ScrollView viewport) { await ScrollToViewIfNeededAsync(viewport, label); break; }
                    await PlanEditorNativeReview.WaitAsync(() => label.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement { ActualHeight: > 0, ActualWidth: > 0 }, ct);
                    CheckCaption(label);
                }
                await CaptureWindowAsync(window, Path.Combine(folder, key + ".png")); captures.Add(new { Language = ".", File = key + ".png" });
            }, ct, scopeOnly);
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Scope = scopeOnly ? "budget-scope-captions" : "budget-period-captions", Captures = captures,
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
