#if DEBUG && WINDOWS
using System.Collections.Specialized;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original Home amount handlers during ordinary and covered refreshes, without financial writes.</summary>
    private static async Task ReviewHomeBalanceReloadAsync(App app, IServiceProvider services, string folder)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance"
            || !File.Exists(Path.Combine(Path.GetDirectoryName(location.Path)!, "protected-review-fixture.json")))
            throw new IOException("Home review requires its independent marked installation.");
        var before = await ProtectedRowsFingerprintAsync(services);
        var localization = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<Presentation.ThemeService>();
        var observations = new List<object>(); var cases = 0; var reloads = 0;
        foreach (var theme in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, localization, language);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var shell = app.Windows[0].Page as AppShell ?? throw new IOException("Home Shell unavailable.");
            await shell.GoToAsync("//home");
            var home = shell.CurrentPage as Features.Home.HomePage ?? throw new IOException("Original Home unavailable.");
            await home.PendingPublication;
            var vm = (Features.Home.HomeViewModel)home.BindingContext;
            if (vm.Balances.Count == 0) throw new IOException("Review requires actual currency rows.");
            var rows = VisualDescendants(home).OfType<Presentation.AmountReadout>()
                .Where(value => value.BindingContext is Features.Accounts.CurrencyTotal).ToArray();
            var handlers = rows.Select(value => value.Handler ?? throw new IOException("Native amount handler missing.")).ToArray();
            var publications = 0;
            NotifyCollectionChangedEventHandler changed = (_, e) =>
            {
                if (e.Action != NotifyCollectionChangedAction.Reset || vm.Balances.Count == 0)
                    throw new IOException("Reload exposed an empty intermediate balance collection.");
                publications++;
            };
            vm.Balances.CollectionChanged += changed;
            try
            {
                // A real modal covers the original native Home, matching theme refresh of open pages.
                var cover = new ContentPage { Content = new Label { Text = "Synthetic covered Home refresh" } };
                await shell.Navigation.PushModalAsync(cover, false);
                await vm.LoadAsync(); reloads++;
                await shell.Navigation.PopModalAsync(false);
                await home.PendingPublication;
                await vm.LoadAsync(); reloads++;
                if (publications < 2) throw new IOException("Original Home did not publish both reloads.");
                for (var index = 0; index < rows.Length; index++)
                {
                    if (!ReferenceEquals(rows[index].Handler, handlers[index])
                        || rows[index].BindingContext is not Features.Accounts.CurrencyTotal total
                        || rows[index].AmountText != total.Text)
                        throw new IOException("Home reload disconnected or left a stale amount row.");
                }
                foreach (var row in rows) CheckCaption(VisualDescendants(row).OfType<Label>().First(), observations);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-home.png"));
                cases++;
            }
            finally { vm.Balances.CollectionChanged -= changed; }
            if (await ProtectedRowsFingerprintAsync(services) != before) throw new IOException("Home reload changed financial data.");
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "home-balance-reload-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases, ExplicitReloads = reloads, OriginalAmountHandlersRetained = true,
            CompleteFinancialRowsUnchanged = true, FinancialFingerprint = before, NativeObservations = observations.Count }));
    }
}
#endif
