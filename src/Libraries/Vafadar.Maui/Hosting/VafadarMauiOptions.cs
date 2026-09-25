using Vafadar.Localization;

namespace Vafadar.Maui.Hosting;

/// <summary>
/// Configures <see cref="VafadarMauiAppBuilderExtensions.UseVafadar"/>.
/// </summary>
public sealed class VafadarMauiOptions
{
    /// <summary>
    /// Gets or sets the stable app id, identical on all platforms (e.g. <c>pro.vafadar.finance</c>).
    /// It identifies the app's backups, so it must never change once the app is released.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Syncfusion license key. Pass <c>AppSecrets.SyncfusionLicenseKey</c>; never hard-code it.
    /// When empty, Syncfusion controls run unlicensed and show a license banner.
    /// </summary>
    public string? SyncfusionLicenseKey { get; set; }

    /// <summary>Gets or sets a callback that configures localization (app resources, languages, defaults).</summary>
    public Action<LocalizationOptions>? ConfigureLocalization { get; set; }
}
