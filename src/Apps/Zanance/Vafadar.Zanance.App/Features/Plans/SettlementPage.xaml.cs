namespace Vafadar.Zanance.App.Features.Plans;

public partial class SettlementPage : ContentPage
{
    public SettlementPage(SettlementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // The Android back button asks before input is lost, like Cancel (CR12).
    protected override bool OnBackButtonPressed() =>
        (BindingContext is Presentation.IUnsavedChanges editor && Presentation.UnsavedChanges.OnBackButton(this, editor)) || base.OnBackButtonPressed();
}
