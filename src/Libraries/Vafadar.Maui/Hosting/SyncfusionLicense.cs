using Syncfusion.Licensing;

namespace Vafadar.Maui.Hosting;

/// <summary>
/// The one place where Syncfusion is licensed for every Vafadar app. The key is a build secret (see
/// docs/guides/secrets-and-configuration.md); it is validated offline by Syncfusion and is never logged, shown or sent.
/// </summary>
public static class SyncfusionLicense
{
    private static int _registered;

    /// <summary>Gets a value indicating whether a license key has been registered in this process.</summary>
    public static bool IsRegistered => Volatile.Read(ref _registered) == 1;

    /// <summary>
    /// Registers <paramref name="licenseKey"/> once, before any Syncfusion control is created. An empty key is ignored:
    /// the app still runs and Syncfusion shows its license notice (builds without secrets, e.g. forks).
    /// </summary>
    /// <returns><see langword="true"/> when a key was registered by this call.</returns>
    public static bool Register(string? licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey) || Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return false;
        }

        SyncfusionLicenseProvider.RegisterLicense(licenseKey.Trim());
        return true;
    }
}