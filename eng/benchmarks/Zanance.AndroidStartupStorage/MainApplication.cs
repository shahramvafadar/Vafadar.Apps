using Android.App;
using Android.Runtime;
using Vafadar.Maui.Hosting;
using Vafadar.Zanance.App.Resources.Strings;

namespace Vafadar.Zanance.App;

/// <summary>Builds only the original translated startup controls; never constructs the financial application or storage.</summary>
[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiApp.CreateBuilder()
        .UseMauiApp<StartupStorageApplication>()
        .UseVafadar(options =>
        {
            options.AppId = MainActivity.Package;
            options.SyncfusionLicenseKey = AppSecrets.SyncfusionLicenseKey;
            options.ConfigureLocalization = localization => localization.Resources.Add(AppStrings.ResourceManager);
        })
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("Figtree-Regular.ttf", "Figtree");
            fonts.AddFont("Figtree-SemiBold.ttf", "FigtreeSemiBold");
            fonts.AddFont("Urbanist-Bold.ttf", "UrbanistBold");
            fonts.AddFont("Vazirmatn-Regular.ttf", "Vazirmatn");
            fonts.AddFont("Vazirmatn-Bold.ttf", "VazirmatnBold");
        }).Build();
}
