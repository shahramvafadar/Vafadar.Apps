#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies only original Home refresh supersession in six isolated native contexts.</summary>
    private static async Task ReviewHomeRefreshAsync(App app, IServiceProvider services, string folder, bool referencesOnly = false)
    {
        if (!AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal))
            throw new IOException("Independent original Home refresh identity required.");
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await BaseRows(); var cases = new List<object>(); var captures = new List<object>();
        foreach (var language in (referencesOnly ? new[] { "en" } : new[] { "en", "fa", "de" }))
        foreach (var choice in (referencesOnly ? new[] { ThemeChoice.Light } : new[] { ThemeChoice.Light, ThemeChoice.Dark }))
        {
            await SetStoreThemeAsync(app, theme, choice); await SetLanguageAsync(app, local, language);
            var context = language + "-" + choice; var output = Path.Combine(folder, context); Directory.CreateDirectory(output);
            var result = await HomeRefreshNativeCase.RunAsync(services, Path.Combine(output, "home-refresh.db"),
                async page => { page.FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; await Shell.Current.Navigation.PushAsync(page, false); },
                async (page, home) =>
                {
                    await Task.Delay(150);
                    var viewport = page.FindByName<ScrollView>("ContentViewport");
                    if (!viewport.IsVisible || viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement { Visibility: Microsoft.UI.Xaml.Visibility.Visible })
                        throw new IOException("Original native Home is still covered after current source publication.");
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "home.png"));
                }, async page =>
                {
                    if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Only the original owned Home may return.");
                    await Shell.Current.Navigation.PopAsync(false);
                }, CancellationToken.None, addAccount: referencesOnly);
            cases.Add(new { Context = context, Result = result }); captures.Add(new { Language = context, File = "home.png" });
        }
        if (before != await BaseRows()) throw new IOException("Native Home refresh changed the original base store.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures, Cases = cases, FinancialRowsUnchangedAfterSeeding = true }));
    }
}
#endif
