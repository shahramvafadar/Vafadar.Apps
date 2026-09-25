namespace Vafadar.Finance.App.Features.Budget;

public partial class BudgetEditorPage : ContentPage
{
    public BudgetEditorPage(BudgetEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
