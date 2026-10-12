namespace Vafadar.Zanance.App.Features.Accounts;

public partial class AccountEditorPage : ContentPage, IQueryAttributable
{
    private readonly AccountEditorViewModel _viewModel;

    public AccountEditorPage(AccountEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Forwards the native route into the actual platform-independent draft loader.</summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query) =>
        await Presentation.Failures.GuardAsync(() => _viewModel.ApplyQueryAsync(query));

    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        if (!_viewModel.IsDirty)
        {
            return base.OnBackButtonPressed();
        }

        Dispatcher.Dispatch(async () =>
        {
            if (await _viewModel.ConfirmDiscardAsync())
            {
                await Shell.Current.GoToAsync("..");
            }
        });
        return true;
    }
}
