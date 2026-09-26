using Vafadar.Localization;

namespace Vafadar.Maui.Hosting;

/// <summary>
/// Configures <see cref="VafadarMauiAppBuilderExtensions.UseVafadar"/>.
/// </summary>
public sealed class VafadarMauiOptions
{
    /// <summary>
    /// Gets or sets the stable app id, identical on all platforms (e.g. <c>pro.vafadar.zanance</c>).
    /// It identifies the app's backups, so it must never change once the app is released.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Syncfusion license key. Pass <c>AppSecrets.SyncfusionLicenseKey</c>, which every MAUI app gets
    /// from eng/AppSecrets.targets; never hard-code it. An app that needs another key sets its own
    /// <c>SyncfusionLicenseKey</c> MSBuild property. When empty, Syncfusion controls show a license notice.
    /// </summary>
    public string? SyncfusionLicenseKey { get; set; }

    /// <summary>Gets or sets a callback that configures localization (app resources, languages, defaults).</summary>
    public Action<LocalizationOptions>? ConfigureLocalization { get; set; }
}
