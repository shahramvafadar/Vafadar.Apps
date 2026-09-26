using Syncfusion.Licensing;

namespace Vafadar.SyncfusionLicense.Tests;

public sealed class SyncfusionLicenseTests
{
    // The release workflow sets this, so a release never passes with the check skipped.
    private static bool Required => Environment.GetEnvironmentVariable("VAFADAR_REQUIRE_SYNCFUSION_LICENSE") == "true";

    [Fact]
    public void The_configured_key_is_valid_for_the_referenced_Syncfusion_version()
    {
        if (string.IsNullOrWhiteSpace(AppSecrets.SyncfusionLicenseKey))
        {
            Assert.False(Required, "No Syncfusion license key is configured for this build.");
            Assert.Skip("No Syncfusion license key is configured for this build.");
        }

        SyncfusionLicenseProvider.RegisterLicense(AppSecrets.SyncfusionLicenseKey);
        var valid = SyncfusionLicenseProvider.ValidateLicense([Platform.MAUI], out var message);

        // The message is Syncfusion's reason (e.g. a version mismatch); it never contains the key.
        Assert.True(valid, $"The Syncfusion license key is not valid for this package version: {message}");
    }
}