namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>Original aggregate disclosure lifetime; departure retires publication without erasing private intent.</summary>
public partial class AiAnalysisPage : ContentPage
{
    private readonly AiAnalysisViewModel model;
    private AppShell? ownerShell;
    private bool rootRetired;
    /// <summary>Retires only this actual page, including its pending local source read.</summary>
    internal void RetirePublication() => model.Retire();
    /// <summary>A replaced root cannot reactivate original consent or a private remote.</summary>
    internal void RetireShellPublication() { rootRetired = true; model.RetireRoot(); }
    /// <summary>Shares actual appearance/navigation and owned native cover without silent failure retry.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (rootRetired) return Task.CompletedTask;
        if (Shell.Current is AppShell shell)
        {
            if (ownerShell is not null && !ReferenceEquals(ownerShell, shell)) { RetireShellPublication(); return Task.CompletedTask; }
            ownerShell = shell; shell.RegisterAnalysis(this); var root = new WeakReference<AppShell>(shell);
            model.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsAnalysisPublication
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        return model.ActivateAsync();
    }
    /// <summary>Constructs native controls without database/network work.</summary>
    public AiAnalysisPage(AiAnalysisViewModel model) { InitializeComponent(); BindingContext = this.model = model; }
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await Presentation.Failures.GuardAsync(RefreshForNavigationAsync); }
    /// <inheritdoc />
    protected override void OnDisappearing()
    { if (!Security.NativeSensitivePresentation.CoversOriginalPage(this)) RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !model.CanLeave || base.OnBackButtonPressed();
}
