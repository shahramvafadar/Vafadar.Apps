namespace Vafadar.Finance.App.Features.Entries;

public partial class EntryEditorPage : ContentPage
{
    private readonly EntryEditorViewModel _viewModel;

    public EntryEditorPage(EntryEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Quick entry (UX-04): the amount is the first thing to type.
        if (string.IsNullOrEmpty(_viewModel.AmountText))
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => AmountEntry.Focus());
        }
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
