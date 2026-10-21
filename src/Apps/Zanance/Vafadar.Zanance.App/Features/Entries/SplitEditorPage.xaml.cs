namespace Vafadar.Zanance.App.Features.Entries;

public partial class SplitEditorPage : ContentPage
{
    public SplitEditorPage(SplitEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing(); (BindingContext as SplitEditorViewModel)?.SetMoneyDisplayActive(true);
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        (BindingContext as SplitEditorViewModel)?.SetMoneyDisplayActive(false);
        (BindingContext as SplitEditorViewModel)?.RetireReview();
        base.OnDisappearing();
    }

    // The Android back button asks before input is lost, like Cancel (CR12).
    protected override bool OnBackButtonPressed() =>
        (BindingContext is Presentation.IUnsavedChanges editor && Presentation.UnsavedChanges.OnBackButton(this, editor)) || base.OnBackButtonPressed();
}
