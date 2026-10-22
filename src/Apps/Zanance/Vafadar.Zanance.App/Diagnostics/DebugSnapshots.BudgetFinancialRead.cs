#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Captures original budget financial methods on the isolated review desktop.</summary>
    private static async Task ReviewBudgetFinancialReadWindowsAsync(App app, IServiceProvider services, string folder)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8)); var ct = timeout.Token;
        var local = services.GetRequiredService<ILocalizationService>(); var themes = services.GetRequiredService<ThemeService>();
        var oldLanguage = local.CurrentLanguage.CultureName; var oldTheme = themes.Choice;
        var captures = new List<object>();
        try
        {
            var result = await BudgetFinancialNativeReview.RunAsync(services, folder, async (language, theme) =>
            {
                await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, local, language);
            }, async (page, key) =>
            {
                var vm = (BudgetViewModel)page.BindingContext;
                var label = VisualDescendants(page).OfType<Label>().First(value => value.Text == vm.TotalLines[0].StatusText && Visible(value));
                var viewport = FindScrollView(page) ?? throw new IOException("Original financial viewport missing.");
                await ScrollToViewIfNeededAsync(viewport, label);
                await PlanEditorNativeReview.WaitAsync(() => label.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement { ActualHeight: > 0, ActualWidth: > 0 }, ct);
                CheckCaption(label);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, key + ".png"));
                captures.Add(new { Language = ".", File = key + ".png" });
            }, ct);
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            {
                Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Scope = "budget-financial-read",
                Captures = captures, OriginalBaseStoreUnchanged = true, FinancialSaves = 0, Evidence = result,
            }), ct);
        }
        catch (Exception error)
        { await File.WriteAllTextAsync(Path.Combine(folder, "original-failure.txt"), error.ToString(), CancellationToken.None); throw; }
        finally
        {
            try { await SetStoreThemeAsync(app, themes, oldTheme); await SetLanguageAsync(app, local, oldLanguage); }
            catch (Exception error) { await File.WriteAllTextAsync(Path.Combine(folder, "cleanup-failure.txt"), error.ToString(), CancellationToken.None); }
        }
    }
}
#endif
