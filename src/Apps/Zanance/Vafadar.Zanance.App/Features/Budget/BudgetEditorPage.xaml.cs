namespace Vafadar.Zanance.App.Features.Budget;

public partial class BudgetEditorPage : ContentPage
{
    public BudgetEditorPage(BudgetEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // The Android back button asks before input is lost, like Cancel (CR12).
    protected override bool OnBackButtonPressed() =>
        (BindingContext is Presentation.IUnsavedChanges editor && Presentation.UnsavedChanges.OnBackButton(this, editor)) || base.OnBackButtonPressed();
}
