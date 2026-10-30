using Vafadar.Localization;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>A covered read-only comparison with growing choices and original access facts.</summary>
public partial class ProductPlansPage : ContentPage
{
    private readonly ProductPlansViewModel _viewModel;
    private readonly ILocalizationService _localization;
    private AppShell? ownerShell;
    private bool rootRetired;
    /// <summary>Retires this page only after its actual original departure.</summary>
    internal void RetirePublication() => _viewModel.DeactivateBilling();
    /// <summary>A replaced native root permanently retires this page and its outstanding store consent.</summary>
    internal void RetireShellPublication() { rootRetired = true; _viewModel.RetireRoot(); }
    /// <summary>Retained navigation shares appearance under the same original page and sensitive native cover.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (rootRetired) return Task.CompletedTask;
        if (Shell.Current is AppShell shell)
        {
            if (ownerShell is not null && !ReferenceEquals(ownerShell, shell)) { RetireShellPublication(); return Task.CompletedTask; }
            ownerShell = shell; shell.RegisterProductPlans(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsProductPlansPublication
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        return _viewModel.LoadAsync();
    }
    /// <summary>Creates the original page without database work or purchase activation.</summary>
    public ProductPlansPage(ProductPlansViewModel viewModel, ILocalizationService localization)
    {
        InitializeComponent(); BindingContext = _viewModel = viewModel; _localization = localization;
        EcbSourceLink.Command = new Command(() => OpenEcbSource(this, EventArgs.Empty));
    }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _localization.Changed -= OnDisplayChanged; _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _localization.Changed -= OnDisplayChanged; if (!Security.NativeSensitivePresentation.CoversOriginalPage(this)) RetirePublication(); base.OnDisappearing();
    }
    private void OnDisplayChanged(object? sender, EventArgs e) => _viewModel.RefreshDisplay();
    // The free original remains available before payment, independently of any eligible store offer.
    private async void OpenEcbSource(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(
        () => _viewModel.OpenReferenceAsync(async () =>
        { await Browser.Default.OpenAsync(new Uri(Core.Rates.EcbReference.SourceUrl), BrowserLaunchMode.External); }));
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !_viewModel.CanLeave || base.OnBackButtonPressed();
}
