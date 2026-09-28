using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
#if ANDROID || IOS
using Plugin.LocalNotification;
#endif
using Vafadar.Backup;
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
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddZananceData(DatabasePath())
            .AddVafadarBackup()
            .AddTransient<IMauiInitializeService, DatabaseInitializer>()
            .AddSingleton<UndoService>()
            .AddSingleton<ReminderService>()
            .AddSingleton<Presentation.ThemeService>()
            .AddTransient<IMauiInitializeService, ReminderInitializer>()
            .AddSingleton<AppLockService>();

#if IOS
        // The app switcher snapshot is covered (SEC-02, D-23); Android uses FLAG_SECURE in MainActivity.
        builder.ConfigureLifecycleEvents(events => events.AddiOS(ios => ios
            .OnResignActivation(_ => PrivacyCover.Show())
            .SceneOnResignActivation(_ => PrivacyCover.Show())
            .OnActivated(_ => PrivacyCover.Hide())
            .SceneOnActivated(_ => PrivacyCover.Hide())));
#endif

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
            .AddTransient<Features.Home.HomeLayoutPage>().AddTransient<Features.Home.HomeLayoutViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
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
