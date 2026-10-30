namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>Native original household page; page lifetime cancels visible secrets and stale asynchronous publication.</summary>
public partial class HouseholdPage : ContentPage
{
    private readonly HouseholdViewModel viewModel;
    private AppShell? ownerShell;
    private WeakReference<Page>? firstRunRoot;
    private bool rootRetired;
    /// <summary>Retires a departed tab without discarding durable original household intent.</summary>
    internal void RetirePublication() => viewModel.Deactivate();
    /// <summary>Permanently retires native continuations from a replaced application root.</summary>
    internal void RetireShellPublication() { rootRetired = true; viewModel.RetireRoot(); }
    /// <summary>Coalesces appearance and retained navigation under the actual original window.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (rootRetired) return Task.CompletedTask;
        if (!viewModel.IsOnboarding && Shell.Current is AppShell shell)
        {
            if (ownerShell is not null && !ReferenceEquals(ownerShell, shell)) { RetireShellPublication(); return Task.CompletedTask; }
            ownerShell = shell; shell.RegisterHousehold(this); var root = new WeakReference<AppShell>(shell);
            viewModel.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsHouseholdPublication
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        else if (viewModel.IsOnboarding)
        {
            firstRunRoot ??= Application.Current?.Windows.FirstOrDefault()?.Page is { } root ? new(root) : null;
            if (firstRunRoot is { } firstRun)
                viewModel.BindRootOwner(() => firstRun.TryGetTarget(out var original)
                    && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                    && original.Navigation.ModalStack.Contains(this));
        }
        return viewModel.ActivateAsync();
    }
    /// <summary>Constructs native controls only; storage and network work start after appearance.</summary>
    public HouseholdPage(HouseholdViewModel viewModel) { InitializeComponent(); BindingContext = this.viewModel = viewModel; }
    /// <summary>Attaches original first-run completion without replacing the wizard's unsaved choices.</summary>
    public void BeginOnboarding(Func<Task> close, Func<Task> finish) => viewModel.BeginOnboarding(close, finish);
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await Presentation.Failures.GuardAsync(RefreshForNavigationAsync); }
    /// <inheritdoc />
    protected override void OnDisappearing() { if (!Security.NativeSensitivePresentation.CoversOriginalPage(this)) RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !viewModel.CanLeave || base.OnBackButtonPressed();
}
