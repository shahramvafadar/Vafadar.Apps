using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;

namespace Vafadar.Maui.Localization;

/// <summary>
/// Applies the saved language while the MAUI app is being built (before any page exists) and keeps the text
/// direction of open windows in sync when the language changes.
/// </summary>
internal sealed class LocalizationInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var localization = services.GetRequiredService<ILocalizationService>();
        localization.Initialize();

        localization.Changed += (_, _) =>
        {
            if (MainThread.IsMainThread)
            {
                FlowDirectionExtensions.ApplyToAllWindows(localization);
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => FlowDirectionExtensions.ApplyToAllWindows(localization));
            }
        };
    }
}
