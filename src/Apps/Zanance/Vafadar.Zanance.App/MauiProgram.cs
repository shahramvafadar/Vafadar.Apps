using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
#if ANDROID || IOS
using Plugin.LocalNotification;
#endif
using Vafadar.Authentication.Maui;
using Vafadar.Backup;
using Vafadar.Backup.GoogleDrive;
using Vafadar.Backup.OneDrive;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.More;
using Vafadar.Zanance.App.Features.Onboarding;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.App.Resources.Strings;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Data;
using Vafadar.Maui.Hosting;

namespace Vafadar.Zanance.App;

public static class MauiProgram
{
#if WINDOWS
    private static bool _focusRingHooked;

#endif
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseVafadar(options =>
            {
                options.AppId = ZananceApp.AppId;
                options.SyncfusionLicenseKey = AppSecrets.SyncfusionLicenseKey;
                options.ConfigureLocalization = localization => localization.Resources.Add(AppStrings.ResourceManager);
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Figtree-Regular.ttf", "Figtree");
                fonts.AddFont("Figtree-SemiBold.ttf", "FigtreeSemiBold");
                fonts.AddFont("Figtree-Bold.ttf", "FigtreeBold");
                fonts.AddFont("Urbanist-SemiBold.ttf", "UrbanistSemiBold");
                fonts.AddFont("Urbanist-Bold.ttf", "UrbanistBold");
                fonts.AddFont("Vazirmatn-Regular.ttf", "Vazirmatn");
                fonts.AddFont("Vazirmatn-Bold.ttf", "VazirmatnBold");
            });

        builder.Services
            // The last open local profile (§3); the main profile is the original database.
            .AddZananceData(Profiles.ProfileService.StartupDatabasePath(DatabasePath()))
            // Each local profile keeps its own backups, retention and last-backup time in shared folders (D-34).
            .AddVafadarBackup(options => options.FileSet = Profiles.ProfileService.CurrentBackupSet)
            .AddSingleton<UndoService>()
            .AddSingleton<ReminderService>()
            .AddSingleton<Presentation.ThemeService>()
            .AddTransient<IMauiInitializeService, ReminderInitializer>()
            .AddSingleton<AppLockService>()
            .AddSingleton<Profiles.ProfileService>();

#if IOS
        // The app switcher snapshot is covered (SEC-02, D-23); Android uses FLAG_SECURE in MainActivity.
        builder.ConfigureLifecycleEvents(events => events.AddiOS(ios => ios
            .OnResignActivation(_ => PrivacyCover.Show())
            .SceneOnResignActivation(_ => PrivacyCover.Show())
            .OnActivated(_ => PrivacyCover.Hide())
            .SceneOnActivated(_ => PrivacyCover.Hide())));
#endif

        // Cloud backup to the user's own Google Drive or OneDrive, offered only when the OAuth clients are configured
        // for the build (D-35). Both providers work on Android, iOS and Windows; Google has one client per platform and
        // each build carries only its own (D-50).
        var cloud = new CloudSignIn(new CloudSignInOptions
        {
            MicrosoftClientId = AppSecrets.MicrosoftEntraClientId,
            GoogleAndroidClientId = AppSecrets.GoogleOAuthClientIdAndroid,
            GoogleIosClientId = AppSecrets.GoogleOAuthClientIdIos,
            GoogleWindowsClientId = AppSecrets.GoogleOAuthClientIdWindows,
        });
        builder.Services.AddVafadarCloudSignIn(options =>
        {
            options.MicrosoftClientId = cloud.Options.MicrosoftClientId;
            options.GoogleAndroidClientId = cloud.Options.GoogleAndroidClientId;
            options.GoogleIosClientId = cloud.Options.GoogleIosClientId;
            options.GoogleWindowsClientId = cloud.Options.GoogleWindowsClientId;
            options.BrowserCompletionMessage = () =>
                AppStrings.ResourceManager.GetString("Cloud_BrowserDone", System.Globalization.CultureInfo.CurrentUICulture) ?? string.Empty;
        });
        if (cloud.IsGoogleAvailable)
        {
            builder.Services.AddGoogleDriveBackupStorage();
        }

        if (cloud.IsMicrosoftAvailable)
        {
            builder.Services.AddOneDriveBackupStorage();
        }

#if ANDROID || IOS
        builder.UseLocalNotification();
        builder.Services.AddSingleton<IReminderScheduler, LocalNotificationScheduler>();
#else
        builder.Services.AddSingleton<IReminderScheduler, NoReminderScheduler>();
#endif

        builder.Services
            .AddTransient<AppShell>()
            .AddTransient<OnboardingPage>().AddTransient<OnboardingViewModel>()
            .AddTransient<HomePage>().AddTransient<HomeViewModel>()
            .AddTransient<AccountsPage>().AddTransient<AccountsViewModel>()
            .AddTransient<AccountEditorPage>().AddTransient<AccountEditorViewModel>()
            .AddTransient<AccountDetailPage>().AddTransient<AccountDetailViewModel>()
            .AddTransient<MorePage>().AddTransient<MoreViewModel>()
            .AddTransient<SettingsPage>().AddTransient<SettingsViewModel>()
            .AddTransient<TransactionsPage>().AddTransient<TransactionsViewModel>()
            .AddTransient<EntryEditorPage>().AddTransient<EntryEditorViewModel>()
            .AddTransient<EntryDetailPage>().AddTransient<EntryDetailViewModel>()
            .AddTransient<CategoriesPage>().AddTransient<CategoriesViewModel>()
            .AddTransient<CategoryEditorPage>().AddTransient<CategoryEditorViewModel>()
            .AddTransient<PlansPage>().AddTransient<PlansViewModel>()
            .AddTransient<PlanEditorPage>().AddTransient<PlanEditorViewModel>()
            .AddTransient<PlanDetailPage>().AddTransient<PlanDetailViewModel>()
            .AddTransient<OccurrencePage>().AddTransient<OccurrenceViewModel>()
            .AddTransient<BackupPage>().AddTransient<BackupViewModel>()
            .AddTransient<BudgetPage>().AddTransient<BudgetViewModel>()
            .AddTransient<BudgetEditorPage>().AddTransient<BudgetEditorViewModel>()
            .AddTransient<ReportsPage>().AddTransient<ReportsViewModel>()
            .AddTransient<ForecastPage>().AddTransient<ForecastViewModel>()
            .AddTransient<RatesPage>().AddTransient<RatesViewModel>()
            .AddTransient<Features.Templates.TemplatesPage>().AddTransient<Features.Templates.TemplatesViewModel>()
            .AddTransient<SplitEditorPage>().AddTransient<SplitEditorViewModel>()
            .AddTransient<SettlementPage>().AddTransient<SettlementViewModel>()
            .AddTransient<ReimbursementsPage>().AddTransient<ReimbursementsViewModel>()
            .AddTransient<LoanSchedulePage>().AddTransient<LoanScheduleViewModel>()
            .AddTransient<Features.Categories.RulesPage>().AddTransient<Features.Categories.RulesViewModel>()
            .AddTransient<Features.Goals.GoalPresenter>()
            .AddTransient<Features.Goals.GoalsPage>().AddTransient<Features.Goals.GoalsViewModel>()
            .AddTransient<Features.Goals.GoalEditorPage>().AddTransient<Features.Goals.GoalEditorViewModel>()
            .AddTransient<Features.Goals.GoalDetailPage>().AddTransient<Features.Goals.GoalDetailViewModel>()
            .AddTransient<ImportExportPage>().AddTransient<ImportExportViewModel>()
            .AddTransient<DisplayUnitsPage>().AddTransient<DisplayUnitsViewModel>()
            .AddTransient<Features.Profiles.ProfilesPage>().AddTransient<Features.Profiles.ProfilesViewModel>()
            .AddTransient<Features.About.AboutPage>().AddTransient<Features.About.NoticesPage>().AddTransient<Features.About.AboutViewModel>()
            .AddTransient<Features.Home.HomeLayoutPage>().AddTransient<Features.Home.HomeLayoutViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if WINDOWS
        // The keyboard focus ring in the action blue of the palette instead of black and white (D-40, D-42). WinUI 3 reads
        // the ring colours from the focused element itself, not from overridable app resources, so each element gets them
        // the moment it receives focus – also the parts MAUI does not create, such as the tabs of the shell – in the
        // colours of the current theme.
        builder.ConfigureLifecycleEvents(events => events.AddWindows(windows => windows.OnWindowCreated(_ =>
        {
            if (_focusRingHooked)
            {
                return;
            }

            _focusRingHooked = true;
            Microsoft.UI.Xaml.Input.FocusManager.GettingFocus += (_, args) =>
            {
                if (args.NewFocusedElement is Microsoft.UI.Xaml.FrameworkElement element)
                {
                    element.FocusVisualPrimaryBrush = Microsoft.Maui.Platform.ColorExtensions.ToPlatform(Presentation.Palette.Primary);
                    element.FocusVisualSecondaryBrush = Microsoft.Maui.Platform.ColorExtensions.ToPlatform(Presentation.Palette.CardBackground);
                }
            };
        })));

        // Screen readers name list rows after their item instead of MAUI's wrapper type (D-45).
        Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("ZananceRowNames", (handler, _) =>
        {
            if (handler.PlatformView is Microsoft.UI.Xaml.Controls.ListViewBase list && handler.VirtualView is ItemsView view)
            {
                Presentation.ListRowNames.Attach(list, view);
            }
        });
#endif

        return builder.Build();
    }

    // The database was called finance.db before the rename to Zanance (D-24): an existing file and its SQLite side
    // files are moved once, before anything opens the database. A newer zanance.db is never replaced.
    private static string DatabasePath()
    {
        var folder = FileSystem.AppDataDirectory;
        var path = Path.Combine(folder, ZananceApp.DatabaseFileName);
        var legacy = Path.Combine(folder, ZananceApp.LegacyDatabaseFileName);
        if (!File.Exists(path) && File.Exists(legacy))
        {
            foreach (var suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
            {
                if (File.Exists(legacy + suffix))
                {
                    File.Move(legacy + suffix, path + suffix);
                }
            }
        }

        return path;
    }
}
