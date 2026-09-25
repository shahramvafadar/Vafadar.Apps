namespace Vafadar.Finance.App.Features.Transactions;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _viewModel;

    public TransactionsPage(TransactionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Undo.Changed += OnUndoChanged;
        _viewModel.UpdateUndo();
        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Undo.Changed -= OnUndoChanged;
        base.OnDisappearing();
    }

    private void OnUndoChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(_viewModel.UpdateUndo);

    private void OnUnreviewedClicked(object? sender, EventArgs e) => _viewModel.UnreviewedOnly = !_viewModel.UnreviewedOnly;
}
