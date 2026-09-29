using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();

        // Display units (e.g. toman) the user defined, before any amount is shown (FX-07).
        Presentation.DisplayUnitPreferences.Load();

        // Copies of opened receipts do not outlive the session that opened them (F2-TX-04).
        Presentation.AttachmentFiles.ClearCache();

        // Light or dark theme (UX-08, D-22). Colors are dynamic resources; screens with computed colors reload with the shell.
        var theme = services.GetRequiredService<Presentation.ThemeService>();
        theme.Initialize(this);
        theme.Changed += OnLocalizationChanged;
        var localization = services.GetRequiredService<ILocalizationService>();

        // Persian digits in the Persian interface (D-27); applied before the screens are rebuilt for a new language.
        Presentation.DigitPreferences.Apply(localization);
        localization.Changed += (_, _) => Presentation.DigitPreferences.Apply(localization);
        Presentation.DigitPreferences.Changed += OnLocalizationChanged;
        localization.Changed += OnLocalizationChanged;

        // Progress bars are drawn left to right on every platform; in right-to-left languages they are mirrored, so they
        // fill in the reading direction.
        Resources["ReadingScaleX"] = localization.IsRightToLeft ? -1d : 1d;
        localization.Changed += (_, _) => Resources["ReadingScaleX"] = localization.IsRightToLeft ? -1d : 1d;

        // Persian text in Vazirmatn; English and German in Figtree with Urbanist (the wordmark's face) for titles and
        // large amounts (D-27).
        ApplyFonts(localization);
        localization.Changed += (_, _) => ApplyFonts(localization);
        this.ApplyToModalPages(localization);

        var reminders = services.GetRequiredService<ReminderService>();
        reminders.WatchChanges();
        reminders.Scheduler.Tapped += OnReminderTapped;
    }

    private void ApplyFonts(ILocalizationService localization)
    {
        var persian = localization.CurrentCulture.TwoLetterISOLanguageName == "fa";
        Resources["AppFont"] = persian ? "Vazirmatn" : "Figtree";
        Resources["DisplayFont"] = persian ? "VazirmatnBold" : "UrbanistBold";
    }

    /// <summary>Replaces the root page of the main window, e.g. after onboarding.</summary>
    public void ShowMainShell()
    {
        if (Windows.FirstOrDefault() is { } window)
        {
            window.Page = CreateShell();
        }
    }

    /// <summary>Shows onboarding again, e.g. after all data was deleted.</summary>
    public void ShowOnboarding()
    {
        if (Windows.FirstOrDefault() is { } window)
        {
            window.Page = _services.GetRequiredService<OnboardingPage>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var settings = _services.GetRequiredService<ZananceStore>().GetSettings();
        Page root = settings.OnboardingCompleted
            ? CreateShell()
            : _services.GetRequiredService<OnboardingPage>().WithFlowDirection(_services.GetRequiredService<ILocalizationService>());

        var window = new Window(root) { Title = Translator.Instance["App_Name"] };
#if DEBUG
        Diagnostics.DebugSnapshots.StartIfRequested(this, _services, window);
#endif
        return window;
    }

    protected override void OnStart()
    {
        base.OnStart();
        Dispatcher.Dispatch(async () => await _services.GetRequiredService<AppLockService>().StartAsync());
        RunForegroundWork(starting: true);
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        Dispatcher.Dispatch(async () => await _services.GetRequiredService<AppLockService>().SleepAsync());
    }

    protected override void OnResume()
    {
        base.OnResume();
        Dispatcher.Dispatch(async () => await _services.GetRequiredService<AppLockService>().ResumeAsync());
        RunForegroundWork(starting: false);
    }

    // Due plan occurrences are recorded whenever the app comes to the foreground, then reminders are rebuilt;
    // correctness never depends on background execution (REC-22). Failures are not fatal: occurrences stay open.
    private void RunForegroundWork(bool starting)
    {
        var processor = _services.GetRequiredService<AutoPostProcessor>();
        var reminders = _services.GetRequiredService<ReminderService>();
        var store = _services.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(_services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        _ = Task.Run(async () =>
        {
            try
            {
                await processor.RunAsync(today);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine($"Automatic posting failed: {ex}");
            }

            await reminders.RefreshAsync();

            // Receipts of deleted entries are removed at start only: undo is kept in memory, so after a restart no
            // deleted entry can come back, while on resume an undo may still be pending (F2-TX-04).
            if (!starting)
            {
                return;
            }

            try
            {
                await store.PurgeOrphanAttachmentsAsync();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine($"Attachment cleanup failed: {ex}");
            }
        });
    }

    // A tapped reminder opens its occurrence; several taps or an old notification never record anything (REM-04, REM-06).
    private void OnReminderTapped(object? sender, string link) => Dispatcher.Dispatch(() =>
        _services.GetRequiredService<AppLockService>().RunWhenUnlockedAsync(() => OpenLinkAsync(link)));

    private async Task OpenLinkAsync(string link)
    {
        var parts = link.Split('|');
        if (Shell.Current is null)
        {
            return;
        }

        if (parts is ["occurrence", var plan, var date] && Guid.TryParse(plan, out var planId)
            && DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var original))
        {
            await Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = planId, ["date"] = original });
        }
        else if (parts is ["plan", var id] && Guid.TryParse(id, out var scheduleId))
        {
            await Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = scheduleId });
        }
        else if (parts is ["plans"])
        {
            await Shell.Current.GoToAsync("//plans");
        }
        else if (parts is ["budget"])
        {
            await Shell.Current.GoToAsync(AppShell.BudgetRoute);
        }
    }

    // Pages cache formatted numbers, dates and icons, and flipping the flow direction of a live visual tree is not
    // reliable on every platform. Rebuilding the shell gives a clean result; the user stays on the current page.
    private void OnLocalizationChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(async () =>
    {
        // Reminder texts are translated when they are scheduled (REM-07).
        _services.GetRequiredService<ReminderService>().RefreshSoon();
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
