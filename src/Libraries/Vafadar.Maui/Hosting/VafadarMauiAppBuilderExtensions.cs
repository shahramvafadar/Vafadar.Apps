using FluentIcons.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Syncfusion.Licensing;
using Syncfusion.Maui.Core.Hosting;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Maui.Localization;

namespace Vafadar.Maui.Hosting;

/// <summary>
/// Bootstraps a Vafadar MAUI app.
/// </summary>
public static class VafadarMauiAppBuilderExtensions
{
    /// <summary>
    /// Registers the shared infrastructure every Vafadar app uses: Syncfusion (license + handlers), app environment,
    /// preferences-backed settings, localization with right-to-left support, and <see cref="TimeProvider"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// builder
    ///     .UseMauiApp&lt;App&gt;()
    ///     .UseVafadar(options =>
    ///     {
    ///         options.AppId = FinanceApp.AppId;
    ///         options.SyncfusionLicenseKey = AppSecrets.SyncfusionLicenseKey;
    ///         options.ConfigureLocalization = l => l.Resources.Add(AppStrings.ResourceManager);
    ///     });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseVafadar(this MauiAppBuilder builder, Action<VafadarMauiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new VafadarMauiOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.AppId))
        {
            throw new InvalidOperationException($"{nameof(VafadarMauiOptions)}.{nameof(VafadarMauiOptions.AppId)} must be set.");
        }

        // Must happen before any Syncfusion control is created.
        if (!string.IsNullOrWhiteSpace(options.SyncfusionLicenseKey))
        {
            SyncfusionLicenseProvider.RegisterLicense(options.SyncfusionLicenseKey);
        }

        builder.ConfigureSyncfusionCore();

        // Fluent UI System Icons (MIT), bundled offline (VIS-03).
        builder.UseFluentIcons();

#if WINDOWS
        // WinUI detects a text's base direction from its first strong character, so a Persian sentence that starts
        // with an amount would be laid out left-to-right. Use the element's flow direction instead, as Android does.
        Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping("VafadarReadingOrder", (handler, _) =>
            handler.PlatformView.TextReadingOrder = Microsoft.UI.Xaml.TextReadingOrder.UseFlowDirection);
#endif

        var services = builder.Services;
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(Preferences.Default);
        services.TryAddSingleton<ISettingsStore, MauiSettingsStore>();
        services.TryAddSingleton<IAppEnvironment>(new MauiAppEnvironment(options.AppId));
        services.AddVafadarLocalization(options.ConfigureLocalization);
        services.TryAddEnumerable(ServiceDescriptor.Transient<IMauiInitializeService, LocalizationInitializer>());

        return builder;
    }
}
