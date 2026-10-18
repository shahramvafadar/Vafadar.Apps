#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original optional Home category rows with explicit fictitious presentation values and no saved layout change.</summary>
    private static async Task ReviewHomeLegendsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var before = JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var local = services.GetRequiredService<ILocalizationService>();
        var captures = new List<object>();
        var languages = (Environment.GetEnvironmentVariable("VAFADAR_HOME_LEGEND_LANGUAGES") ?? "en,fa,de").Split(',');
        if (languages.Length == 0 || languages.Distinct(StringComparer.Ordinal).Count() != languages.Length
            || languages.Any(language => language is not ("en" or "fa" or "de")))
            throw new IOException("Explicit original Home review languages required.");
        foreach (var language in languages)
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            await Shell.Current.GoToAsync("//home");
            app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            await WaitForStorePageAsync(app);
            var page = (HomePage)Shell.Current.CurrentPage;
            await page.PendingPublication;
            var model = (HomeViewModel)page.BindingContext;
            var first = model.Slices.First();
            model.Slices[0] = first with
            {
                Name = "Food, household groceries and everyday essentials / Lebensmittel und Haushaltsbedarf / خوراک و نیازهای روزانه",
                AmountText = Core.Money.MoneyText.Format(long.MinValue, "EUR", local.CurrentCulture),
                PercentText = 1d.ToString("P0", local.CurrentCulture),
            };
            model.SliceTotalText = Core.Money.MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture);
            // Exercise the real attachment path for both states without saving the user's layout.
            var categoryRows = page.FindByName<VerticalStackLayout>("CategoryRows");
            page.PreviewCategorySectionForReview(false);
            if (BindableLayout.GetItemsSource(categoryRows) is not null)
                throw new IOException("Hidden Home categories retain native rows.");
            page.PreviewCategorySectionForReview(true);
            if (!ReferenceEquals(BindableLayout.GetItemsSource(categoryRows), model.Slices))
                throw new IOException("Visible Home categories lost the complete original source.");
            await Task.Delay(350);
            var row = VisualDescendants(page).OfType<Grid>().Single(value =>
                ReferenceEquals(value.BindingContext, model.Slices[0]) && value.Children.OfType<Button>().Any());
            var viewport = page.FindByName<ScrollView>("ContentViewport");
            await viewport.ScrollToAsync(row, ScrollToPosition.Start, animated: false);
            await Task.Delay(100);
            var name = row.Children.OfType<Label>().Single(label => label.Text == model.Slices[0].Name);
            if (name.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
                throw new IOException("Original native Home category name is unavailable.");
            native.UpdateLayout();
            var action = row.Children.OfType<Button>().Single();
            var wired = ReferenceEquals(action.Command, model.OpenSliceCommand) && ReferenceEquals(action.CommandParameter, model.Slices[0]);
            var destination = Path.Combine(folder, language + "-" + theme + "-" + width);
            Directory.CreateDirectory(destination);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, "home.png"));
            File.WriteAllText(Path.Combine(destination, "observed.json"), JsonSerializer.Serialize(new
            {
                name.Text, native.IsTextTrimmed, native.IsTextScaleFactorEnabled, native.FontSize,
                native.ActualWidth, native.ActualHeight, TargetWidth = action.Width, TargetHeight = action.Height,
                Wired = wired, FictitiousPresentationOnly = true, LayoutNeverSaved = true,
            }));
            if (native.IsTextTrimmed || !native.IsTextScaleFactorEnabled || action.Width < 44 || action.Height < 44 || !wired)
                throw new IOException("Original Home category identity or target is incomplete.");
            CheckCaption(name);
            CheckCaption(row.Children.OfType<Label>().Single(label => label.Text == model.Slices[0].PercentText));
            await CheckLegendAmountAsync(page, row, model.Slices[0].AmountText);
            var total = page.FindByName<AmountReadout>("CategoryTotalReadout");
            await viewport.ScrollToAsync(total, ScrollToPosition.Center, animated: false);
            await Task.Delay(100);
            await CheckHomeTotalAsync(page, total, model.SliceTotalText, destination);
            File.WriteAllText(Path.Combine(destination, "complete.json"), JsonSerializer.Serialize(new
                { CompleteCategoryAndPercent = true, BothAmountPacketsAndNativeScrollEnds = true,
                    OriginalTargetWired = true, HiddenRowsDetached = true, VisibleSourceComplete = true, LayoutNeverSaved = true }));
            captures.Add(new { Language = language + "-" + theme + "-" + width, File = "home.png" });
        }
        var after = JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        if (before != after) throw new IOException("Home legend review changed original stored inputs.");
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        {
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = true, LayoutNeverSaved = true, WindowsLayoutStressOnly = true,
        }));
    }

    /// <summary>Checks the original full total packet, native glyphs, page bounds and both actual scroll ends.</summary>
    private static async Task CheckHomeTotalAsync(HomePage page, AmountReadout amount, string? packet, string destination)
    {
        if (string.IsNullOrEmpty(packet) || amount.AmountText != packet)
            throw new IOException("Original complete Home total packet is missing.");
        var caption = VisualDescendants(amount).OfType<Label>().Single(label => label.Text == packet);
        CheckCaption(caption);
        var scroll = VisualDescendants(amount).OfType<ScrollView>().Single();
        if (scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native
            || page.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement root)
            throw new IOException("Native Home total viewport is missing.");
        var transform = native.TransformToVisual(root);
        var first = transform.TransformPoint(new(0, 0));
        var last = transform.TransformPoint(new(native.ActualWidth, 0));
        // RTL may mirror the native coordinate transform; measure both physical edges, not an assumed positive X axis.
        File.WriteAllText(Path.Combine(destination, "total-viewport.json"), JsonSerializer.Serialize(new
            { FirstX = first.X, LastX = last.X, PageWidth = root.ActualWidth, native.ActualWidth, native.ViewportWidth, native.ScrollableWidth }));
        if (Math.Min(first.X, last.X) < -1 || Math.Max(first.X, last.X) > root.ActualWidth + 1 || native.ViewportWidth <= 0)
            throw new IOException("Home total escapes its original page viewport.");
        if (native.ScrollableWidth > 1)
        {
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(native);
            if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll)
                is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider pattern)
                throw new IOException("Oversized Home total has no native scroll pattern.");
            pattern.SetScrollPercent(100, -1); await Task.Delay(50);
            if (native.HorizontalOffset < native.ScrollableWidth - 1) throw new IOException("Home total currency cannot be reached.");
            pattern.SetScrollPercent(0, -1); await Task.Delay(50);
            if (native.HorizontalOffset > 1) throw new IOException("Home total sign cannot be reached.");
        }
    }

}
#endif
