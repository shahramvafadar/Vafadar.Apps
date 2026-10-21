using System.ComponentModel;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Dashboard;

namespace Vafadar.Zanance.App.Features.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly Dictionary<HomeSection, VerticalStackLayout> _sections;
    private HomeLayout _layout = HomeLayout.Default;
    private bool _observingUndo;
    private long _undoVersion;
    /// <summary>Gets the actual appearance publication so native review awaits this lifecycle rather than racing a second read.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    /// <summary>Prevents pending reads from publishing into controls whose original Shell is being disconnected.</summary>
    internal void RetirePublication() => _viewModel.RetirePublication();

    public HomePage(HomeViewModel viewModel)
    {
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
        // D-285: apply this same captured profile layout before the loading cover is removed.
        // Appearance must not perform a separate synchronous SQLCipher settings read.
        _viewModel.PresentingSnapshot += ApplyLayout;

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

    /// <summary>Uses the original Shell back handling for a pushed Home without adding a second header.</summary>
    private void OnHeaderBackClicked(object? sender, EventArgs e) => Shell.Current?.SendBackButtonPressed();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _observingUndo = true;
        _viewModel.Undo.Changed -= OnUndoChanged; _viewModel.Undo.Changed += OnUndoChanged;
        UpdateOriginalUndo();
        // D-375: replace the native title bar once, retaining Back when this same page is pushed.
        HeaderBack.IsVisible = Navigation.NavigationStack.Count > 1;
        var backIcon = new FluentIcons.Maui.SymbolImageSource
        {
            Symbol = FlowDirection == FlowDirection.RightToLeft ? FluentIcons.Common.Symbol.ArrowRight : FluentIcons.Common.Symbol.ArrowLeft,
            Size = 22,
        };
        backIcon.SetDynamicResource(FontImageSource.ColorProperty, "Primary");
        HeaderBack.Source = backIcon;
        // Register the actual lazy-created page, not a lookup through ShellContent.Content.
        AppShell? owner = null;
        for (Element? ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor is AppShell shell) { owner = shell; break; }
        (owner ?? Shell.Current as AppShell)?.RegisterHomePublication(this);
        // Language, calendar or data may have changed elsewhere.
        PendingPublication = _viewModel.LoadAsync();
        await Presentation.Failures.GuardAsync(() => PendingPublication);
    }

    /// <summary>Detaches only this page's transient offer observation; the original receipt/deadline remains shared.</summary>
    protected override void OnDisappearing()
    {
        _observingUndo = false; _undoVersion++; _viewModel.Undo.Changed -= OnUndoChanged;
        base.OnDisappearing();
    }
    private void OnUndoChanged(object? sender, EventArgs args) => Dispatcher.Dispatch(UpdateOriginalUndo);
    private void UpdateOriginalUndo()
    {
        if (!_observingUndo) return;
        var version = ++_undoVersion; _viewModel.UpdateUndo();
        if (_viewModel.ShowUndo)
            Dispatcher.DispatchDelayed(_viewModel.Undo.Remaining, () =>
            { if (_observingUndo && version == _undoVersion) _viewModel.UpdateUndo(); });
    }

    // The user's order and visibility of the sections (§21.5); balance and attention always stay on top (DASH-03).
    /// <summary>Applies saved visibility and order while retaining every correctly placed native section.</summary>
    internal void ApplyLayout()
    {
        _layout = HomeLayoutPreferences.FromSnapshot(_viewModel.CapturedHomeLayout);
        var desired = _layout.Sections.Select(state => _sections[state.Section]).ToArray();
        var first = _sections.Values.Min(section => Sections.Children.IndexOf(section));
        // D-177: keep native section trees attached when their order already matches. Move only a displaced section,
        // rather than detaching every chart, input and financial row on each appearance.
        for (var index = 0; index < desired.Length; index++)
        {
            var target = first + index;
            var section = desired[index];
            if (ReferenceEquals(Sections.Children[target], section)) continue;
            Sections.Children.Remove(section);
            Sections.Children.Insert(target, section);
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

        // D-95: a hidden BindableLayout still constructs every native row when its collection changes. Keep the
        // complete VM snapshot, but attach it only when the user's Accounts section can display it. The same source
        // stays attached during visible reloads, preserving D-92's native rows and fresh binding contexts.
        var source = AccountsSection.IsVisible ? _viewModel.Accounts : null;
        if (!ReferenceEquals(BindableLayout.GetItemsSource(AccountRows), source))
        {
            BindableLayout.SetItemsSource(AccountRows, source);
        }

        // D-342: a hidden category section must not construct complete readout/scroll controls.
        // Retain the same source during visible reloads, just as the account rows do above.
        var categories = CategoriesSection.IsVisible ? _viewModel.Slices : null;
        if (!ReferenceEquals(BindableLayout.GetItemsSource(CategoryRows), categories))
            BindableLayout.SetItemsSource(CategoryRows, categories);
    }

#if DEBUG && WINDOWS
    /// <summary>Exercises original account-row visibility in an isolated presentation fixture without saving layout.</summary>
    internal void PreviewAccountSectionForReview(bool visible)
    {
        _layout.SetVisible(HomeSection.Accounts, visible);
        UpdateVisibility();
    }

    /// <summary>Exercises original optional-section attachment with a fictitious view-only layout, without saving preferences.</summary>
    internal void PreviewCategorySectionForReview(bool visible)
    {
        _layout.SetVisible(HomeSection.Categories, visible);
        UpdateVisibility();
    }
#endif

}
