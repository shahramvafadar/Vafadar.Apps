#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies both original plan-row hosts with presentation-only extreme packets and unchanged stored inputs.</summary>
    private static async Task ReviewPlanRowsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await StoredAsync();
        var local = services.GetRequiredService<ILocalizationService>();
        var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            if (Environment.GetEnvironmentVariable("VAFADAR_PLAN_ROWS_EDGE_PROBE") == "1"
                && (language != "en" || theme != ThemeChoice.Light || width != 360)) continue;
            await Shell.Current.GoToAsync("//home");
            app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            await WaitForStorePageAsync(app);
            var homePage = (HomePage)Shell.Current.CurrentPage;
            await homePage.PendingPublication;
            var home = (HomeViewModel)homePage.BindingContext;
            var original = home.Upcoming.First();
            var fixture = original with { AmountText = Core.Money.MoneyText.Format(long.MinValue, "EUR", local.CurrentCulture) };
            home.Upcoming[0] = fixture;
            var destination = Path.Combine(folder, language + "-" + theme + "-" + width);
            Directory.CreateDirectory(destination);
            await Task.Delay(250);
            var view = VisualDescendants(homePage).OfType<PlanRowView>().Single(value => ReferenceEquals(value.BindingContext, fixture));
            await homePage.FindByName<ScrollView>("ContentViewport").ScrollToAsync(view, ScrollToPosition.Center, false);
            await CheckAsync(homePage, view, fixture, home.OpenOccurrenceCommand, Path.Combine(destination, "home"));

            await Shell.Current.GoToAsync("//plans");
            await WaitForStorePageAsync(app);
            var plansPage = (PlansPage)Shell.Current.CurrentPage;
            var plans = (PlansViewModel)plansPage.BindingContext;
            for (var wait = 0; plans.Rows.Count == 0 && wait < 100; wait++) await Task.Delay(50);
            if (plans.Rows.Count == 0) throw new IOException("Original seeded plan rows are unavailable.");
            var plan = plans.Rows[0] with { AmountText = Core.Money.MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture) };
            plans.Rows = new[] { plan };
            await Task.Delay(250);
            var planView = VisualDescendants(plansPage).OfType<PlanRowView>().Single(value => ReferenceEquals(value.BindingContext, plan));
            await CheckAsync(plansPage, planView, plan, plans.OpenCommand, Path.Combine(destination, "plans"));
            captures.Add(new { Language = language + "-" + theme + "-" + width, File = "home/window.png" });
        }
        if (before != await StoredAsync()) throw new IOException("Plan-row presentation changed original stored inputs.");
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        {
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = true, FinancialCommandsInvoked = false, WindowsLayoutStressOnly = true,
        }));

        async Task CheckAsync(ContentPage page, PlanRowView view, PlanRow model, System.Windows.Input.ICommand command, string output)
        {
            Directory.CreateDirectory(output);
            var row = (Grid)view.Content;
            var caption = VisualDescendants(view).OfType<Label>().Single(value => value.Text == model.AmountText);
            if (caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
                throw new IOException("Original native plan amount is missing.");
            var button = row.Children.OfType<Button>().Single();
            var independent = caption.Parent is ScrollView;
            var wired = ReferenceEquals(button.Command, command) && ReferenceEquals(button.CommandParameter, model);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "window.png"));
            File.WriteAllText(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(new
            {
                caption.Text, native.IsTextTrimmed, native.IsTextScaleFactorEnabled, native.FontSize,
                native.ActualWidth, native.ActualHeight, IndependentMonetaryViewport = independent,
                TargetWidth = button.Width, TargetHeight = button.Height, Wired = wired,
                FinancialCommandsInvoked = false,
            }));
            if (!independent || !wired || button.Width < 44 || button.Height < 44)
                throw new IOException("Original plan amount has no independent accessible viewport or unchanged target.");
            await CheckLegendAmountAsync(page, row, model.AmountText);
            // Check actual post-layout glyph positions: a valid outer viewport alone cannot prove the sign is visible.
            var scroll = (ScrollView)caption.Parent;
            var viewer = (Microsoft.UI.Xaml.Controls.ScrollViewer)scroll.Handler!.PlatformView!;
            native.UpdateLayout();
            var transform = native.TransformToVisual(viewer);
            var glyphs = Enumerable.Range(0, native.ContentEnd.Offset - native.ContentStart.Offset + 1)
                .Select(offset => native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward))
                .Where(pointer => pointer is not null)
                .Select(pointer => pointer!.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)).ToArray();
            var start = glyphs.MinBy(rectangle => rectangle.Left);
            // WinUI returns zero-width caret rectangles; the next distinct position bounds the first glyph advance.
            var nextX = glyphs.Select(rectangle => rectangle.Left).Where(left => left > start.Left + 0.01).DefaultIfEmpty(start.Left).Min();
            var advance = nextX - start.Left;
            var startPoint = transform.TransformPoint(new(start.X, start.Y));
            File.WriteAllText(Path.Combine(output, "amount-edge.json"), JsonSerializer.Serialize(new
                { StartX = startPoint.X, FirstAdvance = advance, viewer.ViewportWidth, viewer.HorizontalOffset,
                    CaptionWidth = native.ActualWidth, ActualViewportWidth = viewer.ActualWidth }));
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "post-scroll-start.png"));
            if (startPoint.X < -1 || startPoint.X + advance > viewer.ViewportWidth + 1)
                throw new IOException("Original plan sign lies outside its native start viewport.");
            if (SemanticProperties.GetDescription(button) != model.Description)
                throw new IOException("Original complete plan spoken identity changed.");
            CheckCaption(VisualDescendants(view).OfType<Label>().Single(value => value.Text == model.Title));
            File.WriteAllText(Path.Combine(output, "complete.json"), JsonSerializer.Serialize(new
                { CompletePacketAndNativeScrollEnds = true, OriginalCommandAndTarget = true, FinancialCommandsInvoked = false }));
        }
    }
}
#endif
