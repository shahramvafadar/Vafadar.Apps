namespace Vafadar.Zanance.App.Features.Holdings;

public partial class AssetEventEditorPage : ContentPage
{
    public AssetEventEditorPage(AssetEventEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        (BindingContext as AssetEventEditorViewModel)?.RetireReview();
        base.OnDisappearing();
    }

    // The Android back button asks before input is lost, like Cancel (CR12).
    protected override bool OnBackButtonPressed() =>
        (BindingContext is Presentation.IUnsavedChanges editor && Presentation.UnsavedChanges.OnBackButton(this, editor)) || base.OnBackButtonPressed();
}
