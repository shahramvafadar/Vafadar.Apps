namespace Vafadar.Finance.App.Features.Categories;

public partial class CategoryEditorPage : ContentPage
{
    private readonly CategoryEditorViewModel _viewModel;

    public CategoryEditorPage(CategoryEditorViewModel viewModel)
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
