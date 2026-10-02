#if WINDOWS
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// The four Insights pages – Budget, Reports, Forecast, Goals – as visible tabs in the page header on Windows (D-43).
/// The Windows shell lists them only in a drop-down of the Insights tab, where they are easily missed; Android and iOS
/// show them as top tabs of their own. The current page's tab is bold with a bar below it.
/// </summary>
internal sealed class InsightsTabs : HorizontalStackLayout
{
    private static readonly (string Route, string TitleKey)[] Tabs =
    [
        (AppShell.BudgetRoute, "Budget_Title"),
        (AppShell.ReportsRoute, "Report_Title"),
        (AppShell.ForecastRoute, "Forecast_Title"),
        // "Goals", not "Savings goals": the four fit next to each other down to a 360 px window.
        (AppShell.GoalsRoute, "Goals_Tab"),
    ];

    public InsightsTabs(string currentRoute)
    {
        Spacing = 2;
        VerticalOptions = LayoutOptions.Center;
        foreach (var (route, titleKey) in Tabs)
        {
            Add(CreateTab(route, titleKey, route == currentRoute));
        }
    }

    /// <summary>The Insights route of a page, or <see langword="null"/> for every other page.</summary>
    public static string? RouteOf(Page page) => page switch
    {
        Features.Budget.BudgetPage => AppShell.BudgetRoute,
        Features.Reports.ReportsPage => AppShell.ReportsRoute,
        Features.Forecast.ForecastPage => AppShell.ForecastRoute,
        Features.Goals.GoalsPage => AppShell.GoalsRoute,
        _ => null,
    };

    private static VerticalStackLayout CreateTab(string route, string titleKey, bool selected)
    {
        var button = new Button
        {
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            CornerRadius = 8,
            Padding = new Thickness(8, 0),
            HeightRequest = 40,
            MinimumHeightRequest = 40,
            FontSize = 17,
            FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
        };
        button.SetBinding(Button.TextProperty, new Binding($"[{titleKey}]", source: Translator.Instance));
        button.SetDynamicResource(Button.TextColorProperty, selected ? "AmountText" : "SecondaryText");
        button.SetDynamicResource(Button.FontFamilyProperty, "DisplayFont");
        if (selected)
        {
            SemanticProperties.SetHint(button, Translator.Instance["Common_Selected"]);
        }
        else
        {
            button.Clicked += async (_, _) => await Shell.Current.GoToAsync(route);
        }

        // The bar keeps its height on every tab, so the titles stay on one line whichever page is open.
        var bar = new BoxView { HeightRequest = 3, CornerRadius = 1.5, Margin = new Thickness(8, 0), Opacity = selected ? 1 : 0 };
        bar.SetDynamicResource(BoxView.ColorProperty, "Primary");
        return new VerticalStackLayout { Spacing = 0, Children = { button, bar } };
    }
}
#endif
