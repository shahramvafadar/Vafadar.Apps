namespace Vafadar.Zanance.App.Features.Holdings;

public partial class AssetTypeEditorPage : ContentPage
{
    public AssetTypeEditorPage(AssetTypeEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    { base.OnAppearing(); (BindingContext as AssetTypeEditorViewModel)?.SetNumericPresentationActive(true); }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        (BindingContext as AssetTypeEditorViewModel)?.SetNumericPresentationActive(false);
        (BindingContext as AssetTypeEditorViewModel)?.RetireReview();
        base.OnDisappearing();
    }

    // The Android back button asks before input is lost, like Cancel (CR12).
    protected override bool OnBackButtonPressed() =>
        (BindingContext is Presentation.IUnsavedChanges editor && Presentation.UnsavedChanges.OnBackButton(this, editor)) || base.OnBackButtonPressed();
}
