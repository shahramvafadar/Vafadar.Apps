namespace Vafadar.Zanance.App.Features.Transactions;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;

    public TransactionsPage(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        TransactionsLayout.SizeChanged += OnFilterLayoutChanged;
        AddAction.SizeChanged += OnFilterLayoutChanged;
        BulkActions.SizeChanged += OnFilterLayoutChanged;
        UndoNotice.SizeChanged += OnFilterLayoutChanged;
        AddAction.PropertyChanged += OnActionVisibilityChanged;
        BulkActions.PropertyChanged += OnActionVisibilityChanged;
        UndoNotice.PropertyChanged += OnActionVisibilityChanged;
    }

    // D-108: retain room for result rows while the same complete filter form scrolls vertically when necessary.
    // Native text scaling and wrapping remain enabled; bound the viewport, never the text or its financial result.
    private void OnFilterLayoutChanged(object? sender, EventArgs e)
    {
        if (TransactionsLayout.Height <= 0) { return; }
        static double Reserved(View element) => element.IsVisible ? Math.Max(44, element.Height) + element.Margin.VerticalThickness : 0;
        var dock = Math.Max(Reserved(AddAction), Reserved(BulkActions)) + Reserved(UndoNotice);
        var results = Math.Min(144, TransactionsLayout.Height / 3);
        var limit = Math.Max(44, TransactionsLayout.Height - dock - results);
        if (Math.Abs(FiltersViewport.MaximumHeightRequest - limit) > .5)
        { FiltersViewport.MaximumHeightRequest = limit; }
    }

    private void OnActionVisibilityChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsVisible)) { OnFilterLayoutChanged(sender, EventArgs.Empty); }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Undo.Changed += OnUndoChanged;
        _viewModel.UpdateUndo();
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }

    protected override void OnDisappearing()
    {
        _viewModel.Undo.Changed -= OnUndoChanged;
        base.OnDisappearing();
    }

    private void OnUndoChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(_viewModel.UpdateUndo);

    private void OnUnreviewedClicked(object? sender, EventArgs e) => _viewModel.UnreviewedOnly = !_viewModel.UnreviewedOnly;

    // The gate coalesces repeated taps; the normal failure dialog and the inline retry remain available.
    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
}
