#if WINDOWS
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Four visible Insights destinations with growing captions and a current-page underline on Windows.</summary>
internal sealed class InsightsTabs : Grid
{
    private static readonly (string Route, string TitleKey)[] Tabs =
    [
        (AppShell.BudgetRoute, "Budget_Title"),
        (AppShell.ReportsRoute, "Report_Title"),
        (AppShell.ForecastRoute, "Forecast_Title"),
        (AppShell.GoalsRoute, "Goals_Tab"),
    ];

    /// <summary>Adds a growing navigation row above the same root-page body exactly once.</summary>
    internal static void Attach(ContentPage page, string currentRoute)
    {
        if (page.Content is not { } body || body is Grid grid && grid.Children.OfType<InsightsTabs>().Any()) { return; }
        // D-85: Shell TitleView cannot wrap all four destinations at large text. Keep them in the page's Auto row.
        if (!body.IsSet(BindingContextProperty))
        { body.SetBinding(BindingContextProperty, new Binding(nameof(Page.BindingContext), source: page)); }
        page.Content = null;
        body.WidthRequest = -1; body.HorizontalOptions = LayoutOptions.Fill;
        var host = new Grid { RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)] };
        host.Add(new InsightsTabs(currentRoute) { Margin = new Thickness(16, 8) }, 0, 0);
        host.Add(body, 0, 1);
        page.Content = host;
        Shell.SetNavBarIsVisible(page, false);
        ReadableWidth.Refresh(page);
    }

    /// <summary>Creates real native targets; narrow windows show two rows rather than hiding a destination.</summary>
    public InsightsTabs(string currentRoute)
    {
        CurrentRoute = currentRoute;
        ColumnSpacing = 4; RowSpacing = 4;
        foreach (var (route, titleKey) in Tabs) { Children.Add(CreateTab(route, titleKey, route == currentRoute)); }
        SizeChanged += (_, _) => ArrangeTabs();
        ArrangeTabs();
    }

    /// <summary>The route whose underline and spoken selected state this navigation surface represents.</summary>
    internal string CurrentRoute { get; }

    /// <summary>The Insights route of a page, or null for any other page.</summary>
    public static string? RouteOf(Page page) => page switch
    {
        Features.Budget.BudgetPage => AppShell.BudgetRoute,
        Features.Reports.ReportsPage => AppShell.ReportsRoute,
        Features.Forecast.ForecastPage => AppShell.ForecastRoute,
        Features.Goals.GoalsPage => AppShell.GoalsRoute,
        _ => null,
    };

    private void ArrangeTabs()
    {
        var columns = Width >= 600 ? 4 : 2;
        if (ColumnDefinitions.Count == columns) { return; }
        ColumnDefinitions.Clear(); RowDefinitions.Clear();
        for (var column = 0; column < columns; column++) { ColumnDefinitions.Add(new(GridLength.Star)); }
        for (var row = 0; row < Tabs.Length / columns; row++) { RowDefinitions.Add(new(GridLength.Auto)); }
        for (var index = 0; index < Children.Count; index++)
        {
            SetColumn((BindableObject)Children[index], index % columns);
            SetRow((BindableObject)Children[index], index / columns);
        }
    }

    private static Grid CreateTab(string route, string titleKey, bool selected)
    {
        var caption = new Label { FontSize = 17, FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
            LineBreakMode = LineBreakMode.WordWrap, HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center, Margin = new Thickness(8, 6), InputTransparent = true };
        caption.SetBinding(Label.TextProperty, new Binding($"[{titleKey}]", source: Translator.Instance));
        caption.SetDynamicResource(Label.TextColorProperty, selected ? "AmountText" : "SecondaryText");
        caption.SetDynamicResource(Label.FontFamilyProperty, "DisplayFont");
        AutomationProperties.SetIsInAccessibleTree(caption, false);
        var bar = new BoxView { HeightRequest = 3, CornerRadius = 1.5, Margin = new Thickness(8, 0),
            Opacity = selected ? 1 : 0, InputTransparent = true };
        bar.SetDynamicResource(BoxView.ColorProperty, "Primary");
        AutomationProperties.SetIsInAccessibleTree(bar, false);
        var button = new Button { Text = string.Empty, Padding = 0, BorderWidth = 0, CornerRadius = 8,
            MinimumHeightRequest = 44, CommandParameter = route,
            Command = new Command(async () =>
            {
                if (!selected && Shell.Current is { } shell)
                { await Failures.GuardAsync(() => shell.GoToAsync(route)); }
            }) };
        if (Application.Current?.Resources.TryGetValue("OverlayButton", out var style) == true && style is Style overlay)
        { button.Style = overlay; }
        button.SetBinding(SemanticProperties.DescriptionProperty, new Binding($"[{titleKey}]", source: Translator.Instance));
        if (selected)
        { button.SetBinding(SemanticProperties.HintProperty, new Binding("[Common_Selected]", source: Translator.Instance)); }
        var tab = new Grid { RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto)], MinimumHeightRequest = 44 };
        tab.Add(caption, 0, 0); tab.Add(bar, 0, 1);
        // D-42: the actual accessible command target stays last and covers the growing caption and underline.
        tab.Add(button, 0, 0); SetRowSpan(button, 2);
        return tab;
    }
}
#endif
