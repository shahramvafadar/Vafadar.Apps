using System.ComponentModel;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Dashboard;

namespace Vafadar.Zanance.App.Features.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly Vafadar.Zanance.Data.ZananceStore _store;
    private readonly Dictionary<HomeSection, VerticalStackLayout> _sections;
    private HomeLayout _layout = HomeLayout.Default;

    public HomePage(HomeViewModel viewModel, Vafadar.Zanance.Data.ZananceStore store)
    {
        _store = store;
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _sections = new()
        {
            [HomeSection.Period] = PeriodSection,
            [HomeSection.Forecast] = ForecastSection,
            [HomeSection.Budget] = BudgetSection,
            [HomeSection.Upcoming] = UpcomingSection,
            [HomeSection.Recent] = RecentSection,
            [HomeSection.Categories] = CategoriesSection,
            [HomeSection.Accounts] = AccountsSection,
            [HomeSection.Goals] = GoalsSection,
        };

        // A section without visible content takes no room, so hidden cards leave no extra gap.
        foreach (var section in _sections.Values)
        {
            foreach (var child in section.Children.OfType<VisualElement>())
            {
                child.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(IsVisible))
                    {
                        UpdateVisibility();
                    }
                };
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyLayout();

        // Language, calendar or data may have changed elsewhere.
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }

    // The user's order and visibility of the sections (§21.5); balance and attention always stay on top (DASH-03).
    private void ApplyLayout()
    {
        _layout = HomeLayoutPreferences.Load(_store);
        var first = _sections.Values.Min(s => Sections.Children.IndexOf(s));
        foreach (var section in _sections.Values)
        {
            Sections.Children.Remove(section);
        }

        var index = first;
        foreach (var state in _layout.Sections)
        {
            Sections.Children.Insert(index++, _sections[state.Section]);
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        foreach (var state in _layout.Sections)
        {
            var section = _sections[state.Section];
            section.IsVisible = state.IsVisible && section.Children.OfType<VisualElement>().Any(c => c.IsVisible);
        }
    }
}
