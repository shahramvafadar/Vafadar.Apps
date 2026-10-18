#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks real translated Flex group captions in the original template without saving a budget or changing OS scale.</summary>
    private static async Task ReviewBudgetTitlesAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var before = JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Budgets = await store.GetBudgetsAsync() });
        var local = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            await Shell.Current.GoToAsync("//home");
            app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            await Shell.Current.GoToAsync(AppShell.BudgetRoute);
            await WaitForStorePageAsync(app);
            var page = (BudgetPage)Shell.Current.CurrentPage;
            var model = (BudgetViewModel)page.BindingContext;
            // Explicit presentation fixture: original translated labels, no saved budget-method or financial changes.
            model.IsFlex = true;
            model.FlexLines.Clear();
            foreach (var key in new[] { "Flex_Fixed", "Flex_NonMonthly" })
                model.FlexLines.Add(new FlexLine(translator[key], "Fictitious presentation only", Symbol.CalendarClock,
                    Palette.PlanText, Palette.PlanBackground, Palette.PlanLine));
            await Task.Delay(300);
            var destination = Path.Combine(folder, language + "-" + theme + "-" + width);
            Directory.CreateDirectory(destination);
            var scroll = VisualDescendants(page).OfType<ScrollView>().First(view => view.Orientation == ScrollOrientation.Vertical);
            var observed = new List<object>();
            var failures = 0;
            foreach (var line in model.FlexLines)
            {
                var label = VisualDescendants(page).OfType<Label>().Single(view => view.Text == line.Title);
                await scroll.ScrollToAsync(label, ScrollToPosition.Center, animated: false);
                await Task.Delay(100);
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
                    throw new IOException("Original native Flex caption unavailable.");
                observed.Add(new { label.Text, native.IsTextTrimmed, native.IsTextScaleFactorEnabled, native.FontSize,
                    native.ActualWidth, native.ActualHeight });
                if (native.IsTextTrimmed) failures++;
                if (!native.IsTextTrimmed) CheckCaption(label);
            }
            await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, "budget.png"));
            File.WriteAllText(Path.Combine(destination, "observed.json"), JsonSerializer.Serialize(observed));
            if (failures != 0) throw new IOException("Original translated Flex group captions are truncated.");
            captures.Add(new { Language = language + "-" + theme + "-" + width, File = "budget.png" });
        }
        var after = JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Budgets = await store.GetBudgetsAsync() });
        if (before != after) throw new IOException("Caption-only review changed original financial inputs.");
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        {
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = true, BudgetMethodNeverSaved = true, WindowsLayoutStressOnly = true,
        }));
    }
}
#endif
