namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Covered loading and lifetime of the exact accountant disclosure form.</summary>
public partial class AccountantExportPage : ContentPage
{
    private readonly AccountantExportViewModel _model;
    /// <summary>Creates the native form without opening its database during construction.</summary>
    public AccountantExportPage(AccountantExportViewModel model)
    { InitializeComponent(); BindingContext = _model = model; }
    /// <inheritdoc />
    protected override async void OnAppearing()
    { base.OnAppearing(); await Presentation.Failures.GuardAsync(_model.LoadAsync); }
    /// <inheritdoc />
    protected override void OnDisappearing() { _model.Retire(); base.OnDisappearing(); }
}
