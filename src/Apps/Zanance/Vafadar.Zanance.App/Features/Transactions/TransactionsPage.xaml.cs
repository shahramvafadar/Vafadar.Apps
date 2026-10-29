namespace Vafadar.Zanance.App.Features.Transactions;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;
    private bool _publicationActive;

    /// <summary>Exposes the original appearance read without racing a second native-review load.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public TransactionsPage(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        TransactionsLayout.SizeChanged += OnFilterLayoutChanged;
        TransactionsHeader.SizeChanged += OnFilterLayoutChanged;
        TransactionsHeader.PropertyChanged += OnHeaderLayoutPropertyChanged;
        BulkActions.SizeChanged += OnFilterLayoutChanged;
        UndoNotice.SizeChanged += OnFilterLayoutChanged;
        BulkActions.PropertyChanged += OnActionVisibilityChanged;
        UndoNotice.PropertyChanged += OnActionVisibilityChanged;
    }

    // D-108: retain room for result rows while the same complete filter form scrolls vertically when necessary.
    // Native text scaling and wrapping remain enabled; bound the viewport, never the text or its financial result.
    private void OnFilterLayoutChanged(object? sender, EventArgs e)
    {
        if (TransactionsLayout.Height <= 0 || TransactionsHeader.Height <= 0) { return; }
        static double Reserved(View element) => element.IsVisible ? Math.Max(44, element.Height) + element.Margin.VerticalThickness : 0;
        // D-548: the arranged header position includes Android container-safe-area space; height alone omits it.
        var headerBottom = Math.Max(0, TransactionsHeader.Y) + TransactionsHeader.Height + TransactionsHeader.Margin.Bottom;
        var limit = TransactionsFilterViewportBudget.Calculate(TransactionsLayout.Height, headerBottom,
            Reserved(BulkActions) + Reserved(UndoNotice), TransactionsLayout.Padding.Bottom);
        if (Math.Abs(FiltersViewport.MaximumHeightRequest - limit) > .5)
        { FiltersViewport.MaximumHeightRequest = limit; }
    }

    /// <summary>Recomputes the original viewport when only the arranged safe-area/header position changes.</summary>
    private void OnHeaderLayoutPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(VisualElement.Y) or nameof(View.Margin)) { OnFilterLayoutChanged(sender, EventArgs.Empty); }
    }

    private void OnActionVisibilityChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsVisible)) { OnFilterLayoutChanged(sender, EventArgs.Empty); }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterTransactionCenter(this);
        }
        HeaderBack.IsVisible = Navigation.NavigationStack.Count > 1;
        var backIcon = new FluentIcons.Maui.SymbolImageSource
        {
            Symbol = FlowDirection == FlowDirection.RightToLeft ? FluentIcons.Common.Symbol.ArrowRight : FluentIcons.Common.Symbol.ArrowLeft,
            Size = 22,
        };
        backIcon.SetDynamicResource(FontImageSource.ColorProperty, "Primary");
        HeaderBack.Source = backIcon;
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    protected override void OnDisappearing()
    {
        RetirePublication();
        base.OnDisappearing();
    }

    /// <summary>Shares the actual appearance and retained-tab read, without automatically retrying a failed presence.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetActive(true);
        _viewModel.Undo.Changed -= OnUndoChanged;
        _viewModel.Undo.Changed += OnUndoChanged;
        _viewModel.UpdateUndo();
        return PendingPublication = _viewModel.LoadAsync();
    }
    /// <summary>Retires complete reads, rows, original native consent and Undo commands on actual departure.</summary>
    internal void RetirePublication()
    {
        // Retained Android tabs/root replacement may omit native OnDisappearing; the actual owner must release this singleton event.
        _viewModel.Undo.Changed -= OnUndoChanged;
        _publicationActive = false; _viewModel.SetActive(false);
    }
    /// <summary>Prevents an old Shell root from reactivating transaction commands.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }

    private void OnUndoChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(_viewModel.UpdateUndo);

    private void OnUnreviewedClicked(object? sender, EventArgs e) => _viewModel.UnreviewedOnly = !_viewModel.UnreviewedOnly;

    /// <summary>Preserves original Shell Back for a pushed Transactions page with its own title row.</summary>
    private void OnHeaderBackClicked(object? sender, EventArgs e) => Shell.Current?.SendBackButtonPressed();

    // The gate coalesces repeated taps; the normal failure dialog and the inline retry remain available.
    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
}
