using Microsoft.Extensions.Logging;
#if ANDROID || IOS
using Plugin.LocalNotification;
#endif
using Vafadar.Backup;
using Vafadar.Finance.App.Features.Accounts;
using Vafadar.Finance.App.Features.Backup;
using Vafadar.Finance.App.Features.Budget;
using Vafadar.Finance.App.Features.Categories;
using Vafadar.Finance.App.Features.DataFiles;
using Vafadar.Finance.App.Features.Entries;
using Vafadar.Finance.App.Features.Forecast;
using Vafadar.Finance.App.Features.Home;
using Vafadar.Finance.App.Features.More;
using Vafadar.Finance.App.Features.Onboarding;
using Vafadar.Finance.App.Features.Plans;
using Vafadar.Finance.App.Features.Rates;
using Vafadar.Finance.App.Features.Reports;
using Vafadar.Finance.App.Features.Settings;
using Vafadar.Finance.App.Features.Transactions;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.App.Reminders;
using Vafadar.Finance.App.Security;
using Vafadar.Finance.App.Resources.Strings;
using Vafadar.Finance.Core;
using Vafadar.Finance.Data;
using Vafadar.Maui.Hosting;

namespace Vafadar.Finance.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseVafadar(options =>
            {
                options.AppId = FinanceApp.AppId;
                options.SyncfusionLicenseKey = AppSecrets.SyncfusionLicenseKey;
                options.ConfigureLocalization = localization => localization.Resources.Add(AppStrings.ResourceManager);
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services
            .AddFinanceData(Path.Combine(FileSystem.AppDataDirectory, FinanceApp.DatabaseFileName))
            .AddVafadarBackup()
            .AddTransient<IMauiInitializeService, DatabaseInitializer>()
            .AddSingleton<UndoService>()
            .AddSingleton<ReminderService>()
            .AddSingleton<AppLockService>();

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
            .AddTransient<ImportExportPage>().AddTransient<ImportExportViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
