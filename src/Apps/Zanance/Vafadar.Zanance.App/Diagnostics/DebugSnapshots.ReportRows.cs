#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls.Primitives;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Measures original report legend rows with explicit fictitious presentation stress, without financial commands.</summary>
    private static async Task ReviewReportRowsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var before = JsonSerializer.Serialize(await store.GetEntriesAsync());
        var local = services.GetRequiredService<ILocalizationService>();
        var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            await Shell.Current.GoToAsync("//home");
            app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            await Shell.Current.GoToAsync(AppShell.ReportsRoute);
            await WaitForStorePageAsync(app);
            var page = (ReportsPage)Shell.Current.CurrentPage;
            var model = (ReportsViewModel)page.BindingContext;
            // Fictitious values enter the real bound row model; this tests rendering, not financial calculations.
            model.IsAdvanced = true;
            var first = model.CategoryRows.First();
            model.CategoryRows[0] = first with
            {
                Name = "Food, household groceries and everyday essentials / Lebensmittel und Haushaltsbedarf / خوراک و نیازهای روزانه",
                NetText = Core.Money.MoneyText.Format(long.MinValue, "EUR", local.CurrentCulture),
            };
            await Task.Delay(500);
            var row = VisualDescendants(page).OfType<Grid>().First(grid =>
                ReferenceEquals(grid.BindingContext, model.CategoryRows[0]) && grid.Children.OfType<Button>().Any());
            var viewport = VisualDescendants(page).OfType<ScrollView>().First(scroll => scroll.Orientation == ScrollOrientation.Vertical);
            await viewport.ScrollToAsync(row, ScrollToPosition.Start, animated: false);
            await Task.Delay(150);
            var name = row.Children.OfType<Label>().Single(label => label.Text == model.CategoryRows[0].Name);
            if (name.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
                throw new IOException("Original report category caption unavailable.");
            native.UpdateLayout();
            var slot = LayoutInformation.GetLayoutSlot(native);
            var action = row.Children.OfType<Button>().Single();
            var wired = ReferenceEquals(action.Command, model.OpenCategoryCommand)
                && ReferenceEquals(action.CommandParameter, model.CategoryRows[0]);
            var destination = Path.Combine(folder, language + "-" + theme + "-" + width);
            Directory.CreateDirectory(destination);
            var image = Path.Combine(destination, "category.png");
            await CaptureWindowAsync(app.Windows[0], image);
            var complete = !native.IsTextTrimmed && native.IsTextScaleFactorEnabled && slot.Width > 0
                && action.Width >= 44 && action.Height >= 44 && wired;
            File.WriteAllText(Path.Combine(destination, "observed.json"), JsonSerializer.Serialize(new
            {
                Name = name.Text, native.IsTextTrimmed, native.IsTextScaleFactorEnabled,
                native.ActualWidth, native.ActualHeight, SlotWidth = slot.Width, SlotHeight = slot.Height,
                TargetWidth = action.Width, TargetHeight = action.Height, Wired = wired, Complete = complete,
                FictitiousPresentationOnly = true, FinancialCommandsInvoked = false,
            }));
            if (!complete) throw new IOException("Original report category row truncates its identity or target.");
            CheckCaption(name);
            await CheckLegendAmountAsync(page, row, model.CategoryRows[0].NetText);
            model.PackageIndex = 3;
            await model.LoadAsync();
            model.CanUseAdvancedReports = true; model.IsAdvanced = true; model.HasComposition = true;
            var composition = model.CompositionRows.First();
            model.CompositionRows[0] = composition with
            {
                Label = "Savings and long-term household reserves / Langfristige Haushaltsruecklagen / پس‌انداز بلندمدت خانواده",
                ValueText = Core.Money.MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture),
            };
            await Task.Delay(400);
            var compositionRow = VisualDescendants(page).OfType<Grid>().First(grid =>
                ReferenceEquals(grid.BindingContext, model.CompositionRows[0]) && grid.Children.OfType<BoxView>().Any());
            await viewport.ScrollToAsync(compositionRow, ScrollToPosition.Start, animated: false);
            await Task.Delay(100);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, "composition.png"));
            var compositionName = compositionRow.Children.OfType<Label>().Single(label => label.Text == model.CompositionRows[0].Label);
            CheckCaption(compositionName);
            await CheckLegendAmountAsync(page, compositionRow, model.CompositionRows[0].ValueText);
            File.WriteAllText(Path.Combine(destination, "complete.json"), JsonSerializer.Serialize(new
                { CategoryAndCompositionComplete = true, BothNativeAmountScrollEndsChecked = true, FinancialCommandsInvoked = false }));
            captures.Add(new { Language = language + "-" + theme + "-" + width, File = "category.png" });
        }
        if (before != JsonSerializer.Serialize(await store.GetEntriesAsync()))
            throw new IOException("Read-only report review changed financial rows.");
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        {
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            FinancialRowsUnchangedAfterSeeding = true, Captures = captures,
            WindowsLayoutStressOnly = true, FinancialCommandsInvoked = false,
        }));
    }

    /// <summary>Checks the original packet and actual native scroll ends, rejecting page overflow and navigation interception.</summary>
    private static async Task CheckLegendAmountAsync(ContentPage page, Grid row, string packet)
    {
        var caption = VisualDescendants(row).OfType<Label>().Single(label => label.Text == packet);
        CheckCaption(caption);
        if (caption.Parent is not ScrollView scroll || scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer native
            || page.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement root)
            throw new IOException("Original report amount has no independent native viewport.");
        var transform = native.TransformToVisual(root);
        var first = transform.TransformPoint(new(0, 0));
        var last = transform.TransformPoint(new(native.ActualWidth, 0));
        if (native.ViewportWidth <= 0 || Math.Min(first.X, last.X) < -1 || Math.Max(first.X, last.X) > root.ActualWidth + 1)
            throw new IOException("Report legend amount exceeds the actual page viewport.");
        var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(native);
        if (native.ScrollableWidth > 1)
        {
            if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll)
                is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider pattern)
                throw new IOException("Oversized report amount has no original native Scroll pattern.");
            pattern.SetScrollPercent(100, -1); await Task.Delay(50);
            if (native.HorizontalOffset < native.ScrollableWidth - 1) throw new IOException("Report currency end is unreachable.");
            pattern.SetScrollPercent(0, -1); await Task.Delay(50);
            if (native.HorizontalOffset > 1) throw new IOException("Report sign start is unreachable.");
        }
        var amount = row.Children.OfType<AmountReadout>().Single(view => view.AmountText == packet);
        if (row.Children.OfType<Button>().Any(button => Grid.GetRow(button) <= Grid.GetRow(amount)
            && Grid.GetRow(button) + Grid.GetRowSpan(button) > Grid.GetRow(amount)))
            throw new IOException("Navigation overlay intercepts the report amount viewport.");
        foreach (var button in row.Children.OfType<Button>())
        {
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement target)
                throw new IOException("Original report category button unavailable.");
            var top = target.TransformToVisual(root).TransformPoint(new(0, 0));
            if (top.Y + target.ActualHeight > first.Y + 1)
                throw new IOException("Actual navigation target overlaps the amount viewport.");
        }
    }
}
#endif
