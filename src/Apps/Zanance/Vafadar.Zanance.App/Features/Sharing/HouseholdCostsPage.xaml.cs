namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>Original native actual-cost page; the persistent actions stay outside the scrolling form.</summary>
public partial class HouseholdCostsPage : ContentPage
{
    private readonly HouseholdCostsViewModel viewModel;
    private AppShell? ownerShell;
    private bool rootRetired;
    /// <summary>Retires publication after an accepted departure from this original native page.</summary>
    internal void RetirePublication() => viewModel.Deactivate();
    /// <summary>Permanently retires a replaced native root and its outstanding consent.</summary>
    internal void RetireShellPublication() { rootRetired = true; viewModel.RetireRoot(); }
    /// <summary>Coalesces retained navigation under the actual original shell and native sensitive cover.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (rootRetired) return Task.CompletedTask;
        if (Shell.Current is AppShell shell)
        {
            if (ownerShell is not null && !ReferenceEquals(ownerShell, shell)) { RetireShellPublication(); return Task.CompletedTask; }
            ownerShell = shell; shell.RegisterHouseholdCosts(this); var root = new WeakReference<AppShell>(shell);
            viewModel.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsHouseholdPublication
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        return viewModel.ActivateAsync();
    }
    /// <summary>Constructs controls only; protected storage reads wait for appearance.</summary>
    public HouseholdCostsPage(HouseholdCostsViewModel viewModel) { InitializeComponent(); BindingContext = this.viewModel = viewModel; }
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await Presentation.Failures.GuardAsync(RefreshForNavigationAsync); }
    /// <inheritdoc />
    protected override void OnDisappearing() { if (!Security.NativeSensitivePresentation.CoversOriginalPage(this)) RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (viewModel.NeedsLeaveConfirmation && !viewModel.HasPendingOperation)
        {
            _ = Presentation.Failures.GuardAsync(() => viewModel.RequestLeaveAsync(() => Shell.Current.GoToAsync("..")));
            return true;
        }
        return !viewModel.CanLeave || base.OnBackButtonPressed();
    }
}
