using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vafadar.Localization.Formatting;

namespace Vafadar.Localization;

/// <summary>
/// Registers localization services.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ILocalizationService"/>, <see cref="IDateFormatter"/> and the shared <see cref="Translator"/>.
    /// Requires an <see cref="Core.Settings.ISettingsStore"/> registration (MAUI apps get one from <c>UseVafadar()</c>).
    /// </summary>
    public static IServiceCollection AddVafadarLocalization(this IServiceCollection services, Action<LocalizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new LocalizationOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton(Translator.Instance);
        services.TryAddSingleton<ILocalizationService, LocalizationService>();
        services.TryAddSingleton<IDateFormatter, DateFormatter>();

        return services;
    }
}
