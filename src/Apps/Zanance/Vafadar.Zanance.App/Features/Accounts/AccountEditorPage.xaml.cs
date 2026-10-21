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

    /// <summary>Activates only this actual nested form's current native presentation.</summary>
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.SetPresentationActive(true); }
    /// <summary>Retires pending original nested metadata/confirmation callbacks before another page receives input.</summary>
    protected override void OnDisappearing() { _viewModel.SetPresentationActive(false); base.OnDisappearing(); }

    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        // Hardware Back shares the same original-child revision checks as the visible Close/Cancel action.
        Dispatcher.Dispatch(async () => await Presentation.Failures.GuardAsync(() => _viewModel.CancelCommand.ExecuteAsync(null)));
        return true;
    }
}
