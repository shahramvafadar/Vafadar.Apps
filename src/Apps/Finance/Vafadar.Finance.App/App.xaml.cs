using Microsoft.Extensions.DependencyInjection;
using Vafadar.Finance.App.Features.Onboarding;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;

namespace Vafadar.Finance.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();

        // Phase 1 ships the light theme only (D-12, UX-08).
        UserAppTheme = AppTheme.Light;
        services.GetRequiredService<ILocalizationService>().Changed += OnLocalizationChanged;
    }

    /// <summary>Replaces the root page of the main window, e.g. after onboarding.</summary>
    public void ShowMainShell()
    {
        if (Windows.FirstOrDefault() is { } window)
        {
            window.Page = CreateShell();
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var settings = _services.GetRequiredService<FinanceStore>().GetSettings();
        Page root = settings.OnboardingCompleted
            ? CreateShell()
            : _services.GetRequiredService<OnboardingPage>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());

        var window = new Window(root) { Title = Translator.Instance["App_Name"] };
#if DEBUG
        Diagnostics.DebugSnapshots.StartIfRequested(this, _services, window);
#endif
        return window;
    }

    // Pages cache formatted numbers, dates and icons, and flipping the flow direction of a live visual tree is not
    // reliable on every platform. Rebuilding the shell gives a clean result; the user stays on the current page.
    private void OnLocalizationChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(async () =>
    {
        if (Windows.FirstOrDefault() is not { Page: AppShell shell } window)
        {
            return;
        }

        var location = shell.CurrentState?.Location?.OriginalString;
        window.Page = CreateShell();
        if (!string.IsNullOrEmpty(location) && Shell.Current is { } current && current.CurrentState?.Location?.OriginalString != location)
        {
            try
            {
                await current.GoToAsync(location, animate: false);
            }
            catch (ArgumentException)
            {
                // The route no longer resolves; staying on the first tab is fine.
            }
        }
    });

    private AppShell CreateShell() =>
        _services.GetRequiredService<AppShell>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());
}
