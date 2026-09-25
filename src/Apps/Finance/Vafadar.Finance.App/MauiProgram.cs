using Microsoft.Extensions.Logging;
using Vafadar.Backup;
using Vafadar.Finance.App.Features.Accounts;
using Vafadar.Finance.App.Features.Home;
using Vafadar.Finance.App.Features.More;
using Vafadar.Finance.App.Features.Onboarding;
using Vafadar.Finance.App.Features.Settings;
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
            .AddTransient<IMauiInitializeService, DatabaseInitializer>();

        builder.Services
            .AddTransient<AppShell>()
            .AddTransient<OnboardingPage>().AddTransient<OnboardingViewModel>()
            .AddTransient<HomePage>().AddTransient<HomeViewModel>()
            .AddTransient<AccountsPage>().AddTransient<AccountsViewModel>()
            .AddTransient<AccountEditorPage>().AddTransient<AccountEditorViewModel>()
            .AddTransient<MorePage>().AddTransient<MoreViewModel>()
            .AddTransient<SettingsPage>().AddTransient<SettingsViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
