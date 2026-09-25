namespace Vafadar.Finance.App.Features.Plans;

public partial class PlanEditorPage : ContentPage
{
    private readonly PlanEditorViewModel _viewModel;

    public PlanEditorPage(PlanEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
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
